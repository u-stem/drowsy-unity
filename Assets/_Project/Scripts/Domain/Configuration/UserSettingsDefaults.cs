namespace Drowsy.Domain.Configuration
{
    /// <summary>
    /// <see cref="IUserSettings"/> の default 値と音量 clamp range を保持する定数。
    /// </summary>
    /// <remarks>
    /// CLAUDE.md §9「定数管理方針」**L4(ユーザー設定の default 値)** に該当。
    /// L1(数学的・物理的不変量)/ L2(ドメイン上の真の不変量)とは異なり、ユーザー
    /// 操作で変更される L4 階層の「初回起動時 default」と「許容 range」を集約する。
    /// 個人開発 / 日本拠点(CLAUDE.md §3)を前提に音量中間値 + 日本語 default を採用。
    /// </remarks>
    public static class UserSettingsDefaults
    {
        /// <summary>BGM 音量 default(0.5、中間値で初回起動の聴感を中立化)。</summary>
        public const float BgmVolume = 0.5f;

        /// <summary>SE 音量 default(0.5、BGM と同等の中間値)。</summary>
        public const float SeVolume = 0.5f;

        /// <summary>言語コード default(<see cref="LanguageCodes.Ja"/>、日本拠点既定)。</summary>
        public const string Language = LanguageCodes.Ja;

        /// <summary>音量の最小値(完全消音)。</summary>
        public const float MinVolume = 0.0f;

        /// <summary>音量の最大値(最大音量)。</summary>
        public const float MaxVolume = 1.0f;
    }
}
