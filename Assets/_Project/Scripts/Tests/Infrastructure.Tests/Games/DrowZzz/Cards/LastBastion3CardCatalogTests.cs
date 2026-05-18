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
    /// カード No.15「最後の砦Ⅲ」(2026-05-17)の SO 表現と InMemory 表現の同値性検証(INF-158)。
    /// Choice2 は連想 effect を含まない終端カードであることも構造的に検証される(等値性検証で連想 effect 不在を担保)。
    /// </summary>
    [TestFixture]
    public sealed class LastBastion3CardCatalogTests
    {
        private static ScriptableObjectCardCatalog NewSoCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "15",
                name: "最後の砦Ⅲ",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new ChoiceEffectAsset(new[]
                    {
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, 10),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -10),
                        }),
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -30),
                            // 連想なし(終端カード)
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
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("15"), new CardData("最後の砦Ⅲ", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("15"),
                    new IEffect[]
                    {
                        new ChoiceEffect(new IReadOnlyList<IEffect>[]
                        {
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, 10),
                                new AdjustSdpEffect(SdpTarget.Opponent, -10),
                            },
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new AdjustSdpEffect(SdpTarget.Opponent, -30),
                            },
                        }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-158")]
        public void Given_No15_SO_When_GetName_Then_InMemoryと一致()
        {
            Assert.That(NewSoCatalog().Get(CardTypeId.Of("15")).Name, Is.EqualTo(NewInMemoryCatalog().Get(CardTypeId.Of("15")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-158")]
        public void Given_No15_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            Assert.That(NewSoCatalog().GetEffects(CardTypeId.Of("15")), Is.EqualTo(NewInMemoryCatalog().GetEffects(CardTypeId.Of("15"))));
        }
    }
}
