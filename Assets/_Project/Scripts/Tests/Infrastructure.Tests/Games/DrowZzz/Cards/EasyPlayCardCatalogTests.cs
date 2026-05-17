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
    /// カード No.10「安直過ぎる一手」(2026-05-17、ADR-0021 と同 PR)の SO 表現と InMemory 表現の同値性検証(INF-149)。
    /// </summary>
    [TestFixture]
    public sealed class EasyPlayCardCatalogTests
    {
        private static PlayerInfluence EasyPlayDomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictDrawCardInfluenceMarkerEffect(),
                1);

        private static PlayerInfluenceAsset EasyPlaySoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictDrawCardInfluenceMarkerEffectAsset(),
                1);

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardTen()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "10",
                name: "安直過ぎる一手",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new AdjustSdpEffectAsset(SdpTarget.Self, -10),
                    new DamageBedEffectAsset(SdpTarget.Opponent, 30),
                    new ApplyInfluenceEffectAsset(SdpTarget.Opponent, EasyPlaySoInfluence()),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardTen()
        {
            var card10 = new CardData("安直過ぎる一手", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("10"), card10),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("10"),
                    new IEffect[]
                    {
                        new AdjustSdpEffect(SdpTarget.Self, -10),
                        new DamageBedEffect(SdpTarget.Opponent, 30),
                        new ApplyInfluenceEffect(SdpTarget.Opponent, EasyPlayDomainInfluence()),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-149")]
        public void Given_No10_SO_When_GetName_Then_InMemoryと一致()
        {
            var so = NewSoCatalogWithCardTen();
            var inMemory = NewInMemoryCatalogWithCardTen();
            Assert.That(so.Get(CardTypeId.Of("10")).Name, Is.EqualTo(inMemory.Get(CardTypeId.Of("10")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-149")]
        public void Given_No10_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            var so = NewSoCatalogWithCardTen();
            var inMemory = NewInMemoryCatalogWithCardTen();
            Assert.That(so.GetEffects(CardTypeId.Of("10")), Is.EqualTo(inMemory.GetEffects(CardTypeId.Of("10"))));
        }
    }
}
