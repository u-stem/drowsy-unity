using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="RemoveInfluenceEffect"/> を <see cref="EffectInterpreter.Apply"/> で評価した時の
    /// 影響除去挙動を検証する(DZ-162 / DZ-163 / DZ-164 / DZ-165)。
    /// </summary>
    [TestFixture]
    public sealed class RemoveInfluenceEffectTests
    {
        // ===== ヘルパー =====

        private static PlayerInfluence Inf(int count = 3, int delta = -5) =>
            new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, delta), count);

        private static DrowZzzGameSession NewSession(
            int currentPlayerIndex = 0,
            IReadOnlyList<PlayerInfluence> p1Influences = null,
            IReadOnlyList<PlayerInfluence> p2Influences = null)
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, Pile.Empty,
                new TurnState(1, currentPlayerIndex));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = p1Influences ?? Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = p2Influences ?? Array.Empty<PlayerInfluence>(),
            };
            return new DrowZzzGameSession(gs, fdp, ddp, sdp, DdpPool.Empty, influences, DrowZzzPhaseState.WaitingForPlay, outcome: null, bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 });
        }

        // ===== DZ-162: index 範囲内で影響を除去 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-162")]
        public void Given_p1current_p1に2件_index0_When_Apply_Then_p1のInfluences件数が1に減る()
        {
            // Given(p1 が current、p1 は 2 件保有)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, p1Influences: new[] { Inf(3), Inf(2) });
            // When(context.InfluenceRemovalIndex = 0)
            var next = interpreter.Apply(session, new RemoveInfluenceEffect(SdpTarget.Self), new EffectContext(0));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-162")]
        public void Given_p1current_p1に2件_index0_When_Apply_Then_残ったのはindex1だった影響()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var keep = Inf(2);  // 2 番目(残るべき)
            var session = NewSession(currentPlayerIndex: 0, p1Influences: new[] { Inf(3), keep });
            // When
            var next = interpreter.Apply(session, new RemoveInfluenceEffect(SdpTarget.Self), new EffectContext(0));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p1")][0], Is.EqualTo(keep));
        }

        // ===== DZ-163: index 範囲外で no-op(graceful)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-163")]
        public void Given_p1空list_index0_When_Apply_Then_session不変返却()
        {
            // Given(p1 が current、空 list)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0);
            // When
            var next = interpreter.Apply(session, new RemoveInfluenceEffect(SdpTarget.Self), new EffectContext(0));
            // Then(session 参照そのまま、graceful no-op)
            Assert.That(next, Is.SameAs(session));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-163")]
        public void Given_p1に1件_index1範囲外_When_Apply_Then_p1のInfluences件数は1のまま()
        {
            // Given(p1 が current、1 件保有、index=1 は範囲外)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, p1Influences: new[] { Inf(3) });
            // When
            var next = interpreter.Apply(session, new RemoveInfluenceEffect(SdpTarget.Self), new EffectContext(1));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-163")]
        public void Given_p1に1件_indexマイナス1_When_Apply_Then_session不変返却()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, p1Influences: new[] { Inf(3) });
            // When
            var next = interpreter.Apply(session, new RemoveInfluenceEffect(SdpTarget.Self), new EffectContext(-1));
            // Then(範囲外: session 不変返却)
            Assert.That(next, Is.SameAs(session));
        }

        // ===== DZ-164: Target=Opponent で相手の list から除去 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-164")]
        public void Given_p1current_p2に1件_TargetOpponent_index0_When_Apply_Then_p2のInfluences件数が0()
        {
            // Given(p1 が current、p2 が 1 件保有)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, p2Influences: new[] { Inf(3) });
            // When
            var next = interpreter.Apply(session, new RemoveInfluenceEffect(SdpTarget.Opponent), new EffectContext(0));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(0));
        }

        // ===== DZ-165: 他プレイヤーの影響は不変 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-165")]
        public void Given_p1current_p1とp2に各1件_TargetSelf_index0_When_Apply_Then_p2のInfluencesは不変()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var p2Inf = Inf(2);
            var session = NewSession(currentPlayerIndex: 0,
                p1Influences: new[] { Inf(3) },
                p2Influences: new[] { p2Inf });
            // When
            var next = interpreter.Apply(session, new RemoveInfluenceEffect(SdpTarget.Self), new EffectContext(0));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p2")][0], Is.EqualTo(p2Inf));
        }
    }
}
