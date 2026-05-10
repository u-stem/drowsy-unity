# ICardCatalog

`CardId` から `CardData` を引く責務を持つ Application 層 interface。

## 概要

`ICardCatalog` は `Drowsy.Application` namespace 直下に置く汎用 interface で、`CardId → CardData` の解決 API を定義する。実装は M1 では in-memory スタブ (`InMemoryCardCatalog`、M1-PR2 で追加)、M2 以降で `ScriptableObject` ベース (`Drowsy.Infrastructure` 配下) を予定。
ADR-0006 §1.3 の決定に基づく。

## 普遍要件 (Ubiquitous)

- [APP-006] [Ubiquitous] The `ICardCatalog` shall expose `Get(CardId) : CardData` and `TryGet(CardId, out CardData) : bool`.

> 注: ADR-0006 §1.3 の signature 表記は `out CardData?` だが、Phase 2 時点では NRT (Nullable Reference Types) を未有効化のため宣言上は `out CardData` とする。NRT 有効化時(`docs/todo.md` 「NRT 検討」)に `out CardData?` へ昇格させる。

## 事象駆動要件 (Event-driven)

- [APP-007] When `Get(id)` is called with a `CardId` registered in the catalog, the catalog shall return the corresponding `CardData`.
- [APP-008] When `TryGet(id, out data)` is called with a `CardId` registered in the catalog, the method shall return `true` and set `data` to the corresponding `CardData`.
- [APP-009] When `TryGet(id, out data)` is called with a `CardId` not registered in the catalog, the method shall return `false` and set `data` to `null` (= `default(CardData)`).

## 異常要件 (Unwanted)

- [APP-010] If `Get(id)` is called with a `CardId` not registered in the catalog, then the catalog shall throw `KeyNotFoundException`.

## 関連

- 実装: `Assets/_Project/Scripts/Application/ICardCatalog.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/ICardCatalogTests.cs`
- シナリオ: `card-catalog.feature`
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §1.3

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-006 | (テスト免除: Ubiquitous) | `ICardCatalog.cs` の interface 定義で構造的に保証 |
| APP-007 | `Given_登録済CardId_When_Getを呼ぶ_Then_対応するCardDataが返る` | ダミー catalog で contract を検証 |
| APP-008 | `Given_登録済CardId_When_TryGetを呼ぶ_Then_trueを返す` / `Given_登録済CardId_When_TryGetを呼ぶ_Then_dataに対応CardDataが設定される` | 「true を返す」と「out 引数に CardData を設定する」を 1 テスト 1 アサーション原則に従い分割 |
| APP-009 | `Given_未登録CardId_When_TryGetを呼ぶ_Then_falseを返す` / `Given_未登録CardId_When_TryGetを呼ぶ_Then_dataにnullが設定される` | 同上(false 返却 / null 設定) |
| APP-010 | `Given_未登録CardId_When_Getを呼ぶ_Then_KeyNotFoundExceptionを投げる` | ダミー catalog で contract を検証 |
