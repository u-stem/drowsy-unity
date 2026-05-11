# ApplyActionUseCase

セッション既存状態に対して `DrowZzzAction` を適用する統一 UseCase。`StartGameUseCase` (セッション未生成スタート、M1-PR3) とは独立した別系統。

## 概要

ADR-0006 §3 / §M1-PR6 の決定に基づく。`ApplyActionUseCase.Execute(session, action)` は **内部で `IsLegalMove` を検証し、`false` の場合は `InvalidOperationException`** を投げる(ADR-0006 §3 §IsLegalMove 違反時の方針)。`true` なら `DrowZzzRule.Apply` に委譲する薄い抽象化層。

`DrawCardAction` / `PlayCardAction` / `EndTurnAction` の 3 種を統一 API で適用できるため、UI / リプレイ / ログ処理の実装が簡潔になる(ADR-0006 §3 「Action 種別ごとの個別 UseCase」を採らない判断)。`StartGameAction` は `DrowZzzRule.IsLegalMove` で常に `false` を返す設計のため、本 UseCase 経由で呼ぶと `InvalidOperationException` が投げられる(ADR-0006 §Implementation Notes §StartGameUseCase の `IsLegalMove` 経由での扱い)。

## 普遍要件 (Ubiquitous)

- [APP-021] [Ubiquitous] The `ApplyActionUseCase` shall be a sealed class in the `Drowsy.Application.Games.DrowZzz` namespace with constructor injection of `DrowZzzRule`.

## 事象駆動要件 (Event-driven)

(汎用契約「IsLegalMove が true のときは rule.Apply の結果を返す」は具体的に APP-023〜025 の各 Action 種別テストでカバーする)

- [APP-023] When `Execute(session, DrawCardAction)` is called in `WaitingForDraw` phase, then the result shall be the same as `rule.Apply(session, DrawCardAction)` directly (薄い委譲)。
- [APP-024] When `Execute(session, PlayCardAction(card))` is called in `WaitingForPlay` phase with `card` in hand, then the result shall be the same as `rule.Apply(session, action)` directly.
- [APP-025] When `Execute(session, EndTurnAction)` is called in `WaitingForEndTurn` phase, then the result shall be the same as `rule.Apply(session, action)` directly.

## 異常要件 (Unwanted)

- [APP-026] If `Execute(session, action)` is called and `rule.IsLegalMove(session, action)` returns `false` (e.g., `DrawCardAction` in `WaitingForPlay`), then it shall throw `InvalidOperationException` (ADR-0006 §3)。
- [APP-027] If `Execute(session, StartGameAction)` is called, then it shall throw `InvalidOperationException` (`IsLegalMove(StartGameAction)` is always `false`、ADR-0006 §Implementation Notes)。
- [APP-028] If `Execute(null, action)` is called, then it shall throw `ArgumentNullException`.
- [APP-029] If `Execute(session, null)` is called, then it shall throw `ArgumentNullException`.
- [APP-030] If `new ApplyActionUseCase(null)` is called, then it shall throw `ArgumentNullException`.

## Implementation Notes

- **`DrowZzzRule` 直接 DI**: 設計上は `IGameRule<DrowZzzAction, DrowZzzGameSession>` 抽象を DI する選択肢もあるが、ADR-0006 §3 「namespace 配置の判断」で「両 UseCase は `Drowsy.Application.Games.DrowZzz` 配下、DrowZzz 固有」と明記済のため `DrowZzzRule` を直接受け取る形で OK。M2 以降で別ゲームを追加する際に汎用 `ApplyActionUseCase<TAction, TSession>` を `Drowsy.Application` 直下に検討(YAGNI、現時点は不要)。
- **null 検証**: `session` / `action` の null は `ArgumentNullException`。`DrowZzzRule.IsLegalMove` 自体も null 検証するため二重防御だが、UseCase 層の責務として独立保持(将来 UseCase 単体で他 Rule に切替可能性を残す)。
- **InvalidOperationException メッセージ**: 「IsLegalMove が false を返したため Apply できない」旨と Action 種別 + TurnPhase を明示。

## 関連

- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/ApplyActionUseCase.cs`
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/ApplyActionUseCaseTests.cs`
- 関連: `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`、[`end-turn.md`](../games/drowzzz/end-turn.md)
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §3 / §M1-PR6 / §Implementation Notes
- 後続: M1-PR7 (統合テスト)、M5 (VContainer 統合)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-021 | (テスト免除: Ubiquitous) | `sealed class ApplyActionUseCase` の宣言で構造的に保証 |
| APP-023 | `Given_WaitingForDraw_When_DrawCardActionをExecute_Then_RuleApplyの結果と一致する` | 委譲確認 |
| APP-024 | `Given_WaitingForPlay_When_PlayCardActionをExecute_Then_RuleApplyの結果と一致する` | |
| APP-025 | `Given_WaitingForEndTurn_When_EndTurnActionをExecute_Then_RuleApplyの結果と一致する` | |
| APP-026 | `Given_WaitingForPlayでDrawCardAction_When_Execute_Then_InvalidOperationExceptionを投げる` | IsLegalMove false の代表 1 ケース |
| APP-027 | `Given_StartGameAction_When_Execute_Then_InvalidOperationExceptionを投げる` | StartGameAction は常に IsLegalMove false |
| APP-028 | `Given_sessionにnull_When_Execute_Then_ArgumentNullExceptionを投げる` | |
| APP-029 | `Given_actionにnull_When_Execute_Then_ArgumentNullExceptionを投げる` | |
| APP-030 | `Given_ruleにnull_When_ApplyActionUseCaseを生成_Then_ArgumentNullExceptionを投げる` | constructor 引数 null 検証 |
