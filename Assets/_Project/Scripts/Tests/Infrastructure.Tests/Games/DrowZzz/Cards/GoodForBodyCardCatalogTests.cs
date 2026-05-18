using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using NUnit.Framework;
using UnityEngine;
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Cards
{
    /// <summary>
    /// カード No.03「身体にいいもの」(Phase 2 完結後追加)の SO 表現と InMemory 表現の同値性検証
    /// (INF-136)。<see cref="TimeOfDayBranchEffectAsset"/> の夜・朝再帰 + 各分岐内
    /// <see cref="AdjustSdpEffectAsset"/> ×2 + <see cref="ApplyInfluenceEffectAsset"/>
    /// (<see cref="PlayerInfluenceAsset"/> 中間型経由、`RemainingCount = InfluenceConstants.Perpetual`)の
    /// 再帰 ToDomain で <see cref="TimeOfDayBranchEffect"/> + <see cref="ApplyInfluenceEffect"/> +
    /// <see cref="AdjustSdpEffect"/> 群を record 値同値で再構築できることを検証する。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本テストは「永続影響」(<see cref="InfluenceConstants.Perpetual"/> = <see cref="int.MaxValue"/>)が
    /// SO シリアライゼーション経路を round-trip しても int の同値性で正しく保存されることを構造的に保証する
    /// (Phase 2 で確立済の `PlayerInfluence` auto-equals + `_remainingCount` プリミティブ int 表現の組み合わせ)。
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class GoodForBodyCardCatalogTests
    {
        // 「身体にいいもの」夜分岐由来の影響(NightInfluence):
        // PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, +4), Perpetual)
        // Application.Tests の `GoodForBodyCardTests.NightInfluence()` と同仕様で再実装。
        private static PlayerInfluence GoodForBodyDomainNightInfluence() =>
            new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 4), InfluenceConstants.Perpetual);

        private static PlayerInfluenceAsset GoodForBodySoNightInfluence() =>
            new PlayerInfluenceAsset(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffectAsset(SdpTarget.Self, 4), InfluenceConstants.Perpetual);

        // 朝分岐由来の影響(MorningInfluence):PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, -6), Perpetual)
        private static PlayerInfluence GoodForBodyDomainMorningInfluence() =>
            new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -6), InfluenceConstants.Perpetual);

        private static PlayerInfluenceAsset GoodForBodySoMorningInfluence() =>
            new PlayerInfluenceAsset(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffectAsset(SdpTarget.Self, -6), InfluenceConstants.Perpetual);

        // ===== ヘルパー: SO 経由 catalog =====

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardThree()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "03",
                name: "身体にいいもの",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new TimeOfDayBranchEffectAsset(
                        nightEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -20),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 5),
                            new ApplyInfluenceEffectAsset(SdpTarget.Self, GoodForBodySoNightInfluence()),
                        },
                        morningEffects: new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -10),
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 5),
                            new ApplyInfluenceEffectAsset(SdpTarget.Self, GoodForBodySoMorningInfluence()),
                        }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        // ===== ヘルパー: InMemory 経由 catalog(Application.Tests と仕様共有の InMemory 構築ロジック)=====
        // 注:Application.Tests の `GoodForBodyCardTests.NewCatalogWithCardThree` を「移植」ではなく
        // 「同仕様で再実装」する(M4-PR4 code-reviewer P-1 の方針を踏襲、Pure C# 哲学維持)。
        private static InMemoryCardCatalog NewInMemoryCatalogWithCardThree()
        {
            var card03 = new CardData("身体にいいもの", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("03"), card03),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("03"),
                    new IEffect[]
                    {
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -20),
                                new AdjustSdpEffect(SdpTarget.Opponent, 5),
                                new ApplyInfluenceEffect(SdpTarget.Self, GoodForBodyDomainNightInfluence()),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -10),
                                new AdjustSdpEffect(SdpTarget.Opponent, 5),
                                new ApplyInfluenceEffect(SdpTarget.Self, GoodForBodyDomainMorningInfluence()),
                            }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // ===== INF-136: No.03 の SO ↔ InMemory 同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-136")]
        public void Given_No03_SO_When_GetName_Then_InMemoryと一致()
        {
            // Given(両 catalog を同じ仕様で構築)
            var so = NewSoCatalogWithCardThree();
            var inMemory = NewInMemoryCatalogWithCardThree();
            // When
            var soName = so.Get(CardTypeId.Of("03")).Name;
            var inMemoryName = inMemory.Get(CardTypeId.Of("03")).Name;
            // Then
            Assert.That(soName, Is.EqualTo(inMemoryName));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-136")]
        public void Given_No03_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            // Given(TimeOfDayBranchEffect 1 件 + 各分岐内 3 effect、PlayerInfluence Perpetual の int 同値性も検証)
            var so = NewSoCatalogWithCardThree();
            var inMemory = NewInMemoryCatalogWithCardThree();
            // When
            var soEffects = so.GetEffects(CardTypeId.Of("03"));
            var inMemoryEffects = inMemory.GetEffects(CardTypeId.Of("03"));
            // Then(TimeOfDayBranchEffect の夜/朝順序保持 + 内側 record 値同値、Perpetual int 値も完全一致)
            Assert.That(soEffects, Is.EqualTo(inMemoryEffects));
        }
    }
}
