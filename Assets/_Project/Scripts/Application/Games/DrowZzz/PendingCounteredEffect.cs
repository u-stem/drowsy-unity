using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// 「無効化された効果」遡及発動のための保留情報(M3-PR5c、ADR-0011 §4.4)。
    /// 反撃カード B が元カード A をカウンタした際、A の効果列を本 record として保留する。
    /// 後続の「反撃の反撃」C が B を打ち消した時点で A の効果列(<see cref="OriginalEffects"/>)を
    /// <see cref="Effects.EffectInterpreter.Apply"/> で遡及評価する設計(JIT 確定 2026-05-12)。
    /// </summary>
    /// <param name="CounterCard">A をカウンタした反撃カード(= B)。C の <c>Target</c> がこの値に一致する経路 2 で合法。</param>
    /// <param name="OriginalCard">B によって無効化された元カード(= A)。遡及発動時の logging / Presentation 表示用途で保持。</param>
    /// <param name="OriginalEffects">A の効果列(catalog から取得した参照を防御コピー)。C 成立で <see cref="Effects.EffectInterpreter"/> によって評価される。</param>
    /// <remarks>
    /// <para>
    /// ADR §312 で示された候補例 <c>(CardId Card, IReadOnlyList&lt;IEffect&gt; Effects)</c> は 2 フィールド構成だったが、
    /// 本実装では C の <c>Target</c> 照合に「無効化した B カード」が必要なため <see cref="CounterCard"/> を追加し
    /// 3 フィールド record に拡張する(ADR-0011 §4.4 の意図を満たしつつ「B 識別」の責務を構造に持たせる設計、
    /// JIT 確定 2026-05-12 / M3-PR5c 着手時に 3 フィールド構成を採用)。
    /// </para>
    /// <para>
    /// null 防御の二重ガード(<see cref="PlayCardAction.Card"/> と同パターン):
    /// バッキングフィールドの初期化式で positional ctor 経由の null を弾き、init setter 本体で
    /// <c>with</c> 式経由の null を弾く。<see cref="OriginalEffects"/> は内部で防御コピー +
    /// null 要素検出を行う(<see cref="Influences.PlayerInfluence"/> と同パターン)。
    /// </para>
    /// <para>
    /// <see cref="Equals(PendingCounteredEffect)"/> / <see cref="GetHashCode"/> は record の
    /// auto-generated を上書き(<see cref="OriginalEffects"/> が <see cref="IReadOnlyList{T}"/> のため
    /// auto-equals が参照同値にフォールバックして値同値が壊れる)。
    /// 順序保持シーケンス同値で比較する(A の効果列の評価順を保つため、リストの順序は意味を持つ)。
    /// </para>
    /// </remarks>
    public sealed record PendingCounteredEffect(
        CardId CounterCard,
        CardId OriginalCard,
        IReadOnlyList<IEffect> OriginalEffects)
    {
        // null 防御の二重ガード(positional ctor 経由 + with 経由)
        private readonly CardId _counterCard = CounterCard ?? throw new ArgumentNullException(nameof(CounterCard));
        private readonly CardId _originalCard = OriginalCard ?? throw new ArgumentNullException(nameof(OriginalCard));
        // OriginalEffects は防御コピー + null 要素検出を伴うため、init setter 経路で正規化する
        // (positional ctor の初期化式は ValidateAndCopyEffects を呼んで _originalEffects に格納)
        private readonly IReadOnlyList<IEffect> _originalEffects = ValidateAndCopyEffects(OriginalEffects, nameof(OriginalEffects));

        /// <summary>A をカウンタした反撃カード(= B)。</summary>
        public CardId CounterCard
        {
            get => _counterCard;
            init => _counterCard = value ?? throw new ArgumentNullException(nameof(CounterCard));
        }

        /// <summary>B によって無効化された元カード(= A)。</summary>
        public CardId OriginalCard
        {
            get => _originalCard;
            init => _originalCard = value ?? throw new ArgumentNullException(nameof(OriginalCard));
        }

        /// <summary>A の効果列(防御コピー済、順序保持)。</summary>
        public IReadOnlyList<IEffect> OriginalEffects
        {
            get => _originalEffects;
            init => _originalEffects = ValidateAndCopyEffects(value, nameof(OriginalEffects));
        }

        // 効果列の防御コピー + null 検証 + null 要素検出
        // 空 list は許容(理論上「効果なしのカードを反撃」は意味薄だが、本 record の責務外で許容)。
        // paramName: 呼び出し元(init setter または ctor 初期化式)の Property 名(post-Phase2 #4 Phase A+B 第 2 弾)
        private static IReadOnlyList<IEffect> ValidateAndCopyEffects(IReadOnlyList<IEffect> source, string paramName)
        {
            if (source is null)
            {
                throw new ArgumentNullException(paramName, "OriginalEffects に null は渡せません(空 list は許容)");
            }
            var arr = new IEffect[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] is null)
                {
                    throw new ArgumentException(
                        $"OriginalEffects[{i}] に null 要素を含めることはできません",
                        paramName);
                }
                arr[i] = source[i];
            }
            return arr;
        }

        /// <summary>
        /// 順序保持シーケンス同値で比較する。<see cref="CounterCard"/> / <see cref="OriginalCard"/> は
        /// <see cref="CardId.Equals"/>、<see cref="OriginalEffects"/> は要素順を含む全要素一致で判定する。
        /// </summary>
        public bool Equals(PendingCounteredEffect other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (!_counterCard.Equals(other._counterCard))
            {
                return false;
            }
            if (!_originalCard.Equals(other._originalCard))
            {
                return false;
            }
            if (_originalEffects.Count != other._originalEffects.Count)
            {
                return false;
            }
            for (int i = 0; i < _originalEffects.Count; i++)
            {
                if (!Equals(_originalEffects[i], other._originalEffects[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            // CardId 2 件 + 効果列の Count + 各要素の HashCode を順序保存型で結合
            int hash = HashCode.Combine(_counterCard, _originalCard, _originalEffects.Count);
            for (int i = 0; i < _originalEffects.Count; i++)
            {
                hash = HashCode.Combine(hash, _originalEffects[i]);
            }
            return hash;
        }
    }
}
