using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="ConditionalApplyOrClearInfluencesEffect"/>(No.16「自分勝手な審判」、2026-05-17 で導入)を
    /// Unity SO で表現する POCO。`PlayerInfluenceAsset` 中間型経由で `InfluenceToApply` を構築する。
    /// </summary>
    [Serializable]
    public sealed class ConditionalApplyOrClearInfluencesEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _target;
        [SerializeField] private int _threshold;
        [SerializeField] private PlayerInfluenceAsset _influenceToApply;

        /// <summary>対象プレイヤー(<see cref="ConditionalApplyOrClearInfluencesEffect.Target"/>)。</summary>
        public SdpTarget Target => _target;

        /// <summary>境界値(<see cref="ConditionalApplyOrClearInfluencesEffect.Threshold"/>、Count <= Threshold で Apply、> Threshold で Clear)。</summary>
        public int Threshold => _threshold;

        /// <summary>Apply 経路で付与する PlayerInfluence(SO 経路では <see cref="PlayerInfluenceAsset"/>)。</summary>
        public PlayerInfluenceAsset InfluenceToApply => _influenceToApply;

        /// <summary>テスト用 ctor。</summary>
        internal ConditionalApplyOrClearInfluencesEffectAsset(SdpTarget target, int threshold, PlayerInfluenceAsset influenceToApply)
        {
            _target = target;
            _threshold = threshold;
            _influenceToApply = influenceToApply;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><see cref="InfluenceToApply"/> が null / <see cref="PlayerInfluenceAsset.TickEffect"/> が null</exception>
        public override IEffect ToDomain()
        {
            if (_influenceToApply is null)
            {
                throw new ArgumentNullException(nameof(InfluenceToApply),
                    "ConditionalApplyOrClearInfluencesEffectAsset.InfluenceToApply が null です。");
            }
            return new ConditionalApplyOrClearInfluencesEffect(_target, _threshold, _influenceToApply.ToDomain());
        }
    }
}
