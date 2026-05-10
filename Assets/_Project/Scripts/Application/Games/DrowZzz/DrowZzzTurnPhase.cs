namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz のターン内フェーズ(Application 層が管理するターン内ステートマシン)。
    /// Phase 1 <see cref="Drowsy.Domain.Game.TurnState"/> には影響を与えない。
    /// 詳細は ADR-0006 §2.3 を参照。
    /// </summary>
    public enum DrowZzzTurnPhase
    {
        /// <summary>ドロー待ち。<c>DrawCardAction</c> のみが合法。</summary>
        WaitingForDraw,

        /// <summary>カードプレイ待ち。<c>PlayCardAction(card)</c> のみが合法(card は手札に存在する必要あり)。</summary>
        WaitingForPlay,

        /// <summary>ターン終了待ち。<c>EndTurnAction</c> のみが合法。</summary>
        WaitingForEndTurn,
    }
}
