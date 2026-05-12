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
    /// <see cref="TimeOfDayBranchEffect"/> の Clock 分岐評価を検証する
    /// (DZ-120 / DZ-121 / DZ-122 / DZ-123 / DZ-124)。
    /// </summary>
    [TestFixture]
    public sealed class TimeOfDayBranchEffectTests
    {
        // ===== ヘルパー =====

        // Clock の RoundNumber を制御するため TurnState.TurnNumber を直接指定可能なセッションヘルパー。
        // TurnNumber = 2*(round-1) + 1 が round の現プレイヤーフェーズ(N=2)。
        private static DrowZzzGameSession NewSession(int turnNumber, int sdpP1 = 0)
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players,
                Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                new TurnState(turnNumber, 0));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            var sdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = sdpP1,
                [PlayerId.Of("p2")] = 0,
            };
            // DDP / DdpPool は M2-PR4 で追加。本 fixture は Clock 分岐評価テスト目的のため DDP=0 / 空 DdpPool で固定。
            var ddp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // M2-PR5: Influences は本 fixture では空 list 固定
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            return new DrowZzzGameSession(gs, fdp, ddp, sdp, DdpPool.Empty, influences, DrowZzzPhaseState.WaitingForPlay, outcome: null, bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 }, System.Array.Empty<PendingCounteredEffect>());
        }

        // ===== DZ-120: 夜で NightEffects 評価 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-120")]
        public void Given_夜のClock_NightEffectsSdpPlus10_When_Apply_Then_SDPが10増加する()
        {
            // Given(TurnNumber=1 → Round=1、夜)
            var interpreter = new EffectInterpreter();
            var session = NewSession(turnNumber: 1, sdpP1: 0);
            var effect = new TimeOfDayBranchEffect(
                nightEffects: new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) },
                morningEffects: Array.Empty<IEffect>());
            // When
            var next = interpreter.Apply(session, effect);
            // Then(夜のため NightEffects = [AdjustSdpEffect(Self, +10)] が評価される)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(10));
        }

        // ===== DZ-121: 朝で MorningEffects 評価 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-121")]
        public void Given_朝のClock_MorningEffectsSdpPlus10_When_Apply_Then_SDPが10増加する()
        {
            // Given(TurnNumber=33 → Round=17、朝)
            var interpreter = new EffectInterpreter();
            var session = NewSession(turnNumber: 33, sdpP1: 0);
            var effect = new TimeOfDayBranchEffect(
                nightEffects: Array.Empty<IEffect>(),
                morningEffects: new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) });
            // When
            var next = interpreter.Apply(session, effect);
            // Then(朝のため MorningEffects が評価される)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(10));
        }

        // ===== DZ-122: Round 22+ で no-op =====

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-122")]
        public void Given_Round22のClock_When_Apply_Then_sessionが変化しない()
        {
            // Given(TurnNumber=43 → Round=22、IsNight=IsMorning=false の過渡的範囲、ADR-0008 §5)
            var interpreter = new EffectInterpreter();
            var session = NewSession(turnNumber: 43, sdpP1: 0);
            var effect = new TimeOfDayBranchEffect(
                nightEffects: new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) },
                morningEffects: new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 20) });
            // When
            var next = interpreter.Apply(session, effect);
            // Then(どちらの効果列も評価されない、session 同等)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0));
        }

        // ===== DZ-123 / DZ-124: null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-123")]
        public void Given_NightEffectsにnull_When_生成_Then_ArgumentNullExceptionを投げる()
        {
            // When / Then
            Assert.Throws<ArgumentNullException>(() =>
                new TimeOfDayBranchEffect(null, Array.Empty<IEffect>()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-124")]
        public void Given_MorningEffectsにnull_When_生成_Then_ArgumentNullExceptionを投げる()
        {
            // When / Then
            Assert.Throws<ArgumentNullException>(() =>
                new TimeOfDayBranchEffect(Array.Empty<IEffect>(), null));
        }

        // ===== DZ-123 補足: list 内 null 要素を構築時に防御(code-reviewer W-2 反映)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-123")]
        public void Given_NightEffectsにnull要素を含む_When_生成_Then_ArgumentExceptionを投げる()
        {
            // Given(list 内に null 要素、他 record の `ValidateAndCopyDp` パターンと整合する構築時検証)
            var withNullElem = new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 5), null };
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                new TimeOfDayBranchEffect(withNullElem, Array.Empty<IEffect>()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-124")]
        public void Given_MorningEffectsにnull要素を含む_When_生成_Then_ArgumentExceptionを投げる()
        {
            // Given
            var withNullElem = new IEffect[] { null };
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                new TimeOfDayBranchEffect(Array.Empty<IEffect>(), withNullElem));
        }
    }
}
