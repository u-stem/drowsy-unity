using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards
{
    /// <summary>
    /// カード No.05「喧騒を纏う」(2026-05-17)の SO 表現と InMemory 表現の同値性検証
    /// (INF-140)。<see cref="TimeOfDayBranchEffectAsset"/> + <see cref="StackHandCardOnDeckTopEffectAsset"/>
    /// の最上位 2 件構成の ToDomain で <see cref="TimeOfDayBranchEffect"/> +
    /// <see cref="StackHandCardOnDeckTopEffect"/> を record 値同値で再構築できることを検証する。
    /// </summary>
    [TestFixture]
    public sealed class CommotionCardCatalogTests
    {
        // ===== ヘルパー: SO 経由 catalog =====

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardFive()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "05",
                name: "喧騒を纏う",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new TimeOfDayBranchEffectAsset(
                        nightEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -8),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -18),
                        },
                        morningEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 12),
                        }),
                    new StackHandCardOnDeckTopEffectAsset(SdpTarget.Self),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        // ===== ヘルパー: InMemory 経由 catalog(Application.Tests `CommotionCardTests` と同仕様で再実装)=====

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardFive()
        {
            var card05 = new CardData("喧騒を纏う", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("05"), card05),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("05"),
                    new IEffect[]
                    {
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -8),
                                new AdjustSdpEffect(SdpTarget.Opponent, -18),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new AdjustSdpEffect(SdpTarget.Opponent, 12),
                            }),
                        new StackHandCardOnDeckTopEffect(SdpTarget.Self),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // ===== INF-140: No.05 の SO ↔ InMemory 同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-140")]
        public void Given_No05_SO_When_GetName_Then_InMemoryと一致()
        {
            // Given
            var so = NewSoCatalogWithCardFive();
            var inMemory = NewInMemoryCatalogWithCardFive();
            // When
            var soName = so.Get(CardTypeId.Of("05")).Name;
            var inMemoryName = inMemory.Get(CardTypeId.Of("05")).Name;
            // Then
            Assert.That(soName, Is.EqualTo(inMemoryName));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-140")]
        public void Given_No05_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            // Given(最上位 2 件 + TimeOfDayBranchEffect 内夜/朝各 2 effect)
            var so = NewSoCatalogWithCardFive();
            var inMemory = NewInMemoryCatalogWithCardFive();
            // When
            var soEffects = so.GetEffects(CardTypeId.Of("05"));
            var inMemoryEffects = inMemory.GetEffects(CardTypeId.Of("05"));
            // Then(record 値同値、StackHandCardOnDeckTopEffect auto-equals 含む)
            Assert.That(soEffects, Is.EqualTo(inMemoryEffects));
        }
    }
}
