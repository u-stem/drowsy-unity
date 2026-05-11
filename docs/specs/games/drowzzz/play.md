# DrowZzz カードプレイ (M1-PR5)

`PlayCardAction(CardId Card)` の合法性判定 (`IsLegalMove`) と状態遷移 (`Apply`) を `DrowZzzRule` に実装する。`PlayCardAction` 自体の null 防御 (`Card == null` 拒否) も併せて追加。

## 概要

ADR-0006 §2.4 / §M1-PR5 の決定に基づく。`PlayCardAction(card)` は `WaitingForPlay` フェーズで現プレイヤーが行う「手札の指定 `card` を場 (Field) に出す」アクション。Apply 後 `TurnPhase` は `WaitingForEndTurn` に遷移する。Field への追加方向は **`AddTop`**(直近プレイカードが index 0、プロジェクトオーナー JIT 確定 2026-05-11)。

`PlayCardAction.Card` の null は **`PlayCardAction` の生成時** に `ArgumentNullException` で弾く(record positional + init setter で `Card ?? throw` パターン、Phase 1 `Hand` / `Pile` の null 検証パターンと整合)。`with { Card = null }` 経由でも同様に弾かれる。

## 普遍要件 (Ubiquitous)

- [DZ-052] [Ubiquitous] The `PlayCardAction.Card` shall be non-null(バッキングフィールド `_card` の初期化式 `= Card ?? throw` + getter/setter 全置換 init setter 本体 `init => _card = value ?? throw` の二重ガードで positional ctor 経由 / with 式経由の両経路をカバー、Phase 1 `GameState` と同パターン + CS8907 回避)。

## 事象駆動要件 (Event-driven)

- [DZ-054] When `IsLegalMove(session, PlayCardAction(card))` is called and `session.TurnPhase == WaitingForPlay` and the current player's `Hand` contains `card`, then it shall return `true`.
- [DZ-057] When `Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then `result.GameState.Players[CurrentPlayerIndex].Hand` shall not contain `card`.
- [DZ-058] When `Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then `result.GameState.Players[CurrentPlayerIndex].Hand.Count` shall equal `session.GameState.Players[CurrentPlayerIndex].Hand.Count - 1`.
- [DZ-059] When `Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then `result.GameState.Field.Cards[0]` shall equal `card` (`AddTop` 採用、直近プレイが Top)。
- [DZ-060] When `Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then `result.GameState.Field.Count` shall equal `session.GameState.Field.Count + 1`.
- [DZ-061] When `Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then `result.TurnPhase` shall be `WaitingForEndTurn`.
- [DZ-062] When `Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then `result.GameState.Turn` shall remain unchanged.
- [DZ-063] When `Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then `result.GameState.Deck` shall remain unchanged.
- [DZ-064] When `Apply(session, PlayCardAction(card))` is called and `IsLegalMove` returns `true`, then non-current Players' `Hand` shall remain unchanged.

## 異常要件 (Unwanted)

- [DZ-053] If `new PlayCardAction(null)` is called, then it shall throw `ArgumentNullException`.
- [DZ-055] If `IsLegalMove(session, PlayCardAction(card))` is called and `session.TurnPhase != WaitingForPlay`, then it shall return `false`(`WaitingForDraw` / `WaitingForEndTurn` 各々検証)。
- [DZ-056] If `IsLegalMove(session, PlayCardAction(card))` is called and the current player's `Hand` does not contain `card`, then it shall return `false`.
- [DZ-065] If `Apply(session, PlayCardAction(card))` is called and `session.TurnPhase != WaitingForPlay` (IsLegalMove false), then it shall throw `InvalidOperationException`.
- [DZ-066] If `Apply(session, PlayCardAction(card))` is called and the current player's `Hand` does not contain `card` (IsLegalMove false), then it shall throw `InvalidOperationException`.

## 定数依存

(本 PR では追加なし)

## Implementation Notes

- **Field 追加方向**: `AddTop` 採用(プロジェクトオーナー JIT 確定 2026-05-11)。`Field.Cards[0]` = 直近プレイカード、末尾 = 最初のプレイ。M2 のカード効果実装で「直前のカードを参照する」効果がある場合に `Field.Cards[0]` で取れる。
- **Hand.Remove の利用**: Phase 1 `Hand.Remove(CardId)` は不在カードで `ArgumentException` を投げる。`Apply` の防御的検証で「`Hand.Cards.Contains(card)` を先に確認」しているため、`Remove` は必ず成功する経路を通る。
- **PlayCardAction の null 防御パターン (二重ガード)**: record positional `(CardId Card)` を保ちつつ、以下の 2 経路を別々に防御する:
  1. **バッキングフィールド `_card` の初期化式** (`= Card ?? throw new ArgumentNullException(nameof(Card))`) で positional ctor 経由の null を弾く。`Card` 引数を初期化式で参照することで CS8907 警告 (「Parameter 'Card' is unread」) も回避する。
  2. **init setter 本体** (`init => _card = value ?? throw new ArgumentNullException(nameof(value))`) で `with { Card = null }` 経由の null を弾く。初期化式は constructor 1 回のみ評価で with 経路をカバーしないため、Phase 1 `GameState` と同じ「getter/setter 全置換 init setter」パターンを採用 (ADR-0002 / ADR-0004)。
- **`session.GameState.Turn` / `Deck` は不変**: ターン進行は `EndTurnAction.Apply` (M1-PR6)、ドローは `DrawCardAction.Apply` (M1-PR4) の責務。`PlayCardAction.Apply` では `Hand` と `Field` のみ変更する。
- **`InvalidOperationException` の二重防御**: `DrowZzzRule.Apply` 内部で IsLegalMove 違反 (TurnPhase / Card 不在の両方) を検証して投げる(ADR-0006 §3 方針)。

## 関連

- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzAction.cs`(`PlayCardAction` の null 防御パターン適用)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalMove` / `Apply` 拡張)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzRuleTests.cs`(DZ-054〜066 を追加、DZ-012/013 を `EndTurnAction` に変更)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/PlayCardActionTests.cs`(DZ-052/053 の null 防御テスト)
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §2.4 / §M1-PR5
- 関連: [`skeleton.md`](skeleton.md)、[`setup.md`](setup.md)、[`draw.md`](draw.md)
- 後続: M1-PR6 (`EndTurnAction.Apply` + `ApplyActionUseCase`)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-052 | (テスト免除: Ubiquitous) | `init` setter の `value ?? throw` 宣言で構造的に保証(挙動は DZ-053 で検証) |
| DZ-053 | `Given_PlayCardActionにnullCard_When_生成_Then_ArgumentNullExceptionを投げる` / `Given_既存PlayCardAction_When_with_Cardにnull_Then_ArgumentNullExceptionを投げる` | 生成 + with 式の両経路 |
| DZ-054 | `Given_WaitingForPlayかつCardが手札にある_When_PlayCardActionでIsLegalMoveを呼ぶ_Then_trueを返す` | |
| DZ-055 | 1 ID 2 テスト分割: `Given_WaitingForDraw_..._Then_falseを返す` / `Given_WaitingForEndTurn_..._Then_falseを返す` | 3 値 enum の MC/DC 相当 |
| DZ-056 | `Given_WaitingForPlayだがCardが手札にない_When_IsLegalMoveを呼ぶ_Then_falseを返す` | |
| DZ-057 | `Given_合法状態_When_PlayCardActionをApply_Then_現プレイヤーHandから指定Cardが除かれる` | |
| DZ-058 | `Given_合法状態_When_PlayCardActionをApply_Then_現プレイヤーHandCount-1` | |
| DZ-059 | `Given_合法状態_When_PlayCardActionをApply_Then_FieldのTopが指定Card` | AddTop 検証 |
| DZ-060 | `Given_合法状態_When_PlayCardActionをApply_Then_FieldCount+1` | |
| DZ-061 | `Given_合法状態_When_PlayCardActionをApply_Then_TurnPhaseがWaitingForEndTurnに遷移する` | |
| DZ-062 | `Given_合法状態_When_PlayCardActionをApply_Then_GameStateTurnは不変` | |
| DZ-063 | `Given_合法状態_When_PlayCardActionをApply_Then_GameStateDeckは不変` | |
| DZ-064 | `Given_合法状態_When_PlayCardActionをApply_Then_他プレイヤーの手札は不変` | |
| DZ-065 | `Given_WaitingForDraw_When_PlayCardActionをApply_Then_InvalidOperationExceptionを投げる` | TurnPhase 違反 |
| DZ-066 | `Given_Cardが手札にない_When_PlayCardActionをApply_Then_InvalidOperationExceptionを投げる` | Card 不在違反 |
