using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="UsageRestrictionMarkerEffect"/> を Unity SO で表現する POCO(INF-037 / INF-038)。
    /// フィールドなし marker(2 役兼用:効果列内 trigger + Influence TickEffect)。
    /// </summary>
    [Serializable]
    public sealed class UsageRestrictionMarkerEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal UsageRestrictionMarkerEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new UsageRestrictionMarkerEffect();
    }
}
