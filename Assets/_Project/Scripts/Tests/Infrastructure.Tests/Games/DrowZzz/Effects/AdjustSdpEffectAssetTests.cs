using NUnit.Framework;
using UnityEngine;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
// NUnit と UnityEngine の双方が PropertyAttribute を提供するため曖昧参照を回避する type alias
// (M4-PR1 で確立、`csharp-nunit-unityengine-property-conflict` memory 永続化済)。
// 注:`using UnityEngine;` を直接書かないと CS1614(`[Property(...)]` の `Attribute` suffix 補完で
// 2 つの `PropertyAttribute` 型が候補化)が発生する。`using UnityEngine;` を入れた状態で alias を入れると
// alias 解決が attribute 名解決時に正しく優先される(M4-PR2 fix 2026-05-13、ユーザー実機検証で判明)。
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AdjustSdpEffectAsset"/> の EditMode テスト(INF-016、M4-PR2)。
    /// `EffectAsset.ToDomain()` の値伝達を検証する。INF-013 / INF-014(Ubiquitous structural)はテスト免除。
    /// </summary>
    [TestFixture]
    public sealed class AdjustSdpEffectAssetTests
    {
        // ===== INF-016: ToDomain の値伝達 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-016")]
        public void Given_SelfMinus5_When_ToDomain_Then_AdjustSdpEffectSelfMinus5()
        {
            // Given
            var asset = new AdjustSdpEffectAsset(SdpTarget.Self, -5);
            // When
            var effect = asset.ToDomain();
            // Then(値同値で完全一致、record auto-equals)
            Assert.That(effect, Is.EqualTo(new AdjustSdpEffect(SdpTarget.Self, -5)));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-016")]
        public void Given_Opponent10_When_ToDomain_Then_AdjustSdpEffectOpponent10()
        {
            // Given(SdpTarget.Opponent + 正の Delta)
            var asset = new AdjustSdpEffectAsset(SdpTarget.Opponent, 10);
            // When
            var effect = asset.ToDomain();
            // Then
            Assert.That(effect, Is.EqualTo(new AdjustSdpEffect(SdpTarget.Opponent, 10)));
        }
    }
}
