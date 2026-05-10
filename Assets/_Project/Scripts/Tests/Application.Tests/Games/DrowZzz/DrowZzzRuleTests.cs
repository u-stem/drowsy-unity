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

        // ===== DZ-012 / DZ-013: skeleton 段階の NotImplementedException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-012")]
        public void Given_DrowZzzRule_When_IsLegalMoveを呼ぶ_Then_NotImplementedExceptionを投げる()
        {
            // Given
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
    }
}
