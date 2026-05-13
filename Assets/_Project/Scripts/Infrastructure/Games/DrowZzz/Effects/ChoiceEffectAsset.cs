using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="ChoiceEffect"/>(M2-PR5、ADR-0007 §1.5)を Unity SO で表現する POCO
    /// (M4-PR3 で導入、INF-041 / INF-042)。wrapper effect、2 次元配列を中間型 <see cref="EffectBranchAsset"/> で表現。
    /// </summary>
    /// <remarks>
    /// 「緑の侵攻」(No.02)の選択肢式効果に使用。<see cref="Branches"/> は <see cref="EffectBranchAsset"/>[] として
    /// 保持(Unity が 2 次元配列を直接シリアライズできないための回避策、INF-022)。<see cref="ToDomain"/> で各
    /// <see cref="EffectBranchAsset.Effects"/> の各要素を再帰的に <see cref="EffectAsset.ToDomain"/> 変換して
    /// <see cref="ChoiceEffect"/> ctor に渡す。
    /// </remarks>
    [Serializable]
    public sealed class ChoiceEffectAsset : EffectAsset
    {
        // 属性選択の根拠(M4-PR3 code-reviewer W-2 反映 2026-05-13):
        // - `EffectBranchAsset` は sealed class で polymorphic serialize 不要のため [SerializeField] 単一型 ok。
        // - 一方 `EffectBranchAsset._effects` は abstract `EffectAsset[]` で polymorphic 必須のため [SerializeReference]。
        // - 中間型 `EffectBranchAsset` を sealed のまま保つ限り本属性は維持、サブクラス化が必要になった場合は
        //   `[SerializeReference] EffectBranchAsset[]` に切り替える(将来の M4-PR 設計判断ポイント)。
        [SerializeField] private EffectBranchAsset[] _branches;

        /// <summary>選択肢ごとの効果列(<see cref="ChoiceEffect.Branches"/>、外側 index が <c>PlayCardAction.Choice</c>)。</summary>
        public IReadOnlyList<EffectBranchAsset> Branches =>
            _branches ?? Array.Empty<EffectBranchAsset>();

        /// <summary>テスト用 ctor。</summary>
        internal ChoiceEffectAsset(EffectBranchAsset[] branches)
        {
            _branches = branches;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">いずれかの分岐 / 内側要素が null</exception>
        /// <exception cref="ArgumentException">外側 list が 2 件未満(<see cref="ChoiceEffect"/> ctor 側で検証)</exception>
        public override IEffect ToDomain()
        {
            var src = _branches ?? Array.Empty<EffectBranchAsset>();
            var outer = new IReadOnlyList<IEffect>[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] is null)
                {
                    throw new ArgumentNullException(
                        $"ChoiceEffectAsset.Branches[{i}]",
                        $"ChoiceEffectAsset.Branches[{i}] が null です。");
                }
                var inner = src[i].Effects;
                var dst = new IEffect[inner.Count];
                for (int j = 0; j < inner.Count; j++)
                {
                    if (inner[j] is null)
                    {
                        throw new ArgumentNullException(
                            $"ChoiceEffectAsset.Branches[{i}].Effects[{j}]",
                            $"ChoiceEffectAsset.Branches[{i}].Effects[{j}] が null です(SerializeReference の missing reference 等)。");
                    }
                    dst[j] = inner[j].ToDomain();
                }
                outer[i] = dst;
            }
            return new ChoiceEffect(outer);
        }
    }
}
