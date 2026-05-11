using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    [TestFixture]
    public class DrowZzzRuleTests
    {
        // ===== ヘルパー =====

        // M2-PR1: DrowZzzRule の constructor は ICardCatalog<IEffect> / EffectInterpreter を要求するようになった
        // (ADR-0007 §3)。M1-PR5 までと同じ挙動を維持するため、効果定義のない catalog (= 全カード空効果列) +
        // 標準 Interpreter を渡す。共通テストヘルパー抽出は docs/todo.md TODO で別途追跡。
        private static DrowZzzRule NewRule() =>
            new DrowZzzRule(
                new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]),
                new EffectInterpreter());

        // 全引数オプション、デフォルトは N=2 / WaitingForDraw / 空 Deck / 空 Hand / 空 DdpPool / 全 DP=0 / 空 Influences
        private static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForDraw,
            int currentPlayerIndex = 0,
            Pile deck = null,
            Hand p0Hand = null,
            Hand p1Hand = null,
            int turnNumber = 1,
            DdpPool ddpPool = null,
            IReadOnlyDictionary<PlayerId, int> ddp = null,
            IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerInfluence>> influences = null)
        {
            var p0 = new PlayerState(PlayerId.Of("p1"), p0Hand ?? Hand.Empty);
            var p1 = new PlayerState(PlayerId.Of("p2"), p1Hand ?? Hand.Empty);
            var gs = new GameState(
                new[] { p0, p1 },
                deck ?? Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                new TurnState(turnNumber, currentPlayerIndex));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 10,
            };
            // SDP は M2-PR3 で追加(ADR-0009 §「DP 種別」)。本ヘルパー利用テストは SDP に関心がないため
            // 全プレイヤー 0 で固定初期化する。
            var sdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // DDP / DdpPool は M2-PR4 で追加(ADR-0009 §「DP 種別」/ §「DDP プールの構造」)。
            // DDP 自動抽選機構を検証しないテストはデフォルト DDP=0 / 空 DdpPool で十分。
            var ddpResolved = ddp ?? new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // M2-PR5: Influences は引数指定なら採用、未指定なら空 list 固定
            var influencesResolved = influences ?? new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            return new DrowZzzGameSession(gs, fdp, ddpResolved, sdp, ddpPool ?? DdpPool.Empty, influencesResolved, phase);
        }

        private static Pile NewDeck(params string[] cardIds)
        {
            var cards = new CardId[cardIds.Length];
            for (int i = 0; i < cardIds.Length; i++)
            {
                cards[i] = CardId.Of(cardIds[i]);
            }
            return new Pile(cards);
        }

        // ===== DZ-012 / DZ-013: 将来拡張防御 (M1 範囲外 DrowZzzAction 派生型のフォールバック)
        //       M1-PR6 で M1 範囲の全 Action 実装済となり、`_` ケースは到達不可だが
        //       カバレッジ確保 + 将来派生型追加時の安全網として、ダミー派生型でテストする =====

        // テスト用ダミー: M1 範囲外の架空 DrowZzzAction 派生型
        private sealed record UnknownDrowZzzAction : DrowZzzAction;

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-012")]
        public void Given_DrowZzzRule_When_未知のDrowZzzAction派生型でIsLegalMoveを呼ぶ_Then_NotImplementedExceptionを投げる()
        {
            // Given(M1 範囲外派生型、`_` ケースのフォールバック)
            var rule = NewRule();
            var session = NewSession();
            var action = new UnknownDrowZzzAction();
            // When / Then
            Assert.Throws<NotImplementedException>(() => rule.IsLegalMove(session, action));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-013")]
        public void Given_DrowZzzRule_When_未知のDrowZzzAction派生型でApplyを呼ぶ_Then_NotImplementedExceptionを投げる()
        {
            // Given(M1 範囲外派生型、`_` ケースのフォールバック。StartGameAction も同経路に来る)
            var rule = NewRule();
            var session = NewSession();
            var action = new UnknownDrowZzzAction();
            // When / Then
            Assert.Throws<NotImplementedException>(() => rule.Apply(session, action));
        }

        // ===== DZ-034: StartGameAction → false (M1-PR3) =====

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-034")]
        public void Given_DrowZzzRule_When_StartGameActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given(StartGameAction はセッション未生成用、StartGameUseCase 経由で扱うため常に false)
            var rule = NewRule();
            var session = NewSession();
            // When
            var legal = rule.IsLegalMove(session, new StartGameAction());
            // Then
            Assert.That(legal, Is.False);
        }

        // ===== DZ-038 / DZ-039: IsLegalMove(DrawCardAction) の PhaseState 依存 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-038")]
        public void Given_WaitingForDrawフェーズ_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_trueを返す()
        {
            // Given
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw);
            // When
            var legal = rule.IsLegalMove(session, new DrawCardAction());
            // Then
            Assert.That(legal, Is.True);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-039")]
        public void Given_WaitingForPlayフェーズ_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay);
            // When
            var legal = rule.IsLegalMove(session, new DrawCardAction());
            // Then
            Assert.That(legal, Is.False);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-039")]
        public void Given_WaitingForEndTurnフェーズ_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given(WaitingForEndTurn でも DrawCardAction は非合法、3 値 enum の MC/DC 相当カバー)
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForEndTurn);
            // When
            var legal = rule.IsLegalMove(session, new DrawCardAction());
            // Then
            Assert.That(legal, Is.False);
        }

        // ===== DZ-040〜045: Apply(DrawCardAction) 正常系(1 テスト 1 アサーション)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-040")]
        public void Given_合法状態_When_DrawCardActionをApply_Then_現プレイヤーの手札枚数が1増える()
        {
            // Given
            var rule = NewRule();
            var session = NewSession(deck: NewDeck("c1", "c2", "c3"));
            int before = session.GameState.Players[0].Hand.Count;
            // When
            var result = rule.Apply(session, new DrawCardAction());
            // Then
            Assert.That(result.GameState.Players[0].Hand.Count, Is.EqualTo(before + 1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-041")]
        public void Given_合法状態_When_DrawCardActionをApply_Then_山札枚数が1減る()
        {
            // Given
            var rule = NewRule();
            var session = NewSession(deck: NewDeck("c1", "c2", "c3"));
            // When
            var result = rule.Apply(session, new DrawCardAction());
            // Then
            Assert.That(result.GameState.Deck.Count, Is.EqualTo(2));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-042")]
        public void Given_合法状態_When_DrawCardActionをApply_Then_山札Topのカードが現プレイヤーの手札に追加される()
        {
            // Given(山札 Top = c1)
            var rule = NewRule();
            var session = NewSession(deck: NewDeck("c1", "c2", "c3"));
            // When
            var result = rule.Apply(session, new DrawCardAction());
            // Then(現プレイヤー Hand に c1 が含まれる)
            Assert.That(result.GameState.Players[0].Hand.Cards, Has.Member(CardId.Of("c1")));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-043")]
        public void Given_合法状態_When_DrawCardActionをApply_Then_PhaseStateがWaitingForPlayに遷移する()
        {
            // Given
            var rule = NewRule();
            var session = NewSession(deck: NewDeck("c1"));
            // When
            var result = rule.Apply(session, new DrawCardAction());
            // Then
            Assert.That(result.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForPlay));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-044")]
        public void Given_合法状態_When_DrawCardActionをApply_Then_GameStateTurnは不変()
        {
            // Given(Turn = (3, 1))
            var rule = NewRule();
            var session = NewSession(
                deck: NewDeck("c1"),
                currentPlayerIndex: 1,
                turnNumber: 3);
            var originalTurn = session.GameState.Turn;
            // When
            var result = rule.Apply(session, new DrawCardAction());
            // Then
            Assert.That(result.GameState.Turn, Is.EqualTo(originalTurn));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-045")]
        public void Given_合法状態_When_DrawCardActionをApply_Then_他プレイヤーの手札は不変()
        {
            // Given(CurrentPlayerIndex=0、p1.Hand=[a]、p2.Hand=[b])
            var rule = NewRule();
            var p1Hand = new Hand(new[] { CardId.Of("a") });
            var p2Hand = new Hand(new[] { CardId.Of("b") });
            var session = NewSession(
                deck: NewDeck("c1"),
                p0Hand: p1Hand,
                p1Hand: p2Hand);
            // When
            var result = rule.Apply(session, new DrawCardAction());
            // Then(他プレイヤー = Players[1] の Hand は不変)
            Assert.That(result.GameState.Players[1].Hand, Is.EqualTo(p2Hand));
        }

        // ===== DZ-046 / DZ-047: 異常系(IsLegalMove false / 山札枯渇)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-046")]
        public void Given_WaitingForPlayフェーズ_When_DrawCardActionをApply_Then_InvalidOperationExceptionを投げる()
        {
            // Given
            var rule = NewRule();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                deck: NewDeck("c1"));
            // When / Then
            Assert.Throws<InvalidOperationException>(() => rule.Apply(session, new DrawCardAction()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-047")]
        public void Given_山札枯渇_When_DrawCardActionをApply_Then_InvalidOperationExceptionを投げる()
        {
            // Given(WaitingForDraw だが Deck = Pile.Empty)
            var rule = NewRule();
            var session = NewSession(deck: Pile.Empty);
            // When / Then(Pile.Draw が空 Pile で InvalidOperationException を投げる)
            Assert.Throws<InvalidOperationException>(() => rule.Apply(session, new DrawCardAction()));
        }

        // ===== DZ-048〜051: null 検証(M1-PR3 reviewer 申し送り N-7 反映)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-048")]
        public void Given_sessionにnull_When_IsLegalMoveを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            var rule = NewRule();
            Assert.Throws<ArgumentNullException>(() => rule.IsLegalMove(null, new DrawCardAction()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-049")]
        public void Given_actionにnull_When_IsLegalMoveを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            var rule = NewRule();
            var session = NewSession();
            Assert.Throws<ArgumentNullException>(() => rule.IsLegalMove(session, null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-050")]
        public void Given_sessionにnull_When_Applyを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            var rule = NewRule();
            Assert.Throws<ArgumentNullException>(() => rule.Apply(null, new DrawCardAction()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-051")]
        public void Given_actionにnull_When_Applyを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            var rule = NewRule();
            var session = NewSession();
            Assert.Throws<ArgumentNullException>(() => rule.Apply(session, null));
        }

        // ===== DZ-054〜056: IsLegalMove(PlayCardAction) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-054")]
        public void Given_WaitingForPlayかつCardが手札にある_When_PlayCardActionでIsLegalMoveを呼ぶ_Then_trueを返す()
        {
            // Given(WaitingForPlay / 現プレイヤー Hand = [c1, c2])
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1"), CardId.Of("c2") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When
            var legal = rule.IsLegalMove(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(legal, Is.True);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-055")]
        public void Given_WaitingForDraw_When_PlayCardActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw, p0Hand: p0Hand);
            // When
            var legal = rule.IsLegalMove(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(legal, Is.False);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-055")]
        public void Given_WaitingForEndTurn_When_PlayCardActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given(3 値 enum の MC/DC 相当カバー)
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForEndTurn, p0Hand: p0Hand);
            // When
            var legal = rule.IsLegalMove(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(legal, Is.False);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-056")]
        public void Given_WaitingForPlayだがCardが手札にない_When_IsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given(WaitingForPlay だが手札に "cX" がない)
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When
            var legal = rule.IsLegalMove(session, new PlayCardAction(CardId.Of("cX")));
            // Then
            Assert.That(legal, Is.False);
        }

        // ===== DZ-057〜064: Apply(PlayCardAction) 正常系 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-057")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_現プレイヤーHandから指定Cardが除かれる()
        {
            // Given
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1"), CardId.Of("c2") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When
            var result = rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(result.GameState.Players[0].Hand.Cards, Has.No.Member(CardId.Of("c1")));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-058")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_現プレイヤーHandCountが1減る()
        {
            // Given
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1"), CardId.Of("c2") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            int before = session.GameState.Players[0].Hand.Count;
            // When
            var result = rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then(DZ-040 と対称: before - 1 で意図を明示)
            Assert.That(result.GameState.Players[0].Hand.Count, Is.EqualTo(before - 1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-059")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_FieldのTopが指定Card()
        {
            // Given(Field = 空、AddTop で c1 が Field.Cards[0] になる想定)
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When
            var result = rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(result.GameState.Field.Cards[0], Is.EqualTo(CardId.Of("c1")));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-060")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_FieldCountが1増える()
        {
            // Given(Field = 空)
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When
            var result = rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(result.GameState.Field.Count, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-061")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_PhaseStateがWaitingForEndTurnに遷移する()
        {
            // Given
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When
            var result = rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(result.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForEndTurn));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-062")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_GameStateTurnは不変()
        {
            // Given
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                p0Hand: p0Hand,
                currentPlayerIndex: 0,
                turnNumber: 5);
            var originalTurn = session.GameState.Turn;
            // When
            var result = rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(result.GameState.Turn, Is.EqualTo(originalTurn));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-063")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_GameStateDeckは不変()
        {
            // Given(Deck = [d1, d2, d3])
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var deck = NewDeck("d1", "d2", "d3");
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                p0Hand: p0Hand,
                deck: deck);
            // When
            var result = rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(result.GameState.Deck, Is.EqualTo(deck));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-064")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_他プレイヤーの手札は不変()
        {
            // Given(CurrentPlayerIndex=0、p1.Hand=[c1]、p2.Hand=[b])
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var p1Hand = new Hand(new[] { CardId.Of("b") });
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                p0Hand: p0Hand,
                p1Hand: p1Hand);
            // When
            var result = rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(result.GameState.Players[1].Hand, Is.EqualTo(p1Hand));
        }

        // ===== DZ-065 / DZ-066: Apply 異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-065")]
        public void Given_WaitingForDraw_When_PlayCardActionをApply_Then_InvalidOperationExceptionを投げる()
        {
            // Given(PhaseState 違反)
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw, p0Hand: p0Hand);
            // When / Then
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(session, new PlayCardAction(CardId.Of("c1"))));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-066")]
        public void Given_Cardが手札にない_When_PlayCardActionをApply_Then_InvalidOperationExceptionを投げる()
        {
            // Given(WaitingForPlay だが手札に "cX" がない)
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When / Then
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(session, new PlayCardAction(CardId.Of("cX"))));
        }

        // ===== DZ-067 / DZ-068: IsLegalMove(EndTurnAction) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-067")]
        public void Given_WaitingForEndTurn_When_EndTurnActionでIsLegalMoveを呼ぶ_Then_trueを返す()
        {
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForEndTurn);
            var legal = rule.IsLegalMove(session, new EndTurnAction());
            Assert.That(legal, Is.True);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-068")]
        public void Given_WaitingForDraw_When_EndTurnActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw);
            var legal = rule.IsLegalMove(session, new EndTurnAction());
            Assert.That(legal, Is.False);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-068")]
        public void Given_WaitingForPlay_When_EndTurnActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // 3 値 enum の MC/DC 相当カバー
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay);
            var legal = rule.IsLegalMove(session, new EndTurnAction());
            Assert.That(legal, Is.False);
        }

        // ===== DZ-069〜075: Apply(EndTurnAction) 正常系 (1 テスト 1 アサーション) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-069")]
        public void Given_合法状態_When_EndTurnActionをApply_Then_TurnNumberが1増える()
        {
            // Given(Turn = (3, 0))
            var rule = NewRule();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 0,
                turnNumber: 3);
            // When
            var result = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(result.GameState.Turn.TurnNumber, Is.EqualTo(4));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-070")]
        public void Given_合法状態_CurrentPlayer0_When_EndTurnActionをApply_Then_CurrentPlayerIndexが1に進む()
        {
            // Given(N=2、CurrentPlayerIndex=0)
            var rule = NewRule();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 0);
            // When
            var result = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(result.GameState.Turn.CurrentPlayerIndex, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-070")]
        public void Given_合法状態_CurrentPlayer1_When_EndTurnActionをApply_Then_CurrentPlayerIndexが0にラップする()
        {
            // Given(N=2、CurrentPlayerIndex=1、(1+1)%2=0 にラップ)
            var rule = NewRule();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 1);
            // When
            var result = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(result.GameState.Turn.CurrentPlayerIndex, Is.EqualTo(0));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-071")]
        public void Given_合法状態_When_EndTurnActionをApply_Then_PhaseStateがWaitingForDrawに戻る()
        {
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForEndTurn);
            var result = rule.Apply(session, new EndTurnAction());
            Assert.That(result.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForDraw));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-072")]
        public void Given_合法状態_When_EndTurnActionをApply_Then_Players全員のHandは不変()
        {
            // Given(p1.Hand=[a]、p2.Hand=[b])
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("a") });
            var p1Hand = new Hand(new[] { CardId.Of("b") });
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                p0Hand: p0Hand,
                p1Hand: p1Hand);
            // When
            var result = rule.Apply(session, new EndTurnAction());
            // Then(順序付きシーケンスとしての等価)
            Assert.That(
                new[] { result.GameState.Players[0].Hand, result.GameState.Players[1].Hand },
                Is.EqualTo(new[] { p0Hand, p1Hand }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-073")]
        public void Given_合法状態_When_EndTurnActionをApply_Then_Deckは不変()
        {
            // Given
            var rule = NewRule();
            var deck = NewDeck("d1", "d2");
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                deck: deck);
            // When
            var result = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(result.GameState.Deck, Is.EqualTo(deck));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-074")]
        public void Given_合法状態_When_EndTurnActionをApply_Then_Fieldは不変()
        {
            // Given(Field に既に c1 を出してある状態を with で構築する代わりに、PlayCardAction で 1 枚出してから検証)
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var playSession = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            var afterPlay = rule.Apply(playSession, new PlayCardAction(CardId.Of("c1")));
            var fieldBefore = afterPlay.GameState.Field;
            // When(EndTurn を Apply)
            var afterEndTurn = rule.Apply(afterPlay, new EndTurnAction());
            // Then
            Assert.That(afterEndTurn.GameState.Field, Is.EqualTo(fieldBefore));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-075")]
        public void Given_合法状態_When_EndTurnActionをApply_Then_FirstDrowsyPointsは不変()
        {
            // Given(FDP は NewSession のデフォルト {p1: 0, p2: 10})
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForEndTurn);
            var fdpBefore = session.FirstDrowsyPoints;
            // When
            var result = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(result.FirstDrowsyPoints, Is.EquivalentTo(fdpBefore));
        }

        // ===== DZ-076: Apply 異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-076")]
        public void Given_WaitingForDraw_When_EndTurnActionをApply_Then_InvalidOperationExceptionを投げる()
        {
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw);
            Assert.Throws<InvalidOperationException>(() => rule.Apply(session, new EndTurnAction()));
        }

        // ===== DZ-083 / DZ-084: 効果メカニズム (M2-PR1)、ADR-0007 §3 =====

        // spy catalog: GetEffects 呼び出しを記録する
        private sealed class SpyCatalog : ICardCatalog<IEffect>
        {
            private static readonly IReadOnlyList<IEffect> Empty = System.Array.Empty<IEffect>();

            public int GetEffectsCallCount { get; private set; }
            public List<CardId> GetEffectsCalledWith { get; } = new List<CardId>();

            public CardData Get(CardId id) =>
                throw new KeyNotFoundException($"SpyCatalog.Get は本テストでは呼ばれない想定 (id: {id?.Value})");

            public bool TryGet(CardId id, out CardData data)
            {
                data = null;
                return false;
            }

            public IReadOnlyList<IEffect> GetEffects(CardId id)
            {
                GetEffectsCallCount++;
                GetEffectsCalledWith.Add(id);
                return Empty;
            }
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-083")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_catalog_GetEffectsが1回呼ばれる()
        {
            // Given(spy catalog + 標準 EffectInterpreter で DrowZzzRule を組む)
            var spy = new SpyCatalog();
            var rule = new DrowZzzRule(spy, new EffectInterpreter());
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When
            rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then
            Assert.That(spy.GetEffectsCallCount, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-083")]
        public void Given_合法状態_When_PlayCardActionをApply_Then_catalog_GetEffectsが指定Cardで呼ばれる()
        {
            // Given
            var spy = new SpyCatalog();
            var rule = new DrowZzzRule(spy, new EffectInterpreter());
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When
            rule.Apply(session, new PlayCardAction(CardId.Of("c1")));
            // Then(呼び出し引数 CardId が PlayCardAction.Card と一致する)
            Assert.That(spy.GetEffectsCalledWith, Is.EqualTo(new[] { CardId.Of("c1") }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-084")]
        public void Given_効果空のcatalog_When_PlayCardActionをApply_Then_例外を投げずに完走する()
        {
            // Given(空 catalog ならば Interpreter.Apply は呼ばれないため NotImplementedException が出ない)
            var rule = NewRule();
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            // When / Then
            Assert.DoesNotThrow(() => rule.Apply(session, new PlayCardAction(CardId.Of("c1"))));
        }

        // ===== DZ-087 / DZ-088: DrowZzzRule constructor null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-087")]
        public void Given_catalogにnull_When_DrowZzzRule生成_Then_ArgumentNullException_ParamName_catalog_を投げる()
        {
            // Given / When / Then
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = new DrowZzzRule(null, new EffectInterpreter()));
            Assert.That(ex!.ParamName, Is.EqualTo("catalog"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-088")]
        public void Given_interpreterにnull_When_DrowZzzRule生成_Then_ArgumentNullException_ParamName_interpreter_を投げる()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]);
            // When / Then
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _ = new DrowZzzRule(catalog, null));
            Assert.That(ex!.ParamName, Is.EqualTo("interpreter"));
        }

        // ===== DZ-141: Turn 5/9/13/17/21 開始時に N=2 枚抽選 + DrawDrowsyPoints 累積 =====
        // turnNumberBefore は「後手フェーズ完了直前」(後手 CurrentPlayerIndex=1) の TurnNumber。
        // N=2 で Round R の最後フェーズ = 2*R、Round R+1 の最初フェーズ = 2*R + 1。
        // EndTurn 後の CurrentPlayerIndex=0 + 新ターン番号 ∈ {5,9,13,17,21} が抽選トリガー(MC/DC ケース 1)。
        //
        // 複数 Assert.That 並列の採用根拠: DDP 自動抽選という「1 ドメインイベント」の複合不変条件
        // (Round 遷移・プール消費枚数・先手取得値・後手取得値)を 1 メソッド内で検証する。
        // 個別テストに分離すると「DdpPool.Count だけ変化して DrawDrowsyPoints が変化しないバグ」を
        // 見逃す恐れがあるため、ドメインイベント単位での一括検証を採用(CLAUDE.md §6 1 テスト 1 アサーション
        // 原則の「観察可能な 1 アクションの複合結果検証」例外)。
        // ※ Unity NUnit は Assert.Multiple 未対応のため、各 Assert.That を並列に書く(最初の失敗で停止する)。

        [Category("Medium"), Category("Normal"), Property("Requirement", "DZ-141")]
        [TestCase(8, 5, TestName = "Round 4→5 (23:00)")]
        [TestCase(16, 9, TestName = "Round 8→9 (01:00)")]
        [TestCase(24, 13, TestName = "Round 12→13 (03:00)")]
        [TestCase(32, 17, TestName = "Round 16→17 (05:00)")]
        [TestCase(40, 21, TestName = "Round 20→21 (07:00)")]
        public void Given_ターン境界後手EndTurn_When_次がDDP抽選ターン_Then_DdpPool2枚消費_先手後手DDP累積(
            int turnNumberBefore, int expectedRoundAfter)
        {
            // Given(turnNumberBefore = TurnNumber 単位、expectedRoundAfter = Round 単位、N=2 で TurnNumber = 2*Round)
            // DdpPool 先頭 = [10, -20, 残 2 枚はダミー]、後手 EndTurn 待ち
            var rule = NewRule();
            var pool = new DdpPool(new[] { 10, -20, 99, 99 });
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                turnNumber: turnNumberBefore,
                currentPlayerIndex: 1,
                ddpPool: pool);

            // When(後手 EndTurn → 新ターン番号 ∈ {5,9,13,17,21})
            var result = rule.Apply(session, new EndTurnAction());

            // Then(MC/DC ケース 1: ターン境界 + 抽選対象ターン → DdpPool 2 枚消費 + DDP 累積)
            Assert.That(result.Clock.RoundNumber, Is.EqualTo(expectedRoundAfter),
                "Round が R+1 へ進んでいる");
            Assert.That(result.DdpPool.Count, Is.EqualTo(2),
                "DdpPool から N=2 枚消費(残 2 枚)");
            Assert.That(result.DrawDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(10),
                "先手 p1 が DdpPool[0]=10 を取得");
            Assert.That(result.DrawDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-20),
                "後手 p2 が DdpPool[1]=-20 を取得");
        }

        // ===== DZ-142: 抽選対象外ターン {2, 3, 4, 6, 10, ...} 開始時は DDP / DdpPool 不変 =====
        // 複数 Assert.That 並列採用根拠は DZ-141 と同様(1 ドメインイベントの複合不変条件)。

        [Category("Medium"), Category("Normal"), Property("Requirement", "DZ-142")]
        [TestCase(2, 2, TestName = "Round 1→2 (対象外)")]
        [TestCase(4, 3, TestName = "Round 2→3 (対象外)")]
        [TestCase(6, 4, TestName = "Round 3→4 (対象外)")]
        [TestCase(10, 6, TestName = "Round 5→6 (対象外)")]
        [TestCase(18, 10, TestName = "Round 9→10 (対象外)")]
        public void Given_ターン境界後手EndTurn_When_次が抽選対象外ターン_Then_DdpPoolもDDPも不変(
            int turnNumberBefore, int expectedRoundAfter)
        {
            // Given(turnNumberBefore = TurnNumber 単位、N=2 で TurnNumber = 2*Round)
            // DdpPool 先頭 = [10, -20, ...]、後手 EndTurn 待ち
            var rule = NewRule();
            var pool = new DdpPool(new[] { 10, -20, 99, 99 });
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                turnNumber: turnNumberBefore,
                currentPlayerIndex: 1,
                ddpPool: pool);

            // When(後手 EndTurn → 新ターン番号 ∉ {5,9,13,17,21})
            var result = rule.Apply(session, new EndTurnAction());

            // Then(MC/DC ケース 4: ターン境界だが抽選対象外 → DdpPool / DDP 共に不変)
            Assert.That(result.Clock.RoundNumber, Is.EqualTo(expectedRoundAfter),
                "Round が R+1 へ進んでいる");
            Assert.That(result.DdpPool, Is.EqualTo(pool),
                "DdpPool は不変(4 枚保持)");
            Assert.That(result.DrawDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0),
                "先手 p1 の DDP は 0 のまま");
            Assert.That(result.DrawDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0),
                "後手 p2 の DDP は 0 のまま");
        }

        // ===== DZ-143: ターン境界以外(先手 EndTurn だけ)では DDP 抽選を行わない =====
        // Round 5 の先手フェーズ(TurnNumber=9, CurrentPlayerIndex=0)で EndTurn → 後手フェーズへ進む
        // (CurrentPlayerIndex 0 → 1)が、ターン境界ではないため抽選トリガーは発火しない。
        // ※ 既存テスト DZ-070 / DZ-071 と異なり、新ターン番号が DrawRounds に含まれる場合でも抽選が行われないことを表明。
        // 複数 Assert.That 並列採用根拠は DZ-141 と同様。

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-143")]
        public void Given_Round5先手フェーズEndTurn_When_CurrentPlayerIndex0から1へ_Then_DdpPoolもDDPも不変()
        {
            // Given(Round 5 の先手フェーズ TurnNumber=9 / CurrentPlayerIndex=0、Round 5 は DrawRounds 対象)
            var rule = NewRule();
            var pool = new DdpPool(new[] { 10, -20, 99, 99 });
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                turnNumber: 9,
                currentPlayerIndex: 0,
                ddpPool: pool);

            // When(先手 EndTurn → CurrentPlayerIndex 0 → 1、まだ Round 5 内、ターン境界ではない)
            var result = rule.Apply(session, new EndTurnAction());

            // Then(MC/DC ケース 3: ターン境界ではない → 抽選トリガー不発)
            Assert.That(result.GameState.Turn.CurrentPlayerIndex, Is.EqualTo(1),
                "CurrentPlayerIndex が 0 → 1 に進む");
            Assert.That(result.DdpPool, Is.EqualTo(pool),
                "DdpPool は不変(Round 5 開始の抽選は後手 EndTurn まで保留)");
            Assert.That(result.DrawDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0),
                "先手 p1 の DDP は 0 のまま");
            Assert.That(result.DrawDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0),
                "後手 p2 の DDP は 0 のまま");
        }

        // ===== DZ-144: 複数回 DDP 抽選で DrawDrowsyPoints が累積される =====
        // Round 4→5 で 1 回目(先手 +5 / 後手 -10)、Round 8→9 で 2 回目(先手 +20 / 後手 +25)を、
        // 2 回目の事前状態を直接構築する形でシミュレートし、累積結果を検証する。
        // 複数 Assert.That 並列採用根拠は DZ-141 と同様。

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-144")]
        public void Given_1回目抽選済の状態_When_2回目DDP抽選_Then_DrawDrowsyPointsが累積される()
        {
            // Given(1 回目 Round 5 で +5 / -10 を取得済の状態、Round 8→9 直前を再現)
            var rule = NewRule();
            var poolBefore2nd = new DdpPool(new[] { 20, 25 });  // 2 回目抽選用に残 2 枚
            var ddpAfter1st = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 5,    // Round 5 1 回目で +5 取得済
                [PlayerId.Of("p2")] = -10,  // Round 5 1 回目で -10 取得済
            };
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                turnNumber: 16,             // Round 8 後手フェーズ (CurrentPlayerIndex=1)
                currentPlayerIndex: 1,
                ddpPool: poolBefore2nd,
                ddp: ddpAfter1st);

            // When(後手 EndTurn → Round 9 = 2 回目の DDP 抽選トリガー)
            var result = rule.Apply(session, new EndTurnAction());

            // Then(累積: p1 = 5 + 20 = 25、p2 = -10 + 25 = 15)
            Assert.That(result.DrawDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(25),
                "先手 p1 の DDP = 5 + 20 = 25");
            Assert.That(result.DrawDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(15),
                "後手 p2 の DDP = -10 + 25 = 15");
            Assert.That(result.DdpPool.IsEmpty, Is.True,
                "2 回目で残 2 枚全て消費、空 Pool");
        }
    }
}
