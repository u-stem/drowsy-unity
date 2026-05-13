using NUnit.Framework;
using UnityEngine;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
// NUnit と UnityEngine の双方が PropertyAttribute を提供するため曖昧参照を回避する type alias
// (M4-PR1 で確立、M4-PR2 で `using UnityEngine;` も両方必須と判明、`csharp-nunit-unityengine-property-conflict`
// memory 永続化済。M4-PR3 code-reviewer P-3 反映 2026-05-13:コメントを M4-PR1〜PR2 慣例に揃える)
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// 中間型 <see cref="PlayerInfluenceAsset"/> の `ToDomain()` テスト(M4-PR3、INF-021)。
    /// 再帰 TickEffect 経路と <see cref="PlayerInfluence"/> 値伝達を検証。INF-020(Ubiquitous structural)はテスト免除。
    /// INF-026(<see cref="ApplyInfluenceEffectAsset"/> 経由)も本 fixture でカバー(再帰呼び出しを統合検証)。
    /// </summary>
    [TestFixture]
    public sealed class PlayerInfluenceAssetTests
    {
        // ===== INF-021: ToDomain 値伝達 + 再帰 TickEffect =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-021")]
        public void Given_OwnPhaseStart_AdjustSdpSelf5_RemainingCount3_When_ToDomain_Then_PlayerInfluence値同値()
        {
            // Given(M2-PR5 緑の侵攻 GreenInvasionInfluence と同じ構造:Tick で SDP -5、3 回発動)
            var tickEffect = new AdjustSdpEffectAsset(SdpTarget.Self, -5);
            var asset = new PlayerInfluenceAsset(InfluenceTrigger.OwnPhaseStart, tickEffect, 3);
            // When
            var domain = asset.ToDomain();
            // Then(record auto-equals で 3 フィールド + 内部 TickEffect 値同値)
            var expected = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, -5),
                3);
            Assert.That(domain, Is.EqualTo(expected));
        }

        // ===== INF-026: ApplyInfluenceEffectAsset 経由(PlayerInfluenceAsset を再帰利用)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-026")]
        public void Given_ApplyInfluenceEffectAssetが内部にPlayerInfluenceAsset_When_ToDomain_Then_ApplyInfluenceEffectを再帰構築()
        {
            // Given(緑の侵攻 PR2-PR5 経由の Influence 付与効果と同等)
            var tickEffect = new AdjustSdpEffectAsset(SdpTarget.Self, -5);
            var influenceAsset = new PlayerInfluenceAsset(InfluenceTrigger.OwnPhaseStart, tickEffect, 3);
            var asset = new ApplyInfluenceEffectAsset(SdpTarget.Opponent, influenceAsset);
            // When
            var effect = asset.ToDomain();
            // Then(再帰構築された ApplyInfluenceEffect + PlayerInfluence の record 値同値)
            var expected = new ApplyInfluenceEffect(
                SdpTarget.Opponent,
                new PlayerInfluence(
                    InfluenceTrigger.OwnPhaseStart,
                    new AdjustSdpEffect(SdpTarget.Self, -5),
                    3));
            Assert.That(effect, Is.EqualTo(expected));
        }
    }
}
