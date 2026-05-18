using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using NUnit.Framework;
using UnityEngine;
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards
{
    /// <summary>
    /// カード No.11「機械仕掛けの冬将軍」(2026-05-17)の SO 表現と InMemory 表現の同値性検証(INF-151)。
    /// </summary>
    [TestFixture]
    public sealed class MechanicalWinterGeneralCardCatalogTests
    {
        private static PlayerInfluence WinterGeneralDomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpByHandCountEffect(),
                InfluenceConstants.Perpetual);

        private static PlayerInfluenceAsset WinterGeneralSoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpByHandCountEffectAsset(),
                InfluenceConstants.Perpetual);

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardEleven()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "11",
                name: "機械仕掛けの冬将軍",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                    new AdjustSdpEffectAsset(SdpTarget.Opponent, -8),
                    new KeywordedEffectAsset(
                        new[] { Keyword.Frenzy },
                        new ApplyInfluenceEffectAsset(SdpTarget.Opponent, WinterGeneralSoInfluence())),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardEleven()
        {
            var card11 = new CardData("機械仕掛けの冬将軍", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("11"), card11),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("11"),
                    new IEffect[]
                    {
                        new AdjustSdpEffect(SdpTarget.Self, -4),
                        new AdjustSdpEffect(SdpTarget.Opponent, -8),
                        new KeywordedEffect(
                            new[] { Keyword.Frenzy },
                            new ApplyInfluenceEffect(SdpTarget.Opponent, WinterGeneralDomainInfluence())),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-151")]
        public void Given_No11_SO_When_GetName_Then_InMemoryと一致()
        {
            var so = NewSoCatalogWithCardEleven();
            var inMemory = NewInMemoryCatalogWithCardEleven();
            Assert.That(so.Get(CardTypeId.Of("11")).Name, Is.EqualTo(inMemory.Get(CardTypeId.Of("11")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-151")]
        public void Given_No11_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            var so = NewSoCatalogWithCardEleven();
            var inMemory = NewInMemoryCatalogWithCardEleven();
            Assert.That(so.GetEffects(CardTypeId.Of("11")), Is.EqualTo(inMemory.GetEffects(CardTypeId.Of("11"))));
        }
    }
}
