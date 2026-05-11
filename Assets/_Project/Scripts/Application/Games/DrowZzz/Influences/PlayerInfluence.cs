using System;
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
    /// <remarks>
    /// ADR-0007 §1.5「継続影響(Influence)」JIT 確定の核心型として M2-PR5 で導入。
    /// <see cref="DrowZzzGameSession.Influences"/> の各プレイヤー list に格納される(空 list 可)。
    /// 内部に <see cref="System.Collections.Generic.IReadOnlyList{T}"/> や <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>
    /// を持たないシンプル値型のため、record auto-generated Equals / GetHashCode が値同値で正しく動く
    /// (Trigger / TickEffect / RemainingCount の 3 フィールド比較、ADR-0006 §M1 進行中の学びの「内部 collection なし → auto-equals 採用」軸)。
    /// <para>
    /// null 防御の二重ガード(<see cref="TickEffect"/>): バッキングフィールド初期化式 + init setter 本体の両方で
    /// <c>value ?? throw</c>(<see cref="PlayCardAction"/> / <see cref="TimeOfDayBranchEffect"/> と同パターン、CS8907 回避)。
    /// </para>
    /// <para>
    /// <see cref="RemainingCount"/> の不変条件: ストア時は常に 1 以上。0 に到達した影響は保有者の list から
    /// 直ちに除去される(<c>DrowZzzRule.TickInfluences</c> 内)ため、本値オブジェクト自体が 0 を持つことはない。
    /// 構築時 / <c>with</c> 式の両経路で <see cref="ArgumentOutOfRangeException"/> を投げる二重ガード。
    /// </para>
    /// </remarks>
    public sealed record PlayerInfluence(InfluenceTrigger Trigger, IEffect TickEffect, int RemainingCount)
    {
        // null 防御の二重ガード(positional ctor 経由)+ RemainingCount 範囲チェック(positional ctor 経由)
        private readonly IEffect _tickEffect = TickEffect ?? throw new ArgumentNullException(nameof(TickEffect));
        private readonly int _remainingCount = RemainingCount >= 1
            ? RemainingCount
            : throw new ArgumentOutOfRangeException(
                nameof(RemainingCount),
                $"RemainingCount は 1 以上である必要があります(0 到達時は除去されるため): {RemainingCount}");

        /// <summary>Tick 時に適用される効果。null 不可。</summary>
        public IEffect TickEffect
        {
            get => _tickEffect;
            init => _tickEffect = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>残発動回数。1 以上必須。</summary>
        public int RemainingCount
        {
            get => _remainingCount;
            init => _remainingCount = value >= 1
                ? value
                : throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"RemainingCount は 1 以上である必要があります(0 到達時は除去されるため): {value}");
        }
    }
}
