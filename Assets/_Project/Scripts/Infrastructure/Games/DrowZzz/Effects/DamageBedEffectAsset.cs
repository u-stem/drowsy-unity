using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="DamageBedEffect"/> を Unity SO で表現する POCO(INF-031 / INF-032)。
    /// <see cref="SdpTarget"/> + <see cref="int"/> の 2 フィールド。
    /// </summary>
    /// <remarks>
    /// <see cref="DamageBedEffect"/> record は <c>Percent</c> の 5 の倍数 / 正値検証を ctor / init で行う。
    /// 本 Asset の <see cref="ToDomain"/> 経路で違反値が渡された場合は <see cref="ArgumentException"/> が
    /// 伝播し、上位 <see cref="ScriptableObjectCardCatalog.BuildEffectsFromAssets"/> で catch + skip + LogError(INF-019)。
    /// </remarks>
    [Serializable]
    public sealed class DamageBedEffectAsset : EffectAsset
    {
        [SerializeField] private SdpTarget _target;
        [SerializeField] private int _percent;

        /// <summary>破損を与える対象プレイヤー(<see cref="DamageBedEffect.Target"/>)。</summary>
        public SdpTarget Target => _target;

        /// <summary>破損率の増加幅(<see cref="DamageBedEffect.Percent"/>、5 の倍数 / 正値が record 側で要求される)。</summary>
        public int Percent => _percent;

        /// <summary>テスト用 ctor。</summary>
        internal DamageBedEffectAsset(SdpTarget target, int percent)
        {
            _target = target;
            _percent = percent;
        }

        /// <inheritdoc />
        public override IEffect ToDomain() => new DamageBedEffect(_target, _percent);
    }
}
