using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="RemoveInfluenceEffect"/> を Unity SO で表現する POCO(INF-027 / INF-028)。<see cref="SdpTarget"/> 1 フィールドのみ。
    /// </summary>
    [Serializable]
    public sealed class RemoveInfluenceEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _target;

        /// <summary>影響を除去する対象プレイヤー(<see cref="RemoveInfluenceEffect.Target"/>)。</summary>
        public SdpTarget Target => _target;

        /// <summary>テスト用 ctor。</summary>
        internal RemoveInfluenceEffectAsset(SdpTarget target)
        {
            _target = target;
        }

        /// <inheritdoc />
        public override IEffect ToDomain() => new RemoveInfluenceEffect(_target);
    }
}
