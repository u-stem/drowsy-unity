using System;
using System.Linq;
using NUnit.Framework;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Random;

namespace Drowsy.Domain.Tests.Cards
{
    [TestFixture]
    public class PileTests
    {
        // テストヘルパー: 短い記法で CardId を作る
        private static CardId Card(string value) => CardId.Of(CardTypeId.Of(value), 0);

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-005")]
        public void Given_空でない山札_When_AddTop_Then_先頭に挿入された新Pileを返す()
        {
            // Given
            var pile = new Pile(new[] { Card("B"), Card("C") });
            // When
            var added = pile.AddTop(Card("A"));
            // Then
            Assert.That(added.Cards.Select(c => c.TypeId.Value), Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-001")]
        public void Given_AddTop呼び出し_When_実行後_Then_元のPileは変更されない()
        {
            var pile = new Pile(new[] { Card("B"), Card("C") });
            _ = pile.AddTop(Card("A"));
            Assert.That(pile.Cards.Select(c => c.TypeId.Value), Is.EqualTo(new[] { "B", "C" }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-006")]
        public void Given_空でない山札_When_AddBottom_Then_末尾に追加された新Pileを返す()
        {
            var pile = new Pile(new[] { Card("A"), Card("B") });
            var added = pile.AddBottom(Card("C"));
            Assert.That(added.Cards.Select(c => c.TypeId.Value), Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-007")]
        public void Given_空でない山札_When_Draw_Then_先頭カードと残りPileを返す()
        {
            // Given
            var pile = new Pile(new[] { Card("A"), Card("B"), Card("C") });
            // When
            var (drawn, remaining) = pile.Draw();
            // Then
            Assert.That(drawn, Is.EqualTo(Card("A")));
            Assert.That(remaining.Cards.Select(c => c.TypeId.Value), Is.EqualTo(new[] { "B", "C" }));
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "PILE-007")]
        public void Given_1枚のみの山札_When_Draw_Then_引いたカードと空Pileを返す()
        {
            var pile = new Pile(new[] { Card("A") });
            var (drawn, remaining) = pile.Draw();
            Assert.That(drawn, Is.EqualTo(Card("A")));
            Assert.That(remaining.IsEmpty, Is.True);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PILE-009")]
        public void Given_空の山札_When_Draw_Then_InvalidOperationExceptionを投げる()
        {
            Assert.Throws<InvalidOperationException>(() => Pile.Empty.Draw());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PILE-010")]
        public void Given_AddTopにnull_When_実行_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Pile.Empty.AddTop(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PILE-011")]
        public void Given_AddBottomにnull_When_実行_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Pile.Empty.AddBottom(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PILE-013")]
        public void Given_コンストラクタにnull_When_生成_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Pile(null));
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "PILE-008")]
        public void Given_同じシードのRandom_When_Shuffle_Then_並びは決定的に同じ()
        {
            // Given
            var pile = new Pile(new[] { Card("A"), Card("B"), Card("C"), Card("D"), Card("E") });
            var rng1 = new XorShiftRandom(42);
            var rng2 = new XorShiftRandom(42);
            // When
            var s1 = pile.Shuffle(rng1);
            var s2 = pile.Shuffle(rng2);
            // Then
            Assert.That(s1.Cards, Is.EqualTo(s2.Cards));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-008")]
        public void Given_山札_When_Shuffle_Then_要素集合は元と同じ()
        {
            var original = new[] { Card("A"), Card("B"), Card("C"), Card("D"), Card("E") };
            var pile = new Pile(original);
            var shuffled = pile.Shuffle(new XorShiftRandom(7));
            Assert.That(
                shuffled.Cards.OrderBy(c => c.Value),
                Is.EqualTo(original.OrderBy(c => c.Value)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PILE-012")]
        public void Given_Shuffleにnull_When_実行_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Pile.Empty.Shuffle(null));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-003"), Property("Requirement", "PILE-004")]
        public void Given_Pile_Empty_When_IsEmptyとCountを確認_Then_trueと0()
        {
            Assert.That(Pile.Empty.IsEmpty, Is.True);
            Assert.That(Pile.Empty.Count, Is.EqualTo(0));
        }

        // ===== PILE-014: 順序付きシーケンス同値 (n=0/n=1/n=2 サイズ網羅 + 不一致 + ReferenceEquals + null) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-014")]
        public void Given_同順序同要素のPile_When_Equals_Then_等価()
        {
            var x = new Pile(new[] { Card("a"), Card("b") });
            var y = new Pile(new[] { Card("a"), Card("b") });
            Assert.That(x, Is.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-014")]
        public void Given_同枚数で異なる順序のPile_When_Equals_Then_非等価()
        {
            var x = new Pile(new[] { Card("a"), Card("b") });
            var y = new Pile(new[] { Card("b"), Card("a") });
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-014")]
        public void Given_同枚数で異なるカードのPile_When_Equals_Then_非等価()
        {
            var x = new Pile(new[] { Card("a"), Card("b") });
            var y = new Pile(new[] { Card("a"), Card("c") });
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-014")]
        public void Given_異なる枚数のPile_When_Equals_Then_非等価()
        {
            var x = new Pile(new[] { Card("a") });
            var y = new Pile(new[] { Card("a"), Card("b") });
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-014")]
        public void Given_同一インスタンスのPile_When_Equals_Then_等価()
        {
            var pile = new Pile(new[] { Card("a") });
            Assert.That(pile.Equals(pile), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-014")]
        public void Given_両方空Pile_When_Equals_Then_等価()
        {
            // Given: n=0 で別インスタンス同士が Equals 一致する(Hand 対称)
            var x = new Pile(Array.Empty<CardId>());
            var y = new Pile(Array.Empty<CardId>());
            Assert.That(x, Is.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-014")]
        public void Given_null_When_EqualsPile_Then_false()
        {
            var pile = new Pile(new[] { Card("a") });
            Pile other = null;
            Assert.That(pile.Equals(other), Is.False);
        }

        // ===== PILE-015: GetHashCode =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-015")]
        public void Given_等価な2つのPile_When_GetHashCode_Then_同じ値を返す()
        {
            var x = new Pile(new[] { Card("a"), Card("b") });
            var y = new Pile(new[] { Card("a"), Card("b") });
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-015")]
        public void Given_両方空Pile_When_GetHashCode_Then_同じ値を返す()
        {
            // Given: n=0 のハッシュ一致(HashCode struct の初期状態が安定値を返すことを担保、Hand 対称)
            var x = new Pile(Array.Empty<CardId>());
            var y = new Pile(Array.Empty<CardId>());
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        // ===== PILE-016: operator== / operator!= =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-016")]
        public void Given_等価な2つのPile_When_operator_等価_Then_true()
        {
            var x = new Pile(new[] { Card("a") });
            var y = new Pile(new[] { Card("a") });
            Assert.That(x == y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-016")]
        public void Given_非等価な2つのPile_When_operator_等価_Then_false()
        {
            var x = new Pile(new[] { Card("a") });
            var y = new Pile(new[] { Card("b") });
            Assert.That(x == y, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-016")]
        public void Given_非等価な2つのPile_When_operator_非等価_Then_true()
        {
            var x = new Pile(new[] { Card("a") });
            var y = new Pile(new[] { Card("b") });
            Assert.That(x != y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-016")]
        public void Given_両方null_When_operator_等価_Then_true()
        {
            Pile x = null;
            Pile y = null;
            Assert.That(x == y, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-016")]
        public void Given_片方nullで他方非null_When_operator_等価_Then_false()
        {
            // Given: 左側 null、右側 非 null(operator== 実装の `left is null ? right is null` の右側 false 経路)
            Pile x = null;
            var y = Pile.Empty;
            Assert.That(x == y, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-016")]
        public void Given_左側非nullで右側null_When_operator_等価_Then_false()
        {
            // Given: operator== 実装の `left.Equals(right)` 経路 (right=null) をカバー
            var x = Pile.Empty;
            Pile y = null;
            Assert.That(x == y, Is.False);
        }

        // ===== PILE-017: Equals(object) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-017")]
        public void Given_null_When_PileEqualsオブジェクト_Then_false()
        {
            var pile = Pile.Empty;
            Assert.That(pile.Equals((object)null), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-017")]
        public void Given_異なる型_When_PileEqualsオブジェクト_Then_false()
        {
            var pile = Pile.Empty;
            Assert.That(pile.Equals((object)"not a Pile"), Is.False);
        }
    }
}
