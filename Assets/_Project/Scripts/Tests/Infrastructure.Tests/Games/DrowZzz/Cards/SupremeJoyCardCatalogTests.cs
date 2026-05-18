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
    /// カード No.20「至上の喜び」(ADR-0025、2026-05-18)の SO 表現と InMemory 表現の同値性検証(INF-165)。
    /// PlayOrAbandonBranchEffect 1 件最上位:PlayEffects 3 件 + AbandonEffects 2 件。
    /// </summary>
    [TestFixture]
    public sealed class SupremeJoyCardCatalogTests
    {
        private static ScriptableObjectCardCatalog NewSoCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "20",
                name: "至上の喜び",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new PlayOrAbandonBranchEffectAsset(
                        playEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, +20),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -20),
                            new ApplyInfluenceEffectAsset(SdpTarget.Self, new PlayerInfluenceAsset(
                                trigger: InfluenceTrigger.OwnPhaseStart,
                                tickEffect: new RestrictAllUsageAndAbandonInfluenceMarkerEffectAsset(),
                                remainingCount: 1)),
                        },
                        abandonEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, +4),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, +6),
                        }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("20"), new CardData("至上の喜び", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("20"),
                    new IEffect[]
                    {
                        new PlayOrAbandonBranchEffect(
                            playEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, +20),
                                new AdjustSdpEffect(SdpTarget.Opponent, -20),
                                new ApplyInfluenceEffect(SdpTarget.Self, new PlayerInfluence(
                                    Trigger: InfluenceTrigger.OwnPhaseStart,
                                    TickEffect: new RestrictAllUsageAndAbandonInfluenceMarkerEffect(),
                                    RemainingCount: 1)),
                            },
                            abandonEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, +4),
                                new AdjustSdpEffect(SdpTarget.Opponent, +6),
                            }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-165")]
        public void Given_No20_SO_When_GetName_Then_InMemoryと一致()
        {
            Assert.That(NewSoCatalog().Get(CardTypeId.Of("20")).Name, Is.EqualTo(NewInMemoryCatalog().Get(CardTypeId.Of("20")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-165")]
        public void Given_No20_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            Assert.That(NewSoCatalog().GetEffects(CardTypeId.Of("20")), Is.EqualTo(NewInMemoryCatalog().GetEffects(CardTypeId.Of("20"))));
        }
    }
}
