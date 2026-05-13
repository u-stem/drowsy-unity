using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="EarlyWinTriggerEffect"/>(M3-PR1、ADR-0010 §5)を Unity SO で表現する POCO
    /// (M4-PR3 で導入、INF-029 / INF-030)。フィールドなし(parameterless marker、夜 + 持ち点 ≥ 100 で発火)。
    /// </summary>
    [Serializable]
    public sealed class EarlyWinTriggerEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal EarlyWinTriggerEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new EarlyWinTriggerEffect();
    }
}
