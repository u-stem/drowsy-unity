# `EarlyWinTriggerEffect` (M3-PR1)

ADR-0010 §5 で新設された **早期勝利トリガー効果**。本効果を効果列に持つカードを「**就寝カード**」と定義する(ADR-0007 §1「カード効果は `IEffect` で表現」と整合)。

## 構造

フィールドなしのマーカー的 `sealed record`。閾値(100)は `DrowZzzVictoryConstants.EarlyWinScoreThreshold` を `EffectInterpreter` 内で参照する形にし、effect record 自身は保持しない(ADR-0010 §5 / §9、全就寝カード共通のゲームルール定数として constants 化)。

```csharp
public sealed record EarlyWinTriggerEffect : IEffect;
```

## 評価ロジック(`EffectInterpreter.Apply`)

| 条件 | 結果 |
| ---- | ---- |
| `Clock.IsNight == true` かつ `TotalPoints[currentPlayer] >= EarlyWinScoreThreshold`(= 100)| `session.Outcome = new WinnerOutcome(currentPlayer)` |
| `Clock.IsMorning == true`(朝)/ `RoundNumber > 21`(時計仕様外)| no-op(session 不変返却)|
| `TotalPoints[currentPlayer] < 100`(閾値未満)| 同上 |

## 普遍要件 (Ubiquitous)

- [DZ-183.0] [Ubiquitous] `EarlyWinTriggerEffect` shall be a `sealed record` with no fields, implementing `IEffect`, declared in `Drowsy.Application.Games.DrowZzz.Effects` namespace.

## 事象駆動要件 (Event-driven)

- [DZ-183] When `EarlyWinTriggerEffect` is evaluated and `Clock.IsNight` is `true` and `TotalPoints[currentPlayer]` is at or above `EarlyWinScoreThreshold`(= 100), the resulting session shall have `Outcome = new WinnerOutcome(currentPlayer)`.

## 異常 / 任意要件 (Unwanted / Optional)

- [DZ-184] When `EarlyWinTriggerEffect` is evaluated in morning(`Clock.IsMorning == true`), the resulting session shall have `Outcome` unchanged.
- [DZ-185] When `EarlyWinTriggerEffect` is evaluated with `TotalPoints[currentPlayer] < 100`, the resulting session shall have `Outcome` unchanged.
- [DZ-186] [Optional] 既に `Outcome` が設定済の session に対する再評価は、現在条件で再判定し新規 `WinnerOutcome` で上書きする(冪等性に近い性質、通常経路では `DrowZzzRule.IsLegalMove` の終了済 session ガード DZ-189 で発生しない)。

## 「就寝カード」の暗黙定義

`EarlyWinTriggerEffect` を効果列に持つカードを「就寝カード」と呼ぶ(ADR-0010 §5 Neutral)。最初の利用カード(就寝カード No.X)は本 PR では確定せず、M2-PR6+ で JIT 共有される運用(ADR-0010 §「Implementation Notes」/ Neutral)。

## 定数依存

- `DrowZzzVictoryConstants.EarlyWinScoreThreshold = 100`(ADR-0010 §9、本 PR で新設)

## 関連

- ADR: [`docs/adr/0010-m3-game-termination-and-victory-determination.md`](../../../../adr/0010-m3-game-termination-and-victory-determination.md) §5
- 前提モデル: [`../../../domain/game/game-outcome.md`](../../../domain/game/game-outcome.md)
- 関連: [`../victory-conditions.md`](../victory-conditions.md)(勝利条件全体)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EarlyWinTriggerEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzVictoryConstants.cs`
- テスト(本 PR): `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/EarlyWinTriggerEffectTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-183 | `Given_夜_持ち点100_..._Then_OutcomeがWinnerOutcomeになる` / `..._持ち点100超過_..._Then_OutcomeがWinnerOutcomeになる` | 閾値境界 + 超過 |
| DZ-184 | `Given_朝_持ち点100_..._Then_Outcomeは設定されない` | 朝の no-op |
| DZ-185 | `Given_夜_持ち点99_..._Then_Outcomeは設定されない` | 閾値未満 |
| DZ-186 | `Given_既にWinnerOutcome設定済_When_EarlyWinTriggerをApply_Then_新規WinnerOutcomeで上書きされる` | 再評価冪等([Optional])|
