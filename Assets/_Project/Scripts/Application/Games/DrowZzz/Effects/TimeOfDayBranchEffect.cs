using System;
using System.Collections.Generic;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 同一カードが夜(<see cref="DrowZzzClock.IsNight"/>)と朝(<see cref="DrowZzzClock.IsMorning"/>)で
    /// 異なる効果列を持つことを表す効果ラッパー record。
    /// </summary>
    /// <remarks>
    /// ADR-0008 §8「Clock を参照する効果の最初の登場時に JIT 確定」の確定形として M2-PR3 で導入。
    /// 「コップ一杯の脅威」(No.01)のような **夜と朝で完全に違う効果列を持つカード** を 1 ストラクチャで表現する
    /// (個別 record `WhenNightEffect` / `WhenMorningEffect` に分けない、ICardCatalog 動的返却にもしない、
    /// `docs/specs/games/drowzzz/effects/time-of-day-branch.md` §「採用理由」参照)。
    /// <para>
    /// 評価は <see cref="EffectInterpreter.Apply"/> 内で <see cref="DrowZzzGameSession.Clock"/> を見て
    /// `IsNight` なら <see cref="NightEffects"/> を、`IsMorning` なら <see cref="MorningEffects"/> を
    /// 左から順に <c>Aggregate</c> で逐次評価する。両方 `false`(`RoundNumber > 21` 過渡的範囲、ADR-0008 §5)
    /// は no-op(session 変化なし、DZ-122)。
    /// </para>
    /// <para>
    /// 内部 <see cref="IReadOnlyList{T}"/> プロパティを持つため、record auto-equals は参照同値で値同値を壊す
    /// (ADR-0002 / <see cref="DrowZzzGameSession"/> 同パターン)。Equals/GetHashCode を順序保持シーケンス同値で override。
    /// 「夜効果は [A, B, C]」と「夜効果は [A, C, B]」は順序が違うため非等値 = カードデータ設計上、効果順序は意味を持つ。
    /// </para>
    /// </remarks>
    public sealed record TimeOfDayBranchEffect : IEffect
    {
        private readonly IReadOnlyList<IEffect> _nightEffects;
        private readonly IReadOnlyList<IEffect> _morningEffects;

        /// <summary>夜(<see cref="DrowZzzClock.IsNight"/>)に評価される効果列(順序保持、左から逐次評価)。</summary>
        public IReadOnlyList<IEffect> NightEffects
        {
            get => _nightEffects;
            init => _nightEffects = value ?? throw new ArgumentNullException(nameof(NightEffects));
        }

        /// <summary>朝(<see cref="DrowZzzClock.IsMorning"/>)に評価される効果列(順序保持、左から逐次評価)。</summary>
        public IReadOnlyList<IEffect> MorningEffects
        {
            get => _morningEffects;
            init => _morningEffects = value ?? throw new ArgumentNullException(nameof(MorningEffects));
        }

        /// <exception cref="ArgumentNullException">いずれかの引数が null(DZ-123 / DZ-124)</exception>
        /// <exception cref="ArgumentException">いずれかの list に null 要素を含む(構築時に即時失敗、他 record の `ValidateAndCopyDp` パターンと整合)</exception>
        public TimeOfDayBranchEffect(
            IReadOnlyList<IEffect> nightEffects,
            IReadOnlyList<IEffect> morningEffects)
        {
            NightEffects = nightEffects;
            MorningEffects = morningEffects;
            // 各 list 内の null 要素は構築時に検出する(EffectInterpreter.Apply の null ガードに頼ると評価時まで遅延、
            // code-reviewer W-2 反映)。
            EnsureNoNullElements(NightEffects, nameof(nightEffects));
            EnsureNoNullElements(MorningEffects, nameof(morningEffects));
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
        public bool Equals(TimeOfDayBranchEffect other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return SequenceEquals(_nightEffects, other._nightEffects)
                && SequenceEquals(_morningEffects, other._morningEffects);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var e in _nightEffects)
            {
                hash = HashCode.Combine(hash, e);
            }
            foreach (var e in _morningEffects)
            {
                // 朝側との衝突を避けるため hash の片側を回転
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
