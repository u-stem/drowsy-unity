using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="EarlyWinTriggerEffect"/> を Unity SO で表現する POCO(INF-029 / INF-030)。
    /// フィールドなし(parameterless marker、夜 + 持ち点 ≥ 100 で発火)。
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
