using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Persistence;
using Drowsy.Infrastructure.Persistence.Models;
using Newtonsoft.Json;

namespace Drowsy.Infrastructure.Persistence
{
    /// <summary>
    /// <see cref="DrowZzzGameSession"/> を JSON ファイルに保存 / 読み込みする serializer。
    /// </summary>
    /// <remarks>
    /// <see cref="DrowZzzGameSession"/> を JSON ファイルに保存 / 読み込みする実装方針:
    /// <list type="bullet">
    /// <item>Serializer: Newtonsoft.Json(<c>com.unity.nuget.newtonsoft-json</c>)</item>
    /// <item>Discriminator: カスタム JsonConverter で <c>"type"</c> 明示(<see cref="Converters.EffectJsonConverter"/> /
    /// <see cref="Converters.GameOutcomeJsonConverter"/>)</item>
    /// <item>RNG seed: Phase 3 送り(本 PR は <see cref="DrowZzzGameSession"/> round-trip のみ)</item>
    /// <item>Save 先: <see cref="DefaultSavePath(string)"/> で <c>Application.persistentDataPath/drowzzz/</c> サブディレクトリ
    /// (呼び出し側が path 指定する API を維持しつつ、テスト容易性 + 標準パス helper の両立)</item>
    /// <item>schemaVersion: <see cref="PersistedSessionV1"/> 固定(本 PR は migration なし、Phase 3 候補)</item>
    /// </list>
    /// <para>
    /// <b>API 設計</b>: <see cref="Save"/> / <see cref="Load"/> 共に path を引数として受け取り、Unity 依存
    /// (<c>Application.persistentDataPath</c>)は <see cref="DefaultSavePath(string)"/> helper に局所化する。
    /// テストは <c>Path.GetTempPath</c> 系で隔離可能、本番経路は呼び出し側で <see cref="DefaultSavePath(string)"/>
    /// を使う想定。
    /// </para>
    /// <para>
    /// <b>WebGL/IL2CPP (AOT)</b>: link.xml は <see cref="Drowsy.Domain"/> / <see cref="Drowsy.Application"/>
    /// 配下の serialize 対象型を <c>preserve="all"</c> で保持する。
    /// </para>
    /// <para>
    /// <b>非同期 API</b>: <see cref="IDrowZzzGameSessionSerializer"/> を実装し、
    /// 非同期 API(<see cref="SaveAsync"/> / <see cref="LoadAsync"/>)は同期版を <c>UniTask.RunOnThreadPool</c>
    /// ラップする(WebGL では Main Thread fallback、必要なら <c>UniTask.Yield</c> 挿入や
    /// <c>StreamReader.ReadToEndAsync</c> 再実装に切替)。
    /// 同期 API(<see cref="Save"/> / <see cref="Load"/>)はシグネチャ・挙動を完全維持する。
    /// </para>
    /// </remarks>
    public sealed class DrowZzzGameSessionSerializer : IDrowZzzGameSessionSerializer
    {
        private readonly JsonSerializerSettings _settings;

        /// <summary>標準設定で <see cref="DrowZzzGameSessionSerializer"/> を生成する。</summary>
        public DrowZzzGameSessionSerializer()
        {
            _settings = DrowZzzJsonSettings.Create();
        }

        /// <summary>
        /// <paramref name="session"/> を <paramref name="path"/> に JSON で保存する。
        /// 親ディレクトリは自動作成され、既存ファイルは上書きされる。
        /// </summary>
        /// <param name="session">永続化対象の session</param>
        /// <param name="path">保存先 path(絶対 / 相対どちらも可、相対の場合は <c>Directory.GetCurrentDirectory</c> 起点)</param>
        /// <exception cref="ArgumentNullException">session が null</exception>
        /// <exception cref="ArgumentException">path が null・空・空白のみ</exception>
        public void Save(DrowZzzGameSession session, string path)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path は null・空・空白のみにできません", nameof(path));
            }

            var dto = PersistedSessionV1.FromDomain(session);
            var json = JsonConvert.SerializeObject(dto, _settings);

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        /// <summary>
        /// <paramref name="path"/> から JSON を読み込んで <see cref="DrowZzzGameSession"/> を再構築する。
        /// </summary>
        /// <param name="path">読み込み元 path</param>
        /// <returns>復元された <see cref="DrowZzzGameSession"/></returns>
        /// <exception cref="ArgumentException">path が null・空・空白のみ</exception>
        /// <exception cref="FileNotFoundException">path が存在しない</exception>
        /// <exception cref="InvalidDataException">JSON が破損している、または schemaVersion 不一致</exception>
        public DrowZzzGameSession Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path は null・空・空白のみにできません", nameof(path));
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"セーブファイルが存在しません: {path}", path);
            }

            var json = File.ReadAllText(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            PersistedSessionV1 dto;
            try
            {
                dto = JsonConvert.DeserializeObject<PersistedSessionV1>(json, _settings);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"セーブファイル JSON が不正です: {path}", ex);
            }
            if (dto is null)
            {
                throw new InvalidDataException($"セーブファイル JSON の deserialize 結果が null です: {path}");
            }
            if (dto.SchemaVersion != 1)
            {
                throw new InvalidDataException(
                    $"未対応の schemaVersion: {dto.SchemaVersion}(本 serializer は schemaVersion=1 のみ対応、" +
                    "migration は Phase 3 候補)");
            }

            return dto.ToDomain();
        }

        /// <summary>
        /// <see cref="Save"/> を ThreadPool 上で非同期実行する。
        /// </summary>
        /// <param name="session">永続化対象の session</param>
        /// <param name="path">保存先 path</param>
        /// <param name="ct">キャンセル要求</param>
        /// <remarks>
        /// 同期版を <c>UniTask.RunOnThreadPool</c> でラップする。
        /// WebGL は ThreadPool が Main Thread fallback になるため、I/O ブロックが UI フリーズを
        /// 引き起こす場合は <c>UniTask.Yield</c> 挿入 / <c>StreamReader.ReadToEndAsync</c>
        /// 再実装等への切替を検討する。引数 null / 空白の検査は同期チェックで先に行い、ThreadPool
        /// 内では <see cref="Save"/> の検査と二重に走らないよう注意(検査結果は同じ例外型なので、外側で
        /// 早期 throw して呼び出し側の <c>try/catch</c> を素直に書けるようにする)。
        /// </remarks>
        public UniTask SaveAsync(DrowZzzGameSession session, string path, CancellationToken ct = default)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path は null・空・空白のみにできません", nameof(path));
            }
            return UniTask.RunOnThreadPool(() => Save(session, path), cancellationToken: ct);
        }

        /// <summary>
        /// <see cref="Load"/> を ThreadPool 上で非同期実行する。
        /// </summary>
        /// <param name="path">読み込み元 path</param>
        /// <param name="ct">キャンセル要求</param>
        /// <returns>復元された <see cref="DrowZzzGameSession"/>(non-null、ファイル不在は <see cref="FileNotFoundException"/>)</returns>
        /// <remarks>
        /// 戻り値は non-null。ファイル不在 / JSON 破損 /
        /// schemaVersion 不一致は <see cref="Load"/> 同様の例外で表現する(同期版と非同期版で例外契約を
        /// 完全一致させる方が、Presenter <c>try/catch</c> 経路で同期版と非同期版を入れ替えても等価)。
        /// </remarks>
        public UniTask<DrowZzzGameSession> LoadAsync(string path, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path は null・空・空白のみにできません", nameof(path));
            }
            return UniTask.RunOnThreadPool(() => Load(path), cancellationToken: ct);
        }

        /// <summary>
        /// 標準のセーブファイル path(<c>Application.persistentDataPath/drowzzz/{fileName}</c>)を返す helper。
        /// </summary>
        /// <param name="fileName">セーブファイル名(default = <c>session.json</c>)</param>
        /// <remarks>
        /// セーブ先は <c>Application.persistentDataPath/drowzzz/</c> サブディレクトリ。
        /// multi-slot save / replay log の追加時にも <c>drowzzz/</c> サブディレクトリ配下で namespace 分離可能。
        /// <para>
        /// 本 method は <c>UnityEngine.Application.persistentDataPath</c> に依存するため、テストでは使わず
        /// <see cref="Save"/> / <see cref="Load"/> に <c>Path.Combine(Path.GetTempPath(), ...)</c> を直接渡す。
        /// </para>
        /// </remarks>
        public static string DefaultSavePath(string fileName = "session.json")
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("fileName は null・空・空白のみにできません", nameof(fileName));
            }
            return Path.Combine(UnityEngine.Application.persistentDataPath, "drowzzz", fileName);
        }
    }
}
