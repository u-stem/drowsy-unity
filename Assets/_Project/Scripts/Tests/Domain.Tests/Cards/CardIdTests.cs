using System;
using NUnit.Framework;
using Drowsy.Domain.Cards;

namespace Drowsy.Domain.Tests.Cards
{
    [TestFixture]
    public class CardIdTests
    {
        [Test, Category("Small"), Category("Normal")]
        public void Given_有効な値_When_Ofを呼ぶ_Then_インスタンスが生成され_Valueは入力と同じ()
        {
            // Given
            var input = "hearts-7";
            // When
            var id = CardId.Of(input);
            // Then
            Assert.That(id.Value, Is.EqualTo(input));
        }

        [Test, Category("Small"), Category("Abnormal")]
        public void Given_null_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => CardId.Of(null));
        }

        [Test, Category("Small"), Category("Abnormal")]
        public void Given_空文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => CardId.Of(""));
        }

        [Test, Category("Small"), Category("Abnormal")]
        public void Given_空白のみの文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => CardId.Of("   "));
        }

        [Test, Category("Small"), Category("Normal")]
        public void Given_同じ値の2つのCardId_When_等価比較_Then_等価()
        {
            // Given
            var a = CardId.Of("X");
            var b = CardId.Of("X");
            // When / Then
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal")]
        public void Given_異なる値の2つのCardId_When_等価比較_Then_非等価()
        {
            var a = CardId.Of("X");
            var b = CardId.Of("Y");
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal")]
        public void Given_CardId_When_ToString_Then_Valueを返す()
        {
            var id = CardId.Of("clubs-Q");
            Assert.That(id.ToString(), Is.EqualTo("clubs-Q"));
        }
    }
}
