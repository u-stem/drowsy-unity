namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.TickEffect"/> として保有時、
    /// 影響保有者の <see cref="DrawCardAction"/> を <see cref="DrowZzzRule.IsLegalMove"/> で illegal 化する marker
    /// (No.10「安直過ぎる一手」、2026-05-17 で導入、ADR-0021 と同 PR)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 禁止対象アクション(オーナー JIT 確定 2026-05-17):
    /// <list type="bullet">
    /// <item><see cref="DrawCardAction"/>(山札からの手段ドロー)</item>
    /// </list>
    /// </para>
    /// <para>
    /// 進行不能化(stuck)回避は ADR-0021 の `HasAnyStuckCausingInfluence` 経路で行う。
    /// 本 marker 保有時、乙の `WaitingForDraw` フェーズで `DrawCardAction` が illegal になるが、
    /// `EndTurnAction` が同フェーズで合法化されるため、乙は EndTurn でターンを送ることが可能。
    /// </para>
    /// <para>
    /// <see cref="EffectInterpreter"/> 経由の評価では <b>session 不変返却(no-op)</b>
    /// (`RestrictAllUsageAndAbandonInfluenceMarkerEffect` / 他既存 marker と同パターン)。
    /// 実際の illegal 化判定は <see cref="DrowZzzRule"/> 内の Influence walk(`HasRestrictDrawCardInfluence`)で行う。
    /// </para>
    /// <para>
    /// <see cref="PlayerInfluence.RemainingCount"/> の減算は ADR-0020 で `DrowZzzRule.ApplyEndTurn` 冒頭の
    /// `DecrementInfluencesForCurrentPlayer` に統一移管されており、本 effect 自体は影響しない。
    /// カウント1 でも自フェーズ全体で機能する(ADR-0020 後の Marker 系 count=1 セマンティクス)。
    /// </para>
    /// </remarks>
    public sealed record RestrictDrawCardInfluenceMarkerEffect : IEffect;
}
