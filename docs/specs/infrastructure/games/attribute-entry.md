# AttributeEntry

## 概要

`Drowsy.Infrastructure.Games.DrowZzz.AttributeEntry` は `CardEntryAsset.Attributes` の要素として `Dictionary<string, int>` を Unity Inspector で編集可能に表現する `[Serializable]` POCO(M4-PR1 で導入、ADR-0012 §2)。

Unity Inspector は `Dictionary<TKey, TValue>` を直接シリアライズできないため、key / value 1 ペアを保持する小型 class で配列化する。テストからは `internal ctor` 経由でインスタンスを直接構築する(Inspector の `[SerializeField] private field` を経由しないテスト構築パターン、`InternalsVisibleTo("Drowsy.Infrastructure.Tests")`)。

B-5 第 1 弾(Infrastructure カバレッジ補完、2026-05-16)で単体テストを追加。

## 普遍要件 (Ubiquitous)

- [INF-131] [Ubiquitous] The `AttributeEntry` shall be `[Serializable] sealed class` with two `[SerializeField] private` fields `_key` (string) and `_value` (int).

## 事象駆動要件 (Event-driven)

- [INF-132] When `AttributeEntry` is constructed via the `internal ctor(string key, int value)`, the `Key` getter shall return the input `key` and the `Value` getter shall return the input `value`.

## 異常要件 (Unwanted)

- [INF-133] If `AttributeEntry` is constructed with `key == null`, the `Key` getter shall return `null`(本 ctor は防御を持たず、`CardData` 構築時に親アセンブリが検証する設計、INF-001 / INF-003)。

## 関連

- 実装: `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/AttributeEntry.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/AttributeEntryTests.cs`
- 親 asset: [`./card-entry-asset.md`](card-entry-asset.md)(`CardEntryAsset.Attributes` の要素)
- ADR: ADR-0012 §2「ScriptableObject 化」

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-131 | (テスト免除: Ubiquitous) | `[Serializable] sealed class` + `[SerializeField] private` は宣言で構造保証 |
| INF-132 | `Given_internal_ctor_When_Construct_Then_Key_Value_getterが入力と一致`(TestCase 3 件:`("key1", 0)` / `("attr-a", 42)` / `("", -1)` の境界) | |
| INF-133 | `Given_null_key_When_Construct_Then_Key_getterがnull` | 防御なし設計、親アセンブリが検証 |
