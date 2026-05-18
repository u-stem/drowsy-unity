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
    /// カード No.09「強引過ぎる一手」(2026-05-17、ADR-0020 と同 PR)の SO 表現と InMemory 表現の同値性検証(INF-147)。
    /// </summary>
    [TestFixture]
    public sealed class ForcePlayCardCatalogTests
    {
        private static PlayerInfluence ForcePlayDomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictAllUsageAndAbandonInfluenceMarkerEffect(),
                1);

        private static PlayerInfluenceAsset ForcePlaySoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictAllUsageAndAbandonInfluenceMarkerEffectAsset(),
                1);

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardNine()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "09",
                name: "強引過ぎる一手",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new AdjustSdpEffectAsset(SdpTarget.Self, -10),
                    new AdjustSdpEffectAsset(SdpTarget.Opponent, 10),
                    new ApplyInfluenceEffectAsset(SdpTarget.Opponent, ForcePlaySoInfluence()),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardNine()
        {
            var card09 = new CardData("強引過ぎる一手", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("09"), card09),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("09"),
                    new IEffect[]
                    {
                        new AdjustSdpEffect(SdpTarget.Self, -10),
                        new AdjustSdpEffect(SdpTarget.Opponent, 10),
                        new ApplyInfluenceEffect(SdpTarget.Opponent, ForcePlayDomainInfluence()),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-147")]
        public void Given_No09_SO_When_GetName_Then_InMemoryと一致()
        {
            var so = NewSoCatalogWithCardNine();
            var inMemory = NewInMemoryCatalogWithCardNine();
            Assert.That(so.Get(CardTypeId.Of("09")).Name, Is.EqualTo(inMemory.Get(CardTypeId.Of("09")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-147")]
        public void Given_No09_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            var so = NewSoCatalogWithCardNine();
            var inMemory = NewInMemoryCatalogWithCardNine();
            Assert.That(so.GetEffects(CardTypeId.Of("09")), Is.EqualTo(inMemory.GetEffects(CardTypeId.Of("09"))));
        }
    }
}
