using System;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Players;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="DamageBedEffect"/> の構築検証と <see cref="EffectInterpreter.Apply"/> による
    /// ベッド破損率更新挙動を検証する(DZ-193 / DZ-194 / DZ-195 / DZ-196)。
    /// </summary>
    [TestFixture]
    public sealed class DamageBedEffectTests
    {
        // ===== ヘルパー =====

        private static DrowZzzGameSession NewSession(
            int currentPlayerIndex = 0,
            int bedP1 = 0,
            int bedP2 = 0) =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: currentPlayerIndex,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                bedDamages: SessionFactory.Dp(p1: bedP1, p2: bedP2));

        // ===== DZ-193: 構築の正常系 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-193")]
        public void Given_有効な引数_When_DamageBedEffectを生成_Then_Targetが入力と一致する()
        {
            var effect = new DamageBedEffect(SdpTarget.Opponent, 20);
            Assert.That(effect.Target, Is.EqualTo(SdpTarget.Opponent));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-193")]
        public void Given_有効な引数_When_DamageBedEffectを生成_Then_Percentが入力と一致する()
        {
            var effect = new DamageBedEffect(SdpTarget.Self, 5);
            Assert.That(effect.Percent, Is.EqualTo(5));
        }

        // ===== DZ-194: Percent が 5 の倍数 / 正値の検証 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-194")]
        public void Given_Percentが5の倍数でない_When_DamageBedEffectを生成_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => new DamageBedEffect(SdpTarget.Self, 7));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-194")]
        public void Given_Percentが0_When_DamageBedEffectを生成_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => new DamageBedEffect(SdpTarget.Self, 0));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-194")]
        public void Given_Percentが負値_When_DamageBedEffectを生成_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => new DamageBedEffect(SdpTarget.Self, -5));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-194")]
        public void Given_withで不正なPercent_When_DamageBedEffectを変更_Then_ArgumentExceptionを投げる()
        {
            var effect = new DamageBedEffect(SdpTarget.Self, 5);
            Assert.Throws<ArgumentException>(() => { var _ = effect with { Percent = 3 }; });
        }

        // ===== DZ-195: Apply で対象プレイヤーの BedDamages が増加 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-195")]
        public void Given_p1current_TargetSelf_Percent20_When_Apply_Then_p1のBedDamagesが20に増加()
        {
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, bedP1: 0);
            var next = interpreter.Apply(session, new DamageBedEffect(SdpTarget.Self, 20));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(20));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-195")]
        public void Given_p1current_TargetOpponent_Percent20_When_Apply_Then_p2のBedDamagesが20に増加()
        {
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, bedP2: 0);
            var next = interpreter.Apply(session, new DamageBedEffect(SdpTarget.Opponent, 20));
            Assert.That(next.BedDamages[PlayerId.Of("p2")], Is.EqualTo(20));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-195")]
        public void Given_既存破損30_Percent20_When_Apply_Then_BedDamagesが累積50()
        {
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, bedP1: 30);
            var next = interpreter.Apply(session, new DamageBedEffect(SdpTarget.Self, 20));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(50));
        }

        // ===== DZ-196: 上限 100% でクランプ =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-196")]
        public void Given_既存破損90_Percent20_When_Apply_Then_BedDamagesが100でクランプ()
        {
            // Given(90 + 20 = 110 だが、上限 100 でクランプ)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, bedP1: 90);
            var next = interpreter.Apply(session, new DamageBedEffect(SdpTarget.Self, 20));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(100));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-196")]
        public void Given_既存破損100_Percent20_When_Apply_Then_BedDamagesが100のまま()
        {
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, bedP1: 100);
            var next = interpreter.Apply(session, new DamageBedEffect(SdpTarget.Self, 20));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(100));
        }

        // ===== DZ-195 続き: 他プレイヤーの BedDamages は不変 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-195")]
        public void Given_TargetSelf_When_Apply_Then_他プレイヤーのBedDamagesは不変()
        {
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, bedP1: 0, bedP2: 40);
            var next = interpreter.Apply(session, new DamageBedEffect(SdpTarget.Self, 20));
            Assert.That(next.BedDamages[PlayerId.Of("p2")], Is.EqualTo(40));
        }
    }
}
