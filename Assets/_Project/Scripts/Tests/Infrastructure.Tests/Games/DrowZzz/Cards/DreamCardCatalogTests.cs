using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
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
    /// カード No.00「夢」(M3-PR6、ADR-0011 §6 / §7)の SO 表現と InMemory 表現の同値性検証
    /// (M4-PR4、INF-047)。M3 全機構統合カードの SO 化を検証:4 effect 最上位
    /// (<see cref="AssociatableMarkerEffectAsset"/> / <see cref="RequiresMinimumTotalPointsMarkerEffectAsset"/> /
    /// <see cref="UsageRestrictionMarkerEffectAsset"/> / <see cref="TimeOfDayBranchEffectAsset"/>)+
    /// 夜効果に <see cref="KeywordedEffectAsset"/>(Frenzy + Instinct)nest + <see cref="EarlyWinTriggerEffectAsset"/>
    /// inner + 朝効果 <see cref="AdjustSdpEffectAsset"/>(Self, -80)の再帰経路を網羅。
    /// </summary>
    /// <remarks>
    /// 本 fixture は M3-PR6 で確立した「夢」カードの 4 effect 構造 + wrapper 再帰 + keyword + nest 4 段以上の
    /// 統合経路の SO ↔ InMemory 同値性を検証する最終ステージ(ADR-0012 §6 / §9 の M4-PR4 想定通り)。
    /// </remarks>
    [TestFixture]
    public sealed class DreamCardCatalogTests
    {
        // ===== ヘルパー: SO 経由 catalog =====

        private static ScriptableObjectCardCatalog NewSoCatalogWithDream()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "00",
                name: "夢",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    // (1) 連想可能カードであることを示すマーカー(ADR-0011 §1、M3-PR4 確立)
                    new AssociatableMarkerEffectAsset(),
                    // (2) 使用条件:FDS ≥ 100 を要求
                    new RequiresMinimumTotalPointsMarkerEffectAsset(DrowZzzVictoryConstants.EarlyWinScoreThreshold),
                    // (3) 連想後使用制限マーカー
                    new UsageRestrictionMarkerEffectAsset(),
                    // (4) 時刻分岐:夜 = 狂乱+本能付き EarlyWinTrigger、朝 = 自分 SDP -80
                    new TimeOfDayBranchEffectAsset(
                        nightEffects: new EffectAsset[]
                        {
                            new KeywordedEffectAsset(
                                new[] { Keyword.Frenzy, Keyword.Instinct },
                                new EarlyWinTriggerEffectAsset()),
                        },
                        morningEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -80),
                        }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        // ===== ヘルパー: InMemory 経由 catalog(Application.Tests と仕様共有の InMemory 構築ロジック)=====
        // 注:Application.Tests の `NewCatalogWithDream` を「移植」ではなく「同仕様で再実装」する。
        // Application.Tests 側が変更された場合に Infrastructure.Tests も同期する責任が呼び出し元にある
        // (M4-PR4 code-reviewer P-1 反映 2026-05-13)。Pure C# 哲学維持(ADR-0006 §4 / ADR-0012 §5)。

        private static InMemoryCardCatalog NewInMemoryCatalogWithDream()
        {
            var dream = new CardData("夢", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("00"), dream),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("00"),
                    new IEffect[]
                    {
                        new AssociatableMarkerEffect(),
                        new RequiresMinimumTotalPointsMarkerEffect(DrowZzzVictoryConstants.EarlyWinScoreThreshold),
                        new UsageRestrictionMarkerEffect(),
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new KeywordedEffect(
                                    new[] { Keyword.Frenzy, Keyword.Instinct },
                                    new EarlyWinTriggerEffect()),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -80),
                            }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // ===== INF-047: No.00 の SO ↔ InMemory 同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-047")]
        public void Given_No00_SO_When_GetName_Then_InMemoryと一致()
        {
            // Given(両 catalog を同じ仕様で構築)
            var so = NewSoCatalogWithDream();
            var inMemory = NewInMemoryCatalogWithDream();
            // When(M4-PR4 code-reviewer W-1 反映 2026-05-13:AAA Given/When/Then を CupOfThreat fixture に揃える)
            var soName = so.Get(CardTypeId.Of("00")).Name;
            var inMemoryName = inMemory.Get(CardTypeId.Of("00")).Name;
            // Then
            Assert.That(soName, Is.EqualTo(inMemoryName));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-047")]
        public void Given_No00_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            // Given(M3 全機構統合:4 最上位 + 夜効果 KeywordedEffect[Frenzy, Instinct] → EarlyWinTrigger nest +
            //       朝効果 AdjustSdpEffect(Self, -80))
            var so = NewSoCatalogWithDream();
            var inMemory = NewInMemoryCatalogWithDream();
            // When
            var soEffects = so.GetEffects(CardTypeId.Of("00"));
            var inMemoryEffects = inMemory.GetEffects(CardTypeId.Of("00"));
            // Then(再帰 ToDomain で構築された IEffect[] と InMemory 直接渡しが record 値同値)
            Assert.That(soEffects, Is.EqualTo(inMemoryEffects));
        }
    }
}
