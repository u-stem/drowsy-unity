# `ApplyInfluenceEffect` (M2-PR5)

ADR-0007 §1.5「継続影響」JIT 確定の付与系効果。指定プレイヤー(Self / Opponent)の影響リストに `PlayerInfluence` を 1 件追加する。

## 普遍要件 (Ubiquitous)

- [DZ-158] [Ubiquitous] `ApplyInfluenceEffect` shall be a `sealed record` with positional parameters `(SdpTarget Target, PlayerInfluence Influence)` and shall implement `IEffect`.
- [DZ-158] When constructed with `Influence == null`, the constructor shall throw `ArgumentNullException` (double-guard pattern on `with` mutation).

## Apply の挙動

- [DZ-159] When `ApplyInfluenceEffect(Target=Self, influence)` is applied to a session, the current player's influence list shall gain `influence` appended at the end (FIFO order preserved).
- [DZ-161] When `ApplyInfluenceEffect(Target=Opponent, influence)` is applied, the opponent's influence list (N=2 で「現プレイヤー以外」) shall gain `influence` appended at the end.

## 重複付与

- [DZ-160] When the same `Influence` value is applied twice via `ApplyInfluenceEffect`, the target player shall hold 2 independent instances (each with its own `RemainingCount` tick lifecycle).

## 関連

- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5
- モデル: [`../influences/influence-model.md`](../influences/influence-model.md)
- 関連効果: [`./remove-influence.md`](./remove-influence.md)
- 利用カード: [`../cards/green-invasion.md`](../cards/green-invasion.md)
