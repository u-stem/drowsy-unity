using NUnit.Framework;
using Newtonsoft.Json;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Persistence;
// 本ファイルは UnityEngine を import しない(ScriptableObject 等の依存なし)。
// `using Property = NUnit.Framework.PropertyAttribute` alias も不採用
// (`EffectJsonConverterTests.cs` 冒頭コメントと同じ理由、CS1614 を避ける)。

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="Converters.CardIdJsonConverter"/> の round-trip + schema 違反異常経路検証
    /// (ADR-0018 §8 / docs/todo.md「CardIdJsonConverter の負値 instance / 不正 schema 経路に
    /// Persistence テストを追加」由来、INF-088〜094)。
    /// </summary>
    /// <remarks>
    /// 本 converter は <c>DrowZzzGameSessionSerializer</c> 内 <c>DrowZzzJsonSettings.Create()</c> に
    /// 登録され、`CardId` プロパティ(Pile / Hand 内のカード参照)を <c>"<typeId>#<instance>"</c> string で
    /// 表現する。Newtonsoft.Json の <c>JsonConvert.SerializeObject</c> / <c>DeserializeObject&lt;CardId&gt;</c>
    /// 経路を本 fixture で直接検証することで、schema 違反時の <c>JsonSerializationException</c> 包装が
    /// 後退しないことを担保する。
    /// </remarks>
    [TestFixture]
    public sealed class CardIdJsonConverterTests
    {
        private static JsonSerializerSettings Settings => DrowZzzJsonSettings.Create();

        // ===== INF-089: 正常系 round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-089")]
        [TestCase("dream", 0)]
        [TestCase("sheep", 3)]
        [TestCase("01", 42)]
        public void Given_normal_CardId_When_RoundTrip_Then_等価(string typeId, int instance)
        {
            // Given
            var original = CardId.Of(CardTypeId.Of(typeId), instance);

            // When
            var json = JsonConvert.SerializeObject(original, Settings);
            var restored = JsonConvert.DeserializeObject<CardId>(json, Settings);

            // Then
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-090: null / 空 / 空白 string token =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-090")]
        [TestCase("\"\"")]
        [TestCase("\" \"")]
        [TestCase("\"\\t\"")]
        public void Given_空または空白string_When_Deserialize_Then_JsonSerializationException(string json)
        {
            // Given / When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<CardId>(json, Settings));
        }

        // ===== INF-091: '#' separator 欠如 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-091")]
        public void Given_separator欠如_When_Deserialize_Then_JsonSerializationException()
        {
            // Given(`#` を含まない string)
            const string json = "\"dream\"";

            // When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<CardId>(json, Settings));
        }

        // ===== INF-092: instance 部分が非 int =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-092")]
        [TestCase("\"dream#abc\"")]
        [TestCase("\"dream#\"")]
        [TestCase("\"dream#1.5\"")]
        public void Given_非int_instance_When_Deserialize_Then_JsonSerializationException(string json)
        {
            // Given / When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<CardId>(json, Settings));
        }

        // ===== INF-093: 負値 instance(CardId.Of の ArgumentOutOfRangeException を wrap)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-093")]
        [TestCase("\"dream#-1\"")]
        [TestCase("\"sheep#-100\"")]
        public void Given_負値_instance_When_Deserialize_Then_JsonSerializationException(string json)
        {
            // Given / When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<CardId>(json, Settings));
        }

        // ===== INF-094: typeId 部分が空文字列(CardTypeId.Of の ArgumentException を wrap)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-094")]
        public void Given_typeIdPart_空文字列_When_Deserialize_Then_JsonSerializationException()
        {
            // Given(`#` 左が空文字列)
            const string json = "\"#0\"";

            // When / Then
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<CardId>(json, Settings));
        }

        // ===== 補助:JsonToken.Null は throw せず null を返す経路(INF-090 と区別)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-089")]
        public void Given_JsonNullToken_When_Deserialize_Then_nullを返す()
        {
            // Given(JSON literal null)
            const string json = "null";

            // When
            var restored = JsonConvert.DeserializeObject<CardId>(json, Settings);

            // Then
            Assert.That(restored, Is.Null);
        }
    }
}
