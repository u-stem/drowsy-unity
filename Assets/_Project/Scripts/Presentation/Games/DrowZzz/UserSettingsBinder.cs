using System;
using System.Collections.Generic;
using Drowsy.Domain.Configuration;
using R3;
using UnityEngine.UIElements;

namespace Drowsy.Presentation.Games.DrowZzz
{
    /// <summary>
    /// UI Toolkit の設定 UI 要素(BGM / SE <see cref="Slider"/> + Language <see cref="DropdownField"/>)と
    /// <see cref="IUserSettings"/> を双方向バインドする Pure C# クラス(M5-PR6)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §4「R3 1.3.0 の利用範囲」+ §11 M5-PR6 で確定。<see cref="DrowZzzGameView"/>(MonoBehaviour)から
    /// UIDocument 依存を切り離し、<see cref="VisualElement"/>(<see cref="Slider"/> / <see cref="DropdownField"/>)と
    /// <see cref="IUserSettings"/> のみを受け取る Pure C# 設計とすることで EditMode 単体テスト可能にする
    /// (M5-PR6 着手時 JIT 確定 2026-05-14、ADR-0006 §4 Pure C# 哲学と整合)。
    /// <para>
    /// <b>双方向バインドのループ防止</b>:settings → UI 反映は <c>SetValueWithoutNotify</c> を使い
    /// <c>RegisterValueChangedCallback</c> を発火させない。UI → settings は <c>RegisterValueChangedCallback</c>
    /// (ユーザー操作のみ)。これにより「settings 更新 → UI 更新 → callback → settings 更新 → …」の無限ループを
    /// 構造的に防ぐ。
    /// </para>
    /// <para>
    /// <b>Language Dropdown の選択肢</b>:<see cref="LanguageCodes.Supported"/>(<c>"ja"</c> / <c>"en"</c>)を
    /// そのまま <see cref="DropdownField.choices"/> に設定する(コード直接表示、表示名マッピングは Phase 3、
    /// M5-PR6 着手時 JIT 確定 2026-05-14)。選択肢が Supported のみのため
    /// <see cref="IUserSettings.SetLanguage"/> が未対応コードで <see cref="ArgumentException"/> を投げる経路は発生しない。
    /// </para>
    /// <para>
    /// <b>Subscribe ライフサイクル</b>:R3 <c>Observable.Subscribe</c> は <see cref="CompositeDisposable"/> で
    /// 管理し <see cref="Dispose"/> で解放する(ADR-0016 §4、M4-PR6 の <c>PlayerPrefsUserSettings.Dispose</c> 同様の
    /// パターン)。<c>RegisterValueChangedCallback</c> も <c>UnregisterValueChangedCallback</c> で対称解除する。
    /// </para>
    /// </remarks>
    public sealed class UserSettingsBinder : IDisposable
    {
        private readonly Slider _bgmSlider;
        private readonly Slider _seSlider;
        private readonly DropdownField _languageDropdown;
        private readonly IUserSettings _userSettings;
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed;

        /// <summary>
        /// 設定 UI 要素と <see cref="IUserSettings"/> を双方向バインドする。
        /// </summary>
        /// <param name="bgmSlider">BGM 音量 Slider(0.0〜1.0)</param>
        /// <param name="seSlider">SE 音量 Slider(0.0〜1.0)</param>
        /// <param name="languageDropdown">言語コード DropdownField</param>
        /// <param name="userSettings">バインド対象のユーザー設定</param>
        /// <exception cref="ArgumentNullException">いずれかの引数が null</exception>
        public UserSettingsBinder(
            Slider bgmSlider,
            Slider seSlider,
            DropdownField languageDropdown,
            IUserSettings userSettings)
        {
            _bgmSlider = bgmSlider ?? throw new ArgumentNullException(nameof(bgmSlider));
            _seSlider = seSlider ?? throw new ArgumentNullException(nameof(seSlider));
            _languageDropdown = languageDropdown ?? throw new ArgumentNullException(nameof(languageDropdown));
            _userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));

            // Language Dropdown の選択肢は LanguageCodes.Supported をそのまま使う(コード直接表示)
            _languageDropdown.choices = new List<string>(LanguageCodes.Supported);

            // settings → UI(SetValueWithoutNotify で UI → settings の callback 発火を抑止し無限ループを防ぐ)。
            // IUserSettings の各 Observable は Subscribe 時に現在値を即発火する(M4-PR6 BehaviorSubject 相当)ため、
            // この Subscribe だけで UI 初期値も settings の現在値に揃う。
            _userSettings.BgmVolumeChanged
                .Subscribe(v => _bgmSlider.SetValueWithoutNotify(v))
                .AddTo(_disposables);
            _userSettings.SeVolumeChanged
                .Subscribe(v => _seSlider.SetValueWithoutNotify(v))
                .AddTo(_disposables);
            _userSettings.LanguageChanged
                .Subscribe(c => _languageDropdown.SetValueWithoutNotify(c))
                .AddTo(_disposables);

            // UI → settings(ユーザー操作による値変更)
            _bgmSlider.RegisterValueChangedCallback(OnBgmSliderChanged);
            _seSlider.RegisterValueChangedCallback(OnSeSliderChanged);
            _languageDropdown.RegisterValueChangedCallback(OnLanguageDropdownChanged);
        }

        private void OnBgmSliderChanged(ChangeEvent<float> evt) => _userSettings.SetBgmVolume(evt.newValue);

        private void OnSeSliderChanged(ChangeEvent<float> evt) => _userSettings.SetSeVolume(evt.newValue);

        private void OnLanguageDropdownChanged(ChangeEvent<string> evt) => _userSettings.SetLanguage(evt.newValue);

        /// <inheritdoc />
        /// <remarks>
        /// 2 回目以降の <see cref="Dispose"/> は silent no-op(冪等)。Subscribe(<see cref="CompositeDisposable"/>)と
        /// <c>RegisterValueChangedCallback</c> の両方を対称的に解放する。
        /// <para>
        /// Pres W-1 post-Phase2 レビュー反映:<see cref="IUserSettings.SetBgmVolume"/> / <c>SetSeVolume</c> /
        /// <c>SetLanguage</c> は内部で <see cref="IUserSettings.Save"/> を呼ばない設計(disk I/O 頻発防止のため
        /// Save を集約する、<see cref="IUserSettings.Save"/> xmldoc 参照)。本 Binder は Dispose 時に
        /// 明示的に <see cref="IUserSettings.Save"/> を呼んで、Standalone / WebGL ビルドでも設定が
        /// 確実に永続化されることを保証する(Application.Quit や強制終了でも flush 漏れがない設計に揃える)。
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            // 先に UI → settings の callback を外し、次に settings → UI の Subscribe を解放する。
            // 順序はどちらでも機能的に安全(R3 CompositeDisposable.Dispose は Subscription を即時終了する)が、
            // 「ユーザー操作の入口を先に塞ぐ」意図でこの順とする(code-reviewer M5-PR6 T-5)。
            _bgmSlider.UnregisterValueChangedCallback(OnBgmSliderChanged);
            _seSlider.UnregisterValueChangedCallback(OnSeSliderChanged);
            _languageDropdown.UnregisterValueChangedCallback(OnLanguageDropdownChanged);
            _disposables.Dispose();
            // Pres W-1: 設定の永続化を Binder のライフサイクル終端に集約する。
            // 現行実装 PlayerPrefsUserSettings は Dispose 後の Save() を silent no-op にしたため
            // (USR-025、Setter / Save 非対称設計)、Unity の GameObject 破棄順序非決定性で
            // Settings → Binder の順で Dispose されても本 try は throw しない経路に揃った。
            // ただし IUserSettings は interface であり、将来 / テストスタブの別実装が
            // Save() で例外を投げる可能性は残るため try-catch を維持し、Dispose 処理の継続を保証する。
            try
            {
                _userSettings.Save();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    $"[UserSettingsBinder] Dispose 時の IUserSettings.Save に失敗: {ex}");
            }
        }
    }
}
