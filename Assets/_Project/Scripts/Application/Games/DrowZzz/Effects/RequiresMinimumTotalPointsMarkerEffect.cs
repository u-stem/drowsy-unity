using System;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 使用条件マーカー効果(ADR-0011 §6 / M3-PR6 で新設)。本効果を効果列に持つカードは
    /// <see cref="DrowZzzRule.IsLegalMove"/>(<see cref="PlayCardAction"/> 経路)で
    /// 現プレイヤーの <see cref="DrowZzzGameSession.TotalPoints"/> が <see cref="Threshold"/> 以上でないと
    /// illegal-move 化される(<see cref="AssociatableMarkerEffect"/> と同じマーカー方式、JIT 確定 2026-05-14)。
    /// </summary>
    /// <param name="Threshold">使用に必要な最小 TotalPoints。1 以上必須(0 / 負値は使用条件として無意味、
    /// 構築時 / <c>with</c> 式の両経路で <see cref="ArgumentOutOfRangeException"/> を投げる二重ガード)。</param>
    /// <remarks>
    /// ADR-0011 §6「夢」カードの使用条件「FDS ≥ 100」を表現するための marker。
    /// 「夢」のカードデータでは <c>Threshold = DrowZzzVictoryConstants.EarlyWinScoreThreshold</c>(= 100)を採用し、
    /// 早期勝利の閾値(夜 + 持ち点 100 以上)と意図的に一致させる設計(JIT 確定 2026-05-14)。
    /// <para>
    /// 検出は <see cref="DrowZzzRule.IsLegalPlayCard"/> で対象カードの効果列を **最上位 scan** する
    /// (`TimeOfDayBranchEffect` / `KeywordedEffect` / `ChoiceEffect` の inner は walk しない、本 PR JIT 確定 2026-05-14:
    /// 夜・朝で使用条件が変わらない単一閾値の表現としては最上位配置で十分。将来 nested 配置が必要な PR で walk 化)。
    /// </para>
    /// <para>
    /// <see cref="EffectInterpreter.Apply"/> で評価された場合は **no-op**(session 不変返却)。
    /// マーカー的役割を保ち、効果としての副作用を持たない(<see cref="AssociatableMarkerEffect"/> と同パターン)。
    /// </para>
    /// </remarks>
    public sealed record RequiresMinimumTotalPointsMarkerEffect(int Threshold) : IEffect
    {
        // Threshold 範囲チェックの二重ガード(positional ctor 経由)
        // CS8907 回避のためバッキングフィールド初期化式で Threshold パラメータを参照(PlayCardAction.Card と同パターン)
        private readonly int _threshold = Threshold >= 1
            ? Threshold
            : throw new ArgumentOutOfRangeException(
                nameof(Threshold),
                $"Threshold は 1 以上である必要があります(0 / 負値は使用条件として無意味): {Threshold}");

        /// <summary>使用に必要な最小 TotalPoints(1 以上必須)。</summary>
        public int Threshold
        {
            get => _threshold;
            init => _threshold = value >= 1
                ? value
                : throw new ArgumentOutOfRangeException(
                    nameof(Threshold),
                    $"Threshold は 1 以上である必要があります(0 / 負値は使用条件として無意味): {value}");
        }
    }
}
