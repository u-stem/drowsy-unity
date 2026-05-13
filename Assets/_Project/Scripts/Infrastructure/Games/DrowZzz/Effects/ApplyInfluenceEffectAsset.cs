using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="ApplyInfluenceEffect"/>(M2-PR5、ADR-0007 §1.5)を Unity SO で表現する POCO
    /// (M4-PR3 で導入、ADR-0012 §3、INF-025 / INF-026)。
    /// </summary>
    /// <remarks>
    /// 「緑の侵攻」(No.02)の継続影響付与に使用された <see cref="ApplyInfluenceEffect"/> の SO 対応。
    /// 中間型 <see cref="PlayerInfluenceAsset"/> 経由で <see cref="PlayerInfluence"/> を構築する。
    /// </remarks>
    [Serializable]
    public sealed class ApplyInfluenceEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _target;
        [SerializeField] private PlayerInfluenceAsset _influence;

        /// <summary>影響を付与する対象プレイヤー(<see cref="ApplyInfluenceEffect.Target"/>)。</summary>
        public SdpTarget Target => _target;

        /// <summary>付与する影響(<see cref="ApplyInfluenceEffect.Influence"/>、SO 経路では <see cref="PlayerInfluenceAsset"/>)。</summary>
        public PlayerInfluenceAsset Influence => _influence;

        /// <summary>テスト用 ctor。</summary>
        internal ApplyInfluenceEffectAsset(SdpTarget target, PlayerInfluenceAsset influence)
        {
            _target = target;
            _influence = influence;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><see cref="Influence"/> が null / <see cref="PlayerInfluenceAsset.TickEffect"/> が null</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="PlayerInfluenceAsset.RemainingCount"/> が 0 以下</exception>
        public override IEffect ToDomain()
        {
            if (_influence is null)
            {
                throw new ArgumentNullException(nameof(Influence),
                    "ApplyInfluenceEffectAsset.Influence が null です。");
            }
            return new ApplyInfluenceEffect(_target, _influence.ToDomain());
        }
    }
}
