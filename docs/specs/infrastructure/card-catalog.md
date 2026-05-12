# `ScriptableObjectCardCatalog`(M4-PR1 骨格)

ADR-0012 §2 で確定した DrowZzz 向け `ICardCatalog<IEffect>` の **SO 実装の骨格**。本 PR(M4-PR1)範囲では **カードデータ登録** + **`Get` / `TryGet` / `OnValidate` 重複検出** を実装し、効果列対応は **M4-PR2 / PR3 でひな形拡張**(`GetEffects` は本 PR では空配列固定)。

## 概要

| 観点 | 値 |
| ---- | ---- |
| 型 | `Drowsy.Infrastructure.Games.DrowZzz.ScriptableObjectCardCatalog : ScriptableObject, ICardCatalog<IEffect>` |
| Asset 配置 | `Assets/_Project/Data/Cards/`(JIT 確定 2026-05-14、Designer 編集対象として Resources フォルダ非採用)|
| 内部表現 | `[SerializeField] CardEntryAsset[] _entries`(通常配列、Reorderable List は将来導入候補)|
| `GetEffects` の挙動(本 PR) | **本 PR 範囲では空配列固定**(`Array.Empty<IEffect>()`)。M4-PR2 で 1 派生型(`AdjustSdpEffect`)対応、M4-PR3 で全 11 派生型対応 |
| `OnValidate` 重複検出 | `Debug.LogError(this)` で Unity Console に Asset リンク付きエラー(JIT 確定 2026-05-14)|
| Asset ロード方式 | M4-PR1 では `ScriptableObject.CreateInstance` ベースのテスト経路のみ。本番経路の `AssetReference` / `Addressables` は M5 Bootstrap で確定 |

## 本 PR で確定した JIT 項目(2026-05-14)

ADR-0012 §「M4-PR1 着手時の JIT 確認項目」を本 PR で全 4 項目確定:

| 項目 | 確定内容 |
| ---- | ---- |
| Asset 配置パス | **`Assets/_Project/Data/Cards/`**(Designer 識別容易、コード / データ分離、Resources フォルダ非採用で Build サイズ膨張回避)|
| `CardEntryAsset` の Inspector 編集 UX | **通常配列 `SerializeField CardEntryAsset[]`**(Unity 標準 + / - / index 操作で M4-PR1 は十分、Reorderable List は Card 件数 10+ になった時点で再検討)|
| `OnValidate` 重複 ID 報告 | **`Debug.LogError`**(Build を妨げない + Editor 編集中に即時フィードバック + CI で `ScriptableObjectCardCatalogTests` の重複検出と二重化)|
| 本 PR スコープ | **骨格のみ**(`GetEffects` ひな形空配列固定、効果列対応は M4-PR2 / PR3、INF-006 で Optional マーカー)|

## カードデータ構造

### `ScriptableObjectCardCatalog`(SO 本体)

```csharp
[CreateAssetMenu(menuName = "Drowsy/DrowZzz/Card Catalog", fileName = "DrowZzzCardCatalog")]
public sealed class ScriptableObjectCardCatalog : ScriptableObject, ICardCatalog<IEffect>
{
    [SerializeField] private CardEntryAsset[] _entries;

    public CardData Get(CardId id);
    public bool TryGet(CardId id, out CardData data);
    public IReadOnlyList<IEffect> GetEffects(CardId id);  // 本 PR では空配列固定
}
```

### `CardEntryAsset`(エントリ POCO)

```csharp
[Serializable]
public sealed class CardEntryAsset
{
    [SerializeField] private string _cardIdValue;
    [SerializeField] private string _name;
    [SerializeField] private AttributeEntry[] _attributes;
    // M4-PR2 / PR3 で効果列フィールド追加予定(EffectAsset[] _effects)

    public string CardIdValue { get; }
    public string Name { get; }
    public IReadOnlyList<AttributeEntry> Attributes { get; }
    public CardData ToCardData();  // Drowsy.Domain.Cards.CardData への変換
}
```

### `AttributeEntry`(Dictionary 表現)

```csharp
[Serializable]
public sealed class AttributeEntry
{
    [SerializeField] private string _key;
    [SerializeField] private int _value;

    public string Key { get; }
    public int Value { get; }
}
```

## 普遍要件 (Ubiquitous)

- [INF-001] [Ubiquitous] `ScriptableObjectCardCatalog` shall be a `sealed class` inheriting from `UnityEngine.ScriptableObject` and implementing `ICardCatalog<IEffect>` in `Drowsy.Infrastructure.Games.DrowZzz` namespace.
- [INF-002] [Ubiquitous] `CardEntryAsset` shall be a `[Serializable] sealed class` with `string CardIdValue`, `string Name`, `AttributeEntry[] Attributes` fields, declared in the same namespace.
- [INF-003] [Ubiquitous] `AttributeEntry` shall be a `[Serializable] sealed class` with `string Key`, `int Value` fields, declared in the same namespace.

## 事象駆動要件 (Event-driven)

### `Get` の挙動

- [INF-004] When `Get(id)` is called with `id` registered in `_entries`, the catalog shall return the `CardData` corresponding to that `id`, built from `CardEntryAsset.ToCardData()` at cache build time(`OnEnable` / `OnValidate` / `SetEntriesForTest` のいずれかで構築済の cache から取得、M4-PR1 code-reviewer P-3 反映 2026-05-14)。
- [INF-005] When `Get(id)` is called with `id` **not** registered in `_entries`, the catalog shall throw `KeyNotFoundException`(`ICardCatalog` 契約)。

### `TryGet` の挙動

- [INF-006] When `TryGet(id, out data)` is called with `id` registered, the catalog shall return `true` and set `data` to the corresponding `CardData`.
- [INF-007] When `TryGet(id, out data)` is called with `id` **not** registered, the catalog shall return `false` and set `data` to `null`.

### `GetEffects` の挙動(本 PR 範囲)

- [INF-008] [Optional] When `GetEffects(id)` is called with any `id`(registered or not), the catalog shall return an empty `IReadOnlyList<IEffect>` in **M4-PR1 scope**。M4-PR2 / PR3 で効果列を返すように拡張する(本 PR では `Array.Empty<IEffect>()` を返す graceful 動作のみ確定)。

### `OnValidate` での重複 ID 検出

- [INF-009] When `OnValidate` is invoked(Unity Editor で `_entries` 編集後)、entries に重複する `CardIdValue` を 2 件以上検出した場合、`Debug.LogError(message, this)` を呼んで Unity Console に Asset リンク付きエラーを報告する。Build は妨げない(Editor 編集中の即時フィードバック目的)。

## 異常要件 (Unwanted)

- [INF-010] When `_entries` is `null`(Unity Editor で未初期化シリアライズ等)、catalog の `Get` / `TryGet` / `GetEffects` は `KeyNotFoundException`(Get)/ `false + null`(TryGet)/ 空列(GetEffects)を返す graceful 動作。`NullReferenceException` を呼び出し側に伝播させない。
- [INF-011] When `_entries` contains an entry with `null` or whitespace `CardIdValue`、その entry は cache 構築時に **skip** される。catalog の他 entry には影響しない(graceful degradation)。
- [INF-012] When `_entries` contains an entry whose `ToCardData()` throws(例:`Name` が null / 空白、`AttributeEntry.Key` が空白)、その entry は cache 構築時に **skip** + `Debug.LogError` 報告。catalog の他 entry には影響しない。

## 定数依存

該当なし。本 PR では数値定数を保持しない(将来 PR で `MaxEntries` 等の上限が必要になれば `DrowZzzCardCatalogConstants` に集約)。

## 関連

- ADR: [`docs/adr/0012-m4-scriptableobject-and-persistence.md`](../../adr/0012-m4-scriptableobject-and-persistence.md) §2(SO 構造)/ §5(InMemoryCardCatalog 併存戦略)/ §「M4-PR1 着手時の JIT 確認項目」
- 関連 ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../adr/0006-m1-detail-application-interfaces.md) §1.3(ICardCatalog interface)/ [`docs/adr/0007-m2-detail-card-effects.md`](../../adr/0007-m2-detail-card-effects.md) §2(ジェネリック化、SO 化を M4 に変更)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/ScriptableObjectCardCatalog.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/CardEntryAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/AttributeEntry.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/AssemblyInfo.cs`(新規、`InternalsVisibleTo("Drowsy.Infrastructure.Tests")`)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Drowsy.Infrastructure.Tests.asmdef`(新規、ADR-0012 §5 スニペット)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/ScriptableObjectCardCatalogTests.cs`(EditMode、`ScriptableObject.CreateInstance` ベース)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-001 | (テスト免除: Ubiquitous) | sealed class + ScriptableObject + ICardCatalog 実装、構造的に保証 |
| INF-002 | (テスト免除: Ubiquitous) | [Serializable] sealed class 構造 |
| INF-003 | (テスト免除: Ubiquitous) | [Serializable] sealed class 構造 |
| INF-004 | `Given_登録済id_When_Get_Then_CardDataを返す` | 正常系 Get |
| INF-005 | `Given_未登録id_When_Get_Then_KeyNotFoundException` | 例外系 Get |
| INF-006 | `Given_登録済id_When_TryGet_Then_trueを返す` + `Then_CardDataがout引数にセットされる` | 正常系 TryGet(2 件に分割、1 テスト 1 アサーション、M4-PR1 code-reviewer W-2 反映 2026-05-14)|
| INF-007 | `Given_未登録id_When_TryGet_Then_falseを返す` + `Then_out引数がnull` | 例外系 TryGet(2 件に分割、同上)|
| INF-008 | (テスト免除: Optional) | M4-PR1 範囲では空配列固定、効果列対応は M4-PR2 / PR3 で拡張(Optional マーカーでトレーサビリティ機械検証から除外) |
| INF-009 | `Given_重複CardIdValue_When_OnValidate_Then_DebugLogError` | OnValidate 重複検出(`LogAssert.Expect` で Debug.LogError を assertion) |
| INF-010 | `Given_entries_null_When_GetまたはTryGet_Then_graceful` | _entries null の graceful 動作(3 件:Get → KeyNotFoundException / TryGet → false / GetEffects → 空) |
| INF-011 | `Given_空白CardIdValue_When_Get他正常id_Then_他entryは影響なし` | 空 / null CardIdValue の skip + 他 entry の継続動作 |
| INF-012 | `Given_不正attributes_When_GetまたはTryGet_Then_他entryは影響なし` | 構築失敗 entry の skip + 他 entry の継続動作 |
