using System;
using R3;
using UnityEngine;
using Drowsy.Domain.Configuration;

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
        public void Save()
        {
            ThrowIfDisposed();
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 内部 <see cref="ReactiveProperty{T}"/> 3 件を Dispose する。
        /// 以降の Setter / Save 呼び出しは <see cref="ObjectDisposedException"/> を投げる。
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
