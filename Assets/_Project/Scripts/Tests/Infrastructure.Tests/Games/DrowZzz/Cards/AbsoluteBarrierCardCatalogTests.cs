using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards
{
    /// <summary>
    /// カード No.19「絶対障壁」の SO 表現と InMemory 表現の同値性検証(INF-164)。
    /// Counter + Frenzy 両キーワード持ち + AssociateToFirstPlayerOnGameStartEffect の 2 件最上位構成。
    /// </summary>
    [TestFixture]
    public sealed class AbsoluteBarrierCardCatalogTests
    {
        private static ScriptableObjectCardCatalog NewSoCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "19",
                name: "絶対障壁",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new KeywordedEffectAsset(
                        new[] { Keyword.Counter, Keyword.Frenzy },
                        new AdjustSdpEffectAsset(SdpTarget.Self, 0)),
                    new AssociateToFirstPlayerOnGameStartEffectAsset(),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        private static InMemoryCardCatalog NewInMemoryCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("19"), new CardData("絶対障壁", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("19"),
                    new IEffect[]
                    {
                        new KeywordedEffect(new[] { Keyword.Counter, Keyword.Frenzy }, new AdjustSdpEffect(SdpTarget.Self, 0)),
                        new AssociateToFirstPlayerOnGameStartEffect(),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-164")]
        public void Given_No19_SO_When_GetName_Then_InMemoryと一致()
        {
            Assert.That(NewSoCatalog().Get(CardTypeId.Of("19")).Name, Is.EqualTo(NewInMemoryCatalog().Get(CardTypeId.Of("19")).Name));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-164")]
        public void Given_No19_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            Assert.That(NewSoCatalog().GetEffects(CardTypeId.Of("19")), Is.EqualTo(NewInMemoryCatalog().GetEffects(CardTypeId.Of("19"))));
        }
    }
}
