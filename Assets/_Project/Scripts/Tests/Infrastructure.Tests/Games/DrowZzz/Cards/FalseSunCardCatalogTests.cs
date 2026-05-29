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
    /// カード No.12「偽りの太陽」の SO 表現と InMemory 表現の同値性検証(INF-153)。
    /// </summary>
    [TestFixture]
    public sealed class FalseSunCardCatalogTests
    {
        private static PlayerInfluence PlayCardDomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OnOwnPlayCardAfter,
                new AdjustSdpAfterPlayCardEffect(-10),
                InfluenceConstants.Perpetual);

        private static PlayerInfluence AbandonDomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OnOwnAbandonAfter,
                new AdjustSdpAfterAbandonEffect(5),
                InfluenceConstants.Perpetual);

        private static PlayerInfluenceAsset PlayCardSoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OnOwnPlayCardAfter,
                new AdjustSdpAfterPlayCardEffectAsset(-10),
                InfluenceConstants.Perpetual);

        private static PlayerInfluenceAsset AbandonSoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OnOwnAbandonAfter,
                new AdjustSdpAfterAbandonEffectAsset(5),
                InfluenceConstants.Perpetual);

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardTwelve()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "12",
                name: "偽りの太陽",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new TimeOfDayBranchEffectAsset(
                        nightEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 6),
                            new ApplyInfluenceEffectAsset(SdpTarget.Self, PlayCardSoInfluence()),
                            new ApplyInfluenceEffectAsset(SdpTarget.Self, AbandonSoInfluence()),
                        },
                        morningEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 18),
                        }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardTwelve()
        {
            var card12 = new CardData("偽りの太陽", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("12"), card12),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("12"),
                    new IEffect[]
                    {
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new AdjustSdpEffect(SdpTarget.Opponent, 6),
                                new ApplyInfluenceEffect(SdpTarget.Self, PlayCardDomainInfluence()),
                                new ApplyInfluenceEffect(SdpTarget.Self, AbandonDomainInfluence()),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new AdjustSdpEffect(SdpTarget.Opponent, 18),
                            }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-153")]
        public void Given_No12_SO_When_GetName_Then_InMemoryと一致()
        {
            var so = NewSoCatalogWithCardTwelve();
            var inMemory = NewInMemoryCatalogWithCardTwelve();
            Assert.That(so.Get(CardTypeId.Of("12")).Name, Is.EqualTo(inMemory.Get(CardTypeId.Of("12")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-153")]
        public void Given_No12_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            var so = NewSoCatalogWithCardTwelve();
            var inMemory = NewInMemoryCatalogWithCardTwelve();
            Assert.That(so.GetEffects(CardTypeId.Of("12")), Is.EqualTo(inMemory.GetEffects(CardTypeId.Of("12"))));
        }
    }
}
