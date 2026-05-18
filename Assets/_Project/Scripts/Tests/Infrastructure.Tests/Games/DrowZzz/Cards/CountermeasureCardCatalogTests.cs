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
    /// カード No.18「対抗手段」(ADR-0023、2026-05-18)の SO 表現と InMemory 表現の同値性検証(INF-163)。
    /// Echo キーワード機構初の本物カード(KeywordedEffect([Echo], ReuseInfluenceSourceEffect))。
    /// </summary>
    [TestFixture]
    public sealed class CountermeasureCardCatalogTests
    {
        private static ScriptableObjectCardCatalog NewSoCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "18",
                name: "対抗手段",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new KeywordedEffectAsset(
                        new[] { Keyword.Echo },
                        new ReuseInfluenceSourceEffectAsset()),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("18"), new CardData("対抗手段", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("18"),
                    new IEffect[]
                    {
                        new KeywordedEffect(new[] { Keyword.Echo }, new ReuseInfluenceSourceEffect()),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-163")]
        public void Given_No18_SO_When_GetName_Then_InMemoryと一致()
        {
            Assert.That(NewSoCatalog().Get(CardTypeId.Of("18")).Name, Is.EqualTo(NewInMemoryCatalog().Get(CardTypeId.Of("18")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-163")]
        public void Given_No18_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            Assert.That(NewSoCatalog().GetEffects(CardTypeId.Of("18")), Is.EqualTo(NewInMemoryCatalog().GetEffects(CardTypeId.Of("18"))));
        }
    }
}
