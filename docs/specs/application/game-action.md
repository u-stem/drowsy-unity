# IGameAction

ゲーム上のアクション(プレイヤーが実行できる操作)を表すマーカー interface。

## 概要

`IGameAction` は `Drowsy.Application` namespace 直下に置く汎用 interface で、メンバを持たないマーカー。各ゲームの具体アクション (`Drowsy.Application.Games.<Game>.<Game>Action`) は `record` でこの interface を実装し、`IGameRule<TAction, TSession>` の `TAction` 型パラメータに渡せる形にする。
ADR-0006 §1.1 の決定に基づく。

## 普遍要件 (Ubiquitous)

- [APP-001] [Ubiquitous] The `IGameAction` shall be a marker interface declared in the `Drowsy.Application` namespace with no required members.

## 事象駆動要件 (Event-driven)

- [APP-002] When a `record` declares `: IGameAction`, the type shall be assignable to a variable of type `IGameAction`.

## 関連

- 実装: `Assets/_Project/Scripts/Application/IGameAction.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/IGameActionTests.cs`
- シナリオ: `game-action.feature`
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §1.1

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-001 | (テスト免除: Ubiquitous) | `interface IGameAction { }` で構造的に保証 |
| APP-002 | `Given_record型がIGameActionを実装_When_IGameActionとして代入_Then_代入できる` | ダミー record で実装可能性を検証 |
