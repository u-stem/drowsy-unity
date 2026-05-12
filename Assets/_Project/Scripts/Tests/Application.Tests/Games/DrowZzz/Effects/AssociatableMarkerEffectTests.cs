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
    /// <see cref="AssociatableMarkerEffect"/> を <see cref="EffectInterpreter.Apply"/> で評価した時の
    /// no-op 挙動を検証する(DZ-208)。ADR-0011 §1 / M3-PR4 で導入。
    /// </summary>
    [TestFixture]
    public sealed class AssociatableMarkerEffectTests
    {
        // ===== ヘルパー =====

        private static DrowZzzGameSession NewSession(
            int turnNumber = 1,
            int fdpP1 = 0,
            int sdpP1 = 0,
            int bedP1 = 0,
            GameOutcome outcome = null)
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, Pile.Empty,
                new TurnState(turnNumber, 0));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = fdpP1, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = sdpP1, [PlayerId.Of("p2")] = 0 };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            var bed = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = bedP1,
                [PlayerId.Of("p2")] = 0,
            };
            return new DrowZzzGameSession(
                gs, fdp, ddp, sdp, DdpPool.Empty, influences, DrowZzzPhaseState.WaitingForPlay,
                outcome: outcome, bedDamages: bed);
        }

        // ===== DZ-208: AssociatableMarkerEffect は no-op(session 不変返却)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-208")]
        public void Given_デフォルトセッション_When_AssociatableMarkerEffectをApply_Then_session不変()
        {
            var interpreter = new EffectInterpreter();
            var session = NewSession();
            var next = interpreter.Apply(session, new AssociatableMarkerEffect());
            // 値同値で完全一致(マーカーは効果を持たない、ADR-0011 §1)
            Assert.That(next, Is.EqualTo(session));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-208")]
        public void Given_FDP100_SDP10_ベッド40_When_AssociatableMarkerEffectをApply_Then_session不変()
        {
            // 多様な状態でも no-op であることを確認
            var interpreter = new EffectInterpreter();
            var session = NewSession(fdpP1: 100, sdpP1: 10, bedP1: 40);
            var next = interpreter.Apply(session, new AssociatableMarkerEffect());
            Assert.That(next, Is.EqualTo(session));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-208")]
        public void Given_Outcome設定済_When_AssociatableMarkerEffectをApply_Then_session不変()
        {
            // Outcome が既に WinnerOutcome の session に対しても no-op
            // (本 effect は marker、終了済 session 上書きや状態遷移を起こさない)
            var interpreter = new EffectInterpreter();
            var session = NewSession(outcome: new WinnerOutcome(PlayerId.Of("p1")));
            var next = interpreter.Apply(session, new AssociatableMarkerEffect());
            Assert.That(next, Is.EqualTo(session));
        }

        // ===== AssociatableMarkerEffect の record 同値性(reference identity ではなく value identity)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-207")]
        public void Given_2つの異なるインスタンス_When_等値比較_Then_true()
        {
            // フィールドなし record は全インスタンスが値同値
            var a = new AssociatableMarkerEffect();
            var b = new AssociatableMarkerEffect();
            Assert.That(a, Is.EqualTo(b));
        }
    }
}
