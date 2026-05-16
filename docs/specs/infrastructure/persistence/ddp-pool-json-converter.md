# DdpPoolJsonConverter

## 概要

`Drowsy.Infrastructure.Persistence.Converters.DdpPoolJsonConverter` は `Drowsy.Application.Games.DrowZzz.DdpPool`(`sealed class : IEquatable<DdpPool>`、`DdpPool(IEnumerable<int>)` ctor)を int 配列として JSON に serialize / deserialize する責務を持つ。

順序付きシーケンス同値で等価判定するため、配列順をそのまま保持。`[-3, 4, 7, -1]` 形式で round-trip 可能。

B-5 第 1 弾(Infrastructure カバレッジ補完、2026-05-16)で単体テストを追加。

## 普遍要件 (Ubiquitous)

- [INF-115] [Ubiquitous] The `DdpPoolJsonConverter` shall serialize `DdpPool` as a JSON array of int values, preserving order.

## 事象駆動要件 (Event-driven)

- [INF-116] When a `DdpPool` instance with multiple values(正 / 負 / 0 混在)is serialized and then deserialized through `JsonConvert.SerializeObject` / `DeserializeObject<DdpPool>` using `DrowZzzJsonSettings.Create()`, the resulting `DdpPool` shall equal the original via value equality(順序保持 + 全要素一致)。
- [INF-117] When an empty `DdpPool`(`DdpPool.Empty`)is serialized, the resulting JSON shall be `[]` and round-trip back to `DdpPool.Empty`.
- [INF-118] When a JSON literal `null` is deserialized as `DdpPool`, the converter shall return `null`.

## 異常要件 (Unwanted)

- [INF-119] If the deserialized JSON value is not an array token (e.g. string / number / object), the converter shall throw `JsonSerializationException` referencing the current token type.
- [INF-120] If the deserialized JSON array contains a non-integer value (e.g. string / float), the converter shall propagate the underlying Newtonsoft deserialization exception(`JsonReaderException` or `JsonSerializationException` 系)。
- [INF-121] When the deserialized JSON array contains duplicate int values, the converter shall **succeed**(`DdpPool` は同値要素を許容するシーケンス)。

## 定数依存

(なし、int 配列の serialize / deserialize 純粋変換)

## Implementation Notes

- `null` token(`JsonToken.Null`)は throw せず `null` を返す経路を維持(`WriteJson` の `WriteNull` と対称、INF-118)
- 各要素は `writer.WriteValue(int)` / `serializer.Deserialize<List<int>>(reader)` 経由で primitive int として serialize / deserialize

## 関連

- 実装: `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/DdpPoolJsonConverter.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/DdpPoolJsonConverterTests.cs`
- 関連 spec: [`pile-json-converter.md`](pile-json-converter.md)(同パターン)、ADR-0009 §「DDP プール構造」

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-115 | (テスト免除: Ubiquitous) | round-trip + 順序保持は INF-116 で実質検証 |
| INF-116 | `Given_正負0混在のDdpPool_When_RoundTrip_Then_等価かつ順序保持` | 順序保持 + 全要素一致 |
| INF-117 | `Given_emptyDdpPool_When_RoundTrip_Then_等価` | `DdpPool.Empty` 経路 |
| INF-118 | `Given_JsonNullToken_When_Deserialize_Then_nullを返す` + `Given_nullDdpPool_When_Serialize_Then_nullリテラル` | 対称性検証 |
| INF-119 | `Given_非array_token_When_Deserialize_Then_JsonSerializationException`(TestCase 3 件)| |
| INF-120 | `Given_非int要素_When_Deserialize_Then_Newtonsoft例外`(TestCase 2 件:string / float)| Newtonsoft 透過 |
| INF-121 | `Given_重複int配列_When_Deserialize_Then_成功で重複保持` | シーケンス許容 |
