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
    /// カード No.06「牙の届かぬ領域」(2026-05-17)の SO 表現と InMemory 表現の同値性検証
    /// (INF-142)。最上位 3 件構成(AdjustSdp ×2 + KeywordedEffect([Frenzy], ApplyInfluenceEffect))の
    /// 再帰 ToDomain で対応する Domain effect 群を record 値同値で再構築できることを検証する。
    /// </summary>
    [TestFixture]
    public sealed class UntouchableRealmCardCatalogTests
    {
        // 影響定義(Application.Tests と同仕様で再実装、M4-PR4 方針)
        private static PlayerInfluence BedDamage2xDomainInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new DoubleBedDamageSdpInfluenceMarkerEffect(),
                4);

        private static PlayerInfluenceAsset BedDamage2xSoInfluence() =>
            new PlayerInfluenceAsset(
                InfluenceTrigger.OwnPhaseStart,
                new DoubleBedDamageSdpInfluenceMarkerEffectAsset(),
                4);

        // ===== ヘルパー: SO 経由 catalog =====

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardSix()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "06",
                name: "牙の届かぬ領域",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new AdjustSdpEffectAsset(SdpTarget.Self, -12),
                    new AdjustSdpEffectAsset(SdpTarget.Opponent, -4),
                    new KeywordedEffectAsset(
                        new[] { Keyword.Frenzy },
                        new ApplyInfluenceEffectAsset(SdpTarget.Opponent, BedDamage2xSoInfluence())),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        // ===== ヘルパー: InMemory 経由 catalog(Application.Tests `UntouchableRealmCardTests` と同仕様で再実装)=====

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardSix()
        {
            var card06 = new CardData("牙の届かぬ領域", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("06"), card06),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("06"),
                    new IEffect[]
                    {
                        new AdjustSdpEffect(SdpTarget.Self, -12),
                        new AdjustSdpEffect(SdpTarget.Opponent, -4),
                        new KeywordedEffect(new[] { Keyword.Frenzy },
                            new ApplyInfluenceEffect(SdpTarget.Opponent, BedDamage2xDomainInfluence())),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // ===== INF-142: No.06 の SO ↔ InMemory 同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-142")]
        public void Given_No06_SO_When_GetName_Then_InMemoryと一致()
        {
            // Given
            var so = NewSoCatalogWithCardSix();
            var inMemory = NewInMemoryCatalogWithCardSix();
            // When
            var soName = so.Get(CardTypeId.Of("06")).Name;
            var inMemoryName = inMemory.Get(CardTypeId.Of("06")).Name;
            // Then
            Assert.That(soName, Is.EqualTo(inMemoryName));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-142")]
        public void Given_No06_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            // Given(最上位 3 件 + KeywordedEffect 内 ApplyInfluenceEffect の再帰 + PlayerInfluence + marker)
            var so = NewSoCatalogWithCardSix();
            var inMemory = NewInMemoryCatalogWithCardSix();
            // When
            var soEffects = so.GetEffects(CardTypeId.Of("06"));
            var inMemoryEffects = inMemory.GetEffects(CardTypeId.Of("06"));
            // Then(record 値同値、DoubleBedDamageSdpInfluenceMarkerEffect の auto-equals 含む)
            Assert.That(soEffects, Is.EqualTo(inMemoryEffects));
        }
    }
}
