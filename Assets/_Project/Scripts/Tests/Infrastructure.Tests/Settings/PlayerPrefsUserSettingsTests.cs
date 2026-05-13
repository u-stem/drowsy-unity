// M4-PR1 / PR2 / PR5 で確立した「Infrastructure.Tests における
// NUnit Property ↔ UnityEngine Property 衝突回避」パターン A(両 using 採用)を継承。
// 本ファイルは PlayerPrefs を直接呼ぶため UnityEngine 利用必須、alias を追加して
// NUnit.Framework.PropertyAttribute / UnityEngine.PropertyAttribute の CS1614 を回避。
using System;
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;
using Drowsy.Domain.Configuration;
using Drowsy.Infrastructure.Settings;
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Settings
{
    /// <summary>
    /// <see cref="PlayerPrefsUserSettings"/> の round-trip / clamp / Observable 発火 /
    /// 異常系を検証する EditMode テスト群。`Infrastructure/AssemblyInfo.cs` の
    /// `InternalsVisibleTo("Drowsy.Infrastructure.Tests")` 経由で internal
    /// <see cref="PlayerPrefsKeys"/> 定数を参照し、key literal の二重定義を避ける(M4-PR6
    /// code-reviewer 提案 S-1 反映)。
    /// </summary>
    [TestFixture]
    public sealed class PlayerPrefsUserSettingsTests
    {
        [SetUp]
        public void SetUp()
        {
            // PlayerPrefs は EditMode テスト間で共有状態を持つため、各テスト前にクリーンアップ
            PlayerPrefs.DeleteAll();
        }

        [TearDown]
        public void TearDown()
        {
            // テスト後もクリーンアップして次テストへのリーク防止 + Unity Editor 起動時のごみ残り防止
            PlayerPrefs.DeleteAll();
        }

        // ===== USR-007: PlayerPrefs 空状態で 3 項目 default 復元 =====
        // 1 テスト 1 アサーション(CLAUDE.md §5)を維持するため BGM / SE / Language の 3 メソッド分割。

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-007")]
        public void Given_PlayerPrefs空_When_インスタンス化_Then_BgmVolumeがdefault値を返す()
        {
            // Given (SetUp で PlayerPrefs.DeleteAll() 済)

            // When
            using var settings = new PlayerPrefsUserSettings();

            // Then
            Assert.That(settings.BgmVolume, Is.EqualTo(UserSettingsDefaults.BgmVolume));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-007")]
        public void Given_PlayerPrefs空_When_インスタンス化_Then_SeVolumeがdefault値を返す()
        {
            // Given

            // When
            using var settings = new PlayerPrefsUserSettings();

            // Then
            Assert.That(settings.SeVolume, Is.EqualTo(UserSettingsDefaults.SeVolume));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-007")]
        public void Given_PlayerPrefs空_When_インスタンス化_Then_Languageがdefault値を返す()
        {
            // Given

            // When
            using var settings = new PlayerPrefsUserSettings();

            // Then
            Assert.That(settings.Language, Is.EqualTo(UserSettingsDefaults.Language));
        }

        // ===== USR-008 / USR-009: PlayerPrefs に値が入っている時の復元 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-008")]
        public void Given_PlayerPrefsに0_3_When_インスタンス化_Then_BgmVolumeは0_3を返す()
        {
            // Given
            PlayerPrefs.SetFloat(PlayerPrefsKeys.BgmVolume, 0.3f);

            // When
            using var settings = new PlayerPrefsUserSettings();

            // Then
            Assert.That(settings.BgmVolume, Is.EqualTo(0.3f));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-009")]
        public void Given_PlayerPrefsにen_When_インスタンス化_Then_Languageはenを返す()
        {
            // Given
            PlayerPrefs.SetString(PlayerPrefsKeys.Language, LanguageCodes.En);

            // When
            using var settings = new PlayerPrefsUserSettings();

            // Then
            Assert.That(settings.Language, Is.EqualTo(LanguageCodes.En));
        }

        // ===== USR-006: SetBgmVolume の Getter / PlayerPrefs 両方反映(2 アサーション分割)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-006")]
        public void Given_PlayerPrefs空_When_SetBgmVolume半端値_Then_BgmVolumeが反映される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When
            settings.SetBgmVolume(0.42f);

            // Then
            Assert.That(settings.BgmVolume, Is.EqualTo(0.42f));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-006")]
        public void Given_PlayerPrefs空_When_SetBgmVolume半端値_Then_PlayerPrefsBgmKeyが反映される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When
            settings.SetBgmVolume(0.42f);

            // Then
            Assert.That(PlayerPrefs.GetFloat(PlayerPrefsKeys.BgmVolume), Is.EqualTo(0.42f));
        }

        // ===== USR-014: SetSeVolume の Getter / PlayerPrefs 両方反映(2 アサーション分割)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-014")]
        public void Given_PlayerPrefs空_When_SetSeVolume半端値_Then_SeVolumeが反映される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When
            settings.SetSeVolume(0.42f);

            // Then
            Assert.That(settings.SeVolume, Is.EqualTo(0.42f));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-014")]
        public void Given_PlayerPrefs空_When_SetSeVolume半端値_Then_PlayerPrefsSeKeyが反映される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When
            settings.SetSeVolume(0.42f);

            // Then
            Assert.That(PlayerPrefs.GetFloat(PlayerPrefsKeys.SeVolume), Is.EqualTo(0.42f));
        }

        // ===== USR-017: SetLanguage の Getter / PlayerPrefs 両方反映(2 アサーション分割)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-017")]
        public void Given_PlayerPrefs空_When_SetLanguageEn_Then_LanguageにEnが反映される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When
            settings.SetLanguage(LanguageCodes.En);

            // Then
            Assert.That(settings.Language, Is.EqualTo(LanguageCodes.En));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-017")]
        public void Given_PlayerPrefs空_When_SetLanguageEn_Then_PlayerPrefsLangKeyにEnが反映される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When
            settings.SetLanguage(LanguageCodes.En);

            // Then
            Assert.That(PlayerPrefs.GetString(PlayerPrefsKeys.Language), Is.EqualTo(LanguageCodes.En));
        }

        // ===== USR-010 / USR-011 / USR-012: Observable 発火 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-010")]
        public void Given_BgmVolumeChangedをSubscribe_When_SetBgmVolume_Then_clamped値が発火される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();
            var received = new List<float>();
            // R3 ReactiveProperty は Subscribe 時に現在値を即発火するため、初期 0.5 が received[0] に入る
            using var subscription = settings.BgmVolumeChanged.Subscribe(v => received.Add(v));

            // When
            settings.SetBgmVolume(0.42f);

            // Then(初期値 0.5 + Set 後の 0.42 = 2 件)
            Assert.That(received, Is.EqualTo(new[] { UserSettingsDefaults.BgmVolume, 0.42f }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-011")]
        public void Given_SeVolumeChangedをSubscribe_When_SetSeVolume_Then_clamped値が発火される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();
            var received = new List<float>();
            using var subscription = settings.SeVolumeChanged.Subscribe(v => received.Add(v));

            // When
            settings.SetSeVolume(0.42f);

            // Then
            Assert.That(received, Is.EqualTo(new[] { UserSettingsDefaults.SeVolume, 0.42f }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-012")]
        public void Given_LanguageChangedをSubscribe_When_SetLanguage_Then_codeが発火される()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();
            var received = new List<string>();
            using var subscription = settings.LanguageChanged.Subscribe(v => received.Add(v));

            // When
            settings.SetLanguage(LanguageCodes.En);

            // Then
            Assert.That(received, Is.EqualTo(new[] { UserSettingsDefaults.Language, LanguageCodes.En }));
        }

        // ===== USR-013: Save → 再インスタンス化で永続化確認 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-013")]
        public void Given_Set後_When_SaveしてからPlayerPrefsを再読み込み_Then_永続化されている()
        {
            // Given
            using (var settings = new PlayerPrefsUserSettings())
            {
                settings.SetBgmVolume(0.7f);

                // When
                settings.Save();
            }

            // Then(新規 instance が PlayerPrefs から復元できれば永続化されている = PlayerPrefs.Save() が呼ばれた証拠)
            using var reloaded = new PlayerPrefsUserSettings();
            Assert.That(reloaded.BgmVolume, Is.EqualTo(0.7f));
        }

        // ===== USR-015 / USR-016: clamp の上下限 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-015")]
        public void Given_PlayerPrefs空_When_SetBgmVolume_1_5_Then_BgmVolumeは1_0にclampされる()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When
            settings.SetBgmVolume(1.5f);

            // Then
            Assert.That(settings.BgmVolume, Is.EqualTo(UserSettingsDefaults.MaxVolume));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "USR-016")]
        public void Given_PlayerPrefs空_When_SetBgmVolume_minus0_5_Then_BgmVolumeは0_0にclampされる()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When
            settings.SetBgmVolume(-0.5f);

            // Then
            Assert.That(settings.BgmVolume, Is.EqualTo(UserSettingsDefaults.MinVolume));
        }

        // ===== USR-018 / USR-019 / USR-026: ctor の範囲外 / 未対応値 default 復帰 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-018")]
        public void Given_PlayerPrefsに範囲外BGM_When_インスタンス化_Then_default0_5に復帰する()
        {
            // Given(手書き編集 / 過去ファイルで範囲外値が入った状態を模擬)
            PlayerPrefs.SetFloat(PlayerPrefsKeys.BgmVolume, 2.0f);

            // When
            using var settings = new PlayerPrefsUserSettings();

            // Then
            Assert.That(settings.BgmVolume, Is.EqualTo(UserSettingsDefaults.BgmVolume));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-026")]
        public void Given_PlayerPrefsに範囲外SE_When_インスタンス化_Then_default0_5に復帰する()
        {
            // Given(下限側の範囲外、USR-018 の上限側と対称)
            PlayerPrefs.SetFloat(PlayerPrefsKeys.SeVolume, -1.0f);

            // When
            using var settings = new PlayerPrefsUserSettings();

            // Then
            Assert.That(settings.SeVolume, Is.EqualTo(UserSettingsDefaults.SeVolume));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-019")]
        public void Given_PlayerPrefsに未対応lang_When_インスタンス化_Then_default_jaに復帰する()
        {
            // Given(未対応コード「zh」を模擬)
            PlayerPrefs.SetString(PlayerPrefsKeys.Language, "zh");

            // When
            using var settings = new PlayerPrefsUserSettings();

            // Then
            Assert.That(settings.Language, Is.EqualTo(UserSettingsDefaults.Language));
        }

        // ===== USR-020 / USR-021: Setter 異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-020")]
        public void Given_PlayerPrefs空_When_SetLanguageNull_Then_ArgumentNullException()
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When / Then
            Assert.Throws<ArgumentNullException>(() => settings.SetLanguage(null));
        }

        // 1 テスト 1 アサーション(CLAUDE.md §5)を維持するため [TestCase] で分割。
        // 空文字 / 未対応コード / 大文字小文字違い / locale 拡張をそれぞれ独立検証する。
        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-021")]
        [TestCase("")]
        [TestCase("zh")]
        [TestCase("JA")]
        [TestCase("ja-JP")]
        public void Given_PlayerPrefs空_When_SetLanguage未対応コード_Then_ArgumentException(string code)
        {
            // Given
            using var settings = new PlayerPrefsUserSettings();

            // When / Then
            Assert.Throws<ArgumentException>(() => settings.SetLanguage(code));
        }

        // ===== USR-022 〜 USR-025: Dispose 後の操作 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-022")]
        public void Given_Dispose済_When_SetBgmVolume_Then_ObjectDisposedException()
        {
            // Given
            var settings = new PlayerPrefsUserSettings();
            settings.Dispose();

            // When / Then
            Assert.Throws<ObjectDisposedException>(() => settings.SetBgmVolume(0.5f));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-023")]
        public void Given_Dispose済_When_SetSeVolume_Then_ObjectDisposedException()
        {
            // Given
            var settings = new PlayerPrefsUserSettings();
            settings.Dispose();

            // When / Then
            Assert.Throws<ObjectDisposedException>(() => settings.SetSeVolume(0.5f));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-024")]
        public void Given_Dispose済_When_SetLanguage_Then_ObjectDisposedException()
        {
            // Given
            var settings = new PlayerPrefsUserSettings();
            settings.Dispose();

            // When / Then
            Assert.Throws<ObjectDisposedException>(() => settings.SetLanguage(LanguageCodes.En));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "USR-025")]
        public void Given_Dispose済_When_Save_Then_ObjectDisposedException()
        {
            // Given
            var settings = new PlayerPrefsUserSettings();
            settings.Dispose();

            // When / Then
            Assert.Throws<ObjectDisposedException>(() => settings.Save());
        }
    }
}
