# IGameRule

ゲームの状態遷移ルールを表すジェネリック interface(純関数)。

## 概要

`IGameRule<TAction, TSession>` は `Drowsy.Application` namespace 直下に置く汎用 interface で、`TAction : IGameAction` 制約を持つ。`IsLegalMove(session, action)` で合法性を判定し、`Apply(session, action)` で次セッションを返す純関数 2 メソッドからなる。各ゲームは具象型(例: `DrowZzzRule : IGameRule<DrowZzzAction, DrowZzzGameSession>`)で実装する。
ADR-0006 §1.2 の決定に基づく。

## 普遍要件 (Ubiquitous)

- [APP-003] [Ubiquitous] The `IGameRule<TAction, TSession>` shall constrain `TAction` to `IGameAction`.
- [APP-004] [Ubiquitous] The `IGameRule<TAction, TSession>` shall expose `IsLegalMove(TSession, TAction) : bool` and `Apply(TSession, TAction) : TSession`.

## 事象駆動要件 (Event-driven)

- [APP-005] When `IsLegalMove(session, action)` returns true and `Apply(session, action)` is invoked with the same arguments, the rule shall return a `TSession` representing the post-state.

## 関連

- 実装: `Assets/_Project/Scripts/Application/IGameRule.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/IGameRuleTests.cs`
- シナリオ: `game-rule.feature`
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §1.2

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-003 | (テスト免除: Ubiquitous) | `where TAction : IGameAction` で構造的に保証 |
| APP-004 | (テスト免除: Ubiquitous) | interface シグネチャで構造的に保証 |
| APP-005 | `Given_合法判定がtrueなRuleとAction_When_Applyを呼ぶ_Then_新しいSessionが返る` | ダミー Rule で contract を検証 |
