using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Persistence;

namespace Drowsy.Application.Tests.Stubs
{
    /// <summary>
    /// <see cref="IDrowZzzGameSessionSerializer"/> のインメモリ fake 実装。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §5.2「<c>IDrowZzzGameSessionSerializer</c> interface 抽出」の契約テストおよび M5-PR2 以降の
    /// Presenter 単体テストで利用する。実 Infrastructure は Newtonsoft.Json + ファイル I/O だが、本 fake は
    /// <c>Dictionary&lt;string, DrowZzzGameSession&gt;</c> をストアにして path をキーに直接保持する。
    /// <para>
    /// <b>変換なし</b>:本 fake は session を「そのまま保存し、そのまま返す」設計とし、JSON round-trip 等の
    /// シリアライズロジックは検証範囲外とする(それは Infrastructure.Tests 側で 43 テストが検証する)。本 fake
    /// が担保するのは「<see cref="IDrowZzzGameSessionSerializer"/> の契約(引数検査・例外型・Async/同期の等価性)」のみ。
    /// </para>
    /// <para>
    /// <b>例外契約</b>:Infrastructure 実装と同一の例外型を投げる(ADR-0016 §5.2):
    /// <list type="bullet">
    /// <item><c>session</c> が null → <see cref="ArgumentNullException"/></item>
    /// <item><c>path</c> が null・空・空白のみ → <see cref="ArgumentException"/></item>
    /// <item>Load / LoadAsync で path 未保存 → <see cref="FileNotFoundException"/></item>
    /// <item>SaveAsync / LoadAsync で <c>CancellationToken</c> が cancelled → <see cref="OperationCanceledException"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>検証順序</b>:Infrastructure 具象実装と一致させる(① 引数 null チェック → ② path whitespace チェック →
    /// ③ <c>ct.ThrowIfCancellationRequested()</c> → ④ 本体実行)。これにより
    /// <c>(session=null, ct=cancelled)</c> のような両条件成立時の例外型が両実装で一致し、契約テストの保証範囲外の
    /// 組合せでも Presenter <c>try/catch</c> 経路の挙動が安定する。
    /// </para>
    /// </remarks>
    public sealed class InMemoryDrowZzzGameSessionSerializer : IDrowZzzGameSessionSerializer
    {
        private readonly Dictionary<string, DrowZzzGameSession> _store = new();

        /// <summary>保存済の path 数(テスト assertion 用)。</summary>
        public int StoredCount => _store.Count;

        /// <summary>指定 path が保存済かどうか(テスト assertion 用)。</summary>
        public bool Contains(string path) => path is not null && _store.ContainsKey(path);

        /// <inheritdoc />
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
            _store[path] = session;
        }

        /// <inheritdoc />
        public DrowZzzGameSession Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path は null・空・空白のみにできません", nameof(path));
            }
            if (!_store.TryGetValue(path, out var session))
            {
                throw new FileNotFoundException($"インメモリストアに path が存在しません: {path}", path);
            }
            return session;
        }

        /// <inheritdoc />
        public UniTask SaveAsync(DrowZzzGameSession session, string path, CancellationToken ct = default)
        {
            // 検証順序は Infrastructure 具象実装と一致させる(クラス xmldoc §「検証順序」参照)。
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path は null・空・空白のみにできません", nameof(path));
            }
            ct.ThrowIfCancellationRequested();
            _store[path] = session;
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask<DrowZzzGameSession> LoadAsync(string path, CancellationToken ct = default)
        {
            // 検証順序は Infrastructure 具象実装と一致させる(クラス xmldoc §「検証順序」参照)。
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path は null・空・空白のみにできません", nameof(path));
            }
            ct.ThrowIfCancellationRequested();
            if (!_store.TryGetValue(path, out var session))
            {
                throw new FileNotFoundException($"インメモリストアに path が存在しません: {path}", path);
            }
            return UniTask.FromResult(session);
        }
    }
}
