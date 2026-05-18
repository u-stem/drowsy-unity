# ADR-0025: PlayOrAbandonBranchEffect 導入 — カード固有の放棄効果機構

- Status: Accepted
- Date: 2026-05-18
- Decider: -

---

## Context

カード No.20「至上の喜び」(オーナー JIT 確定 2026-05-18)が以下の仕様を持つ:

- **「御業」+20/-20**:プレイ時、Self SDP +20 / Opponent SDP -20
- **甲(プレイ時付与影響)**:この手段が持つ影響を Self に付与(カウント 1)= `RestrictAllUsageAndAbandonInfluenceMarkerEffect`(No.09「強引過ぎる一手」と同じ Marker、ADR-0020)
- **「御業・放棄」+4/+6**:放棄時、Self SDP +4 / Opponent SDP +6
- **影響**:存在時、手段の使用や放棄ができない(自爆カード = プレイ後の次フェーズで自分が固まる)

このうち「プレイ時効果」「放棄時効果」が **同じカード effects 列内に併存** する構造は既存にない:

- 既存 `AbandonAction` の処理(M3-PR3、ADR-0011 §2)は固定 2 択(`AbandonChoice` enum):
  - `GainSdp`:手札 1 枚を捨てて SDP +5
  - `RepairBed`:手札 1 枚を捨てて Bed -20%
- カード自身の効果列 (`catalog.GetEffects(typeId)`) は **`PlayCardAction` 経路でのみ評価** されており、`AbandonAction` 経路では参照されない

「放棄時のカード固有効果」を実現する経路がなく、新規機構が必要。

### 表現方法の選択肢

| 案 | 概要 | 評価 |
| ---- | ---- | ---- |
| A | 新規 wrapper `PlayOrAbandonBranchEffect(PlayEffects, AbandonEffects)` を carrier として効果列に 1 件含める。`PlayCardAction` 経路では `PlayEffects` を unwrap、`AbandonAction` 経路では `AbandonEffects` を unwrap | `TimeOfDayBranchEffect` / `ChoiceEffect` と同じ「rule 評価層で unwrap される wrapper」パターン、対称性が良い |
| B | 新規 marker `OnAbandonEffectMarker(IReadOnlyList<IEffect>)` を効果列に追加(プレイ時は no-op、放棄時に検出 + 内側を発動) | プレイ時効果が wrapper の外、放棄時効果が marker の内、と非対称 |
| C | `ICardCatalog` に `GetAbandonEffects(CardTypeId)` API を追加(カード定義時にプレイ用/放棄用を別フィールドで保持) | catalog API 拡張、`InMemoryCardCatalog` / `ScriptableObjectCardCatalog` 双方拡張、保守性低下 |

→ **案 A 採用**。`TimeOfDayBranchEffect`(夜 / 朝)/ `ChoiceEffect`(分岐選択)と同じ「2 つの効果列を carrier する wrapper」パターンで、対称性が高い。

### AbandonChoice との関係

オーナー JIT(2026-05-18)で「**AbandonChoice 選択後、カード固有効果も追加で発動**」と確定:

- `AbandonChoice.GainSdp` 選択時:SDP +5 を加算 → その後にカード固有放棄効果(本ケースでは Self+4 / Opp+6)も発動
- `AbandonChoice.RepairBed` 選択時:Bed -20% を適用 → その後にカード固有放棄効果も発動

つまり「カード固有効果が AbandonChoice を上書きする」のではなく「AbandonChoice + カード固有効果の **両方が発動する累積モデル**」。

### 「御業」の位置づけ

カード No.20 テキストの「御業」「御業・放棄」は **カードテキスト上の分類タグ(フレーバー)**。`Keyword` enum 拡張は不要(将来 consumer が増えた時点で再評価、No.18 PR で確立した「キーワード化判断」と整合)。

## Decision

### 1. 新規 wrapper `PlayOrAbandonBranchEffect`

```csharp
public sealed record PlayOrAbandonBranchEffect : IEffect
{
    public IReadOnlyList<IEffect> PlayEffects { get; init; }
    public IReadOnlyList<IEffect> AbandonEffects { get; init; }

    // 順序保持シーケンス Equals / GetHashCode override(TimeOfDayBranchEffect 同パターン)
    // PlayEffects と AbandonEffects 双方 1 件以上必須(空 list 拒否)
    // null / null 要素は構築時 ArgumentNullException / ArgumentException
}
```

評価方針:
- `EffectInterpreter.Apply` には届かない(`ChoiceEffect` / 一部 marker と同じ「rule 評価層で unwrap される」設計)
- `EffectInterpreter` の `switch` に **no-op case を追加** + `default` case の例外メッセージで「`PlayOrAbandonBranchEffect` は rule 経由でのみ unwrap される設計」を明示

### 2. `DrowZzzRule.ApplyPlayCard` の unwrap 経路

既存 effects walk の中に `PlayOrAbandonBranchEffect` ケース追加:

```
foreach (var effect in rawEffects)
{
    if (effect is PlayOrAbandonBranchEffect po)
    {
        foreach (var inner in po.PlayEffects)
        {
            currentSession = _interpreter.Apply(currentSession, inner, context);
        }
    }
    else if (...) // 既存 IsReuseEffectMarker / ChoiceEffect / etc.
    else
    {
        currentSession = _interpreter.Apply(currentSession, effect, context);
    }
}
```

### 3. `DrowZzzRule.ApplyAbandon` の追加発動経路

既存 `ApplyAbandonGainSdp` / `ApplyAbandonRepairBed` 適用 **後**、本ターンで放棄したカードの effects 列を catalog から取得し、`PlayOrAbandonBranchEffect.AbandonEffects` を順次 `EffectInterpreter.Apply`:

```
session = ApplyAbandonChoice(session, action.Choice, action.CardIndex);
// ADR-0025: AbandonChoice 適用後にカード固有放棄効果を発動
var abandonedTypeId = abandonedCardId.TypeId;
var effects = _catalog.GetEffects(abandonedTypeId);
foreach (var effect in effects)
{
    if (effect is PlayOrAbandonBranchEffect po)
    {
        foreach (var inner in po.AbandonEffects)
        {
            session = _interpreter.Apply(session, inner, EffectContext.Default);
        }
    }
}
```

走査スコープは **最上位 effects のみ**(wrapper 内側は再帰しない、ADR-0024 `HasFirstPlayerAssociationEffectInTopLevel` と同方針)。

### 4. 永続化 / SO

| 観点 | 確定 |
| ---- | ---- |
| `EffectJsonConverter` | `"PlayOrAbandonBranch"` discriminator(`"playEffects": [...]`, `"abandonEffects": [...]` の 2 配列、`TimeOfDayBranchEffect` の `nightEffects` / `morningEffects` と同パターン)|
| `PlayOrAbandonBranchEffectAsset`(新規 SO) | `[SerializeReference] List<EffectAsset> _playEffects` + `_abandonEffects`、`ToDomain()` で `IEffect[]` 再構築 |
| `PersistedSessionV1` schemaVersion | bump 不要(新規 effect 追加のみ)|

## Consequences

### 正

- `TimeOfDayBranchEffect` と同じ wrapper パターンで対称性が高く、将来「プレイ時 / 放棄時」以外の二択分岐(例:夜プレイ用 / 朝プレイ用、自プレイ / 相手プレイ等)が必要になっても同パターンで派生可能
- 既存カード(No.00〜19)への影響なし(本 wrapper を使うカードのみ新経路を通る、他はそのまま)
- AbandonChoice との関係が「両方発動」で明確、`AbandonAction` 仕様が単純化(カード効果有無で挙動分岐しない)

### 負

- `DrowZzzRule.ApplyAbandon` に catalog 依存が新たに発生(本 PR 以前は catalog 不要だった、`ApplyPlayCard` と同じ catalog 経路で問題なし)
- 「プレイ効果のみのカード」「プレイ + 放棄効果のカード」の 2 形式が併存し、効果列定義の表現方法が増える(`PlayOrAbandonBranchEffect` で wrap するかしないかの判断が必要)
- `AbandonAction` 経路の状態遷移が増える(`AbandonChoice` 後の追加 EffectInterpreter 連鎖)

### 中立

- Phase 3 候補:
  - プレイ時 / 放棄時の効果列に **両方 null 許容**(放棄時のみ効果を持つカード等)
  - `ChoiceEffect` を `PlayEffects` 内にネストした場合、`_interpreter.Apply` に `ChoiceEffect` が直接渡されて `NotImplementedException` が発生する(`EffectInterpreter` は `ChoiceEffect` を rule 評価層 unwrap 専用として扱うため interpreter 経路に case がない)。対応が必要な場合は `ApplyPlayCard` の `PlayOrAbandonBranchEffect` unwrap ループ内に `ChoiceEffect` 専用 case を別途追加する必要がある(現 No.20 はこの経路を踏まないため実害なし、code-reviewer W-1 反映 2026-05-18)
  - `PlayOrAbandonBranchEffect.AbandonEffects` の中で更に `ApplyInfluenceEffect` が含まれる場合の OriginEffects 動的注入(ADR-0023 §7、本 PR ではテスト範囲外)

## Related

- ADR:
  - [ADR-0011](./0011-m3-dream-card-and-game-mechanics-expansion.md) §2「放棄(代替ターン行動)」(本 ADR で拡張する基盤)
  - [ADR-0007](./0007-m2-detail-card-effects.md) §1.5 / `TimeOfDayBranchEffect`(本 ADR の wrapper パターン参照元)
  - [ADR-0020](./0020-influence-count-decrement-timing.md)(`RestrictAllUsageAndAbandonInfluenceMarkerEffect` count=1 機能化、本カードの「甲」効果が同 Marker を使用)
- 実装(本 PR、`feat/card-no20-supreme-joy`):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/PlayOrAbandonBranchEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(no-op case + default メッセージ更新)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyPlayCard` PlayEffects unwrap + `ApplyAbandon` AbandonEffects unwrap)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/PlayOrAbandonBranchEffectAsset.cs`(新規 SO)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(`PlayOrAbandonBranch` discriminator 追加)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.20 entry + rid 5600 系)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント 20→21 種)
- 仕様(本 PR):
  - `docs/specs/games/drowzzz/cards/supreme-joy.md` / `.feature`
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/SupremeJoyCardTests.cs`(新規)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/SupremeJoyCardCatalogTests.cs`(新規、INF-165)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/EffectJsonConverterTests.cs`(round-trip 追加)
