# 勝利条件と終了判定 (M3-PR1)

ADR-0010 で確定した DrowZzz の勝利条件 + ゲーム終了判定の振る舞いを集約する仕様。
早期勝利 / 終了時勝利 / 引き分け / Round 22 ガードの 4 軸を 1 文書で扱う。

## 用語 / 前提

| 用語 | 意味 |
| ---- | ---- |
| **持ち点(TotalPoints)** | FDP + DDP + SDP の合計(ADR-0009 §「持ち点」、`DrowZzzGameSession.TotalPoints(playerId)`)|
| **早期勝利** | 夜の間に持ち点 ≥ 100 + 就寝カード(`EarlyWinTriggerEffect` を持つカード)で発火 |
| **終了時勝利** | Round 21 完了後に持ち点が低い方が勝利 |
| **引き分け** | 終了時に両プレイヤーの持ち点が等値の場合(tiebreaker なし)|
| **Outcome** | `GameOutcome?`(null = 未終了 / WinnerOutcome / DrawOutcome)|

## 早期勝利(`EarlyWinTriggerEffect`)

- [DZ-183] When `EarlyWinTriggerEffect` is evaluated by `EffectInterpreter.Apply` and the session satisfies `Clock.IsNight == true` and `TotalPoints[currentPlayer] >= DrowZzzVictoryConstants.EarlyWinScoreThreshold`(= 100), the resulting session shall have `Outcome = new WinnerOutcome(currentPlayer)`.
- [DZ-184] When `EarlyWinTriggerEffect` is evaluated in morning(`Clock.IsMorning == true`), the resulting session shall have `Outcome` unchanged (no-op, ADR-0010 §5).
- [DZ-185] When `EarlyWinTriggerEffect` is evaluated with `TotalPoints[currentPlayer] < 100`, the resulting session shall have `Outcome` unchanged (no-op).
- [DZ-186] [Optional] When `EarlyWinTriggerEffect` is re-evaluated on a session that already has `Outcome != null`, the existing `Outcome` is overwritten based on the current condition evaluation. 通常経路では `DrowZzzRule.IsLegalMove` が終了済 session への Action を全て illegal 化する(DZ-189)ため、再評価は発生しない構造保証。`[Optional]` マーカーで定義し冪等性を仕様化する。

## 終了時勝利(`DrowZzzRule.ApplyEndTurn` 内 Round 21 完了検出)

- [DZ-190] When `EndTurnAction` is applied and the resulting turn meets `newTurn.CurrentPlayerIndex == 0 && nextSession.Clock.RoundNumber > DrowZzzClockConstants.MaxRoundNumber`(= 22 以上), the resulting session shall have `Outcome` set as follows:
  - If `TotalPoints[p1] < TotalPoints[p2]`: `Outcome = new WinnerOutcome(p1)`
  - If `TotalPoints[p1] > TotalPoints[p2]`: `Outcome = new WinnerOutcome(p2)`
  - If equal: `Outcome = new DrawOutcome()`(tiebreaker なし、ADR-0010 §7)

## Round 21 内の境界保持

- [DZ-191] When `EndTurnAction` is applied within Round 21(e.g., transition from `TurnNumber=41` to `TurnNumber=42` where new Round is still 21), `Outcome` shall remain unchanged (Round 22 への到達前なので終了判定は走らない)。

## 終了済 session への Action は illegal(Round 22 ガード)

- [DZ-189] [Ubiquitous] When `session.IsTerminated == true` (i.e., `Outcome != null`), `DrowZzzRule.IsLegalMove(session, action)` shall return `false` for **all** `DrowZzzAction` types (DrawCardAction / PlayCardAction / EndTurnAction / StartGameAction)。ADR-0010 §6 / ADR-0008 §5「過渡的範囲」を明示ガードに昇格。

## `DrowZzzRule.IsTerminated` / `GetWinner` 契約

- [DZ-187] [Ubiquitous] `DrowZzzRule.IsTerminated(session)` shall return `session.IsTerminated`(= `session.Outcome != null`)。null session は `ArgumentNullException`。
- [DZ-188] `DrowZzzRule.GetWinner(session)` の契約:
  - `session.IsTerminated == false`(未終了)→ `InvalidOperationException` を投げる
  - `Outcome is WinnerOutcome(p)` → `p` を返す
  - `Outcome is DrawOutcome` → `null` を返す

## `DrowZzzGameSession.Outcome` の保持と Equals 寄与

- [DZ-192] [Ubiquitous] `DrowZzzGameSession` は `Outcome: GameOutcome?` プロパティを持ち、`IsTerminated` computed プロパティを `Outcome != null` の薄いラッパーとして提供する。Equals は `Outcome` の差異を反映する(null / WinnerOutcome / DrawOutcome の三値比較)。

## 定数依存

- `DrowZzzClockConstants.MaxRoundNumber = 21`(ADR-0010 §8、Clock 構造に紐づく L2 不変量)
- `DrowZzzVictoryConstants.EarlyWinScoreThreshold = 100`(ADR-0010 §9、勝利条件関連 L2 不変量)

## 関連

- ADR: [`docs/adr/0010-m3-game-termination-and-victory-determination.md`](../../../adr/0010-m3-game-termination-and-victory-determination.md)(全体)
- 前提 ADR: ADR-0008 §5(Round 22+ 過渡的範囲、本 PR で明示ガードに昇格)/ ADR-0009 §5 §6(M3 範囲の予告、本 ADR で具体化)
- 前提モデル: [`../../domain/game/game-outcome.md`](../../domain/game/game-outcome.md)
- 関連効果: [`./effects/early-win-trigger.md`](./effects/early-win-trigger.md)
- 関連: [`./dp-mechanism.md`](./dp-mechanism.md)(持ち点の computed)/ [`./end-turn.md`](./end-turn.md)(EndTurn 拡張)
- 実装: `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs` / `DrowZzzGameSession.cs` / `DrowZzzVictoryConstants.cs`
- テスト: `DrowZzzRuleTests`(DZ-187〜DZ-191)/ `DrowZzzGameSessionTests`(DZ-192)/ `EarlyWinTriggerEffectTests`(DZ-183〜DZ-186)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-183 | `Given_夜_持ち点100_When_EarlyWinTriggerをApply_Then_OutcomeがWinnerOutcomeになる` / `..._持ち点100超過_..._Then_OutcomeがWinnerOutcomeになる` | 閾値ちょうど + 超過 |
| DZ-184 | `Given_朝_持ち点100_When_EarlyWinTriggerをApply_Then_Outcomeは設定されない` | 朝の no-op |
| DZ-185 | `Given_夜_持ち点99_When_EarlyWinTriggerをApply_Then_Outcomeは設定されない` | 閾値未満の no-op |
| DZ-186 | `Given_既にWinnerOutcome設定済_When_EarlyWinTriggerをApply_Then_新規WinnerOutcomeで上書きされる` | 再評価冪等性([Optional])|
| DZ-187 | `Given_未終了Session_When_IsTerminated_Then_false` / `..._WinnerOutcome設定済Session_..._Then_true` | IsTerminated 契約 |
| DZ-188 | `Given_WinnerOutcome設定済Session_When_GetWinner_Then_勝者PlayerIdを返す` / `..._DrawOutcome設定済Session_..._Then_nullを返す` / `..._未終了Session_..._Then_InvalidOperationExceptionを投げる` | GetWinner 契約(3 ケース)|
| DZ-189 | `Given_終了済Session_When_DrawCardActionでIsLegalMove_Then_false` / `..._EndTurnActionでIsLegalMove_Then_false` | 終了済での illegal |
| DZ-190 | `Given_Round21最終フェーズでp1低スコア_When_EndTurnでRound22到達_Then_Outcomeはp1勝利` / `..._両者同点_..._Then_OutcomeはDraw` | 終了時勝利 + 引き分け |
| DZ-191 | `Given_Round21先手フェーズ完了_When_EndTurnでp2にローテート_Then_Outcomeは未設定` | Round 21 内境界 |
| DZ-192 | `DrowZzzGameSessionTests` 内の Outcome 関連 5 テスト | 値保持 / IsTerminated / Equals 寄与 |
