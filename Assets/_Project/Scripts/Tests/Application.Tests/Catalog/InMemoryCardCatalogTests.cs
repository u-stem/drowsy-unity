using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Tests.Catalog
{
    [TestFixture]
    public sealed class InMemoryCardCatalogTests
    {
        // ===== ヘルパー =====

        private static CardData NewData(string name) =>
            new CardData(name, new Dictionary<string, int>());

        private static KeyValuePair<CardTypeId, CardData> Entry(string id, CardData data) =>
            new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of(id), data);

        // ===== APP-012: 防御コピー =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-012")]
        public void Given_有効なentries_When_InMemoryCardCatalogを生成_Then_元entries変更後も内部状態は不変()
        {
            // Given
            var data = NewData("X-card");
            var entries = new Dictionary<CardTypeId, CardData>
            {
                [CardTypeId.Of("X")] = data,
            };
            var catalog = new InMemoryCardCatalog(entries);
            // When(元 entries から削除)
            entries.Remove(CardTypeId.Of("X"));
            // Then(catalog 側は元のまま Get できる = 防御コピーされている)
            Assert.That(catalog.Get(CardTypeId.Of("X")), Is.SameAs(data));
        }

        // ===== APP-013 / APP-014: Get 正常系 / 異常系 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-013")]
        public void Given_登録済CardId_When_Getを呼ぶ_Then_対応CardDataが返る()
        {
            // Given
            var data = NewData("X-card");
            var catalog = new InMemoryCardCatalog(new[] { Entry("X", data) });
            // When
            var got = catalog.Get(CardTypeId.Of("X"));
            // Then
            Assert.That(got, Is.SameAs(data));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-014")]
        public void Given_未登録CardId_When_Getを呼ぶ_Then_KeyNotFoundExceptionを投げる()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardTypeId, CardData>[0]);
            // When / Then
            Assert.Throws<KeyNotFoundException>(() => catalog.Get(CardTypeId.Of("Y")));
        }

        // ===== APP-015 〜 APP-018: TryGet 4 系統 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-015")]
        public void Given_登録済CardId_When_TryGetを呼ぶ_Then_trueを返す()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new[] { Entry("X", NewData("X-card")) });
            // When
            var ok = catalog.TryGet(CardTypeId.Of("X"), out _);
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
            catalog.TryGet(CardTypeId.Of("X"), out var got);
            // Then
            Assert.That(got, Is.SameAs(data));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-017")]
        public void Given_未登録CardId_When_TryGetを呼ぶ_Then_falseを返す()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardTypeId, CardData>[0]);
            // When
            var ok = catalog.TryGet(CardTypeId.Of("Y"), out _);
            // Then
            Assert.That(ok, Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-018")]
        public void Given_未登録CardId_When_TryGetを呼ぶ_Then_dataにnullが設定される()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardTypeId, CardData>[0]);
            // When
            catalog.TryGet(CardTypeId.Of("Y"), out var got);
            // Then
            Assert.That(got, Is.Null);
        }

        // ===== APP-019 / APP-020: コンストラクタ異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-019")]
        public void Given_entriesにnull_When_InMemoryCardCatalogを生成_Then_ArgumentNullExceptionを投げる()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new InMemoryCardCatalog(null));
            Assert.That(ex!.ParamName, Is.EqualTo("entries"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-020")]
        public void Given_entriesにnullCardData_When_InMemoryCardCatalogを生成_Then_ArgumentExceptionを投げる()
        {
            // Given(null CardData を含む entries)
            var entries = new[] { Entry("X", null) };
            // When / Then
            Assert.Throws<ArgumentException>(() => new InMemoryCardCatalog(entries));
        }

        // ===== APP-037 / APP-038: GetEffects は M2-PR1 段階では常に空配列を返す =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-037")]
        public void Given_登録済CardId_When_GetEffectsを呼ぶ_Then_空配列を返す()
        {
            // Given(M2-PR1 段階では効果定義がないため、登録済でも空配列が返る)
            var catalog = new InMemoryCardCatalog(new[] { Entry("X", NewData("X-card")) });
            // When
            var effects = catalog.GetEffects(CardTypeId.Of("X"));
            // Then(IsEmpty で空であることを確認、IReadOnlyList<IEffect> の Count == 0)
            Assert.That(effects, Is.Empty);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-038")]
        public void Given_未登録CardId_When_GetEffectsを呼ぶ_Then_例外を投げず空配列を返す()
        {
            // Given(未登録 CardId に対して KeyNotFoundException を投げない契約、Aggregate 0 個ループが自然)
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardTypeId, CardData>[0]);
            // When
            var effects = catalog.GetEffects(CardTypeId.Of("Y"));
            // Then
            Assert.That(effects, Is.Empty);
        }

        // ===== APP-040: 2 段 constructor で効果列を登録した CardId に対して効果列を返す(M2-PR3)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-040")]
        public void Given_2段constructorで効果列を登録_When_GetEffectsを呼ぶ_Then_登録効果列を返す()
        {
            // Given(2 段 constructor で CardId "X" に効果列を登録)
            var entries = new[] { Entry("X", NewData("X-card")) };
            var registeredEffects = new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 5) };
            var effectsDict = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("X"), registeredEffects),
            };
            var catalog = new InMemoryCardCatalog(entries, effectsDict);
            // When
            var got = catalog.GetEffects(CardTypeId.Of("X"));
            // Then
            Assert.That(got, Is.EqualTo(registeredEffects));
        }

        // ===== APP-061: GetEffects(null) は ArgumentNullException(App W-3 post-Phase2 レビュー反映)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-061")]
        public void Given_nullCardTypeId_When_GetEffectsを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardTypeId, CardData>[0]);
            // When / Then(ICardCatalog 契約上 typeId は non-null、サイレント空列返却ではなく呼び出し側のバグ検出を優先)
            var ex = Assert.Throws<ArgumentNullException>(() => catalog.GetEffects(null));
            Assert.That(ex!.ParamName, Is.EqualTo("typeId"));
        }
    }
}
