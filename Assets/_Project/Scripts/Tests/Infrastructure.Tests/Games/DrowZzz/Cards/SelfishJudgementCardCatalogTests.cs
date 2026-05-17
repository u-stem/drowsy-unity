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
    /// カード No.16「自分勝手な審判」(2026-05-17)の SO 表現と InMemory 表現の同値性検証(INF-160)。
    /// 条件分岐 effect の SO 経路初導入。
    /// </summary>
    [TestFixture]
    public sealed class SelfishJudgementCardCatalogTests
    {
        private static PlayerInfluence DomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, -4),
                InfluenceConstants.Perpetual);

        private static PlayerInfluenceAsset SoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                InfluenceConstants.Perpetual);

        private static ScriptableObjectCardCatalog NewSoCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "16",
                name: "自分勝手な審判",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new ChoiceEffectAsset(new[]
                    {
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -8),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 5),
                            new ConditionalApplyOrClearInfluencesEffectAsset(SdpTarget.Self, 2, SoInfluence()),
                        }),
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, 5),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -8),
                            new ConditionalApplyOrClearInfluencesEffectAsset(SdpTarget.Opponent, 2, SoInfluence()),
                        }),
                    }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("16"), new CardData("自分勝手な審判", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("16"),
                    new IEffect[]
                    {
                        new ChoiceEffect(new IReadOnlyList<IEffect>[]
                        {
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -8),
                                new AdjustSdpEffect(SdpTarget.Opponent, 5),
                                new ConditionalApplyOrClearInfluencesEffect(SdpTarget.Self, 2, DomainInfluence()),
                            },
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, 5),
                                new AdjustSdpEffect(SdpTarget.Opponent, -8),
                                new ConditionalApplyOrClearInfluencesEffect(SdpTarget.Opponent, 2, DomainInfluence()),
                            },
                        }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-160")]
        public void Given_No16_SO_When_GetName_Then_InMemoryと一致()
        {
            Assert.That(NewSoCatalog().Get(CardTypeId.Of("16")).Name, Is.EqualTo(NewInMemoryCatalog().Get(CardTypeId.Of("16")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-160")]
        public void Given_No16_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            Assert.That(NewSoCatalog().GetEffects(CardTypeId.Of("16")), Is.EqualTo(NewInMemoryCatalog().GetEffects(CardTypeId.Of("16"))));
        }
    }
}
