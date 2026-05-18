using System;
using Drowsy.Domain.Configuration;
using R3;
using UnityEngine;

// InternalsVisibleTo("Drowsy.Infrastructure.Tests") は Infrastructure/AssemblyInfo.cs に
// 集約済(M4-PR4 確立)、本ファイルでの重複定義は不要。

namespace Drowsy.Infrastructure.Settings
{
    /// <summary>
    /// <see cref="IUserSettings"/> の PlayerPrefs 実装(ADR-0012 §8 + M4-PR6
    /// 着手時の JIT 確定 2026-05-13 に基づく)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 内部状態として R3 <see cref="ReactiveProperty{T}"/> を保持し、
    /// <see cref="Observable{T}"/> として interface 公開する。
    /// Setter は値変更時に <see cref="ReactiveProperty{T}"/> を更新
    /// + PlayerPrefs に書き込み、<see cref="Save"/> で
    /// <see cref="PlayerPrefs.Save"/> を呼ぶ(明示 flush、disk I/O 頻発防止)。
    /// </para>
    /// <para>
    /// ctor で PlayerPrefs から現在値を読み込み、範囲外 / 未対応値が入っていた場合は
    /// default 値に復帰する(過去ファイル / 手書き編集に対するデータ衛生)。
    /// Setter での音量範囲外は clamp、言語の未対応コードは throw する非対称設計は
    /// IUserSettings xmldoc 通り(volume = 連続値 / language = 離散値)。
    /// </para>
    /// </remarks>
    public sealed class PlayerPrefsUserSettings : IUserSettings, IDisposable
    {
        private readonly ReactiveProperty<float> _bgmVolume;
        private readonly ReactiveProperty<float> _seVolume;
        private readonly ReactiveProperty<string> _language;
        private bool _disposed;

        /// <summary>
        /// PlayerPrefs から現在の設定を読み込んで初期化する。
        /// キー未存在 / 範囲外 / 未対応値は default 値に復帰させる。
        /// </summary>
        public PlayerPrefsUserSettings()
        {
            var bgm = PlayerPrefs.GetFloat(PlayerPrefsKeys.BgmVolume, UserSettingsDefaults.BgmVolume);
            var se = PlayerPrefs.GetFloat(PlayerPrefsKeys.SeVolume, UserSettingsDefaults.SeVolume);
            var lang = PlayerPrefs.GetString(PlayerPrefsKeys.Language, UserSettingsDefaults.Language);

            // 過去ファイル / 手書き編集で範囲外が混入した場合は default に復帰
            // (Setter 経由では clamp / throw で防御するが ctor では既存値を「初期化時のデータ衛生」として default fallback)
            if (bgm < UserSettingsDefaults.MinVolume || bgm > UserSettingsDefaults.MaxVolume)
            {
                bgm = UserSettingsDefaults.BgmVolume;
            }
            if (se < UserSettingsDefaults.MinVolume || se > UserSettingsDefaults.MaxVolume)
            {
                se = UserSettingsDefaults.SeVolume;
            }
            if (!LanguageCodes.IsSupported(lang))
            {
                lang = UserSettingsDefaults.Language;
            }

            _bgmVolume = new ReactiveProperty<float>(bgm);
            _seVolume = new ReactiveProperty<float>(se);
            _language = new ReactiveProperty<string>(lang);
        }

        /// <inheritdoc />
        public float BgmVolume => _bgmVolume.Value;

        /// <inheritdoc />
        public float SeVolume => _seVolume.Value;

        /// <inheritdoc />
        public string Language => _language.Value;

        /// <inheritdoc />
        public Observable<float> BgmVolumeChanged => _bgmVolume;

        /// <inheritdoc />
        public Observable<float> SeVolumeChanged => _seVolume;

        /// <inheritdoc />
        public Observable<string> LanguageChanged => _language;

        /// <inheritdoc />
        public void SetBgmVolume(float value)
        {
            ThrowIfDisposed();
            var clamped = Mathf.Clamp(value, UserSettingsDefaults.MinVolume, UserSettingsDefaults.MaxVolume);
            _bgmVolume.Value = clamped;
            PlayerPrefs.SetFloat(PlayerPrefsKeys.BgmVolume, clamped);
        }

        /// <inheritdoc />
        public void SetSeVolume(float value)
        {
            ThrowIfDisposed();
            var clamped = Mathf.Clamp(value, UserSettingsDefaults.MinVolume, UserSettingsDefaults.MaxVolume);
            _seVolume.Value = clamped;
            PlayerPrefs.SetFloat(PlayerPrefsKeys.SeVolume, clamped);
        }

        /// <inheritdoc />
        public void SetLanguage(string code)
        {
            ThrowIfDisposed();
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }
            if (!LanguageCodes.IsSupported(code))
            {
                throw new ArgumentException(
                    $"Language code '{code}' is not supported. Supported codes: {string.Join(", ", LanguageCodes.Supported)}",
                    nameof(code));
            }
            _language.Value = code;
            PlayerPrefs.SetString(PlayerPrefsKeys.Language, code);
        }

        /// <inheritdoc />
        /// <remarks>
        /// USR-025: Dispose 後の <see cref="Save"/> は silent no-op(USR-022 / 023 / 024 の Setter 群が
        /// <see cref="ObjectDisposedException"/> を投げる非対称設計)。本 wrapper を Dispose した時点で
        /// 内部 <see cref="ReactiveProperty{T}"/> は解放されるが、Setter が呼ばれる度に
        /// <see cref="PlayerPrefs.SetFloat(string,float)"/> / <see cref="PlayerPrefs.SetString(string,string)"/> で
        /// 静的 PlayerPrefs バッファには値が既に書かれているため、flush(<see cref="PlayerPrefs.Save"/>)は
        /// wrapper 状態に依存しない無害な操作として扱う。
        /// <para>
        /// この非対称により、Unity の GameObject 破棄順序が非決定的でも
        /// <c>DrowZzzGameView.OnDestroy</c> → <c>UserSettingsBinder.Dispose</c> → <c>IUserSettings.Save</c>
        /// の呼び出しが <c>ProjectLifetimeScope</c> の本 wrapper 破棄より後に走っても安全に flush を試みられる
        /// (VContainer LifetimeScope と MonoBehaviour ライフサイクルが別管理である M5 構成に必要な吸収層)。
        /// </para>
        /// <para>
        /// 注意:Dispose 後の <see cref="Save"/> は <see cref="PlayerPrefs.Save"/> を呼ばずに silent return
        /// するため、最後の Set 以降に明示 flush されていない値はディスク永続化が遅延する可能性がある。
        /// 通常は (1) Dispose 前に <c>UserSettingsBinder</c> が Dispose されて Save が走る、(2) Unity が
        /// <see cref="MonoBehaviour"/>.OnApplicationQuit で <see cref="PlayerPrefs.Save"/> を自動実行する、の
        /// いずれかで flush 漏れが発生しない設計(Dispose 後の Save は破棄順序非決定性吸収のための安全網)。
        /// </para>
        /// </remarks>
        public void Save()
        {
            if (_disposed)
            {
                return;
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 内部 <see cref="ReactiveProperty{T}"/> 3 件を Dispose する。
        /// 以降の Setter 呼び出し(<see cref="SetBgmVolume"/> / <see cref="SetSeVolume"/> /
        /// <see cref="SetLanguage"/>)は <see cref="ObjectDisposedException"/> を投げる。
        /// <see cref="Save"/> のみ silent no-op(USR-025、ライフサイクル非対称性吸収)。
        /// </summary>
        /// <remarks>
        /// Getter(<see cref="BgmVolume"/> / <see cref="SeVolume"/> / <see cref="Language"/>)は
        /// Dispose 後も最後の値を返す(R3 <see cref="ReactiveProperty{T}.Value"/> は Dispose
        /// 後も保持値を返す挙動)。Observable プロパティの Subscribe は Dispose 後は
        /// `OnCompleted` で即終了するため、購読者側は subscription を解放可能。
        /// 二重 Dispose は冪等(無音で抜ける)。
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _bgmVolume.Dispose();
            _seVolume.Dispose();
            _language.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PlayerPrefsUserSettings));
            }
        }
    }
}
