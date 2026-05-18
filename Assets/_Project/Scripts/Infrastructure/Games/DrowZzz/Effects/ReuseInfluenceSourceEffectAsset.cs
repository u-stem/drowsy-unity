using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="ReuseInfluenceSourceEffect"/>(ADR-0023 / No.18「対抗手段」、2026-05-18)を Unity SO で表現する POCO。
    /// フィールドなし marker(`KeywordedEffectAsset` の <c>_inner</c> として参照される)。
    /// </summary>
    [Serializable]
    public sealed class ReuseInfluenceSourceEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal ReuseInfluenceSourceEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new ReuseInfluenceSourceEffect();
    }
}
