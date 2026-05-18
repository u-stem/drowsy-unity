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
    /// カード No.17「見掛け倒しの障壁」(2026-05-18)の SO 表現と InMemory 表現の同値性検証(INF-162)。
    /// Counter キーワード初の本物カード(SDP 変動なし、KeywordedEffect ダミー Inner)。
    /// </summary>
    [TestFixture]
    public sealed class FacadeBarrierCardCatalogTests
    {
        private static ScriptableObjectCardCatalog NewSoCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "17",
                name: "見掛け倒しの障壁",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new KeywordedEffectAsset(
                        new[] { Keyword.Counter },
                        new AdjustSdpEffectAsset(SdpTarget.Self, 0)),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("17"), new CardData("見掛け倒しの障壁", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("17"),
                    new IEffect[]
                    {
                        new KeywordedEffect(new[] { Keyword.Counter },
                            new AdjustSdpEffect(SdpTarget.Self, 0)),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-162")]
        public void Given_No17_SO_When_GetName_Then_InMemoryと一致()
        {
            Assert.That(NewSoCatalog().Get(CardTypeId.Of("17")).Name, Is.EqualTo(NewInMemoryCatalog().Get(CardTypeId.Of("17")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-162")]
        public void Given_No17_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            Assert.That(NewSoCatalog().GetEffects(CardTypeId.Of("17")), Is.EqualTo(NewInMemoryCatalog().GetEffects(CardTypeId.Of("17"))));
        }
    }
}
