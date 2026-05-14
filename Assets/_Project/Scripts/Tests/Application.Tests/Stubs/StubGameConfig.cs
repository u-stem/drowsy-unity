using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Configuration;

namespace Drowsy.Application.Tests.Stubs
{
    /// <summary>
    /// テスト用の <see cref="IGameConfig"/> stub 実装。
    /// デフォルトコンストラクタは DrowZzz の本物 FdpPool <c>[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]</c>
    /// + DDP プール 39 要素(<see cref="DdpPoolConstants.BuildDefaultPool"/>)を返す。
    /// テスト時に異なる値で検証したい場合は引数付きコンストラクタで上書きする(明示 <c>null</c> は <see cref="ArgumentNullException"/>)。
    /// </summary>
    /// <remarks>
    /// 本物の <c>ScriptableObject</c> ベース実装は M2 以降で <c>Drowsy.Infrastructure</c> 配下に追加予定
    /// (ADR-0006 §1.4 の追加予定プロパティ表参照)。
    /// M2-PR4 で <see cref="IGameConfig.DdpPool"/> 追加に追随して 2 引数 ctor を新設(後方互換維持のため
    /// 既存 1 引数 ctor は DdpPool デフォルト使用、ADR-0009 §「DDP プールの構造」)。
    /// </remarks>
    public sealed class StubGameConfig : IGameConfig
    {
        // DrowZzz の本物 FdpPool (ADR-0006 §M1)
        private static readonly IReadOnlyList<int> DefaultFdpPool =
            new[] { 0, 10, 20, 30, 35, 40, 45, 50, 55, 60 };

        public IReadOnlyList<int> FdpPool { get; }

        public IReadOnlyList<int> DdpPool { get; }

        /// <summary>デフォルト FdpPool + デフォルト DdpPool で生成。</summary>
        public StubGameConfig() : this(DefaultFdpPool, DdpPoolConstants.BuildDefaultPool()) { }

        /// <summary>明示的な FdpPool + デフォルト DdpPool で生成。</summary>
        public StubGameConfig(IReadOnlyList<int> fdpPool)
            : this(fdpPool, DdpPoolConstants.BuildDefaultPool()) { }

        /// <summary>明示的な FdpPool + 明示的な DdpPool で生成。両者 null なら <see cref="ArgumentNullException"/>。</summary>
        public StubGameConfig(IReadOnlyList<int> fdpPool, IReadOnlyList<int> ddpPool)
        {
            FdpPool = fdpPool ?? throw new ArgumentNullException(nameof(fdpPool));
            DdpPool = ddpPool ?? throw new ArgumentNullException(nameof(ddpPool));
        }
    }
}
