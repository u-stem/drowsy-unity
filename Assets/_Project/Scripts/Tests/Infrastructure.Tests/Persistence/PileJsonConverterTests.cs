using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Persistence;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="Converters.PileJsonConverter"/> の round-trip + null token + schema 違反異常経路検証
    /// (B-5 第 1 弾、Infrastructure カバレッジ補完、INF-108〜114)。
    /// </summary>
    [TestFixture]
    public sealed class PileJsonConverterTests
    {
        private static JsonSerializerSettings Settings => DrowZzzJsonSettings.Create();

        // ===== INF-109: 3 枚 round-trip(順序保持)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-109")]
        public void Given_3枚のPile_When_RoundTrip_Then_等価かつ順序保持()
        {
            // Given(順序を意識した 3 枚:01 / 02 / dream)
            var original = new Pile(new[]
            {
                CardId.Of(CardTypeId.Of("01"), 0),
                CardId.Of(CardTypeId.Of("02"), 0),
                CardId.Of(CardTypeId.Of("dream"), 0),
            });

            // When
            var json = JsonConvert.SerializeObject(original, Settings);
            var restored = JsonConvert.DeserializeObject<Pile>(json, Settings);

            // Then(値同値 + 順序保持)
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-110: empty Pile round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-110")]
        public void Given_emptyPile_When_RoundTrip_Then_等価()
        {
            // Given
            var original = Pile.Empty;

            // When
            var json = JsonConvert.SerializeObject(original, Settings);
            var restored = JsonConvert.DeserializeObject<Pile>(json, Settings);

            // Then
            Assert.That(json, Is.EqualTo("[]"));
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-111: null 対称性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-111")]
        public void Given_JsonNullToken_When_Deserialize_Then_nullを返す()
        {
            // Given
            const string json = "null";

            // When
            var restored = JsonConvert.DeserializeObject<Pile>(json, Settings);

            // Then
            Assert.That(restored, Is.Null);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-111")]
        public void Given_nullPile_When_Serialize_Then_nullリテラル()
        {
            // Given
            Pile original = null;

            // When
            var json = JsonConvert.SerializeObject(original, Settings);

            // Then
            Assert.That(json, Is.EqualTo("null"));
        }

        // ===== INF-112: 非 array token は JsonSerializationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-112")]
        [TestCase("\"pile\"")]
        [TestCase("123")]
        [TestCase("{}")]
        public void Given_非array_token_When_Deserialize_Then_JsonSerializationException(string json)
        {
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<Pile>(json, Settings));
        }

        // ===== INF-113: 不正 CardId schema は CardIdJsonConverter の JsonSerializationException 透過 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-113")]
        public void Given_不正CardId_When_Deserialize_Then_JsonSerializationException()
        {
            // Given(`#` 欠如、schema 違反)
            const string json = "[\"dream\"]";

            // When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<Pile>(json, Settings));
        }

        // ===== INF-114: 重複 CardId は Pile では成功(Hand との対称差)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-114")]
        public void Given_重複CardId配列_When_Deserialize_Then_成功で重複保持()
        {
            // Given(同じ `01#0` が 2 回 — Pile は重複許容)
            const string json = "[\"01#0\",\"01#0\"]";

            // When
            var restored = JsonConvert.DeserializeObject<Pile>(json, Settings);

            // Then(2 件保持)— `Has.Count` は IReadOnlyList<T> の explicit interface 経由を解決できないため
            // `.Count` を直接呼ぶ(Unity NUnit 3.x の制約、本 PR 着手時の Test Runner 実行で発覚)。
            Assert.That(restored.Cards.Count, Is.EqualTo(2));
        }
    }
}
