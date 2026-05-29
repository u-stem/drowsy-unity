namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 連想可能マーカー効果。フィールドなしのマーカー的 record。本効果を効果列に持つカードを
    /// **「連想可能カード」** と定義する。
    /// </summary>
    /// <remarks>
    /// <see cref="DrowZzzRule.IsLegalMove"/> で <see cref="AssociateAction"/> の合法性を判定する際、
    /// <c>action.Card</c> の効果列に本 record が含まれているかで「連想可能性」を判別する
    /// (<see cref="EarlyWinTriggerEffect"/> による「就寝カード」識別と同パターン)。
    /// <para>
    /// <see cref="EffectInterpreter.Apply"/> で評価された場合は **no-op**(session 不変返却)。
    /// マーカー的役割を保ち、効果としての副作用を持たない。連想は <see cref="AssociateAction"/> という独立 action 経由で
    /// 起こす機構のため、effect 列に置かれたマーカーが評価されてもゲーム状態は変化しない。
    /// </para>
    /// </remarks>
    public sealed record AssociatableMarkerEffect : IEffect;
}
