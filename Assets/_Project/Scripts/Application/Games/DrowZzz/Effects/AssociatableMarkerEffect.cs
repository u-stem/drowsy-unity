namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 連想可能マーカー効果。フィールドなしのマーカー的 record。本効果を効果列に持つカードを
    /// **「連想可能カード」** と定義する(ADR-0011 §1 / ADR-0007 §1「カード効果は <c>IEffect</c> で表現」と整合)。
    /// </summary>
    /// <remarks>
    /// ADR-0011 §1 で M3-PR4 として新設(JIT 確定 2026-05-13:判別方式 (i) マーカー effect 方式を採用)。
    /// <see cref="DrowZzzRule.IsLegalMove"/> で <see cref="AssociateAction"/> の合法性を判定する際、
    /// <c>action.Card</c> の効果列に本 record が含まれているかで「連想可能性」を判別する
    /// (<see cref="EarlyWinTriggerEffect"/> による「就寝カード」識別と同パターン、ADR-0010 §5)。
    /// <para>
    /// <see cref="EffectInterpreter.Apply"/> で評価された場合は **no-op**(session 不変返却)。
    /// マーカー的役割を保ち、効果としての副作用を持たない(<see cref="EarlyWinTriggerEffect"/> も条件不成立時は no-op、
    /// 本 effect は常時 no-op)。連想は通常のカードプレイ(<see cref="DrowZzzRule.ApplyPlayCard"/> 経由)で
    /// 起こされる効果ではなく、<see cref="AssociateAction"/> という独立 action 経由で起こす機構のため、
    /// effect 列に置かれたマーカーが評価されてもゲーム状態は変化しない。
    /// </para>
    /// <para>
    /// 最初の利用カード(連想可能カード No.X)は本 PR(M3-PR4)では確定しない。カード仕様は M2-PR6+ で JIT 共有される運用
    /// (ADR-0011 §1 「Implementation Notes」/ ADR-0010 §「Implementation Notes」と同パターン)。
    /// 「夢」カード(No.00)は ADR-0011 §6 で M3-PR6 に予定。
    /// </para>
    /// </remarks>
    public sealed record AssociatableMarkerEffect : IEffect;
}
