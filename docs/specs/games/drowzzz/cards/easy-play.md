# カード No.10「安直過ぎる一手」 (Phase 2 完結後、ADR-0021 と同 PR)

DrowZzz Phase 2 完結後の **第 8 新規カード追加**(2026-05-17 オーナー JIT 確定)。No.09「強引過ぎる一手」と **対をなすカード**:「御業」-10 の自己コスト + 乙にベッド 30% 破損 + カウント1 の「ドロー禁止」Marker を付与する戦術カード。本カードのカウント1 「ドロー禁止」Marker + No.09 既存「使用・放棄禁止」Marker により、PhaseState 進行不能化(stuck)問題が顕在化し、ADR-0021 で EndTurnAction の全フェーズ合法化条件(stuck 化 Marker 保有時の脱出弁)を確定した(本 PR 同梱)。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.10 |
| 名前 | 安直過ぎる一手 |
| CardTypeId | `"10"` |
| 初期山札枚数 | 2(オーナー JIT 確定 2026-05-17、Phase 3 本物デッキ。M5 簡易デッキでは uniform 20 維持。No.09 と同じく対 No.09 ペア前提の希少枠)|
| 効果構造 | 最上位 3 件:`AdjustSdpEffect(Self, -10)` + `DamageBedEffect(Opponent, 30)` + `ApplyInfluenceEffect(Opponent, EasyPlayInfluence)` |
| 新規導入概念 | `RestrictDrawCardInfluenceMarkerEffect`(`DrawCardAction` を illegal 化する Marker)|

## 効果

時間帯非依存(夜・朝両方で同じ効果、`ChoiceEffect` / `TimeOfDayBranchEffect` なし):

- 自分の SDP が 10 減る(「御業」=高火力、自分が眠くなる方向に大きく振る自己コスト)
- 相手の SDP は ±0(コード上は `AdjustSdpEffect(Opponent, 0)` を省略、No.00 / No.08 と同じ慣例)
- 相手のベッドを 30% 破損(`DamageBedEffect(Opponent, 30)`、相手の次の自フェーズ開始時に SDP -6 自動発動)
- 相手にこのカード固有の **影響(カウント 1)** を付与

戦略的解釈:
- 自分 SDP -10 のコストを払い、相手のベッド 30% 破損 +「次の自フェーズで山札ドロー禁止」を仕掛ける。**手札補充を 1 フェーズ封じる + ベッド破損 SDP -6 確定** の二段攻撃カード
- No.09 が「乙の手段使用 / 放棄を封じる(攻撃手段の封鎖)」のに対し、No.10 は「乙の手札補充を封じる(リソース供給の封鎖)」。両者は対をなす設計で、両方を同時に乙に保有させると乙はそのターン実質ターンスキップ(連想と EndTurn のみ可)になる
- 「安直」というフレーバー通り、相手の判断機会を「次に出すカードがない」状態にして消極化させる、シンプルだが強力な戦術

## 「御業」-10/±0 の用語注記

「御業」は人智を超えた力によって発動するものという位置づけで、火力の高い SDP 変動全般の通称(オーナー JIT 共有 2026-05-17、No.09 と同じフレーバー語)。本カードでは `AdjustSdpEffect(Self, -10)` 単体が「御業」と表現される根拠。`±0` は「相手 SDP に変動なし」の意で、コード上は `AdjustSdpEffect(Opponent, 0)` を省略する(No.00「夢」朝効果「-80 / ±0」、No.08「廻るための知恵」各 Choice branch の片方 ±0 と同じ慣例)。

## カード固有「影響」

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart`(影響保有者の自フェーズ開始時)|
| Tick 効果 | `RestrictDrawCardInfluenceMarkerEffect`(session 不変、判別用 marker)|
| 残発動回数 | **1**(1 フェーズ寿命、ADR-0020 後の Marker 系 count=1 セマンティクス、No.09 と同じ短期効果)|
| Semantics | **存在時:`DrawCardAction` を `IsLegalMove` で illegal 化**。`AssociateAction` / `EndTurnAction`(ADR-0021 で stuck 化時 全フェーズ合法化)/ `PlayCardAction` / `AbandonAction` / `CounterAction` は本 marker 単独では制限しない |

### 禁止対象アクションの詳細

| アクション | 禁止可否 | 判定経路 |
| ---- | ---- | ---- |
| `DrawCardAction` | **不可** | `DrowZzzRule.IsLegalDraw` 内で current プレイヤーの Influences を walk して `HasRestrictDrawCardInfluence` true なら false |
| `PlayCardAction` | 可 | 本 marker は影響しない(No.09 `RestrictAllUsageAndAbandonInfluenceMarkerEffect` と別)|
| `AbandonAction` | 可 | 同上 |
| `CounterAction` | 可 | 同上 |
| `AssociateAction` | 可 | 連想は自ターン中常に合法(本 marker 影響外)|
| `EndTurnAction` | **常時可**(ADR-0021)| `IsLegalEndTurn` で「`WaitingForEndTurn` OR `HasAnyStuckCausingInfluence`」、本 marker 保有時は全フェーズ合法化(stuck 脱出弁)|

### 進行不能化(stuck)回避と ADR-0021

`DrawCardAction` を illegal にすると、乙の `WaitingForDraw` フェーズで PhaseState を進める手段が **連想(`AssociateAction`、`TotalPoints >= 80` 必須かつ PhaseState を進めない)以外なくなる** ため進行不能化の問題が発生。

ADR-0021 で `EndTurnAction` を「stuck 化 Marker(`RestrictAllUsageAndAbandon` / `RestrictDrawCard`)を保有時のみ全フェーズで合法化」する変更を行い、本 marker 保有時に `WaitingForDraw` でも `EndTurnAction` を打てるようにすることで脱出弁を確立。

### No.09 との同時保有時の挙動

ADR-0017 N=2 ホットシート想定で、A が同一フェーズに No.09(乙=B に使用・放棄禁止)と No.10(乙=B にドロー禁止)を両方プレイした場合、B は次フェーズで以下のみ可能:
- `AssociateAction`(TotalPoints >= 80 + 連想可能カードが Field にあれば)
- `EndTurnAction`(ADR-0021 で stuck 状態のため全フェーズ合法化、これがメインの脱出弁)

事実上「次の乙のターンはスキップ」と等価になり、戦略的に非常に強い妨害だがコスト(自分 SDP -20 + 自分の手札 2 枚消費)も大きいため、終盤狙いの戦術カードとしてバランスを保つ。

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("10"), new CardData("安直過ぎる一手", new Dictionary<string, int>()))

// 影響定義
var EasyPlayInfluence = new PlayerInfluence(
    InfluenceTrigger.OwnPhaseStart,
    new RestrictDrawCardInfluenceMarkerEffect(),
    1);

// effects 側(最上位 3 件、`AdjustSdpEffect(Opponent, 0)` は ±0 慣例で省略)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("10"), new IEffect[]
{
    new AdjustSdpEffect(SdpTarget.Self, -10),
    new DamageBedEffect(SdpTarget.Opponent, 30),
    new ApplyInfluenceEffect(SdpTarget.Opponent, EasyPlayInfluence),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-314] [Ubiquitous] Card `"10"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"安直過ぎる一手"` and the **3 top-level effects** specified above(`AdjustSdpEffect(Self, -10)` / `DamageBedEffect(Opponent, 30)` / `ApplyInfluenceEffect(Opponent, EasyPlayInfluence)`)。

## 事象駆動要件 (Event-driven)

- [DZ-315] When Card `"10"` is played by player A on player B, the resulting session shall reflect `SDP[A] -= 10`(時間帯非依存)。
- [DZ-316] When Card `"10"` is played by player A on player B, the resulting session shall reflect `BedDamages[B] += 30`(時間帯非依存、`DamageBedEffect(Opponent, 30)` の効果)。
- [DZ-317] When Card `"10"` is played by player A on player B, B's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, RestrictDrawCardInfluenceMarkerEffect, 1)` entry.

## 合法性判定(`IsLegalMove` 拡張、ADR-0021 と同 PR)

- [DZ-318] When B holds `PlayerInfluence(OwnPhaseStart, RestrictDrawCardInfluenceMarkerEffect, 1)` and B tries to perform `DrawCardAction` during `WaitingForDraw`, `IsLegalMove` shall return `false`(山札からの手段ドロー禁止)。
- [DZ-319] When B holds the marker influence and B tries to perform `EndTurnAction` during **any non-counter PhaseState**(`WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn`)、`IsLegalMove` shall return `true`(ADR-0021:stuck 化 Marker 保有時の全フェーズ合法化、脱出弁)。`WaitingForCounterResponse` のみ例外で false(`PendingCounteredEffects` 強制破棄回避、ADR-0021 §「`WaitingForCounterResponse` の例外扱い」)。
- [DZ-320] When B holds the marker influence and B tries to perform `PlayCardAction` / `AbandonAction` / `CounterAction` / `AssociateAction`, `IsLegalMove` shall return its **normal value**(本 marker は他の 4 アクションを制限しない、No.09 とは別概念)。

## ライフサイクル / Tick 評価のシナリオ(ADR-0020 後の count=1 Marker、ADR-0021 後の stuck 脱出弁)

- [DZ-321] When A's EndTurn rotates current to B after A played Card `"10"`(B が本 marker count=1 保有開始), B's influence shall remain with `RemainingCount = 1` during B's entire phase(B Tick は count 不変、`IsLegalDraw` で本 marker walk が機能)。
- [DZ-322] When B's own EndTurn fires while B holds the marker influence with `RemainingCount = 1`, `DecrementInfluencesForCurrentPlayer(B)` shall remove the marker from B's influences (1 → 0 で除去)。次の A 自フェーズ開始時には本 marker は存在しない。

## 定数依存

- **L3(カード設計値、本カード固有)**:
  - 自分 SDP 即時変動: -10
  - 相手 BedDamage 即時変動: +30 (%)
  - 影響 RemainingCount: 1(1 フェーズ寿命、ADR-0020 後の Marker 系 count=1 セマンティクス)
- **L2(既存)**:
  - `DrowZzzBedConstants.BedDamageRatePerSdp = 5`(`DamageBedEffect.Percent` の倍数制約、30 は 5 の倍数で valid)
- **L1**: 該当なし

## 関連

- ADR:
  - [`docs/adr/0021-endturn-stuck-escape-valve.md`](../../../../adr/0021-endturn-stuck-escape-valve.md)(本 PR 同梱、本カードの「ドロー禁止」Marker 導入で顕在化した stuck 化問題への対応)
  - [`docs/adr/0020-influence-count-decrement-timing.md`](../../../../adr/0020-influence-count-decrement-timing.md)(count=1 Marker 機能化、本カードでも前提)
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §3「ベッド破損」(`DamageBedEffect` の根拠)
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/damage-bed.md`](../effects/damage-bed.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md)
- 前提効果(本 PR で実装、spec md 未作成):`RestrictDrawCardInfluenceMarkerEffect`(`Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RestrictDrawCardInfluenceMarkerEffect.cs` 参照、xmldoc に Design 記述あり)
- 既存類似カード:
  - [`./force-play.md`](./force-play.md)(No.09「強引過ぎる一手」、対をなすカード:使用・放棄禁止 vs ドロー禁止)
  - [`./untouchable-realm.md`](./untouchable-realm.md)(No.06、`DamageBedEffect` 既存採用、Frenzy + ベッド破損 2 倍化)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RestrictDrawCardInfluenceMarkerEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`HasRestrictDrawCardInfluence` / `HasAnyStuckCausingInfluence` 新設 + `IsLegalDraw` / `IsLegalEndTurn` 新設 + `IsLegalMove` の DrawCardAction / EndTurnAction 経路の差し替え + `ApplyEndTurn` の防御的検証を `IsLegalEndTurn` ベースに変更)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(no-op marker case)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/RestrictDrawCardInfluenceMarkerEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.10 entry + rid 980〜983 追加)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「10 → 11 種」)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/EasyPlayCardTests.cs`(新規、DZ-315〜322)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/EasyPlayCardCatalogTests.cs`(新規、SO ↔ InMemory 同値性、INF-149)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/EffectJsonConverterTests.cs`(INF-150、Round-Trip)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/ForcePlayCardTests.cs`(No.09 関連、DZ-311 第 2 で `WaitingForPlay` からの EndTurn 合法化を追加)
- シナリオ: `easy-play.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-314 | (テスト免除: Ubiquitous) | catalog 登録は `EasyPlayCardTests` のヘルパー + `EasyPlayCardCatalogTests` で構造的に保証 |
| DZ-315 | `Given_任意フェーズ_When_Card10をプレイ_Then_自分のSDPがマイナス10` | 統合テスト |
| DZ-316 | `Given_任意フェーズ_When_Card10をプレイ_Then_相手のBedDamageがプラス30` | 統合テスト |
| DZ-317 | `Given_任意フェーズ_When_Card10をプレイ_Then_相手のInfluencesにEasyPlayMarkerが付与される` | 統合テスト |
| DZ-318 | `Given_p2が本Markerカウント1保有_When_p2がDrawCardActionでIsLegalMove_Then_false` | illegal 化(DrawCardAction)|
| DZ-319 | `Given_p2が本Markerカウント1保有_WaitingForDraw_When_p2がEndTurnActionでIsLegalMove_Then_true` | stuck 脱出弁(ADR-0021)|
| DZ-320 | `Given_p2が本Markerカウント1保有_When_p2がPlayCardActionでIsLegalMove_Then_本Markerは影響せず通常判定` | 他 4 アクション制限なし |
| DZ-321 | `Given_p2が本Markerカウント1保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluencesはカウント1で残存` | ADR-0020 後の count=1 Marker は p2 フェーズで機能 |
| DZ-322 | `Given_p2が本Markerカウント1保有_p2current_When_p2EndTurn_Then_p2のInfluences件数が0` | p2 自身の EndTurn 冒頭 Decrement で除去 |
