# 継続影響(Influence)モデル (M2-PR5)

ADR-0007 §1.5「継続影響」JIT 確定の核心モデル。プレイヤーがカード効果で受け取る「継続して特定タイミングで発動する効果」を表す値オブジェクト + 状態保持機構。

## 用語規約

ADR-0009「用語規約」と整合:

| 単位 | 用語 | 実装 |
| ---- | ---- | ---- |
| 大単位 | **ターン** | `Clock.RoundNumber`(30 分 = 全プレイヤー 1 巡)|
| 中単位 | **フェーズ** | `TurnState.TurnNumber`(1 プレイヤー 1 行動)|
| 小単位 | **PhaseState** | `DrowZzzPhaseState`(WaitingForDraw / WaitingForPlay / WaitingForEndTurn)|

カードフレーバー上「自分のターン開始時」と書かれていても、実装上は「影響保有者の **フェーズ** 開始時」(= `InfluenceTrigger.OwnPhaseStart`)である。

## 普遍要件 (Ubiquitous)

- [DZ-155] [Ubiquitous] `PlayerInfluence` shall be a `sealed record` with positional parameters `(InfluenceTrigger Trigger, IEffect TickEffect, int RemainingCount)`.
- [DZ-156] When `PlayerInfluence` is constructed with `TickEffect == null`, the constructor shall throw `ArgumentNullException`; when `RemainingCount < 1`, it shall throw `ArgumentOutOfRangeException`. Both checks shall apply on `with` mutation as well (double-guard pattern).
- [DZ-157] [Ubiquitous] `PlayerInfluence` value equality shall hold by per-field comparison (Trigger / TickEffect / RemainingCount), inherited from record auto-generated `Equals` (no internal collection field).

## 影響の保有と状態遷移

- [DZ-179] [Ubiquitous] `DrowZzzGameSession.Influences` shall be `IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerInfluence>>` whose key set equals `GameState.Players` PlayerId set (cross-field invariant, same pattern as FDP / DDP / SDP).
- [DZ-180] [Ubiquitous] `StartGameUseCase.Execute` shall initialize `Influences` with an empty `IReadOnlyList<PlayerInfluence>` per player (no influences at game start).

## トリガー評価(自フェーズ開始時)

- [DZ-181] [Ubiquitous] When `EndTurnAction` is applied and the resulting phase change makes a new player the current player, each `PlayerInfluence` with `Trigger == OwnPhaseStart` held by the new current player shall be evaluated in list order (FIFO):
  - The `TickEffect` shall be applied via `EffectInterpreter.Apply(session, effect, EffectContext.Default)`.
  - The influence's `RemainingCount` shall be decremented by 1.
  - When `RemainingCount` reaches 0, the influence shall be removed from the player's list.
  - When `RemainingCount` is 1 or more after decrement, the influence shall be replaced in place with a new `PlayerInfluence` instance.
  - 構造的性質として `[Ubiquitous]` マーカー(DZ-176 / DZ-177 統合テストでカード単位の動作実証 + DrowZzzRule.TickInfluencesForCurrentPlayer 実装で構造保証)。
- [DZ-182] [Ubiquitous] Influences held by players other than the new current player shall not be evaluated by the EndTurn tick (other-player influence is not ticked). 構造的性質として `[Ubiquitous]` マーカー(DZ-178 統合テストでカード単位の動作実証)。

## 定数依存

該当なし。`RemainingCount >= 1` は値オブジェクトの不変条件として `PlayerInfluence` 自体に内包される。

## 関連

- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」
- 用語規約: [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../../adr/0009-m2-m3-dp-and-victory-conditions.md) §「用語規約」
- 関連効果: [`../effects/apply-influence.md`](../effects/apply-influence.md) / [`../effects/remove-influence.md`](../effects/remove-influence.md)
- 関連カード: [`../cards/green-invasion.md`](../cards/green-invasion.md)
