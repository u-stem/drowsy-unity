using System;
using NUnit.Framework;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Domain.Tests.Game
{
    /// <summary>
    /// <see cref="GameOutcome"/> 階層(<see cref="WinnerOutcome"/> / <see cref="DrawOutcome"/>)の
    /// 不変条件と値同値性を検証する(GS-101 / GS-102 / GS-103)。
    /// </summary>
    [TestFixture]
    public sealed class GameOutcomeTests
    {
        // ===== GS-101: WinnerOutcome の構築正常系と null 防御 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-101")]
        public void Given_有効なPlayerId_When_WinnerOutcomeを生成_Then_Winnerが入力と一致する()
        {
            // Given
            var p1 = PlayerId.Of("p1");
            // When
            var outcome = new WinnerOutcome(p1);
            // Then
            Assert.That(outcome.Winner, Is.EqualTo(p1));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-105")]
        public void Given_Winnerにnull_When_WinnerOutcomeを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() => new WinnerOutcome(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "GS-105")]
        public void Given_既存WinnerOutcome_When_withでWinnerにnull_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var outcome = new WinnerOutcome(PlayerId.Of("p1"));
            // When / Then(with 経由でも init setter の二重ガードが効く)
            Assert.Throws<ArgumentNullException>(() => { var _ = outcome with { Winner = null }; });
        }

        // ===== GS-102: WinnerOutcome の値同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-102")]
        public void Given_同PlayerIdの2WinnerOutcome_When_Equals_Then_true()
        {
            var a = new WinnerOutcome(PlayerId.Of("p1"));
            var b = new WinnerOutcome(PlayerId.Of("p1"));
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-102")]
        public void Given_異なるPlayerIdの2WinnerOutcome_When_Equals_Then_false()
        {
            var a = new WinnerOutcome(PlayerId.Of("p1"));
            var b = new WinnerOutcome(PlayerId.Of("p2"));
            Assert.That(a, Is.Not.EqualTo(b));
        }

        // ===== GS-103: DrawOutcome の値同値性(常に等価)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-103")]
        public void Given_2つのDrawOutcome_When_Equals_Then_true()
        {
            // Given
            var a = new DrawOutcome();
            var b = new DrawOutcome();
            // When / Then(フィールドなし record の auto-equals は常に等価)
            Assert.That(a, Is.EqualTo(b));
        }

        // ===== GS-104: WinnerOutcome と DrawOutcome は非等価 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "GS-104")]
        public void Given_WinnerOutcomeとDrawOutcome_When_Equals_Then_false()
        {
            // Given
            GameOutcome a = new WinnerOutcome(PlayerId.Of("p1"));
            GameOutcome b = new DrawOutcome();
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
