# Pile(山札 / 順序ありカード列)

順序を持つカード集合の不変オブジェクト。山札・捨て札・場の基底として使う。

## 概要

`Pile` は `CardId` を順序付きで保持する Immutable コレクション。すべての変更操作は新インスタンスを返す純関数として定義される。内部実装は `CardId[]`(配列)を private で保持し、`IReadOnlyList<CardId>` 経由で読み取り専用公開する形を採る(Unity 6 では `System.Collections.Immutable` が internal アクセシビリティのため `ImmutableArray<T>` は利用不可)。

## 普遍要件 (Ubiquitous)

- The `Pile` shall be immutable.
- The `Pile` shall expose its cards via a read-only collection (`IReadOnlyList<CardId>`).
- The `Pile` shall expose `Count` and `IsEmpty` properties.
- The `Pile` shall expose a static `Empty` singleton representing an empty pile.

## 事象駆動要件 (Event-driven)

- When `AddTop(card)` is called, the `Pile` shall return a new `Pile` with the card inserted at index 0.
- When `AddBottom(card)` is called, the `Pile` shall return a new `Pile` with the card appended at the end.
- When `Draw()` is called on a non-empty `Pile`, the `Pile` shall return a tuple of the top card and the remaining `Pile`.
- When `Shuffle(rng)` is called, the `Pile` shall return a new `Pile` whose cards are a Fisher-Yates shuffled permutation determined by `rng`.

## 異常要件 (Unwanted)

- If `Draw()` is called on an empty `Pile`, then the `Pile` shall throw `InvalidOperationException`.
- If `AddTop(null)` or `AddBottom(null)` is called, then the `Pile` shall throw `ArgumentNullException`.
- If `Shuffle(null)` is called, then the `Pile` shall throw `ArgumentNullException`.
- If the constructor is called with `null`, then the `Pile` shall throw `ArgumentNullException`.

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Cards/Pile.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Cards/PileTests.cs`
- シナリオ: `pile.feature`
