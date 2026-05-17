using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="RemoveInvertBedDamageInfluenceEffect"/>(No.07「知恵の及ばぬ領域」、2026-05-17 で導入)を
    /// Unity SO で表現する POCO。<see cref="Target"/>(<see cref="SdpTarget"/>)を Inspector 編集可能で保持。
    /// </summary>
    [Serializable]
    public sealed class RemoveInvertBedDamageInfluenceEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _target;

        /// <summary>影響を除去する対象プレイヤー(<see cref="RemoveInvertBedDamageInfluenceEffect.Target"/>)。</summary>
        public SdpTarget Target => _target;

        /// <summary>テスト用 ctor。</summary>
        internal RemoveInvertBedDamageInfluenceEffectAsset(SdpTarget target)
        {
            _target = target;
        }

        /// <inheritdoc />
        public override IEffect ToDomain() => new RemoveInvertBedDamageInfluenceEffect(_target);
    }
}
