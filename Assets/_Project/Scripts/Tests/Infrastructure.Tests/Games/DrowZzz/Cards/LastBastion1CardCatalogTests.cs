using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using NUnit.Framework;
using UnityEngine;
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards
{
    /// <summary>
    /// カード No.13「最後の砦Ⅰ」(2026-05-17)の SO 表現と InMemory 表現の同値性検証(INF-156)。
    /// </summary>
    [TestFixture]
    public sealed class LastBastion1CardCatalogTests
    {
        private static ScriptableObjectCardCatalog NewSoCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "13",
                name: "最後の砦Ⅰ",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new ChoiceEffectAsset(new[]
                    {
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, 6),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -10),
                        }),
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -10),
                            new AssociateSpecificCardEffectAsset("14"),
                        }),
                    }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalog()
        {
            var card = new CardData("最後の砦Ⅰ", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("13"), card),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("13"),
                    new IEffect[]
                    {
                        new ChoiceEffect(new IReadOnlyList<IEffect>[]
                        {
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, 6),
                                new AdjustSdpEffect(SdpTarget.Opponent, -10),
                            },
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new AdjustSdpEffect(SdpTarget.Opponent, -10),
                                new AssociateSpecificCardEffect(CardTypeId.Of("14")),
                            },
                        }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-156")]
        public void Given_No13_SO_When_GetName_Then_InMemoryと一致()
        {
            Assert.That(NewSoCatalog().Get(CardTypeId.Of("13")).Name, Is.EqualTo(NewInMemoryCatalog().Get(CardTypeId.Of("13")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-156")]
        public void Given_No13_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            Assert.That(NewSoCatalog().GetEffects(CardTypeId.Of("13")), Is.EqualTo(NewInMemoryCatalog().GetEffects(CardTypeId.Of("13"))));
        }
    }
}
