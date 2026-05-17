using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AdjustSdpByHandCountEffect"/>(No.11「機械仕掛けの冬将軍」、2026-05-17 で導入)を
    /// Unity SO で表現する POCO。フィールドなし。
    /// </summary>
    [Serializable]
    public sealed class AdjustSdpByHandCountEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal AdjustSdpByHandCountEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new AdjustSdpByHandCountEffect();
    }
}
