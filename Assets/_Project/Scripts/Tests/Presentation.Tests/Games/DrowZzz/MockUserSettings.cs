using System;
using R3;
using Drowsy.Domain.Configuration;

namespace Drowsy.Presentation.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="IUserSettings"/> の最小モック実装(M5-PR2 Presenter ctor 注入用)。
    /// </summary>
    /// <remarks>
    /// M5-PR2 の Presenter は ctor で <see cref="IUserSettings"/> を受けるが、本 PR の Presenter 実装は
    /// UserSettings の値を読まないため、初期値固定の Mock で十分(M5-PR6 で View バインディングが入る際に
    /// テスト粒度を上げる)。<see cref="ReactiveProperty{T}"/> を内部で持ち、 <see cref="Dispose"/> 時に解放する。
    /// </remarks>
    public sealed class MockUserSettings : IUserSettings, IDisposable
    {
        private readonly ReactiveProperty<float> _bgmVolume = new(0.5f);
        private readonly ReactiveProperty<float> _seVolume = new(0.5f);
        private readonly ReactiveProperty<string> _language = new("ja");
        private bool _disposed;

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
        public void SetBgmVolume(float value) => _bgmVolume.Value = value;

        /// <inheritdoc />
        public void SetSeVolume(float value) => _seVolume.Value = value;

        /// <inheritdoc />
        public void SetLanguage(string code) => _language.Value = code;

        /// <summary>テストで <see cref="Save"/> 呼出回数を検証するためのカウンタ(Pres W-1 post-Phase2 レビュー反映)。</summary>
        public int SaveCallCount { get; private set; }

        /// <inheritdoc />
        public void Save()
        {
            // テスト用 Mock では永続化なし(no-op)+ 呼出回数のみ記録。
            SaveCallCount++;
        }

        /// <summary>内部 ReactiveProperty を解放する。二重 Dispose は silent no-op(M4-PR6 PlayerPrefsUserSettings 同様)。</summary>
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
    }
}
