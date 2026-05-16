# CardIdJsonConverter

## 概要

`Drowsy.Infrastructure.Persistence.Converters.CardIdJsonConverter` は `Drowsy.Domain.Cards.CardId`(ADR-0018 で `(CardTypeId TypeId, int Instance)` 複合型に refactor された value object)を plain string(`"<typeId>#<instance>"` 形式)として JSON に serialize / deserialize する責務を持つ。

`CardId` は `private ctor + static factory` パターンのため Newtonsoft.Json の自動 deserialize ができず、本 converter が schema 違反を `JsonSerializationException` に正規化する。`DrowZzzGameSessionSerializer` は当該 converter を `DrowZzzJsonSettings.Create()` で登録済で、本 spec は converter 単体の round-trip と異常経路を `Infrastructure.Tests/Persistence/CardIdJsonConverterTests.cs` で機械検証する範囲を定義する。

ADR-0018 §8 / `docs/todo.md`「`CardIdJsonConverter` の負値 instance / 不正 schema 経路に Persistence テストを追加」由来。

## 普遍要件 (Ubiquitous)

- [INF-088] [Ubiquitous] The `CardIdJsonConverter` shall serialize `CardId` as a plain string in `"<typeId>#<instance>"` form (`#` is the last separator) and deserialize the same form back to an equal `CardId` via `CardId.Equals`.

## 事象駆動要件 (Event-driven)

- [INF-089] When a `CardId` instance is serialized and then deserialized through `JsonConvert.SerializeObject` / `DeserializeObject<CardId>` using `DrowZzzJsonSettings.Create()`, the resulting `CardId` shall equal the original via value equality.

## 異常要件 (Unwanted)

- [INF-090] If the deserialized JSON string is `null` / empty / whitespace, the converter shall throw `JsonSerializationException`.
- [INF-091] If the deserialized JSON string does not contain `#`, the converter shall throw `JsonSerializationException` referencing ADR-0018 schema.
- [INF-092] If the `instance` portion (after the last `#`) cannot be parsed as `int`, the converter shall throw `JsonSerializationException`.
- [INF-093] If the `instance` portion is a negative integer (which causes `CardId.Of` to throw `ArgumentOutOfRangeException`), the converter shall wrap the underlying exception in `JsonSerializationException`.
- [INF-094] If the `typeId` portion (before the last `#`) is an empty string (which causes `CardTypeId.Of` to throw `ArgumentException`), the converter shall wrap the underlying exception in `JsonSerializationException`.

## 定数依存

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| separator `'#'` | L2 | `CardId.Value` 内 schema(ADR-0018、`CardTypeId` 内に `#` を含まない前提) |

## Implementation Notes

- `null` token(`JsonToken.Null`)は throw せず `null` を返す経路を維持(`WriteJson` の `WriteNull` と対称)。本仕様の異常要件 INF-090 は **string token 値が null/空/空白の場合** を対象とする。
- `#` の split は `string.LastIndexOf('#')` を使い、`CardTypeId` 自体に `#` を含まないという ADR-0018 §3 の前提に依拠する。
- `CardId.Of` の `ArgumentOutOfRangeException`(`Instance < 0`)と `CardTypeId.Of` の `ArgumentException`(空文字列)は両方 `ArgumentException` を継承するため、本 converter は `catch (ArgumentException ex)` で受けて `JsonSerializationException` に wrap する。

## 関連

- 実装: `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/CardIdJsonConverter.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/CardIdJsonConverterTests.cs`
- ADR: [`docs/adr/0018-cardtypeid-cardid-instance-separation.md`](../../../adr/0018-cardtypeid-cardid-instance-separation.md) §8
- 関連 spec: [`drowzzz-game-session-serializer.md`](drowzzz-game-session-serializer.md)(本 converter を含む `DrowZzzJsonSettings.Create()` の登録経路)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-088 | (テスト免除: Ubiquitous) | `WriteJson` / `ReadJson` の対称性は INF-089 round-trip で実質検証 |
| INF-089 | `Given_normal_CardId_When_RoundTrip_Then_等価`(TestCase 3 件:`dream#0` / `sheep#3` / `01#42`)+ `Given_JsonNullToken_When_Deserialize_Then_nullを返す` | `CardTypeId.Of` は `#` を含む文字列を `ArgumentException` で reject するため、本 round-trip では `#` を含まない typeId のみで検証 |
| INF-090 | `Given_<空文字列|空白>_When_Deserialize_Then_JsonSerializationException`(TestCase 3 件) | null token は別経路で `null` を返すため対象外 |
| INF-091 | `Given_#欠如_When_Deserialize_Then_JsonSerializationException` | |
| INF-092 | `Given_非int_instance_When_Deserialize_Then_JsonSerializationException` | |
| INF-093 | `Given_負値_instance_When_Deserialize_Then_JsonSerializationException` | `CardId.Of` の `ArgumentOutOfRangeException` を wrap |
| INF-094 | `Given_typeIdPart_空文字列_When_Deserialize_Then_JsonSerializationException` | `CardTypeId.Of` の `ArgumentException` を wrap |
