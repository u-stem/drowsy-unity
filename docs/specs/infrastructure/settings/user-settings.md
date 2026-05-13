# IUserSettings + PlayerPrefsUserSettings

## 概要

`Drowsy.Domain.Configuration.IUserSettings` interface(L4 ユーザー設定、CLAUDE.md §9)と PlayerPrefs 実装 `Drowsy.Infrastructure.Settings.PlayerPrefsUserSettings` の仕様。BGM 音量 / SE 音量 / 言語コードの 3 項目を getter + setter + R3 `Observable<T>` + `Save()` の対称 API で提供する。

ADR-0012 §8「`IUserSettings` + PlayerPrefs(サブスコープ、M4-PR6)」+ M4-PR6 着手時の JIT 確定(2026-05-13、4 項目)に基づく実装。

## 普遍要件 (Ubiquitous)

- [USR-001] [Ubiquitous] The `IUserSettings` shall expose three properties: `BgmVolume` (float), `SeVolume` (float), `Language` (string).(構造的保証、テスト免除)
- [USR-002] [Ubiquitous] The `PlayerPrefsUserSettings` shall use the key prefix `drowsy.*` (`drowsy.bgm` / `drowsy.se` / `drowsy.lang`).(構造的保証、INF-014 / USR-006 系で間接検証)
- [USR-003] [Ubiquitous] Default values shall be `BgmVolume` = 0.5, `SeVolume` = 0.5, `Language` = `"ja"`.(構造的保証、USR-007 / USR-008 / USR-009 で検証)
- [USR-004] [Ubiquitous] `BgmVolume` and `SeVolume` shall be in the range [0.0, 1.0].(Setter clamp + ctor default 復帰で保証、USR-015 / USR-016 系で検証)
- [USR-005] Supported language codes shall be `"ja"` and `"en"`, and `LanguageCodes.IsSupported(code)` shall return `true` for these codes and `false` for any other input (including `null` / empty / case-different / locale-extended forms).(`[Ubiquitous]` マーカー削除して通常要件化、`LanguageCodesTests` 直接テストで機械検証、2026-05-13 カバレッジ補完 PR で昇格)

## 事象駆動要件 (Event-driven)

- [USR-006] When `SetBgmVolume(v)` is called with `v ∈ [0.0, 1.0]`, the `BgmVolume` getter shall return `v` and `PlayerPrefs.GetFloat("drowsy.bgm")` shall return `v`.
- [USR-007] When the ctor is invoked with no PlayerPrefs entries, `BgmVolume` / `SeVolume` / `Language` shall return their default values.
- [USR-008] When the ctor is invoked with `PlayerPrefs.GetFloat("drowsy.bgm")` = 0.3, the `BgmVolume` getter shall return 0.3.
- [USR-009] When the ctor is invoked with `PlayerPrefs.GetString("drowsy.lang")` = `"en"`, the `Language` getter shall return `"en"`.
- [USR-010] When `SetBgmVolume(v)` is called, `BgmVolumeChanged` shall emit the clamped value.
- [USR-011] When `SetSeVolume(v)` is called, `SeVolumeChanged` shall emit the clamped value.
- [USR-012] When `SetLanguage(code)` is called with a supported code, `LanguageChanged` shall emit `code`.
- [USR-013] When `Save()` is called, `PlayerPrefs.Save()` shall be invoked (verified by re-instantiation reading the value).
- [USR-014] When `SetSeVolume(v)` is called with `v ∈ [0.0, 1.0]`, the `SeVolume` getter shall return `v` and `PlayerPrefs.GetFloat("drowsy.se")` shall return `v`.
- [USR-015] When `SetBgmVolume(v)` is called with `v > 1.0`, the `BgmVolume` getter shall return 1.0 (clamped).
- [USR-016] When `SetBgmVolume(v)` is called with `v < 0.0`, the `BgmVolume` getter shall return 0.0 (clamped).
- [USR-017] When `SetLanguage(code)` is called with `"en"`, the `Language` getter shall return `"en"` and `PlayerPrefs.GetString("drowsy.lang")` shall return `"en"`.
- [USR-027] When `Dispose()` is called after the first `Dispose()` (二重 Dispose), the second call shall be a silent no-op (冪等性、`if (_disposed) return` ガードで内部 `ReactiveProperty<T>.Dispose()` を二度呼ばない正常系設計、code-reviewer S-1 で Event-driven に分類変更、2026-05-13 カバレッジ補完 PR で新規追加).

## 状態駆動要件 (State-driven)

- [USR-018] While `PlayerPrefs.GetFloat("drowsy.bgm")` is out of [0.0, 1.0], the ctor shall fall back to the default `BgmVolume` value (data hygiene against manual editing of save files).
- [USR-019] While `PlayerPrefs.GetString("drowsy.lang")` is not in `LanguageCodes.Supported`, the ctor shall fall back to the default language `"ja"`.
- [USR-026] While `PlayerPrefs.GetFloat("drowsy.se")` is out of [0.0, 1.0], the ctor shall fall back to the default `SeVolume` value (USR-018 と対称、SE 側独立コードパスのカバレッジ確保)。

## 異常要件 (Unwanted)

- [USR-020] If `SetLanguage(null)` is called, `ArgumentNullException` shall be thrown.
- [USR-021] If `SetLanguage(code)` is called with `code` not in `LanguageCodes.Supported`, `ArgumentException` shall be thrown.
- [USR-022] If `SetBgmVolume(v)` is called after `Dispose()`, `ObjectDisposedException` shall be thrown.
- [USR-023] If `SetSeVolume(v)` is called after `Dispose()`, `ObjectDisposedException` shall be thrown.
- [USR-024] If `SetLanguage(code)` is called after `Dispose()`, `ObjectDisposedException` shall be thrown.
- [USR-025] If `Save()` is called after `Dispose()`, `ObjectDisposedException` shall be thrown.

## 任意要件 (Optional)

該当なし(R3 `Observable<T>` の購読解除や複数購読のテストは Phase 3 で M5 Presentation 連携時に拡張する)。

## 定数依存

CLAUDE.md §9「定数管理方針」階層別に分類:

- **L4(ユーザー設定の default 値、Domain `UserSettingsDefaults`)**:
  - `UserSettingsDefaults.BgmVolume` = 0.5(初回起動時 default、JIT 確定 2026-05-13)
  - `UserSettingsDefaults.SeVolume` = 0.5(同上)
  - `UserSettingsDefaults.Language` = `"ja"`(同上、CLAUDE.md §3 日本拠点既定)
  - `UserSettingsDefaults.MinVolume` = 0.0(L4 設定の許容 range 下限)
  - `UserSettingsDefaults.MaxVolume` = 1.0(L4 設定の許容 range 上限)
- **L2(ドメイン上の真の不変量、Domain `LanguageCodes`)**:
  - `LanguageCodes.Ja` = `"ja"`(ISO 639-1 形式の固定値)
  - `LanguageCodes.En` = `"en"`(同上)
  - `LanguageCodes.Supported` = `[Ja, En]`(M4-PR6 でサポートする言語の集合)
- **L2(Infrastructure 内部の真の不変量、Infrastructure `PlayerPrefsKeys`)**:
  - `PlayerPrefsKeys.BgmVolume` = `"drowsy.bgm"`(JIT 確定 2026-05-13、prefix `drowsy.*`)
  - `PlayerPrefsKeys.SeVolume` = `"drowsy.se"`(同上)
  - `PlayerPrefsKeys.Language` = `"drowsy.lang"`(同上)

## 関連

- 実装:
  - `Assets/_Project/Scripts/Domain/Configuration/IUserSettings.cs`
  - `Assets/_Project/Scripts/Domain/Configuration/UserSettingsDefaults.cs`
  - `Assets/_Project/Scripts/Domain/Configuration/LanguageCodes.cs`
  - `Assets/_Project/Scripts/Infrastructure/Settings/PlayerPrefsUserSettings.cs`
  - `Assets/_Project/Scripts/Infrastructure/Settings/PlayerPrefsKeys.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Settings/PlayerPrefsUserSettingsTests.cs`
- シナリオ: `user-settings.feature`(同ディレクトリ)
- ADR: [ADR-0012 §8](../../../adr/0012-m4-scriptableobject-and-persistence.md)

## トレーサビリティ

| 要件 ID | カバーするテスト(実メソッド名)| 備考 |
| ---- | ---- | ---- |
| USR-001 | (テスト免除: Ubiquitous) | interface の構造的保証 |
| USR-002 | (テスト免除: Ubiquitous) | key prefix は USR-006 / USR-014 / USR-017 で間接検証 |
| USR-003 | (テスト免除: Ubiquitous) | default は USR-007 で検証 |
| USR-004 | (テスト免除: Ubiquitous) | clamp range は USR-015 / USR-016 で検証 |
| USR-005 | `Given_LanguageCode_When_IsSupported_Then_期待値を返す`(`[TestCase]` 6 ケース:`"ja"`/`"en"`/`"zh"`/`"JA"`/`"ja-JP"`/`""`)+ `Given_LanguageCodeがnull_When_IsSupported_Then_false` | `LanguageCodes.IsSupported` の直接機械検証、2026-05-13 カバレッジ補完 PR で Ubiquitous 解除 |
| USR-006 | `Given_PlayerPrefs空_When_SetBgmVolume半端値_Then_BgmVolumeが反映される` / `Given_PlayerPrefs空_When_SetBgmVolume半端値_Then_PlayerPrefsBgmKeyが反映される` | 1 テスト 1 アサーション分割 |
| USR-007 | `Given_PlayerPrefs空_When_インスタンス化_Then_BgmVolumeがdefault値を返す` / `Given_PlayerPrefs空_When_インスタンス化_Then_SeVolumeがdefault値を返す` / `Given_PlayerPrefs空_When_インスタンス化_Then_Languageがdefault値を返す` | 3 項目 = 3 テスト分割 |
| USR-008 | `Given_PlayerPrefsに0_3_When_インスタンス化_Then_BgmVolumeは0_3を返す` | |
| USR-009 | `Given_PlayerPrefsにen_When_インスタンス化_Then_Languageはenを返す` | |
| USR-010 | `Given_BgmVolumeChangedをSubscribe_When_SetBgmVolume_Then_clamped値が発火される` | |
| USR-011 | `Given_SeVolumeChangedをSubscribe_When_SetSeVolume_Then_clamped値が発火される` | |
| USR-012 | `Given_LanguageChangedをSubscribe_When_SetLanguage_Then_codeが発火される` | |
| USR-013 | `Given_Set後_When_SaveしてからPlayerPrefsを再読み込み_Then_永続化されている` | |
| USR-014 | `Given_PlayerPrefs空_When_SetSeVolume半端値_Then_SeVolumeが反映される` / `Given_PlayerPrefs空_When_SetSeVolume半端値_Then_PlayerPrefsSeKeyが反映される` | 1 テスト 1 アサーション分割 |
| USR-015 | `Given_PlayerPrefs空_When_SetBgmVolume_1_5_Then_BgmVolumeは1_0にclampされる` | 上限 clamp |
| USR-016 | `Given_PlayerPrefs空_When_SetBgmVolume_minus0_5_Then_BgmVolumeは0_0にclampされる` | 下限 clamp |
| USR-017 | `Given_PlayerPrefs空_When_SetLanguageEn_Then_LanguageにEnが反映される` / `Given_PlayerPrefs空_When_SetLanguageEn_Then_PlayerPrefsLangKeyにEnが反映される` | 1 テスト 1 アサーション分割 |
| USR-018 | `Given_PlayerPrefsに範囲外BGM_When_インスタンス化_Then_default0_5に復帰する` | BGM ctor 範囲外復帰 |
| USR-019 | `Given_PlayerPrefsに未対応lang_When_インスタンス化_Then_default_jaに復帰する` | Lang ctor 未対応復帰 |
| USR-020 | `Given_PlayerPrefs空_When_SetLanguageNull_Then_ArgumentNullException` | |
| USR-021 | `Given_PlayerPrefs空_When_SetLanguage未対応コード_Then_ArgumentException`(`[TestCase]` 4 ケース: `""` / `"zh"` / `"JA"` / `"ja-JP"`)| |
| USR-022 | `Given_Dispose済_When_SetBgmVolume_Then_ObjectDisposedException` | |
| USR-023 | `Given_Dispose済_When_SetSeVolume_Then_ObjectDisposedException` | |
| USR-024 | `Given_Dispose済_When_SetLanguage_Then_ObjectDisposedException` | |
| USR-025 | `Given_Dispose済_When_Save_Then_ObjectDisposedException` | |
| USR-026 | `Given_PlayerPrefsに範囲外SE_When_インスタンス化_Then_default0_5に復帰する` | SE ctor 範囲外復帰(USR-018 BGM 対称) |
| USR-027 | `Given_既Dispose_When_2回目Dispose_Then_冪等で例外なし` | `_disposed` フラグ冪等性、Event-driven 系正常動作、2026-05-13 カバレッジ補完 PR で新規 |
