# language: ja
@USR
機能: IUserSettings + PlayerPrefsUserSettings
  L4 ユーザー設定(BGM / SE / 言語)を PlayerPrefs に永続化し、
  R3 Observable で変更通知する。

  @USR-007
  シナリオ: PlayerPrefs 空状態で 3 項目 default 復元 (正常系・Small)
    前提 PlayerPrefs に drowsy.* キーが存在しない
    もし PlayerPrefsUserSettings をインスタンス化する
    ならば BgmVolume は 0.5 / SeVolume は 0.5 / Language は ja を返す

  @USR-008
  シナリオ: PlayerPrefs に 0.3 が入っている時の BGM 復元 (正常系・Small)
    前提 PlayerPrefs.SetFloat("drowsy.bgm", 0.3) を呼ぶ
    もし PlayerPrefsUserSettings をインスタンス化する
    ならば BgmVolume は 0.3 を返す

  @USR-009
  シナリオ: PlayerPrefs に en が入っている時の言語復元 (正常系・Small)
    前提 PlayerPrefs.SetString("drowsy.lang", "en") を呼ぶ
    もし PlayerPrefsUserSettings をインスタンス化する
    ならば Language は en を返す

  @USR-006
  シナリオ: SetBgmVolume の Getter / PlayerPrefs 両方反映 (正常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings
    もし SetBgmVolume(0.42) を呼ぶ
    ならば BgmVolume は 0.42 を返し PlayerPrefs.GetFloat("drowsy.bgm") も 0.42 を返す

  @USR-014
  シナリオ: SetSeVolume の Getter / PlayerPrefs 両方反映 (正常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings
    もし SetSeVolume(0.42) を呼ぶ
    ならば SeVolume は 0.42 を返し PlayerPrefs.GetFloat("drowsy.se") も 0.42 を返す

  @USR-017
  シナリオ: SetLanguage の Getter / PlayerPrefs 両方反映 (正常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings
    もし SetLanguage("en") を呼ぶ
    ならば Language は en を返し PlayerPrefs.GetString("drowsy.lang") も en を返す

  @USR-010
  シナリオ: SetBgmVolume で BgmVolumeChanged が発火 (正常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings と BgmVolumeChanged.Subscribe
    もし SetBgmVolume(0.42) を呼ぶ
    ならば Subscribe 側に 0.42 が届く(初期値 0.5 + 0.42 の 2 件目)

  @USR-011
  シナリオ: SetSeVolume で SeVolumeChanged が発火 (正常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings と SeVolumeChanged.Subscribe
    もし SetSeVolume(0.42) を呼ぶ
    ならば Subscribe 側に 0.42 が届く

  @USR-012
  シナリオ: SetLanguage で LanguageChanged が発火 (正常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings と LanguageChanged.Subscribe
    もし SetLanguage("en") を呼ぶ
    ならば Subscribe 側に "en" が届く

  @USR-013
  シナリオ: Save で永続化 → 再インスタンス化で値復元 (正常系・Small)
    前提 PlayerPrefs 空状態
    もし SetBgmVolume(0.7) + Save() を呼んでから新しい PlayerPrefsUserSettings をインスタンス化
    ならば 新インスタンスの BgmVolume は 0.7 を返す

  @USR-015
  シナリオ: BGM の上限 clamp (正常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings
    もし SetBgmVolume(1.5) を呼ぶ
    ならば BgmVolume は 1.0(上限 clamp)を返す

  @USR-016
  シナリオ: BGM の下限 clamp (正常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings
    もし SetBgmVolume(-0.5) を呼ぶ
    ならば BgmVolume は 0.0(下限 clamp)を返す

  @USR-018
  シナリオ: ctor で PlayerPrefs の範囲外 BGM を default 復帰 (異常系・Small)
    前提 PlayerPrefs.SetFloat("drowsy.bgm", 2.0)
    もし PlayerPrefsUserSettings をインスタンス化する
    ならば BgmVolume は 0.5(default)を返す

  @USR-026
  シナリオ: ctor で PlayerPrefs の範囲外 SE を default 復帰 (異常系・Small)
    前提 PlayerPrefs.SetFloat("drowsy.se", -1.0)
    もし PlayerPrefsUserSettings をインスタンス化する
    ならば SeVolume は 0.5(default)を返す

  @USR-019
  シナリオ: ctor で PlayerPrefs の未対応 lang を default 復帰 (異常系・Small)
    前提 PlayerPrefs.SetString("drowsy.lang", "zh")
    もし PlayerPrefsUserSettings をインスタンス化する
    ならば Language は ja(default)を返す

  @USR-020
  シナリオ: SetLanguage(null) は ArgumentNullException (異常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings
    もし SetLanguage(null) を呼ぶ
    ならば ArgumentNullException が投げられる

  @USR-021
  シナリオアウトライン: SetLanguage 未対応コードは ArgumentException (異常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings
    もし SetLanguage(<code>) を呼ぶ
    ならば ArgumentException が投げられる

    例:
      | code |
      | ""   |
      | "zh" |
      | "JA" |
      | "ja-JP" |

  @USR-022 @USR-023 @USR-024 @USR-025
  シナリオアウトライン: Dispose 後の操作は ObjectDisposedException (異常系・Small)
    前提 PlayerPrefs 空状態の PlayerPrefsUserSettings を Dispose 済
    もし <operation> を呼ぶ
    ならば ObjectDisposedException が投げられる

    例:
      | operation             |
      | SetBgmVolume(0.5)     |
      | SetSeVolume(0.5)      |
      | SetLanguage("ja")     |
      | Save()                |
