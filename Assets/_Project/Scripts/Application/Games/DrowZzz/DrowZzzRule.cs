using System;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の状態遷移ルール。<see cref="IGameRule{TAction, TSession}"/> の DrowZzz 具象実装。
    /// </summary>
    /// <remarks>
    /// M1-PR3 段階では <see cref="IsLegalMove"/> の <see cref="StartGameAction"/> 分岐のみが実装済 (常に false)。
    /// 他の Action 種別の <see cref="IsLegalMove"/> および全 Action の <see cref="Apply"/> は依然
    /// <see cref="NotImplementedException"/>。本格実装は M1-PR4〜PR6 で各 Action 種別ごとに段階的に追加する
    /// (ADR-0006 §M1 着手 PR 群を参照)。
    /// </remarks>
    public sealed class DrowZzzRule : IGameRule<DrowZzzAction, DrowZzzGameSession>
    {
        /// <summary>
        /// 与えられた <paramref name="session"/> 状態で <paramref name="action"/> が合法かを返す。
        /// 本 PR (M1-PR3) では <see cref="StartGameAction"/> は常に <c>false</c>(セッション未生成用、
        /// <c>StartGameUseCase</c> 経由で扱うため、ADR-0006 §Implementation Notes §StartGameUseCase の
        /// <c>IsLegalMove</c> 経由での扱い)。他の Action 種別は M1-PR4〜PR6 で TurnPhase ↔ Action 種別の
        /// 合法性表に従って実装する (ADR-0006 §2.4 §IsLegalMove の判定 表)。
        /// </summary>
        /// <exception cref="NotImplementedException">non-<see cref="StartGameAction"/> は M1-PR3 段階では常に投出される</exception>
        public bool IsLegalMove(DrowZzzGameSession session, DrowZzzAction action) =>
            action switch
            {
                StartGameAction => false,
                // 注: action == null も `_` にフォールスルーして NotImplementedException が投げられる。
                // ArgumentNullException としての null 検証は M1-PR4 (DrawCardAction Apply 実装時) で
                // 各 case の本格実装と同時に追加する想定。
                _ => throw new NotImplementedException(
                    "DrowZzzRule.IsLegalMove (non-StartGameAction) は M1-PR4〜PR6 で実装される (ADR-0006 §M1 着手 PR 群)"),
            };

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
