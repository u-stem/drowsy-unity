# DrowZzz ターン終了 (M1-PR6)

`EndTurnAction` の合法性判定 (`IsLegalMove`) と状態遷移 (`Apply`) を `DrowZzzRule` に実装する。本 PR で M1 範囲の `DrowZzzRule` 実装は完了する(`StartGameAction` は依然 `IGameRule` ルートではなく `StartGameUseCase` 経由)。

## 概要

ADR-0006 §2.4 / §M1-PR6 の決定に基づく。`EndTurnAction` は `WaitingForEndTurn` フェーズで現プレイヤーが行う「ターン終了」アクション。Apply で `GameState.Turn` を `Next(playerCount)` で次フェーズへ進行させ、`PhaseState` を `WaitingForDraw` に戻す。Field / Hand / Deck / Discard / FirstDrowsyPoints は不変。

## 普遍要件 (Ubiquitous)

(本 PR で `DrowZzzRule` の構造的不変量は変更なし。skeleton.md DZ-011 を継承)

## 事象駆動要件 (Event-driven)

- [DZ-067] When `IsLegalMove(session, EndTurnAction)` is called and `session.PhaseState == WaitingForEndTurn`, then it shall return `true`.
- [DZ-069] When `Apply(session, EndTurnAction)` is called and `IsLegalMove` returns `true`, then `result.GameState.Turn.TurnNumber` shall equal `session.GameState.Turn.TurnNumber + 1`.
- [DZ-070] When `Apply(session, EndTurnAction)` is called and `IsLegalMove` returns `true`, then `result.GameState.Turn.CurrentPlayerIndex` shall equal `(session.GameState.Turn.CurrentPlayerIndex + 1) % session.GameState.Players.Count`.
- [DZ-071] When `Apply(session, EndTurnAction)` is called and `IsLegalMove` returns `true`, then `result.PhaseState` shall be `WaitingForDraw`.
- [DZ-072] When `Apply(session, EndTurnAction)` is called and `IsLegalMove` returns `true`, then `result.GameState.Players` (Hand 含む) shall remain unchanged.
- [DZ-073] When `Apply(session, EndTurnAction)` is called and `IsLegalMove` returns `true`, then `result.GameState.Deck` shall remain unchanged.
- [DZ-074] When `Apply(session, EndTurnAction)` is called and `IsLegalMove` returns `true`, then `result.GameState.Field` shall remain unchanged.
- [DZ-075] When `Apply(session, EndTurnAction)` is called and `IsLegalMove` returns `true`, then `result.FirstDrowsyPoints` shall remain unchanged.

## 異常要件 (Unwanted)

- [DZ-068] If `IsLegalMove(session, EndTurnAction)` is called and `session.PhaseState != WaitingForEndTurn`, then it shall return `false`(`WaitingForDraw` / `WaitingForPlay` 各々検証)。
- [DZ-076] If `Apply(session, EndTurnAction)` is called and `session.PhaseState != WaitingForEndTurn` (IsLegalMove false), then it shall throw `InvalidOperationException`(`WaitingForDraw` / `WaitingForPlay` 各々検証、DZ-068 と一貫の MC/DC 相当)。

## 定数依存

(本 PR では追加なし。`MaxRoundNumber` によるゲーム終了判定は ADR-0006 §M1 範囲外、M3 で実装)

## Implementation Notes

- **`TurnState.Next(playerCount)` の利用**: Phase 1 で実装済の `TurnState.Next(int playerCount)` をそのまま使う(`TurnNumber + 1`、`CurrentPlayerIndex = (current + 1) % playerCount`)。`playerCount` には `session.GameState.Players.Count` を渡す。
- **Field / Hand / Deck / Discard 不変**: `EndTurnAction.Apply` は `Turn` フィールドのみ更新。`gameState with { Turn = newTurn }` で他フィールドを暗黙保持する(record の with 式)。
- **PhaseState 戻し**: 次プレイヤーの最初のフェーズは `WaitingForDraw` で固定(M1 範囲では「省略」「特殊な初手」等の分岐は未実装、ADR-0006 §6 で M1 範囲外と確定)。
- **ターン上限判定なし**: 全体 20 ターン (M3 で実装) のチェックは本 PR では行わない。`TurnNumber` は単調増加し続ける。M3 の `IsTerminated(session)` 実装で `TurnNumber > MaxRoundNumber × Players.Count` を判定する設計(ADR-0006 §7)。`MaxRoundNumber` 実装名は維持(ADR-0009 §6.5、用語規約は「ターン」だが実装名リネームは別 PR で追随予定)。

## 関連

- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalMove` / `Apply` の `EndTurnAction` ケース追加)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzRuleTests.cs`(DZ-067〜076 を追加、既存 DZ-012/013 のテスト対象を `EndTurnAction` から `UnknownDrowZzzAction` ダミー派生型に更新して `_` ケースをカバー)
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §2.4 / §M1-PR6 / §7
- 関連: [`skeleton.md`](skeleton.md)、[`setup.md`](setup.md)、[`draw.md`](draw.md)、[`play.md`](play.md)、[`apply-action-usecase.md`](../application/apply-action-usecase.md)
- 後続: M1-PR7(統合テスト N=2 数ターン)、M3 着手 PR (`MaxRoundNumber` + ゲーム終了判定)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-067 | `Given_WaitingForEndTurn_When_EndTurnActionでIsLegalMoveを呼ぶ_Then_trueを返す` | |
| DZ-068 | 1 ID 2 テスト分割: `Given_WaitingForDraw_..._Then_falseを返す` / `Given_WaitingForPlay_..._Then_falseを返す` | 3 値 enum の MC/DC 相当 |
| DZ-069 | `Given_合法状態_When_EndTurnActionをApply_Then_TurnNumberが1増える` | |
| DZ-070 | 1 ID 2 テスト分割: `Given_合法状態_CurrentPlayer0_..._Then_CurrentPlayerIndexが1に進む` / `Given_合法状態_CurrentPlayer1_..._Then_CurrentPlayerIndexが0にラップする` | (current + 1) % N の進行 + ラップ両方を検証 |
| DZ-071 | `Given_合法状態_When_EndTurnActionをApply_Then_PhaseStateがWaitingForDrawに戻る` | |
| DZ-072 | `Given_合法状態_When_EndTurnActionをApply_Then_Playersは不変` | |
| DZ-073 | `Given_合法状態_When_EndTurnActionをApply_Then_Deckは不変` | |
| DZ-074 | `Given_合法状態_When_EndTurnActionをApply_Then_Fieldは不変` | |
| DZ-075 | `Given_合法状態_When_EndTurnActionをApply_Then_FirstDrowsyPointsは不変` | |
| DZ-076 | 1 ID 2 テスト分割: `Given_WaitingForDraw_..._Then_InvalidOperationExceptionを投げる` / `Given_WaitingForPlay_..._Then_InvalidOperationExceptionを投げる` | PhaseState 違反、3 値 enum の MC/DC 相当 |
