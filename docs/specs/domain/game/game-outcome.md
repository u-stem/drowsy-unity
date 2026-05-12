# `GameOutcome`(ゲーム終了状態) (M3-PR1)

ADR-0010 §2 で Domain 層に新設された値オブジェクト。ゲームが終了したことと、その終わり方(勝者あり / 引き分け)を表す。

## 階層

| 型 | 役割 |
| ---- | ---- |
| `abstract record GameOutcome` | 階層の基底、`null` = 未終了を表現するため(`GameOutcome?` で「未終了」を表す)|
| `sealed record WinnerOutcome(PlayerId Winner)` | 特定プレイヤーが勝者として確定 |
| `sealed record DrawOutcome` | 引き分け(両プレイヤーの持ち点が等値)|

## 普遍要件 (Ubiquitous)

- [GS-101] [Ubiquitous] `WinnerOutcome` shall be a `sealed record` with positional `(PlayerId Winner)`, derive from `GameOutcome`, and require null防御の二重ガード(backing-field initializer + init setter body)を備える型構造であること。
- [GS-103] [Ubiquitous] `DrowOutcome` shall be a `sealed record` with no fields, derive from `GameOutcome`, and shall be value-equal to any other `DrawOutcome` instance via record auto-equals.
- [GS-104] [Ubiquitous] A `WinnerOutcome` and a `DrawOutcome` shall not be value-equal (different runtime types).

## 事象駆動要件 (Event-driven)

- [GS-105] When `WinnerOutcome` is constructed with `Winner == null`, the constructor shall throw `ArgumentNullException`. The same protection applies to `with { Winner = null }` mutation (double-guard).
- [GS-102] When two `WinnerOutcome` instances have the same `Winner` value, `Equals` shall return `true`. When they differ, `Equals` shall return `false`.

## 配置の根拠

Domain 層に置く(ADR-0010 §2 採用判断):

- 「ゲームに勝者がいる / 引き分け」はゲーム非依存の汎用概念
- `IGameRule<TAction, TSession>.GetWinner` の generic 性質と整合
- ADR-0002 Domain ゲーム非依存原則(`GameOutcome` 階層は DrowZzz 固有でなく、他ゲーム実装でも `IGameRule` 経由で使える)

## 関連

- ADR: [`docs/adr/0010-m3-game-termination-and-victory-determination.md`](../../../adr/0010-m3-game-termination-and-victory-determination.md) §2
- 利用先: `Drowsy.Application.Games.DrowZzz.DrowZzzGameSession.Outcome` プロパティ、`IGameRule.GetWinner` 戻り値
- 実装(本 PR): `Assets/_Project/Scripts/Domain/Game/GameOutcome.cs`
- テスト(本 PR): `Assets/_Project/Scripts/Tests/Domain.Tests/Game/GameOutcomeTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| GS-101 | `Given_有効なPlayerId_When_WinnerOutcomeを生成_Then_Winnerが入力と一致する` | 構築の正常系(record 型構造の確認) |
| GS-102 | `Given_同PlayerIdの2WinnerOutcome_When_Equals_Then_true` / `Given_異なるPlayerIdの2WinnerOutcome_When_Equals_Then_false` | 値同値 |
| GS-103 | `Given_2つのDrawOutcome_When_Equals_Then_true` | DrawOutcome 常等価 |
| GS-104 | `Given_WinnerOutcomeとDrawOutcome_When_Equals_Then_false` | 派生型非等価 |
| GS-105 | `Given_Winnerにnull_When_WinnerOutcomeを生成_Then_ArgumentNullExceptionを投げる` / `Given_既存WinnerOutcome_When_withでWinnerにnull_Then_ArgumentNullExceptionを投げる` | 二重ガード null 防御 |
