using System.Collections.Generic;

namespace Drowsy.Domain.Configuration
{
    /// <summary>
    /// サポートする言語コード定数。本格 i18n は Phase 3 で別途検討する
    /// (ADR-0012 §8「最小項目(JIT 確認待ち、初期推奨)」)。
    /// </summary>
    /// <remarks>
    /// 値は ISO 639-1 形式 2 文字 lowercase に揃える。M4-PR6 では `ja` / `en` のみ。
    /// </remarks>
    public static class LanguageCodes
    {
        /// <summary>日本語(default、CLAUDE.md §3「日本拠点」既定)。</summary>
        public const string Ja = "ja";

        /// <summary>英語。</summary>
        public const string En = "en";

        /// <summary>サポート対象の言語コード一覧。</summary>
        public static readonly IReadOnlyList<string> Supported = new[] { Ja, En };

        /// <summary>引数の言語コードがサポート対象か判定する。</summary>
        /// <param name="code">判定対象の言語コード。</param>
        /// <returns>サポート対象なら true、null / 未対応コードなら false。</returns>
        public static bool IsSupported(string code)
        {
            if (code is null)
            {
                return false;
            }
            for (var i = 0; i < Supported.Count; i++)
            {
                if (Supported[i] == code)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
