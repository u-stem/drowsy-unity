using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// wrapper Asset 派生型(<see cref="TimeOfDayBranchEffectAsset"/> / <see cref="ChoiceEffectAsset"/> /
    /// <see cref="KeywordedEffectAsset"/>)の `ToDomain()` 再帰経路テスト(M4-PR3、INF-040 / INF-042 / INF-044)。
    /// 各 wrapper の Inner / Branches が <see cref="EffectAsset.ToDomain"/> を再帰呼び出しして
    /// domain record を構築することを検証。INF-039 / INF-041 / INF-043(Ubiquitous structural)はテスト免除。
    /// </summary>
    [TestFixture]
    public sealed class WrapperEffectAssetsTests
    {
        // ===== INF-040: TimeOfDayBranchEffectAsset の再帰 ToDomain =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-040")]
        public void Given_夜AdjustSdp_朝AdjustSdp_When_TimeOfDayBranchEffectAssetをToDomain_Then_record値同値()
        {
            // Given(「コップ一杯の脅威」風:夜と朝で異なる AdjustSdp、簡素化のため 1 effect ずつ)
            var asset = new TimeOfDayBranchEffectAsset(
                new EffectAsset[] { new AdjustSdpEffectAsset(SdpTarget.Self, -4) },
                new EffectAsset[] { new AdjustSdpEffectAsset(SdpTarget.Opponent, 10) });
            // When
            var effect = asset.ToDomain();
            // Then(再帰構築された TimeOfDayBranchEffect の record 値同値、ChoiceEffect / TimeOfDayBranchEffect は
            //       内部 IReadOnlyList<IEffect> を持つため auto-equals でなく override 経由)
            var expected = new TimeOfDayBranchEffect(
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -4) },
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, 10) });
            Assert.That(effect, Is.EqualTo(expected));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-040")]
        public void Given_NightEffects内にnull要素_When_ToDomain_Then_ArgumentNullException()
        {
            // Given(SerializeReference の missing reference 復元想定、null 要素を含む)
            var asset = new TimeOfDayBranchEffectAsset(
                new EffectAsset[] { null },
                new EffectAsset[] { new AdjustSdpEffectAsset(SdpTarget.Self, 0) });
            // When/Then(上位 catalog でこれが catch + skip される、INF-018 + INF-019 経路の wrapper 内側 null)
            Assert.That(() => asset.ToDomain(), Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-040")]
        public void Given_MorningEffects内にnull要素_When_ToDomain_Then_ArgumentNullException()
        {
            // Given(夜・朝両方の null 経路で同じ ArgumentNullException が伝播することの対称性保証、
            // M4-PR3 code-reviewer P-2 反映 2026-05-13:Night / Morning 両フィールドが同一 ConvertList ロジックを通る)
            var asset = new TimeOfDayBranchEffectAsset(
                new EffectAsset[] { new AdjustSdpEffectAsset(SdpTarget.Self, 0) },
                new EffectAsset[] { null });
            // When/Then
            Assert.That(() => asset.ToDomain(), Throws.TypeOf<ArgumentNullException>());
        }

        // ===== INF-042: ChoiceEffectAsset の再帰 ToDomain(中間型 EffectBranchAsset 経由)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-042")]
        public void Given_2分岐_When_ChoiceEffectAssetをToDomain_Then_record値同値()
        {
            // Given(「緑の侵攻」風:選択 1 = 攻撃的、選択 2 = 防御的、簡素化のため 1 effect ずつ)
            var asset = new ChoiceEffectAsset(new[]
            {
                new EffectBranchAsset(new EffectAsset[] { new AdjustSdpEffectAsset(SdpTarget.Self, -6) }),
                new EffectBranchAsset(new EffectAsset[] { new AdjustSdpEffectAsset(SdpTarget.Opponent, 6) }),
            });
            // When
            var effect = asset.ToDomain();
            // Then(2 次元再帰、ChoiceEffect.Branches.Count >= 2 は record 側で検証済)
            var expected = new ChoiceEffect(new IReadOnlyList<IEffect>[]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -6) },
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, 6) },
            });
            Assert.That(effect, Is.EqualTo(expected));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-042")]
        public void Given_Branches内にnull要素_When_ChoiceEffectAssetをToDomain_Then_ArgumentNullException()
        {
            // Given(SerializeReference の missing reference 復元想定 + 中間型 EffectBranchAsset 内の null も同様、
            // M4-PR3 code-reviewer W-1 反映 2026-05-13:TimeOfDayBranchEffectAsset との対称テスト保証)
            var asset = new ChoiceEffectAsset(new[]
            {
                new EffectBranchAsset(new EffectAsset[] { null }),  // 内側 null 経路
                new EffectBranchAsset(new EffectAsset[] { new AdjustSdpEffectAsset(SdpTarget.Self, 0) }),
            });
            // When/Then(上位 catalog で catch + skip、INF-019 graceful 経路)
            Assert.That(() => asset.ToDomain(), Throws.TypeOf<ArgumentNullException>());
        }

        // ===== INF-044: KeywordedEffectAsset の再帰 ToDomain + Inner null 防御 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-044")]
        public void Given_FrenzyとInner_When_KeywordedEffectAssetをToDomain_Then_record値同値()
        {
            // Given(「夢」夜効果風:Frenzy + Instinct 付き EarlyWinTriggerEffect)
            var asset = new KeywordedEffectAsset(
                new[] { Keyword.Frenzy, Keyword.Instinct },
                new EarlyWinTriggerEffectAsset());
            // When
            var effect = asset.ToDomain();
            // Then
            var expected = new KeywordedEffect(
                new[] { Keyword.Frenzy, Keyword.Instinct },
                new EarlyWinTriggerEffect());
            Assert.That(effect, Is.EqualTo(expected));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-044")]
        public void Given_Innerがnull_When_KeywordedEffectAssetをToDomain_Then_ArgumentNullException()
        {
            // Given(SerializeReference の missing reference 復元想定、INF-019 本格化を支える経路)
            var asset = new KeywordedEffectAsset(new[] { Keyword.Frenzy }, null);
            // When/Then
            Assert.That(() => asset.ToDomain(), Throws.TypeOf<ArgumentNullException>());
        }
    }
}
