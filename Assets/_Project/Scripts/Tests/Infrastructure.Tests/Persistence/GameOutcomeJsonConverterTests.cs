using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using Drowsy.Infrastructure.Persistence;
using Newtonsoft.Json;
using NUnit.Framework;
// 本ファイルは UnityEngine を import しない(ScriptableObject 等の依存なし、code-reviewer W-1 反映)。
// `using Property = NUnit.Framework.PropertyAttribute` alias は不採用:
// alias と `using NUnit.Framework;` 経由で import される `NUnit.Framework.Property`(class)が
// `Property` 名で衝突して CS1614 が発生するため(`csharp-nunit-unityengine-property-conflict` memory の
// 元シナリオは UnityEngine と NUnit の PropertyAttribute 衝突だが、本ファイルでは UnityEngine 不要なので
// alias 自体が衝突源になる、code-reviewer W-1 反映後 verify で発覚 2026-05-13)。

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="Converters.GameOutcomeJsonConverter"/> の round-trip 検証(M4-PR5、INF-051 / INF-068 / INF-069)。
    /// </summary>
    [TestFixture]
    public sealed class GameOutcomeJsonConverterTests
    {
        private static JsonSerializerSettings Settings => DrowZzzJsonSettings.Create();

        private static GameOutcome RoundTrip(GameOutcome outcome)
        {
            var json = JsonConvert.SerializeObject(outcome, Settings);
            return JsonConvert.DeserializeObject<GameOutcome>(json, Settings);
        }

        // ===== INF-051: WinnerOutcome / DrawOutcome round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-051")]
        public void Given_WinnerOutcome_When_RoundTrip_Then_等価()
        {
            var original = new WinnerOutcome(PlayerId.Of("PlayerA"));
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-051")]
        public void Given_DrawOutcome_When_RoundTrip_Then_等価()
        {
            var original = new DrawOutcome();
            Assert.That(RoundTrip(original), Is.EqualTo(original));
        }

        // ===== INF-068: type 欠落 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-068")]
        public void Given_typeフィールド欠落_When_Deserialize_Then_JsonSerializationException()
        {
            const string json = "{\"winner\": \"PlayerA\"}";
            Assert.That(
                () => JsonConvert.DeserializeObject<GameOutcome>(json, Settings),
                Throws.TypeOf<JsonSerializationException>().With.Message.Contains("type"));
        }

        // ===== INF-069: type 未知 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-069")]
        public void Given_未知のtype値_When_Deserialize_Then_JsonSerializationException()
        {
            const string json = "{\"type\": \"UnknownOutcomeType\"}";
            Assert.That(
                () => JsonConvert.DeserializeObject<GameOutcome>(json, Settings),
                Throws.TypeOf<JsonSerializationException>().With.Message.Contains("UnknownOutcomeType"));
        }
    }
}
