using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Domain.Cards;

namespace Drowsy.Domain.Tests.Cards
{
    [TestFixture]
    public sealed class CardDataTests
    {
        // 普遍要件 CDATA-001 / CDATA-002 は sealed class + private readonly + IReadOnlyDictionary で構造的に保証

        // ===== CDATA-003: 値保持 (1 テスト 1 アサーション原則のため Name と Attributes を分離) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-003")]
        public void Given_有効なnameと属性_When_コンストラクタ_Then_Nameが入力と同じ()
        {
            // Given
            const string name = "Joker";
            var attrs = new Dictionary<string, int> { ["power"] = 10, ["rarity"] = 3 };
            // When
            var card = new CardData(name, attrs);
            // Then
            Assert.That(card.Name, Is.EqualTo(name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-003")]
        public void Given_有効なnameと属性_When_コンストラクタ_Then_Attributesに同じキー値が含まれる()
        {
            // Given
            var attrs = new Dictionary<string, int> { ["power"] = 10 };
            // When
            var card = new CardData("Joker", attrs);
            // Then
            Assert.That(card.Attributes["power"], Is.EqualTo(10));
        }

        // ===== CDATA-004 / CDATA-005: HasAttribute =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-004")]
        public void Given_存在するキー_When_HasAttribute_Then_trueを返す()
        {
            // Given
            var card = new CardData("Joker", new Dictionary<string, int> { ["power"] = 10 });
            // When
            var result = card.HasAttribute("power");
            // Then
            Assert.That(result, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-005")]
        public void Given_存在しないキー_When_HasAttribute_Then_falseを返す()
        {
            // Given
            var card = new CardData("Joker", new Dictionary<string, int> { ["power"] = 10 });
            // When
            var result = card.HasAttribute("cost");
            // Then
            Assert.That(result, Is.False);
        }

        // ===== CDATA-006 / CDATA-007: GetAttribute =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-006")]
        public void Given_存在するキー_When_GetAttribute_Then_格納値を返す()
        {
            // Given
            var card = new CardData("Joker", new Dictionary<string, int> { ["power"] = 10 });
            // When
            var result = card.GetAttribute("power", -1);
            // Then
            Assert.That(result, Is.EqualTo(10));
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "CDATA-007")]
        public void Given_存在しないキー_When_GetAttribute_Then_defaultValueを返す()
        {
            // Given
            var card = new CardData("Joker", new Dictionary<string, int> { ["power"] = 10 });
            // When
            var result = card.GetAttribute("cost", 999);
            // Then
            Assert.That(result, Is.EqualTo(999));
        }

        // ===== CDATA-008: 順序非依存マルチセット同値(n=0/n=1/n=2 サイズ網羅 + 不一致 3 ケース) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_順序の異なる同じキー値ペア_When_等価比較_Then_等価()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
            var b = new CardData("X", new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 });
            // When / Then
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_異なるName_When_等価比較_Then_非等価()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            var b = new CardData("Y", new Dictionary<string, int> { ["a"] = 1 });
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_異なる属性値_When_等価比較_Then_非等価()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            var b = new CardData("X", new Dictionary<string, int> { ["a"] = 2 });
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_両方空辞書の同名CardData_When_等価比較_Then_等価()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int>());
            var b = new CardData("X", new Dictionary<string, int>());
            // When / Then
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_単一属性で同じキー値_When_等価比較_Then_等価()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            var b = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            // When / Then
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_属性キー数が異なる_When_等価比較_Then_非等価()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            var b = new CardData("X", new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_同数だが異なるキー名_When_等価比較_Then_非等価()
        {
            // Given: Count は同じだが片方に存在しないキーを持つ
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
            var b = new CardData("X", new Dictionary<string, int> { ["a"] = 1, ["c"] = 2 });
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_同一インスタンス_When_等価比較_Then_等価()
        {
            // Given: ReferenceEquals 短絡パスをカバー
            var card = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            // When / Then
            Assert.That(card.Equals(card), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-008")]
        public void Given_null_When_EqualsCardData_Then_falseを返す()
        {
            // Given: Equals(CardData) overload に null を渡す経路をカバー
            var card = new CardData("X", new Dictionary<string, int>());
            CardData other = null;
            // When / Then
            Assert.That(card.Equals(other), Is.False);
        }

        // ===== CDATA-009: 等価インスタンスの GetHashCode 一致 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-009")]
        public void Given_等価な2つのCardData_When_GetHashCode_Then_同じ値を返す()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
            var b = new CardData("X", new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 });
            // When
            var ha = a.GetHashCode();
            var hb = b.GetHashCode();
            // Then
            Assert.That(ha, Is.EqualTo(hb));
        }

        // ===== CDATA-010: 防御コピー(2 観点を分離) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-010")]
        public void Given_生成後にソース辞書の既存キーを変更_When_Attributes参照_Then_変更されない()
        {
            // Given
            var source = new Dictionary<string, int> { ["a"] = 1 };
            var card = new CardData("X", source);
            // When
            source["a"] = 999;
            // Then
            Assert.That(card.Attributes["a"], Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-010")]
        public void Given_生成後にソース辞書に新キーを追加_When_Attributes参照_Then_新キーが含まれない()
        {
            // Given
            var source = new Dictionary<string, int> { ["a"] = 1 };
            var card = new CardData("X", source);
            // When
            source["b"] = 2;
            // Then
            Assert.That(card.Attributes.ContainsKey("b"), Is.False);
        }

        // ===== CDATA-011: name の null/空/空白 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-011")]
        public void Given_null_When_コンストラクタのname_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => new CardData(null!, new Dictionary<string, int>()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-011")]
        public void Given_空文字列_When_コンストラクタのname_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => new CardData("", new Dictionary<string, int>()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-011")]
        public void Given_空白のみ_When_コンストラクタのname_Then_ArgumentExceptionを投げる()
        {
            Assert.Throws<ArgumentException>(() => new CardData("   ", new Dictionary<string, int>()));
        }

        // ===== CDATA-012: attributes が null =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-012")]
        public void Given_null_When_コンストラクタのattributes_Then_ArgumentNullExceptionを投げる()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new CardData("X", null!));
            Assert.That(ex!.ParamName, Is.EqualTo("attributes"));
        }

        // ===== CDATA-013: attributes に null/空/空白キーを含む =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-013")]
        public void Given_nullキーを含むattributes_When_コンストラクタ_Then_ArgumentExceptionを投げる()
        {
            // Given: Dictionary は string キーに null を許容しないため List<KVP> で組み立てる
            var attrs = new List<KeyValuePair<string, int>>
            {
                new("valid", 1),
                new(null!, 2),
            };
            // When / Then
            Assert.Throws<ArgumentException>(() => new CardData("X", attrs));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-013")]
        public void Given_空文字列キーを含むattributes_When_コンストラクタ_Then_ArgumentExceptionを投げる()
        {
            // Given
            var attrs = new Dictionary<string, int> { ["valid"] = 1, [""] = 2 };
            // When / Then
            Assert.Throws<ArgumentException>(() => new CardData("X", attrs));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-013")]
        public void Given_空白のみキーを含むattributes_When_コンストラクタ_Then_ArgumentExceptionを投げる()
        {
            // Given
            var attrs = new Dictionary<string, int> { ["valid"] = 1, ["   "] = 2 };
            // When / Then
            Assert.Throws<ArgumentException>(() => new CardData("X", attrs));
        }

        // ===== CDATA-014: HasAttribute / GetAttribute の null key =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-014")]
        public void Given_nullキー_When_HasAttribute_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var card = new CardData("X", new Dictionary<string, int>());
            // When / Then
            var ex = Assert.Throws<ArgumentNullException>(() => card.HasAttribute(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("key"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CDATA-014")]
        public void Given_nullキー_When_GetAttribute_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var card = new CardData("X", new Dictionary<string, int>());
            // When / Then
            var ex = Assert.Throws<ArgumentNullException>(() => card.GetAttribute(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("key"));
        }

        // ===== CDATA-015: operator== / operator!= =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-015")]
        public void Given_等価な2つのCardData_When_operator_等価_Then_trueを返す()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            var b = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            // When
            var result = a == b;
            // Then
            Assert.That(result, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-015")]
        public void Given_非等価な2つのCardData_When_operator_非等価_Then_trueを返す()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            var b = new CardData("Y", new Dictionary<string, int> { ["a"] = 1 });
            // When
            var result = a != b;
            // Then
            Assert.That(result, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-015")]
        public void Given_非等価な2つのCardData_When_operator_等価_Then_falseを返す()
        {
            // Given
            var a = new CardData("X", new Dictionary<string, int> { ["a"] = 1 });
            var b = new CardData("Y", new Dictionary<string, int> { ["a"] = 1 });
            // When
            var result = a == b;
            // Then
            Assert.That(result, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-015")]
        public void Given_両方null_When_operator_等価_Then_trueを返す()
        {
            // Given
            CardData a = null;
            CardData b = null;
            // When
            var result = a == b;
            // Then
            Assert.That(result, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-015")]
        public void Given_片方nullで他方非null_When_operator_等価_Then_falseを返す()
        {
            // Given
            CardData a = null;
            var b = new CardData("X", new Dictionary<string, int>());
            // When
            var result = a == b;
            // Then
            Assert.That(result, Is.False);
        }

        // ===== CDATA-017: Equals(object) overload の null / 異型挙動 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-017")]
        public void Given_null_When_Equalsオブジェクト_Then_falseを返す()
        {
            // Given
            var card = new CardData("X", new Dictionary<string, int>());
            // When
            var result = card.Equals((object)null);
            // Then
            Assert.That(result, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CDATA-017")]
        public void Given_異なる型のオブジェクト_When_Equalsオブジェクト_Then_falseを返す()
        {
            // Given
            var card = new CardData("X", new Dictionary<string, int>());
            // When
            var result = card.Equals((object)"not a CardData");
            // Then
            Assert.That(result, Is.False);
        }
    }
}
