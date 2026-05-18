using System.Linq;
using Drowsy.Application.Games.DrowZzz;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Stubs
{
    /// <summary>
    /// <see cref="StubGameConfig"/> のデフォルト値の正当性を検証する(DZ-154 / 既存 CFG-101 を間接担保)。
    /// </summary>
    /// <remarks>
    /// CFG-101 (FdpPool 構造) / CFG-103 (DdpPool 構造) はテスト免除 Ubiquitous(`IGameConfig` の signature で
    /// 構造的に保証)だが、実際の `StubGameConfig` インスタンスのデフォルト値の妥当性は本 fixture で間接検証する。
    /// </remarks>
    [TestFixture]
    public sealed class StubGameConfigTests
    {
        // ===== DZ-154: デフォルト DdpPool 構造(13 種 × 3 枚 = 39 要素)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-154")]
        public void Given_デフォルトStubGameConfig_When_DdpPoolを取得_Then_Count39()
        {
            // Given
            var config = new StubGameConfig();
            // When / Then(13 種 × 3 枚 = 39)
            Assert.That(config.DdpPool.Count, Is.EqualTo(39));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-154")]
        public void Given_デフォルトStubGameConfig_When_DdpPool_Distinctを取得_Then_13種類()
        {
            // Given(値の種類: {-30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30} = 13 種)
            var config = new StubGameConfig();
            // When / Then
            Assert.That(config.DdpPool.Distinct().Count(), Is.EqualTo(13));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-154")]
        public void Given_デフォルトStubGameConfig_When_DdpPool_各値の出現回数を取得_Then_全て3回()
        {
            // Given(13 種それぞれ 3 枚)
            var config = new StubGameConfig();
            // When(各値の出現回数を集計)
            var allThree = config.DdpPool
                .GroupBy(v => v)
                .All(g => g.Count() == 3);
            // Then
            Assert.That(allThree, Is.True);
        }

        // Tests W-3 post-Phase2 レビュー反映:「1 テスト 1 アサーション原則」(CLAUDE.md §6)に従って
        // min / max を別テストに分離(失敗時にどちらが問題かエラーメッセージから判別可能にする)。

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-154")]
        public void Given_デフォルトStubGameConfig_When_DdpPoolMin_Then_マイナス30()
        {
            // Given
            var config = new StubGameConfig();
            // When
            var min = config.DdpPool.Min();
            // Then(ADR-0009 §「DDP プールの構造」: -30 〜 +30 の下限)
            Assert.That(min, Is.EqualTo(-30));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-154")]
        public void Given_デフォルトStubGameConfig_When_DdpPoolMax_Then_プラス30()
        {
            // Given
            var config = new StubGameConfig();
            // When
            var max = config.DdpPool.Max();
            // Then(ADR-0009 §「DDP プールの構造」: -30 〜 +30 の上限)
            Assert.That(max, Is.EqualTo(30));
        }
    }
}
