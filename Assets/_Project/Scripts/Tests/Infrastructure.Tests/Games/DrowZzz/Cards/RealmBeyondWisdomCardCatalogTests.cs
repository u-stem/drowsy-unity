using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards
{
    /// <summary>
    /// カード No.07「知恵の及ばぬ領域」(2026-05-17)の SO 表現と InMemory 表現の同値性検証(INF-144)。
    /// </summary>
    [TestFixture]
    public sealed class RealmBeyondWisdomCardCatalogTests
    {
        private static PlayerInfluence RestrictWisdomDomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictSpecificCardInfluenceEffect(CardTypeId.Of("08")),
                4);

        private static PlayerInfluenceAsset RestrictWisdomSoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictSpecificCardInfluenceEffectAsset("08"),
                4);

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardSeven()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "07",
                name: "知恵の及ばぬ領域",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new AdjustSdpEffectAsset(SdpTarget.Self, -6),
                    new AdjustSdpEffectAsset(SdpTarget.Opponent, 5),
                    new RemoveInvertBedDamageInfluenceEffectAsset(SdpTarget.Opponent),
                    new KeywordedEffectAsset(
                        new[] { Keyword.Frenzy },
                        new ApplyInfluenceEffectAsset(SdpTarget.Opponent, RestrictWisdomSoInfluence())),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardSeven()
        {
            var card07 = new CardData("知恵の及ばぬ領域", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("07"), card07),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("07"),
                    new IEffect[]
                    {
                        new AdjustSdpEffect(SdpTarget.Self, -6),
                        new AdjustSdpEffect(SdpTarget.Opponent, 5),
                        new RemoveInvertBedDamageInfluenceEffect(SdpTarget.Opponent),
                        new KeywordedEffect(new[] { Keyword.Frenzy },
                            new ApplyInfluenceEffect(SdpTarget.Opponent, RestrictWisdomDomainInfluence())),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-144")]
        public void Given_No07_SO_When_GetName_Then_InMemoryと一致()
        {
            var so = NewSoCatalogWithCardSeven();
            var inMemory = NewInMemoryCatalogWithCardSeven();
            Assert.That(so.Get(CardTypeId.Of("07")).Name, Is.EqualTo(inMemory.Get(CardTypeId.Of("07")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-144")]
        public void Given_No07_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            var so = NewSoCatalogWithCardSeven();
            var inMemory = NewInMemoryCatalogWithCardSeven();
            Assert.That(so.GetEffects(CardTypeId.Of("07")), Is.EqualTo(inMemory.GetEffects(CardTypeId.Of("07"))));
        }
    }
}
