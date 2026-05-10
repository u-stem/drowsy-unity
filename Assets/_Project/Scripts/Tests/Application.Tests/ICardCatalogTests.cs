using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Tests
{
    [TestFixture]
    public class ICardCatalogTests
    {
        // ICardCatalog のダミー実装。M1-PR2 で本物 (InMemoryCardCatalog) が来るまでの契約検証用。
        private sealed class DummyCatalog : ICardCatalog
        {
            private readonly Dictionary<CardId, CardData> _store = new Dictionary<CardId, CardData>();

            public void Register(CardId id, CardData data) => _store[id] = data;

            public CardData Get(CardId id) => _store[id];

            public bool TryGet(CardId id, out CardData data) => _store.TryGetValue(id, out data);
        }

        private static CardData NewData(string name) =>
            new CardData(name, new Dictionary<string, int>());

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-007")]
        public void Given_登録済CardId_When_Getを呼ぶ_Then_対応するCardDataが返る()
        {
            // Given
            var catalog = new DummyCatalog();
            var id = CardId.Of("X");
            var data = NewData("X-card");
            catalog.Register(id, data);
            // When
            var got = catalog.Get(id);
            // Then
            Assert.That(got, Is.SameAs(data));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-008")]
        public void Given_登録済CardId_When_TryGetを呼ぶ_Then_trueを返す()
        {
            // Given
            var catalog = new DummyCatalog();
            var id = CardId.Of("X");
            catalog.Register(id, NewData("X-card"));
            // When
            var ok = catalog.TryGet(id, out _);
            // Then
            Assert.That(ok, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-008")]
        public void Given_登録済CardId_When_TryGetを呼ぶ_Then_dataに対応CardDataが設定される()
        {
            // Given
            var catalog = new DummyCatalog();
            var id = CardId.Of("X");
            var data = NewData("X-card");
            catalog.Register(id, data);
            // When
            catalog.TryGet(id, out var got);
            // Then
            Assert.That(got, Is.SameAs(data));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-009")]
        public void Given_未登録CardId_When_TryGetを呼ぶ_Then_falseを返す()
        {
            // Given
            var catalog = new DummyCatalog();
            // When
            var ok = catalog.TryGet(CardId.Of("Y"), out _);
            // Then
            Assert.That(ok, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-009")]
        public void Given_未登録CardId_When_TryGetを呼ぶ_Then_dataにnullが設定される()
        {
            // Given
            var catalog = new DummyCatalog();
            // When
            catalog.TryGet(CardId.Of("Y"), out var got);
            // Then
            Assert.That(got, Is.Null);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-010")]
        public void Given_未登録CardId_When_Getを呼ぶ_Then_KeyNotFoundExceptionを投げる()
        {
            // Given
            var catalog = new DummyCatalog();
            // When / Then
            Assert.Throws<KeyNotFoundException>(() => catalog.Get(CardId.Of("Y")));
        }
    }
}
