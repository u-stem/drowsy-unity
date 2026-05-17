namespace Drowsy.Application.Games.DrowZzz.Influences
{
    /// <summary>
    /// <see cref="PlayerInfluence"/> がいつ Tick(発動)するかを表す列挙。
    /// </summary>
    /// <remarks>
    /// M2-PR5 で <c>OwnPhaseStart</c> のみ導入(カード No.02「緑の侵攻」が必要とする単一トリガー)。
    /// ADR-0022(2026-05-17)で Reactive Influence(アクション後発動型)対応として
    /// <c>OnOwnPlayCardAfter</c> / <c>OnOwnAbandonAfter</c> を追加(No.12「偽りの太陽」)。
    /// 将来後続効果カードで他のトリガー(<c>OpponentPhaseStart</c> / <c>OwnPhaseEnd</c> / <c>RoundEnd</c> 等)が
    /// 登場した時点で値を追加する(ADR-0007 §1.5「継続影響」、JIT 拡張方針)。
    /// <para>
    /// 用語規約は ADR-0009「用語規約」と整合:
    /// <list type="bullet">
    /// <item><b>ターン</b>: 30 分 = 全プレイヤー 1 巡 = <see cref="DrowZzzClock.RoundNumber"/> の単位</item>
    /// <item><b>フェーズ</b>: 1 プレイヤー 1 行動 = <see cref="Drowsy.Domain.Game.TurnState.TurnNumber"/> の単位</item>
    /// </list>
    /// カードフレーバー上「自分のターン開始時」と書かれていても、実装は「影響保有者の <b>フェーズ</b> 開始時」
    /// (= <see cref="OwnPhaseStart"/>)であることに留意。
    /// </para>
    /// </remarks>
    public enum InfluenceTrigger
    {
        /// <summary>
        /// 影響保有者の自フェーズ開始時に発動する。
        /// </summary>
        /// <remarks>
        /// 検出箇所: <c>DrowZzzRule.ApplyEndTurn</c> の DDP 抽選後 / 新フェーズ確定後に、
        /// 新 <c>CurrentPlayerIndex</c> が指すプレイヤーの保有影響を Tick する。
        /// </remarks>
        OwnPhaseStart,

        /// <summary>
        /// 影響保有者の <c>PlayCardAction</c> 実行直後に発動する(ADR-0022 / No.12「偽りの太陽」)。
        /// </summary>
        /// <remarks>
        /// 検出箇所: <c>DrowZzzRule.ApplyPlayCard</c> の末尾で、当該アクション **開始時** の influences snapshot に対して walk
        /// (本アクションで新規付与された影響は snapshot 外で対象外、ADR-0022 §4 snapshot ベース walk)。
        /// </remarks>
        OnOwnPlayCardAfter,

        /// <summary>
        /// 影響保有者の <c>AbandonAction</c> 実行直後に発動する(ADR-0022 / No.12「偽りの太陽」)。
        /// </summary>
        /// <remarks>
        /// 検出箇所: <c>DrowZzzRule.ApplyAbandon</c> の末尾、<see cref="OnOwnPlayCardAfter"/> と同じ snapshot ベース walk。
        /// </remarks>
        OnOwnAbandonAfter,
    }
}
