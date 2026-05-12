# `ChoiceEffect` (M2-PR5)

ADR-0007 §1.5「継続影響」追記と同梱で導入された、選択式カードを表すラッパー record。プレイ時に `PlayCardAction.Choice` が指す index の効果列だけが適用される。

## 普遍要件 (Ubiquitous)

- [DZ-166] [Ubiquitous] `ChoiceEffect` shall be a `sealed record` exposing `IReadOnlyList<IReadOnlyList<IEffect>> Branches`, and shall implement `IEffect`.

## 構築時検証

- [DZ-167] When `ChoiceEffect` is constructed with `branches == null`, the constructor shall throw `ArgumentNullException`.
- [DZ-167] When `Branches.Count < 2` (選択肢が 2 つ未満)、the constructor shall throw `ArgumentException`.
- [DZ-167] When any inner `IReadOnlyList<IEffect>` is null, the constructor shall throw `ArgumentNullException`.
- [DZ-167] When any inner list contains a null `IEffect` element, the constructor shall throw `ArgumentException`.

## 値同値性

- [DZ-168] [Ubiquitous] `ChoiceEffect` shall override `Equals` / `GetHashCode` for sequence-equality of `Branches`(外側 / 内側ともに順序保持)。auto-equals は内部 `IReadOnlyList` を持つため参照同値にフォールバックし値同値を壊すため。

## Unwrap タイミング

- [DZ-169] When a `ChoiceEffect` is passed directly to `EffectInterpreter.Apply`, the interpreter shall throw `NotImplementedException`. ChoiceEffect は `DrowZzzRule.ApplyPlayCard` 内で `PlayCardAction.Choice` を読んで unwrap される設計のため、interpreter には届かない。

## 関連

- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5
- 利用カード: [`../cards/green-invasion.md`](../cards/green-invasion.md)
- 対称設計: [`./time-of-day-branch.md`](./time-of-day-branch.md)(session 状態由来の分岐、interpreter 内 unwrap)
