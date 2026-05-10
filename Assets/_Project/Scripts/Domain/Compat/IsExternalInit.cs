// Polyfill for C# 9 init-only setter on Unity 6 / Mono.
//
// `init` accessor and `record` の `with` 式は、コンパイラが
// `System.Runtime.CompilerServices.IsExternalInit` という marker 型の存在を要求する。
// .NET 5 以降の BCL には標準で含まれるが、Unity 6 が同梱する Mono / .NET Standard 2.1
// 相当の SDK には含まれないため、未定義のまま `init` を使うと CS0518
// (Predefined type 'IsExternalInit' is not defined or imported) でビルドが失敗する。
//
// このシム(stub)を Domain assembly に internal として置くことで、Domain 層内で
// `record + init` および `with` 式を利用可能にする。アクセシビリティを internal に
// 限定しているため、将来 Unity / Mono の BCL に IsExternalInit が組み込まれても
// 型衝突は発生しない(internal の重複は許容される)。
//
// 採用根拠: docs/adr/0004-init-setter-polyfill.md
//
// Application / Infrastructure / Presentation の各 assembly で `record + init` を使う場合は
// 同じ shim を各 assembly 内にコピーする必要がある(internal は assembly 境界を越えないため)。

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// C# 9 init-only setter が要求する marker 型の Unity / Mono 向け polyfill。
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
