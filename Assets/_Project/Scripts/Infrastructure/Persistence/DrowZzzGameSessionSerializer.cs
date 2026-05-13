using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Infrastructure.Persistence.Models;

namespace Drowsy.Infrastructure.Persistence
{
    /// <summary>
    /// <see cref="DrowZzzGameSession"/> を JSON ファイルに保存 / 読み込みする serializer。
    /// </summary>
    /// <remarks>
    /// ADR-0012 §7「`DrowZzzGameSession` JSON 永続化(サブスコープ、M4-PR5)」+
    /// JIT 確定(2026-05-13 ユーザー):
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
    /// <b>WebGL/IL2CPP (AOT)</b>: ADR-0012 §「M4-PR5 着手時の JIT 確認項目」確定通り、本 PR は EditMode round-trip
    /// テスト + <c>link.xml</c> 設定までを完成基準とし、WebGL build 実機検証は M4-PR7(M4 完成 PR)に送る
    /// (2026-05-13 ユーザー JIT 確定)。link.xml は <see cref="Drowsy.Domain"/> / <see cref="Drowsy.Application"/>
    /// 配下の serialize 対象型を <c>preserve="all"</c> で保持する。
    /// </para>
    /// </remarks>
    public sealed class DrowZzzGameSessionSerializer
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
        /// 標準のセーブファイル path(<c>Application.persistentDataPath/drowzzz/{fileName}</c>)を返す helper。
        /// </summary>
        /// <param name="fileName">セーブファイル名(default = <c>session.json</c>)</param>
        /// <remarks>
        /// ADR-0012 §「M4-PR5 着手時の JIT 確認項目」確定通り、<c>Application.persistentDataPath/drowzzz/</c>
        /// サブディレクトリを採用(2026-05-13 ユーザー JIT 確定)。multi-slot save / replay log の追加時にも
        /// <c>drowzzz/</c> サブディレクトリ配下で namespace 分離可能。
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
