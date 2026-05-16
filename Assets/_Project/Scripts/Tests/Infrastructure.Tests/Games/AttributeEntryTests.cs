using NUnit.Framework;
using Drowsy.Infrastructure.Games.DrowZzz;

namespace Drowsy.Infrastructure.Tests.Games
{
    /// <summary>
    /// <see cref="AttributeEntry"/> の internal ctor + getter 検証
    /// (B-5 第 1 弾、Infrastructure カバレッジ補完、INF-132 / INF-133)。
    /// </summary>
    /// <remarks>
    /// `AttributeEntry` は <c>[Serializable] sealed class</c> + <c>[SerializeField] private</c> fields(`_key` / `_value`)+ <c>internal ctor</c> で構築する POCO。
    /// 本 fixture は <c>InternalsVisibleTo("Drowsy.Infrastructure.Tests")</c> 経由で internal ctor を呼ぶ
    /// (`Assets/_Project/Scripts/Infrastructure/AssemblyInfo.cs`)。
    /// </remarks>
    [TestFixture]
    public sealed class AttributeEntryTests
    {
        // ===== INF-132: ctor 経由で Key / Value getter が入力と一致 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-132")]
        [TestCase("key1", 0)]
        [TestCase("attr-a", 42)]
        [TestCase("", -1)]
        public void Given_internal_ctor_When_Construct_Then_Key_Value_getterが入力と一致(string key, int value)
        {
            // Given / When
            var entry = new AttributeEntry(key, value);

            // Then
            Assert.That(entry.Key, Is.EqualTo(key));
            Assert.That(entry.Value, Is.EqualTo(value));
        }

        // ===== INF-133: null key は防御なし(getter が null を透過) =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-133")]
        public void Given_null_key_When_Construct_Then_Key_getterがnull()
        {
            // Given / When
            var entry = new AttributeEntry(null, 0);

            // Then(本 ctor は防御なし、INF-001 / INF-003 で親が検証)
            Assert.That(entry.Key, Is.Null);
        }
    }
}
