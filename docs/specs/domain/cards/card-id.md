# CardId

カードの一意識別子を表す不変値オブジェクト。

## 概要

`CardId` は string をラップした値オブジェクトで、空白文字列・null での生成を禁止する。`record` による値同等性を持つ。

## 普遍要件 (Ubiquitous)

- [CARD-001] [Ubiquitous] The `CardId` shall be immutable.
- [CARD-002] [Ubiquitous] The `CardId` shall expose its underlying `Value` as a read-only string.
- [CARD-003] Two `CardId` instances with the same `Value` shall be considered equal.

## 事象駆動要件 (Event-driven)

- [CARD-004] When `CardId.Of(value)` is called with a non-empty, non-whitespace string, the `CardId` shall return a new instance whose `Value` equals the input.
- [CARD-005] When `ToString()` is called, the `CardId` shall return its `Value`.

## 異常要件 (Unwanted)

- [CARD-006] If `CardId.Of(null)` is called, then the `CardId` shall throw `ArgumentException`.
- [CARD-007] If `CardId.Of("")` is called, then the `CardId` shall throw `ArgumentException`.
- [CARD-008] If `CardId.Of("   ")` (whitespace only) is called, then the `CardId` shall throw `ArgumentException`.

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Cards/CardId.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Cards/CardIdTests.cs`
- シナリオ: `card-id.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| CARD-001 | (テスト免除: Ubiquitous) | `sealed record` で構造的に保証 |
| CARD-002 | (テスト免除: Ubiquitous) | `Value { get; }` で構造的に保証 |
| CARD-003 | `Given_同じ値の2つのCardId_When_等価比較_Then_等価` / `Given_異なる値の2つのCardId_When_等価比較_Then_非等価` | record の自動生成等価性 |
| CARD-004 | `Given_有効な値_When_Ofを呼ぶ_Then_インスタンスが生成され_Valueは入力と同じ` | |
| CARD-005 | `Given_CardId_When_ToString_Then_Valueを返す` | |
| CARD-006 | `Given_null_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる` | |
| CARD-007 | `Given_空文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる` | |
| CARD-008 | `Given_空白のみの文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる` | |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
