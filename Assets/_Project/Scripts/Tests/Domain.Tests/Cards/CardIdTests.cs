using System;
using NUnit.Framework;
using Drowsy.Domain.Cards;

namespace Drowsy.Domain.Tests.Cards
{
    /// <summary>
    /// <see cref="CardId"/>(ADR-0018 で `(CardTypeId TypeId, int Instance)` 複合 record に refactor 済)の単体テスト。
    /// 旧 Phase 1 設計の <c>CardId.Of(string)</c> 単純文字列 API は削除されたため、本 fixture は新 API に対応した
    /// テスト集合に全面差し替え(ADR-0018 §2)。
    /// </summary>
    [TestFixture]
    public class CardIdTests
    {
        // ===== CARD-004: 有効な (typeId, instance) で Of を呼ぶと TypeId / Instance / Value が保持される =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CARD-004")]
        public void Given_有効なtypeIdとinstance_When_Ofを呼ぶ_Then_TypeIdとInstanceが保持される()
        {
            // Given
            var typeId = CardTypeId.Of("dream");
            // When
            var id = CardId.Of(typeId, 3);
            // Then
            Assert.That(id.TypeId, Is.EqualTo(typeId));
            Assert.That(id.Instance, Is.EqualTo(3));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CARD-004")]
        public void Given_有効なtypeIdとinstance_When_Ofを呼ぶ_Then_Valueは_typeIdとinstanceを_で連結した形式()
        {
            // Given
            var typeId = CardTypeId.Of("dream");
            // When
            var id = CardId.Of(typeId, 3);
            // Then
            Assert.That(id.Value, Is.EqualTo("dream#3"));
        }

        // ===== CARD-006: null typeId は ArgumentNullException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CARD-006")]
        public void Given_typeIdがnull_When_Ofを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() => CardId.Of(null, 0));
        }

        // ===== CARD-007: instance 負数は ArgumentOutOfRangeException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "CARD-007")]
        public void Given_instanceが負数_When_Ofを呼ぶ_Then_ArgumentOutOfRangeExceptionを投げる()
        {
            // Given
            var typeId = CardTypeId.Of("dream");
            // When / Then
            Assert.Throws<ArgumentOutOfRangeException>(() => CardId.Of(typeId, -1));
        }

        // ===== CARD-003: 同じ (typeId, instance) の 2 つの CardId は等価 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CARD-003")]
        public void Given_同じtypeIdとinstanceの2つのCardId_When_等価比較_Then_等価()
        {
            // Given
            var typeId = CardTypeId.Of("dream");
            var a = CardId.Of(typeId, 0);
            var b = CardId.Of(typeId, 0);
            // When / Then(record 自動生成の等価)
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        // ===== CARD-005: 異なる typeId / 異なる instance なら非等価 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CARD-005")]
        public void Given_異なるtypeIdの2つのCardId_When_等価比較_Then_非等価()
        {
            // Given
            var a = CardId.Of(CardTypeId.Of("dream"), 0);
            var b = CardId.Of(CardTypeId.Of("sheep"), 0);
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CARD-005")]
        public void Given_同じtypeIdで異なるinstanceの2つのCardId_When_等価比較_Then_非等価()
        {
            // Given
            var typeId = CardTypeId.Of("dream");
            var a = CardId.Of(typeId, 0);
            var b = CardId.Of(typeId, 1);
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }

        // ===== CARD-009: ToString は Value と同じ =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "CARD-009")]
        public void Given_CardId_When_ToStringを呼ぶ_Then_Valueと同じ文字列を返す()
        {
            // Given
            var id = CardId.Of(CardTypeId.Of("dream"), 5);
            // When / Then
            Assert.That(id.ToString(), Is.EqualTo("dream#5"));
            Assert.That(id.ToString(), Is.EqualTo(id.Value));
        }
    }
}
