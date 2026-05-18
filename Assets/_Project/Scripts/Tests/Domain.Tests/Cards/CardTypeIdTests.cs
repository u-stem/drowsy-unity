using System;
using Drowsy.Domain.Cards;
using NUnit.Framework;

namespace Drowsy.Domain.Tests.Cards
{
    /// <summary>
    /// <see cref="CardTypeId"/>(ADR-0018 で新設したカード種別 ID、catalog の lookup key)の単体テスト。
    /// </summary>
    /// <remarks>
    /// CTYPE-001(sealed record)はコンパイル時保証で Ubiquitous 免除、本 fixture は防御要件と
    /// Value 公開、record 等値性を検証する。
    /// </remarks>
    [TestFixture]
    public sealed class CardTypeIdTests
    {
        // ===== CTYPE-002: null / 空 / 空白のみは ArgumentException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CTYPE-002")]
        public void Given_null_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => CardTypeId.Of(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CTYPE-002")]
        public void Given_空文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => CardTypeId.Of(""));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CTYPE-002")]
        public void Given_空白のみの文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => CardTypeId.Of("   "));
        }

        // ===== CTYPE-005: '#' を含む文字列は ArgumentException(ADR-0018 §8 で予約済の区切り文字) =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CTYPE-005")]
        public void Given_シャープを含む文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる()
        {
            // Given(CardId.Value の "<typeId>#<instance>" 区切り文字 '#' を含む)
            // When / Then
            Assert.Throws<ArgumentException>(() => CardTypeId.Of("dream#extra"));
        }

        // ===== CTYPE-003: 非空の string で Of を呼ぶと Value に保持される =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CTYPE-003")]
        public void Given_非空のstring_When_Ofを呼ぶ_Then_Valueに保持される()
        {
            // Given
            const string value = "dream";
            // When
            var typeId = CardTypeId.Of(value);
            // Then
            Assert.That(typeId.Value, Is.EqualTo(value));
        }

        // ===== CTYPE-004: 同じ Value の 2 つの CardTypeId は等価(record default) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CTYPE-004")]
        public void Given_同じValueの2つのCardTypeId_When_等価比較_Then_等価()
        {
            // Given
            var a = CardTypeId.Of("dream");
            var b = CardTypeId.Of("dream");
            // When / Then(record 自動生成の等価)
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CTYPE-004")]
        public void Given_異なるValueの2つのCardTypeId_When_等価比較_Then_非等価()
        {
            // Given
            var a = CardTypeId.Of("dream");
            var b = CardTypeId.Of("sheep");
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
