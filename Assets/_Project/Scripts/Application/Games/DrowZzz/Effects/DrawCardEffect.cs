namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 指定プレイヤーが山札から <see cref="Count"/> 枚を手札に引く効果。
    /// </summary>
    /// <param name="Target">引くプレイヤー(M2-PR3 範囲では <see cref="SdpTarget.Self"/> のみサポート、Opponent は将来拡張)</param>
    /// <param name="Count">引く枚数。山札が Count より少ない場合は引ける分だけ引く(DZ-117 graceful degradation)</param>
    /// <remarks>
    /// 「コップ一杯の脅威」(No.01)夜効果「甲: 山札から手段を 1 枚引く」のために M2-PR3 で導入(ADR-0007 §6)。
    /// 山札→手札の移動ロジックは <see cref="DrawCardAction"/> の既存実装(M1-PR4)と同等で、TurnPhase 遷移は伴わない
    /// (Phase 遷移は <see cref="PlayCardAction"/> 全体が管理する、ADR-0007 §3)。
    /// <para>
    /// 山札枯渇の影響は ADR-0007 §「山札枯渇」で計算更新中(本 PR で「コップ一杯の脅威」夜効果 +最大 2 ドロー
    /// → 合計 54 ≤ 56 で余裕 2 枚、`docs/todo.md` の枯渇監視 TODO 継続)。
    /// </para>
    /// </remarks>
    public sealed record DrawCardEffect(SdpTarget Target, int Count) : IEffect;
}
