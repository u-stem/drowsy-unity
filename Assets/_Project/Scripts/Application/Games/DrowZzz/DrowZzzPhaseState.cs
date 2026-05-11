namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz のフェーズ内の待ち状態(Application 層が管理するフェーズ内ステートマシン)。
    /// Phase 1 <see cref="Drowsy.Domain.Game.TurnState"/> には影響を与えない。
    /// </summary>
    /// <remarks>
    /// 用語規約(ADR-0009 §「用語規約」):
    /// <list type="bullet">
    /// <item><b>ターン</b>(30 分 = 全プレイヤー 1 巡 = <see cref="DrowZzzClock.RoundNumber"/> の単位)</item>
    /// <item><b>フェーズ</b>(1 プレイヤー 1 行動 = <see cref="Drowsy.Domain.Game.TurnState.TurnNumber"/> の単位)</item>
    /// <item><b>PhaseState</b>(フェーズ内の待ち状態 = 本 enum、WaitingForDraw / WaitingForPlay / WaitingForEndTurn)</item>
    /// </list>
    /// 詳細は ADR-0006 §2.3(旧名 <c>DrowZzzTurnPhase</c> の導入経緯)/ ADR-0009 §「用語規約」(本 enum への改名根拠)を参照。
    /// </remarks>
    public enum DrowZzzPhaseState
    {
        /// <summary>ドロー待ち。<c>DrawCardAction</c> のみが合法。</summary>
        WaitingForDraw,

        /// <summary>カードプレイ待ち。<c>PlayCardAction(card)</c> のみが合法(card は手札に存在する必要あり)。</summary>
        WaitingForPlay,

        /// <summary>フェーズ終了待ち。<c>EndTurnAction</c> のみが合法。</summary>
        WaitingForEndTurn,
    }
}
