using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Persistence;

namespace Drowsy.Presentation.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="IDrowZzzGameSessionSerializer"/> の Presenter テスト用モック実装(M5-PR2)。
    /// </summary>
    /// <remarks>
    /// <c>Drowsy.Application.Tests.Stubs.InMemoryDrowZzzGameSessionSerializer</c> は Application.Tests
    /// asmdef 内に閉じており、本 Presentation.Tests からは <c>Drowsy.Application.Tests</c> reference 経由で
    /// 参照可能だが、Presenter テスト固有の制御(<see cref="LoadAsyncBehavior"/> = FileNotFound / Throws /
    /// ReturnSession / Cancel)を持たせたいため、本 PR では Presentation.Tests 内に専用 Mock を新設する。
    /// <para>
    /// <b>制御ポイント</b>:<see cref="LoadAsyncBehavior"/> を設定することで、Presenter の <see cref="BootAsync"/>
    /// 経路を「LoadAsync 成功」/ 「FileNotFoundException」/ 「OperationCanceledException」/ 「その他 Exception」の
    /// 各分岐に駆動できる。
    /// </para>
    /// </remarks>
    public sealed class MockDrowZzzGameSessionSerializer : IDrowZzzGameSessionSerializer
    {
        /// <summary>LoadAsync の挙動指定。</summary>
        public enum LoadBehavior
        {
            /// <summary><see cref="LoadAsyncReturnSession"/> を返す(default)。</summary>
            ReturnSession,
            /// <summary><see cref="FileNotFoundException"/> を投げる。</summary>
            ThrowFileNotFound,
            /// <summary>呼び出し時の <c>ct</c> を無視して <see cref="OperationCanceledException"/> を投げる。</summary>
            ThrowOperationCanceled,
        }

        /// <summary>LoadAsync の挙動(default = <see cref="LoadBehavior.ReturnSession"/>)。</summary>
        public LoadBehavior LoadAsyncBehavior { get; set; } = LoadBehavior.ReturnSession;

        /// <summary><see cref="LoadAsyncBehavior"/> が <see cref="LoadBehavior.ReturnSession"/> のときに返す session。</summary>
        public DrowZzzGameSession LoadAsyncReturnSession { get; set; }

        /// <summary>SaveAsync 呼び出し回数(テスト assertion 用)。</summary>
        public int SaveAsyncCallCount { get; private set; }

        /// <summary>LoadAsync 呼び出し回数(テスト assertion 用)。</summary>
        public int LoadAsyncCallCount { get; private set; }

        /// <inheritdoc />
        public void Save(DrowZzzGameSession session, string path)
        {
            // M5-PR2 では同期 API は使わない、必要なら別 PR で機能拡張する。
            throw new NotImplementedException("MockDrowZzzGameSessionSerializer.Save は M5-PR2 では未使用");
        }

        /// <inheritdoc />
        public DrowZzzGameSession Load(string path)
        {
            throw new NotImplementedException("MockDrowZzzGameSessionSerializer.Load は M5-PR2 では未使用");
        }

        /// <inheritdoc />
        public UniTask SaveAsync(DrowZzzGameSession session, string path, CancellationToken ct = default)
        {
            SaveAsyncCallCount++;
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask<DrowZzzGameSession> LoadAsync(string path, CancellationToken ct = default)
        {
            LoadAsyncCallCount++;
            // switch は exhaustive を明示する設計(LoadBehavior に新ケース追加時の漏れを実行時 fail-fast で検出、W-2 反映)。
            switch (LoadAsyncBehavior)
            {
                case LoadBehavior.ThrowFileNotFound:
                    throw new FileNotFoundException($"Mock: ファイル不在を模擬: {path}", path);
                case LoadBehavior.ThrowOperationCanceled:
                    throw new OperationCanceledException("Mock: cancellation を模擬");
                case LoadBehavior.ReturnSession:
                    if (LoadAsyncReturnSession is null)
                    {
                        throw new InvalidOperationException(
                            "MockDrowZzzGameSessionSerializer.LoadAsyncReturnSession が未設定です。" +
                            "ReturnSession 挙動の場合は事前にテストフィクスチャで session を設定してください。");
                    }
                    return UniTask.FromResult(LoadAsyncReturnSession);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(LoadAsyncBehavior),
                        LoadAsyncBehavior,
                        "未知の LoadBehavior 値です。LoadBehavior enum 拡張時は本 switch にもケースを追加してください。");
            }
        }
    }
}
