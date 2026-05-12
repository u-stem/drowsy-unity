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
    /// <see cref="UsageRestrictionMarkerEffect"/> の record 値同値性 /
    /// <see cref="EffectInterpreter.Apply"/> no-op 挙動を検証する(DZ-244 / DZ-245)。
    /// ADR-0011 §6 / M3-PR6 で導入。2 役兼用 marker の単体挙動を確認(統合動作は DreamCardTests でカバー)。
    /// </summary>
    [TestFixture]
    public sealed class UsageRestrictionMarkerEffectTests
    {
        // ===== ヘルパー(AssociatableMarkerEffectTests と同パターン) =====

        private static DrowZzzGameSession NewSession()
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, Pile.Empty,
                new TurnState(1, 0));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            var bed = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            return new DrowZzzGameSession(
                gs, fdp, ddp, sdp, DdpPool.Empty, influences, DrowZzzPhaseState.WaitingForPlay,
                outcome: null, bedDamages: bed, Array.Empty<PendingCounteredEffect>());
        }

        // ===== DZ-244: record 値同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-244")]
        public void Given_2つの異なるインスタンス_When_等値比較_Then_true()
        {
            // Given(フィールドなし record は全インスタンスが値同値)
            var a = new UsageRestrictionMarkerEffect();
            var b = new UsageRestrictionMarkerEffect();
            // When/Then
            Assert.That(a, Is.EqualTo(b));
        }

        // ===== DZ-245: EffectInterpreter で no-op(直接 / Tick 経由ともに同じ挙動)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-245")]
        public void Given_任意session_When_UsageRestrictionMarkerEffectをApply_Then_session不変()
        {
            // Given(直接 Apply パス:カードの効果列内マーカーとして evaluator から呼ばれた場合)
            var interpreter = new EffectInterpreter();
            var session = NewSession();
            // When
            var next = interpreter.Apply(session, new UsageRestrictionMarkerEffect());
            // Then(値同値、Tick 時の RemainingCount 減算は DrowZzzRule.TickInfluences の責務、本 effect は session を変えない)
            Assert.That(next, Is.EqualTo(session));
        }
    }
}
