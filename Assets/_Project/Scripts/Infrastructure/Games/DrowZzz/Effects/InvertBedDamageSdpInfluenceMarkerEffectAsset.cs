using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="InvertBedDamageSdpInfluenceMarkerEffect"/>(No.08「廻るための知恵」、2026-05-17 で導入)を
    /// Unity SO で表現する POCO。フィールドなし marker。
    /// </summary>
    [Serializable]
    public sealed class InvertBedDamageSdpInfluenceMarkerEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal InvertBedDamageSdpInfluenceMarkerEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new InvertBedDamageSdpInfluenceMarkerEffect();
    }
}
