namespace Drowsy.Infrastructure.Settings
{
    /// <summary>
    /// <see cref="PlayerPrefsUserSettings"/> が使用する PlayerPrefs key 定数。
    /// </summary>
    /// <remarks>
    /// ADR-0012 §8 + M4-PR6 着手時の JIT 確定(2026-05-13):key prefix は `drowsy.*`
    /// に統一する(他プロジェクトの PlayerPrefs と衝突回避、`.` 区切りで階層性表現)。
    /// </remarks>
    internal static class PlayerPrefsKeys
    {
        /// <summary>BGM 音量の PlayerPrefs key。</summary>
        internal const string BgmVolume = "drowsy.bgm";

        /// <summary>SE 音量の PlayerPrefs key。</summary>
        internal const string SeVolume = "drowsy.se";

        /// <summary>言語コードの PlayerPrefs key。</summary>
        internal const string Language = "drowsy.lang";
    }
}
