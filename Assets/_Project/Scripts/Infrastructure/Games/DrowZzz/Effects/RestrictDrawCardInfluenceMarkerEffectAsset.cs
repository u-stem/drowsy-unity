using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="RestrictDrawCardInfluenceMarkerEffect"/>(No.10「安直過ぎる一手」、2026-05-17 で導入、
    /// ADR-0021 と同 PR)を Unity SO で表現する POCO。フィールドなし marker。
    /// </summary>
    [Serializable]
    public sealed class RestrictDrawCardInfluenceMarkerEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal RestrictDrawCardInfluenceMarkerEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new RestrictDrawCardInfluenceMarkerEffect();
    }
}
