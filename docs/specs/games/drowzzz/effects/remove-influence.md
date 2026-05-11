# `RemoveInfluenceEffect` (M2-PR5)

ADR-0007 §1.5「継続影響」JIT 確定の除去系効果。指定プレイヤーの影響リストから 1 件を `EffectContext.InfluenceRemovalIndex` 指定で除去する。

## 普遍要件 (Ubiquitous)

- [DZ-162.0] [Ubiquitous] `RemoveInfluenceEffect` shall be a `sealed record` with positional parameter `(SdpTarget Target)` and shall implement `IEffect`.

## Apply の挙動(index 範囲内)

- [DZ-162] When `RemoveInfluenceEffect(Target=Self)` is applied with `context.InfluenceRemovalIndex` within the current player's influence list range, the influence at that index shall be removed and trailing elements shall shift forward.

## Apply の挙動(graceful no-op)

- [DZ-163] When `RemoveInfluenceEffect` is applied to a target with an empty influence list, or when `context.InfluenceRemovalIndex` is out of range (negative or `>= count`), the session shall be returned unchanged (no exception, no mutation).

## Target 解決

- [DZ-164] When `RemoveInfluenceEffect(Target=Opponent)` is applied (N=2 想定), the opponent's influence list shall be the removal target.

## 他プレイヤーの影響保護

- [DZ-165] [Ubiquitous] `RemoveInfluenceEffect.Apply` shall not modify influences held by players other than the resolved target.

## 「プレイヤーが選択」セマンティクスとの関係

`InfluenceRemovalIndex` は `PlayCardAction.InfluenceRemovalIndex` から `EffectContext` 経由で transfer される(JIT 確定 2026-05-11、カードをプレイするプレイヤーが action 構築時に index を指定する)。範囲外を `IsLegalMove` で illegal-move 化せず graceful no-op とした理由は、影響リストの内容(件数)を action 構築側が知らない場合があるため(UI 側の制約は Presentation 層 M5 で実装する想定)。

## 関連

- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5
- 関連効果: [`./apply-influence.md`](./apply-influence.md)
- 利用カード: [`../cards/green-invasion.md`](../cards/green-invasion.md)
