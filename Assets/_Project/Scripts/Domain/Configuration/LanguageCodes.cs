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
        /// <remarks>
        /// 式本体プロパティとして呼び出しごとに新しい配列を返す(`static readonly` フィールドにしない)。
        /// `static readonly` フィールドの初期化は暗黙の static ctor(`.cctor`)になり、AppDomain 内で 1 回のみ
        /// 実行されるため、カバレッジ計測区間の外(Unity Editor のアセンブリロード / ドメインリロード時など)で
        /// 先に走ると永久に未カバー扱いになる(M5-PR6 で発覚、`.cctor` 計測の構造的限界)。式本体プロパティなら
        /// getter が呼ばれるたびに計測区間内で実行される。要素数 2 の小さな配列のため毎回生成のコストは無視できる。
        /// </remarks>
        public static IReadOnlyList<string> Supported => new[] { Ja, En };

        /// <summary>引数の言語コードがサポート対象か判定する。</summary>
        /// <param name="code">判定対象の言語コード。</param>
        /// <returns>サポート対象なら true、null / 未対応コードなら false。</returns>
        /// <remarks>
        /// <see cref="Supported"/> を参照せず <see cref="Ja"/> / <see cref="En"/> の const 比較で判定する
        /// (M5-PR6 で <see cref="Supported"/> 依存を除去、static ctor 廃止に伴う簡素化)。<paramref name="code"/>
        /// が null でも C# の <c>string ==</c> は null セーフのため例外を投げず false を返す。
        /// </remarks>
        public static bool IsSupported(string code) => code == Ja || code == En;
    }
}
