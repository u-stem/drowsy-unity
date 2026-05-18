using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using NUnit.Framework;
using UnityEngine;
// NUnit と UnityEngine の双方が PropertyAttribute を提供するため曖昧参照を回避する type alias
// (M4-PR1 で確立、M4-PR2 で `using UnityEngine;` も両方必須と判明、`csharp-nunit-unityengine-property-conflict`
// memory 永続化済。M4-PR3 code-reviewer P-3 反映 2026-05-13:コメントを M4-PR1〜PR2 慣例に揃える)
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// 非 wrapper Asset 派生型の `ToDomain()` 値伝達テスト(M4-PR3)。
    /// 8 派生型 + marker 系をまとめて 1 fixture で網羅。Ubiquitous structural(INF-023 / 025 / 027 /
    /// 029 / 031 / 033 / 035 / 037)はテスト免除、ToDomain 値伝達(INF-024 / 028 / 030 / 032 / 034 /
    /// 036 / 038)を中心に検証。INF-026 は <see cref="PlayerInfluenceAssetTests"/> でカバー(再帰経路)。
    /// </summary>
    [TestFixture]
    public sealed class SimpleEffectAssetsTests
    {
        // ===== INF-024: DrawCardEffectAsset =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-024")]
        public void Given_SelfCount1_When_DrawCardEffectAssetをToDomain_Then_DrawCardEffectを返す()
        {
            // Given
            var asset = new DrawCardEffectAsset(SdpTarget.Self, 1);
            // When
            var effect = asset.ToDomain();
            // Then
            Assert.That(effect, Is.EqualTo(new DrawCardEffect(SdpTarget.Self, 1)));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-024")]
        public void Given_SelfCount2_When_DrawCardEffectAssetをToDomain_Then_DrawCardEffectを返す()
        {
            // Given(カード No.01 夜効果は 1 ドロー、本テストは複数枚も値伝達確認)
            var asset = new DrawCardEffectAsset(SdpTarget.Self, 2);
            // When
            var effect = asset.ToDomain();
            // Then
            Assert.That(effect, Is.EqualTo(new DrawCardEffect(SdpTarget.Self, 2)));
        }

        // ===== INF-028: RemoveInfluenceEffectAsset =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-028")]
        public void Given_Self_When_RemoveInfluenceEffectAssetをToDomain_Then_RemoveInfluenceEffectを返す()
        {
            // Given
            var asset = new RemoveInfluenceEffectAsset(SdpTarget.Self);
            // When/Then
            Assert.That(asset.ToDomain(), Is.EqualTo(new RemoveInfluenceEffect(SdpTarget.Self)));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-028")]
        public void Given_Opponent_When_RemoveInfluenceEffectAssetをToDomain_Then_RemoveInfluenceEffectを返す()
        {
            // Given
            var asset = new RemoveInfluenceEffectAsset(SdpTarget.Opponent);
            // When/Then
            Assert.That(asset.ToDomain(), Is.EqualTo(new RemoveInfluenceEffect(SdpTarget.Opponent)));
        }

        // ===== INF-030: EarlyWinTriggerEffectAsset =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-030")]
        public void Given_引数なし_When_EarlyWinTriggerEffectAssetをToDomain_Then_EarlyWinTriggerEffectを返す()
        {
            // Given
            var asset = new EarlyWinTriggerEffectAsset();
            // When/Then(marker、record auto-equals でフィールドなし record は値同値)
            Assert.That(asset.ToDomain(), Is.EqualTo(new EarlyWinTriggerEffect()));
        }

        // ===== INF-032: DamageBedEffectAsset =====
        // 注(M4-PR3 code-reviewer P-4 反映 2026-05-13):異常系(Percent 0 / 負値 / 非 5 倍数で
        // ArgumentException 伝播)のユニットテストは本 fixture では追加しない。設計方針として
        // 「Abnormal は catalog 統合テスト INF-019(KeywordedEffectAsset.Inner null 経路)で代表」
        // でカバーする(同経路で BuildEffectsFromAssets の catch + skip + LogError が機能することを実証)。
        // 個別 Asset の Abnormal を全件展開すると 11 派生型 × 異常系で爆発するため、INF-019 統合経路で抑制する。

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-032")]
        public void Given_SelfPercent5_When_DamageBedEffectAssetをToDomain_Then_DamageBedEffectを返す()
        {
            // Given(Percent は 5 の倍数 / 正値、record 側で検証)
            var asset = new DamageBedEffectAsset(SdpTarget.Self, 5);
            // When/Then
            Assert.That(asset.ToDomain(), Is.EqualTo(new DamageBedEffect(SdpTarget.Self, 5)));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-032")]
        public void Given_OpponentPercent20_When_DamageBedEffectAssetをToDomain_Then_DamageBedEffectを返す()
        {
            // Given
            var asset = new DamageBedEffectAsset(SdpTarget.Opponent, 20);
            // When/Then
            Assert.That(asset.ToDomain(), Is.EqualTo(new DamageBedEffect(SdpTarget.Opponent, 20)));
        }

        // ===== INF-034: AssociatableMarkerEffectAsset =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-034")]
        public void Given_引数なし_When_AssociatableMarkerEffectAssetをToDomain_Then_AssociatableMarkerEffectを返す()
        {
            // Given
            var asset = new AssociatableMarkerEffectAsset();
            // When/Then
            Assert.That(asset.ToDomain(), Is.EqualTo(new AssociatableMarkerEffect()));
        }

        // ===== INF-036: RequiresMinimumTotalPointsMarkerEffectAsset =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-036")]
        public void Given_Threshold100_When_RequiresMinimumTotalPointsMarkerEffectAssetをToDomain_Then_record値同値()
        {
            // Given(夢カードの FDS ≥ 100 想定)
            var asset = new RequiresMinimumTotalPointsMarkerEffectAsset(100);
            // When/Then
            Assert.That(asset.ToDomain(), Is.EqualTo(new RequiresMinimumTotalPointsMarkerEffect(100)));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-036")]
        public void Given_Threshold50_When_RequiresMinimumTotalPointsMarkerEffectAssetをToDomain_Then_record値同値()
        {
            // Given
            var asset = new RequiresMinimumTotalPointsMarkerEffectAsset(50);
            // When/Then
            Assert.That(asset.ToDomain(), Is.EqualTo(new RequiresMinimumTotalPointsMarkerEffect(50)));
        }

        // ===== INF-038: UsageRestrictionMarkerEffectAsset =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-038")]
        public void Given_引数なし_When_UsageRestrictionMarkerEffectAssetをToDomain_Then_UsageRestrictionMarkerEffectを返す()
        {
            // Given
            var asset = new UsageRestrictionMarkerEffectAsset();
            // When/Then
            Assert.That(asset.ToDomain(), Is.EqualTo(new UsageRestrictionMarkerEffect()));
        }
    }
}
