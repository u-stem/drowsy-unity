using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="DoubleBedDamageSdpInfluenceMarkerEffect"/>(No.06「牙の届かぬ領域」、2026-05-17 で導入)を
    /// Unity SO で表現する POCO。フィールドなし marker(<c>PlayerInfluence.TickEffect</c> 用)。
    /// </summary>
    [Serializable]
    public sealed class DoubleBedDamageSdpInfluenceMarkerEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal DoubleBedDamageSdpInfluenceMarkerEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new DoubleBedDamageSdpInfluenceMarkerEffect();
    }
}
