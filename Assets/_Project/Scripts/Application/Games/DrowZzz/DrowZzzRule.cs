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
            // M3-PR1: ゲーム終了後はいかなる Action も illegal(ADR-0010 §6、Round 22 への遷移ブロックも兼ねる)
            if (session.IsTerminated)
            {
                return false;
            }
            return action switch
            {
                StartGameAction => false, // ADR-0006 §Implementation Notes §StartGameUseCase の IsLegalMove 経由での扱い
                DrawCardAction => session.PhaseState == DrowZzzPhaseState.WaitingForDraw,
                PlayCardAction p => IsLegalPlayCard(session, p),
                EndTurnAction => session.PhaseState == DrowZzzPhaseState.WaitingForEndTurn,
                AbandonAction a => IsLegalAbandon(session, a),  // M3-PR3: 放棄(代替ターン行動)、ADR-0011 §2
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.IsLegalMove ({action.GetType().Name}) は M1 範囲では到達不可。将来 DrowZzzAction 派生型を追加する PR で対応する"),
            };
        }

        // AbandonAction の合法性(M3-PR3、ADR-0011 §2):
        // (1) PhaseState == WaitingForPlay(PlayCardAction の代替フェーズ)
        // (2) 手札に 1 枚以上のカード + CardIndex が範囲内
        // (3) AbandonChoice.RepairBed 選択時のみ、現プレイヤーの BedDamages > 0%(JIT 確定 2026-05-13:0% では修繕不可)
        // 「本能(Instinct)」キーワードによる手札除外は M3-PR5 で追加(本 PR 範囲では未対応)
        private static bool IsLegalAbandon(DrowZzzGameSession session, AbandonAction action)
        {
            if (session.PhaseState != DrowZzzPhaseState.WaitingForPlay)
            {
                return false;
            }
            int currentIndex = session.GameState.Turn.CurrentPlayerIndex;
            var currentHand = session.GameState.Players[currentIndex].Hand;
            if (currentHand.Count == 0 || action.CardIndex < 0 || action.CardIndex >= currentHand.Count)
            {
                return false;
            }
            if (action.Choice == AbandonChoice.RepairBed)
            {
                // 0% では修繕不可(JIT 確定 2026-05-13、(b) 不可選択を採用)
                var currentPlayerId = session.GameState.Players[currentIndex].Id;
                if (session.BedDamages[currentPlayerId] <= DrowZzzBedConstants.MinBedDamagePercent)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 与えられた <paramref name="session"/> がゲーム終了状態かどうかを返す。M3-PR1 で追加(ADR-0010 §1)。
        /// </summary>
        /// <param name="session">問い合わせ対象のセッション</param>
        /// <returns><see cref="DrowZzzGameSession.Outcome"/> が非 null なら <c>true</c></returns>
        /// <exception cref="ArgumentNullException">session が null</exception>
        public bool IsTerminated(DrowZzzGameSession session)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            return session.IsTerminated;
        }

        /// <summary>
        /// 与えられた <paramref name="session"/> から勝者を返す。M3-PR1 で追加(ADR-0010 §1)。
        /// </summary>
        /// <param name="session">問い合わせ対象のセッション</param>
        /// <returns>
        /// <see cref="WinnerOutcome"/> の場合は <see cref="WinnerOutcome.Winner"/>、<see cref="DrawOutcome"/> の場合は <c>null</c>。
        /// </returns>
        /// <exception cref="ArgumentNullException">session が null</exception>
        /// <exception cref="InvalidOperationException"><see cref="IsTerminated"/> が <c>false</c> の session に対する呼び出し</exception>
        public PlayerId GetWinner(DrowZzzGameSession session)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (!session.IsTerminated)
            {
                throw new InvalidOperationException(
                    "GetWinner は未終了の session に対しては呼べません。先に IsTerminated を確認してください(ADR-0010 §1)");
            }
            return session.Outcome switch
            {
                WinnerOutcome w => w.Winner,
                DrawOutcome => null,
                // 構造的に到達不可だが、将来 GameOutcome 派生型が追加された場合の防御
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.GetWinner: 未知の GameOutcome 派生型 ({session.Outcome?.GetType().Name})"),
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
            // M3-PR1: 防御的検証(ADR-0006 §3 / ADR-0010 §6)。IsLegalMove が先にガードする想定だが、
            // 直接 Apply を呼ぶ呼び出し側(テスト経路 / 将来の利用側)に対しても illegal 状態を明示する。
            if (session.IsTerminated)
            {
                throw new InvalidOperationException(
                    "Apply は終了済 session(Outcome != null)に対して呼べません。先に IsLegalMove で合法性を確認してください(ADR-0010 §6)。");
            }
            return action switch
            {
                DrawCardAction => ApplyDrawCard(session),
                PlayCardAction p => ApplyPlayCard(session, p),
                EndTurnAction => ApplyEndTurn(session),
                AbandonAction a => ApplyAbandon(session, a),  // M3-PR3: 放棄(代替ターン行動)、ADR-0011 §2
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.Apply ({action.GetType().Name}) は M1 範囲では到達不可 (StartGameAction は StartGameUseCase 経由)。将来 DrowZzzAction 派生型を追加する PR で対応する"),
            };
        }

        // AbandonAction の状態遷移(M3-PR3、ADR-0011 §2):
        // (1) 現プレイヤーの手札から CardIndex のカードを除去 → Discard に AddTop
        // (2) Choice に応じて SDP +5 (GainSdp) or BedDamages -20% (RepairBed、下限 0%)
        // (3) PhaseState = WaitingForEndTurn(PlayCardAction と同じフェーズ遷移)
        private static DrowZzzGameSession ApplyAbandon(DrowZzzGameSession session, AbandonAction action)
        {
            // 防御的 IsLegalMove 検証
            if (session.PhaseState != DrowZzzPhaseState.WaitingForPlay)
            {
                throw new InvalidOperationException(
                    $"AbandonAction は WaitingForPlay フェーズでのみ合法です (現フェーズ: {session.PhaseState})");
            }
            int currentIndex = session.GameState.Turn.CurrentPlayerIndex;
            var currentPlayer = session.GameState.Players[currentIndex];
            if (currentPlayer.Hand.Count == 0
                || action.CardIndex < 0
                || action.CardIndex >= currentPlayer.Hand.Count)
            {
                throw new InvalidOperationException(
                    $"AbandonAction の CardIndex ({action.CardIndex}) が現プレイヤーの手札 (count={currentPlayer.Hand.Count}) の範囲外です");
            }
            if (action.Choice == AbandonChoice.RepairBed
                && session.BedDamages[currentPlayer.Id] <= DrowZzzBedConstants.MinBedDamagePercent)
            {
                throw new InvalidOperationException(
                    $"AbandonChoice.RepairBed は BedDamages > 0% でのみ合法です (現在: {session.BedDamages[currentPlayer.Id]}%、ADR-0011 §2 JIT 確定)");
            }

            // (1) 手札から CardIndex のカードを Discard に移動
            var discardCard = currentPlayer.Hand.Cards[action.CardIndex];
            var updatedHand = currentPlayer.Hand.Remove(discardCard);
            var newDiscard = session.GameState.Discard.AddTop(discardCard);

            // Players 配列を新しい配列に置換(現プレイヤーのみ差し替え)
            var newPlayers = new PlayerState[session.GameState.Players.Count];
            for (int i = 0; i < newPlayers.Length; i++)
            {
                newPlayers[i] = i == currentIndex ? currentPlayer with { Hand = updatedHand } : session.GameState.Players[i];
            }
            var newGameState = session.GameState with
            {
                Players = newPlayers,
                Discard = newDiscard,
            };
            var afterDiscard = session with
            {
                GameState = newGameState,
                PhaseState = DrowZzzPhaseState.WaitingForEndTurn,
            };

            // (2) Choice に応じた追加効果
            return action.Choice switch
            {
                AbandonChoice.GainSdp => ApplyAbandonGainSdp(afterDiscard, currentPlayer.Id),
                AbandonChoice.RepairBed => ApplyAbandonRepairBed(afterDiscard, currentPlayer.Id),
                _ => throw new NotImplementedException(
                    $"DrowZzzRule.ApplyAbandon: 未知の AbandonChoice ({action.Choice})、将来 enum 拡張時に case 追加"),
            };
        }

        // AbandonChoice.GainSdp の処理: 現プレイヤーの SDP を AbandonSdpGain(+5) だけ増やす
        private static DrowZzzGameSession ApplyAbandonGainSdp(DrowZzzGameSession session, PlayerId currentPlayerId)
        {
            var newSdp = new Dictionary<PlayerId, int>(session.SecondDrowsyPoints.Count);
            foreach (var kv in session.SecondDrowsyPoints)
            {
                newSdp[kv.Key] = kv.Key.Equals(currentPlayerId)
                    ? kv.Value + DrowZzzBedConstants.AbandonSdpGain
                    : kv.Value;
            }
            return session with { SecondDrowsyPoints = newSdp };
        }

        // AbandonChoice.RepairBed の処理: 現プレイヤーの BedDamages を BedRepairPercent(20%) だけ減らす
        // 下限 0% でクランプ(IsLegalMove で BedDamages > 0% を確認済のため通常は減算後も 0 以上だが、防御的に Math.Max)
        private static DrowZzzGameSession ApplyAbandonRepairBed(DrowZzzGameSession session, PlayerId currentPlayerId)
        {
            var newBedDamages = new Dictionary<PlayerId, int>(session.BedDamages.Count);
            foreach (var kv in session.BedDamages)
            {
                newBedDamages[kv.Key] = kv.Key.Equals(currentPlayerId)
                    ? System.Math.Max(DrowZzzBedConstants.MinBedDamagePercent, kv.Value - DrowZzzBedConstants.BedRepairPercent)
                    : kv.Value;
            }
            return session with { BedDamages = newBedDamages };
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

            // M3-PR2: 自フェーズ開始時のベッド破損による SDP マイナス(ADR-0011 §3 / §5「順序保証」の最先頭)。
            // 新 current player の BedDamages を `/ BedDamageRatePerSdp` で SDP マイナスに換算、SDP から減算する
            // (整数除算で切り捨て、0% なら 0 マイナス = no-op)。
            // ADR-0011 §5 順序保証では「ベッドダメージ → DDP 抽選 → 影響 Tick → 終了判定」の順序が確定。
            nextSession = ApplyBedDamageToCurrentPlayer(nextSession);

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

            // M3-PR1: Round 21 完了検出 + Outcome 設定(ADR-0010 §4(b))。
            // 「ターン境界(CurrentPlayerIndex == 0)で新 RoundNumber > MaxRoundNumber(22 以上)」が終了時勝利の境界。
            //   - RoundNumber は Clock の単位(全プレイヤー 1 巡 = 1 ターン、Clock.RoundNumber)
            //   - TurnNumber は Domain の単位(1 プレイヤー 1 行動 = 1 フェーズ、TurnState.TurnNumber)
            //   - N=2 で Round 22 到達は TurnNumber=43 / CurrentPlayerIndex=0 の瞬間
            // TotalPoints を比較し、低い方を WinnerOutcome、等値なら DrawOutcome を session に設定する。
            // ADR-0010 §4「順序保証」の通り、DDP 抽選(3.)/ 影響 Tick(4.)の後に評価する。
            if (newTurn.CurrentPlayerIndex == 0
                && nextSession.Clock.RoundNumber > DrowZzzClockConstants.MaxRoundNumber)
            {
                nextSession = nextSession with { Outcome = DetermineEndOfGameOutcome(nextSession) };
            }

            return nextSession;
        }

        // M3-PR2: 自フェーズ開始時のベッド破損による SDP マイナス計算(ADR-0011 §3 / §5)。
        // 新 current player の BedDamages を `/ DrowZzzBedConstants.BedDamageRatePerSdp` で SDP マイナスに換算。
        // 例: BedDamages[p1]=40% なら `40 / 5 = 8`、SDP[p1] -= 8 を適用。
        // 0% なら計算結果 0 で session 不変返却(no-op、graceful)。
        private static DrowZzzGameSession ApplyBedDamageToCurrentPlayer(DrowZzzGameSession session)
        {
            int currentIndex = session.GameState.Turn.CurrentPlayerIndex;
            var currentPlayerId = session.GameState.Players[currentIndex].Id;
            // BedDamages のキー集合は cross-field 検証で Players と一致が保証されているため、TryGetValue 失敗は構造的に不可能
            // (将来 cross-field 検証が緩んだ場合の保険として明示防御)
            if (!session.BedDamages.TryGetValue(currentPlayerId, out var bedDamagePercent))
            {
                throw new InvalidOperationException(
                    $"内部不変条件違反: BedDamages に PlayerId {currentPlayerId.Value} のキーがありません(cross-field 検証の漏れ)");
            }
            int sdpDamage = bedDamagePercent / DrowZzzBedConstants.BedDamageRatePerSdp;
            if (sdpDamage == 0)
            {
                // 破損 0% / 1〜4%(整数除算で切り捨て 0)では SDP に影響なし、session 不変返却
                return session;
            }
            // SDP を damage 分減算(0 floor なし、ADR-0009 「持ち点低い方が勝ち」と整合、SDP は負値許容)
            var newSdp = new Dictionary<PlayerId, int>(session.SecondDrowsyPoints.Count);
            foreach (var kv in session.SecondDrowsyPoints)
            {
                newSdp[kv.Key] = kv.Key.Equals(currentPlayerId) ? kv.Value - sdpDamage : kv.Value;
            }
            return session with { SecondDrowsyPoints = newSdp };
        }

        // 終了時勝利の Outcome を決定する: 各プレイヤーの TotalPoints を比較し、
        // (a) 低い方が一意なら WinnerOutcome(lower)、(b) 等値なら DrawOutcome を返す。
        // tiebreaker は ADR-0010 §7 で「設けない」と確定済(両者同点で朝を迎えた = 引き分け、SDP / DDP / FDP 個別比較なし)。
        // N>2 拡張時は本メソッドの「2 名比較」を「最小値プレイヤー集合」探索に一般化する必要(現状 N=2 想定)。
        private static GameOutcome DetermineEndOfGameOutcome(DrowZzzGameSession session)
        {
            var players = session.GameState.Players;
            // N=2 想定。N=1 は StartGameUseCase で弾かれている前提だが、防御的に処理。
            if (players.Count != 2)
            {
                throw new InvalidOperationException(
                    $"M3-PR1 範囲では N=2 のみ対応(現在 Players.Count={players.Count})。N>2 拡張は Phase 3 候補。");
            }
            var pointsA = session.TotalPoints(players[0].Id);
            var pointsB = session.TotalPoints(players[1].Id);
            if (pointsA == pointsB)
            {
                return new DrawOutcome();
            }
            return new WinnerOutcome(pointsA < pointsB ? players[0].Id : players[1].Id);
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
