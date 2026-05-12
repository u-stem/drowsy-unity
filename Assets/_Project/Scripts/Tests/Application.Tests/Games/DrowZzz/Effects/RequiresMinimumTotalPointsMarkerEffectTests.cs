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
    /// <see cref="RequiresMinimumTotalPointsMarkerEffect"/> の構造防御 / record 値同値性 /
    /// <see cref="EffectInterpreter.Apply"/> no-op 挙動を検証する(DZ-240 / DZ-241 / DZ-242)。
    /// ADR-0011 §6 / M3-PR6 で導入。
    /// </summary>
    [TestFixture]
    public sealed class RequiresMinimumTotalPointsMarkerEffectTests
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

        // ===== DZ-240: 構築防御(0 / 負値で ArgumentOutOfRangeException)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-240")]
        public void Given_Threshold0_When_コンストラクト_Then_ArgumentOutOfRangeException()
        {
            // Given/When/Then(0 は使用条件として無意味、本 record は 1 以上を要求)
            Assert.That(
                () => new RequiresMinimumTotalPointsMarkerEffect(0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-240")]
        public void Given_Threshold負値_When_コンストラクト_Then_ArgumentOutOfRangeException()
        {
            // Given/When/Then(負値も使用条件として無意味)
            Assert.That(
                () => new RequiresMinimumTotalPointsMarkerEffect(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-240")]
        public void Given_有効インスタンス_When_withで0に上書き_Then_ArgumentOutOfRangeException()
        {
            // Given(Threshold=100 の有効インスタンス)
            var valid = new RequiresMinimumTotalPointsMarkerEffect(100);
            // When/Then(with 式経由でも防御、二重ガード)
            Assert.That(
                () => valid with { Threshold = 0 },
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        // ===== DZ-241: record 値同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-241")]
        public void Given_同一Thresholdの2インスタンス_When_等値比較_Then_true()
        {
            // Given(同じ Threshold=100 の 2 インスタンス)
            var a = new RequiresMinimumTotalPointsMarkerEffect(100);
            var b = new RequiresMinimumTotalPointsMarkerEffect(100);
            // When/Then(record 値同値性で同値)
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-241")]
        public void Given_異なるThreshold_When_等値比較_Then_false()
        {
            // Given(Threshold=100 と Threshold=80)
            var a = new RequiresMinimumTotalPointsMarkerEffect(100);
            var b = new RequiresMinimumTotalPointsMarkerEffect(80);
            // When/Then(値が異なるため非同値)
            Assert.That(a, Is.Not.EqualTo(b));
        }

        // ===== DZ-242: EffectInterpreter で no-op =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-242")]
        public void Given_任意session_When_RequiresMinimumTotalPointsMarkerEffectをApply_Then_session不変()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var session = NewSession();
            // When
            var next = interpreter.Apply(session, new RequiresMinimumTotalPointsMarkerEffect(100));
            // Then(値同値、マーカーは状態を変えない)
            Assert.That(next, Is.EqualTo(session));
        }
    }
}
