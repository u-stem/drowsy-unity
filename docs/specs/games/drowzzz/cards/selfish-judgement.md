# カード No.16「自分勝手な審判」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 15 新規カード追加**(2026-05-17 オーナー JIT 確定)。**条件分岐 effect の初導入**(`ConditionalApplyOrClearInfluencesEffect`)で、対象プレイヤーの保有 Influences 件数で「2 以下なら自分の影響を背負わせる / 3 以上なら全消滅」と分岐する戦術カード。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.16 |
| 名前 | 自分勝手な審判 |
| CardTypeId | `"16"` |
| 初期山札枚数 | 1(オーナー JIT 確定 2026-05-17、レア枠、No.06/07/11 と同じ)|
| 効果構造 | `ChoiceEffect` 1 件最上位(2 分岐:選択1 / 選択2)|
| 新規導入概念 | `ConditionalApplyOrClearInfluencesEffect(SdpTarget Target, int Threshold, PlayerInfluence InfluenceToApply)`(条件分岐 effect:Count <= Threshold で Apply、> Threshold で Clear)|

## 効果

### 選択1(`Choice == 0`):御業 -8/+5、甲対象

- 自分の SDP が 8 減る(自己加速 = 有利)
- 相手の SDP が 5 増える(相手覚醒 = 攻撃)
- **甲(自分)の保有影響数で分岐**:
  - `Influences.Count <= 2`(0/1/2 件)→ 甲に本カードの影響(SDP-4 永続)を付与
  - `Influences.Count > 2`(3 件以上)→ 甲の **全 Influence を消滅**(Apply されない、排他)

### 選択2(`Choice == 1`):御業 +5/-8、乙対象

- 自分の SDP が 5 増える(自己ペナルティ = 不利)
- 相手の SDP が 8 減る(相手加速 = ?、反転攻撃)
- **乙(相手)の保有影響数で分岐**:
  - `Influences.Count <= 2` → 乙に本カードの影響を付与
  - `Influences.Count > 2` → 乙の **全 Influence を消滅**(Apply されない)

戦略的解釈:
- 選択1 は「自分が眠くなる + 相手が目覚める」+「自分の影響を整理(<=2 なら追加、>=3 なら一掃)」= **自己制御型**
- 選択2 は「自分が目覚める + 相手が眠くなる」+「相手の影響を弄る」= **相手制御型**
- 「3 以上で消滅」が **強力なリセット効果**:相手が大量の Influence を抱えた状態(No.12 偽りの太陽 Reactive + 他)を一掃可能、戦略的タイミングが鍵
- 「自分勝手な審判」フレーバー:状況によって自分か相手を一方的に審判する不公平な裁定

## 「御業」の用語注記

「御業」は人智を超えた力によって発動するものという位置づけで、高火力 SDP 変動全般の通称(過去 No.09/10/11/12 と同じ、オーナー JIT)。本カードでは選択1 / 選択2 とも SDP 変動 13 ポイント差(±8/±5)が「御業」と表現される根拠。

## カード固有「影響」(Apply 経路で付与)

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart`(影響保有者の自フェーズ開始時)|
| Tick 効果 | `AdjustSdpEffect(SdpTarget.Self, -4)`(保有者の SDP -4、固定 delta)|
| 残発動回数 | `InfluenceConstants.Perpetual`(int.MaxValue、永続、テキスト記述なし慣例)|
| Semantics | 存在時:保有者の自フェーズ開始時に SDP-4 が発動(継続加速デバフ)|

選択1 では「甲(自分)に付与 → 自分の SDP-4 が永続」(自己加速)、選択2 では「乙(相手)に付与 → 相手の SDP-4 が永続」(相手加速 = 相手有利、ただし「3 以上消滅」リセット効果との抱き合わせで使い所が読みづらい)。

## 「2 以下/3 以上」の境界と Count 定義

オーナー JIT 確定 2026-05-17:
- **Influence カウント**:`Influences[targetPlayerId].Count`(全 trigger:`OwnPhaseStart` / `OnOwnPlayCardAfter` / `OnOwnAbandonAfter`、Marker / Reactive 含めた `PlayerInfluence` の件数)
- **境界**:`Count <= 2`(0/1/2) → Apply、`Count > 2`(3/4/...) → Clear
- **数えるタイミング**:PlayCardAction 開始時の Influences 件数(本 effect 評価時点での `session.Influences[Target].Count`、本カードの他 effect は SDP 変動のみで Influences 不変)
- **Apply / Clear は排他**:3 以上で Clear した場合、本カードの影響は **付与されない**(オーナー JIT)

## Clear の範囲

「受けている影響をすべて消滅」(オーナー指示)= 対象プレイヤーの **全 Influence** を一括除去:
- 全 trigger(`OwnPhaseStart` / `OnOwnPlayCardAfter` / `OnOwnAbandonAfter`)
- Marker 系(`RestrictAllUsageAndAbandon` / `RestrictDrawCard` / `DoubleBedDamageSdp` / `InvertBedDamageSdp` 等)も含む
- Reactive 系(`AdjustSdpAfterPlayCardEffect` / `AdjustSdpAfterAbandonEffect`)も含む

→ 純粋に `session.Influences[targetId]` を空 list で置換する(`Array.Empty<PlayerInfluence>()`)。

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("16"), new CardData("自分勝手な審判", new Dictionary<string, int>()))

// 影響定義(両 Choice で同じ Influence、Self は Tick 時の current player = 保有者)
var OwnPhaseSdpMinus4Influence = new PlayerInfluence(
    InfluenceTrigger.OwnPhaseStart,
    new AdjustSdpEffect(SdpTarget.Self, -4),
    InfluenceConstants.Perpetual);

// effects 側(ChoiceEffect 1 件最上位、2 分岐)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("16"), new IEffect[]
{
    new ChoiceEffect(new IReadOnlyList<IEffect>[]
    {
        // 選択1: -8/+5、甲(Self)を条件分岐対象に
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -8),
            new AdjustSdpEffect(SdpTarget.Opponent, 5),
            new ConditionalApplyOrClearInfluencesEffect(SdpTarget.Self, 2, OwnPhaseSdpMinus4Influence),
        },
        // 選択2: +5/-8、乙(Opponent)を条件分岐対象に
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, 5),
            new AdjustSdpEffect(SdpTarget.Opponent, -8),
            new ConditionalApplyOrClearInfluencesEffect(SdpTarget.Opponent, 2, OwnPhaseSdpMinus4Influence),
        },
    }),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-359] [Ubiquitous] Card `"16"` shall be registered with name `"自分勝手な審判"` and a single top-level `ChoiceEffect` containing 2 branches as specified.

## 事象駆動要件 (Event-driven、選択1 = `Choice == 0`)

- [DZ-360] When Card `"16"` is played with `Choice == 0`, the resulting session shall reflect `SDP[A] -= 8` and `SDP[B] += 5`。
- [DZ-361] When Card `"16"` is played with `Choice == 0` while `A.Influences.Count == 0`, A's influence list shall gain `PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, -4), Perpetual)`(Count <= 2 の Apply 経路、境界 0)。
- [DZ-362] When Card `"16"` is played with `Choice == 0` while `A.Influences.Count == 2`, A's influence list shall gain the new influence(Count <= 2 の Apply 経路、境界 2)。
- [DZ-363] When Card `"16"` is played with `Choice == 0` while `A.Influences.Count == 3`, A's influence list shall be **cleared to empty**(Count > 2 の Clear 経路、本カード影響も付与されない)。

## 事象駆動要件 (Event-driven、選択2 = `Choice == 1`)

- [DZ-364] When Card `"16"` is played with `Choice == 1`, the resulting session shall reflect `SDP[A] += 5` and `SDP[B] -= 8`。
- [DZ-365] When Card `"16"` is played with `Choice == 1` while `B.Influences.Count == 0`, B's influence list shall gain the new influence(乙 Apply 経路)。
- [DZ-366] When Card `"16"` is played with `Choice == 1` while `B.Influences.Count == 3`, B's influence list shall be **cleared to empty**(乙 Clear 経路)。

## 他プレイヤー保護(非リグレッション)

- [DZ-367] When Card `"16"` is played with `Choice == 0` while `B.Influences.Count == 5`, B's influence list shall remain unchanged(本 effect は Target = Self のため、B には触らない)。
- [DZ-368] When Card `"16"` is played with `Choice == 1` while `A.Influences.Count == 5`, A's influence list shall remain unchanged(Target = Opponent のため、A には触らない)。

## 定数依存

- **L3(カード設計値、本カード固有)**:
  - 選択1 SDP:Self -8 / Opponent +5
  - 選択2 SDP:Self +5 / Opponent -8
  - Threshold:`2`(Count <= 2 で Apply、> 2 で Clear)
  - 影響 RemainingCount:`InfluenceConstants.Perpetual`
  - 影響 Tick delta:`-4`(`AdjustSdpEffect(Self, -4)`)

## 関連

- ADR:
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.3「EffectInterpreter」(条件分岐 effect の switch パターン)+ §1.5「継続影響」(影響 Clear の意味論)
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md)
- 既存類似カード:
  - [`./cup-of-threat.md`](./cup-of-threat.md)(No.01、ChoiceEffect 同パターン)
  - [`./circulating-wisdom.md`](./circulating-wisdom.md)(No.08、Perpetual Influence 同パターン)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/ConditionalApplyOrClearInfluencesEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case + `ApplyConditionalApplyOrClearInfluences` ヘルパー)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/ConditionalApplyOrClearInfluencesEffectAsset.cs`(新規、PlayerInfluenceAsset 中間型経由)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.16 entry + rid 1200〜1209)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「16 → 17 種」)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/SelfishJudgementCardTests.cs`(新規、DZ-360〜368)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/SelfishJudgementCardCatalogTests.cs`(新規、SO 同等性、INF-160)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/EffectJsonConverterTests.cs`(INF-161、Round-Trip)
- シナリオ: `selfish-judgement.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-359 | (テスト免除: Ubiquitous) | catalog 登録は SelfishJudgementCardTests + CatalogTests で構造保証 |
| DZ-360 | `Given_任意_When_Choice0プレイ_Then_自分SDPマイナス8` + `Then_相手SDPプラス5` | 2 件に分割 |
| DZ-361 | `Given_甲影響0件_When_Choice0プレイ_Then_甲にSDPMinus4Influenceが追加` | Apply 経路 境界 0 |
| DZ-362 | `Given_甲影響2件_When_Choice0プレイ_Then_甲の影響が3件に増える` | Apply 経路 境界 2 |
| DZ-363 | `Given_甲影響3件_When_Choice0プレイ_Then_甲の影響が空になる` | Clear 経路 境界 3 |
| DZ-364 | `Given_任意_When_Choice1プレイ_Then_自分SDPプラス5` + `Then_相手SDPマイナス8` | 2 件に分割 |
| DZ-365 | `Given_乙影響0件_When_Choice1プレイ_Then_乙にSDPMinus4Influenceが追加` | 乙 Apply 経路 |
| DZ-366 | `Given_乙影響3件_When_Choice1プレイ_Then_乙の影響が空になる` | 乙 Clear 経路 |
| DZ-367 | `Given_乙影響5件_When_Choice0プレイ_Then_乙の影響は不変` | 他プレイヤー保護 (Self target) |
| DZ-368 | `Given_甲影響5件_When_Choice1プレイ_Then_甲の影響は不変` | 他プレイヤー保護 (Opponent target) |
