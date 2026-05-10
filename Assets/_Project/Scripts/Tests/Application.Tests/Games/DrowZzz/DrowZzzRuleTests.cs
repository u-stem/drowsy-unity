using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    [TestFixture]
    public class DrowZzzRuleTests
    {
        // ===== ヘルパー(全引数オプション、デフォルトは N=2 / WaitingForDraw / 空 Deck / 空 Hand)=====

        private static DrowZzzGameSession NewSession(
            DrowZzzTurnPhase phase = DrowZzzTurnPhase.WaitingForDraw,
            int currentPlayerIndex = 0,
            Pile deck = null,
            Hand p0Hand = null,
            Hand p1Hand = null,
            int turnNumber = 1)
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
            return new DrowZzzGameSession(gs, fdp, phase);
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

        // ===== DZ-012 / DZ-013: M1-PR3 段階の NotImplementedException(non-StartGameAction)
        //       本 PR (M1-PR4) で DrawCardAction は実装済になったため、PlayCardAction で代用 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-012")]
        public void Given_DrowZzzRule_When_PlayCardActionでIsLegalMoveを呼ぶ_Then_NotImplementedExceptionを投げる()
        {
            // Given(PlayCardAction は M1-PR5 で本格実装、M1-PR4 では NotImpl)
            var rule = new DrowZzzRule();
            var session = NewSession();
            var action = new PlayCardAction(CardId.Of("x"));
            // When / Then
            Assert.Throws<NotImplementedException>(() => rule.IsLegalMove(session, action));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-013")]
        public void Given_DrowZzzRule_When_PlayCardActionでApplyを呼ぶ_Then_NotImplementedExceptionを投げる()
        {
            // Given(PlayCardAction Apply は M1-PR5 で本格実装、M1-PR4 では NotImpl)
            var rule = new DrowZzzRule();
            var session = NewSession();
            var action = new PlayCardAction(CardId.Of("x"));
            // When / Then
            Assert.Throws<NotImplementedException>(() => rule.Apply(session, action));
        }

        // ===== DZ-034: StartGameAction → false (M1-PR3) =====

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-034")]
        public void Given_DrowZzzRule_When_StartGameActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given(StartGameAction はセッション未生成用、StartGameUseCase 経由で扱うため常に false)
            var rule = new DrowZzzRule();
            var session = NewSession();
            // When
            var legal = rule.IsLegalMove(session, new StartGameAction());
            // Then
            Assert.That(legal, Is.False);
        }

        // ===== DZ-038 / DZ-039: IsLegalMove(DrawCardAction) の TurnPhase 依存 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-038")]
        public void Given_WaitingForDrawフェーズ_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_trueを返す()
        {
            // Given
            var rule = new DrowZzzRule();
            var session = NewSession(phase: DrowZzzTurnPhase.WaitingForDraw);
            // When
            var legal = rule.IsLegalMove(session, new DrawCardAction());
            // Then
            Assert.That(legal, Is.True);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-039")]
        public void Given_WaitingForPlayフェーズ_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given
            var rule = new DrowZzzRule();
            var session = NewSession(phase: DrowZzzTurnPhase.WaitingForPlay);
            // When
            var legal = rule.IsLegalMove(session, new DrawCardAction());
            // Then
            Assert.That(legal, Is.False);
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-039")]
        public void Given_WaitingForEndTurnフェーズ_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given(WaitingForEndTurn でも DrawCardAction は非合法、3 値 enum の MC/DC 相当カバー)
            var rule = new DrowZzzRule();
            var session = NewSession(phase: DrowZzzTurnPhase.WaitingForEndTurn);
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
            var rule = new DrowZzzRule();
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
            var rule = new DrowZzzRule();
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
            var rule = new DrowZzzRule();
            var session = NewSession(deck: NewDeck("c1", "c2", "c3"));
            // When
            var result = rule.Apply(session, new DrawCardAction());
            // Then(現プレイヤー Hand に c1 が含まれる)
            Assert.That(result.GameState.Players[0].Hand.Cards, Has.Member(CardId.Of("c1")));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-043")]
        public void Given_合法状態_When_DrawCardActionをApply_Then_TurnPhaseがWaitingForPlayに遷移する()
        {
            // Given
            var rule = new DrowZzzRule();
            var session = NewSession(deck: NewDeck("c1"));
            // When
            var result = rule.Apply(session, new DrawCardAction());
            // Then
            Assert.That(result.TurnPhase, Is.EqualTo(DrowZzzTurnPhase.WaitingForPlay));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-044")]
        public void Given_合法状態_When_DrawCardActionをApply_Then_GameStateTurnは不変()
        {
            // Given(Turn = (3, 1))
            var rule = new DrowZzzRule();
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
            var rule = new DrowZzzRule();
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
            var rule = new DrowZzzRule();
            var session = NewSession(
                phase: DrowZzzTurnPhase.WaitingForPlay,
                deck: NewDeck("c1"));
            // When / Then
            Assert.Throws<InvalidOperationException>(() => rule.Apply(session, new DrawCardAction()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-047")]
        public void Given_山札枯渇_When_DrawCardActionをApply_Then_InvalidOperationExceptionを投げる()
        {
            // Given(WaitingForDraw だが Deck = Pile.Empty)
            var rule = new DrowZzzRule();
            var session = NewSession(deck: Pile.Empty);
            // When / Then(Pile.Draw が空 Pile で InvalidOperationException を投げる)
            Assert.Throws<InvalidOperationException>(() => rule.Apply(session, new DrawCardAction()));
        }

        // ===== DZ-048〜051: null 検証(M1-PR3 reviewer 申し送り N-7 反映)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-048")]
        public void Given_sessionにnull_When_IsLegalMoveを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            var rule = new DrowZzzRule();
            Assert.Throws<ArgumentNullException>(() => rule.IsLegalMove(null, new DrawCardAction()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-049")]
        public void Given_actionにnull_When_IsLegalMoveを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            var rule = new DrowZzzRule();
            var session = NewSession();
            Assert.Throws<ArgumentNullException>(() => rule.IsLegalMove(session, null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-050")]
        public void Given_sessionにnull_When_Applyを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            var rule = new DrowZzzRule();
            Assert.Throws<ArgumentNullException>(() => rule.Apply(null, new DrawCardAction()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-051")]
        public void Given_actionにnull_When_Applyを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            var rule = new DrowZzzRule();
            var session = NewSession();
            Assert.Throws<ArgumentNullException>(() => rule.Apply(session, null));
        }
    }
}
