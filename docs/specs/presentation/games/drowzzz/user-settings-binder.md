# UserSettingsBinder (Presentation 層 設定 UI 双方向バインダ) (M5-PR6)

このファイルは `UserSettingsBinder` の契約を EARS で記述する。
ADR-0016 §4「R3 1.3.0 の利用範囲」+ §11「PR 分割計画」M5-PR6 で確定したスコープに対応する。

`UserSettingsBinder` は UI Toolkit の設定 UI 要素(BGM / SE `Slider` + Language `DropdownField`)と
`IUserSettings` を双方向バインドする Pure C# クラス。`DrowZzzGameView`(MonoBehaviour)から UIDocument 依存を
切り離すことで EditMode 単体テスト可能にする。

配置先: `docs/specs/presentation/games/drowzzz/user-settings-binder.md`

---

## 概要

`Drowsy.Presentation.Games.DrowZzz.UserSettingsBinder` は ctor で `Slider` × 2 + `DropdownField` + `IUserSettings`
を受け取り、双方向バインドする。settings → UI は R3 `Observable.Subscribe` + `SetValueWithoutNotify`(callback 抑止)、
UI → settings は `RegisterValueChangedCallback`。`IDisposable` で Subscribe / callback を対称解放する。
`DrowZzzGameView` は `[Inject]` で受け取った `IUserSettings` を本バインダに渡す(ADR-0016 §11 M5-PR6「View に直接 Inject」)。

## 普遍要件 (Ubiquitous)

- [PRES-022] [Ubiquitous] The `UserSettingsBinder` shall implement `System.IDisposable`.

## 異常要件 (Unwanted) — ctor 引数検査

- [PRES-023] If the ctor is called with `bgmSlider = null`, then the binder shall throw `ArgumentNullException`.
- [PRES-024] If the ctor is called with `seSlider = null`, then the binder shall throw `ArgumentNullException`.
- [PRES-025] If the ctor is called with `languageDropdown = null`, then the binder shall throw `ArgumentNullException`.
- [PRES-026] If the ctor is called with `userSettings = null`, then the binder shall throw `ArgumentNullException`.

## 事象駆動要件 (Event-driven)

- [PRES-027] When the ctor is invoked, the binder shall set `languageDropdown.choices` to `LanguageCodes.Supported` (codes displayed directly, no display-name mapping).
- [PRES-028] When `IUserSettings` raises a volume / language change (`BgmVolumeChanged` / `SeVolumeChanged` / `LanguageChanged`), the binder shall update the corresponding UI element's value via `SetValueWithoutNotify` (settings → UI, Subscribe path; verified for BGM as the representative case).
- [PRES-029] When `Dispose()` is invoked twice, the binder shall be idempotent (the second call shall be a silent no-op).

## 任意要件 (Optional)

- [PRES-030] [Optional] Where the user changes a `Slider` / `DropdownField` value through UI interaction, the binder shall propagate the new value to `IUserSettings` via the corresponding setter (UI → settings, `RegisterValueChangedCallback` path). `Slider.value` setter raises `ChangeEvent` only when `panel != null`, so verifying this path requires a UIDocument-attached panel and is not feasible in EditMode unit tests — covered by manual QA (ADR-0016 §10).

## 関連

- 確定 ADR: [ADR-0016 §4 R3 利用範囲 / §11 PR 分割計画 M5-PR6](../../../adr/0016-m5-bootstrap-presentation.md)
- 関連 ADR: [ADR-0006 §4 Pure C# 哲学](../../../adr/0006-m1-detail-application-interfaces.md)、[ADR-0012 §8](../../../adr/0012-m4-scriptableobject-and-persistence.md)(`IUserSettings` の M4-PR6 確立)
- 実装: `Assets/_Project/Scripts/Presentation/Games/DrowZzz/UserSettingsBinder.cs`
- 利用元: `Assets/_Project/Scripts/Presentation/Games/DrowZzz/DrowZzzGameView.cs`(`[Inject] Construct` + `Start` で生成)
- テスト: `Assets/_Project/Scripts/Tests/Presentation.Tests/Games/DrowZzz/UserSettingsBinderTests.cs`
- UI: `Assets/_Project/UI/Games/DrowZzz/DrowZzzGame.uxml`(`settings-section`:`bgm-slider` / `se-slider` / `language-dropdown`)
- シナリオ: `user-settings-binder.feature`(同ディレクトリ)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| PRES-022 | (テスト免除: Ubiquitous) | `IDisposable` 実装はコンパイル時保証 |
| PRES-023 | Given_bgmSliderNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-024 | Given_seSliderNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-025 | Given_languageDropdownNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-026 | Given_userSettingsNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-027 | Given_ctor_When_Construct_Then_LanguageDropdownChoicesAreSupportedCodes | Normal |
| PRES-028 | Given_bound_When_SetBgmVolume_Then_BgmSliderValueUpdated | Normal(BGM を代表ケースとして検証) |
| PRES-029 | Given_disposed_When_DisposeAgain_Then_NoException | Normal(冪等性) |
| PRES-030 | (テスト免除: Optional、手動 QA) | UI → settings は UIDocument パネルアタッチが必要で EditMode 単体テスト不可 |
