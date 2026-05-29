using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Drowsy.Application.Games.DrowZzz;

namespace Drowsy.Application.Persistence
{
    /// <summary>
    /// <see cref="DrowZzzGameSession"/> を永続化するための抽象。
    /// </summary>
    /// <remarks>
    /// Application 層が interface を定義し Infrastructure 層が具象を実装する Ports &amp; Adapters パターン
    /// (CLAUDE.md §5 アーキテクチャ依存ルール準拠)。
    /// <para>
    /// 同期 API(<see cref="Save"/> / <see cref="Load"/>)と非同期 API(<see cref="SaveAsync"/> / <see cref="LoadAsync"/>)の両方を提供する。
    /// 引数順は session-first / path-only で統一。
    /// </para>
    /// <para>
    /// <see cref="LoadAsync"/> の戻り値は <see cref="UniTask{T}"/>(non-null)。ファイル不在は
    /// <see cref="FileNotFoundException"/> で表現する。
    /// </para>
    /// <para>
    /// セーブパスの標準値ヘルパー(<c>persistentDataPath</c> 配下のサブディレクトリ等)は実装側の
    /// <c>static</c> メンバとして提供する。C# 8 / .NET Standard 2.1 範囲では interface に <c>static</c>
    /// メンバを定義できないため本 interface には含めず、Bootstrap で <c>RegisterInstance(string)</c> として
    /// Project Singleton 化する。
    /// </para>
    /// </remarks>
    public interface IDrowZzzGameSessionSerializer
    {
        /// <summary>
        /// <paramref name="session"/> を <paramref name="path"/> に同期保存する。
        /// </summary>
        /// <param name="session">永続化対象の session(null 不可)</param>
        /// <param name="path">保存先 path(null・空・空白のみ不可)</param>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> が null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> が null・空・空白のみ</exception>
        void Save(DrowZzzGameSession session, string path);

        /// <summary>
        /// <paramref name="path"/> から同期読み込みする。
        /// </summary>
        /// <param name="path">読み込み元 path(null・空・空白のみ不可)</param>
        /// <returns>復元された <see cref="DrowZzzGameSession"/></returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> が null・空・空白のみ</exception>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> が存在しない</exception>
        /// <exception cref="InvalidDataException">JSON が破損している、または schemaVersion 不一致</exception>
        DrowZzzGameSession Load(string path);

        /// <summary>
        /// <paramref name="session"/> を <paramref name="path"/> に非同期保存する。
        /// </summary>
        /// <param name="session">永続化対象の session(null 不可)</param>
        /// <param name="path">保存先 path(null・空・空白のみ不可)</param>
        /// <param name="ct">キャンセル要求(default 可)</param>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> が null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> が null・空・空白のみ</exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> 経由でキャンセル要求</exception>
        UniTask SaveAsync(DrowZzzGameSession session, string path, CancellationToken ct = default);

        /// <summary>
        /// <paramref name="path"/> から非同期読み込みする。
        /// </summary>
        /// <param name="path">読み込み元 path(null・空・空白のみ不可)</param>
        /// <param name="ct">キャンセル要求(default 可)</param>
        /// <returns>復元された <see cref="DrowZzzGameSession"/>(non-null)</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> が null・空・空白のみ</exception>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> が存在しない</exception>
        /// <exception cref="InvalidDataException">JSON が破損している、または schemaVersion 不一致</exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> 経由でキャンセル要求</exception>
        UniTask<DrowZzzGameSession> LoadAsync(string path, CancellationToken ct = default);
    }
}
