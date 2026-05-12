# `KeywordedEffect`(M3-PR5a)

ADR-0011 §4 で確定したキーワード能力の付与方式を実装するラッパー record。`TimeOfDayBranchEffect` / `ChoiceEffect` と同パターンで、既存 effect を wrap する形でキーワードを付与する。

## 構造

```csharp
public sealed record KeywordedEffect : IEffect
{
    public IReadOnlyList<Keyword> Keywords { get; init; }
    public IEffect Inner { get; init; }
    public KeywordedEffect(IReadOnlyList<Keyword> keywords, IEffect inner);
    public bool HasKeyword(Keyword keyword);
}
```

| プロパティ | 不変条件 |
| ---- | ---- |
| `Keywords` | null 不可、1 件以上必須(空 list での wrap は無意味、`ChoiceEffect.Branches.Count >= 2` と同パターン)|
| `Inner` | null 不可 |
| `with` 式での null 代入 | `ArgumentNullException` で二重ガード(positional ctor と init setter の両経路)|

## 評価ロジック(`EffectInterpreter.Apply`)

| 入力 | 結果 |
| ---- | ---- |
| `KeywordedEffect(keywords, inner)` | `Inner` を `context` 込みで再帰的に Apply する(Keywords は判別用属性、評価時に副作用なし)|

**注**: Counter キーワードを持つ効果が `PlayCardAction` 経路で評価された場合の skip 機構は M3-PR5b で実装される。M3-PR5a 範囲では `KeywordedEffect.Apply` は常に Inner を Apply する(skip 判定は呼び出し側の責務、interpreter は意味論上 Inner を Apply)。

## 普遍要件 (Ubiquitous)

- [DZ-210.0] [Ubiquitous] `KeywordedEffect` は `sealed record` で `(IReadOnlyList<Keyword> Keywords, IEffect Inner)` を持ち、`IEffect` を実装する。`Keywords` / `Inner` は null 不可、positional ctor / `with` 式の両経路で `ArgumentNullException` で防御される。`Keywords.Count == 0` は `ArgumentException`。

## 構造要件

- [DZ-210] When `KeywordedEffect(keywords, inner)` is constructed:
  - `keywords == null` → `ArgumentNullException`
  - `inner == null` → `ArgumentNullException`
  - `keywords.Count == 0` → `ArgumentException`(1 件以上必須)
  - `with { Keywords = null }` / `with { Inner = null }` → `ArgumentNullException`
- [DZ-210] Two `KeywordedEffect` instances are value-equal iff their `Keywords` sequences are identical (order-preserving) and their `Inner` values are equal(`record` auto-equals を override、`TimeOfDayBranchEffect` / `ChoiceEffect` と同パターン)。

## 判定要件

- [DZ-211] `KeywordedEffect.HasKeyword(Keyword k)` returns `true` iff `Keywords` contains `k`(順序非依存マルチセット判定、線形検索)。

## 評価要件

- [DZ-212] When `KeywordedEffect(keywords, inner)` is evaluated by `EffectInterpreter.Apply(session, _, context)`:
  - The result shall equal `EffectInterpreter.Apply(session, inner, context)`(Inner の意味論をそのまま継承、Keywords は副作用なし)
  - For nested `KeywordedEffect(_, KeywordedEffect(_, terminalEffect))`, evaluation recurses until reaching the non-`KeywordedEffect` innermost effect

## 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| `Keywords.Count >= 0` 許容(空 list でも合法) | 「空 keywords で wrap」は意味的に普通の effect と区別がつかず、設計意図が不明瞭になる(`ChoiceEffect.Branches.Count >= 2` と同じ「無意味な wrap を防ぐ」防御方針)|
| `Keywords` を `HashSet<Keyword>` 等の集合型で表現 | 順序保持シーケンス同値で扱う方が他 wrapper(`ChoiceEffect` / `TimeOfDayBranchEffect`)と一貫、序数で keyword の優先順位を表現する将来拡張にも対応 |
| `Inner` を `IReadOnlyList<IEffect>` で持つ(複数効果を 1 wrap で表現)| 単一 wrapper の semantic を保つため不採用。複数効果をまとめてキーワード付与したい場合は `KeywordedEffect([k], TimeOfDayBranchEffect(...))` 等の組み合わせで表現 |

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4
- 関連: [`../keyword-abilities.md`](../keyword-abilities.md)(キーワード能力全体)/ [`../abandon.md`](../abandon.md)(Instinct を含むカードの CardIndex 除外)/ [`time-of-day-branch.md`](time-of-day-branch.md)(同じく wrapper パターン)/ [`choice-effect.md`](choice-effect.md)(同じく wrapper パターン)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/Keyword.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/KeywordedEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加、Inner を Apply)
- テスト(本 PR): `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/KeywordedEffectTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-210 | null/empty 防御 5 件(Keywords null / Inner null / 空 Keywords / with Keywords null / with Inner null)+ 値同値 2 件(同一値 EqualTo / 順序違い NotEqualTo) | 構造的不変条件 |
| DZ-211 | `HasKeyword` 判定 3 件(Instinct true / Frenzy false / Counter false with multi-keyword)| 線形検索の判定 |
| DZ-212 | Apply 意味論 3 件(inner AdjustSdp で SDP 変化 / inner AssociatableMarker で session 不変 / nested で最内まで再帰)| Inner 逐次評価 |
