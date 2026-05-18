using System;
using Drowsy.Domain.Players;
using NUnit.Framework;

namespace Drowsy.Domain.Tests.Players
{
    [TestFixture]
    public sealed class PlayerIdTests
    {
        // 普遍要件 PLAYER-001 / PLAYER-002 は sealed record で構造的に保証

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-004")]
        public void Given_有効な値_When_Ofを呼ぶ_Then_インスタンスが生成され_Valueは入力と同じ()
        {
            // Given
            const string input = "p1";
            // When
            var id = PlayerId.Of(input);
            // Then
            Assert.That(id.Value, Is.EqualTo(input));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PLAYER-006")]
        public void Given_null_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => PlayerId.Of(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PLAYER-007")]
        public void Given_空文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => PlayerId.Of(""));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PLAYER-008")]
        public void Given_空白のみの文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => PlayerId.Of("   "));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-003")]
        public void Given_同じ値の2つのPlayerId_When_等価比較_Then_等価()
        {
            var a = PlayerId.Of("p1");
            var b = PlayerId.Of("p1");
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-003")]
        public void Given_異なる値の2つのPlayerId_When_等価比較_Then_非等価()
        {
            var a = PlayerId.Of("p1");
            var b = PlayerId.Of("p2");
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-005")]
        public void Given_PlayerId_When_ToString_Then_Valueを返す()
        {
            var id = PlayerId.Of("alice");
            Assert.That(id.ToString(), Is.EqualTo("alice"));
        }
    }
}
