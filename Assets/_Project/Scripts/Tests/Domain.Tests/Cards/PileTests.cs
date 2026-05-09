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
        private static CardId Card(string value) => CardId.Of(value);

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-005")]
        public void Given_空でない山札_When_AddTop_Then_先頭に挿入された新Pileを返す()
        {
            // Given
            var pile = new Pile(new[] { Card("B"), Card("C") });
            // When
            var added = pile.AddTop(Card("A"));
            // Then
            Assert.That(added.Cards.Select(c => c.Value), Is.EqualTo(new[] { "A", "B", "C" }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-001")]
        public void Given_AddTop呼び出し_When_実行後_Then_元のPileは変更されない()
        {
            var pile = new Pile(new[] { Card("B"), Card("C") });
            _ = pile.AddTop(Card("A"));
            Assert.That(pile.Cards.Select(c => c.Value), Is.EqualTo(new[] { "B", "C" }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PILE-006")]
        public void Given_空でない山札_When_AddBottom_Then_末尾に追加された新Pileを返す()
        {
            var pile = new Pile(new[] { Card("A"), Card("B") });
            var added = pile.AddBottom(Card("C"));
            Assert.That(added.Cards.Select(c => c.Value), Is.EqualTo(new[] { "A", "B", "C" }));
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
            Assert.That(remaining.Cards.Select(c => c.Value), Is.EqualTo(new[] { "B", "C" }));
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
    }
}
