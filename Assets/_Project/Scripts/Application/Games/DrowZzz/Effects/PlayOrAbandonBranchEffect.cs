using System;
using System.Collections.Generic;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// プレイ時(<c>PlayCardAction</c>)と放棄時(<c>AbandonAction</c>)で発動する効果列を分岐表現する wrapper record
    /// (ADR-0025、No.20「至上の喜び」で初導入)。
    /// </summary>
    /// <remarks>
    /// 構造は <see cref="TimeOfDayBranchEffect"/> と対称(2 list を carrier、評価は rule 評価層で unwrap、
    /// <see cref="EffectInterpreter.Apply"/> には届かない設計)。
    /// <para>
    /// 評価経路:
    /// <list type="bullet">
    /// <item><c>DrowZzzRule.ApplyPlayCard</c>:effects walk で本 effect を検出したら <see cref="PlayEffects"/> のみ
    /// <see cref="EffectInterpreter.Apply"/> 連鎖</item>
    /// <item><c>DrowZzzRule.ApplyAbandon</c>:既存 <c>AbandonChoice</c> 適用(GainSdp = SDP+5 / RepairBed = Bed-20%)の
    /// **後** に、放棄したカードの最上位 effects から本 effect を検出したら <see cref="AbandonEffects"/> のみを
    /// <see cref="EffectInterpreter.Apply"/> 連鎖(AbandonChoice を上書きせず、累積モデル、ADR-0025 §「AbandonChoice との関係」)</item>
    /// </list>
    /// </para>
    /// <para>
    /// 両 list とも順序保持 + 1 件以上必須(空 list は wrap する意味がないため、`KeywordedEffect.Keywords` と同じ防御方針)。
    /// </para>
    /// <para>
    /// 内部 <see cref="IReadOnlyList{T}"/> プロパティを持つため、record auto-equals は参照同値で値同値を壊す。
    /// 順序保持シーケンス同値で <see cref="Equals(PlayOrAbandonBranchEffect)"/> / <see cref="GetHashCode"/> を override
    /// (<see cref="TimeOfDayBranchEffect"/> と同パターン)。
    /// </para>
    /// </remarks>
    public sealed record PlayOrAbandonBranchEffect : IEffect
    {
        private readonly IReadOnlyList<IEffect> _playEffects;
        private readonly IReadOnlyList<IEffect> _abandonEffects;

        /// <summary>プレイ時(<c>PlayCardAction</c>)に評価される効果列(順序保持、左から逐次評価)。</summary>
        public IReadOnlyList<IEffect> PlayEffects
        {
            get => _playEffects;
            init => _playEffects = value ?? throw new ArgumentNullException(nameof(PlayEffects));
        }

        /// <summary>放棄時(<c>AbandonAction</c>)に <c>AbandonChoice</c> 適用後に追加発動される効果列(順序保持、左から逐次評価)。</summary>
        public IReadOnlyList<IEffect> AbandonEffects
        {
            get => _abandonEffects;
            init => _abandonEffects = value ?? throw new ArgumentNullException(nameof(AbandonEffects));
        }

        /// <exception cref="ArgumentNullException">いずれかの引数が null</exception>
        /// <exception cref="ArgumentException">いずれかの list が空 / null 要素を含む(構築時に即時失敗)</exception>
        public PlayOrAbandonBranchEffect(
            IReadOnlyList<IEffect> playEffects,
            IReadOnlyList<IEffect> abandonEffects)
        {
            PlayEffects = playEffects;
            AbandonEffects = abandonEffects;
            EnsureNonEmpty(PlayEffects, nameof(playEffects));
            EnsureNonEmpty(AbandonEffects, nameof(abandonEffects));
            EnsureNoNullElements(PlayEffects, nameof(playEffects));
            EnsureNoNullElements(AbandonEffects, nameof(abandonEffects));
        }

        private static void EnsureNonEmpty(IReadOnlyList<IEffect> list, string paramName)
        {
            if (list.Count == 0)
            {
                throw new ArgumentException(
                    $"{paramName} は 1 件以上必要です(空 list での wrap は無意味、ADR-0025)",
                    paramName);
            }
        }

        private static void EnsureNoNullElements(IReadOnlyList<IEffect> list, string paramName)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is null)
                {
                    throw new ArgumentException(
                        $"{paramName} に null 要素を含めることはできません (index: {i})",
                        paramName);
                }
            }
        }

        /// <summary>順序保持シーケンス同値で比較する。</summary>
        public bool Equals(PlayOrAbandonBranchEffect other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return SequenceEquals(_playEffects, other._playEffects)
                && SequenceEquals(_abandonEffects, other._abandonEffects);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var e in _playEffects)
            {
                hash = HashCode.Combine(hash, e);
            }
            foreach (var e in _abandonEffects)
            {
                // 放棄側との衝突を避けるため hash の片側を回転(`TimeOfDayBranchEffect` と同パターン)
                hash = HashCode.Combine(hash, e, 1);
            }
            return hash;
        }

        private static bool SequenceEquals(IReadOnlyList<IEffect> a, IReadOnlyList<IEffect> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            for (int i = 0; i < a.Count; i++)
            {
                if (!Equals(a[i], b[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
