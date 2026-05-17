using System;
using NUnit.Framework;
using Newtonsoft.Json;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Persistence;

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="Converters.HandJsonConverter"/> の round-trip + null token + schema 違反異常経路検証
    /// (B-5 第 1 弾、Infrastructure カバレッジ補完、INF-101〜107)。
    /// </summary>
    [TestFixture]
    public sealed class HandJsonConverterTests
    {
        private static JsonSerializerSettings Settings => DrowZzzJsonSettings.Create();

        // ===== INF-102: 3 枚 round-trip(順序保持)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-102")]
        public void Given_3枚のHand_When_RoundTrip_Then_等価かつ順序保持()
        {
            // Given(順序を意識した 3 枚:01 / 02 / dream)
            var original = new Hand(new[]
            {
                CardId.Of(CardTypeId.Of("01"), 0),
                CardId.Of(CardTypeId.Of("02"), 0),
                CardId.Of(CardTypeId.Of("dream"), 0),
            });

            // When
            var json = JsonConvert.SerializeObject(original, Settings);
            var restored = JsonConvert.DeserializeObject<Hand>(json, Settings);

            // Then(値同値 + 順序保持)
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-103: empty Hand round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-103")]
        public void Given_emptyHand_When_RoundTrip_Then_等価()
        {
            // Given
            var original = Hand.Empty;

            // When
            var json = JsonConvert.SerializeObject(original, Settings);
            var restored = JsonConvert.DeserializeObject<Hand>(json, Settings);

            // Then
            Assert.That(json, Is.EqualTo("[]"));
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-104: null 対称性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-104")]
        public void Given_JsonNullToken_When_Deserialize_Then_nullを返す()
        {
            // Given
            const string json = "null";

            // When
            var restored = JsonConvert.DeserializeObject<Hand>(json, Settings);

            // Then
            Assert.That(restored, Is.Null);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-104")]
        public void Given_nullHand_When_Serialize_Then_nullリテラル()
        {
            // Given
            Hand original = null;

            // When
            var json = JsonConvert.SerializeObject(original, Settings);

            // Then
            Assert.That(json, Is.EqualTo("null"));
        }

        // ===== INF-105: 非 array token は JsonSerializationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-105")]
        [TestCase("\"hand\"")]
        [TestCase("123")]
        [TestCase("{}")]
        public void Given_非array_token_When_Deserialize_Then_JsonSerializationException(string json)
        {
            // Given / When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<Hand>(json, Settings));
        }

        // ===== INF-106: 重複 CardId 配列は Hand ctor の ArgumentException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-106")]
        public void Given_重複CardId配列_When_Deserialize_Then_ArgumentException()
        {
            // Given(同じ `01#0` が 2 回出現)
            const string json = "[\"01#0\",\"01#0\"]";

            // When / Then
            Assert.Throws<ArgumentException>(
                () => JsonConvert.DeserializeObject<Hand>(json, Settings));
        }

        // ===== INF-107: 不正 CardId schema は CardIdJsonConverter の JsonSerializationException 透過 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-107")]
        public void Given_不正CardId_When_Deserialize_Then_JsonSerializationException()
        {
            // Given(`#` 欠如、ADR-0018 schema 違反)
            const string json = "[\"dream\"]";

            // When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<Hand>(json, Settings));
        }
    }
}
