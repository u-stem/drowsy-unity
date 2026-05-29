using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="RestrictAllUsageAndAbandonInfluenceMarkerEffect"/>(No.09「強引過ぎる一手」)を Unity SO で表現する POCO。
    /// フィールドなし marker。
    /// </summary>
    [Serializable]
    public sealed class RestrictAllUsageAndAbandonInfluenceMarkerEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal RestrictAllUsageAndAbandonInfluenceMarkerEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new RestrictAllUsageAndAbandonInfluenceMarkerEffect();
    }
}
