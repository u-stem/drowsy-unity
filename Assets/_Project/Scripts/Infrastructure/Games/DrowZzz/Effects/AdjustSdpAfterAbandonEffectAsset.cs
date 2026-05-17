using System;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AdjustSdpAfterAbandonEffect"/>(ADR-0022 / No.12「偽りの太陽」、2026-05-17 で導入)を
    /// Unity SO で表現する POCO。<see cref="Delta"/> int フィールド 1 件。
    /// </summary>
    [Serializable]
    public sealed class AdjustSdpAfterAbandonEffectAsset : EffectAsset
    {
        [SerializeField] private int _delta;

        /// <summary>SDP 増減量(正値=増、負値=減)。No.12 では Delta=+5。</summary>
        public int Delta => _delta;

        /// <summary>テスト用 ctor。</summary>
        internal AdjustSdpAfterAbandonEffectAsset(int delta)
        {
            _delta = delta;
        }

        /// <inheritdoc />
        public override IEffect ToDomain() => new AdjustSdpAfterAbandonEffect(_delta);
    }
}
