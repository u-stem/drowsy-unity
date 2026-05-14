using NUnit.Framework;
using Drowsy.Domain.Configuration;

namespace Drowsy.Domain.Tests.Configuration
{
    /// <summary>
    /// <see cref="LanguageCodes.IsSupported"/> の直接機械検証(2026-05-13 カバレッジ補完
    /// PR で USR-005 を `[Ubiquitous]` → 通常要件化、`LanguageCodesTests` 直接テストに昇格)。
    /// </summary>
    [TestFixture]
    public class LanguageCodesTests
    {
        // null 以外の正常 / 異常コードを `[TestCase]` で網羅(`null` は C# attribute 構文上の
        // 曖昧性回避のため別メソッドに分離、CLAUDE.md §5 「1 テスト 1 アサーション」維持)。
        // 6 ケースのうち 2 件が正常入力(`ja`/`en` → true)、4 件が未対応入力(`zh`/`JA`/`ja-JP`/`""` → false)で
        // 混在するため Category は `SemiNormal`(code-reviewer S-2 反映、CLAUDE.md §6 Type Category 規約)。
        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "USR-005")]
        [TestCase("ja", true)]
        [TestCase("en", true)]
        [TestCase("zh", false)]
        [TestCase("JA", false)]
        [TestCase("ja-JP", false)]
        [TestCase("", false)]
        public void Given_LanguageCode_When_IsSupported_Then_期待値を返す(string code, bool expected)
        {
            // Given / When
            var result = LanguageCodes.IsSupported(code);

            // Then
            Assert.That(result, Is.EqualTo(expected));
        }

        // null 経路のみ独立メソッドで検証(`LanguageCodes.IsSupported` の null セーフな const 比較経路)。
        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-005")]
        public void Given_LanguageCodeがnull_When_IsSupported_Then_false()
        {
            // Given / When
            var result = LanguageCodes.IsSupported(null);

            // Then
            Assert.That(result, Is.False);
        }

        // Supported は LanguageCodes.cs の式本体プロパティ(M5-PR6 で static readonly フィールドから変更、
        // .cctor 計測限界を回避)。Drowsy.Domain.Tests から直接参照して計測区間内で getter を実行させる。
        // USR-005「Supported language codes shall be "ja" and "en"」の検証。Is.EqualTo で順序まで保証する
        // (USR-005 の記載順 "ja" → "en"、UserSettingsBinder が Dropdown choices に使うため表示順も確定、
        // code-reviewer M5-PR6 T-1)。
        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-005")]
        public void Given_Supported_When_参照_Then_jaとenがこの順で並ぶ()
        {
            // Given / When / Then
            Assert.That(LanguageCodes.Supported, Is.EqualTo(new[] { LanguageCodes.Ja, LanguageCodes.En }));
        }
    }
}
