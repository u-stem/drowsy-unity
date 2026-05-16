# HandJsonConverter

## 概要

`Drowsy.Infrastructure.Persistence.Converters.HandJsonConverter` は `Drowsy.Domain.Cards.Hand`(`sealed class : IEquatable<Hand>`、`Hand(IEnumerable<CardId>)` ctor)を `CardId` 配列として JSON に serialize / deserialize する責務を持つ。

`PileJsonConverter` と対称設計。`Hand` は順序保持 + 重複禁止の制約を持つが、serialize 順は `Hand.Cards`(防御コピー後の配列)と一致するため `["01#0", "02#0"]` 形式で round-trip 可能。重複検出は ctor 側 `Hand(IEnumerable<CardId>)` が `ArgumentException` で担う。

B-5 第 1 弾(Infrastructure カバレッジ補完、2026-05-16)で単体テストを追加。

## 普遍要件 (Ubiquitous)

- [INF-101] [Ubiquitous] The `HandJsonConverter` shall serialize `Hand` as a JSON array of `CardId` strings using the registered `CardIdJsonConverter`, preserving order.

## 事象駆動要件 (Event-driven)

- [INF-102] When a `Hand` instance with multiple cards is serialized and then deserialized through `JsonConvert.SerializeObject` / `DeserializeObject<Hand>` using `DrowZzzJsonSettings.Create()`, the resulting `Hand` shall equal the original via value equality(順序保持 + 全要素一致)。
- [INF-103] When an empty `Hand`(`Hand.Empty`)is serialized, the resulting JSON shall be `[]`(空配列)and round-trip back to `Hand.Empty`.
- [INF-104] When a JSON literal `null` is deserialized as `Hand`, the converter shall return `null`.

## 異常要件 (Unwanted)

- [INF-105] If the deserialized JSON value is not an array token (e.g. string / number / object), the converter shall throw `JsonSerializationException` referencing the current token type.
- [INF-106] If the deserialized JSON array contains duplicate `CardId` instances, the converter shall throw `ArgumentException`(`Hand(IEnumerable<CardId>)` ctor の重複検出による透過)。
- [INF-107] If the deserialized JSON array contains an invalid `CardId` schema string(`PlayerIdJsonConverter` と同様、`CardIdJsonConverter` 経路で `JsonSerializationException`)、the converter shall propagate the underlying `JsonSerializationException`.

## 定数依存

(なし、`CardId` 配列の serialize / deserialize 純粋変換)

## Implementation Notes

- `null` token(`JsonToken.Null`)は throw せず `null` を返す経路を維持(`WriteJson` の `WriteNull` と対称、INF-104)
- 重複検出は `Hand(IEnumerable<CardId>)` ctor が担うため、本 converter は wrap せず `ArgumentException` を透過
- `serializer.Deserialize<List<CardId>>(reader)` 経由で各 `CardId` を `CardIdJsonConverter` で復元する(ADR-0018 schema 違反は `JsonSerializationException` として透過)

## 関連

- 実装: `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/HandJsonConverter.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/HandJsonConverterTests.cs`
- 対称 converter: [`pile-json-converter.md`](pile-json-converter.md)
- 関連 spec: [`card-id-json-converter.md`](card-id-json-converter.md)(配列要素の converter)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-101 | (テスト免除: Ubiquitous) | round-trip + 順序保持は INF-102 で実質検証 |
| INF-102 | `Given_3枚のHand_When_RoundTrip_Then_等価かつ順序保持` | 順序保持 + 全要素一致 |
| INF-103 | `Given_emptyHand_When_RoundTrip_Then_等価` | `Hand.Empty` 経路 |
| INF-104 | `Given_JsonNullToken_When_Deserialize_Then_nullを返す` + `Given_nullHand_When_Serialize_Then_nullリテラル` | 対称性検証 |
| INF-105 | `Given_非array_token_When_Deserialize_Then_JsonSerializationException`(TestCase 3 件:string / number / object)| |
| INF-106 | `Given_重複CardId配列_When_Deserialize_Then_ArgumentException` | `Hand` ctor の重複検出透過 |
| INF-107 | `Given_不正CardId_When_Deserialize_Then_JsonSerializationException` | `CardIdJsonConverter` の schema 違反透過 |
