# CardData

カードの中身(名前と数値属性集合)を表す不変値オブジェクト。

## 概要

`CardData` は `Name` と `Attributes` (`IReadOnlyDictionary<string, int>`) を保持する不変値オブジェクト。`CardId` とは独立し、Application 層の `ICardCatalog` (Phase 2 スコープ) で結合する想定。`record` ではなく `sealed class` として実装し、`Equals` / `GetHashCode` / `operator==` / `operator!=` を順序非依存マルチセット同値で override する(設計判断は ADR-0002)。

## 普遍要件 (Ubiquitous)

- [CDATA-001] [Ubiquitous] The `CardData` shall be immutable.
- [CDATA-002] [Ubiquitous] The `CardData` shall expose its `Name` as a read-only string and `Attributes` as a read-only dictionary.

## 事象駆動要件 (Event-driven)

- [CDATA-003] When `new CardData(name, attributes)` is called with a non-blank `name` and a non-null `attributes`, the constructed instance shall hold the same `Name` and a defensive copy of `attributes`.
- [CDATA-004] When `HasAttribute(key)` is called and `key` exists in `Attributes`, the `CardData` shall return `true`.
- [CDATA-005] When `HasAttribute(key)` is called and `key` does not exist in `Attributes`, the `CardData` shall return `false`.
- [CDATA-006] When `GetAttribute(key, defaultValue)` is called and `key` exists in `Attributes`, the `CardData` shall return the stored value.
- [CDATA-007] When `GetAttribute(key, defaultValue)` is called and `key` does not exist in `Attributes`, the `CardData` shall return `defaultValue`.
- [CDATA-008] When two `CardData` instances are compared with `Equals`, they shall be equal if they have the same `Name` and the same key-value pairs in `Attributes`, regardless of insertion order.
- [CDATA-009] When `GetHashCode` is called on two equal `CardData` instances, they shall return the same hash value.
- [CDATA-010] When the source dictionary passed to the constructor is mutated after construction, `CardData.Attributes` shall remain unchanged.
- [CDATA-015] When two `CardData` references are compared with `operator==` or `operator!=`, the result shall be value-equal consistent with `Equals`. Both null operands compare as equal; one-sided null compares as not equal.
- [CDATA-017] When `Equals(object)` is called with `null` or an argument that is not a `CardData`, the `CardData` shall return `false`.

## 異常要件 (Unwanted)

- [CDATA-011] If `name` is null, empty, or whitespace-only, then the constructor shall throw `ArgumentException`.
- [CDATA-012] If `attributes` is null, then the constructor shall throw `ArgumentNullException`.
- [CDATA-013] If `attributes` contains a null, empty, or whitespace-only key, then the constructor shall throw `ArgumentException`.
- [CDATA-014] If `HasAttribute(null)` or `GetAttribute(null, ...)` is called, then the `CardData` shall throw `ArgumentNullException`.

## 任意要件 (Optional)

- [CDATA-016] [Optional] Where `attributes` contains the same key multiple times (only possible via `IEnumerable<KeyValuePair>` source, since `Dictionary<string, int>` enforces uniqueness), the `CardData` shall keep the last value (last-write-wins, consistent with `Dictionary<TKey, TValue>` semantics).

## 実装メモ

### Equals / GetHashCode の実装方針

順序非依存マルチセット同値は以下で担保:
- `Equals(CardData)` は `Name` 同値 + `Attributes.Count` 一致 + 各キーが他方にも存在し値が一致することを順次照合
- `Equals(object)` は `is CardData` パターンマッチで型不一致・null を false に帰着 (CDATA-017)
- `GetHashCode` は `Name` のハッシュと各 (key, value) ペアの `HashCode.Combine` 結果を XOR 合成

XOR 合成は同じペアハッシュが偶数回現れると打ち消し合う理論的衝突リスクがあるが、典型的な属性数(1〜10 個)では実用上問題ない。属性数が大幅に増えるゲームで `Dictionary` / `HashSet` のバケット検索効率が問題化する場合は累積加算等への変更を検討する。

### operator== の null 安全実装

`left is null ? right is null : left.Equals(right)` のパターンで null 安全性を担保(CDATA-015)。これにより両 null は等価、片方のみ null は非等価、両非 null は値同値で比較される。

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Cards/CardData.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Cards/CardDataTests.cs`
- シナリオ: `card-data.feature`
- 設計根拠: [`docs/adr/0002-phase1-domain-boundaries.md`](../../../adr/0002-phase1-domain-boundaries.md) (CardData 等値性 / Card 抽象 / Immutability)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| CDATA-001 | (テスト免除: Ubiquitous) | `sealed class` + `private readonly` フィールドで構造的に保証 |
| CDATA-002 | (テスト免除: Ubiquitous) | `Name { get; }` / `Attributes => _attributes` (`IReadOnlyDictionary`) で構造的に保証 |
| CDATA-003 | `Given_有効なnameと属性_When_コンストラクタ_Then_Nameが入力と同じ` / `Given_有効なnameと属性_When_コンストラクタ_Then_Attributesに同じキー値が含まれる` | 1 テスト 1 アサーション原則のため Name と Attributes を分離 |
| CDATA-004 | `Given_存在するキー_When_HasAttribute_Then_trueを返す` | |
| CDATA-005 | `Given_存在しないキー_When_HasAttribute_Then_falseを返す` | |
| CDATA-006 | `Given_存在するキー_When_GetAttribute_Then_格納値を返す` | |
| CDATA-007 | `Given_存在しないキー_When_GetAttribute_Then_defaultValueを返す` | |
| CDATA-008 | `Given_順序の異なる同じキー値ペア_..._Then_等価` / `Given_異なるName_..._Then_非等価` / `Given_異なる属性値_..._Then_非等価` / `Given_両方空辞書の同名CardData_..._Then_等価` / `Given_単一属性で同じキー値_..._Then_等価` / `Given_属性キー数が異なる_..._Then_非等価` / `Given_同数だが異なるキー名_..._Then_非等価` / `Given_同一インスタンス_..._Then_等価` / `Given_null_When_EqualsCardData_Then_falseを返す` | n=0/n=1/n=2 のサイズ網羅 + 不一致 4 種(Name 異 / 値異 / Count 不一致 / キー名不一致) + ReferenceEquals 短絡 + null 引数 |
| CDATA-009 | `Given_等価な2つのCardData_When_GetHashCode_Then_同じ値を返す` | |
| CDATA-010 | `Given_生成後にソース辞書の既存キーを変更_..._Then_変更されない` / `Given_生成後にソース辞書に新キーを追加_..._Then_新キーが含まれない` | 防御コピーの 2 観点(既存値・新キー)を分離 |
| CDATA-011 | `Given_null_..._コンストラクタのname_..._ArgumentException` / `Given_空文字列_...` / `Given_空白のみ_...` | |
| CDATA-012 | `Given_null_When_コンストラクタのattributes_Then_ArgumentNullExceptionを投げる` | |
| CDATA-013 | `Given_nullキーを含むattributes_...` / `Given_空文字列キーを含むattributes_...` / `Given_空白のみキーを含むattributes_...` | null/empty/whitespace の 3 ケース |
| CDATA-014 | `Given_nullキー_When_HasAttribute_Then_ArgumentNullException` / `Given_nullキー_When_GetAttribute_Then_ArgumentNullException` | |
| CDATA-015 | `Given_等価な2つのCardData_..._operator_等価_Then_true` / `Given_非等価な2つのCardData_..._operator_非等価_Then_true` / `Given_非等価な2つのCardData_..._operator_等価_Then_false` / `Given_両方null_..._operator_等価_Then_true` / `Given_片方nullで他方非null_..._operator_等価_Then_false` | == の true/false + != の true + 両 null + 片 null の 5 ケース |
| CDATA-016 | (テスト免除: Optional) | `Dictionary<TKey, TValue>` 標準の last-write-wins 挙動に委譲。要件として明示し将来の方針変更時のトレーサビリティを確保 |
| CDATA-017 | `Given_null_When_Equalsオブジェクト_Then_falseを返す` / `Given_異なる型のオブジェクト_When_Equalsオブジェクト_Then_falseを返す` | |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
