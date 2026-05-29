using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="PlayOrAbandonBranchEffect"/>(No.20「至上の喜び」)を Unity SO で表現する POCO。
    /// wrapper effect(`TimeOfDayBranchEffectAsset` と同パターン)。
    /// </summary>
    /// <remarks>
    /// <see cref="PlayEffects"/> / <see cref="AbandonEffects"/> を <c>[SerializeReference] EffectAsset[]</c> で
    /// 再帰的に保持し、<see cref="ToDomain"/> で各要素の <see cref="EffectAsset.ToDomain"/> を呼んで
    /// <see cref="IEffect"/>[] に変換する。null 要素 / ToDomain 失敗は <see cref="ArgumentException"/> として伝播し、
    /// 上位 <see cref="ScriptableObjectCardCatalog.BuildEffectsFromAssets"/> で graceful skip。
    /// </remarks>
    [Serializable]
    public sealed class PlayOrAbandonBranchEffectAsset : EffectAsset
    {
        [SerializeReference] private EffectAsset[] _playEffects;
        [SerializeReference] private EffectAsset[] _abandonEffects;

        /// <summary>プレイ時に評価される効果列(<see cref="PlayOrAbandonBranchEffect.PlayEffects"/>)。</summary>
        public IReadOnlyList<EffectAsset> PlayEffects =>
            _playEffects ?? Array.Empty<EffectAsset>();

        /// <summary>放棄時に評価される効果列(<see cref="PlayOrAbandonBranchEffect.AbandonEffects"/>)。</summary>
        public IReadOnlyList<EffectAsset> AbandonEffects =>
            _abandonEffects ?? Array.Empty<EffectAsset>();

        /// <summary>テスト用 ctor。</summary>
        internal PlayOrAbandonBranchEffectAsset(EffectAsset[] playEffects, EffectAsset[] abandonEffects)
        {
            _playEffects = playEffects;
            _abandonEffects = abandonEffects;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">いずれかの要素が null</exception>
        /// <exception cref="ArgumentException">いずれかの list が空(<see cref="PlayOrAbandonBranchEffect"/> ctor の防御)</exception>
        public override IEffect ToDomain()
        {
            var play = ConvertList(_playEffects, nameof(PlayEffects));
            var abandon = ConvertList(_abandonEffects, nameof(AbandonEffects));
            return new PlayOrAbandonBranchEffect(play, abandon);
        }

        // EffectAsset[] → IEffect[] への再帰変換。null 要素は ArgumentNullException で伝播(上位 catch + skip)。
        private static IEffect[] ConvertList(EffectAsset[] source, string fieldName)
        {
            var src = source ?? Array.Empty<EffectAsset>();
            var dst = new IEffect[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] is null)
                {
                    throw new ArgumentNullException(
                        $"PlayOrAbandonBranchEffectAsset.{fieldName}[{i}]",
                        $"PlayOrAbandonBranchEffectAsset.{fieldName}[{i}] が null です(SerializeReference の missing reference 等)。");
                }
                dst[i] = src[i].ToDomain();
            }
            return dst;
        }
    }
}
