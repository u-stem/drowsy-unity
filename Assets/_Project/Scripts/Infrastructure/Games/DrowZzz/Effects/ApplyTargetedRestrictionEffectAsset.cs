using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="ApplyTargetedRestrictionEffect"/>(ADR-0019 PR ②、No.04「静寂を纏う」)を Unity SO で表現する POCO。
    /// </summary>
    /// <remarks>
    /// プレイ時に <c>PlayCardAction.TargetCardId</c> を読んで <see cref="Target"/> プレイヤーに動的影響を付与する効果。
    /// SO 上は <see cref="Target"/>(<see cref="SdpTarget"/> 列挙値、Inspector 編集可能)+ <see cref="RemainingCount"/>
    /// (1 以上の整数)を保持。動的部分(TargetCardTypeId)は runtime に決まるため SO には含めない。
    /// </remarks>
    [Serializable]
    public sealed class ApplyTargetedRestrictionEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _target;
        [SerializeField] private int _remainingCount;

        /// <summary>影響を付与する対象プレイヤー(<see cref="ApplyTargetedRestrictionEffect.Target"/>)。</summary>
        public SdpTarget Target => _target;

        /// <summary>付与する影響の残発動回数(<see cref="ApplyTargetedRestrictionEffect.RemainingCount"/>、1 以上必須)。</summary>
        public int RemainingCount => _remainingCount;

        /// <summary>テスト用 ctor。</summary>
        internal ApplyTargetedRestrictionEffectAsset(SdpTarget target, int remainingCount)
        {
            _target = target;
            _remainingCount = remainingCount;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentOutOfRangeException"><see cref="RemainingCount"/> が 0 以下(後段 record ctor 検証経由)</exception>
        public override IEffect ToDomain()
            => new ApplyTargetedRestrictionEffect(_target, _remainingCount);
    }
}
