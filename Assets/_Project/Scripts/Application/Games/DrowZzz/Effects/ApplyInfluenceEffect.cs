using System;
using Drowsy.Application.Games.DrowZzz.Influences;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 指定プレイヤー(<see cref="SdpTarget.Self"/> / <see cref="SdpTarget.Opponent"/>)の影響リストに
    /// <see cref="Influence"/> を追加する効果。
    /// </summary>
    /// <param name="Target">影響を付与する対象プレイヤー</param>
    /// <param name="Influence">付与する影響(<see cref="PlayerInfluence"/>、null 不可)</param>
    /// <remarks>
    /// 継続影響(Influence)の付与系効果。カード No.02「緑の侵攻」の「自分 / 相手にこの手段が持つ影響を受けさせる」に使う。
    /// <para>
    /// 付与の重複は許容する(同じ影響を複数回付与すると、それぞれ独立した残カウントで保有 list に追加される)。
    /// list 末尾に追加され、Tick 時は先頭(index 0)から評価する FIFO 規約(<c>DrowZzzRule.TickInfluences</c>)。
    /// </para>
    /// </remarks>
    public sealed record ApplyInfluenceEffect(SdpTarget Target, PlayerInfluence Influence) : IEffect
    {
        // null 防御の二重ガード(positional ctor 経由)
        private readonly PlayerInfluence _influence = Influence ?? throw new ArgumentNullException(nameof(Influence));

        /// <summary>付与する影響。null 不可。</summary>
        public PlayerInfluence Influence
        {
            get => _influence;
            init => _influence = value ?? throw new ArgumentNullException(nameof(Influence));
        }
    }
}
