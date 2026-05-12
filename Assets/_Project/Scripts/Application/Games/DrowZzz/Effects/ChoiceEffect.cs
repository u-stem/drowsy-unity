using System;
using System.Collections.Generic;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 複数の効果選択肢を束ねるラッパー record。プレイ時に <see cref="PlayCardAction.Choice"/> が指す index の
    /// 効果列だけが適用される(他の選択肢は無視される)。
    /// </summary>
    /// <remarks>
    /// ADR-0007 §1.5「継続影響」追記と同梱で M2-PR5 で導入。カード No.02「緑の侵攻」は「選択1 / 選択2」の
    /// 2 択を持つ最初の選択式カードで、本 record で表現する。
    /// <para>
    /// 評価は <c>DrowZzzRule.ApplyPlayCard</c> で行い、<see cref="EffectInterpreter"/> には届かない(unwrapping は
    /// PlayCardAction 評価層の責務、<see cref="TimeOfDayBranchEffect"/> が interpreter で unwrap される対称設計に
    /// 対し、選択肢解決は action 由来文脈なので action 評価層で行う方が筋)。<see cref="EffectInterpreter.Apply"/> に
    /// 直接渡された場合は <c>NotImplementedException</c> を投げる(rule 経由のみで利用される設計の保証)。
    /// </para>
    /// <para>
    /// 内部 <see cref="IReadOnlyList{T}"/> プロパティ(<see cref="Branches"/>)を持つため、record auto-equals は
    /// 参照同値で値同値を壊す。順序保持シーケンス同値で <see cref="Equals(ChoiceEffect)"/> / <see cref="GetHashCode"/>
    /// を override(<see cref="TimeOfDayBranchEffect"/> と同パターン)。
    /// </para>
    /// </remarks>
    public sealed record ChoiceEffect : IEffect
    {
        private readonly IReadOnlyList<IReadOnlyList<IEffect>> _branches;

        /// <summary>選択肢ごとの効果列。外側 list の index が <c>PlayCardAction.Choice</c> に対応(0-based)。</summary>
        public IReadOnlyList<IReadOnlyList<IEffect>> Branches
        {
            get => _branches;
            init => _branches = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <exception cref="ArgumentNullException"><paramref name="branches"/> または各 inner list が null</exception>
        /// <exception cref="ArgumentException">外側 list の要素が 2 未満 / 内側 list に null 要素を含む</exception>
        public ChoiceEffect(IReadOnlyList<IReadOnlyList<IEffect>> branches)
        {
            Branches = branches;
            // 選択肢は 2 つ以上必須(1 つだけなら ChoiceEffect で wrap する意味がない)
            if (_branches.Count < 2)
            {
                throw new ArgumentException(
                    $"ChoiceEffect.Branches は 2 件以上必要です(現在: {_branches.Count} 件)",
                    nameof(branches));
            }
            // 内側 list の null と null 要素を構築時に検出(TimeOfDayBranchEffect と同パターン)
            for (int i = 0; i < _branches.Count; i++)
            {
                if (_branches[i] is null)
                {
                    throw new ArgumentNullException(
                        nameof(branches),
                        $"ChoiceEffect.Branches[{i}] に null は渡せません");
                }
                for (int j = 0; j < _branches[i].Count; j++)
                {
                    if (_branches[i][j] is null)
                    {
                        throw new ArgumentException(
                            $"ChoiceEffect.Branches[{i}][{j}] に null 要素を含めることはできません",
                            nameof(branches));
                    }
                }
            }
        }

        /// <summary>順序保持シーケンス同値で比較する(外側 / 内側ともに index 一致で要素同値判定)。</summary>
        public bool Equals(ChoiceEffect other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (_branches.Count != other._branches.Count)
            {
                return false;
            }
            for (int i = 0; i < _branches.Count; i++)
            {
                var a = _branches[i];
                var b = other._branches[i];
                if (a.Count != b.Count)
                {
                    return false;
                }
                for (int j = 0; j < a.Count; j++)
                {
                    if (!Equals(a[j], b[j]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _branches.Count; i++)
            {
                var inner = _branches[i];
                for (int j = 0; j < inner.Count; j++)
                {
                    // 外側 i / 内側 j を seed として組み込み、異なる位置同士の衝突を回避
                    hash = HashCode.Combine(hash, inner[j], i, j);
                }
            }
            return hash;
        }
    }
}
