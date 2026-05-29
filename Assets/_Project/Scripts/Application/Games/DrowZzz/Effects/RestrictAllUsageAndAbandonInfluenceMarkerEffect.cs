namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.TickEffect"/> として保有時、
    /// 影響保有者の以下 3 アクションを <see cref="DrowZzzRule.IsLegalMove"/> で illegal 化する marker
    /// (No.09「強引過ぎる一手」)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 禁止対象アクション(オーナー JIT 確定 2026-05-17):
    /// <list type="bullet">
    /// <item><see cref="PlayCardAction"/>(手段の使用)</item>
    /// <item><see cref="CounterAction"/>(相手フェーズでの反撃 / 自フェーズでの反撃の反撃、いずれも「使用」に含む)</item>
    /// <item><see cref="AbandonAction"/>(手段の放棄)</item>
    /// </list>
    /// </para>
    /// <para>
    /// 許可対象(進行不能化を回避):
    /// <list type="bullet">
    /// <item><see cref="AssociateAction"/>(連想、テキストは「使用や放棄」とのみ言及、連想は明示禁止されていない)</item>
    /// <item><see cref="EndTurnAction"/>(進行不能を避けるため常に合法、PhaseState 進行の必須経路)</item>
    /// <item><see cref="DrawCardAction"/>(必須経路、`WaitingForDraw` フェーズで明示的に合法)</item>
    /// <item><see cref="PassCounterAction"/>(反撃応答スキップ、進行不能化回避)</item>
    /// </list>
    /// </para>
    /// <para>
    /// `UsageRestrictionMarkerEffect`(連想後の使用制限、`IsLegalPlayCard` のみ)/
    /// `RestrictSpecificCardInfluenceEffect`(特定 CardTypeId のみ封じる、`IsLegalPlayCard` のみ)とは異なり、
    /// 本 marker は <b>3 アクション全般を CardTypeId 非依存で封じる</b>「強引な一手」のフレーバーに対応した最強の使用制限。
    /// </para>
    /// <para>
    /// <see cref="EffectInterpreter"/> 経由の評価では <b>session 不変返却(no-op)</b>
    /// (`InvertBedDamageSdpInfluenceMarkerEffect` / `DoubleBedDamageSdpInfluenceMarkerEffect` と同パターン)。
    /// 実際の illegal 化判定は <see cref="DrowZzzRule"/> 内の Influence walk(`HasUsageAndAbandonRestrictionInfluence`)で行う。
    /// </para>
    /// <para>
    /// <see cref="PlayerInfluence.RemainingCount"/> の減算は <c>DrowZzzRule.ApplyEndTurn</c> 冒頭の
    /// `DecrementInfluencesForCurrentPlayer` に統一移管されており、本 effect 自体は影響しない。
    /// カウント1 でも自フェーズ全体で機能する。
    /// </para>
    /// </remarks>
    public sealed record RestrictAllUsageAndAbandonInfluenceMarkerEffect : IEffect;
}
