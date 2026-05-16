using System;
using NUnit.Framework;
using Newtonsoft.Json;
using Drowsy.Domain.Players;
using Drowsy.Infrastructure.Persistence;

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="Converters.PlayerIdJsonConverter"/> の round-trip + null token + schema 違反異常経路検証
    /// (B-5 第 1 弾、Infrastructure カバレッジ補完、INF-095〜099)。
    /// </summary>
    [TestFixture]
    public sealed class PlayerIdJsonConverterTests
    {
        private static JsonSerializerSettings Settings => DrowZzzJsonSettings.Create();

        // ===== INF-096: 正常系 round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-096")]
        [TestCase("p1")]
        [TestCase("p2")]
        [TestCase("player-001")]
        public void Given_normal_PlayerId_When_RoundTrip_Then_等価(string playerIdValue)
        {
            // Given
            var original = PlayerId.Of(playerIdValue);

            // When
            var json = JsonConvert.SerializeObject(original, Settings);
            var restored = JsonConvert.DeserializeObject<PlayerId>(json, Settings);

            // Then
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-097: null 対称性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-097")]
        public void Given_JsonNullToken_When_Deserialize_Then_nullを返す()
        {
            // Given(JSON literal null)
            var json = "null";

            // When
            var restored = JsonConvert.DeserializeObject<PlayerId>(json, Settings);

            // Then
            Assert.That(restored, Is.Null);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-097")]
        public void Given_nullPlayerId_When_Serialize_Then_nullリテラル()
        {
            // Given
            PlayerId original = null;

            // When
            var json = JsonConvert.SerializeObject(original, Settings);

            // Then
            Assert.That(json, Is.EqualTo("null"));
        }

        // ===== INF-098: 非 string token は JsonSerializationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-098")]
        [TestCase("123")]
        [TestCase("{}")]
        [TestCase("[]")]
        public void Given_非string_token_When_Deserialize_Then_JsonSerializationException(string json)
        {
            // Given / When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<PlayerId>(json, Settings));
        }

        // ===== INF-099: 空文字列 / 空白は PlayerId.Of の ArgumentException 透過 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-099")]
        [TestCase("\"\"")]
        [TestCase("\" \"")]
        [TestCase("\"\\t\"")]
        public void Given_空文字列_When_Deserialize_Then_ArgumentException(string json)
        {
            // Given / When / Then
            Assert.Throws<ArgumentException>(
                () => JsonConvert.DeserializeObject<PlayerId>(json, Settings));
        }
    }
}
