# InMemoryCardCatalog

`ICardCatalog<IEffect>` の in-memory 実装。`Dictionary<CardId, CardData>` をベースとした汎用 stub。

## 概要

`InMemoryCardCatalog` は `Drowsy.Application.Catalog` namespace に置く `ICardCatalog<IEffect>` (DrowZzz 用、`TEffect = IEffect`) の最小実装。M1〜M2 のテスト・skeleton 用途で利用する。

ADR-0006 §1.3 では「M2 で SO 化 (`ScriptableObjectCardCatalog`)」と記載されていたが、ADR-0007 §5 で M4(永続化と一緒に判断)へ送ることが確定。よって本 in-memory 実装は M4 まで主役となる。

M2-PR1 段階では `GetEffects` は常に空列を返す(全カードが効果なし、M1 互換)。M2-PR2 以降で 1 PR = 1 effect record を追加する都度、本クラスの効果定義を拡張するか、effect 別 store の導入を判断する。

## 普遍要件 (Ubiquitous)

- [APP-011] [Ubiquitous] The `InMemoryCardCatalog` shall implement `ICardCatalog<IEffect>` in the `Drowsy.Application.Catalog` namespace.

## 事象駆動要件 (Event-driven)

- [APP-012] When constructed with valid `entries`, the catalog shall internally store a defensive copy of the entries.
- [APP-013] When `Get(id)` is called with a registered `CardId`, the catalog shall return the registered `CardData`.
- [APP-015] When `TryGet(id, out data)` is called with a registered `CardId`, the catalog shall return `true`.
- [APP-016] When `TryGet(id, out data)` is called with a registered `CardId`, the catalog shall set `data` to the registered `CardData`.
- [APP-017] When `TryGet(id, out data)` is called with an unregistered `CardId`, the catalog shall return `false`.
- [APP-018] When `TryGet(id, out data)` is called with an unregistered `CardId`, the catalog shall set `data` to `null`.
- [APP-037] When `GetEffects(id)` is called with a registered `CardId`, the catalog shall return an empty `IReadOnlyList<IEffect>` (M2-PR1 段階の全カード共通挙動、M2-PR2 以降で個別効果定義が追加される予定)。
- [APP-038] When `GetEffects(id)` is called with an unregistered `CardId`, the catalog shall return an empty `IReadOnlyList<IEffect>` without throwing (`KeyNotFoundException` 等の例外を投げず、`Aggregate` 0 個ループとして自然に扱える契約。`Get` / `TryGet` とは扱いが異なる、ADR-0007 §3 末尾)。

## 異常要件 (Unwanted)

- [APP-014] If `Get(id)` is called with an unregistered `CardId`, then the catalog shall throw `KeyNotFoundException`.
- [APP-019] If constructed with `null` `entries`, then the catalog shall throw `ArgumentNullException`.
- [APP-020] If constructed with `entries` containing a `null` `CardData` value, then the catalog shall throw `ArgumentException`.

## 関連

- 実装: `Assets/_Project/Scripts/Application/Catalog/InMemoryCardCatalog.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/Catalog/InMemoryCardCatalogTests.cs`
- シナリオ: `in-memory-card-catalog.feature`
- 関連 interface: `Assets/_Project/Scripts/Application/ICardCatalog.cs` ([`card-catalog.md`](card-catalog.md))
- 関連 EARS: [`effect-mechanism.md`](../games/drowzzz/effect-mechanism.md)(DZ-082〜DZ-088、`DrowZzzRule.PlayCardAction.Apply` から `GetEffects` 呼び出しの統合契約)
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §1.3 / [`docs/adr/0007-m2-detail-card-effects.md`](../../adr/0007-m2-detail-card-effects.md) §5

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-011 | (テスト免除: Ubiquitous) | `class InMemoryCardCatalog : ICardCatalog<IEffect>` で構造的に保証 |
| APP-012 | `Given_有効なentries_When_InMemoryCardCatalogを生成_Then_元entries変更後も内部状態は不変` | 元 entries を変更しても catalog の挙動が変わらないことを確認(防御コピー) |
| APP-013 | `Given_登録済CardId_When_Getを呼ぶ_Then_対応CardDataが返る` | |
| APP-014 | `Given_未登録CardId_When_Getを呼ぶ_Then_KeyNotFoundExceptionを投げる` | |
| APP-015 | `Given_登録済CardId_When_TryGetを呼ぶ_Then_trueを返す` | |
| APP-016 | `Given_登録済CardId_When_TryGetを呼ぶ_Then_dataに対応CardDataが設定される` | |
| APP-017 | `Given_未登録CardId_When_TryGetを呼ぶ_Then_falseを返す` | |
| APP-018 | `Given_未登録CardId_When_TryGetを呼ぶ_Then_dataにnullが設定される` | |
| APP-019 | `Given_entriesにnull_When_InMemoryCardCatalogを生成_Then_ArgumentNullExceptionを投げる` | |
| APP-020 | `Given_entriesにnullCardData_When_InMemoryCardCatalogを生成_Then_ArgumentExceptionを投げる` | |
| APP-037 | `Given_登録済CardId_When_GetEffectsを呼ぶ_Then_空配列を返す` | M2-PR1 段階の挙動。M2-PR2 以降で個別効果定義が追加されたら本 ID の表記を見直す |
| APP-038 | `Given_未登録CardId_When_GetEffectsを呼ぶ_Then_例外を投げず空配列を返す` | `Get` / `TryGet` と異なり例外を投げない仕様 |
