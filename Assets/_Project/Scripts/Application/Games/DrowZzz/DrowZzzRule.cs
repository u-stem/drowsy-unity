using System;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の状態遷移ルール。<see cref="IGameRule{TAction, TSession}"/> の DrowZzz 具象実装。
    /// </summary>
    /// <remarks>
    /// 本 PR (M1-PR2) は **skeleton 段階** のため <see cref="IsLegalMove"/> / <see cref="Apply"/> は
    /// <see cref="NotImplementedException"/> を投げる。本格実装は M1-PR3〜PR6 で各 Action 種別ごとに段階的に追加する
    /// (ADR-0006 §M1 着手 PR 群を参照)。
    /// </remarks>
    public sealed class DrowZzzRule : IGameRule<DrowZzzAction, DrowZzzGameSession>
    {
        /// <summary>
        /// 与えられた <paramref name="session"/> 状態で <paramref name="action"/> が合法かを返す。
        /// 本 PR では未実装(skeleton)。M1-PR3〜PR6 で TurnPhase ↔ Action 種別の合法性表に従って実装する
        /// (ADR-0006 §2.4 §IsLegalMove の判定 表)。
        /// </summary>
        /// <exception cref="NotImplementedException">M1-PR2 (skeleton) では常に投出される</exception>
        public bool IsLegalMove(DrowZzzGameSession session, DrowZzzAction action) =>
            throw new NotImplementedException("DrowZzzRule.IsLegalMove は M1-PR3〜PR6 で実装される (ADR-0006 §M1 着手 PR 群)");

        /// <summary>
        /// 与えられた <paramref name="session"/> 状態に <paramref name="action"/> を適用した次セッションを返す。
        /// 本 PR では未実装(skeleton)。M1-PR3〜PR6 で各 Action 種別 (Draw / Play / EndTurn) を順次実装する
        /// (StartGameAction は <c>StartGameUseCase</c> 経由で別系統、ADR-0006 §3 参照)。
        /// </summary>
        /// <exception cref="NotImplementedException">M1-PR2 (skeleton) では常に投出される</exception>
        public DrowZzzGameSession Apply(DrowZzzGameSession session, DrowZzzAction action) =>
            throw new NotImplementedException("DrowZzzRule.Apply は M1-PR3〜PR6 で実装される (ADR-0006 §M1 着手 PR 群)");
    }
}
