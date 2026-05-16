# PileJsonConverter

## 概要

`Drowsy.Infrastructure.Persistence.Converters.PileJsonConverter` は `Drowsy.Domain.Cards.Pile`(`sealed class : IEquatable<Pile>`、`Pile(IEnumerable<CardId>)` ctor)を `CardId` 配列として JSON に serialize / deserialize する責務を持つ。

`HandJsonConverter` と対称設計だが、`Pile` は重複許容(同じ `CardId` を複数枚保持可能)+ 順序保持シーケンスとして扱う。`[ "01#0", "02#0", "01#0" ]` 形式で round-trip 可能。

B-5 第 1 弾(Infrastructure カバレッジ補完、2026-05-16)で単体テストを追加。

## 普遍要件 (Ubiquitous)

- [INF-108] [Ubiquitous] The `PileJsonConverter` shall serialize `Pile` as a JSON array of `CardId` strings using the registered `CardIdJsonConverter`, preserving order.

## 事象駆動要件 (Event-driven)

- [INF-109] When a `Pile` instance with multiple cards is serialized and then deserialized through `JsonConvert.SerializeObject` / `DeserializeObject<Pile>` using `DrowZzzJsonSettings.Create()`, the resulting `Pile` shall equal the original via value equality(順序保持 + 全要素一致)。
- [INF-110] When an empty `Pile`(`Pile.Empty`)is serialized, the resulting JSON shall be `[]`(空配列)and round-trip back to `Pile.Empty`.
- [INF-111] When a JSON literal `null` is deserialized as `Pile`, the converter shall return `null`.

## 異常要件 (Unwanted)

- [INF-112] If the deserialized JSON value is not an array token (e.g. string / number / object), the converter shall throw `JsonSerializationException` referencing the current token type.
- [INF-113] If the deserialized JSON array contains an invalid `CardId` schema string, the converter shall propagate the underlying `JsonSerializationException` from `CardIdJsonConverter`.
- [INF-114] When the deserialized JSON array contains duplicate `CardId` entries, the converter shall **succeed**(`Pile` は重複許容、`Hand` との対称差)。

## 定数依存

(なし、`CardId` 配列の serialize / deserialize 純粋変換)

## Implementation Notes

- `null` token(`JsonToken.Null`)は throw せず `null` を返す経路を維持(`WriteJson` の `WriteNull` と対称、INF-111)
- `Pile` は重複許容、`Hand` との設計差を INF-114 で明示テスト化(`["01#0","01#0"]` が成功する)
- `serializer.Deserialize<List<CardId>>(reader)` 経由で各 `CardId` を `CardIdJsonConverter` で復元する

## 関連

- 実装: `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/PileJsonConverter.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/PileJsonConverterTests.cs`
- 対称 converter: [`hand-json-converter.md`](hand-json-converter.md)(重複禁止が違い)
- 関連 spec: [`card-id-json-converter.md`](card-id-json-converter.md)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-108 | (テスト免除: Ubiquitous) | round-trip + 順序保持は INF-109 で実質検証 |
| INF-109 | `Given_3枚のPile_When_RoundTrip_Then_等価かつ順序保持` | 順序保持 + 全要素一致 |
| INF-110 | `Given_emptyPile_When_RoundTrip_Then_等価` | `Pile.Empty` 経路 |
| INF-111 | `Given_JsonNullToken_When_Deserialize_Then_nullを返す` + `Given_nullPile_When_Serialize_Then_nullリテラル` | 対称性検証 |
| INF-112 | `Given_非array_token_When_Deserialize_Then_JsonSerializationException`(TestCase 3 件)| |
| INF-113 | `Given_不正CardId_When_Deserialize_Then_JsonSerializationException` | `CardIdJsonConverter` 透過 |
| INF-114 | `Given_重複CardId配列_When_Deserialize_Then_成功(重複保持)` | `Pile` は重複許容、`Hand` との対称差 |
