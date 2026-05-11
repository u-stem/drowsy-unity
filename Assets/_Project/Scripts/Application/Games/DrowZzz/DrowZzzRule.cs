using System;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の状態遷移ルール。<see cref="IGameRule{TAction, TSession}"/> の DrowZzz 具象実装。
    /// </summary>
    /// <remarks>
    /// M1-PR4 段階で実装済の挙動:
    /// <list type="bullet">
    /// <item><see cref="IsLegalMove"/>: <see cref="StartGameAction"/> → <c>false</c>(M1-PR3)、
    /// <see cref="DrawCardAction"/> → <c>session.TurnPhase == WaitingForDraw</c> なら <c>true</c>(M1-PR4)。
    /// <see cref="PlayCardAction"/> / <see cref="EndTurnAction"/> は依然 <see cref="NotImplementedException"/>(M1-PR5/PR6)。</item>
    /// <item><see cref="Apply"/>: <see cref="DrawCardAction"/> のみ実装(M1-PR4、TurnPhase 遷移 + 手札更新)。
    /// それ以外は依然 <see cref="NotImplementedException"/>(<see cref="StartGameAction"/> は <c>StartGameUseCase</c> 経由で扱うため
    /// 本 Rule の <see cref="Apply"/> ルートには来ない設計、ADR-0006 §Implementation Notes)。</item>
    /// </list>
    /// 引数 (<paramref name="session"/> / <paramref name="action"/>) の null は <see cref="ArgumentNullException"/>。
    /// (M1-PR4 で全 Action 種別共通の null 検証として導入、M1-PR3 reviewer 申し送り N-7 反映)
    /// </remarks>
    public sealed class DrowZzzRule : IGameRule<DrowZzzAction, DrowZzzGameSession>
    {
        /// <summary>
        /// 与えられた <paramref name="session"/> 状態で <paramref name="action"/> が合法かを返す。
        /// </summary>
        /// <exception cref="ArgumentNullException">session または action が null</exception>
        /// <exception cref="NotImplementedException">非 <see cref="StartGameAction"/> / 非 <see cref="DrawCardAction"/> は M1-PR5〜PR6 で実装される</exception>
        public bool IsLegalMove(DrowZzzGameSession session, DrowZzzAction action)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            return action switch
            {
                StartGameAction => false, // ADR-0006 §Implementation Notes §StartGameUseCase の IsLegalMove 経由での扱い
                DrawCardAction => session.TurnPhase == DrowZzzTurnPhase.WaitingForDraw,
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.IsLegalMove ({action.GetType().Name}) は M1-PR5〜PR6 で実装される (ADR-0006 §M1 着手 PR 群)"),
            };
        }

        /// <summary>
        /// 与えられた <paramref name="session"/> 状態に <paramref name="action"/> を適用した次セッションを返す。
        /// 呼び出し側は事前に <see cref="IsLegalMove"/> で合法性を確認する想定だが、
        /// Rule 内部でも防御的に検証し違反時は <see cref="InvalidOperationException"/> を投げる
        /// (ADR-0006 §3 §IsLegalMove 違反時の方針)。
        /// </summary>
        /// <exception cref="ArgumentNullException">session または action が null</exception>
        /// <exception cref="InvalidOperationException">IsLegalMove が false を返す状態で Apply された場合、または山札枯渇で <see cref="Pile.Draw"/> が失敗した場合</exception>
        /// <exception cref="NotImplementedException">非 <see cref="DrawCardAction"/> は M1-PR5〜PR6 で実装される</exception>
        public DrowZzzGameSession Apply(DrowZzzGameSession session, DrowZzzAction action)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            return action switch
            {
                DrawCardAction => ApplyDrawCard(session),
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.Apply ({action.GetType().Name}) は M1-PR5〜PR6 で実装される (ADR-0006 §M1 着手 PR 群)"),
            };
        }

        // DrawCardAction の状態遷移: 山札 Top → 現プレイヤー Hand に 1 枚移動 + TurnPhase = WaitingForPlay。
        // GameState.Turn は不変(ターン進行は EndTurnAction.Apply の責務、M1-PR6)。
        private static DrowZzzGameSession ApplyDrawCard(DrowZzzGameSession session)
        {
            // 防御的 IsLegalMove 検証
            if (session.TurnPhase != DrowZzzTurnPhase.WaitingForDraw)
            {
                throw new InvalidOperationException(
                    $"DrawCardAction は WaitingForDraw フェーズでのみ合法です (現フェーズ: {session.TurnPhase})");
            }

            var gameState = session.GameState;
            int currentIndex = gameState.Turn.CurrentPlayerIndex;
            var current = gameState.Players[currentIndex];

            // 山札から 1 枚 Draw(空 Pile は Pile.Draw が InvalidOperationException を投げる)
            var (drawn, remainingDeck) = gameState.Deck.Draw();

            // 現プレイヤーの Hand に追加
            var updatedPlayer = current with { Hand = current.Hand.Add(drawn) };

            // Players 配列を新しい配列に置換(防御コピー、現プレイヤーのみ差し替え)
            var newPlayers = new PlayerState[gameState.Players.Count];
            for (int i = 0; i < newPlayers.Length; i++)
            {
                newPlayers[i] = i == currentIndex ? updatedPlayer : gameState.Players[i];
            }

            var newGameState = gameState with
            {
                Players = newPlayers,
                Deck = remainingDeck,
            };

            return session with
            {
                GameState = newGameState,
                TurnPhase = DrowZzzTurnPhase.WaitingForPlay,
            };
        }
    }
}
