# Pile(山札 / 順序ありカード列)

順序を持つカード集合の不変オブジェクト。山札・捨て札・場の基底として使う。

## 概要

`Pile` は `CardId` を順序付きで保持する Immutable コレクション。すべての変更操作は新インスタンスを返す純関数として定義される。内部実装は `CardId[]`(配列)を private で保持し、`IReadOnlyList<CardId>` 経由で読み取り専用公開する形を採る(Unity 6 では `System.Collections.Immutable` が internal アクセシビリティのため `ImmutableArray<T>` は利用不可)。

`record` ではなく `sealed class` として実装し、`Equals` / `GetHashCode` / `operator==` / `operator!=` を **順序付きシーケンス同値** で override する(設計判断は ADR-0002 §「Domain 集合型の値同値性方針」、Hand と同じパターン)。

## 普遍要件 (Ubiquitous)

- [PILE-001] [Ubiquitous] The `Pile` shall be immutable.
- [PILE-002] [Ubiquitous] The `Pile` shall expose its cards via a read-only collection (`IReadOnlyList<CardId>`).
- [PILE-003] [Ubiquitous] The `Pile` shall expose `Count` and `IsEmpty` properties.
- [PILE-004] [Ubiquitous] The `Pile` shall expose a static `Empty` singleton representing an empty pile.

## 事象駆動要件 (Event-driven)

- [PILE-005] When `AddTop(card)` is called, the `Pile` shall return a new `Pile` with the card inserted at index 0.
- [PILE-006] When `AddBottom(card)` is called, the `Pile` shall return a new `Pile` with the card appended at the end.
- [PILE-007] When `Draw()` is called on a non-empty `Pile`, the `Pile` shall return a tuple of the top card and the remaining `Pile`.
- [PILE-008] When `Shuffle(rng)` is called, the `Pile` shall return a new `Pile` whose cards are a Fisher-Yates shuffled permutation determined by `rng`.
- [PILE-014] When two `Pile` instances are compared with `Equals`, they shall be equal if and only if their card sequences are equal in order and length.
- [PILE-015] When `GetHashCode` is called on two equal `Pile` instances, they shall return the same hash value.
- [PILE-016] When two `Pile` references are compared with `operator==` or `operator!=`, the result shall be value-equal consistent with `Equals`. Both null operands compare as equal; one-sided null compares as not equal.
- [PILE-017] When `Equals(object)` is called with `null` or an argument that is not a `Pile`, the `Pile` shall return `false`.

## 異常要件 (Unwanted)

- [PILE-009] If `Draw()` is called on an empty `Pile`, then the `Pile` shall throw `InvalidOperationException`.
- [PILE-010] If `AddTop(null)` is called, then the `Pile` shall throw `ArgumentNullException`.
- [PILE-011] If `AddBottom(null)` is called, then the `Pile` shall throw `ArgumentNullException`.
- [PILE-012] If `Shuffle(null)` is called, then the `Pile` shall throw `ArgumentNullException`.
- [PILE-013] If the constructor is called with `null`, then the `Pile` shall throw `ArgumentNullException`.

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Cards/Pile.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Cards/PileTests.cs`
- シナリオ: `pile.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| PILE-001 | `Given_AddTop呼び出し_When_実行後_Then_元のPileは変更されない` | immutable 性は AddTop 経由で確認 |
| PILE-002 | (テスト免除: Ubiquitous) | `IReadOnlyList<CardId>` 戻り値で構造的に保証 |
| PILE-003 | `Given_Pile_Empty_When_IsEmptyとCountを確認_Then_trueと0` | |
| PILE-004 | `Given_Pile_Empty_When_IsEmptyとCountを確認_Then_trueと0` | static singleton 経由 |
| PILE-005 | `Given_空でない山札_When_AddTop_Then_先頭に挿入された新Pileを返す` | |
| PILE-006 | `Given_空でない山札_When_AddBottom_Then_末尾に追加された新Pileを返す` | |
| PILE-007 | `Given_空でない山札_When_Draw_Then_先頭カードと残りPileを返す` / `Given_1枚のみの山札_When_Draw_Then_引いたカードと空Pileを返す` | |
| PILE-008 | `Given_同じシードのRandom_When_Shuffle_Then_並びは決定的に同じ` / `Given_山札_When_Shuffle_Then_要素集合は元と同じ` | |
| PILE-009 | `Given_空の山札_When_Draw_Then_InvalidOperationExceptionを投げる` | |
| PILE-010 | `Given_AddTopにnull_When_実行_Then_ArgumentNullException` | |
| PILE-011 | `Given_AddBottomにnull_When_実行_Then_ArgumentNullException` | |
| PILE-012 | `Given_Shuffleにnull_When_実行_Then_ArgumentNullException` | |
| PILE-013 | `Given_コンストラクタにnull_When_生成_Then_ArgumentNullException` | |
| PILE-014 | `Given_同順序同要素のPile_..._Then_等価` / `Given_同枚数で異なる順序のPile_..._Then_非等価` / `Given_同枚数で異なるカードのPile_..._Then_非等価` / `Given_異なる枚数のPile_..._Then_非等価` / `Given_同一インスタンスのPile_..._Then_等価` / `Given_両方空Pile_..._Then_等価` / `Given_null_When_EqualsPile_Then_false` | n=0 / n=1 / n=2 のサイズ網羅 + 不一致 3 種(順序異 / カード異 / 枚数異)+ ReferenceEquals 短絡 + null 引数 |
| PILE-015 | `Given_等価な2つのPile_When_GetHashCode_Then_同じ値を返す` / `Given_両方空Pile_When_GetHashCode_Then_同じ値を返す` | n=0 と n>0 のハッシュ一致を網羅 |
| PILE-016 | `Given_等価な2つのPile_When_operator_等価_Then_true` / `Given_非等価な2つのPile_When_operator_等価_Then_false` / `Given_非等価な2つのPile_When_operator_非等価_Then_true` / `Given_両方null_When_operator_等価_Then_true` / `Given_片方nullで他方非null_When_operator_等価_Then_false` (left=null) / `Given_左側非nullで右側null_When_operator_等価_Then_false` (right=null) | == の true/false + != の true + 両 null + 片 null × 2 |
| PILE-017 | `Given_null_When_PileEqualsオブジェクト_Then_false` / `Given_異なる型_When_PileEqualsオブジェクト_Then_false` | |

## 実装メモ

### Hand との対称性

PILE-014〜017 は Hand-011〜014 と同じ「順序付きシーケンス同値」パターンで実装する。Hand と Pile のテストは構造が揃い、両クラス保守が一貫する(ADR-0002 §「Domain 集合型の値同値性方針」)。

### Equals / GetHashCode の実装方針

- `Equals(Pile)`: `null` チェック → `ReferenceEquals` 短絡 → `Length` 一致 → 要素を順次比較
- `Equals(object)`: `is Pile` パターンマッチで型不一致・null を `false` に帰着 (PILE-017)
- `GetHashCode`: `HashCode` struct (`new HashCode()`) に各 `CardId` を `.Add()` で順次合成し `.ToHashCode()` で値を取得(C# `System.HashCode` の標準パターン、Hand と完全同等)
- `operator==`: `left is null ? right is null : left.Equals(right)` で null 安全

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
