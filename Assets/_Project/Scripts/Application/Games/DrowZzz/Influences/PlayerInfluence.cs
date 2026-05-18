using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Application.Games.DrowZzz.Influences
{
    /// <summary>
    /// プレイヤーが保有する「継続影響」(以下、影響)の値オブジェクト。
    /// <see cref="Trigger"/> 条件で <see cref="TickEffect"/> が発動し、<see cref="RemainingCount"/> が
    /// 1 ずつ減少する。0 に到達した時点で保有プレイヤーの影響リストから除去される(発動回数ベース、JIT 確定 2026-05-11)。
    /// </summary>
    /// <param name="Trigger">影響の発動トリガー(M2-PR5 では <see cref="InfluenceTrigger.OwnPhaseStart"/> のみ)</param>
    /// <param name="TickEffect">Tick 時に <see cref="EffectInterpreter"/> 経由で適用される効果。<see cref="SdpTarget.Self"/> は影響保有者自身を指す</param>
    /// <param name="RemainingCount">残発動回数(1 以上必須、0 になったら除去)</param>
    /// <param name="OriginEffects">
    /// 本影響を生成したカードの効果列のスナップショット(ADR-0023)。No.18「対抗手段」の Reuse 経路で、
    /// プレイヤーが本影響を選択したときに再 EffectInterpreter される元データ。
    /// 空 list 許容(連鎖 Reuse 防止 / 旧 JSON 後方互換用)、null 不可。
    /// 既存カード(No.02/04/06/07/08/09/10/11/12 等)は本フィールドをカタログ側で渡さず、
    /// <see cref="EffectInterpreter"/> が <c>EffectContext.CurrentCardEffects</c> から動的に詰める(循環参照回避)。
    /// </param>
    /// <remarks>
    /// ADR-0007 §1.5「継続影響(Influence)」JIT 確定の核心型として M2-PR5 で導入。
    /// <see cref="DrowZzzGameSession.Influences"/> の各プレイヤー list に格納される(空 list 可)。
    /// <para>
    /// ADR-0023 で <see cref="OriginEffects"/> フィールドを追加(2026-05-18、No.18「対抗手段」)。
    /// 4 フィールド化により内部 <see cref="IReadOnlyList{T}"/> プロパティを持つようになるため、
    /// record auto-equals が参照同値で値同値を壊す。順序保持シーケンス同値で
    /// <see cref="Equals(PlayerInfluence)"/> / <see cref="GetHashCode"/> を override
    /// (<see cref="ChoiceEffect"/> / <see cref="KeywordedEffect"/> / <see cref="TimeOfDayBranchEffect"/> と同パターン)。
    /// </para>
    /// <para>
    /// null 防御の二重ガード(<see cref="TickEffect"/> / <see cref="OriginEffects"/>): バッキングフィールド初期化式 + init setter 本体の両方で
    /// <c>value ?? throw</c>(<see cref="PlayCardAction"/> / <see cref="TimeOfDayBranchEffect"/> と同パターン、CS8907 回避)。
    /// </para>
    /// <para>
    /// <see cref="RemainingCount"/> の不変条件: ストア時は常に 1 以上。0 に到達した影響は保有者の list から
    /// 直ちに除去される(<c>DrowZzzRule.TickInfluences</c> 内)ため、本値オブジェクト自体が 0 を持つことはない。
    /// 構築時 / <c>with</c> 式の両経路で <see cref="ArgumentOutOfRangeException"/> を投げる二重ガード。
    /// </para>
    /// </remarks>
    public sealed record PlayerInfluence
    {
        private readonly IEffect _tickEffect;
        private readonly int _remainingCount;
        private readonly IReadOnlyList<IEffect> _originEffects;

        /// <summary>影響の発動トリガー。</summary>
        public InfluenceTrigger Trigger { get; init; }

        /// <summary>Tick 時に適用される効果。null 不可。</summary>
        public IEffect TickEffect
        {
            get => _tickEffect;
            init => _tickEffect = value ?? throw new ArgumentNullException(nameof(TickEffect));
        }

        /// <summary>残発動回数。1 以上必須。</summary>
        public int RemainingCount
        {
            get => _remainingCount;
            init => _remainingCount = value >= 1
                ? value
                : throw new ArgumentOutOfRangeException(
                    nameof(RemainingCount),
                    $"RemainingCount は 1 以上である必要があります(0 到達時は除去されるため): {value}");
        }

        /// <summary>
        /// 本影響を生成したカードの効果列スナップショット(ADR-0023)。常に non-null の <see cref="IReadOnlyList{T}"/>
        /// (内部で空 list に正規化)。<c>init</c> セッターは null を <see cref="Array.Empty{T}"/> にフォールバックする
        /// (旧 v1 JSON 後方互換、Newtonsoft default 経路で 4 引数 ctor が null を渡しても安全に動作する)。
        /// </summary>
        public IReadOnlyList<IEffect> OriginEffects
        {
            get => _originEffects;
            init => _originEffects = value ?? Array.Empty<IEffect>();
        }

        // ctor 引数は PascalCase 統一(旧 record positional record の named arg 慣習を後方互換維持、
        // 既存テスト `new PlayerInfluence(Trigger: ..., TickEffect: ..., RemainingCount: ...)` を破壊しない)
        public PlayerInfluence(InfluenceTrigger Trigger, IEffect TickEffect, int RemainingCount)
            : this(Trigger, TickEffect, RemainingCount, Array.Empty<IEffect>())
        {
        }

        public PlayerInfluence(
            InfluenceTrigger Trigger,
            IEffect TickEffect,
            int RemainingCount,
            IReadOnlyList<IEffect> OriginEffects)
        {
            this.Trigger = Trigger;
            _tickEffect = TickEffect ?? throw new ArgumentNullException(nameof(TickEffect));
            _remainingCount = RemainingCount >= 1
                ? RemainingCount
                : throw new ArgumentOutOfRangeException(
                    nameof(RemainingCount),
                    $"RemainingCount は 1 以上である必要があります(0 到達時は除去されるため): {RemainingCount}");
            // 旧 v1 JSON 後方互換:null は空 list にフォールバック(ADR-0023 §8)
            _originEffects = OriginEffects ?? Array.Empty<IEffect>();
            // OriginEffects 内の null 要素を構築時に検出(ChoiceEffect / TimeOfDayBranchEffect と同パターン)
            for (int i = 0; i < _originEffects.Count; i++)
            {
                if (_originEffects[i] is null)
                {
                    throw new ArgumentException(
                        $"OriginEffects[{i}] に null は渡せません",
                        nameof(OriginEffects));
                }
            }
        }

        /// <summary>
        /// Trigger / TickEffect / RemainingCount の 3 フィールドで比較する。<see cref="OriginEffects"/> は
        /// Reuse 用の補助データで Influence の本質的アイデンティティではないため equality 対象外
        /// (ADR-0023 §「Equals は OriginEffects を対象外」、既存 PlayerInfluence 依存テスト 40+ 箇所の
        /// 破壊回避と振る舞い的同値性の両立)。
        /// </summary>
        public bool Equals(PlayerInfluence other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Trigger == other.Trigger
                && _remainingCount == other._remainingCount
                && Equals(_tickEffect, other._tickEffect);
        }

        public override int GetHashCode()
        {
            // OriginEffects は equality 対象外なので hash にも含めない(ADR-0023)
            return HashCode.Combine(Trigger, _tickEffect, _remainingCount);
        }
    }
}
