using System;
using System.Collections.Generic;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="ChoiceEffectAsset"/> の 1 分岐(<see cref="ChoiceEffect.Branches"/> の内側 <see cref="IReadOnlyList{IEffect}"/>)
    /// を表現する中間 POCO(M4-PR3 で導入、INF-022)。
    /// </summary>
    /// <remarks>
    /// Unity Inspector は **配列の配列(2 次元配列)を直接シリアライズできない** ため、
    /// <see cref="ChoiceEffectAsset"/> の <c>Branches</c> は <see cref="EffectBranchAsset"/>[] として表現する。
    /// 各 <see cref="EffectBranchAsset"/> は内部に <c>[SerializeReference] EffectAsset[] _effects</c> を保持し、
    /// Designer は Inspector で 1 分岐ずつ effect を polymorphic に追加する。
    /// </remarks>
    [Serializable]
    public sealed class EffectBranchAsset
    {
        [SerializeReference] private EffectAsset[] _effects;

        /// <summary>本分岐の効果列(null safe、空配列許容)。</summary>
        public IReadOnlyList<EffectAsset> Effects =>
            _effects ?? Array.Empty<EffectAsset>();

        /// <summary>テスト用 ctor。</summary>
        internal EffectBranchAsset(EffectAsset[] effects)
        {
            _effects = effects;
        }
    }
}
