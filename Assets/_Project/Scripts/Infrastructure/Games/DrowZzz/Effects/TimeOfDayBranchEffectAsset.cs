using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="TimeOfDayBranchEffect"/>(M2-PR3、ADR-0008)を Unity SO で表現する POCO
    /// (M4-PR3 で導入、INF-039 / INF-040)。wrapper effect。
    /// </summary>
    /// <remarks>
    /// 「コップ一杯の脅威」(No.01)の夜・朝分岐に使用。<see cref="NightEffects"/> / <see cref="MorningEffects"/> を
    /// <c>[SerializeReference] EffectAsset[]</c> で再帰的に保持し、<see cref="ToDomain"/> で各要素の
    /// <see cref="EffectAsset.ToDomain"/> を呼んで <see cref="IEffect"/>[] に変換する(JIT 確定 2026-05-13)。
    /// null 要素 / ToDomain 失敗は <see cref="ArgumentException"/> として伝播し、上位
    /// <see cref="ScriptableObjectCardCatalog.BuildEffectsFromAssets"/> で graceful skip(INF-018 / INF-019)。
    /// </remarks>
    [Serializable]
    public sealed class TimeOfDayBranchEffectAsset : EffectAsset
    {
        [SerializeReference] private EffectAsset[] _nightEffects;
        [SerializeReference] private EffectAsset[] _morningEffects;

        /// <summary>夜に評価される効果列(<see cref="TimeOfDayBranchEffect.NightEffects"/>)。</summary>
        public IReadOnlyList<EffectAsset> NightEffects =>
            _nightEffects ?? Array.Empty<EffectAsset>();

        /// <summary>朝に評価される効果列(<see cref="TimeOfDayBranchEffect.MorningEffects"/>)。</summary>
        public IReadOnlyList<EffectAsset> MorningEffects =>
            _morningEffects ?? Array.Empty<EffectAsset>();

        /// <summary>テスト用 ctor。</summary>
        internal TimeOfDayBranchEffectAsset(EffectAsset[] nightEffects, EffectAsset[] morningEffects)
        {
            _nightEffects = nightEffects;
            _morningEffects = morningEffects;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">いずれかの要素が null / <see cref="TimeOfDayBranchEffect"/> ctor の防御に違反</exception>
        public override IEffect ToDomain()
        {
            var night = ConvertList(_nightEffects, nameof(NightEffects));
            var morning = ConvertList(_morningEffects, nameof(MorningEffects));
            return new TimeOfDayBranchEffect(night, morning);
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
                        $"TimeOfDayBranchEffectAsset.{fieldName}[{i}]",
                        $"TimeOfDayBranchEffectAsset.{fieldName}[{i}] が null です(SerializeReference の missing reference 等)。");
                }
                dst[i] = src[i].ToDomain();
            }
            return dst;
        }
    }
}
