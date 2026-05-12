# `AssociatableMarkerEffect`(M3-PR4)

ADR-0011 §1 で新設された **連想可能マーカー効果**。本効果を効果列に持つカードを「**連想可能カード**」と定義する(ADR-0011 §1 / ADR-0007 §1「カード効果は `IEffect` で表現」と整合、`EarlyWinTriggerEffect` による「就寝カード」識別と同パターン)。

## 構造

フィールドなしのマーカー的 `sealed record`。本 record 自身は閾値・対象等の状態を持たない(連想の閾値は `DrowZzzAssociationConstants.AssociationThreshold` で別途集約、ADR-0011 §1 / CLAUDE.md §9)。

```csharp
public sealed record AssociatableMarkerEffect : IEffect;
```

## 評価ロジック(`EffectInterpreter.Apply`)

| 条件 | 結果 |
| ---- | ---- |
| 任意の `session`(`Clock` / `Outcome` / `PhaseState` / DP / Hand に依存しない)| **no-op**(`session` 不変返却)|

本 effect は判別用マーカーであり、評価時に状態を変えない。連想で手札に追加する動作は `AssociateAction` の `Apply` 経路で行う(`DrowZzzRule.ApplyAssociate`)。`EffectInterpreter` の switch case には登録されているが、これは「将来 `_` ケースで `NotImplementedException` を投げる safety net に引っかからないようにする」ためであり、副作用は持たない(`EarlyWinTriggerEffect` も条件不成立時は no-op、本 effect は **常時 no-op**)。

## 普遍要件 (Ubiquitous)

- [DZ-207.0] [Ubiquitous] `AssociatableMarkerEffect` shall be a `sealed record` with no fields, implementing `IEffect`, declared in `Drowsy.Application.Games.DrowZzz.Effects` namespace.

## 構造要件(record 値同値性)

- [DZ-207] Two distinct instances of `AssociatableMarkerEffect`(constructed independently)shall be value-equal under `record` auto-generated equality(`a == b` and `a.Equals(b)` shall be `true`)。

## 評価要件

- [DZ-208] When `AssociatableMarkerEffect` is evaluated by `EffectInterpreter.Apply`, the resulting session shall be value-equal to the input session(no-op)。

## 「連想可能カード」の暗黙定義

`AssociatableMarkerEffect` を効果列に持つカードを「連想可能カード」と呼ぶ(ADR-0011 §1)。最初の利用カード(連想可能カード No.X)は本 PR では確定せず、M2-PR6+ で JIT 共有される運用(ADR-0011 §1 Neutral / ADR-0010 §「Implementation Notes」と同パターン)。「夢」カード(No.00)は ADR-0011 §6 で M3-PR6 に予定。

## 定数依存

なし(本 effect 自身は数値定数を保持しない。連想の閾値は `DrowZzzAssociationConstants.AssociationThreshold` を `DrowZzzRule.IsLegalAssociate` 内で参照する設計)。

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §1
- 関連: [`../association.md`](../association.md)(連想機構)/ [`early-win-trigger.md`](early-win-trigger.md)(同じくマーカー的役割、「就寝カード」識別)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AssociatableMarkerEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加、no-op)
- テスト(本 PR): `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/AssociatableMarkerEffectTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-207 | `Given_2つの異なるインスタンス_When_等値比較_Then_true` | record 値同値性 |
| DZ-208 | `Apply` no-op 関連 3 件(デフォルト / FDP+SDP+ベッド多様 / Outcome 設定済)| 評価で session 不変 |
