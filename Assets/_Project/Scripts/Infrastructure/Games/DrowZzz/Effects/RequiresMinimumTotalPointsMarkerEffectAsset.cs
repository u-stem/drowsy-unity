using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="RequiresMinimumTotalPointsMarkerEffect"/>(M3-PR6、ADR-0011 §6)を Unity SO で表現する POCO
    /// (M4-PR3 で導入、INF-035 / INF-036)。<see cref="int"/> 1 フィールド(<see cref="Threshold"/>)。
    /// </summary>
    /// <remarks>
    /// <see cref="RequiresMinimumTotalPointsMarkerEffect"/> record は <c>Threshold >= 1</c> を ctor / init で検証する。
    /// 違反値の場合 <see cref="ArgumentOutOfRangeException"/> が伝播し、上位 <see cref="ScriptableObjectCardCatalog.BuildEffectsFromAssets"/> で
    /// catch + skip + LogError(INF-019)。
    /// </remarks>
    [Serializable]
    public sealed class RequiresMinimumTotalPointsMarkerEffectAsset : EffectAsset
    {
        [SerializeField] private int _threshold;

        /// <summary>使用に必要な最小 TotalPoints(<see cref="RequiresMinimumTotalPointsMarkerEffect.Threshold"/>、1 以上必須)。</summary>
        public int Threshold => _threshold;

        /// <summary>テスト用 ctor。</summary>
        internal RequiresMinimumTotalPointsMarkerEffectAsset(int threshold)
        {
            _threshold = threshold;
        }

        /// <inheritdoc />
        public override IEffect ToDomain() => new RequiresMinimumTotalPointsMarkerEffect(_threshold);
    }
}
