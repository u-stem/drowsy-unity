using System;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AssociateToFirstPlayerOnGameStartEffect"/>(ADR-0024 / No.19「絶対障壁」、2026-05-18)を Unity SO で表現する POCO。
    /// フィールドなし marker(カード自身の effects 列に最上位 effect として配置される)。
    /// </summary>
    [Serializable]
    public sealed class AssociateToFirstPlayerOnGameStartEffectAsset : EffectAsset
    {
        /// <summary>テスト用 ctor。</summary>
        internal AssociateToFirstPlayerOnGameStartEffectAsset() { }

        /// <inheritdoc />
        public override IEffect ToDomain() => new AssociateToFirstPlayerOnGameStartEffect();
    }
}
