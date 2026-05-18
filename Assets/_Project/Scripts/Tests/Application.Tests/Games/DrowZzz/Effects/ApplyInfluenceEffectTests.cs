using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="ApplyInfluenceEffect"/> を <see cref="EffectInterpreter.Apply"/> で評価した時の
    /// Influences 更新挙動を検証する(DZ-158 / DZ-159 / DZ-160 / DZ-161)。
    /// </summary>
    [TestFixture]
    public sealed class ApplyInfluenceEffectTests
    {
        // ===== ヘルパー =====

        private static IEffect Tick(int delta = -5) => new AdjustSdpEffect(SdpTarget.Self, delta);

        private static PlayerInfluence Inf(int count = 3, int delta = -5) =>
            new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, Tick(delta), count);

        private static DrowZzzGameSession NewSession(
            int currentPlayerIndex = 0,
            IReadOnlyList<PlayerInfluence> p1Influences = null,
            IReadOnlyList<PlayerInfluence> p2Influences = null)
        {
            var influences = p1Influences == null && p2Influences == null
                ? null
                : new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = p1Influences ?? Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = p2Influences ?? Array.Empty<PlayerInfluence>(),
                };
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: currentPlayerIndex,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
        }

        // ===== DZ-158: 構築 / null 防御 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-158")]
        public void Given_有効な引数_When_ApplyInfluenceEffectを生成_Then_Targetが入力と一致する()
        {
            var effect = new ApplyInfluenceEffect(SdpTarget.Opponent, Inf());
            Assert.That(effect.Target, Is.EqualTo(SdpTarget.Opponent));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-158")]
        public void Given_Influenceにnull_When_ApplyInfluenceEffectを生成_Then_ArgumentNullExceptionを投げる()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ApplyInfluenceEffect(SdpTarget.Self, null));
            Assert.That(ex!.ParamName, Is.EqualTo("Influence"));
        }

        // ===== DZ-159: Target=Self で現プレイヤーの list 末尾に追加 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-159")]
        public void Given_p1current_TargetSelf_When_Apply_Then_p1のInfluencesに1件追加される()
        {
            // Given(p1 が current)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0);
            // When
            var next = interpreter.Apply(session, new ApplyInfluenceEffect(SdpTarget.Self, Inf()));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-159")]
        public void Given_p1current_TargetSelf_When_Apply_Then_p2のInfluencesは不変()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0);
            // When
            var next = interpreter.Apply(session, new ApplyInfluenceEffect(SdpTarget.Self, Inf()));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(0));
        }

        // ===== DZ-160: 重複付与で末尾に追加(FIFO 規約)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-160")]
        public void Given_既に1件保有_When_同種類影響を更にApply_Then_2件保有になる()
        {
            // Given(p1 が既に 1 件保有)
            var interpreter = new EffectInterpreter();
            var existing = Inf(count: 3, delta: -5);
            var session = NewSession(currentPlayerIndex: 0, p1Influences: new[] { existing });
            // When
            var next = interpreter.Apply(session, new ApplyInfluenceEffect(SdpTarget.Self, Inf(count: 2, delta: -5)));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(2));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-160")]
        public void Given_既に1件保有_When_更にApply_Then_新規影響は末尾index1に配置される()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var existing = Inf(count: 3, delta: -5);
            var newOne = Inf(count: 2, delta: -5);
            var session = NewSession(currentPlayerIndex: 0, p1Influences: new[] { existing });
            // When
            var next = interpreter.Apply(session, new ApplyInfluenceEffect(SdpTarget.Self, newOne));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p1")][1], Is.EqualTo(newOne));
        }

        // ===== DZ-161: Target=Opponent で相手の list に追加 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-161")]
        public void Given_p1current_TargetOpponent_When_Apply_Then_p2のInfluencesに1件追加される()
        {
            // Given(p1 が current、Opponent = p2)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0);
            // When
            var next = interpreter.Apply(session, new ApplyInfluenceEffect(SdpTarget.Opponent, Inf()));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
        }
    }
}
