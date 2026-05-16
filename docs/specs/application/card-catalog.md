# ICardCatalog<TEffect>

`CardTypeId` から `CardData` と効果列 (`TEffect`) を引く責務を持つ Application 層汎用 interface。

> **注(ADR-0018 関連)**: 当 interface の lookup key は当初 `CardId` だったが、ADR-0018 で `CardId` を `(CardTypeId TypeId, int Instance)` 複合型に refactor し、catalog 引きの key は **`CardTypeId`(種別 ID)** に変更した。`CardId` は Hand 内 instance unique な識別子に専念し、catalog 引き呼び出し側は `cardId.TypeId` を渡す。本 spec 内で `id` と書かれている箇所は `CardTypeId` 値を指す(旧 spec 名残としてテスト名に "CardId" 表記が残るのは仕様、G-7 で順次 rename 予定)。

## 概要

`ICardCatalog<TEffect>` は `Drowsy.Application` namespace 直下に置く汎用 interface で、`CardTypeId → CardData` と `CardTypeId → IReadOnlyList<TEffect>` の解決 API を定義する。`TEffect` はゲーム固有の効果型(DrowZzz では `Drowsy.Application.Games.DrowZzz.Effects.IEffect`)。

実装は M1 では in-memory スタブ (`InMemoryCardCatalog`、M1-PR2 で追加 / M2-PR1 で `ICardCatalog<IEffect>` に拡張)、永続化と一緒に判断する M4 で `ScriptableObject` ベース (`Drowsy.Infrastructure` 配下) を採用(ADR-0012)。

ジェネリック化の根拠(ADR-0007 §2):「汎用 interface に DrowZzz 固有型を露出させない」+「将来別ゲームが `ICardCatalog<IOtherEffect>` で自分の効果型を選べる」。案 A(本ジェネリック)を採用、案 B(別 interface 分離) / 案 C(`IReadOnlyList<object>`) / 案 D(DrowZzz namespace 移動)は不採用(ADR-0007 §2)。

ADR-0006 §1.3 の「M2 で SO 化」記載は ADR-0007 §5 で M4 に変更された。

## 普遍要件 (Ubiquitous)

- [APP-006] [Ubiquitous] The `ICardCatalog<TEffect>` shall expose `Get(CardTypeId) : CardData` and `TryGet(CardTypeId, out CardData) : bool`.
- [APP-036] [Ubiquitous] The `ICardCatalog<TEffect>` shall expose `GetEffects(CardTypeId) : IReadOnlyList<TEffect>` where `TEffect : class`.

> 注: ADR-0006 §1.3 の signature 表記は `out CardData?` だが、ADR-0015 で NRT (Nullable Reference Types) を非採用と決定済のため宣言上は `out CardData` とする。NRT 採用に転じた際(ADR-0015 が Superseded by ... で覆る場合)に `out CardData?` へ昇格させる。

## 事象駆動要件 (Event-driven)

- [APP-007] When `Get(id)` is called with a `CardTypeId` registered in the catalog, the catalog shall return the corresponding `CardData`.
- [APP-008] When `TryGet(id, out data)` is called with a `CardTypeId` registered in the catalog, the method shall return `true` and set `data` to the corresponding `CardData`.
- [APP-009] When `TryGet(id, out data)` is called with a `CardTypeId` not registered in the catalog, the method shall return `false` and set `data` to `null` (= `default(CardData)`).

## 異常要件 (Unwanted)

- [APP-010] If `Get(id)` is called with a `CardTypeId` not registered in the catalog, then the catalog shall throw `KeyNotFoundException`.

## 関連

- 実装: `Assets/_Project/Scripts/Application/ICardCatalog.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/ICardCatalogTests.cs`
- シナリオ: `card-catalog.feature`
- 実装(具象): `Assets/_Project/Scripts/Application/Catalog/InMemoryCardCatalog.cs`([`in-memory-card-catalog.md`](in-memory-card-catalog.md)) — `ICardCatalog<IEffect>` を実装、`GetEffects` は M2-PR1 段階で常に空列
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §1.3 / [`docs/adr/0007-m2-detail-card-effects.md`](../../adr/0007-m2-detail-card-effects.md) §2 / §5

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-006 | (テスト免除: Ubiquitous) | `ICardCatalog<TEffect>.cs` の interface 定義で構造的に保証 |
| APP-007 | `Given_登録済CardId_When_Getを呼ぶ_Then_対応するCardDataが返る` | ダミー catalog で contract を検証 |
| APP-008 | `Given_登録済CardId_When_TryGetを呼ぶ_Then_trueを返す` / `Given_登録済CardId_When_TryGetを呼ぶ_Then_dataに対応CardDataが設定される` | 「true を返す」と「out 引数に CardData を設定する」を 1 テスト 1 アサーション原則に従い分割 |
| APP-009 | `Given_未登録CardId_When_TryGetを呼ぶ_Then_falseを返す` / `Given_未登録CardId_When_TryGetを呼ぶ_Then_dataにnullが設定される` | 同上(false 返却 / null 設定) |
| APP-010 | `Given_未登録CardId_When_Getを呼ぶ_Then_KeyNotFoundExceptionを投げる` | ダミー catalog で contract を検証 |
| APP-036 | (テスト免除: Ubiquitous) | `ICardCatalog<TEffect>.cs` の interface 定義(`GetEffects` シグネチャ + `TEffect : class` 制約)で構造的に保証。挙動は具象実装側 (APP-037 / APP-038、[`in-memory-card-catalog.md`](in-memory-card-catalog.md)) で検証 |
