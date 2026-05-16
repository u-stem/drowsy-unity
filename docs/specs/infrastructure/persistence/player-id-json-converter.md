# PlayerIdJsonConverter

## 概要

`Drowsy.Infrastructure.Persistence.Converters.PlayerIdJsonConverter` は `Drowsy.Domain.Players.PlayerId`(`private ctor + static PlayerId.Of(string)` パターンの value object)を plain string として JSON に serialize / deserialize する責務を持つ。

`CardIdJsonConverter` と対称設計。schema 違反は `JsonSerializationException` に正規化する。`DrowZzzGameSessionSerializer` は本 converter を `DrowZzzJsonSettings.Create()` で登録済。

B-5 第 1 弾(Infrastructure カバレッジ補完、ADR-0012 §M4-PR7 完成記録 / `docs/todo.md` 由来、2026-05-16)で単体テストを追加。

## 普遍要件 (Ubiquitous)

- [INF-095] [Ubiquitous] The `PlayerIdJsonConverter` shall serialize `PlayerId` as a plain string equal to `PlayerId.Value` and deserialize the same string back to an equal `PlayerId` via `PlayerId.Equals`.

## 事象駆動要件 (Event-driven)

- [INF-096] When a `PlayerId` instance is serialized and then deserialized through `JsonConvert.SerializeObject` / `DeserializeObject<PlayerId>` using `DrowZzzJsonSettings.Create()`, the resulting `PlayerId` shall equal the original via value equality.
- [INF-097] When a JSON literal `null` is deserialized as `PlayerId`, the converter shall return `null`(対称的に `WriteJson` も `null` 値で `WriteNull`)。

## 異常要件 (Unwanted)

- [INF-098] If the deserialized JSON value is not a string token (e.g. number / object / array), the converter shall throw `JsonSerializationException` referencing the current token type.
- [INF-099] If the deserialized JSON string is `null` / empty / whitespace, the converter shall throw `ArgumentException`(`PlayerId.Of` の防御による、本 converter は wrap せず透過)。

## 定数依存

(なし、純粋な string ↔ `PlayerId` 変換)

## Implementation Notes

- `null` token(`JsonToken.Null`)は throw せず `null` を返す経路を維持(`WriteJson` の `WriteNull` と対称、INF-097)
- `PlayerId.Of(null / 空)` は `ArgumentException` を投げるため、本 converter は wrap せず透過(`CardIdJsonConverter` は `CardId.Of` の wrap を `JsonSerializationException` 化したのとは設計差。これは `PlayerId` 構造に schema 違反の余地がない(`#` 等の separator なし)+ Newtonsoft の deserialize 経路で `string` token 値が `null` になることは pathological なので、`PlayerId.Of` の例外を透過する方が原因追跡が容易)。

## 関連

- 実装: `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/PlayerIdJsonConverter.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/PlayerIdJsonConverterTests.cs`
- 対称 converter: [`card-id-json-converter.md`](card-id-json-converter.md)(ADR-0018 由来、本 converter と同パターン)
- 関連 spec: [`drowzzz-game-session-serializer.md`](drowzzz-game-session-serializer.md)(本 converter を含む `DrowZzzJsonSettings.Create()` の登録経路)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-095 | (テスト免除: Ubiquitous) | `WriteJson` / `ReadJson` の対称性は INF-096 round-trip で実質検証 |
| INF-096 | `Given_normal_PlayerId_When_RoundTrip_Then_等価`(TestCase 3 件:`p1` / `p2` / `player-001`)| |
| INF-097 | `Given_JsonNullToken_When_Deserialize_Then_nullを返す` + `Given_nullPlayerId_When_Serialize_Then_nullリテラル` | 対称性検証 |
| INF-098 | `Given_非string_token_When_Deserialize_Then_JsonSerializationException`(TestCase 3 件:number / object / array)| |
| INF-099 | `Given_空文字列_When_Deserialize_Then_ArgumentException`(TestCase 3 件:空 / 空白 / tab)| `PlayerId.Of` の例外透過 |
