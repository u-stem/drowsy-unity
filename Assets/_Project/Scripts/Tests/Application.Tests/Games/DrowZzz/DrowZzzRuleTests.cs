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
        // ===== ヘルパー =====

        private static DrowZzzGameSession NewSession()
        {
            var gs = new GameState(
                new[]
                {
                    new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                    new PlayerState(PlayerId.Of("p2"), Hand.Empty),
                },
                Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                TurnState.Initial(0));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 10,
            };
            return new DrowZzzGameSession(gs, fdp, DrowZzzTurnPhase.WaitingForDraw);
        }

        // ===== DZ-012 / DZ-013: M1-PR3 段階の NotImplementedException(non-StartGameAction) =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-012")]
        public void Given_DrowZzzRule_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_NotImplementedExceptionを投げる()
        {
            // Given(DrawCardAction は M1-PR4 で本格実装、M1-PR3 では NotImpl)
            var rule = new DrowZzzRule();
            var session = NewSession();
            var action = new DrawCardAction();
            // When / Then
            Assert.Throws<NotImplementedException>(() => rule.IsLegalMove(session, action));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-013")]
        public void Given_DrowZzzRule_When_Applyを呼ぶ_Then_NotImplementedExceptionを投げる()
        {
            // Given
            var rule = new DrowZzzRule();
            var session = NewSession();
            var action = new DrawCardAction();
            // When / Then
            Assert.Throws<NotImplementedException>(() => rule.Apply(session, action));
        }

        // ===== DZ-034: StartGameAction → false (M1-PR3 で追加) =====

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-034")]
        public void Given_DrowZzzRule_When_StartGameActionでIsLegalMoveを呼ぶ_Then_falseを返す()
        {
            // Given(StartGameAction はセッション未生成用、StartGameUseCase 経由で扱うため常に false)
            var rule = new DrowZzzRule();
            var session = NewSession();
            var action = new StartGameAction();
            // When
            var legal = rule.IsLegalMove(session, action);
            // Then
            Assert.That(legal, Is.False);
        }
    }
}
