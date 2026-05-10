# InMemoryCardCatalog

`ICardCatalog` の in-memory 実装。`Dictionary<CardId, CardData>` をベースとした汎用 stub。

## 概要

`InMemoryCardCatalog` は `Drowsy.Application.Catalog` namespace に置く `ICardCatalog` の最小実装。M1〜M2 のテスト・skeleton 用途で利用し、本格的な永続化や ScriptableObject ベースの実装 (M2 以降の `Drowsy.Infrastructure.Games.DrowZzz.ScriptableObjectCardCatalog`) と並行存続する。
ADR-0006 §1.3 / §M1-PR2 の決定に基づく。

## 普遍要件 (Ubiquitous)

- [APP-011] [Ubiquitous] The `InMemoryCardCatalog` shall implement `ICardCatalog` in the `Drowsy.Application.Catalog` namespace.

## 事象駆動要件 (Event-driven)

- [APP-012] When constructed with valid `entries`, the catalog shall internally store a defensive copy of the entries.
- [APP-013] When `Get(id)` is called with a registered `CardId`, the catalog shall return the registered `CardData`.
- [APP-015] When `TryGet(id, out data)` is called with a registered `CardId`, the catalog shall return `true`.
- [APP-016] When `TryGet(id, out data)` is called with a registered `CardId`, the catalog shall set `data` to the registered `CardData`.
- [APP-017] When `TryGet(id, out data)` is called with an unregistered `CardId`, the catalog shall return `false`.
- [APP-018] When `TryGet(id, out data)` is called with an unregistered `CardId`, the catalog shall set `data` to `null`.

## 異常要件 (Unwanted)

- [APP-014] If `Get(id)` is called with an unregistered `CardId`, then the catalog shall throw `KeyNotFoundException`.
- [APP-019] If constructed with `null` `entries`, then the catalog shall throw `ArgumentNullException`.
- [APP-020] If constructed with `entries` containing a `null` `CardData` value, then the catalog shall throw `ArgumentException`.

## 関連

- 実装: `Assets/_Project/Scripts/Application/Catalog/InMemoryCardCatalog.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/Catalog/InMemoryCardCatalogTests.cs`
- シナリオ: `in-memory-card-catalog.feature`
- 関連 interface: `Assets/_Project/Scripts/Application/ICardCatalog.cs` ([`card-catalog.md`](card-catalog.md))
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §1.3

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-011 | (テスト免除: Ubiquitous) | `class InMemoryCardCatalog : ICardCatalog` で構造的に保証 |
| APP-012 | `Given_有効なentries_When_InMemoryCardCatalogを生成_Then_元entries変更後も内部状態は不変` | 元 entries を変更しても catalog の挙動が変わらないことを確認(防御コピー) |
| APP-013 | `Given_登録済CardId_When_Getを呼ぶ_Then_対応CardDataが返る` | |
| APP-014 | `Given_未登録CardId_When_Getを呼ぶ_Then_KeyNotFoundExceptionを投げる` | |
| APP-015 | `Given_登録済CardId_When_TryGetを呼ぶ_Then_trueを返す` | |
| APP-016 | `Given_登録済CardId_When_TryGetを呼ぶ_Then_dataに対応CardDataが設定される` | |
| APP-017 | `Given_未登録CardId_When_TryGetを呼ぶ_Then_falseを返す` | |
| APP-018 | `Given_未登録CardId_When_TryGetを呼ぶ_Then_dataにnullが設定される` | |
| APP-019 | `Given_entriesにnull_When_InMemoryCardCatalogを生成_Then_ArgumentNullExceptionを投げる` | |
| APP-020 | `Given_entriesにnullCardData_When_InMemoryCardCatalogを生成_Then_ArgumentExceptionを投げる` | |
