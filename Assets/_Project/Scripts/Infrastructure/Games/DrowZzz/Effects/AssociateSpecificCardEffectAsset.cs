using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AssociateSpecificCardEffect"/>(No.13/14/15「最後の砦Ⅰ/Ⅱ/Ⅲ」、2026-05-17 で導入)を
    /// Unity SO で表現する POCO。<see cref="TargetCardTypeIdValue"/> string フィールド 1 件
    /// (`CardTypeId` には default ctor がなく `CardTypeId.Of(string)` 経由で構築する制約のため、
    /// SO 層では string で保持し ToDomain で変換)。
    /// </summary>
    [Serializable]
    public sealed class AssociateSpecificCardEffectAsset : EffectAsset
    {
        [SerializeField] private string _targetCardTypeIdValue;

        /// <summary>連想する対象のカード型 ID 文字列(`CardTypeId.Value` 相当、null/空不可)。</summary>
        public string TargetCardTypeIdValue => _targetCardTypeIdValue;

        /// <summary>テスト用 ctor。</summary>
        internal AssociateSpecificCardEffectAsset(string targetCardTypeIdValue)
        {
            _targetCardTypeIdValue = targetCardTypeIdValue;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">`TargetCardTypeIdValue` が null / 空 / 空白 / `#` 含む(`CardTypeId.Of` の検証経路)</exception>
        public override IEffect ToDomain() => new AssociateSpecificCardEffect(CardTypeId.Of(_targetCardTypeIdValue));
    }
}
