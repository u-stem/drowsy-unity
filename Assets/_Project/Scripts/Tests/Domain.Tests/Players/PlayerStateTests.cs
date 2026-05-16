using System;
using NUnit.Framework;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Players;

namespace Drowsy.Domain.Tests.Players
{
    [TestFixture]
    public class PlayerStateTests
    {
        // 普遍要件 PLAYER-009 / PLAYER-010 は record class + init-only プロパティで構造的に保証

        // ===== PLAYER-011: コンストラクタで各プロパティが入力通り保持される(1 テスト 1 アサーション) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-011")]
        public void Given_有効なIdとHand_When_コンストラクタ_Then_Idが入力と同じ()
        {
            // Given
            var id = PlayerId.Of("p1");
            // When
            var state = new PlayerState(id, Hand.Empty);
            // Then
            Assert.That(state.Id, Is.EqualTo(id));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-011")]
        public void Given_有効なIdとHand_When_コンストラクタ_Then_Handが入力と同じ()
        {
            // Given: Hand.Equals(Hand) で値同値比較される
            var hand = new Hand(new[] { CardId.Of(CardTypeId.Of("a"), 0) });
            // When
            var state = new PlayerState(PlayerId.Of("p1"), hand);
            // Then
            Assert.That(state.Hand, Is.EqualTo(hand));
        }

        // ===== PLAYER-012: 値同値性(N=1 同値 / Id 異 / Hand 異 / N=2 独立性)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-012")]
        public void Given_同じIdと同じHandの2つのPlayerState_When_Equals_Then_等価()
        {
            var x = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            var y = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            Assert.That(x, Is.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-012")]
        public void Given_異なるId_When_Equals_Then_非等価()
        {
            var x = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            var y = new PlayerState(PlayerId.Of("p2"), Hand.Empty);
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-012")]
        public void Given_異なるHand_When_Equals_Then_非等価()
        {
            var id = PlayerId.Of("p1");
            var x = new PlayerState(id, Hand.Empty);
            var y = new PlayerState(id, new Hand(new[] { CardId.Of(CardTypeId.Of("a"), 0) }));
            Assert.That(x, Is.Not.EqualTo(y));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-012")]
        public void Given_独立した2人のPlayerState_When_Equals_Then_非等価()
        {
            // Given: N=2 想定 - 別々の Player が独立して保持される
            var p1 = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            var p2 = new PlayerState(PlayerId.Of("p2"), Hand.Empty);
            // When / Then: 異なるプレイヤーは値同値として非等価
            Assert.That(p1, Is.Not.EqualTo(p2));
        }

        // ===== PLAYER-013: GetHashCode =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-013")]
        public void Given_等価な2つのPlayerState_When_GetHashCode_Then_同じ値を返す()
        {
            var x = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            var y = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            Assert.That(x.GetHashCode(), Is.EqualTo(y.GetHashCode()));
        }

        // ===== PLAYER-014: with 式で Hand を差し替え(1 テスト 1 アサーションで 3 観点に分割) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-014")]
        public void Given_PlayerState_When_with式でHandを差し替え_Then_新インスタンスのIdは不変()
        {
            // Given
            var original = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            // When
            var updated = original with { Hand = new Hand(new[] { CardId.Of(CardTypeId.Of("a"), 0) }) };
            // Then
            Assert.That(updated.Id, Is.EqualTo(original.Id));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-014")]
        public void Given_PlayerState_When_with式でHandを差し替え_Then_新インスタンスのHandが差し替え済み()
        {
            // Given
            var original = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            var newHand = new Hand(new[] { CardId.Of(CardTypeId.Of("a"), 0) });
            // When
            var updated = original with { Hand = newHand };
            // Then
            Assert.That(updated.Hand, Is.EqualTo(newHand));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PLAYER-014")]
        public void Given_PlayerState_When_with式でHandを差し替え_Then_元インスタンスのHandは不変()
        {
            // Given
            var original = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            // When
            _ = original with { Hand = new Hand(new[] { CardId.Of(CardTypeId.Of("a"), 0) }) };
            // Then
            Assert.That(original.Hand, Is.EqualTo(Hand.Empty));
        }

        // ===== PLAYER-015 / PLAYER-016: コンストラクタの異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PLAYER-015")]
        public void Given_nullId_When_コンストラクタ_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerState(null, Hand.Empty));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PLAYER-016")]
        public void Given_nullHand_When_コンストラクタ_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerState(PlayerId.Of("p1"), null));
        }

        // ===== PLAYER-017 / PLAYER-018: with 式での null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PLAYER-017")]
        public void Given_PlayerState_When_with式でIdをnullに_Then_ArgumentNullException()
        {
            // Given: init setter の null 防御が with 式経由でも効くことを確認
            var state = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            // When / Then
            Assert.Throws<ArgumentNullException>(() => _ = state with { Id = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PLAYER-018")]
        public void Given_PlayerState_When_with式でHandをnullに_Then_ArgumentNullException()
        {
            // Given
            var state = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            // When / Then
            Assert.Throws<ArgumentNullException>(() => _ = state with { Hand = null });
        }
    }
}
