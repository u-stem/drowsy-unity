# カード No.04「静寂を纏う」 (Phase 2 完結後、ADR-0019 PR ②)

DrowZzz Phase 2 完結後の **第 2 新規カード追加**(2026-05-17 オーナー JIT 確定)。**「相手の手札から 1 枚選択して使用禁止にする」** という DrowZzz 初の対人介入カード。**連想由来カードは選択不可** という制約を honor するため、ADR-0019 PR ① で先行整備した `DrowZzzGameSession.AssociatedCardIds` を初の consumer として利用する。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.04 |
| 名前 | 静寂を纏う |
| CardTypeId | `"04"` |
| 初期山札枚数 | **2**(Phase 3 本物デッキ、M5 簡易デッキでは uniform 20 維持。オーナー追加共有 2026-05-17、PR #116 マージ後修正)|
| 効果構造 | **2 件**:(1) `TimeOfDayBranchEffect`(時間帯依存の SDP 変動のみ)+ (2) `ApplyTargetedRestrictionEffect(Opponent, 2)`(時間帯非依存、最上位配置)|
| 新規導入概念 | `RestrictSpecificCardInfluenceEffect`(特定カード使用禁止 marker)/ `ApplyTargetedRestrictionEffect`(動的影響付与)/ `PlayCardAction.TargetCardId`(相手手札選択)|

## 効果

プレイ時、現在時刻に応じて以下の効果が発動する。**actor=甲(自分) / target=乙(相手)** の単一効果(ChoiceEffect ではない、両方同時実行)。

### 夜(`Clock.IsNight`、Turn 1〜16)

- 自分の SDP が 12 減る
- 相手の SDP が 5 増える
- 自分(actor)が相手(target)の手札から **手段を 1 枚選択する**(`PlayCardAction.TargetCardId`)
  - **連想された手段は選択不可**(`AssociatedCardIds` に含まれる CardId は除外)
- 相手にこのカードの **影響を 2 カウント付与**(下記「影響」)

### 朝(`Clock.IsMorning`、Turn 17〜21)

- 自分の SDP が 5 増える
- 相手の SDP が 8 減る
- 自分(actor)が相手(target)の手札から手段を 1 枚選択する(夜と同じ)
- 相手にこのカードの **影響を 2 カウント付与**(下記「影響」)

## カード固有「影響」

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart`(影響保有者の自フェーズ開始時)|
| Tick 効果 | `RestrictSpecificCardInfluenceEffect(TargetCardTypeId)`(session 不変、`IsLegalPlayCard` で判別)|
| 残発動回数 | 2(2 フェーズ寿命、N=2 で「次の自フェーズ + その次の自フェーズ」で 2 回 Tick、3 度目で除去)|
| Semantics | **存在時:選択された手段(TargetCardId.TypeId)を使用できない** = `IsLegalPlayCard` で本 Influence を walk して TargetCardTypeId 一致のカードプレイを illegal 化 |

戦略的解釈:
- **夜**: 自分 SDP -12 で起きるコストを払い、相手の特定カードを 2 フェーズ封じる戦術カード。「夢」のような重要カードを封じる用途
- **朝**: 朝の戦略「相手を眠くさせる」に沿った相手 SDP +8 と、追加で特定カード封じ

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側 (CardData は名前のみ、属性は空)
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("04"), new CardData("静寂を纏う", new Dictionary<string, int>()))

// effects 側(最上位 2 件:時間帯依存の SDP 変動 + 時間帯非依存の動的影響付与)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("04"), new IEffect[]
{
    // (1) TimeOfDayBranchEffect:時間帯依存の SDP 変動のみ
    new TimeOfDayBranchEffect(
        nightEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -12),
            new AdjustSdpEffect(SdpTarget.Opponent, 5),
        },
        morningEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, 5),
            new AdjustSdpEffect(SdpTarget.Opponent, -8),
        }
    ),
    // (2) ApplyTargetedRestrictionEffect:時間帯非依存の動的影響付与(最上位、夜/朝両方で発動)
    new ApplyTargetedRestrictionEffect(SdpTarget.Opponent, 2),
})
```

### 構造上の設計判断:`ApplyTargetedRestrictionEffect` を最上位に置く理由

オーナー仕様「甲乙の選択 + 影響付与」は夜/朝共通(時間帯非依存)であり、`TimeOfDayBranchEffect` 内に nested 配置するとカード意味と乖離する。
さらに `DrowZzzRule.IsLegalPlayCard` の効果列 walk は **最上位のみ**(M3-PR6 JIT 確定、`HasKeywordInEffect` と同方針)のため、nested 配置だと `ApplyTargetedRestrictionEffect` 検出経路が動作せず `TargetCardId` 必須検証 / 連想由来除外検証 / 相手手札所持検証が **静的に通って illegal 化されないバグ** になる(2026-05-17 PR ② 開発時に統合テスト 3 件失敗で発覚)。最上位配置で両方解決。

## 普遍要件 (Ubiquitous)

- [DZ-259] [Ubiquitous] Card `"04"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"静寂を纏う"` and a single `TimeOfDayBranchEffect` containing the night/morning effect lists specified above.

## 事象駆動要件 (Event-driven)

- [DZ-260] When Card `"04"` is played by player A on player B with `TargetCardId = c` while `session.Clock.IsNight` is `true`, the resulting session shall reflect `SDP[A] -= 12`.
- [DZ-261] When Card `"04"` is played by player A on player B with `TargetCardId = c` while `session.Clock.IsNight` is `true`, the resulting session shall reflect `SDP[B] += 5`.
- [DZ-262] When Card `"04"` is played by player A on player B with `TargetCardId = c` while `session.Clock.IsNight` is `true`, B's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(c.TypeId), 2)` entry.
- [DZ-263] When Card `"04"` is played by player A on player B with `TargetCardId = c` while `session.Clock.IsMorning` is `true`, the resulting session shall reflect `SDP[A] += 5`, `SDP[B] -= 8`, and B's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(c.TypeId), 2)` entry.

## 合法性判定(`IsLegalMove` 拡張)

- [DZ-264] When Card `"04"` is played without specifying `TargetCardId`(= null), `IsLegalMove` shall return `false`(ApplyTargetedRestrictionEffect 含むカードは TargetCardId 必須)。
- [DZ-265] When Card `"04"` is played with `TargetCardId = c` where `c` is **not in the opponent's hand**, `IsLegalMove` shall return `false`(対象カード不在の選択は無効)。
- [DZ-266] When Card `"04"` is played with `TargetCardId = c` where `c` is contained in `session.AssociatedCardIds`(連想由来), `IsLegalMove` shall return `false`(連想由来カードは選択不可、ADR-0019)。

## Tick 評価 / 使用禁止検証のシナリオ

- [DZ-267] When B holds `PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(c.TypeId), 2)` and B tries to play any card with `CardTypeId == c.TypeId`, `IsLegalMove` shall return `false`(使用禁止 Influence による illegal 化、`RestrictSpecificCardInfluenceEffect` 自体は session 不変な marker)。
- [DZ-268] When B holds `PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(c.TypeId), 1)` and A's EndTurn rotates current to B, B's influence shall remain with `RemainingCount = 1`(ADR-0020:Tick は count 不変、B 自身の EndTurn で count -1 → 0 で除去)。これにより count=1 marker が B の自フェーズ全体で機能し、本 Influence walk で Card `c.TypeId` が illegal 化される。

## 定数依存

- **L3(カード設計値、本カード固有)**:
  - 夜 SDP 即時変動: 自分 -12 / 相手 +5
  - 朝 SDP 即時変動: 自分 +5 / 相手 -8
  - 影響 RemainingCount: 2(2 フェーズ寿命)

上記 L3 値はカード固有設計値のため定数集約しない(L3 = ゲームバランス調整可能値の方針、CLAUDE.md §9)。

## 関連

- ADR:
  - [`docs/adr/0019-associated-card-ids-session-field.md`](../../../../adr/0019-associated-card-ids-session-field.md) — PR ① 設計基盤(AssociatedCardIds)+ PR ② 本カード(Related で前方参照済)
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」
  - [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../../adr/0009-m2-m3-dp-and-victory-conditions.md) §「戦略示唆」 — 夜・朝戦略反転の根拠
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/time-of-day-branch.md`](../effects/time-of-day-branch.md)
- 前提効果(本 PR ② で実装あり / spec md は未作成):`RestrictSpecificCardInfluenceEffect`(`Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RestrictSpecificCardInfluenceEffect.cs` 参照、xmldoc に Design 記述あり)/ `ApplyTargetedRestrictionEffect`(同上)。Effect 単体仕様 md(`../effects/restrict-specific-card-influence.md` / `../effects/apply-targeted-restriction.md`)は将来 effect 単体仕様の網羅整理 PR で起票予定(code-reviewer P-3 反映 2026-05-17、現状は本カード spec + xmldoc が SoT)
- 前提モデル: [`../influences/influence-model.md`](../influences/influence-model.md)
- 既存類似カード:
  - [`./cup-of-threat.md`](./cup-of-threat.md)(No.01、時間帯分岐パターン)
  - [`./green-invasion.md`](./green-invasion.md)(No.02、Influence 付与パターン)
  - [`./good-for-body.md`](./good-for-body.md)(No.03、時間帯分岐 + 永続 Influence 付与)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RestrictSpecificCardInfluenceEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/ApplyTargetedRestrictionEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzAction.cs`(`PlayCardAction.TargetCardId` 拡張)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectContext.cs`(`TargetCardId` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalPlayCard` 拡張 + `HasSpecificCardRestrictionInfluence`)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(2 effect dispatch + `ApplyApplyTargetedRestriction`)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/RestrictSpecificCardInfluenceEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/ApplyTargetedRestrictionEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(2 dispatch case 追加)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/SoundOfSilenceCardTests.cs`
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/SoundOfSilenceCardCatalogTests.cs`
- シナリオ: `sound-of-silence.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-259 | (テスト免除: Ubiquitous) | catalog 登録は `SoundOfSilenceCardTests` のヘルパー + `SoundOfSilenceCardCatalogTests` で構造的に保証 |
| DZ-260 | `Given_夜のフェーズ_When_Card04をプレイ_Then_自分のSDPがマイナス12` | 統合テスト |
| DZ-261 | `Given_夜のフェーズ_When_Card04をプレイ_Then_相手のSDPがプラス5` | 統合テスト |
| DZ-262 | `Given_夜のフェーズ_When_Card04をプレイ_Then_相手のInfluencesに使用禁止が付与される` | 統合テスト |
| DZ-263 | `Given_朝のフェーズ_When_Card04をプレイ_Then_*` 3 件(SDP[A]+5 / SDP[B]-8 / Influence 付与)| 統合テスト |
| DZ-264 | `Given_TargetCardIdなし_When_Card04をIsLegalMove_Then_false` | IsLegalPlayCard 防御 |
| DZ-265 | `Given_相手手札にないTargetCardId_When_Card04をIsLegalMove_Then_false` | 同上 |
| DZ-266 | `Given_AssociatedCardIds含有のTargetCardId_When_Card04をIsLegalMove_Then_false` | 連想由来除外 |
| DZ-267 | `Given_使用禁止InfluencePlayer_When_対象カードをIsLegalMove_Then_false` | RestrictSpecificCardInfluenceEffect 経由 illegal 化 |
| DZ-268 | `Given_カウント1の使用禁止Influence_When_自フェーズTick_Then_カウント1で残存` | ADR-0020 後の count=1 marker は B フェーズで機能、B 自身の EndTurn で除去 |
