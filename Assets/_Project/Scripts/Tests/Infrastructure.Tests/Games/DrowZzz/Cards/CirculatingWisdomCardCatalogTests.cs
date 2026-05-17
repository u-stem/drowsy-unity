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
    /// カード No.08「廻るための知恵」(2026-05-17)の SO 表現と InMemory 表現の同値性検証(INF-145)。
    /// </summary>
    [TestFixture]
    public sealed class CirculatingWisdomCardCatalogTests
    {
        private static PlayerInfluence InvertDomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new InvertBedDamageSdpInfluenceMarkerEffect(),
                InfluenceConstants.Perpetual);

        private static PlayerInfluenceAsset InvertSoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OwnPhaseStart,
                new InvertBedDamageSdpInfluenceMarkerEffectAsset(),
                InfluenceConstants.Perpetual);

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardEight()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "08",
                name: "廻るための知恵",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    // 最上位 ChoiceEffect、各 branch 内 ApplyInfluenceEffectAsset を Keyworded([Instinct]) で包む
                    // (2026-05-17 開発中の DZ-294/302 失敗から構造修正、`CirculatingWisdomCardTests` のヘルパーと同仕様)
                    new ChoiceEffectAsset(new[]
                    {
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 5),
                            new KeywordedEffectAsset(
                                new[] { Keyword.Instinct },
                                new ApplyInfluenceEffectAsset(SdpTarget.Self, InvertSoInfluence())),
                        }),
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, 5),
                            new KeywordedEffectAsset(
                                new[] { Keyword.Instinct },
                                new ApplyInfluenceEffectAsset(SdpTarget.Opponent, InvertSoInfluence())),
                        }),
                    }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardEight()
        {
            var card08 = new CardData("廻るための知恵", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("08"), card08),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("08"),
                    new IEffect[]
                    {
                        new ChoiceEffect(new IReadOnlyList<IEffect>[]
                        {
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Opponent, 5),
                                new KeywordedEffect(new[] { Keyword.Instinct },
                                    new ApplyInfluenceEffect(SdpTarget.Self, InvertDomainInfluence())),
                            },
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, 5),
                                new KeywordedEffect(new[] { Keyword.Instinct },
                                    new ApplyInfluenceEffect(SdpTarget.Opponent, InvertDomainInfluence())),
                            },
                        }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-145")]
        public void Given_No08_SO_When_GetName_Then_InMemoryと一致()
        {
            var so = NewSoCatalogWithCardEight();
            var inMemory = NewInMemoryCatalogWithCardEight();
            Assert.That(so.Get(CardTypeId.Of("08")).Name, Is.EqualTo(inMemory.Get(CardTypeId.Of("08")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-145")]
        public void Given_No08_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            var so = NewSoCatalogWithCardEight();
            var inMemory = NewInMemoryCatalogWithCardEight();
            Assert.That(so.GetEffects(CardTypeId.Of("08")), Is.EqualTo(inMemory.GetEffects(CardTypeId.Of("08"))));
        }
    }
}
