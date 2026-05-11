namespace Drowsy.Application.Games.DrowZzz.Influences
{
    /// <summary>
    /// <see cref="PlayerInfluence"/> がいつ Tick(発動)するかを表す列挙。
    /// </summary>
    /// <remarks>
    /// M2-PR5 で <c>OwnPhaseStart</c> のみ導入(カード No.02「緑の侵攻」が必要とする単一トリガー)。
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
    }
}
