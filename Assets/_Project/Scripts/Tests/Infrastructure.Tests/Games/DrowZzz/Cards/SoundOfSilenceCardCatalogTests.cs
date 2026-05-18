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
    /// カード No.04「静寂を纏う」(ADR-0019 PR ②)の SO 表現と InMemory 表現の同値性検証
    /// (INF-138)。<see cref="TimeOfDayBranchEffectAsset"/> の夜・朝再帰 + 各分岐内
    /// <see cref="AdjustSdpEffectAsset"/> ×2 + <see cref="ApplyTargetedRestrictionEffectAsset"/>(動的影響付与)の
    /// 再帰 ToDomain で <see cref="TimeOfDayBranchEffect"/> + <see cref="AdjustSdpEffect"/> +
    /// <see cref="ApplyTargetedRestrictionEffect"/> 群を record 値同値で再構築できることを検証する。
    /// </summary>
    [TestFixture]
    public sealed class SoundOfSilenceCardCatalogTests
    {
        // ===== ヘルパー: SO 経由 catalog =====

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardFour()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "04",
                name: "静寂を纏う",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new TimeOfDayBranchEffectAsset(
                        nightEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -12),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 5),
                        },
                        morningEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, 5),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -8),
                        }),
                    new ApplyTargetedRestrictionEffectAsset(SdpTarget.Opponent, 2),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        // ===== ヘルパー: InMemory 経由 catalog(Application.Tests と仕様共有の InMemory 構築ロジック)=====
        // 注:Application.Tests の `SoundOfSilenceCardTests.NewCatalogWithCardFour` を「移植」ではなく
        // 「同仕様で再実装」する(M4-PR4 code-reviewer P-1 の方針を踏襲、Pure C# 哲学維持)。

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardFour()
        {
            var card04 = new CardData("静寂を纏う", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("04"), card04),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("04"),
                    new IEffect[]
                    {
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -12),
                                new AdjustSdpEffect(SdpTarget.Opponent, 5),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, 5),
                                new AdjustSdpEffect(SdpTarget.Opponent, -8),
                            }),
                        new ApplyTargetedRestrictionEffect(SdpTarget.Opponent, 2),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // ===== INF-138: No.04 の SO ↔ InMemory 同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-138")]
        public void Given_No04_SO_When_GetName_Then_InMemoryと一致()
        {
            // Given
            var so = NewSoCatalogWithCardFour();
            var inMemory = NewInMemoryCatalogWithCardFour();
            // When
            var soName = so.Get(CardTypeId.Of("04")).Name;
            var inMemoryName = inMemory.Get(CardTypeId.Of("04")).Name;
            // Then
            Assert.That(soName, Is.EqualTo(inMemoryName));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-138")]
        public void Given_No04_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            // Given(TimeOfDayBranchEffect 1 件 + 各分岐内 3 effect、ApplyTargetedRestrictionEffect の SO ↔ Domain 同値も検証)
            var so = NewSoCatalogWithCardFour();
            var inMemory = NewInMemoryCatalogWithCardFour();
            // When
            var soEffects = so.GetEffects(CardTypeId.Of("04"));
            var inMemoryEffects = inMemory.GetEffects(CardTypeId.Of("04"));
            // Then(TimeOfDayBranchEffect の夜/朝順序保持 + 内側 record 値同値)
            Assert.That(soEffects, Is.EqualTo(inMemoryEffects));
        }
    }
}
