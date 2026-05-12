# IGameRule

ゲームの状態遷移ルールを表すジェネリック interface(純関数)。

## 概要

`IGameRule<TAction, TSession>` は `Drowsy.Application` namespace 直下に置く汎用 interface で、`TAction : IGameAction` 制約を持つ。`IsLegalMove(session, action)` で合法性を判定し、`Apply(session, action)` で次セッションを返す純関数 2 メソッドからなる。各ゲームは具象型(例: `DrowZzzRule : IGameRule<DrowZzzAction, DrowZzzGameSession>`)で実装する。
ADR-0006 §1.2 の決定に基づく。

## 普遍要件 (Ubiquitous)

- [APP-003] [Ubiquitous] The `IGameRule<TAction, TSession>` shall constrain `TAction` to `IGameAction`.
- [APP-004] [Ubiquitous] The `IGameRule<TAction, TSession>` shall expose `IsLegalMove(TSession, TAction) : bool` and `Apply(TSession, TAction) : TSession`.
- [APP-043] [Ubiquitous] M3-PR1 で API を拡張(ADR-0010 §1):`IsTerminated(TSession) : bool` / `GetWinner(TSession) : PlayerId` を追加し、計 4 メソッドの API シグネチャを持つ。

## 事象駆動要件 (Event-driven)

- [APP-005] When `IsLegalMove(session, action)` returns true and `Apply(session, action)` is invoked with the same arguments, the rule shall return a `TSession` representing the post-state.
- [APP-041] When `IsTerminated(session)` is invoked on a session, the rule shall return `true` if and only if the game has ended (semantics determined by the concrete implementation, e.g., `DrowZzzRule` returns `session.IsTerminated`).
- [APP-042] When `GetWinner(session)` is invoked, the rule shall behave as follows:
  - If `IsTerminated(session) == false`: throw `InvalidOperationException`.
  - If the session is terminated with a winner: return that `PlayerId`.
  - If the session is terminated with a draw: return `null`.

## 関連

- 実装: `Assets/_Project/Scripts/Application/IGameRule.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/IGameRuleTests.cs`
- シナリオ: `game-rule.feature`
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §1.2(M1 起点)
- ADR: [`docs/adr/0010-m3-game-termination-and-victory-determination.md`](../../adr/0010-m3-game-termination-and-victory-determination.md) §1(M3 拡張)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-003 | (テスト免除: Ubiquitous) | `where TAction : IGameAction` で構造的に保証 |
| APP-004 | (テスト免除: Ubiquitous) | interface シグネチャで構造的に保証 |
| APP-005 | `Given_合法判定がtrueなRuleとAction_When_Applyを呼ぶ_Then_新しいSessionが返る` | ダミー Rule で contract を検証 |
| APP-041 | `Given_未終了Session_When_IsTerminated_Then_false` / `..._終了済Session_..._Then_true` | ダミー Rule で IsTerminated 契約を検証 |
| APP-042 | `Given_終了済Session勝者あり_When_GetWinner_Then_勝者PlayerIdを返す` / `..._引き分け_..._Then_nullを返す` / `..._未終了Session_..._Then_InvalidOperationExceptionを投げる` | ダミー Rule で GetWinner 契約を検証(3 ケース) |
