# DrowZzz ドロー (M1-PR4)

`DrawCardAction` の合法性判定 (`IsLegalMove`) と状態遷移 (`Apply`) を `DrowZzzRule` に実装する。

## 概要

ADR-0006 §2.4 / §M1-PR4 の決定に基づく。`DrawCardAction` は `WaitingForDraw` フェーズで現プレイヤーが行う「山札から手札に 1 枚移動」のアクション。Apply 後 `PhaseState` は `WaitingForPlay` に遷移する。プレイヤー Id は payload に持たず、`session.GameState.Turn.CurrentPlayerIndex` から暗黙取得する (ADR-0006 §2.1)。

本 PR では併せて、`DrowZzzRule.IsLegalMove` / `Apply` の **`session` / `action` 引数の null 検証** を追加する (M1-PR3 の reviewer 申し送り N-7)。`StartGameAction` 以外の `Apply` および `PlayCardAction` / `EndTurnAction` の `IsLegalMove` は依然 `NotImplementedException`(M1-PR5 / PR6 で実装)。

## 普遍要件 (Ubiquitous)

(本 PR で `DrowZzzRule` の構造的不変量は変更なし、skeleton.md DZ-011 / DZ-012 / DZ-034 を継承)

## 事象駆動要件 (Event-driven)

- [DZ-038] When `IsLegalMove(session, DrawCardAction)` is called and `session.PhaseState == WaitingForDraw`, then it shall return `true`.
- [DZ-039] When `IsLegalMove(session, DrawCardAction)` is called and `session.PhaseState != WaitingForDraw`, then it shall return `false`.
- [DZ-040] When `Apply(session, DrawCardAction)` is called and `IsLegalMove` returns `true`, then `result.GameState.Players[CurrentPlayerIndex].Hand.Count` shall equal `session.GameState.Players[CurrentPlayerIndex].Hand.Count + 1`.
- [DZ-041] When `Apply(session, DrawCardAction)` is called and `IsLegalMove` returns `true`, then `result.GameState.Deck.Count` shall equal `session.GameState.Deck.Count - 1`.
- [DZ-042] When `Apply(session, DrawCardAction)` is called and `IsLegalMove` returns `true`, then the card moved to the current player's hand shall be the original `Deck` Top card.
- [DZ-043] When `Apply(session, DrawCardAction)` is called and `IsLegalMove` returns `true`, then `result.PhaseState` shall be `WaitingForPlay`.
- [DZ-044] When `Apply(session, DrawCardAction)` is called and `IsLegalMove` returns `true`, then `result.GameState.Turn` shall remain unchanged from `session.GameState.Turn`.
- [DZ-045] When `Apply(session, DrawCardAction)` is called and `IsLegalMove` returns `true`, then non-current Players' `Hand` shall remain unchanged.

## 異常要件 (Unwanted)

- [DZ-046] If `Apply(session, DrawCardAction)` is called and `IsLegalMove` returns `false` (i.e. `session.PhaseState != WaitingForDraw`), then it shall throw `InvalidOperationException`.
- [DZ-047] If `Apply(session, DrawCardAction)` is called when `session.GameState.Deck.IsEmpty`, then it shall throw `InvalidOperationException` (Pile.Draw 由来、山札枯渇の本格対応は M2 以降).
- [DZ-048] If `IsLegalMove(null, action)` is called, then it shall throw `ArgumentNullException`.
- [DZ-049] If `IsLegalMove(session, null)` is called, then it shall throw `ArgumentNullException`.
- [DZ-050] If `Apply(null, action)` is called, then it shall throw `ArgumentNullException`.
- [DZ-051] If `Apply(session, null)` is called, then it shall throw `ArgumentNullException`.

## 定数依存

(本 PR では追加なし)

## Implementation Notes

- **現プレイヤー参照パターン**: `var current = session.GameState.Players[session.GameState.Turn.CurrentPlayerIndex]` を Rule 内のローカル変数で取得し、Hand 更新時に `with { Hand = current.Hand.Add(drawn) }` を使う。Players 配列は新しい配列に置き換えて `GameState` の `with { Players = newPlayers, Deck = remaining }` で更新する。
- **`session.GameState.Turn` は不変**: ターン進行(`TurnNumber` 加算 / `CurrentPlayerIndex` 進行)は `EndTurnAction.Apply` (M1-PR6) の責務。`DrawCardAction.Apply` では Turn を保持する。
- **`InvalidOperationException` の二重防御**: `DrowZzzRule.Apply` 内部でも `IsLegalMove` を呼んで `false` なら投げる。将来 `ApplyActionUseCase` (M1-PR6) でも同検証を行うが、Rule 側でも防御的に持つ (ADR-0006 §3 の方針)。
- **null 検証の範囲**: 本 PR で `IsLegalMove` / `Apply` の `session` / `action` の null 検証を追加。`PlayCardAction` / `EndTurnAction` も switch の `_` ケースに到達する前にガードされるが、それらの本格 case 実装は M1-PR5/PR6。

## 関連

- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalMove` / `Apply` 拡張)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzRuleTests.cs`(DZ-038〜051 を追加)
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §2.4 / §M1-PR4
- 関連: [`skeleton.md`](skeleton.md)(DZ-001〜017 / DZ-035/036)、[`setup.md`](setup.md)(DZ-018〜037)
- 後続: M1-PR5 (`PlayCardAction.Apply`)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-038 | `Given_WaitingForDrawフェーズ_When_DrawCardActionでIsLegalMoveを呼ぶ_Then_trueを返す` | |
| DZ-039 | 1 ID 2 テスト分割(3 値 enum の MC/DC 相当): `Given_WaitingForPlayフェーズ_..._Then_falseを返す` / `Given_WaitingForEndTurnフェーズ_..._Then_falseを返す` | `WaitingForDraw` 以外の全フェーズで false を返すことを検証 |
| DZ-040 | `Given_合法状態_When_DrawCardActionをApply_Then_現プレイヤーの手札枚数が1増える` | |
| DZ-041 | `Given_合法状態_When_DrawCardActionをApply_Then_山札枚数が1減る` | |
| DZ-042 | `Given_合法状態_When_DrawCardActionをApply_Then_山札Topのカードが現プレイヤーの手札に追加される` | |
| DZ-043 | `Given_合法状態_When_DrawCardActionをApply_Then_PhaseStateがWaitingForPlayに遷移する` | |
| DZ-044 | `Given_合法状態_When_DrawCardActionをApply_Then_GameStateTurnは不変` | |
| DZ-045 | `Given_合法状態_When_DrawCardActionをApply_Then_他プレイヤーの手札は不変` | |
| DZ-046 | `Given_WaitingForPlayフェーズ_When_DrawCardActionをApply_Then_InvalidOperationExceptionを投げる` | IsLegalMove false 時の防御 |
| DZ-047 | `Given_山札枯渇_When_DrawCardActionをApply_Then_InvalidOperationExceptionを投げる` | Pile.Draw 由来、本格対応は M2 |
| DZ-048 | `Given_sessionにnull_When_IsLegalMoveを呼ぶ_Then_ArgumentNullExceptionを投げる` | M1-PR3 申し送り N-7 反映 |
| DZ-049 | `Given_actionにnull_When_IsLegalMoveを呼ぶ_Then_ArgumentNullExceptionを投げる` | 同上 |
| DZ-050 | `Given_sessionにnull_When_Applyを呼ぶ_Then_ArgumentNullExceptionを投げる` | 同上 |
| DZ-051 | `Given_actionにnull_When_Applyを呼ぶ_Then_ArgumentNullExceptionを投げる` | 同上 |
