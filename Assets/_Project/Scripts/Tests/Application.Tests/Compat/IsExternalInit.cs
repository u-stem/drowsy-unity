// Polyfill for C# 9 init-only setter on Unity 6 / Mono. (Drowsy.Application.Tests assembly 用コピー)
//
// 本ファイルは Domain assembly の Compat/IsExternalInit.cs と同内容のコピーである。
// `internal` polyfill は assembly 境界を越えないため、record + init を使う各 assembly で
// 個別に shim を保持する必要がある。
//
// Drowsy.Application.Tests は IGameActionTests / IGameRuleTests のダミー record
// (`sealed record DummyAction` / `sealed record DummySession(int Counter)` 等) で
// init setter を暗黙的に使用するため、本 polyfill が必須。
//
// 採用根拠: docs/adr/0004-init-setter-polyfill.md
// 撤去条件: 同 ADR §「撤去条件」を参照。

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// C# 9 init-only setter が要求する marker 型の Unity / Mono 向け polyfill。
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
