using NUnit.Framework;
using Newtonsoft.Json;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Infrastructure.Persistence;

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="Converters.DdpPoolJsonConverter"/> の round-trip + null token + schema 違反異常経路検証
    /// (B-5 第 1 弾、Infrastructure カバレッジ補完、INF-115〜121)。
    /// </summary>
    [TestFixture]
    public sealed class DdpPoolJsonConverterTests
    {
        private static JsonSerializerSettings Settings => DrowZzzJsonSettings.Create();

        // ===== INF-116: 正負 0 混在の round-trip(順序保持)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-116")]
        public void Given_正負0混在のDdpPool_When_RoundTrip_Then_等価かつ順序保持()
        {
            // Given(順序を意識した 4 要素:-3 / 0 / 7 / -1)
            var original = new DdpPool(new[] { -3, 0, 7, -1 });

            // When
            var json = JsonConvert.SerializeObject(original, Settings);
            var restored = JsonConvert.DeserializeObject<DdpPool>(json, Settings);

            // Then(値同値 + 順序保持)
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-117: empty DdpPool round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-117")]
        public void Given_emptyDdpPool_When_RoundTrip_Then_等価()
        {
            // Given
            var original = DdpPool.Empty;

            // When
            var json = JsonConvert.SerializeObject(original, Settings);
            var restored = JsonConvert.DeserializeObject<DdpPool>(json, Settings);

            // Then
            Assert.That(json, Is.EqualTo("[]"));
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-118: null 対称性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-118")]
        public void Given_JsonNullToken_When_Deserialize_Then_nullを返す()
        {
            var json = "null";

            var restored = JsonConvert.DeserializeObject<DdpPool>(json, Settings);

            Assert.That(restored, Is.Null);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-118")]
        public void Given_nullDdpPool_When_Serialize_Then_nullリテラル()
        {
            DdpPool original = null;

            var json = JsonConvert.SerializeObject(original, Settings);

            Assert.That(json, Is.EqualTo("null"));
        }

        // ===== INF-119: 非 array token は JsonSerializationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-119")]
        [TestCase("\"pool\"")]
        [TestCase("123")]
        [TestCase("{}")]
        public void Given_非array_token_When_Deserialize_Then_JsonSerializationException(string json)
        {
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<DdpPool>(json, Settings));
        }

        // ===== INF-120: 非 int 要素は Newtonsoft の例外を透過 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-120")]
        [TestCase("[\"abc\"]")]
        [TestCase("[1.5]")]
        public void Given_非int要素_When_Deserialize_Then_Newtonsoft例外(string json)
        {
            // Newtonsoft.Json は List<int> deserialize 時に string / float を int 変換できず
            // JsonReaderException(基底)or JsonSerializationException を投げる。型を分けるよりも
            // 「`JsonException` 系のいずれかを投げる」ことを確認する。
            Assert.Throws(Is.InstanceOf<JsonException>(),
                () => JsonConvert.DeserializeObject<DdpPool>(json, Settings));
        }

        // ===== INF-121: 重複 int は DdpPool で許容(シーケンス) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-121")]
        public void Given_重複int配列_When_Deserialize_Then_成功で重複保持()
        {
            // Given(同じ 5 が 3 回)
            var json = "[5,5,5]";

            // When
            var restored = JsonConvert.DeserializeObject<DdpPool>(json, Settings);

            // Then(3 件保持)— `Has.Count` は IReadOnlyList<T> の explicit interface 経由を解決できないため
            // `.Count` を直接呼ぶ(Unity NUnit 3.x の制約、本 PR 着手時の Test Runner 実行で発覚)。
            Assert.That(restored.Values.Count, Is.EqualTo(3));
        }
    }
}
