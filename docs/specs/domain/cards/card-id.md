# CardId

カードの一意識別子を表す不変値オブジェクト。

## 概要

`CardId` は string をラップした値オブジェクトで、空白文字列・null での生成を禁止する。`record` による値同等性を持つ。

## 普遍要件 (Ubiquitous)

- The `CardId` shall be immutable.
- The `CardId` shall expose its underlying `Value` as a read-only string.
- Two `CardId` instances with the same `Value` shall be considered equal.

## 事象駆動要件 (Event-driven)

- When `CardId.Of(value)` is called with a non-empty, non-whitespace string, the `CardId` shall return a new instance whose `Value` equals the input.
- When `ToString()` is called, the `CardId` shall return its `Value`.

## 異常要件 (Unwanted)

- If `CardId.Of(null)` is called, then the `CardId` shall throw `ArgumentException`.
- If `CardId.Of("")` is called, then the `CardId` shall throw `ArgumentException`.
- If `CardId.Of("   ")` (whitespace only) is called, then the `CardId` shall throw `ArgumentException`.

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Cards/CardId.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Cards/CardIdTests.cs`
- シナリオ: `card-id.feature`
