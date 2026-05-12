# `RequiresMinimumTotalPointsMarkerEffect`(M3-PR6)

ADR-0011 §6 で確定した「夢」カードの **使用条件マーカー効果**(本 PR で新設、JIT 確定 2026-05-14)。カードの効果列に本 effect を持つと、当該カードの `PlayCardAction` は現プレイヤーの `TotalPoints` が `Threshold` 以上でないと `IsLegalMove` が `false` を返す(使用条件チェック)。

## 構造

`int Threshold` を持つ最小 marker `sealed record`。本 record 自身は評価時に副作用を持たない(`AssociatableMarkerEffect` と同パターン、判別用)。

```csharp
public sealed record RequiresMinimumTotalPointsMarkerEffect(int Threshold) : IEffect;
```

`Threshold` は **1 以上** 必須(0 / 負値は意味なし、構築時 `ArgumentOutOfRangeException`)。

## 評価ロジック(`EffectInterpreter.Apply`)

| 条件 | 結果 |
| ---- | ---- |
| 任意の `session` | **no-op**(`session` 不変返却) |

本 effect は判別用マーカーで、評価時に状態を変えない。使用条件チェックは `DrowZzzRule.IsLegalPlayCard` で `_catalog.GetEffects(action.Card)` を最上位 scan して本 record を検出 → `session.TotalPoints(currentPlayer) >= Threshold` を判定する(`AssociatableMarkerEffect` を `IsLegalAssociate` で検出する経路と同パターン)。

## 検出 walk のスコープ(本 PR JIT 確定 2026-05-14)

- **最上位の effect 列のみ scan**(`TimeOfDayBranchEffect` / `KeywordedEffect` / `ChoiceEffect` の inner には walk しない)
- 「夢」では本 marker を最上位に置く設計(夜・朝で使用条件が変わらない単一閾値、ADR-0011 §6)
- 将来「夜だけ FDS ≥ N 要件」のような nested 配置が必要なカードが出てきた時点で walk を再帰化する(`HasKeywordInEffect` 再帰 walk と同パターン、ADR-0011 §4.2 / M3-PR5a 確立)

## 普遍要件 (Ubiquitous)

- [DZ-239] [Ubiquitous] `RequiresMinimumTotalPointsMarkerEffect` shall be a `sealed record` with a single `int Threshold` field, implementing `IEffect`, declared in `Drowsy.Application.Games.DrowZzz.Effects` namespace.

## 構造要件

- [DZ-240] When `RequiresMinimumTotalPointsMarkerEffect(threshold)` is constructed with `threshold <= 0`, the constructor shall throw `ArgumentOutOfRangeException`(0 / 負値は使用条件として無意味)。
- [DZ-241] Two instances of `RequiresMinimumTotalPointsMarkerEffect` with the same `Threshold` shall be value-equal under `record` auto-generated equality(`a == b` and `a.Equals(b)` shall be `true`)。

## 評価要件

- [DZ-242] When `RequiresMinimumTotalPointsMarkerEffect` is evaluated by `EffectInterpreter.Apply`, the resulting session shall be value-equal to the input session(no-op)。

## 「使用条件チェック」の実装場所

`DrowZzzRule.IsLegalPlayCard` で対象カードの効果列を最上位 scan し、本 record が含まれていれば `session.TotalPoints(currentPlayer) < Threshold` の場合 `false` を返す(ADR-0011 §6 / 本 PR 新設)。

## 定数依存

なし(本 effect 自身は数値定数を保持しないが、「夢」カード仕様 md の `Threshold` 値は `DrowZzzVictoryConstants.EarlyWinScoreThreshold = 100` を再利用する設計、ADR-0011 §6)。

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §6
- 関連: [`associatable-marker-effect.md`](associatable-marker-effect.md)(同じくマーカー的役割、ただし「連想可能カード」識別)/ [`usage-restriction-marker.md`](usage-restriction-marker.md)(同 PR で新設、連想後の使用制限)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RequiresMinimumTotalPointsMarkerEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加、no-op)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalPlayCard` 拡張で walk 検出)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/RequiresMinimumTotalPointsMarkerEffectTests.cs`(record 構造 + interpreter no-op)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DreamCardTests.cs`(IsLegalPlayCard 経由の walk 検出は夢カード統合テスト DZ-233 / DZ-234 でカバー)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-239 | (テスト免除: Ubiquitous) | sealed record / 単一フィールド構造 |
| DZ-240 | `Given_Threshold_0または負値_When_RequiresMinimumTotalPointsMarkerEffectをコンストラクト_Then_ArgumentOutOfRangeException` | 構築防御(0 / -1 の 2 ケース) |
| DZ-241 | `Given_同一Thresholdの2インスタンス_When_Equals_Then_true` + `Given_異なるThreshold_When_Equals_Then_false` | record 値同値性 |
| DZ-242 | `Given_任意session_When_RequiresMinimumTotalPointsMarkerEffectをEffectInterpreterで評価_Then_session不変` | no-op 評価 |
