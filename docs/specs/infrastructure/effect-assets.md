# Effect Assets(M4-PR2:`EffectAsset` 基底 + `AdjustSdpEffectAsset`)

ADR-0012 §3 で確定した `IEffect` の SO 表現を支える **`[Serializable]` POCO + 変換層** の最初の実装(M4-PR2)。本 PR では `EffectAsset` 基底 abstract class + `AdjustSdpEffectAsset`(最初の派生型)を導入し、`ScriptableObjectCardCatalog.GetEffects` を SO ベースで本格化する。残り 11 派生型(`DrawCardEffect` / `TimeOfDayBranchEffect` / `ChoiceEffect` 等)は **M4-PR3** で順次対応。

## 概要

| 観点 | 値 |
| ---- | ---- |
| 採用方式 | **案 (a) `[Serializable]` POCO + 変換層**(JIT 確定 2026-05-13、ADR-0012 §3) |
| 配置 namespace | `Drowsy.Infrastructure.Games.DrowZzz.Effects` |
| 基底型 | `public abstract class EffectAsset` with `[Serializable]` + `abstract IEffect ToDomain()` |
| 派生型(本 PR)| `public sealed class AdjustSdpEffectAsset : EffectAsset`(`SdpTarget` + `int` の 2 フィールド) |
| カード側保有 | `CardEntryAsset` に `[SerializeReference] EffectAsset[] _effects` フィールド追加 |
| Catalog 経路 | `ScriptableObjectCardCatalog.GetEffects(id)` が `_effectsCache` から `IReadOnlyList<IEffect>` を返す(`RebuildCache` 時に `ToDomain()` 集計) |
| `ToDomain()` 失敗時 | `ArgumentException` を catch → skip + `Debug.LogError`(`InMemoryCardCatalog` graceful degradation と同パターン)|
| `SerializeReference` null 要素 | skip + `Debug.LogError`(Unity の missing reference 復元防御)|

## 本 PR で確定した JIT 項目(2026-05-13)

ADR-0012 §「M4-PR2 着手時の JIT 確認項目」を本 PR で全 4 項目確定:

| 項目 | 確定内容 |
| ---- | ---- |
| `IEffect` の SO 表現方式 | **案 (a) `[Serializable]` POCO + 変換層**(`EffectAsset` abstract class + 派生 sealed class + `ToDomain()` 変換)|
| `EffectAsset` 基底 class の API | **`abstract IEffect ToDomain()`**(`Infrastructure → Application` ストリート / Ports & Adapters、`CardEntryAsset.ToCardData()` と同じ「To[Domain object]」命名パターン)|
| 第一号 SO 対応 effect | **`AdjustSdpEffect`**(最もシンプル、`SdpTarget` enum + `int` の 2 フィールド、wrapper なし、Marker でない、`ADR-0012 §9` 記載通り)|
| `[SerializeReference]` Unity バージョン | Unity 6(2022.2+ で安定)で問題なく利用可能、本 PR で前提化 |

## 構造

### `EffectAsset`(基底 abstract class)

```csharp
namespace Drowsy.Infrastructure.Games.DrowZzz.Effects
{
    [Serializable]
    public abstract class EffectAsset
    {
        /// <summary>本 SO 表現を Application 層の <see cref="IEffect"/> ドメインモデルに変換する。</summary>
        public abstract IEffect ToDomain();
    }
}
```

### `AdjustSdpEffectAsset`(派生 sealed class、最初の SO 対応 effect)

```csharp
[Serializable]
public sealed class AdjustSdpEffectAsset : EffectAsset
{
    [SerializeField] private SdpTarget _target;
    [SerializeField] private int _delta;

    public SdpTarget Target => _target;
    public int Delta => _delta;

    internal AdjustSdpEffectAsset(SdpTarget target, int delta);  // テスト用

    public override IEffect ToDomain() => new AdjustSdpEffect(_target, _delta);
}
```

### `CardEntryAsset` 拡張

```csharp
[SerializeReference] private EffectAsset[] _effects;

public IReadOnlyList<EffectAsset> Effects => _effects ?? Array.Empty<EffectAsset>();

internal CardEntryAsset(
    string cardIdValue,
    string name,
    AttributeEntry[] attributes,
    EffectAsset[] effects = null);  // 末尾 default null で後方互換
```

### `ScriptableObjectCardCatalog.GetEffects` 本格化

`RebuildCache` で各 entry の `Effects` を walk して `ToDomain()` を呼び、`_effectsCache: Dictionary<CardId, IReadOnlyList<IEffect>>` に集計。`GetEffects(id)` は cache から取得して返す。空 / 未登録 id は `Array.Empty<IEffect>()`(INF-010 graceful 動作と同パターン)。

## 普遍要件 (Ubiquitous)

- [INF-013] [Ubiquitous] `EffectAsset` shall be an `abstract class` annotated with `[Serializable]`, declaring `public abstract IEffect ToDomain()`, in `Drowsy.Infrastructure.Games.DrowZzz.Effects` namespace.
- [INF-014] [Ubiquitous] `AdjustSdpEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `SdpTarget _target` and `int _delta` fields, declared in the same namespace.
- [INF-015] [Ubiquitous] `CardEntryAsset.Effects` shall be a `IReadOnlyList<EffectAsset>` property that returns `_effects ?? Array.Empty<EffectAsset>()`(null safety、空配列許容)。

## 事象駆動要件 (Event-driven)

### `AdjustSdpEffectAsset.ToDomain` の挙動

- [INF-016] When `AdjustSdpEffectAsset(target, delta).ToDomain()` is called, the method shall return `new AdjustSdpEffect(target, delta)`(値伝達:`Target` / `Delta` がそのまま渡される)。

### `ScriptableObjectCardCatalog.GetEffects` 本格化

- [INF-017] When `GetEffects(id)` is called with `id` registered and the corresponding `CardEntryAsset.Effects` contains 1+ valid `EffectAsset` 要素, the catalog shall return an `IReadOnlyList<IEffect>` whose elements are `EffectAsset.ToDomain()` evaluated **in declaration order**(0 番目の `EffectAsset` が 0 番目の `IEffect`)。

## 異常要件 (Unwanted)

### `[SerializeReference]` null 要素の skip

- [INF-018] When `CardEntryAsset.Effects` contains a `null` element(`[SerializeReference]` の missing reference 復元等)、the catalog's `RebuildCache` shall **skip** that element + call `Debug.LogError(message, this)` to report it. The other elements in the same `_effects` array shall be processed unaffected. **全要素が null だった場合は空配列(`Array.Empty<IEffect>()`)を返す**(`BuildEffectsFromAssets` の `list.Count == 0` フォールバック、M4-PR2 code-reviewer P-1 反映 2026-05-13)。

### `ToDomain()` 失敗(`ArgumentException` 系)の skip

- [INF-019] When an `EffectAsset.ToDomain()` call throws `ArgumentException`(派生 `ArgumentNullException` / `ArgumentOutOfRangeException` 含む)、the catalog's `RebuildCache` shall **skip** that element + call `Debug.LogError(message, this)`. The other elements and other entries shall be processed unaffected(`InMemoryCardCatalog` / 既存 `RebuildCache` の entry skip パターンと整合)。**全要素が ToDomain 失敗した場合も空配列を返す**(INF-018 と同じ `BuildEffectsFromAssets` の `list.Count == 0` フォールバック、M4-PR2 code-reviewer P-1 反映 2026-05-13)。**M4-PR3 で `KeywordedEffectAsset.Inner` null 経路を介した本格テストを追加し Optional マーカーを解除**(`docs/todo.md` の INF-019 TODO を完了済へ移動、M4-PR3 着手記録 2026-05-13)。

## M4-PR3 追加要件(残り 11 派生型 SO 対応 + wrapper 再帰 + 中間型 2 件)

### 中間型 (Ubiquitous structural)

- [INF-020] [Ubiquitous] `PlayerInfluenceAsset` shall be a `[Serializable] sealed class` with `InfluenceTrigger _trigger`, `[SerializeReference] EffectAsset _tickEffect`, `int _remainingCount` fields, declaring `PlayerInfluence ToDomain()`, in the same namespace.
- [INF-021] When `PlayerInfluenceAsset(trigger, tickEffectAsset, remainingCount).ToDomain()` is called, the method shall return `new PlayerInfluence(trigger, tickEffectAsset.ToDomain(), remainingCount)`(`TickEffect` の再帰 ToDomain と `PlayerInfluence` ctor 防御に値を渡す)。
- [INF-022] [Ubiquitous] `EffectBranchAsset` shall be a `[Serializable] sealed class` with `[SerializeReference] EffectAsset[] _effects` field, declaring `IReadOnlyList<EffectAsset> Effects` null-safe property(`ChoiceEffectAsset` の 2 次元配列を中間型で表現するための回避策、Unity が 2D 配列を直接シリアライズ不可)。

### 非 wrapper 8 派生型(Ubiquitous structural + ToDomain 値伝達)

| Asset 派生型 | Ubiquitous | ToDomain 値伝達 |
| ---- | ---- | ---- |
| `DrawCardEffectAsset` | [INF-023] | [INF-024] |
| `ApplyInfluenceEffectAsset` | [INF-025] | [INF-026] |
| `RemoveInfluenceEffectAsset` | [INF-027] | [INF-028] |
| `EarlyWinTriggerEffectAsset` | [INF-029] | [INF-030] |
| `DamageBedEffectAsset` | [INF-031] | [INF-032] |
| `AssociatableMarkerEffectAsset` | [INF-033] | [INF-034] |
| `RequiresMinimumTotalPointsMarkerEffectAsset` | [INF-035] | [INF-036] |
| `UsageRestrictionMarkerEffectAsset` | [INF-037] | [INF-038] |

- [INF-023] [Ubiquitous] `DrawCardEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `SdpTarget _target` and `int _count` fields.
- [INF-024] When `DrawCardEffectAsset(target, count).ToDomain()` is called, the method shall return `new DrawCardEffect(target, count)`(値伝達)。
- [INF-025] [Ubiquitous] `ApplyInfluenceEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `SdpTarget _target` and `PlayerInfluenceAsset _influence` fields.
- [INF-026] When `ApplyInfluenceEffectAsset(target, influenceAsset).ToDomain()` is called, the method shall return `new ApplyInfluenceEffect(target, influenceAsset.ToDomain())`(`Influence` の再帰 ToDomain)。
- [INF-027] [Ubiquitous] `RemoveInfluenceEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `SdpTarget _target` field.
- [INF-028] When `RemoveInfluenceEffectAsset(target).ToDomain()` is called, the method shall return `new RemoveInfluenceEffect(target)`。
- [INF-029] [Ubiquitous] `EarlyWinTriggerEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with no fields(marker)。
- [INF-030] When `EarlyWinTriggerEffectAsset().ToDomain()` is called, the method shall return `new EarlyWinTriggerEffect()`。
- [INF-031] [Ubiquitous] `DamageBedEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `SdpTarget _target` and `int _percent` fields.
- [INF-032] When `DamageBedEffectAsset(target, percent).ToDomain()` is called, the method shall return `new DamageBedEffect(target, percent)`(`Percent` の 5 の倍数 / 正値検証は record 側)。
- [INF-033] [Ubiquitous] `AssociatableMarkerEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with no fields(marker)。
- [INF-034] When `AssociatableMarkerEffectAsset().ToDomain()` is called, the method shall return `new AssociatableMarkerEffect()`。
- [INF-035] [Ubiquitous] `RequiresMinimumTotalPointsMarkerEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `int _threshold` field.
- [INF-036] When `RequiresMinimumTotalPointsMarkerEffectAsset(threshold).ToDomain()` is called, the method shall return `new RequiresMinimumTotalPointsMarkerEffect(threshold)`(`Threshold >= 1` は record 側で検証)。
- [INF-037] [Ubiquitous] `UsageRestrictionMarkerEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with no fields(marker)。
- [INF-038] When `UsageRestrictionMarkerEffectAsset().ToDomain()` is called, the method shall return `new UsageRestrictionMarkerEffect()`。

### wrapper 3 派生型(Ubiquitous structural + 再帰 ToDomain)

- [INF-039] [Ubiquitous] `TimeOfDayBranchEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `[SerializeReference] EffectAsset[] _nightEffects` and `_morningEffects` fields.
- [INF-040] When `TimeOfDayBranchEffectAsset(night[], morning[]).ToDomain()` is called, the method shall return `new TimeOfDayBranchEffect(nightDomain[], morningDomain[])` where each domain element is `EffectAsset.ToDomain()` evaluated in declaration order. null 要素は `ArgumentNullException` を投げる(INF-018 の wrapper 内側 null 経路、上位 catalog で graceful skip)。
- [INF-041] [Ubiquitous] `ChoiceEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `[SerializeField] EffectBranchAsset[] _branches` field.
- [INF-042] When `ChoiceEffectAsset(branches[]).ToDomain()` is called, the method shall return `new ChoiceEffect(IReadOnlyList<IReadOnlyList<IEffect>>)` reconstructed from each `EffectBranchAsset.Effects` element's `ToDomain()`. `Branches.Count >= 2` は record 側で検証、null 要素は wrapper 同様 `ArgumentNullException` で伝播。
- [INF-043] [Ubiquitous] `KeywordedEffectAsset` shall be a `[Serializable] sealed class` inheriting from `EffectAsset` with `[SerializeField] Keyword[] _keywords` and `[SerializeReference] EffectAsset _inner` fields.
- [INF-044] When `KeywordedEffectAsset(keywords[], inner).ToDomain()` is called, the method shall return `new KeywordedEffect(keywords, inner.ToDomain())`. `Inner` が null の場合は `ArgumentNullException` を投げる(本経路が **INF-019 の Optional 解除を支える本格テスト** の対象)。

## 定数依存

該当なし。本 PR で数値定数を導入しない(`AdjustSdpEffect` の `Delta` はカードごとに異なる L3 個別カード設計値、定数化対象外)。

## 関連

- ADR: [`docs/adr/0012-m4-scriptableobject-and-persistence.md`](../../adr/0012-m4-scriptableobject-and-persistence.md) §3(SO 表現方式)/ §「M4-PR2 着手時の JIT 確認項目」
- 前提: [`card-catalog.md`](card-catalog.md)(`ScriptableObjectCardCatalog` 骨格、M4-PR1)
- 前提 effect: `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AdjustSdpEffect.cs`(M2-PR3、ADR-0007 §1.4)/ `SdpTarget.cs`(M2-PR3、同上)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/EffectAsset.cs`(新規、基底)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/AdjustSdpEffectAsset.cs`(新規、派生)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/CardEntryAsset.cs`(`_effects` フィールド追加、ctor 4 引数化)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/ScriptableObjectCardCatalog.cs`(`_effectsCache` 追加、`RebuildCache` 拡張、`GetEffects` 本格化)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Effects/AdjustSdpEffectAssetTests.cs`(新規)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/ScriptableObjectCardCatalogTests.cs`(`GetEffects` 経路テスト追加)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-013 | (テスト免除: Ubiquitous) | abstract class + [Serializable] + abstract method 構造 |
| INF-014 | (テスト免除: Ubiquitous) | sealed class + EffectAsset 継承 + 2 フィールド構造 |
| INF-015 | (テスト免除: Ubiquitous) | プロパティ null-safe 性は `Given_effects未指定_When_Effects_Then_空配列を返す` で構造的保証 |
| INF-016 | `Given_SelfMinus5_When_ToDomain_Then_AdjustSdpEffectSelfMinus5` + `Given_Opponent10_When_ToDomain_Then_AdjustSdpEffectOpponent10` | 2 ケース(`SdpTarget.Self` / `Opponent` × `Delta` の値伝達) |
| INF-017 | `Given_CardEntryに2effect_When_GetEffects_Then_IEffect配列を順序保持で返す` | 順序保証 |
| INF-018 | `Given_Effects配列にnull要素_When_GetEffects_Then_null要素skip_他要素は影響なし` | SerializeReference null 防御(LogAssert.Expect で Debug.LogError マッチ) |
| INF-019 | `ScriptableObjectCardCatalogTests.Given_KeywordedEffectAssetのInnerがnull_When_GetEffects_Then_skip + LogError` | M4-PR3 で Optional マーカー解除、`KeywordedEffectAsset.Inner` null 経路で本格テスト |
| INF-020 | (テスト免除: Ubiquitous) | structural |
| INF-021 | `PlayerInfluenceAssetTests.Given_*_When_ToDomain_Then_PlayerInfluenceを返す` | 値伝達 + 再帰 TickEffect |
| INF-022 | (テスト免除: Ubiquitous) | structural |
| INF-023〜INF-037 (奇数番号 8 件) | (テスト免除: Ubiquitous) | 各 sealed class + EffectAsset 継承構造 |
| INF-024 | `DrawCardEffectAssetTests.Given_*` 2 件 | 値伝達 |
| INF-026 | `ApplyInfluenceEffectAssetTests.Given_*` 1 件(再帰 PlayerInfluenceAsset) | 値伝達 + 再帰 |
| INF-028 | `RemoveInfluenceEffectAssetTests.Given_*` 2 件 | 値伝達 |
| INF-030 | `EarlyWinTriggerEffectAssetTests.Given_*` 1 件 | 値伝達(空) |
| INF-032 | `DamageBedEffectAssetTests.Given_*` 2 件 | 値伝達(5 の倍数検証は record 側) |
| INF-034 | `AssociatableMarkerEffectAssetTests.Given_*` 1 件 | 値伝達(空 marker) |
| INF-036 | `RequiresMinimumTotalPointsMarkerEffectAssetTests.Given_*` 2 件 | 値伝達 |
| INF-038 | `UsageRestrictionMarkerEffectAssetTests.Given_*` 1 件 | 値伝達(空 marker) |
| INF-039 | (テスト免除: Ubiquitous) | structural |
| INF-040 | `TimeOfDayBranchEffectAssetTests.Given_*` 2 件 | 夜・朝 + 再帰 ToDomain |
| INF-041 | (テスト免除: Ubiquitous) | structural |
| INF-042 | `ChoiceEffectAssetTests.Given_*` 1 件 | 2 次元再帰 ToDomain(中間型 EffectBranchAsset 経由) |
| INF-043 | (テスト免除: Ubiquitous) | structural |
| INF-044 | `KeywordedEffectAssetTests.Given_*` 2 件(値伝達 + Inner null 防御) | 再帰 ToDomain + INF-019 本格化経路 |
