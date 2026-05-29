namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 連想後使用制限マーカー効果。**2 役を兼ねるマーカー**:
    /// <list type="number">
    /// <item><b>カードの効果列内に存在する場合</b>(=「夢」のカードデータ):<see cref="DrowZzzRule.ApplyAssociate"/> で検出 →
    /// 自プレイヤーに <c>PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, RemainingCount=1)</c> を付与する trigger marker。</item>
    /// <item><b><see cref="Influences.PlayerInfluence.TickEffect"/> として保有される場合</b>(=連想で付与された Influence の中身):
    /// <see cref="DrowZzzRule.IsLegalPlayCard"/> で「該当カードの使用を illegal にする」フラグ。Tick 時は
    /// <see cref="EffectInterpreter.Apply"/> で no-op(<c>RemainingCount</c> 1→0 で除去)。</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// 「次の自分のターン以降に限り使用可能」semantics を <see cref="Influences.PlayerInfluence"/> 流用で表現する。
    /// 連想時に 1 フェーズ分の使用制限を付与し、次の自フェーズ開始時の
    /// Tick で除去されることで「次の自分のターン以降」を実現する(N=2 の場合、相手フェーズを経由して自分が current に戻る間隔)。
    /// <para>
    /// 検出は <see cref="DrowZzzRule.ApplyAssociate"/> / <see cref="DrowZzzRule.IsLegalPlayCard"/> 両者で
    /// 対象カードの効果列を **最上位 scan** する(<see cref="RequiresMinimumTotalPointsMarkerEffect"/> と同じスコープ)。
    /// </para>
    /// <para>
    /// <see cref="EffectInterpreter.Apply"/> で評価された場合は **no-op**(session 不変返却)。
    /// 効果列内のマーカーとしても Tick 時の <c>TickEffect</c> としても、本 effect 自体は session を変えない設計
    /// (<see cref="AssociatableMarkerEffect"/> と同パターン)。
    /// </para>
    /// </remarks>
    public sealed record UsageRestrictionMarkerEffect : IEffect;
}
