using System;
using NUnit.Framework;
using UnityEngine.UIElements;
using Drowsy.Domain.Configuration;
using Drowsy.Presentation.Games.DrowZzz;

namespace Drowsy.Presentation.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="UserSettingsBinder"/> の単体テスト(M5-PR6)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §11 M5-PR6「Presentation テストで Subscribe 経路検証」で確定したスコープ。
    /// <see cref="UserSettingsBinder"/> は <see cref="VisualElement"/>(<see cref="Slider"/> /
    /// <see cref="DropdownField"/>)と <see cref="IUserSettings"/> のみに依存する Pure C# クラスのため、
    /// EditMode テストで <c>new Slider()</c> / <c>new DropdownField()</c> + <see cref="MockUserSettings"/> を
    /// 直接組み立てて検証できる(UIDocument 非依存)。
    /// <para>
    /// <b>テストスコープ</b>:ctor null 防御(PRES-023〜026)/ ctor の <c>choices</c> 設定(PRES-027)/
    /// settings → UI の Subscribe 経路(PRES-028)/ Dispose 冪等(PRES-029)。
    /// <b>UI → settings</b> 方向(<c>RegisterValueChangedCallback</c>)は <c>Slider.value</c> setter が
    /// <c>panel != null</c> のときのみ <c>ChangeEvent</c> を発火する UI Toolkit 仕様により、パネル(UIDocument)
    /// アタッチなしの EditMode 単体テストでは確実に検証できないため手動 QA に委ねる(ADR-0016 §10 と整合)。
    /// </para>
    /// <para>
    /// <see cref="MockUserSettings"/> の各 Observable は <c>ReactiveProperty</c> ベースで Subscribe 時に
    /// 現在値を即発火するため、<see cref="UserSettingsBinder"/> ctor 直後に UI 要素の値が settings の
    /// 現在値(BGM/SE = 0.5、Language = "ja")に揃う。
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class UserSettingsBinderTests
    {
        // ===== PRES-023〜026: ctor null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-023")]
        public void Given_bgmSliderNull_When_Ctor_Then_ArgumentNullException()
        {
            using var settings = new MockUserSettings();

            Assert.That(
                () => new UserSettingsBinder(null, new Slider(), new DropdownField(), settings),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-024")]
        public void Given_seSliderNull_When_Ctor_Then_ArgumentNullException()
        {
            using var settings = new MockUserSettings();

            Assert.That(
                () => new UserSettingsBinder(new Slider(), null, new DropdownField(), settings),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-025")]
        public void Given_languageDropdownNull_When_Ctor_Then_ArgumentNullException()
        {
            using var settings = new MockUserSettings();

            Assert.That(
                () => new UserSettingsBinder(new Slider(), new Slider(), null, settings),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-026")]
        public void Given_userSettingsNull_When_Ctor_Then_ArgumentNullException()
        {
            Assert.That(
                () => new UserSettingsBinder(new Slider(), new Slider(), new DropdownField(), null),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ===== PRES-027: ctor で languageDropdown.choices が LanguageCodes.Supported に設定される =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-027")]
        public void Given_ctor_When_Construct_Then_LanguageDropdownChoicesAreSupportedCodes()
        {
            // Given
            var dropdown = new DropdownField();
            using var settings = new MockUserSettings();

            // When
            using var binder = new UserSettingsBinder(new Slider(), new Slider(), dropdown, settings);

            // Then(choices が LanguageCodes.Supported = ["ja", "en"])
            Assert.That(dropdown.choices, Is.EquivalentTo(LanguageCodes.Supported));
        }

        // ===== PRES-028: settings → UI(Subscribe 経路)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-028")]
        public void Given_bound_When_SetBgmVolume_Then_BgmSliderValueUpdated()
        {
            // Given
            var bgmSlider = new Slider();
            using var settings = new MockUserSettings();
            using var binder = new UserSettingsBinder(bgmSlider, new Slider(), new DropdownField(), settings);

            // When(settings 側の値を変更)
            settings.SetBgmVolume(0.7f);

            // Then(BgmVolumeChanged の Subscribe 経由で bgmSlider.value が SetValueWithoutNotify される)
            Assert.That(bgmSlider.value, Is.EqualTo(0.7f));
        }

        // ===== PRES-029: Dispose 冪等性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-029")]
        public void Given_disposed_When_DisposeAgain_Then_NoException()
        {
            // Given
            using var settings = new MockUserSettings();
            var binder = new UserSettingsBinder(new Slider(), new Slider(), new DropdownField(), settings);
            binder.Dispose();

            // When / Then(2 回目の Dispose は silent no-op、例外を投げない)
            Assert.That(() => binder.Dispose(), Throws.Nothing);
        }
    }
}
