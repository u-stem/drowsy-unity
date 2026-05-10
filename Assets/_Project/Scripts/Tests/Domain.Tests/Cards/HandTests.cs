using System;
using NUnit.Framework;
using Drowsy.Domain.Cards;

namespace Drowsy.Domain.Tests.Cards
{
    [TestFixture]
    public class HandTests
    {
        // 普遍要件 HAND-001 / HAND-002 は sealed class + private readonly + IReadOnlyList で構造的に保証

        // ===== HAND-003: コンストラクタで Cards が同順序で保持される =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-003")]
        public void Given_有効なcards_When_コンストラクタ_Then_Cardsが入力と同じ順序で保持される()
        {
            // Given
            var a = CardId.Of("a");
            var b = CardId.Of("b");
            // When
            var hand = new Hand(new[] { a, b });
            // Then
            Assert.That(hand.Cards, Is.EqualTo(new[] { a, b }));
        }

        // ===== HAND-004: Empty シングルトン =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-004")]
        public void Given_HandEmpty_When_Count参照_Then_0()
        {
            Assert.That(Hand.Empty.Count, Is.EqualTo(0));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-004")]
        public void Given_HandEmpty_When_IsEmpty参照_Then_true()
        {
            Assert.That(Hand.Empty.IsEmpty, Is.True);
        }

        // ===== HAND-005: Add =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-005")]
        public void Given_既存にないCardId_When_Add_Then_末尾に追加された新Handが返る()
        {
            // Given
            var a = CardId.Of("a");
            var b = CardId.Of("b");
            var hand = new Hand(new[] { a });
            // When
            var next = hand.Add(b);
            // Then
            Assert.That(next.Cards, Is.EqualTo(new[] { a, b }));
        }

        // ===== HAND-006: Remove(中間 / 先頭 / 末尾)で Array.Copy の境界を網羅 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-006")]
        public void Given_存在するCardId_When_Remove_Then_除去された新Handが返り残りの順序が保たれる()
        {
            // Given: 中間要素を Remove(両 Array.Copy が動作)
            var a = CardId.Of("a");
            var b = CardId.Of("b");
            var c = CardId.Of("c");
            var hand = new Hand(new[] { a, b, c });
            // When
            var next = hand.Remove(b);
            // Then
            Assert.That(next.Cards, Is.EqualTo(new[] { a, c }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-006")]
        public void Given_先頭のCardId_When_Remove_Then_先頭が除去された新Handが返る()
        {
            // Given: 先頭 (index=0) Remove は前半 Array.Copy が長さ 0 の境界
            var a = CardId.Of("a");
            var b = CardId.Of("b");
            var c = CardId.Of("c");
            var hand = new Hand(new[] { a, b, c });
            // When
            var next = hand.Remove(a);
            // Then
            Assert.That(next.Cards, Is.EqualTo(new[] { b, c }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-006")]
        public void Given_末尾のCardId_When_Remove_Then_末尾が除去された新Handが返る()
        {
            // Given: 末尾 (index=len-1) Remove は後半 Array.Copy が長さ 0 の境界
            var a = CardId.Of("a");
            var b = CardId.Of("b");
            var c = CardId.Of("c");
            var hand = new Hand(new[] { a, b, c });
            // When
            var next = hand.Remove(c);
            // Then
            Assert.That(next.Cards, Is.EqualTo(new[] { a, b }));
        }

        // ===== HAND-007 / HAND-008: Contains =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-007")]
        public void Given_存在するCardId_When_Contains_Then_true()
        {
            // Given
            var a = CardId.Of("a");
            var hand = new Hand(new[] { a });
            // When
            var result = hand.Contains(a);
            // Then
            Assert.That(result, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-008")]
        public void Given_存在しないCardId_When_Contains_Then_false()
        {
            // Given
            var hand = new Hand(new[] { CardId.Of("a") });
            // When
            var result = hand.Contains(CardId.Of("z"));
            // Then
            Assert.That(result, Is.False);
        }

        // ===== HAND-009: Count =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-009")]
        public void Given_2枚のHand_When_Count_Then_2()
        {
            var hand = new Hand(new[] { CardId.Of("a"), CardId.Of("b") });
            Assert.That(hand.Count, Is.EqualTo(2));
        }

        // ===== HAND-010: IsEmpty =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-010")]
        public void Given_空Hand_When_IsEmpty_Then_true()
        {
            var hand = new Hand(Array.Empty<CardId>());
            Assert.That(hand.IsEmpty, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-010")]
        public void Given_1枚以上のHand_When_IsEmpty_Then_false()
        {
            var hand = new Hand(new[] { CardId.Of("a") });
            Assert.That(hand.IsEmpty, Is.False);
        }

        // ===== HAND-011: 順序付きシーケンス同値 (n=0/n=1/n=2 サイズ網羅 + 不一致 + ReferenceEquals + null) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-011")]
        public void Given_同順序同要素のHand_When_Equals_Then_等価()
        {
            var a = CardId.Of("a");
            var b = CardId.Of("b");
            var x = new Hand(new[] { a, b });
            var y = new Hand(new[] { a, b });
            Assert.That(x, Is.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-011")]
        public void Given_同枚数で異なる順序_When_Equals_Then_非等価()
        {
            var a = CardId.Of("a");
            var b = CardId.Of("b");
            var x = new Hand(new[] { a, b });
            var y = new Hand(new[] { b, a });
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-011")]
        public void Given_同枚数で異なるカード_When_Equals_Then_非等価()
        {
            var x = new Hand(new[] { CardId.Of("a"), CardId.Of("b") });
            var y = new Hand(new[] { CardId.Of("a"), CardId.Of("c") });
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-011")]
        public void Given_異なる枚数_When_Equals_Then_非等価()
        {
            var x = new Hand(new[] { CardId.Of("a") });
            var y = new Hand(new[] { CardId.Of("a"), CardId.Of("b") });
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-011")]
        public void Given_同一インスタンス_When_Equals_Then_等価()
        {
            var hand = new Hand(new[] { CardId.Of("a") });
            Assert.That(hand.Equals(hand), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-011")]
        public void Given_両方空Hand_When_Equals_Then_等価()
        {
            var x = new Hand(Array.Empty<CardId>());
            var y = new Hand(Array.Empty<CardId>());
            Assert.That(x, Is.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-011")]
        public void Given_null_When_EqualsHand_Then_false()
        {
            var hand = new Hand(new[] { CardId.Of("a") });
            Hand other = null;
            Assert.That(hand.Equals(other), Is.False);
        }

        // ===== HAND-012: GetHashCode =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-012")]
        public void Given_等価な2つのHand_When_GetHashCode_Then_同じ値を返す()
        {
            var a = CardId.Of("a");
            var b = CardId.Of("b");
            var x = new Hand(new[] { a, b });
            var y = new Hand(new[] { a, b });
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-012")]
        public void Given_両方空Hand_When_GetHashCode_Then_同じ値を返す()
        {
            // Given: n=0 のハッシュ一致(HashCode struct の初期状態が安定値を返すことを担保)
            var x = new Hand(Array.Empty<CardId>());
            var y = new Hand(Array.Empty<CardId>());
            // When / Then
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        // ===== HAND-013: operator== / operator!= =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-013")]
        public void Given_等価な2つのHand_When_operator_等価_Then_true()
        {
            var x = new Hand(new[] { CardId.Of("a") });
            var y = new Hand(new[] { CardId.Of("a") });
            Assert.That(x == y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-013")]
        public void Given_非等価な2つのHand_When_operator_等価_Then_false()
        {
            var x = new Hand(new[] { CardId.Of("a") });
            var y = new Hand(new[] { CardId.Of("b") });
            Assert.That(x == y, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-013")]
        public void Given_非等価な2つのHand_When_operator_非等価_Then_true()
        {
            var x = new Hand(new[] { CardId.Of("a") });
            var y = new Hand(new[] { CardId.Of("b") });
            Assert.That(x != y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-013")]
        public void Given_両方null_When_operator_等価_Then_true()
        {
            Hand x = null;
            Hand y = null;
            Assert.That(x == y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-013")]
        public void Given_片方nullで他方非null_When_operator_等価_Then_false()
        {
            // Given: 左側 null、右側 非 null
            Hand x = null;
            var y = new Hand(Array.Empty<CardId>());
            Assert.That(x == y, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-013")]
        public void Given_左側非nullで右側null_When_operator_等価_Then_false()
        {
            // Given: operator== 実装の `left.Equals(right)` 経路 (right=null) をカバー
            var x = new Hand(Array.Empty<CardId>());
            Hand y = null;
            Assert.That(x == y, Is.False);
        }

        // ===== HAND-014: Equals(object) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-014")]
        public void Given_null_When_Equalsオブジェクト_Then_false()
        {
            var hand = new Hand(Array.Empty<CardId>());
            Assert.That(hand.Equals((object)null), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "HAND-014")]
        public void Given_異なる型_When_Equalsオブジェクト_Then_false()
        {
            var hand = new Hand(Array.Empty<CardId>());
            Assert.That(hand.Equals((object)"not a Hand"), Is.False);
        }

        // ===== HAND-015 〜 HAND-021: 異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "HAND-015")]
        public void Given_null_When_コンストラクタ_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Hand(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "HAND-016")]
        public void Given_nullCardIdを含むcards_When_コンストラクタ_Then_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Hand(new[] { CardId.Of("a"), null }));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "HAND-017")]
        public void Given_重複CardIdを含むcards_When_コンストラクタ_Then_ArgumentException()
        {
            var a = CardId.Of("a");
            Assert.Throws<ArgumentException>(() => new Hand(new[] { a, a }));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "HAND-018")]
        public void Given_null_When_Add_Then_ArgumentNullException()
        {
            var hand = new Hand(Array.Empty<CardId>());
            Assert.Throws<ArgumentNullException>(() => hand.Add(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "HAND-019")]
        public void Given_既存CardId_When_Add_Then_ArgumentException()
        {
            var a = CardId.Of("a");
            var hand = new Hand(new[] { a });
            Assert.Throws<ArgumentException>(() => hand.Add(a));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "HAND-020")]
        public void Given_null_When_Remove_Then_ArgumentNullException()
        {
            var hand = new Hand(Array.Empty<CardId>());
            Assert.Throws<ArgumentNullException>(() => hand.Remove(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "HAND-021")]
        public void Given_不在CardId_When_Remove_Then_ArgumentException()
        {
            var hand = new Hand(new[] { CardId.Of("a") });
            Assert.Throws<ArgumentException>(() => hand.Remove(CardId.Of("z")));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "HAND-022")]
        public void Given_null_When_Contains_Then_ArgumentNullException()
        {
            // Given: Add/Remove 経由ではなく Contains を直接呼ぶケース
            var hand = new Hand(new[] { CardId.Of("a") });
            // When / Then
            Assert.Throws<ArgumentNullException>(() => hand.Contains(null));
        }
    }
}
