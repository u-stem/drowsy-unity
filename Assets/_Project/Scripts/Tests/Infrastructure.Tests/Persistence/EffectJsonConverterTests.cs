using System.Collections.Generic;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Infrastructure.Persistence;
// 本ファイルは UnityEngine を import しない(ScriptableObject 等の依存なし、code-reviewer W-1 反映)。
// `using Property = NUnit.Framework.PropertyAttribute` alias は不採用:
// alias と `using NUnit.Framework;` 経由で import される `NUnit.Framework.Property`(class)が
// `Property` 名で衝突して CS1614 が発生するため(`csharp-nunit-unityengine-property-conflict` memory の
// 元シナリオは UnityEngine と NUnit の PropertyAttribute 衝突だが、本ファイルでは UnityEngine 不要なので
// alias 自体が衝突源になる、code-reviewer W-1 反映後 verify で発覚 2026-05-13)。

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="Converters.EffectJsonConverter"/> の round-trip 検証(M4-PR5、INF-050 / INF-055 / INF-066 / INF-067)。
    /// 12 IEffect 派生型すべてを <c>JsonConvert.SerializeObject</c> ↔ <c>DeserializeObject&lt;IEffect&gt;</c> 経路で検証する。
    /// </summary>
    [TestFixture]
    public sealed class EffectJsonConverterTests
    {
        private static JsonSerializerSettings Settings => DrowZzzJsonSettings.Create();

        private static IEffect RoundTrip(IEffect effect)
        {
            var json = JsonConvert.SerializeObject(effect, Settings);
            return JsonConvert.DeserializeObject<IEffect>(json, Settings);
        }

        // ===== INF-050: 12 派生型 round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_AdjustSdpEffect_When_RoundTrip_Then_等価()
        {
            var original = new AdjustSdpEffect(SdpTarget.Opponent, -7);
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_ApplyInfluenceEffect_When_RoundTrip_Then_等価()
        {
            var original = new ApplyInfluenceEffect(
                Target: SdpTarget.Self,
                Influence: new PlayerInfluence(
                    Trigger: InfluenceTrigger.OwnPhaseStart,
                    TickEffect: new AdjustSdpEffect(SdpTarget.Self, 1),
                    RemainingCount: 3));
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_RemoveInfluenceEffect_When_RoundTrip_Then_等価()
        {
            var original = new RemoveInfluenceEffect(SdpTarget.Self);
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_DrawCardEffect_When_RoundTrip_Then_等価()
        {
            var original = new DrawCardEffect(SdpTarget.Self, 2);
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_DamageBedEffect_When_RoundTrip_Then_等価()
        {
            var original = new DamageBedEffect(SdpTarget.Opponent, 25);
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_EarlyWinTriggerEffect_When_RoundTrip_Then_等価()
        {
            var original = new EarlyWinTriggerEffect();
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_ChoiceEffect_When_RoundTrip_Then_等価()
        {
            var original = new ChoiceEffect(new IReadOnlyList<IEffect>[]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -3) },
                new IEffect[] { new DrawCardEffect(SdpTarget.Opponent, 1) },
            });
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_TimeOfDayBranchEffect_When_RoundTrip_Then_等価()
        {
            var original = new TimeOfDayBranchEffect(
                nightEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -4),
                    new DrawCardEffect(SdpTarget.Self, 1),
                },
                morningEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Opponent, 5),
                });
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_KeywordedEffect_When_RoundTrip_Then_等価()
        {
            var original = new KeywordedEffect(
                keywords: new[] { Keyword.Counter, Keyword.Frenzy },
                inner: new AdjustSdpEffect(SdpTarget.Opponent, -10));
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_RequiresMinimumTotalPointsMarkerEffect_When_RoundTrip_Then_等価()
        {
            var original = new RequiresMinimumTotalPointsMarkerEffect(100);
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_UsageRestrictionMarkerEffect_When_RoundTrip_Then_等価()
        {
            var original = new UsageRestrictionMarkerEffect();
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_AssociatableMarkerEffect_When_RoundTrip_Then_等価()
        {
            var original = new AssociatableMarkerEffect();
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        // ===== INF-139: ADR-0019 PR ② 追加 2 effect の Persistence Round-Trip(code-reviewer P-4 反映 2026-05-17)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-139")]
        public void Given_RestrictSpecificCardInfluenceEffect_When_RoundTrip_Then_等価()
        {
            // Given(CardTypeId "X" を TargetCardTypeId に持つ marker)
            var original = new RestrictSpecificCardInfluenceEffect(Drowsy.Domain.Cards.CardTypeId.Of("X"));
            // When / Then(round-trip 経由で CardTypeId.Value も含めて値同値)
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-139")]
        public void Given_ApplyTargetedRestrictionEffect_When_RoundTrip_Then_等価()
        {
            // Given(Opponent / RemainingCount=2 の動的影響付与効果)
            var original = new ApplyTargetedRestrictionEffect(SdpTarget.Opponent, 2);
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        // ===== INF-055: wrapper 再帰(KeywordedEffect (Choice (Keyworded (AdjustSdp))))=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "INF-055")]
        public void Given_3段ネストWrapper_When_RoundTrip_Then_最深まで等価()
        {
            // Given(Keyworded → Choice → Keyworded → AdjustSdp の 3 段ネスト)
            var deepest = new AdjustSdpEffect(SdpTarget.Self, -1);
            var middleWrap = new KeywordedEffect(new[] { Keyword.Instinct }, deepest);
            var choice = new ChoiceEffect(new IReadOnlyList<IEffect>[]
            {
                new IEffect[] { middleWrap },
                new IEffect[] { new EarlyWinTriggerEffect() },
            });
            var original = new KeywordedEffect(new[] { Keyword.Counter }, choice);

            // When
            var result = RoundTrip(original);

            // Then
            Assert.That(result, Is.EqualTo(original));
        }

        // ===== INF-066: type 欠落 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-066")]
        public void Given_typeフィールド欠落_When_Deserialize_Then_JsonSerializationException()
        {
            const string json = "{\"target\": \"Self\", \"delta\": 1}";
            Assert.That(
                () => JsonConvert.DeserializeObject<IEffect>(json, Settings),
                Throws.TypeOf<JsonSerializationException>().With.Message.Contains("type"));
        }

        // ===== INF-067: type 未知 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-067")]
        public void Given_未知のtype値_When_Deserialize_Then_JsonSerializationException()
        {
            const string json = "{\"type\": \"UnknownEffectType\"}";
            Assert.That(
                () => JsonConvert.DeserializeObject<IEffect>(json, Settings),
                Throws.TypeOf<JsonSerializationException>().With.Message.Contains("UnknownEffectType"));
        }

        // ===== INF-050 補助: discriminator 命名検証(serialize の "type" 値が EffectAsset と整合)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-050")]
        public void Given_AdjustSdpEffect_When_Serialize_Then_typeはAdjustSdp()
        {
            var json = JObject.Parse(JsonConvert.SerializeObject(
                new AdjustSdpEffect(SdpTarget.Self, 0), Settings));
            Assert.That(json["type"]?.ToString(), Is.EqualTo("AdjustSdp"));
        }

        // ===== INF-134 / INF-135: 必須キー欠落で JsonSerializationException(Infra W-1 / W-2 post-Phase2 レビュー反映)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-134")]
        public void Given_AdjustSdpでdeltaキー欠落_When_Deserialize_Then_JsonSerializationException()
        {
            // Given(target はあるが delta が欠落)
            const string json = "{\"type\": \"AdjustSdp\", \"target\": \"Self\"}";
            // When / Then(NullReferenceException ではなく JsonSerializationException で診断価値のある欠落キー名を返す)
            Assert.That(
                () => JsonConvert.DeserializeObject<IEffect>(json, Settings),
                Throws.TypeOf<JsonSerializationException>().With.Message.Contains("delta"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-135")]
        public void Given_TimeOfDayBranchでnightEffectsキー欠落_When_Deserialize_Then_JsonSerializationException()
        {
            // Given(morningEffects はあるが nightEffects が欠落)
            const string json = "{\"type\": \"TimeOfDayBranch\", \"morningEffects\": []}";
            // When / Then(「効果列の必須キー 'nightEffects' が見つかりません」と診断)
            Assert.That(
                () => JsonConvert.DeserializeObject<IEffect>(json, Settings),
                Throws.TypeOf<JsonSerializationException>().With.Message.Contains("nightEffects"));
        }
    }
}
