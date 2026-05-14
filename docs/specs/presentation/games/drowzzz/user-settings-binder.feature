# language: ja
機能: UserSettingsBinder (Presentation 層 設定 UI 双方向バインダ) (M5-PR6)

  @PRES-023
  シナリオ: ctor で bgmSlider = null は ArgumentNullException (異常系・Small)
    前提 seSlider / languageDropdown / userSettings は有効
    もし new UserSettingsBinder(null, seSlider, languageDropdown, userSettings) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-024
  シナリオ: ctor で seSlider = null は ArgumentNullException (異常系・Small)
    前提 bgmSlider / languageDropdown / userSettings は有効
    もし new UserSettingsBinder(bgmSlider, null, languageDropdown, userSettings) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-025
  シナリオ: ctor で languageDropdown = null は ArgumentNullException (異常系・Small)
    前提 bgmSlider / seSlider / userSettings は有効
    もし new UserSettingsBinder(bgmSlider, seSlider, null, userSettings) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-026
  シナリオ: ctor で userSettings = null は ArgumentNullException (異常系・Small)
    前提 bgmSlider / seSlider / languageDropdown は有効
    もし new UserSettingsBinder(bgmSlider, seSlider, languageDropdown, null) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-027
  シナリオ: ctor で languageDropdown.choices が LanguageCodes.Supported に設定される (正常系・Small)
    前提 有効な Slider × 2 + DropdownField + MockUserSettings
    もし new UserSettingsBinder(...) を呼ぶ
    ならば languageDropdown.choices は LanguageCodes.Supported(["ja", "en"])と等価

  @PRES-028
  シナリオ: settings の音量変更で対応 Slider の value が更新される (正常系・Small)
    前提 UserSettingsBinder で bgmSlider と MockUserSettings をバインド済
    もし MockUserSettings.SetBgmVolume(0.7) を呼ぶ
    ならば BgmVolumeChanged の Subscribe 経由で bgmSlider.value が 0.7 に更新される(SetValueWithoutNotify)

  @PRES-029
  シナリオ: Dispose() の二重呼び出しは silent no-op (正常系・Small)
    前提 UserSettingsBinder 構築済 + Dispose() 1 回呼び済
    もし 2 回目の Dispose() を呼ぶ
    ならば 例外を投げず、副作用なし(冪等性)

  # @PRES-030(UI → settings)は UIDocument パネルアタッチが必要で EditMode 単体テスト不可、手動 QA
  # (Slider.value setter は panel != null のときのみ ChangeEvent を発火する UI Toolkit 仕様、ADR-0016 §10)
