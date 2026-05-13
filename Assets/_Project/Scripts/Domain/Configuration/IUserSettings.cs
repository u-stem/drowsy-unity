using R3;

namespace Drowsy.Domain.Configuration
{
    /// <summary>
    /// L4 階層(CLAUDE.md §9 定数管理方針)のユーザー設定。BGM 音量 / SE 音量 /
    /// 言語コードの 3 項目を getter + setter + R3 <see cref="Observable{T}"/> +
    /// <see cref="Save"/> の対称 API で提供する。
    /// </summary>
    /// <remarks>
    /// ADR-0012 §8 + M4-PR6 着手時の JIT 確定(2026-05-13)に基づく interface。
    /// 実装側は Setter 呼び出し時に対応 <see cref="Observable{T}"/> を発火させ、
    /// <see cref="Save"/> で永続化先(PlayerPrefs 等)に書き出す責務を持つ。
    /// R3 を Domain.asmdef に含める判断は M4-PR6 着手時の JIT 確定で
    /// 「Unity デファクト = Drowsy.Domain.asmdef に R3 を入れる」を採用したことに基づく
    /// (CLAUDE.md §4 が R3 を VContainer / UniTask と並ぶコア採用 lib と確定済、
    /// CLAUDE.md §5「Domain は純粋 C# (`noEngineReferences: true`)」は
    /// 「UnityEngine 非依存」を主旨とした規約で R3 自体は Pure C# library のため整合)。
    /// </remarks>
    public interface IUserSettings
    {
        /// <summary>BGM 音量(0.0〜1.0、default <see cref="UserSettingsDefaults.BgmVolume"/>)。</summary>
        float BgmVolume { get; }

        /// <summary>SE 音量(0.0〜1.0、default <see cref="UserSettingsDefaults.SeVolume"/>)。</summary>
        float SeVolume { get; }

        /// <summary>
        /// 言語コード(<see cref="LanguageCodes.Ja"/> / <see cref="LanguageCodes.En"/>、
        /// default <see cref="UserSettingsDefaults.Language"/>)。
        /// </summary>
        string Language { get; }

        /// <summary>
        /// BGM 音量変更の通知。Subscribe 時に現在値を即発火し(BehaviorSubject 相当の
        /// R3 <see cref="ReactiveProperty{T}"/> 挙動)、以降は <see cref="SetBgmVolume"/>
        /// 呼び出し時に clamp 済み値を発火する。M5 Presentation の UI バインディングで
        /// 初期値を別経路で取得せず Subscribe 1 経路で済ませる前倒し設計。
        /// </summary>
        Observable<float> BgmVolumeChanged { get; }

        /// <summary>
        /// SE 音量変更の通知(<see cref="BgmVolumeChanged"/> 同様、Subscribe 時即発火)。
        /// </summary>
        Observable<float> SeVolumeChanged { get; }

        /// <summary>
        /// 言語コード変更の通知(<see cref="BgmVolumeChanged"/> 同様、Subscribe 時即発火)。
        /// </summary>
        Observable<string> LanguageChanged { get; }

        /// <summary>
        /// BGM 音量を設定する。範囲外は
        /// <see cref="UserSettingsDefaults.MinVolume"/> 〜 <see cref="UserSettingsDefaults.MaxVolume"/>
        /// に clamp される(UI スライダーの連続値を許容する設計、throw しない)。
        /// </summary>
        void SetBgmVolume(float value);

        /// <summary>SE 音量を設定する。範囲外は clamp される(<see cref="SetBgmVolume"/> 同様)。</summary>
        void SetSeVolume(float value);

        /// <summary>
        /// 言語コードを設定する。<see cref="LanguageCodes.IsSupported"/> = false の
        /// コードは <see cref="System.ArgumentException"/> を投げる
        /// (Volume と異なり離散値のため clamp ではなく throw する設計)。
        /// </summary>
        /// <exception cref="System.ArgumentNullException"><paramref name="code"/> が null。</exception>
        /// <exception cref="System.ArgumentException"><paramref name="code"/> が未対応。</exception>
        void SetLanguage(string code);

        /// <summary>
        /// 現在の設定を永続化する(PlayerPrefs.Save() 等の明示 flush)。
        /// Setter 内部での自動 Save は disk I/O 頻発防止のため行わず、本メソッド呼び出しで集約する。
        /// </summary>
        void Save();
    }
}
