# `UsageRestrictionMarkerEffect`(M3-PR6)

ADR-0011 §6 で確定した「夢」カードの **連想後使用制限マーカー効果**(本 PR で新設、JIT 確定 2026-05-14)。**2 役を兼ねるマーカー**:

1. **カードの効果列内に存在する場合**(=「夢」のカードデータ):`AssociateAction.Apply` で検出 → 自プレイヤーに `PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, RemainingCount=1)` を付与する trigger marker として動作
2. **`PlayerInfluence.TickEffect` として保有される場合**(=連想で付与された Influence の中身):`DrowZzzRule.IsLegalPlayCard` で「該当カードの使用を illegal にする」フラグとして動作 + Tick 時は `EffectInterpreter.Apply` で no-op(`RemainingCount` 1→0 で除去)

## 構造

フィールドなしのマーカー的 `sealed record`(`AssociatableMarkerEffect` と同パターン)。

```csharp
public sealed record UsageRestrictionMarkerEffect : IEffect;
```

## 評価ロジック(`EffectInterpreter.Apply`)

| 条件 | 結果 |
| ---- | ---- |
| 任意の `session` | **no-op**(`session` 不変返却) |

本 effect は判別用マーカーで、評価時に状態を変えない。Tick 時(`PlayerInfluence.TickEffect` 経由)も no-op で、`RemainingCount` の減少と影響除去は `DrowZzzRule.TickInfluences`(M2-PR5 で確立)が担当する。

## 「2 役の semantics」具体例

### (1) 効果列内のマーカー → AssociateAction.Apply の trigger

「夢」のカードデータ:

```csharp
new IEffect[]
{
    new AssociatableMarkerEffect(),
    new RequiresMinimumTotalPointsMarkerEffect(100),
    new UsageRestrictionMarkerEffect(),  // ← (1) 本 marker (効果列内)
    new TimeOfDayBranchEffect(...),
}
```

`AssociateAction(CardId.Of("00"))` を Apply する際、`DrowZzzRule.ApplyAssociate` が `_catalog.GetEffects(action.Card)` の最上位 scan で `UsageRestrictionMarkerEffect` を検出 → 自プレイヤーの `Influences` 末尾に `PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, 1)` を付与。

### (2) PlayerInfluence.TickEffect → 使用制限フラグ

連想で付与された Influence(`PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, 1)`)が保有されている間、`DrowZzzRule.IsLegalPlayCard` は対象カードの最上位効果列に `UsageRestrictionMarkerEffect` が含まれているかを scan → 含まれていて、かつ自プレイヤーの Influences のいずれかの `TickEffect` も `UsageRestrictionMarkerEffect` だった場合は `false` を返す。

## 検出 walk のスコープ(本 PR JIT 確定 2026-05-14)

- **最上位の effect 列のみ scan**(`RequiresMinimumTotalPointsMarkerEffect` と同じスコープ)
- 「夢」では本 marker を最上位に置く設計(連想で引いた瞬間に画一的に Influence 付与、夜・朝で挙動が変わらない)

## 普遍要件 (Ubiquitous)

- [DZ-243] [Ubiquitous] `UsageRestrictionMarkerEffect` shall be a `sealed record` with no fields, implementing `IEffect`, declared in `Drowsy.Application.Games.DrowZzz.Effects` namespace.

## 構造要件

- [DZ-244] Two distinct instances of `UsageRestrictionMarkerEffect`(constructed independently)shall be value-equal under `record` auto-generated equality(`a == b` and `a.Equals(b)` shall be `true`)。

## 評価要件

- [DZ-245] When `UsageRestrictionMarkerEffect` is evaluated by `EffectInterpreter.Apply`(both as a direct top-level effect and as `PlayerInfluence.TickEffect` during the Tick step), the resulting session shall be value-equal to the input session(no-op、Tick 時の `RemainingCount` 減少 + 0 到達除去は `DrowZzzRule.TickInfluences` の責務で本 effect は session を変えない)。

## 「使用制限の検出」の実装場所

- `DrowZzzRule.ApplyAssociate` で対象カードの効果列を最上位 scan し、本 record が含まれていれば自プレイヤーの Influences 末尾に `PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, 1)` を付与
- `DrowZzzRule.IsLegalPlayCard` で対象カードの効果列を最上位 scan し、本 record が含まれていて、かつ自プレイヤーの Influences のいずれかが本 record を `TickEffect` として持つ場合 `false` を返す
- `DrowZzzRule.TickInfluences`(M2-PR5 で確立)の挙動は無改修:Tick で `RemainingCount` を 1 減算し 0 で除去するパスにそのまま乗る(本 effect の `EffectInterpreter.Apply` が no-op なので Tick 時の session 変動は `RemainingCount` 減算のみ)

## 定数依存

なし(本 effect 自身は値を保持しない、`RemainingCount` の初期値 1 は `ApplyAssociate` 側でハードコード:JIT 確定「次の自分のターン以降」= 1 フェーズ分の待機、ADR-0011 §6)。

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §6
- 関連: [`associatable-marker-effect.md`](associatable-marker-effect.md)(同じくマーカー的役割、ただし「連想可能カード」識別)/ [`requires-minimum-total-points-marker.md`](requires-minimum-total-points-marker.md)(同 PR で新設、使用条件チェック)/ [`../influences/influence-model.md`](../influences/influence-model.md)(`PlayerInfluence` の Tick 機構、M2-PR5)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/UsageRestrictionMarkerEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加、no-op)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyAssociate` 拡張で Influence 付与 + `IsLegalPlayCard` 拡張で walk 検出)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/UsageRestrictionMarkerEffectTests.cs`(record 構造 + interpreter no-op)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DreamCardTests.cs`(2 役の動作は夢カード統合テスト DZ-230 / DZ-231 / DZ-232 でカバー)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-243 | (テスト免除: Ubiquitous) | sealed record / フィールドなし構造 |
| DZ-244 | `Given_異なる2インスタンス_When_Equals_Then_true` | record 値同値性 |
| DZ-245 | `Given_任意session_When_UsageRestrictionMarkerEffectをApply_Then_session不変`(直接 Apply パス、本 effect が `EffectInterpreter` 経由で no-op であることを確認) | Tick 経由パスの session 不変性は DZ-232 統合テスト(`DreamCardTests`)+ M2-PR5 で確立した Tick 経路独立テストでカバー。本単体テストでは直接 Apply パス 1 件のみ(M3-PR6 code-reviewer W-2 反映 2026-05-14) |
