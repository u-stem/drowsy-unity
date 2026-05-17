# `ScriptableObjectCardCatalog`(M4-PR1 骨格)

ADR-0012 §2 で確定した DrowZzz 向け `ICardCatalog<IEffect>` の **SO 実装の骨格**。本 PR(M4-PR1)範囲では **カードデータ登録** + **`Get` / `TryGet` / `OnValidate` 重複検出** を実装し、効果列対応は **M4-PR2 / PR3 でひな形拡張**(`GetEffects` は本 PR では空配列固定)。

> **注(ADR-0018 関連)**: `ICardCatalog<IEffect>` の lookup key は ADR-0018 で `CardId` から `CardTypeId` に変更された。本 spec 内の `Get(CardId.Of(...))` 表記および INF-045〜047 の引用はすべて現在の実装では `CardTypeId.Of(...)` で呼び出される。下記コード snippet と要件文中の `CardId` 引数は `CardTypeId` と読み替える(M4-PR1 当時の歴史的記述として残置)。

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

    // ADR-0018 後の現行 API は CardTypeId 引数。
    public CardData Get(CardTypeId typeId);
    public bool TryGet(CardTypeId typeId, out CardData data);
    public IReadOnlyList<IEffect> GetEffects(CardTypeId typeId);  // 本 PR では空配列固定
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

## M4-PR4 追加要件:既存 3 カードの SO ↔ InMemory 同値性

ADR-0012 §6 で確定した「既存 3 カード(No.00 / No.01 / No.02)の SO 移行」を本 PR で **テスト内動的構築 のみ** で実装(実 `.asset` ファイル配置は M4-PR7 / M5 で Designer ワークフロー実証時に追加、JIT 確定 2026-05-13)。`ScriptableObjectCardCatalog + CardEntryAsset + EffectAsset[]` で構築した SO catalog と、`InMemoryCardCatalog`(Application.Tests で利用中)を **同じ CardId で 1 対 1 のカードデータ + 効果列で構築** し、両者の `GetEffects(id)` を `Is.EqualTo`(record auto-equals + wrapper の `Equals` override 順序保持シーケンス同値)で比較する。

評価軸(JIT 確定 2026-05-13、推奨案 a):

- **ToDomain 経由の `IEffect[]` が record 値同値**(InMemory が返す `IReadOnlyList<IEffect>` と SO 経由(`EffectAsset.ToDomain` × N)で再構築した `IEffect[]` が `Is.EqualTo`)
- 評価軸 b(`DrowZzzRule` 経由 end-to-end)は本 PR スコープ外:Application.Tests 既存 `CupOfThreatCardTests` / `GreenInvasionCardTests` / `DreamCardTests` が `InMemoryCardCatalog` 経由で end-to-end をカバー済、SO 表現の **構造同値** が本 PR の目的

実装順序(JIT 確定 2026-05-13、難易度低 → 高):

1. **No.01「コップ一杯の脅威」**:`TimeOfDayBranchEffect` 1 件(夜・朝 wrapper、`AdjustSdpEffect` / `DrawCardEffect` を nest)
2. **No.02「緑の侵攻」**:`ChoiceEffect` 1 件(2 分岐、`AdjustSdpEffect` / `RemoveInfluenceEffect` / `ApplyInfluenceEffect(PlayerInfluence)`)
3. **No.00「夢」**:4 effect 最上位(`AssociatableMarkerEffect` / `RequiresMinimumTotalPointsMarkerEffect(100)` / `UsageRestrictionMarkerEffect` / `TimeOfDayBranchEffect`)+ 夜効果に `KeywordedEffect([Frenzy, Instinct], EarlyWinTriggerEffect)` nest + 朝効果 `AdjustSdpEffect(Self, -80)`(M3-PR6 で確立した M3 全機構統合カード)

### `Get`(`CardData`)と `GetEffects`(`IEffect[]`)の同値性

- [INF-045] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"01"`「コップ一杯の脅威」, both `Get(CardTypeId.Of("01")).Name` and `GetEffects(CardTypeId.Of("01"))` shall equal the corresponding values from an `InMemoryCardCatalog` constructed with the same card data and effects.
  - 関連:M2-PR3、ADR-0009 §戦略示唆
  - SO 経路:`TimeOfDayBranchEffectAsset` の再帰 ToDomain → `TimeOfDayBranchEffect` 値同値
  - 検証粒度:M4-PR4 code-reviewer P-3 反映 2026-05-13、補足を bullet に分離
- [INF-046] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"02"`「緑の侵攻」, `Get(CardTypeId.Of("02")).Name` and `GetEffects(CardTypeId.Of("02"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:M2-PR5、ADR-0007 §1.5
  - SO 経路:`ChoiceEffectAsset` の 2 次元再帰 + `PlayerInfluenceAsset` 中間型経由 ToDomain
  - record 値同値:`ChoiceEffect.Branches` 順序保持シーケンス + `PlayerInfluence` auto-equals
- [INF-047] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"00"`「夢」, `Get(CardTypeId.Of("00")).Name` and `GetEffects(CardTypeId.Of("00"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:M3-PR6、ADR-0011 §6 / §7(M3 全機構統合カード)
  - SO 経路:4 最上位 marker / wrapper + nested `KeywordedEffectAsset([Frenzy, Instinct], EarlyWinTriggerEffectAsset)` + 朝効果 `AdjustSdpEffectAsset(Self, -80)`
  - record 値同値:wrapper override Equals + marker auto-equals + KeywordedEffect.Inner 再帰
- [INF-136] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"03"`「身体にいいもの」, `Get(CardTypeId.Of("03")).Name` and `GetEffects(CardTypeId.Of("03"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:Phase 2 完結後初の新規カード追加、`InfluenceConstants.Perpetual` 初導入
  - SO 経路:`TimeOfDayBranchEffectAsset` の夜・朝再帰 + 各分岐内 `AdjustSdpEffectAsset` ×2 + `ApplyInfluenceEffectAsset(PlayerInfluenceAsset(...Perpetual))` ×1
  - `DrowZzzCardCatalog.asset` 内 rid 採番(`.asset` 差分の可読性補足、code-reviewer W-2 反映 2026-05-17):
    - rid 900 = `TimeOfDayBranchEffectAsset`(`_nightEffects` = [901, 902, 903] / `_morningEffects` = [905, 906, 907])
    - rid 901 / 902 = 夜分岐の `AdjustSdpEffectAsset`(Self -20 / Opponent +5)
    - rid 903 = 夜分岐の `ApplyInfluenceEffectAsset`(`_influence._tickEffect.rid` = **904**)
    - **rid 904 = NightInfluence の tickEffect**(`AdjustSdpEffect(Self, +4)`、`_nightEffects` 配列直下ではなく `PlayerInfluenceAsset._tickEffect` のネスト内に存在)
    - rid 905 / 906 = 朝分岐の `AdjustSdpEffectAsset`(Self -10 / Opponent +5)
    - rid 907 = 朝分岐の `ApplyInfluenceEffectAsset`(`_influence._tickEffect.rid` = **908**)
    - **rid 908 = MorningInfluence の tickEffect**(`AdjustSdpEffect(Self, -6)`、同上ネスト内)
  - record 値同値:`TimeOfDayBranchEffect` 順序保持シーケンス + `PlayerInfluence` auto-equals(`RemainingCount = int.MaxValue` でも int 同値で正しく比較される)
- [INF-138] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"04"`「静寂を纏う」, `Get(CardTypeId.Of("04")).Name` and `GetEffects(CardTypeId.Of("04"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:ADR-0019 PR ②、`PlayCardAction.TargetCardId` + `ApplyTargetedRestrictionEffect` + `RestrictSpecificCardInfluenceEffect` 初導入
  - SO 経路:`TimeOfDayBranchEffectAsset` の夜・朝再帰 + 各分岐内 `AdjustSdpEffectAsset` ×2 + `ApplyTargetedRestrictionEffectAsset(Opponent, 2)` ×1
  - record 値同値:`TimeOfDayBranchEffect` 順序保持シーケンス + `ApplyTargetedRestrictionEffect` auto-equals(Target / RemainingCount)
  - 動的影響の TickEffect(`RestrictSpecificCardInfluenceEffect`)は SO 上は直接ノードを持たない(runtime に `PlayCardAction.TargetCardId.TypeId` から構築されるため、SO 同値性検証の対象外)
- [INF-140] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"05"`「喧騒を纏う」, `Get(CardTypeId.Of("05")).Name` and `GetEffects(CardTypeId.Of("05"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:2026-05-17、`StackHandCardOnDeckTopEffect` 初導入(自分手札 → 共通山札 top 押し込み戦術カード)
  - SO 経路:`TimeOfDayBranchEffectAsset` の夜・朝再帰 + 各分岐内 `AdjustSdpEffectAsset` ×2 + `StackHandCardOnDeckTopEffectAsset(Self)` ×1(最上位 2 件構成、No.04 と同パターン)
  - record 値同値:`TimeOfDayBranchEffect` 順序保持 + `StackHandCardOnDeckTopEffect` auto-equals(Source)
  - 動的部分(TargetCardId)は SO 上ノード不要(runtime に `PlayCardAction.TargetCardId` から決定、`ApplyTargetedRestrictionEffectAsset` と同パターン)
- [INF-142] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"06"`「牙の届かぬ領域」, `Get(CardTypeId.Of("06")).Name` and `GetEffects(CardTypeId.Of("06"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:2026-05-17、`DoubleBedDamageSdpInfluenceMarkerEffect` 初導入(ベッド破損 SDP 変動 2 倍化 marker)
  - SO 経路:`AdjustSdpEffectAsset` ×2 最上位 + `KeywordedEffectAsset([Frenzy], ApplyInfluenceEffectAsset(Opponent, PlayerInfluenceAsset(OwnPhaseStart, DoubleBedDamageSdpInfluenceMarkerEffectAsset, 4)))`
  - record 値同値:最上位 3 件順序保持 + `KeywordedEffect.Inner` 再帰 + `PlayerInfluence` auto-equals + `DoubleBedDamageSdpInfluenceMarkerEffect` フィールドなし record の auto-equals
- [INF-144] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"07"`「知恵の及ばぬ領域」, `Get(CardTypeId.Of("07")).Name` and `GetEffects(CardTypeId.Of("07"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:2026-05-17、`RemoveInvertBedDamageInfluenceEffect` 初導入 + `RestrictSpecificCardInfluenceEffect`(No.04 由来既存型)を CardTypeId 固定値で流用
  - SO 経路:`AdjustSdpEffectAsset` ×2 + `RemoveInvertBedDamageInfluenceEffectAsset(Opponent)` + `KeywordedEffectAsset([Frenzy], ApplyInfluenceEffectAsset(Opponent, PlayerInfluenceAsset(OwnPhaseStart, RestrictSpecificCardInfluenceEffectAsset("08"), 4)))`
  - record 値同値:最上位 4 件順序保持 + `KeywordedEffect.Inner` 再帰 + `RestrictSpecificCardInfluenceEffect.TargetCardTypeId` 一致(CardTypeId.Of("08") string 経路)
- [INF-145] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"08"`「廻るための知恵」, `Get(CardTypeId.Of("08")).Name` and `GetEffects(CardTypeId.Of("08"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:2026-05-17、`InvertBedDamageSdpInfluenceMarkerEffect` 初導入(ベッド破損 SDP 符号反転 marker、保有数奇偶判定)
  - SO 経路:`KeywordedEffectAsset([Instinct], ChoiceEffectAsset([branch1, branch2]))` 1 件最上位、各 branch 内 `AdjustSdpEffectAsset` + `ApplyInfluenceEffectAsset(_, PlayerInfluenceAsset(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffectAsset, Perpetual))`
  - record 値同値:`KeywordedEffect.Inner = ChoiceEffect` 再帰 + ChoiceEffect 2 次元順序保持 + `InvertBedDamageSdpInfluenceMarkerEffect` auto-equals + `Perpetual` (int.MaxValue) も int 同値で正しく比較
- [INF-147] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"09"`「強引過ぎる一手」, `Get(CardTypeId.Of("09")).Name` and `GetEffects(CardTypeId.Of("09"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:2026-05-17、`RestrictAllUsageAndAbandonInfluenceMarkerEffect` 初導入(`PlayCardAction` / `CounterAction` / `AbandonAction` 3 アクション illegal 化 marker、ADR-0020 と同 PR)
  - SO 経路:`AdjustSdpEffectAsset(Self, -10)` + `AdjustSdpEffectAsset(Opponent, 10)` + `ApplyInfluenceEffectAsset(Opponent, PlayerInfluenceAsset(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffectAsset, 1))` の最上位 3 件構成
  - record 値同値:最上位 3 件順序保持 + `PlayerInfluence` auto-equals + `RestrictAllUsageAndAbandonInfluenceMarkerEffect` フィールドなし record の auto-equals + RemainingCount=1(ADR-0020 後初の Marker 系 count=1 採用カード、SO 経路でも値 1 を保持)
- [INF-149] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"10"`「安直過ぎる一手」, `Get(CardTypeId.Of("10")).Name` and `GetEffects(CardTypeId.Of("10"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:2026-05-17、`RestrictDrawCardInfluenceMarkerEffect` 初導入(`DrawCardAction` illegal 化 marker、ADR-0021 と同 PR)
  - SO 経路:`AdjustSdpEffectAsset(Self, -10)` + `DamageBedEffectAsset(Opponent, 30)` + `ApplyInfluenceEffectAsset(Opponent, PlayerInfluenceAsset(OwnPhaseStart, RestrictDrawCardInfluenceMarkerEffectAsset, 1))` の最上位 3 件構成(±0 慣例で `AdjustSdpEffectAsset(Opponent, 0)` は省略)
  - record 値同値:最上位 3 件順序保持 + `PlayerInfluence` auto-equals + `RestrictDrawCardInfluenceMarkerEffect` フィールドなし record の auto-equals + RemainingCount=1 + `DamageBedEffect(Opponent, 30)`(`Percent=30` は `BedDamageRatePerSdp=5` の倍数で valid)
- [INF-151] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"11"`「機械仕掛けの冬将軍」, `Get(CardTypeId.Of("11")).Name` and `GetEffects(CardTypeId.Of("11"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:2026-05-17、`AdjustSdpByHandCountEffect` 初導入(動的計算 TickEffect:影響保有者の Hand.Count を Tick 時に取得して SDP-n)
  - SO 経路:`AdjustSdpEffectAsset(Self, -4)` + `AdjustSdpEffectAsset(Opponent, -8)` + `KeywordedEffectAsset([Frenzy], ApplyInfluenceEffectAsset(Opponent, PlayerInfluenceAsset(OwnPhaseStart, AdjustSdpByHandCountEffectAsset, Perpetual)))` の最上位 3 件構成
  - record 値同値:最上位 3 件順序保持 + `KeywordedEffect.Inner` 再帰 + `PlayerInfluence` auto-equals + `AdjustSdpByHandCountEffect` フィールドなし record の auto-equals + `Perpetual` (int.MaxValue)
- [INF-153] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"12"`「偽りの太陽」, `Get(CardTypeId.Of("12")).Name` and `GetEffects(CardTypeId.Of("12"))` shall equal the corresponding `InMemoryCardCatalog` values.
  - 関連:2026-05-17、`AdjustSdpAfterPlayCardEffect(int Delta)` / `AdjustSdpAfterAbandonEffect(int Delta)` 初導入(Reactive TickEffect、ADR-0022)+ `InfluenceTrigger` 拡張(`OnOwnPlayCardAfter` / `OnOwnAbandonAfter`)
  - SO 経路:`TimeOfDayBranchEffectAsset(nightEffects: 4 件 = AdjustSdp ×2 + ApplyInfluenceEffectAsset(Self, PlayerInfluenceAsset(OnOwnPlayCardAfter, AdjustSdpAfterPlayCardEffectAsset(-10), Perpetual)) + ApplyInfluenceEffectAsset(Self, PlayerInfluenceAsset(OnOwnAbandonAfter, AdjustSdpAfterAbandonEffectAsset(+5), Perpetual)), morningEffects: 2 件 = AdjustSdp ×2)` の 1 件最上位
  - record 値同値:`TimeOfDayBranchEffect.NightEffects` / `MorningEffects` の各順序保持 + 新規 InfluenceTrigger enum int 同値 + `AdjustSdpAfterPlayCardEffect.Delta` / `AdjustSdpAfterAbandonEffect.Delta` int 同値

### Application.Tests への影響(本 PR で変更なし)

Application.Tests 配下の `CupOfThreatCardTests` / `GreenInvasionCardTests` / `DreamCardTests` は **本 PR で一切変更しない**(ADR-0006 §4 Pure C# 哲学 + ADR-0012 §5 併存戦略):

- Application.Tests は引き続き `InMemoryCardCatalog` 経由で end-to-end テスト(`DrowZzzRule.Apply` 経路)を担う
- Infrastructure.Tests(本 PR で追加)は SO 経由の **構造同値検証** に専念する Ports & Adapters 整合
- 両者は同じ「3 カードの効果列定義」を共有(将来 M4-PR7 で実 .asset 配置時に SO 側を再ロード経路に切り替えても、本 PR で確立した同値性検証が回帰防御として機能)

### トレーサビリティ(M4-PR4 追加分)

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-045 | `CupOfThreatCardCatalogTests.Given_*` 2 件(`Get(Name)` 同値 + `GetEffects` IEffect[] 同値) | wrapper 1 段 + 非 wrapper 内側 |
| INF-046 | `GreenInvasionCardCatalogTests.Given_*` 2 件(`Get(Name)` 同値 + `GetEffects` IEffect[] 同値) | ChoiceEffect 2 次元 + PlayerInfluence 中間型 |
| INF-047 | `DreamCardCatalogTests.Given_*` 2 件(`Get(Name)` 同値 + `GetEffects` IEffect[] 同値) | M3 全機構統合(4 最上位 + nested KeywordedEffect 経路) |
| INF-136 | `GoodForBodyCardCatalogTests.Given_*` 2 件(`Get(Name)` 同値 + `GetEffects` IEffect[] 同値) | TimeOfDayBranchEffect + 各分岐内 ApplyInfluenceEffect(PlayerInfluence Perpetual) |
| INF-138 | `SoundOfSilenceCardCatalogTests.Given_*` 2 件(`Get(Name)` 同値 + `GetEffects` IEffect[] 同値) | TimeOfDayBranchEffect + 各分岐内 ApplyTargetedRestrictionEffect(Opponent, 2)、動的 TickEffect は SO 対象外 |
| INF-140 | `CommotionCardCatalogTests.Given_*` 2 件(`Get(Name)` 同値 + `GetEffects` IEffect[] 同値) | TimeOfDayBranchEffect + StackHandCardOnDeckTopEffect(Self) の最上位 2 件構成、動的 TargetCardId は SO 対象外 |
| INF-142 | `UntouchableRealmCardCatalogTests.Given_*` 2 件(`Get(Name)` 同値 + `GetEffects` IEffect[] 同値) | AdjustSdp ×2 + KeywordedEffect([Frenzy], ApplyInfluenceEffect(Opponent, BedDamage2x Influence)) の最上位 3 件構成 |
| INF-144 | `RealmBeyondWisdomCardCatalogTests.Given_*` 2 件 | AdjustSdp ×2 + RemoveInvertBedDamage + KeywordedEffect([Frenzy], ApplyInfluenceEffect(Opponent, RestrictCard08 Influence)) の最上位 4 件構成 |
| INF-145 | `CirculatingWisdomCardCatalogTests.Given_*` 2 件 | KeywordedEffect([Instinct], ChoiceEffect 2 分岐) 1 件最上位、各 branch 内 ApplyInfluence(_, Invert Perpetual Influence) |
| INF-147 | `ForcePlayCardCatalogTests.Given_*` 2 件 | AdjustSdp ×2 + ApplyInfluence(Opponent, ForcePlay Marker count=1) の最上位 3 件構成、ADR-0020 後初の count=1 Marker SO 経路 |
| INF-149 | `EasyPlayCardCatalogTests.Given_*` 2 件 | AdjustSdp(Self, -10) + DamageBed(Opponent, 30) + ApplyInfluence(Opponent, EasyPlay Marker count=1) の最上位 3 件構成、ADR-0021 同 PR / DamageBedEffect の SO 経路初の Marker と組み合わせ |
| INF-151 | `MechanicalWinterGeneralCardCatalogTests.Given_*` 2 件 | AdjustSdp ×2 + KeywordedEffect([Frenzy], ApplyInfluence(Opponent, AdjustSdpByHandCount Perpetual)) の最上位 3 件構成、動的計算 TickEffect の SO 経路初導入 |
| INF-153 | `FalseSunCardCatalogTests.Given_*` 2 件 | TimeOfDayBranchEffect 1 件最上位、夜 4 件(AdjustSdp ×2 + ApplyInfluence ×2 Reactive Perpetual)/朝 2 件(AdjustSdp ×2)、Reactive Influence(OnOwnPlayCardAfter / OnOwnAbandonAfter)SO 経路初導入 |

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
