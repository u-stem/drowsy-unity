using System;
using System.Linq;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の状態遷移ルール。<see cref="IGameRule{TAction, TSession}"/> の DrowZzz 具象実装。
    /// </summary>
    /// <remarks>
    /// M1-PR6 段階で M1 範囲の全 Action 種別が実装済:
    /// <list type="bullet">
    /// <item><see cref="IsLegalMove"/>: <see cref="StartGameAction"/> → <c>false</c>(M1-PR3、<c>StartGameUseCase</c> 経由で扱うため)、
    /// <see cref="DrawCardAction"/> → <c>WaitingForDraw</c> のみ <c>true</c>(M1-PR4)、
    /// <see cref="PlayCardAction"/> → <c>WaitingForPlay</c> かつ現プレイヤーの <c>Hand</c> に <c>Card</c> がある場合 <c>true</c>(M1-PR5)、
    /// <see cref="EndTurnAction"/> → <c>WaitingForEndTurn</c> のみ <c>true</c>(M1-PR6)。</item>
    /// <item><see cref="Apply"/>: <see cref="DrawCardAction"/> / <see cref="PlayCardAction"/> / <see cref="EndTurnAction"/> を実装済。
    /// <see cref="StartGameAction"/> は <c>StartGameUseCase</c> 経由で扱うため本 Rule の <see cref="Apply"/> ルートには来ない設計、
    /// ADR-0006 §Implementation Notes。
    /// 将来 DrowZzz の Action 種別を新規追加する場合、<c>_</c> ケースの <see cref="NotImplementedException"/> が守る。</item>
    /// </list>
    /// 引数 (<paramref name="session"/> / <paramref name="action"/>) の null は <see cref="ArgumentNullException"/>。
    /// (M1-PR4 で全 Action 種別共通の null 検証として導入、M1-PR3 reviewer 申し送り N-7 反映)
    /// <para>
    /// M2-PR1 で constructor injection に <see cref="ICardCatalog{TEffect}"/>(<c>TEffect = IEffect</c>)/
    /// <see cref="EffectInterpreter"/> を追加(ADR-0007 §3)。<see cref="PlayCardAction"/> の Apply 後、
    /// catalog から効果列を取得し、<c>Aggregate</c> で左から順に逐次評価する。効果 0 個なら M1 と完全互換。
    /// </para>
    /// </remarks>
    public sealed class DrowZzzRule : IGameRule<DrowZzzAction, DrowZzzGameSession>
    {
        private readonly ICardCatalog<IEffect> _catalog;
        private readonly EffectInterpreter _interpreter;

        /// <summary>
        /// <see cref="DrowZzzRule"/> を生成する。
        /// </summary>
        /// <param name="catalog">カード→効果列の引きに利用する <see cref="ICardCatalog{TEffect}"/></param>
        /// <param name="interpreter">効果適用を担う <see cref="EffectInterpreter"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="catalog"/> または <paramref name="interpreter"/> が null</exception>
        public DrowZzzRule(ICardCatalog<IEffect> catalog, EffectInterpreter interpreter)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
        }

        /// <summary>
        /// 与えられた <paramref name="session"/> 状態で <paramref name="action"/> が合法かを返す。
        /// </summary>
        /// <exception cref="ArgumentNullException">session または action が null</exception>
        /// <exception cref="NotImplementedException">M1 範囲外の <see cref="DrowZzzAction"/> 派生型(将来拡張用、M1 では到達不可)</exception>
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
                PlayCardAction p => IsLegalPlayCard(session, p),
                EndTurnAction => session.TurnPhase == DrowZzzTurnPhase.WaitingForEndTurn,
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.IsLegalMove ({action.GetType().Name}) は M1 範囲では到達不可。将来 DrowZzzAction 派生型を追加する PR で対応する"),
            };
        }

        // PlayCardAction の合法性: WaitingForPlay フェーズ かつ 現プレイヤーの Hand に Card が含まれる
        private static bool IsLegalPlayCard(DrowZzzGameSession session, PlayCardAction action)
        {
            if (session.TurnPhase != DrowZzzTurnPhase.WaitingForPlay)
            {
                return false;
            }
            int currentIndex = session.GameState.Turn.CurrentPlayerIndex;
            return session.GameState.Players[currentIndex].Hand.Contains(action.Card);
        }

        /// <summary>
        /// 与えられた <paramref name="session"/> 状態に <paramref name="action"/> を適用した次セッションを返す。
        /// 呼び出し側は事前に <see cref="IsLegalMove"/> で合法性を確認する想定だが、
        /// Rule 内部でも防御的に検証し違反時は <see cref="InvalidOperationException"/> を投げる
        /// (ADR-0006 §3 §IsLegalMove 違反時の方針)。
        /// </summary>
        /// <remarks>
        /// <see cref="StartGameAction"/> は <c>ApplyActionUseCase</c> の <see cref="IsLegalMove"/> チェックで事前排除されるため
        /// 通常は本 <see cref="Apply"/> ルートに到達しない設計(ADR-0006 §Implementation Notes)。
        /// もし直接呼ばれた場合は <see cref="NotImplementedException"/> が投げられる(`_` ケースのフォールバック)。
        /// </remarks>
        /// <exception cref="ArgumentNullException">session または action が null</exception>
        /// <exception cref="InvalidOperationException">IsLegalMove が false を返す状態で Apply された場合、または山札枯渇で <see cref="Pile.Draw"/> が失敗した場合</exception>
        /// <exception cref="NotImplementedException">M1 範囲外の <see cref="DrowZzzAction"/> 派生型(将来拡張用、`_` ケース)</exception>
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
                PlayCardAction p => ApplyPlayCard(session, p),
                EndTurnAction => ApplyEndTurn(session),
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.Apply ({action.GetType().Name}) は M1 範囲では到達不可 (StartGameAction は StartGameUseCase 経由)。将来 DrowZzzAction 派生型を追加する PR で対応する"),
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

        // PlayCardAction の状態遷移:
        // 現プレイヤーの Hand から指定 Card を Remove → Field に AddTop で追加 + TurnPhase = WaitingForEndTurn。
        // GameState.Turn / Deck は不変。
        // M2-PR1 で末尾に効果評価を追加: catalog から取得した IEffect 列を左から順に Aggregate で適用する。
        // 効果 0 個なら afterPlay がそのまま返るため M1 完全互換(ADR-0007 §3)。
        private DrowZzzGameSession ApplyPlayCard(DrowZzzGameSession session, PlayCardAction action)
        {
            // 防御的 IsLegalMove 検証 (TurnPhase + Card 不在の両方を分けて投げる、原因明示のため)
            if (session.TurnPhase != DrowZzzTurnPhase.WaitingForPlay)
            {
                throw new InvalidOperationException(
                    $"PlayCardAction は WaitingForPlay フェーズでのみ合法です (現フェーズ: {session.TurnPhase})");
            }

            var gameState = session.GameState;
            int currentIndex = gameState.Turn.CurrentPlayerIndex;
            var current = gameState.Players[currentIndex];

            if (!current.Hand.Contains(action.Card))
            {
                throw new InvalidOperationException(
                    $"PlayCardAction の Card ({action.Card.Value}) は現プレイヤーの手札に含まれません");
            }

            // 手札から指定 Card を Remove(防御検証で Card 存在確認済のため Remove は成功する)
            var updatedPlayer = current with { Hand = current.Hand.Remove(action.Card) };

            // Field に AddTop (直近プレイカードが Field.Cards[0]、ADR は ADR-0006 §M1-PR5 補足、JIT 確定 2026-05-11)
            var newField = gameState.Field.AddTop(action.Card);

            // Players 配列を新しい配列に置換
            var newPlayers = new PlayerState[gameState.Players.Count];
            for (int i = 0; i < newPlayers.Length; i++)
            {
                newPlayers[i] = i == currentIndex ? updatedPlayer : gameState.Players[i];
            }

            var newGameState = gameState with
            {
                Players = newPlayers,
                Field = newField,
            };

            // M1-PR5 互換のプレイ後セッション(効果評価前)
            var afterPlay = session with
            {
                GameState = newGameState,
                TurnPhase = DrowZzzTurnPhase.WaitingForEndTurn,
            };

            // M2-PR1: プレイされたカードの効果列を catalog から取得し、左から順に Interpreter で逐次評価。
            // M2-PR1 段階では InMemoryCardCatalog.GetEffects が常に空列を返すため、Aggregate の初期値
            // (afterPlay) がそのまま返り、結果として M1 と完全互換になる。
            var effects = _catalog.GetEffects(action.Card);
            return effects.Aggregate(afterPlay, (s, e) => _interpreter.Apply(s, e));
        }

        // EndTurnAction の状態遷移:
        // GameState.Turn を Next(playerCount) で次サブターンへ進行 + TurnPhase = WaitingForDraw。
        // Players / Deck / Discard / Field / FirstDrowsyPoints は不変。
        // ターン上限 (MaxRoundNumber) 判定は本 PR では行わない (M3 で実装、ADR-0006 §7)。
        private static DrowZzzGameSession ApplyEndTurn(DrowZzzGameSession session)
        {
            // 防御的 IsLegalMove 検証
            if (session.TurnPhase != DrowZzzTurnPhase.WaitingForEndTurn)
            {
                throw new InvalidOperationException(
                    $"EndTurnAction は WaitingForEndTurn フェーズでのみ合法です (現フェーズ: {session.TurnPhase})");
            }

            var gameState = session.GameState;
            var newTurn = gameState.Turn.Next(gameState.Players.Count);
            var newGameState = gameState with { Turn = newTurn };

            return session with
            {
                GameState = newGameState,
                TurnPhase = DrowZzzTurnPhase.WaitingForDraw,
            };
        }
    }
}
