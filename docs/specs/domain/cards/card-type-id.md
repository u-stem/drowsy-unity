# CardTypeId(カード種別 ID)

このファイルは `CardTypeId` の不変条件と防御を EARS で記述する(ADR-0018 で新設)。

配置先: `docs/specs/domain/cards/card-type-id.md`

---

## 概要

`Drowsy.Domain.Cards.CardTypeId` は **カードの種別(catalog の lookup key)** を表す `sealed record`。
業界デファクト(Card Type / Card Instance 分離)に沿い、カードデータ(`CardData`)を catalog 引きするためのキー型として使う。
インスタンス側の ID は `CardId(CardTypeId TypeId, int Instance)` が表現する(別 spec `card-id.md`)。

## 普遍要件 (Ubiquitous)

- [CTYPE-001] [Ubiquitous] The `CardTypeId` shall be a `sealed record` holding a `string Value` as a `get`-only property.

## 異常要件 (Unwanted)

- [CTYPE-002] If `Of(value)` is called with `null`, an empty string, or a whitespace-only string, then `ArgumentException` shall be thrown.
- [CTYPE-005] If `Of(value)` is called with a string containing `'#'`, then `ArgumentException` shall be thrown(ADR-0018 §8 で予約済の `CardId.Value` 区切り文字、runtime 強制)。

## 事象駆動要件 (Event-driven)

- [CTYPE-003] When `Of(value)` is called with a non-blank `string`, the `CardTypeId` shall expose it via the `Value` property.

## 普遍要件 — 値同値 (Ubiquitous)

- [CTYPE-004] Two `CardTypeId` instances shall be equal iff their `Value` strings are equal (record auto-generated `Equals` / `GetHashCode`).

## 関連

- 確定 ADR: [ADR-0018 CardTypeId と CardId(instance)の分離](../../../adr/0018-cardtypeid-cardid-instance-separation.md)
- 関連 spec: `card-id.md`(同ディレクトリ、`CardId` の TypeId 部分を保持)
- 実装: `Assets/_Project/Scripts/Domain/Cards/CardTypeId.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Cards/CardTypeIdTests.cs`
- シナリオ: `card-type-id.feature`(同ディレクトリ)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| CTYPE-001 | (テスト免除: Ubiquitous) | `sealed record` 定義はコンパイル時保証 |
| CTYPE-002 | `Given_null_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる` / `Given_空文字列_..._ArgumentException` / `Given_空白のみの文字列_..._ArgumentException` | Abnormal(null / 空 / 空白の 3 ケース網羅) |
| CTYPE-005 | `Given_シャープを含む文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる` | Abnormal(`#` は ADR-0018 §8 で予約済) |
| CTYPE-003 | `Given_非空のstring_When_Ofを呼ぶ_Then_Valueに保持される` | Normal |
| CTYPE-004 | `Given_同じValueの2つのCardTypeId_When_等価比較_Then_等価` / `Given_異なるValueの2つのCardTypeId_When_等価比較_Then_非等価` | Normal(record auto-equals 検証) |
