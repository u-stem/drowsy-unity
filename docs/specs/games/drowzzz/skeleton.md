# DrowZzz skeleton (M1-PR2)

DrowZzz 固有型(`DrowZzzAction` / `DrowZzzGameSession` / `DrowZzzTurnPhase` / `DrowZzzRule`)の skeleton 構造を確定する機能。

## 概要

ADR-0006 §2 の決定に基づき、DrowZzz の Application 層型骨格を `Drowsy.Application.Games.DrowZzz` namespace 配下に実装する。本 PR (M1-PR2) では:

- 型・名前空間・継承関係・field 構造の確定
- `DrowZzzGameSession` の null 検証 + cross-field 検証 (FirstDrowsyPoints のキーと GameState.Players の PlayerId 集合一致)
- `DrowZzzRule.IsLegalMove` / `Apply` は **`NotImplementedException`** (本格 Apply 実装は M1-PR3〜PR6)

までをスコープとする。

## 普遍要件 (Ubiquitous)

- [DZ-001] [Ubiquitous] The `DrowZzzAction` shall be an abstract record implementing `IGameAction` declared in the `Drowsy.Application.Games.DrowZzz` namespace.
- [DZ-002] [Ubiquitous] The `StartGameAction`, `DrawCardAction`, and `EndTurnAction` shall be sealed records inheriting `DrowZzzAction` without payload.
- [DZ-003] [Ubiquitous] The `PlayCardAction` shall be a sealed record inheriting `DrowZzzAction` with a `CardId Card` payload.
- [DZ-004] [Ubiquitous] The `DrowZzzTurnPhase` shall be an enum with members `WaitingForDraw`, `WaitingForPlay`, and `WaitingForEndTurn`.
- [DZ-005] [Ubiquitous] The `DrowZzzGameSession` shall be a record class with `GameState` (`Drowsy.Domain.Game.GameState`), `FirstDrowsyPoints` (`IReadOnlyDictionary<PlayerId, int>`), and `TurnPhase` (`DrowZzzTurnPhase`) init-only properties.
- [DZ-011] [Ubiquitous] The `DrowZzzRule` shall implement `IGameRule<DrowZzzAction, DrowZzzGameSession>`.
- [DZ-035] [Ubiquitous] The `DrowZzzGameSession` shall override `Equals(DrowZzzGameSession)` and `GetHashCode()` to compare by value(`FirstDrowsyPoints` は順序非依存マルチセット同値、`GameState` / `TurnPhase` は値同値、Phase 1 `GameState` と同パターン;ADR-0002 の判断軸)。

## 事象駆動要件 (Event-driven)

- [DZ-006] When `DrowZzzGameSession` is constructed with valid arguments, the property values shall be retained.
- [DZ-010] When `DrowZzzGameSession.CurrentRound` is computed (N=2), the value shall equal `(GameState.Turn.TurnNumber + 1) / 2`.
- [DZ-036] When two `DrowZzzGameSession` instances have field values that are value-equal (with `FirstDrowsyPoints` insertion order possibly different but key-value pairs identical), `Equals` shall return `true`.
- [DZ-012] When `DrowZzzRule.IsLegalMove` is called with a non-`StartGameAction` action in M1-PR3 stage, it shall throw `NotImplementedException`(本格実装は M1-PR4〜PR6 で各 Action 種別ごとに段階的に追加;`StartGameAction` 分岐は `setup.md` DZ-034 を参照).
- [DZ-013] When `DrowZzzRule.Apply` is called in skeleton stage, it shall throw `NotImplementedException`.

## 異常要件 (Unwanted)

- [DZ-007] If `DrowZzzGameSession` is constructed with `null` `GameState`, then it shall throw `ArgumentNullException`.
- [DZ-008] If `DrowZzzGameSession` is constructed with `null` `FirstDrowsyPoints`, then it shall throw `ArgumentNullException`.
- [DZ-009] If `DrowZzzGameSession` is constructed with `FirstDrowsyPoints` whose key set does not match `GameState.Players.Select(p => p.Id)`, then it shall throw `ArgumentException`.
- [DZ-014] If `with { GameState = null }` is applied to an existing `DrowZzzGameSession`, then it shall throw `ArgumentNullException`.
- [DZ-015] If `with { FirstDrowsyPoints = null }` is applied to an existing `DrowZzzGameSession`, then it shall throw `ArgumentNullException`.
- [DZ-016] If `with { FirstDrowsyPoints = ... }` whose key set does not match the existing `GameState.Players` `PlayerId` set is applied, then it shall throw `ArgumentException`.
- [DZ-017] If `with { GameState = ... }` whose `Players` `PlayerId` set does not match the existing `FirstDrowsyPoints` keys is applied, then it shall throw `ArgumentException`.

## 定数依存

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| プレイヤー数 N=2 (CurrentRound 計算式に暗黙) | L2 (Phase 2 暫定) | M1-PR2 では DrowZzzGameSession.CurrentRound 計算プロパティに直接埋め込み。N>2 拡張は Phase 3 候補(ADR-0006 §2.2 / §Negative) |

## 関連

- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §2 / §M1-PR2
- ADR: [`docs/adr/0005-phase2-roadmap-drowzzz.md`](../../../adr/0005-phase2-roadmap-drowzzz.md) — Phase 2 全体ロードマップ
- 実装 (本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzAction.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzTurnPhase.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`
- テスト (本 PR、Ubiquitous 要件 DZ-001〜005 / DZ-011 はテスト免除のため対応 fixture を作成しない):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzGameSessionTests.cs` (DZ-006〜010)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzRuleTests.cs` (DZ-012, DZ-013)
- シナリオ: `skeleton.feature`
- 後続: M1-PR3 (`StartGameAction.Apply` 実装 + `IGameConfig.FdpPool` 追加)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-001 | (テスト免除: Ubiquitous) | `abstract record DrowZzzAction : IGameAction` で構造的に保証 |
| DZ-002 | (テスト免除: Ubiquitous) | `sealed record StartGameAction : DrowZzzAction` 等で構造的に保証 |
| DZ-003 | (テスト免除: Ubiquitous) | `sealed record PlayCardAction(CardId Card) : DrowZzzAction` で構造的に保証 |
| DZ-004 | (テスト免除: Ubiquitous) | `enum DrowZzzTurnPhase { ... }` で構造的に保証 |
| DZ-005 | (テスト免除: Ubiquitous) | `record class DrowZzzGameSession` の init-only プロパティ宣言で構造的に保証 |
| DZ-006 | コンストラクタ正常系を 1 テスト 1 アサーションで 3 プロパティ分離: `Then_GameStateが入力と一致する` / `Then_FirstDrowsyPointsが入力と一致する` / `Then_TurnPhaseが入力と一致する` | record class の値保持 |
| DZ-007 | `Given_GameStateにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる` | コンストラクタ null 防御 |
| DZ-008 | `Given_FirstDrowsyPointsにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる` | コンストラクタ null 防御 |
| DZ-009 | キー集合不一致を 2 ケース分離: `Given_FirstDrowsyPointsのキー数がPlayersより少ない_...` / `Given_FirstDrowsyPointsのキーがPlayersと部分的に異なる_...` (Then 共通: `ArgumentException`) | コンストラクタ cross-field 検証 |
| DZ-010 | N=2 想定を 3 ケース分離: TurnNumber=1→1 / TurnNumber=2→1 / TurnNumber=3→2 | `(TurnNumber + 1) / 2` 計算検証 |
| DZ-011 | (テスト免除: Ubiquitous) | `class DrowZzzRule : IGameRule<DrowZzzAction, DrowZzzGameSession>` で構造的に保証 |
| DZ-012 | `Given_DrowZzzRule_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_NotImplementedExceptionを投げる` | non-`StartGameAction` の skeleton 段階意図(M1-PR4〜PR6 で本格実装) |
| DZ-013 | `Given_DrowZzzRule_When_Applyを呼ぶ_Then_NotImplementedExceptionを投げる` | skeleton 段階の意図 |
| DZ-014 | `Given_既存Session_When_with_GameStateにnull_Then_ArgumentNullExceptionを投げる` | with 式経由 null 防御 |
| DZ-015 | `Given_既存Session_When_with_FirstDrowsyPointsにnull_Then_ArgumentNullExceptionを投げる` | with 式経由 null 防御 |
| DZ-016 | `Given_既存Session_When_with_FirstDrowsyPointsをキー不一致に変更_Then_ArgumentExceptionを投げる` | with 式経由 cross-field 検証 |
| DZ-017 | `Given_既存Session_When_with_GameStateをPlayers不一致に変更_Then_ArgumentExceptionを投げる` | with 式経由 cross-field 検証 |
| DZ-035 | (テスト免除: Ubiquitous) | `Equals(DrowZzzGameSession)` / `GetHashCode()` の override 宣言で構造的に保証(挙動は DZ-036 で検証) |
| DZ-036 | `Given_同フィールド値の2つのDrowZzzGameSession_When_等価比較_Then_等価` / `Given_FirstDrowsyPoints挿入順が異なる2つのDrowZzzGameSession_When_等価比較_Then_等価` | 値同値性(順序非依存マルチセット)の挙動検証 |
