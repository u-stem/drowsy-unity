# CardId(カードの instance unique 識別子、ADR-0018 で refactor 済)

カードの **インスタンス** 識別子(deck / hand / field / discard 内で unique)を表す不変値オブジェクト。

## 概要

`CardId` は `(CardTypeId TypeId, int Instance)` の複合 `sealed record`。同じ種別(`TypeId`)のカードが
複数枚 deck / hand に存在する場合、それぞれが異なる `Instance` を持つことで unique 性を担保する
(業界デファクト準拠、ADR-0018)。

旧 Phase 1 設計の `CardId.Of(string)` 単純文字列型は廃止された。catalog の lookup key 用途は
`CardTypeId`(別 spec `card-type-id.md`)が担う。

## 普遍要件 (Ubiquitous)

- [CARD-001] [Ubiquitous] The `CardId` shall be immutable (`sealed record` with `get`-only properties).
- [CARD-002] [Ubiquitous] The `CardId` shall expose `CardTypeId TypeId` and `int Instance` as read-only properties, and `string Value` as a computed property of form `"<TypeId.Value>#<Instance>"`.
- [CARD-003] Two `CardId` instances shall be equal iff their `(TypeId, Instance)` tuples are equal (record auto-generated `Equals` / `GetHashCode`).

## 事象駆動要件 (Event-driven)

- [CARD-004] When `CardId.Of(typeId, instance)` is called with a non-null `typeId` and a non-negative `instance`, the `CardId` shall return a new instance whose `TypeId`, `Instance`, and `Value` reflect the inputs.
- [CARD-005] Two `CardId` instances with different `TypeId` (or different `Instance`) shall be considered non-equal.
- [CARD-009] When `ToString()` is called, the `CardId` shall return its `Value`(= `"<TypeId.Value>#<Instance>"`).

## 異常要件 (Unwanted)

- [CARD-006] If `CardId.Of(null, instance)` is called, then `ArgumentNullException` shall be thrown.
- [CARD-007] If `CardId.Of(typeId, instance)` is called with a negative `instance`, then `ArgumentOutOfRangeException` shall be thrown.

## Note(ADR-0018 によるリファクタ)

旧要件 ID とその移行:

| 旧要件 | 移行 |
| ---- | ---- |
| 旧 CARD-006(`CardId.Of(null)` で `ArgumentException`) | **CARD-006 redefined**: `CardId.Of(null, instance)` で `ArgumentNullException`(string null → ANE への昇格)|
| 旧 CARD-007(`CardId.Of("")` で `ArgumentException`) | **削除**(catalog key 検証は `CardTypeId.Of("")` 側、CTYPE-002 に移行) |
| 旧 CARD-008(`CardId.Of("   ")` で `ArgumentException`) | **削除**(同上、CTYPE-002 に統合) |
| 新 CARD-007 | `CardId.Of(typeId, -1)` で `ArgumentOutOfRangeException`(新規) |
| 新 CARD-009 | `ToString` 仕様(旧 CARD-005 を CARD-009 に rename、番号衝突回避) |

## 関連

- 確定 ADR: [ADR-0018 CardTypeId と CardId(instance)の分離](../../../adr/0018-cardtypeid-cardid-instance-separation.md)
- 関連 spec: `card-type-id.md`(同ディレクトリ、catalog key 用の種別 ID)、`hand.md`(unique CardId 集合の正当化)、`pile.md`
- 実装: `Assets/_Project/Scripts/Domain/Cards/CardId.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Cards/CardIdTests.cs`
- シナリオ: `card-id.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| CARD-001 | (テスト免除: Ubiquitous) | `sealed record` で構造的に保証 |
| CARD-002 | (テスト免除: Ubiquitous) | `TypeId`, `Instance`, `Value` の `get` プロパティで構造的に保証 |
| CARD-003 | `Given_同じtypeIdとinstanceの2つのCardId_When_等価比較_Then_等価` | record の自動生成等価性 |
| CARD-004 | `Given_有効なtypeIdとinstance_When_Ofを呼ぶ_Then_TypeIdとInstanceが保持される` / `..._Then_ValueはtypeIdとinstanceを_で連結した形式` | Normal |
| CARD-005 | `Given_異なるtypeIdの2つのCardId_When_等価比較_Then_非等価` / `Given_同じtypeIdで異なるinstanceの..._Then_非等価` | 非等価の網羅 |
| CARD-006 | `Given_typeIdがnull_When_Ofを呼ぶ_Then_ArgumentNullExceptionを投げる` | Abnormal(typeId null) |
| CARD-007 | `Given_instanceが負数_When_Ofを呼ぶ_Then_ArgumentOutOfRangeExceptionを投げる` | Abnormal(instance < 0) |
| CARD-009 | `Given_CardId_When_ToStringを呼ぶ_Then_Valueと同じ文字列を返す` | Normal |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
