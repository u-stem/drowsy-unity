using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Tests.Catalog
{
    [TestFixture]
    public class InMemoryCardCatalogTests
    {
        // ===== ヘルパー =====

        private static CardData NewData(string name) =>
            new CardData(name, new Dictionary<string, int>());

        private static KeyValuePair<CardId, CardData> Entry(string id, CardData data) =>
            new KeyValuePair<CardId, CardData>(CardId.Of(id), data);

        // ===== APP-012: 防御コピー =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-012")]
        public void Given_有効なentries_When_InMemoryCardCatalogを生成_Then_元entries変更後も内部状態は不変()
        {
            // Given
            var data = NewData("X-card");
            var entries = new Dictionary<CardId, CardData>
            {
                [CardId.Of("X")] = data,
            };
            var catalog = new InMemoryCardCatalog(entries);
            // When(元 entries から削除)
            entries.Remove(CardId.Of("X"));
            // Then(catalog 側は元のまま Get できる = 防御コピーされている)
            Assert.That(catalog.Get(CardId.Of("X")), Is.SameAs(data));
        }

        // ===== APP-013 / APP-014: Get 正常系 / 異常系 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-013")]
        public void Given_登録済CardId_When_Getを呼ぶ_Then_対応CardDataが返る()
        {
            // Given
            var data = NewData("X-card");
            var catalog = new InMemoryCardCatalog(new[] { Entry("X", data) });
            // When
            var got = catalog.Get(CardId.Of("X"));
            // Then
            Assert.That(got, Is.SameAs(data));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-014")]
        public void Given_未登録CardId_When_Getを呼ぶ_Then_KeyNotFoundExceptionを投げる()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]);
            // When / Then
            Assert.Throws<KeyNotFoundException>(() => catalog.Get(CardId.Of("Y")));
        }

        // ===== APP-015 〜 APP-018: TryGet 4 系統 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-015")]
        public void Given_登録済CardId_When_TryGetを呼ぶ_Then_trueを返す()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new[] { Entry("X", NewData("X-card")) });
            // When
            var ok = catalog.TryGet(CardId.Of("X"), out _);
            // Then
            Assert.That(ok, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-016")]
        public void Given_登録済CardId_When_TryGetを呼ぶ_Then_dataに対応CardDataが設定される()
        {
            // Given
            var data = NewData("X-card");
            var catalog = new InMemoryCardCatalog(new[] { Entry("X", data) });
            // When
            catalog.TryGet(CardId.Of("X"), out var got);
            // Then
            Assert.That(got, Is.SameAs(data));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-017")]
        public void Given_未登録CardId_When_TryGetを呼ぶ_Then_falseを返す()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]);
            // When
            var ok = catalog.TryGet(CardId.Of("Y"), out _);
            // Then
            Assert.That(ok, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-018")]
        public void Given_未登録CardId_When_TryGetを呼ぶ_Then_dataにnullが設定される()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]);
            // When
            catalog.TryGet(CardId.Of("Y"), out var got);
            // Then
            Assert.That(got, Is.Null);
        }

        // ===== APP-019 / APP-020: コンストラクタ異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-019")]
        public void Given_entriesにnull_When_InMemoryCardCatalogを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() => new InMemoryCardCatalog(null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-020")]
        public void Given_entriesにnullCardData_When_InMemoryCardCatalogを生成_Then_ArgumentExceptionを投げる()
        {
            // Given(null CardData を含む entries)
            var entries = new[] { Entry("X", null) };
            // When / Then
            Assert.Throws<ArgumentException>(() => new InMemoryCardCatalog(entries));
        }
    }
}
