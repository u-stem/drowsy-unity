namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.TickEffect"/> として使われ、
    /// 影響保有者の <c>PlayCardAction</c> 実行直後に発動する Reactive effect(ADR-0022 / No.12「偽りの太陽」、2026-05-17 で導入)。
    /// 保有者の SDP に <see cref="Delta"/> を加算する。
    /// </summary>
    /// <param name="Delta">SDP の増減量(正値=増、負値=減、0=no-op)。No.12「偽りの太陽」では Delta=-10。</param>
    /// <remarks>
    /// <para>
    /// 本 effect は <see cref="Drowsy.Application.Games.DrowZzz.Influences.InfluenceTrigger.OnOwnPlayCardAfter"/> 専用。
    /// `DrowZzzRule.ApplyPlayCard` の末尾で「アクション開始時の influences snapshot」に対して walk され、
    /// 本 PlayCardAction で新規付与された影響は snapshot 外で対象外(ADR-0022 §4 snapshot ベース walk)。
    /// </para>
    /// <para>
    /// 計算式:`SDP[保有者] += Delta`。0 floor なし(ADR-0009「持ち点低い方が勝ち」と整合)。
    /// </para>
    /// <para>
    /// 将来「使用時 SDP+3」「使用時 SDP-20」等のカードが出てきた際にも本 effect record(Delta 値変更)で再利用可能
    /// (No.02 `AdjustSdpEffect(SdpTarget, int Delta)` と同パターン、カード固有特化型ではなく Delta フィールドあり)。
    /// </para>
    /// </remarks>
    public sealed record AdjustSdpAfterPlayCardEffect(int Delta) : IEffect;
}
