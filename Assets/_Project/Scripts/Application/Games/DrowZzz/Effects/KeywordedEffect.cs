using System;
using System.Collections.Generic;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 1 件以上の <see cref="Keyword"/> を <see cref="Inner"/> 効果に付与するラッパー record(ADR-0011 §4)。
    /// 効果単位でキーワード(狂乱 / 本能 / 反撃)を付与する設計を表現する。
    /// </summary>
    /// <remarks>
    /// ADR-0011 §4「配置の判断(JIT 確定 2026-05-12)」で採用案 (b) として確定。`TimeOfDayBranchEffect` /
    /// `ChoiceEffect` と同パターンで、既存 effect を wrap する設計。「効果単位で 0 個以上のキーワードを付与」は
    /// 「キーワードを持つ効果は本 record で wrap する、持たない効果は wrap しない」と解釈し、
    /// 本 record では <see cref="Keywords"/> が **1 件以上必須**(空 list で wrap する意味がないため、
    /// <see cref="ChoiceEffect.Branches"/> の「2 件以上」と同じ「無意味な wrap を防ぐ」防御)。
    /// <para>
    /// 評価は <see cref="EffectInterpreter.Apply"/> で行い、<see cref="Inner"/> を逐次評価する。<see cref="Keywords"/>
    /// 自体は **judge 用属性** で、評価時に副作用を持たない:
    /// <list type="bullet">
    /// <item><see cref="Keyword.Instinct"/>:<c>DrowZzzRule.IsLegalAbandon</c> で「手札を捨てる対象」判定に利用
    /// (M3-PR5a)</item>
    /// <item><see cref="Keyword.Frenzy"/>:M3-PR5b 以降で <c>CounterAction</c> の対象判定に利用
    /// (反撃を受けない)</item>
    /// <item><see cref="Keyword.Counter"/>:M3-PR5b 以降で「相手ターン中に反撃プレイ可能」判定に利用</item>
    /// </list>
    /// </para>
    /// <para>
    /// 内部 <see cref="IReadOnlyList{T}"/> プロパティ(<see cref="Keywords"/>)を持つため、record auto-equals は
    /// 参照同値で値同値を壊す。順序保持シーケンス同値で <see cref="Equals(KeywordedEffect)"/> /
    /// <see cref="GetHashCode"/> を override(`ChoiceEffect` / `TimeOfDayBranchEffect` と同パターン)。
    /// </para>
    /// </remarks>
    public sealed record KeywordedEffect : IEffect
    {
        private readonly IReadOnlyList<Keyword> _keywords;
        private readonly IEffect _inner;

        /// <summary>
        /// 付与するキーワード(1 件以上)。順序は保持されるが、`[Frenzy, Instinct]` と `[Instinct, Frenzy]` は
        /// 順序が違うため非等値(マルチセットとして扱いたい場合は呼び出し側で正規化する想定)。
        /// </summary>
        /// <remarks>
        /// 値型 (<see cref="Keyword"/> は enum) のため list 要素の null チェックは不要
        /// (<see cref="TimeOfDayBranchEffect.NightEffects"/> や <see cref="ChoiceEffect.Branches"/> のような
        /// 参照型要素 list で行う <c>EnsureNoNullElements</c> 検証は本クラスでは省略)。
        /// </remarks>
        public IReadOnlyList<Keyword> Keywords
        {
            get => _keywords;
            init => _keywords = value ?? throw new ArgumentNullException(nameof(Keywords));
        }

        /// <summary>キーワードを付与する対象の効果(評価時に逐次 Apply される)。</summary>
        public IEffect Inner
        {
            get => _inner;
            init => _inner = value ?? throw new ArgumentNullException(nameof(Inner));
        }

        /// <exception cref="ArgumentNullException"><paramref name="keywords"/> または <paramref name="inner"/> が null</exception>
        /// <exception cref="ArgumentException"><paramref name="keywords"/> が空 list(1 件以上必須、空での wrap は無意味)</exception>
        public KeywordedEffect(IReadOnlyList<Keyword> keywords, IEffect inner)
        {
            Keywords = keywords;
            Inner = inner;
            // Keyword 空 list は wrap する意味がない(ADR-0011 §4 解釈、ChoiceEffect.Branches.Count >= 2 と同パターン)
            if (_keywords.Count == 0)
            {
                throw new ArgumentException(
                    "KeywordedEffect.Keywords は 1 件以上必要です(空 list での wrap は無意味、ADR-0011 §4)",
                    nameof(keywords));
            }
        }

        /// <summary>判定:<paramref name="keyword"/> が本 effect の <see cref="Keywords"/> に含まれているか。</summary>
        public bool HasKeyword(Keyword keyword)
        {
            for (int i = 0; i < _keywords.Count; i++)
            {
                if (_keywords[i] == keyword)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>順序保持シーケンス同値で比較する(<see cref="Keywords"/> 順序 + <see cref="Inner"/> 値同値)。</summary>
        public bool Equals(KeywordedEffect other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (_keywords.Count != other._keywords.Count)
            {
                return false;
            }
            for (int i = 0; i < _keywords.Count; i++)
            {
                if (_keywords[i] != other._keywords[i])
                {
                    return false;
                }
            }
            return Equals(_inner, other._inner);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < _keywords.Count; i++)
            {
                hash = HashCode.Combine(hash, _keywords[i], i);
            }
            return HashCode.Combine(hash, _inner);
        }
    }
}
