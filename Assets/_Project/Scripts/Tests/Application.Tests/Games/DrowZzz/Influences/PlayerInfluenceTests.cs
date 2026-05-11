using System;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;

namespace Drowsy.Application.Tests.Games.DrowZzz.Influences
{
    /// <summary>
    /// <see cref="PlayerInfluence"/> の不変条件 / 値同値性を検証する(DZ-155 / DZ-156 / DZ-157)。
    /// </summary>
    [TestFixture]
    public sealed class PlayerInfluenceTests
    {
        // ===== ヘルパー =====

        private static IEffect Tick() => new AdjustSdpEffect(SdpTarget.Self, -5);

        // ===== DZ-155: 構築の正常系 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-155")]
        public void Given_有効な引数_When_PlayerInfluenceを生成_Then_Triggerが入力と一致する()
        {
            // Given / When
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, Tick(), 3);
            // Then
            Assert.That(inf.Trigger, Is.EqualTo(InfluenceTrigger.OwnPhaseStart));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-155")]
        public void Given_有効な引数_When_PlayerInfluenceを生成_Then_TickEffectが入力と一致する()
        {
            // Given
            var tick = Tick();
            // When
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, tick, 3);
            // Then
            Assert.That(inf.TickEffect, Is.SameAs(tick));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-155")]
        public void Given_有効な引数_When_PlayerInfluenceを生成_Then_RemainingCountが入力と一致する()
        {
            // When
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, Tick(), 3);
            // Then
            Assert.That(inf.RemainingCount, Is.EqualTo(3));
        }

        // ===== DZ-156: null / 範囲外防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-156")]
        public void Given_TickEffectにnull_When_PlayerInfluenceを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, null, 3));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-156")]
        public void Given_RemainingCount0_When_PlayerInfluenceを生成_Then_ArgumentOutOfRangeExceptionを投げる()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, Tick(), 0));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-156")]
        public void Given_RemainingCount負値_When_PlayerInfluenceを生成_Then_ArgumentOutOfRangeExceptionを投げる()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, Tick(), -1));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-156")]
        public void Given_withでTickEffectnull_When_PlayerInfluenceを変更_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, Tick(), 3);
            // When / Then(with 経由でも init setter の二重ガードが効く)
            Assert.Throws<ArgumentNullException>(() => { var _ = inf with { TickEffect = null }; });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-156")]
        public void Given_withでRemainingCount0_When_PlayerInfluenceを変更_Then_ArgumentOutOfRangeExceptionを投げる()
        {
            // Given
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, Tick(), 3);
            // When / Then
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = inf with { RemainingCount = 0 }; });
        }

        // ===== DZ-157: 値同値性(record auto-equals)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-157")]
        public void Given_同フィールド値の2インスタンス_When_Equals_Then_true()
        {
            // Given
            var a = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -5), 3);
            var b = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -5), 3);
            // When / Then
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-157")]
        public void Given_RemainingCount異なる2インスタンス_When_Equals_Then_false()
        {
            // Given
            var a = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -5), 3);
            var b = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -5), 2);
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
