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
    /// カード No.02「緑の侵攻」(M2-PR5、ADR-0007 §1.5)の SO 表現と InMemory 表現の同値性検証
    /// (M4-PR4、INF-046)。<see cref="ChoiceEffectAsset"/>(2 次元再帰、<see cref="EffectBranchAsset"/> 中間型経由)+
    /// <see cref="ApplyInfluenceEffectAsset"/>(<see cref="PlayerInfluenceAsset"/> 中間型経由)+
    /// <see cref="RemoveInfluenceEffectAsset"/> の再帰 ToDomain で <see cref="ChoiceEffect"/> /
    /// <see cref="ApplyInfluenceEffect"/> / <see cref="RemoveInfluenceEffect"/> 群を record 値同値で再構築できることを検証する。
    /// </summary>
    [TestFixture]
    public sealed class GreenInvasionCardCatalogTests
    {
        // 「緑の侵攻」の継続影響(GreenInvasionInfluence):
        // PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, -5), 3)
        // Application.Tests の `GreenInvasionCardTests.GreenInvasionInfluence()` と同仕様で再実装。
        // M4-PR4 code-reviewer W-2 反映 2026-05-13:ヘルパー名にカード名 (`GreenInvasion`) を含めて
        // 将来 fixture 内に別カードのヘルパーが増えた場合の衝突 / 誤読を予防。
        private static PlayerInfluence GreenInvasionDomainInfluence() =>
            new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -5), 3);

        private static PlayerInfluenceAsset GreenInvasionSoInfluence() =>
            new PlayerInfluenceAsset(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffectAsset(SdpTarget.Self, -5), 3);

        // ===== ヘルパー: SO 経由 catalog =====

        private static ScriptableObjectCardCatalog NewSoCatalogWithCardTwo()
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            var entry = new CardEntryAsset(
                cardIdValue: "02",
                name: "緑の侵攻",
                attributes: System.Array.Empty<AttributeEntry>(),
                effects: new EffectAsset[]
                {
                    new ChoiceEffectAsset(new[]
                    {
                        // 選択 1(攻撃的):自分 SDP -6 / 相手影響除去 / 相手に影響付与
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Self, -6),
                            new RemoveInfluenceEffectAsset(SdpTarget.Opponent),
                            new ApplyInfluenceEffectAsset(SdpTarget.Opponent, GreenInvasionSoInfluence()),
                        }),
                        // 選択 2(防御的):相手 SDP +6 / 自分影響除去 / 自分に影響付与
                        new EffectBranchAsset(new EffectAsset[]
                        {
                            new AdjustSdpEffectAsset(SdpTarget.Opponent, 6),
                            new RemoveInfluenceEffectAsset(SdpTarget.Self),
                            new ApplyInfluenceEffectAsset(SdpTarget.Self, GreenInvasionSoInfluence()),
                        }),
                    }),
                });
            catalog.SetEntriesForTest(new[] { entry });
            return catalog;
        }

        // ===== ヘルパー: InMemory 経由 catalog(Application.Tests と仕様共有の InMemory 構築ロジック)=====
        // 注:Application.Tests の `NewCatalogWithCardTwo` を「移植」ではなく「同仕様で再実装」する。
        // Application.Tests 側が変更された場合に Infrastructure.Tests も同期する責任が呼び出し元にある
        // (M4-PR4 code-reviewer P-1 反映 2026-05-13)。Pure C# 哲学維持(ADR-0006 §4 / ADR-0012 §5)。

        private static InMemoryCardCatalog NewInMemoryCatalogWithCardTwo()
        {
            var card02 = new CardData("緑の侵攻", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("02"), card02),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("02"),
                    new IEffect[]
                    {
                        new ChoiceEffect(new IReadOnlyList<IEffect>[]
                        {
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -6),
                                new RemoveInfluenceEffect(SdpTarget.Opponent),
                                new ApplyInfluenceEffect(SdpTarget.Opponent, GreenInvasionDomainInfluence()),
                            },
                            new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Opponent, 6),
                                new RemoveInfluenceEffect(SdpTarget.Self),
                                new ApplyInfluenceEffect(SdpTarget.Self, GreenInvasionDomainInfluence()),
                            },
                        }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // ===== INF-046: No.02 の SO ↔ InMemory 同値性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-046")]
        public void Given_No02_SO_When_GetName_Then_InMemoryと一致()
        {
            // Given(両 catalog を同じ仕様で構築)
            var so = NewSoCatalogWithCardTwo();
            var inMemory = NewInMemoryCatalogWithCardTwo();
            // When(M4-PR4 code-reviewer W-1 反映 2026-05-13:AAA Given/When/Then を CupOfThreat fixture に揃える)
            var soName = so.Get(CardTypeId.Of("02")).Name;
            var inMemoryName = inMemory.Get(CardTypeId.Of("02")).Name;
            // Then
            Assert.That(soName, Is.EqualTo(inMemoryName));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-046")]
        public void Given_No02_SO_When_GetEffects_Then_InMemoryとIEffect配列が記録値同値()
        {
            // Given(ChoiceEffect 2 分岐 + 各分岐内の 3 effect、PlayerInfluence 中間型経由)
            var so = NewSoCatalogWithCardTwo();
            var inMemory = NewInMemoryCatalogWithCardTwo();
            // When
            var soEffects = so.GetEffects(CardTypeId.Of("02"));
            var inMemoryEffects = inMemory.GetEffects(CardTypeId.Of("02"));
            // Then(ChoiceEffect.Branches の 2 次元順序保持 + 内側 record 値同値)
            Assert.That(soEffects, Is.EqualTo(inMemoryEffects));
        }
    }
}
