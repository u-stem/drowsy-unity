namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 指定プレイヤーが山札から <see cref="Count"/> 枚を手札に引く効果。
    /// </summary>
    /// <param name="Target">引くプレイヤー(M2-PR3 範囲では <see cref="SdpTarget.Self"/> のみサポート、Opponent は将来拡張)</param>
    /// <param name="Count">引く枚数。山札が Count より少ない場合は引ける分だけ引く(DZ-117 graceful degradation)</param>
    /// <remarks>
    /// 「コップ一杯の脅威」(No.01)夜効果「自分が山札から手段を 1 枚引く」のために導入。
    /// 山札→手札の移動ロジックは <see cref="DrawCardAction"/> の既存実装と同等で、TurnPhase 遷移は伴わない
    /// (Phase 遷移は <see cref="PlayCardAction"/> 全体が管理する)。
    /// </remarks>
    public sealed record DrawCardEffect(SdpTarget Target, int Count) : IEffect;
}
