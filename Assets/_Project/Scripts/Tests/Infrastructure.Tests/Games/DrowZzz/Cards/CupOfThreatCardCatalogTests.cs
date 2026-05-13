using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
// NUnit と UnityEngine の双方が PropertyAttribute を提供するため曖昧参照を回避する type alias
// (M4-PR1 で確立、両 using 必須、`csharp-nunit-unityengine-property-conflict` memory 永続化済)
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards
{
    /// <summary>
    /// カード No.01「コップ一杯の脅威」(M2-PR3、ADR-0009)の SO 表現と InMemory 表現の同値性検証
    /// (M4-PR4、INF-045)。<see cref="TimeOfDayBranchEffectAsset"/> wrapper 1 段 + 非 wrapper 内側
    /// (<see cref="AdjustSdpEffectAsset"/> / <see cref="DrawCardEffectAsset"/>)が
    /// <see cref="EffectAsset.ToDomain"/> 再帰経路で <see cref="TimeOfDayBranchEffect"/> + 内側 IEffect 群を
    /// 順序保持シーケンス同値で再構築できることを検証する。
    /// </summary>
    /// <remarks>
    /// JIT 確定 2026-05-13(M4-PR4):**テスト内動的構築 のみ**(実 .asset ファイル配置は M4-PR7 / M5 で
    /// Designer ワークフロー実証時に追加)。Application.Tests の `CupOfThreatCardTests` は本 PR で変更しない
    /// (Pure C# 維持、Ports &amp; Adapters 整合、ADR-0012 §5)。
    /// </remarks>
    [TestFixture]
    public sealed class CupOfThreatCardCatalogTests
    {
        // ===== ヘルパー: SO 経由 catalog =====

        // ADR-0009 §「コップ一杯の脅威」JIT 共有仕様を SO 表現で構築する。
        // CardEntryAsset(internal ctor) + EffectAsset 派生型(internal ctor)経由で
        // ScriptableObjectCardCatalog.SetEntriesForTest に渡す。
        private static ScriptableObjectCardCatalog NewSoCatalogWithCardOne()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "01",
                name: "コップ一杯の脅威",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new TimeOfDayBranchEffectAsset(
                        nightEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                            new DrawCardEffectAsset(SdpTarget.Self, 1),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, -10),
                        },
                        morningEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -4),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 10),
                        }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        // ===== ヘルパー: InMemory 経由 catalog(Application.Tests と仕様共有の InMemory 構築ロジック)=====
        // 注:Application.Tests の `NewCatalogWithCardOne` を「移植」ではなく「同仕様で再実装」する。
        // Application.Tests 側が変更された場合に Infrastructure.Tests も同期する責任が呼び出し元にある
        // (M4-PR4 code-reviewer P-1 反映 2026-05-13)。Pure C# 哲学維持と Ports & Adapters 整合のため、
        // ファイル共有でなく重複保持を選択(ADR-0006 §4 / ADR-0012 §5)。

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardOne()
        {
            var card01 = new CardData("コップ一杯の脅威", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardId, CardData>(CardId.Of("01"), card01),
            };
            var effects = new[]
            {
                new KeyValuePair<CardId, IReadOnlyList<IEffect>>(
                    CardId.Of("01"),
                    new IEffect[]
                    {
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new DrawCardEffect(SdpTarget.Self, 1),
                                new AdjustSdpEffect(SdpTarget.Opponent, -10),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new AdjustSdpEffect(SdpTarget.Opponent, 10),
                            }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // ===== INF-045: No.01 の SO ↔ InMemory 同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-045")]
        public void Given_No01_SO_When_GetName_Then_InMemoryと一致()
        {
            // Given(両 catalog を同じ仕様で構築)
            var so = NewSoCatalogWithCardOne();
            var inMemory = NewInMemoryCatalogWithCardOne();
            // When
            var soName = so.Get(CardId.Of("01")).Name;
            var inMemoryName = inMemory.Get(CardId.Of("01")).Name;
            // Then
            Assert.That(soName, Is.EqualTo(inMemoryName));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-045")]
        public void Given_No01_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            // Given
            var so = NewSoCatalogWithCardOne();
            var inMemory = NewInMemoryCatalogWithCardOne();
            // When
            var soEffects = so.GetEffects(CardId.Of("01"));
            var inMemoryEffects = inMemory.GetEffects(CardId.Of("01"));
            // Then(`TimeOfDayBranchEffect` は内部 IReadOnlyList<IEffect> を持つため override Equals で
            //       順序保持シーケンス同値、内側 `AdjustSdpEffect` / `DrawCardEffect` は record auto-equals)
            Assert.That(soEffects, Is.EqualTo(inMemoryEffects));
        }
    }
}
