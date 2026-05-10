using System;
using System.Collections.Generic;
using Drowsy.Domain.Configuration;

namespace Drowsy.Application.Tests.Stubs
{
    /// <summary>
    /// テスト用の <see cref="IGameConfig"/> stub 実装。
    /// デフォルトコンストラクタは DrowZzz の本物 FdpPool <c>[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]</c> を返す。
    /// テスト時に異なる値で検証したい場合は引数付きコンストラクタで上書きする(明示 <c>null</c> は <see cref="ArgumentNullException"/>)。
    /// </summary>
    /// <remarks>
    /// 本物の <c>ScriptableObject</c> ベース実装は M2 以降で <c>Drowsy.Infrastructure</c> 配下に追加予定
    /// (ADR-0006 §1.4 の追加予定プロパティ表参照)。
    /// </remarks>
    internal sealed class StubGameConfig : IGameConfig
    {
        // DrowZzz の本物 FdpPool (ADR-0006 §M1)
        private static readonly IReadOnlyList<int> DefaultFdpPool =
            new[] { 0, 10, 20, 30, 35, 40, 45, 50, 55, 60 };

        public IReadOnlyList<int> FdpPool { get; }

        /// <summary>デフォルト FdpPool で生成。</summary>
        public StubGameConfig() : this(DefaultFdpPool) { }

        /// <summary>明示的な FdpPool で生成。<paramref name="fdpPool"/> が null なら <see cref="ArgumentNullException"/>。</summary>
        public StubGameConfig(IReadOnlyList<int> fdpPool)
        {
            FdpPool = fdpPool ?? throw new ArgumentNullException(nameof(fdpPool));
        }
    }
}
