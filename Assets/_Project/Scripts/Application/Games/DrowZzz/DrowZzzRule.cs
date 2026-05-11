using System;
using System.Collections.Generic;
using System.Linq;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
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
    /// <para>
    /// M2-PR4 で <see cref="EndTurnAction"/> Apply 内に DDP 自動抽選機構を追加(ADR-0009 §4)。
    /// ターン境界(<c>newTurn.CurrentPlayerIndex == 0</c>)を検出し、新ターン番号が
    /// <see cref="DdpPoolConstants.DrawRounds"/> に含まれる場合は <c>DdpPool</c> 先頭から N (= player count) 枚を
    /// 抽選してプレイヤー順に <c>DrawDrowsyPoints</c> に累積する(自動進行、ADR-0009 §4 採用案 A)。
    /// rng は <see cref="StartGameUseCase"/> で <c>DdpPool</c> を事前 Shuffle 済みのため Rule 内では不要、
    /// constructor は ADR-0007 §3 の 2 引数を維持する(本 PR で ADR-0009 §4 の「rng を Rule に注入する案」を
    /// 「StartGame での事前 Shuffle で十分」と再評価、PR description に記録)。
    /// </para>
    /// <para>
    /// M2-PR5 で 3 つの新機構を追加(ADR-0007 §1.5「継続影響」):
    /// <list type="bullet">
    /// <item><b>選択式カード対応</b>: <see cref="PlayCardAction.Choice"/> を読んで <see cref="ChoiceEffect"/> を unwrap
    /// (interpreter には届かない、ApplyPlayCard 内で展開)。範囲外 Choice は illegal-move 扱い(IsLegalMove で false)。</item>
    /// <item><b>影響削除 index thread</b>: <see cref="PlayCardAction.InfluenceRemovalIndex"/> を <see cref="EffectContext"/>
    /// 経由で <see cref="RemoveInfluenceEffect"/> に届ける。</item>
    /// <item><b>フェーズ開始時の Tick</b>: <see cref="EndTurnAction"/> Apply 内、DDP 抽選後に新 <c>CurrentPlayerIndex</c> が
    /// 指すプレイヤーの保有影響を順次 Tick する(<see cref="InfluenceTrigger.OwnPhaseStart"/>)。
    /// 各 Tick は <c>TickEffect</c> を Interpreter で適用 → <c>RemainingCount</c> -1 → 0 で除去。</item>
    /// </list>
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
                DrawCardAction => session.PhaseState == DrowZzzPhaseState.WaitingForDraw,
                PlayCardAction p => IsLegalPlayCard(session, p),
                EndTurnAction => session.PhaseState == DrowZzzPhaseState.WaitingForEndTurn,
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.IsLegalMove ({action.GetType().Name}) は M1 範囲では到達不可。将来 DrowZzzAction 派生型を追加する PR で対応する"),
            };
        }

        // PlayCardAction の合法性: WaitingForPlay フェーズ + 現プレイヤーの Hand に Card が含まれる
        // + (選択式カードなら) Choice が ChoiceEffect.Branches の範囲内
        // M2-PR5: Choice 範囲外を illegal-move 化(InfluenceRemovalIndex は範囲外でも graceful no-op するため illegal 化しない)
        private bool IsLegalPlayCard(DrowZzzGameSession session, PlayCardAction action)
        {
            if (session.PhaseState != DrowZzzPhaseState.WaitingForPlay)
            {
                return false;
            }
            int currentIndex = session.GameState.Turn.CurrentPlayerIndex;
            if (!session.GameState.Players[currentIndex].Hand.Contains(action.Card))
            {
                return false;
            }
            // Choice 範囲チェック: カードが ChoiceEffect を含む場合、action.Choice が範囲内である必要がある。
            // 非選択式カードでは Choice は無視され、(action.Choice != 0) でも合法とする(default 0 想定)。
            var effects = _catalog.GetEffects(action.Card);
            foreach (var e in effects)
            {
                if (e is ChoiceEffect ce)
                {
                    if (action.Choice < 0 || action.Choice >= ce.Branches.Count)
                    {
                        return false;
                    }
                    // 1 カードに ChoiceEffect は高々 1 回想定だが、複数あっても各々を範囲チェック
                }
            }
            return true;
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

        // DrawCardAction の状態遷移: 山札 Top → 現プレイヤー Hand に 1 枚移動 + PhaseState = WaitingForPlay。
        // GameState.Turn は不変(ターン進行は EndTurnAction.Apply の責務、M1-PR6)。
        private static DrowZzzGameSession ApplyDrawCard(DrowZzzGameSession session)
        {
            // 防御的 IsLegalMove 検証
            if (session.PhaseState != DrowZzzPhaseState.WaitingForDraw)
            {
                throw new InvalidOperationException(
                    $"DrawCardAction は WaitingForDraw フェーズでのみ合法です (現フェーズ: {session.PhaseState})");
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
                PhaseState = DrowZzzPhaseState.WaitingForPlay,
            };
        }

        // PlayCardAction の状態遷移:
        // 現プレイヤーの Hand から指定 Card を Remove → Field に AddTop で追加 + PhaseState = WaitingForEndTurn。
        // GameState.Turn / Deck は不変。
        // M2-PR1 で末尾に効果評価を追加: catalog から取得した IEffect 列を左から順に Aggregate で適用する。
        // 効果 0 個なら afterPlay がそのまま返るため M1 完全互換(ADR-0007 §3)。
        // M2-PR5 で 2 つの拡張:
        //  (1) ChoiceEffect を unwrap して action.Choice が指す branch のみ評価(範囲外は IsLegalMove で防御済)
        //  (2) EffectContext(action.InfluenceRemovalIndex) を interpreter に thread
        private DrowZzzGameSession ApplyPlayCard(DrowZzzGameSession session, PlayCardAction action)
        {
            // 防御的 IsLegalMove 検証 (PhaseState + Card 不在の両方を分けて投げる、原因明示のため)
            if (session.PhaseState != DrowZzzPhaseState.WaitingForPlay)
            {
                throw new InvalidOperationException(
                    $"PlayCardAction は WaitingForPlay フェーズでのみ合法です (現フェーズ: {session.PhaseState})");
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
                PhaseState = DrowZzzPhaseState.WaitingForEndTurn,
            };

            // M2-PR1: プレイされたカードの効果列を catalog から取得し、左から順に Interpreter で逐次評価。
            // M2-PR5: ChoiceEffect は unwrap して action.Choice の branch のみ評価(interpreter には届かない)。
            //         EffectContext(InfluenceRemovalIndex) を interpreter に thread し、RemoveInfluenceEffect に届ける。
            var rawEffects = _catalog.GetEffects(action.Card);
            var context = new EffectContext(action.InfluenceRemovalIndex);
            var currentSession = afterPlay;
            foreach (var effect in rawEffects)
            {
                if (effect is ChoiceEffect ce)
                {
                    // IsLegalMove で範囲チェック済。防御的に再検証して範囲外は InvalidOperationException で明示
                    if (action.Choice < 0 || action.Choice >= ce.Branches.Count)
                    {
                        throw new InvalidOperationException(
                            $"PlayCardAction.Choice ({action.Choice}) は ChoiceEffect.Branches (count={ce.Branches.Count}) の範囲外です");
                    }
                    foreach (var inner in ce.Branches[action.Choice])
                    {
                        currentSession = _interpreter.Apply(currentSession, inner, context);
                    }
                }
                else
                {
                    currentSession = _interpreter.Apply(currentSession, effect, context);
                }
            }
            return currentSession;
        }

        // EndTurnAction の状態遷移:
        // GameState.Turn を Next(playerCount) で次フェーズへ進行 + PhaseState = WaitingForDraw。
        // Players / Deck / Discard / Field / FirstDrowsyPoints / SecondDrowsyPoints は不変。
        // ターン上限 (MaxRoundNumber) 判定は本 PR では行わない (M3 で実装、ADR-0006 §7)。
        // M2-PR4: ターン境界 (CurrentPlayerIndex == 0) で新ターン番号が DDP 抽選対象に該当する場合、
        // DdpPool 先頭から N (= player count) 枚を抽選してプレイヤー順に DrawDrowsyPoints へ累積する
        // (ADR-0009 §4 採用案 A、DZ-141 / DZ-142 / DZ-143 / DZ-144)。
        // M2-PR5: 上記 DDP 抽選後、新 CurrentPlayerIndex が指すプレイヤーの保有影響を Tick する
        // (InfluenceTrigger.OwnPhaseStart、ADR-0007 §1.5)。
        private DrowZzzGameSession ApplyEndTurn(DrowZzzGameSession session)
        {
            // 防御的 IsLegalMove 検証
            if (session.PhaseState != DrowZzzPhaseState.WaitingForEndTurn)
            {
                throw new InvalidOperationException(
                    $"EndTurnAction は WaitingForEndTurn フェーズでのみ合法です (現フェーズ: {session.PhaseState})");
            }

            var gameState = session.GameState;
            var newTurn = gameState.Turn.Next(gameState.Players.Count);
            var newGameState = gameState with { Turn = newTurn };

            var nextSession = session with
            {
                GameState = newGameState,
                PhaseState = DrowZzzPhaseState.WaitingForDraw,
            };

            // ターン境界での DDP 自動抽選トリガー(MC/DC ケース 1):
            //   ケース 1: CurrentPlayerIndex == 0 (=全プレイヤー 1 巡完了) かつ 新ターン番号 ∈ {5,9,13,17,21}
            //     → DrawDdpForAllPlayers で N 枚抽選 + 累積
            //   ケース 3 (CurrentPlayerIndex != 0): フェーズ進行のみ、抽選なし
            //   ケース 4 (新ターン番号が DrawRounds 外): フェーズ + ターン進行のみ、抽選なし
            if (newTurn.CurrentPlayerIndex == 0
                && DdpPoolConstants.DrawRounds.Contains(nextSession.Clock.RoundNumber))
            {
                nextSession = DrawDdpForAllPlayers(nextSession);
            }

            // M2-PR5: 新フェーズの current player が保有する影響を Tick(OwnPhaseStart)。
            // 0 件なら no-op、影響を持つ場合は順次 TickEffect 適用 + RemainingCount-1、0 到達で list から除去。
            nextSession = TickInfluencesForCurrentPlayer(nextSession);

            return nextSession;
        }

        // ターン境界 DDP 抽選: GameState.Players 順に 1 枚ずつ DdpPool 先頭から取り出し、
        // 各プレイヤーの DrawDrowsyPoints に累積する。プールは事前 Shuffle 済(StartGameUseCase)
        // のため Draw 順は決定論的で rng 不要(ADR-0009 §4 採用案 A 補足、PR description 参照)。
        private static DrowZzzGameSession DrawDdpForAllPlayers(DrowZzzGameSession session)
        {
            var pool = session.DdpPool;
            // 既存 DrawDrowsyPoints の防御コピーを作成して累積後に置き換える
            var ddpAccumulator = new Dictionary<PlayerId, int>(session.DrawDrowsyPoints.Count);
            foreach (var kv in session.DrawDrowsyPoints)
            {
                ddpAccumulator[kv.Key] = kv.Value;
            }

            foreach (var player in session.GameState.Players)
            {
                // 防御的: プール枯渇は ADR-0009 §「DDP プール枯渇可能性チェック」で
                // 現状想定下では発生しない(総抽選 5 × N=2 = 10 ≤ プール 39)が、
                // DdpPool.Draw が InvalidOperationException を投げる(Pile.Draw と同パターン)。
                var (drawn, remaining) = pool.Draw();
                ddpAccumulator[player.Id] = ddpAccumulator[player.Id] + drawn;
                pool = remaining;
            }

            return session with
            {
                DdpPool = pool,
                DrawDrowsyPoints = ddpAccumulator,
            };
        }

        // M2-PR5: 新フェーズの current player が保有する影響を Tick する(InfluenceTrigger.OwnPhaseStart)。
        // - list 先頭から順に評価(FIFO)
        // - 各影響の TickEffect を Interpreter で適用 + 適用後の session を持ち越し
        // - RemainingCount-1 し、0 到達なら除去、1 以上なら新 PlayerInfluence で置換
        // - 他プレイヤーの影響は不変
        // 影響 0 件なら session 不変返却(graceful no-op)。
        private DrowZzzGameSession TickInfluencesForCurrentPlayer(DrowZzzGameSession session)
        {
            int currentIndex = session.GameState.Turn.CurrentPlayerIndex;
            var currentPlayerId = session.GameState.Players[currentIndex].Id;
            if (!session.Influences.TryGetValue(currentPlayerId, out var currentInfluences)
                || currentInfluences.Count == 0)
            {
                return session;
            }

            // OwnPhaseStart の影響のみを評価対象とする(将来トリガー拡張時に他種類は素通し)。
            // 評価後の新 list を構築:
            //  - TickEffect を Interpreter 適用 → session が新 session に置換
            //  - RemainingCount-1 → 0 ならスキップ、1 以上なら新 PlayerInfluence で置換
            var current = session;
            var rebuilt = new List<PlayerInfluence>(currentInfluences.Count);
            for (int i = 0; i < currentInfluences.Count; i++)
            {
                var inf = currentInfluences[i];
                if (inf.Trigger != InfluenceTrigger.OwnPhaseStart)
                {
                    // 将来トリガー拡張: 他種類は Tick せず保持
                    rebuilt.Add(inf);
                    continue;
                }
                // Tick 評価: TickEffect を current session に適用、context は Default
                // (Tick 内部で RemoveInfluenceEffect 等の per-play 文脈を必要とする効果は M2-PR5 範囲では登場しない)
                current = _interpreter.Apply(current, inf.TickEffect, EffectContext.Default);
                int newCount = inf.RemainingCount - 1;
                if (newCount >= 1)
                {
                    rebuilt.Add(inf with { RemainingCount = newCount });
                }
                // newCount == 0 はスキップ = 除去
            }

            // 影響 dict を新規 build(他プレイヤーの list は不変、current player の list を rebuilt で置換)
            var newInfluences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>(current.Influences.Count);
            foreach (var kv in current.Influences)
            {
                newInfluences[kv.Key] = kv.Key.Equals(currentPlayerId)
                    ? rebuilt
                    : kv.Value;
            }
            return current with { Influences = newInfluences };
        }
    }
}
