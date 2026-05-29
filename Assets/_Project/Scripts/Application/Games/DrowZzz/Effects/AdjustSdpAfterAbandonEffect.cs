namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.TickEffect"/> として使われ、
    /// 影響保有者の <c>AbandonAction</c> 実行直後に発動する Reactive effect(No.12「偽りの太陽」)。
    /// 保有者の SDP に <see cref="Delta"/> を加算する。
    /// </summary>
    /// <param name="Delta">SDP の増減量(正値=増、負値=減、0=no-op)。No.12「偽りの太陽」では Delta=+5。</param>
    /// <remarks>
    /// <para>
    /// 本 effect は <see cref="Drowsy.Application.Games.DrowZzz.Influences.InfluenceTrigger.OnOwnAbandonAfter"/> 専用。
    /// `DrowZzzRule.ApplyAbandon` の末尾で「アクション開始時の influences snapshot」に対して walk され、
    /// 本 AbandonAction で新規付与された影響は snapshot 外で対象外。
    /// </para>
    /// <para>
    /// 計算式:`SDP[保有者] += Delta`。0 floor なし(「持ち点低い方が勝ち」と整合)。
    /// </para>
    /// <para>
    /// `AdjustSdpAfterPlayCardEffect` と完全対称の設計(Delta フィールドあり、再利用性)。
    /// </para>
    /// </remarks>
    public sealed record AdjustSdpAfterAbandonEffect(int Delta) : IEffect;
}
