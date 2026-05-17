# カード No.00「夢」 (M3-PR6)

DrowZzz の **M3 完成 PR で導入される連想専用カード**(プロジェクトオーナー JIT 共有 2026-05-12 / 2026-05-14)。連想機構(ADR-0011 §1)・キーワード能力(ADR-0011 §4)・早期勝利機構(ADR-0010 §5)・時刻分岐(ADR-0008)を統合して 1 枚に表現するゲームメカニクスの集大成。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.00 |
| 名前 | 夢 |
| CardId | `"00"` |
| 初期山札枚数 | **0**(連想専用、`InMemoryCardCatalog` には登録するが initial deck には含めない) |
| 取得手段 | `AssociateAction(CardId.Of("00"))` 経由のみ(汎用連想機構を流用、ADR-0011 §1) |
| 効果構造 | 4 件:`AssociatableMarkerEffect` / `RequiresMinimumTotalPointsMarkerEffect(100)` / `UsageRestrictionMarkerEffect` / `TimeOfDayBranchEffect`(夜 = Frenzy+Instinct 付き `EarlyWinTriggerEffect`、朝 = `AdjustSdpEffect(Self, -80)`) |

## 本 PR で確定した JIT 項目(2026-05-14)

ADR-0011 §6 起票時点では「JIT 確認待ち」だった 4 項目を本 PR で全て確定する:

| 項目 | 確定内容 |
| ---- | ---- |
| 朝効果「-80 / ±0」の解釈 | **自分 -80 / 相手 ±0**(M2-PR3 / M2-PR5 慣例「自分 / 相手」順、`AdjustSdpEffect(SdpTarget.Self, -80)` 単体) |
| 「次の自分のターン以降」の実装方式 | **候補 C `PlayerInfluence` 流用**:連想時に自プレイヤーに `PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, RemainingCount=1)` を付与、次の自フェーズ開始時の Tick で 0 になり除去 |
| 連想条件と使用条件の境界記号 | **両方 inclusive(≥)に統一**:連想は `TotalPoints ≥ 80`(M3-PR4 で確定済)/ 使用は `TotalPoints ≥ 100`(`DrowZzzVictoryConstants.EarlyWinScoreThreshold` と同値で一貫) |
| 初期山札含む / 連想専用 | **連想専用**:`InMemoryCardCatalog` に登録するが initial deck には混入させない(連想機構 ADR-0011 §1 の「`ICardCatalog` から直接生成」semantics と整合) |

## 効果

「夢」は連想で手札に加え、**次の自分のフェーズ以降** に **持ち点 100 以上** で使用可能。プレイ時は現在時刻に応じて以下が発動する。

### 夜(`Clock.IsNight`、Round 1〜16)

- **狂乱(Frenzy)+ 本能(Instinct)付き `EarlyWinTriggerEffect`**:`Clock.IsNight` かつ `TotalPoints ≥ 100` で `Outcome = WinnerOutcome(現プレイヤー)` を設定(ADR-0010 §5、夜 + 持ち点閾値で早期勝利)
- 狂乱 → `CounterAction` で反撃を受けない(ADR-0011 §4.3、`DrowZzzRule.IsLegalCounter` で target に Frenzy 検出 → false)
- 本能 → 放棄(`AbandonAction`)の捨てる対象として選択不可(ADR-0011 §4.2、`DrowZzzRule.IsLegalAbandon` で Instinct 検出 → false)

### 朝(`Clock.IsMorning`、Round 17〜21)

- 自分の SDP が **80 減る**(`AdjustSdpEffect(SdpTarget.Self, -80)` 単体)
- 相手への影響なし(±0)

戦略的解釈:**夜に連想 → 次フェーズで FDS ≥ 100 状態を作って夢使用 → 早期勝利**が主要戦略。朝に使うと自分が大損するため、夜の早期勝利狙いが第一義。Round 17 以降に連想用条件(FDS ≥ 80)を整えても、夢を引いた次フェーズで使えるのは朝になり -80 を負う(戦略的に避ける状況)。

## カードデータ表現(`InMemoryCardCatalog` 登録形)

```csharp
// entries 側 (CardData は名前のみ、属性は空、initial deck には含めず catalog 登録のみ)
new KeyValuePair<CardId, CardData>(CardId.Of("00"), new CardData("夢", new Dictionary<string, int>()))

// effects 側 (4 つの effect を最上位 effect 列に並べる)
new KeyValuePair<CardId, IReadOnlyList<IEffect>>(CardId.Of("00"), new IEffect[]
{
    // (1) 連想可能カードであることを示すマーカー (ADR-0011 §1、M3-PR4 確立)
    new AssociatableMarkerEffect(),

    // (2) 使用条件:FDS ≥ 100 を要求 (本 PR 新設、PlayCardAction.IsLegalMove で walk 検出)
    new RequiresMinimumTotalPointsMarkerEffect(DrowZzzVictoryConstants.EarlyWinScoreThreshold),

    // (3) 連想後使用制限マーカー:AssociateAction.Apply で検出して自プレイヤーに Influence を付与
    //     (本 PR 新設、Influence の TickEffect として再利用される 2 役兼用 marker)
    new UsageRestrictionMarkerEffect(),

    // (4) 時刻分岐:夜 = 狂乱+本能付き EarlyWinTrigger、朝 = 自分 SDP -80
    new TimeOfDayBranchEffect(
        nightEffects: new IEffect[]
        {
            new KeywordedEffect(
                new[] { Keyword.Frenzy, Keyword.Instinct },
                new EarlyWinTriggerEffect()),
        },
        morningEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -80),
        }),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-228] [Ubiquitous] Card `"00"` shall be registered in the `InMemoryCardCatalog` test helper with name `"夢"` and the 4-effect list specified above(`AssociatableMarkerEffect` + `RequiresMinimumTotalPointsMarkerEffect(100)` + `UsageRestrictionMarkerEffect` + `TimeOfDayBranchEffect`). The initial deck factory shall **not** include Card `"00"`(連想専用、ADR-0011 §6 / 本 PR JIT 確定 2026-05-14)。
- [DZ-229] [Ubiquitous] Card `"00"` is a connectable card(連想可能カード):its effect list contains `AssociatableMarkerEffect`、so `AssociateAction(CardId.Of("00"))` is allowed when the threshold + phase conditions hold(M3-PR4 で確立した汎用連想機構を再利用)。

## 事象駆動要件 (Event-driven)

### 連想 → Influence 付与

- [DZ-230] When `AssociateAction(CardId.Of("00"))` is applied to a session whose current player holds `TotalPoints ≥ 80`, the resulting session shall reflect:
  - 自プレイヤーの `Hand` に `CardId.Of("00")` が末尾追加されている
  - 自プレイヤーの `Influences` に `PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, RemainingCount=1)` が末尾追加されている
  - `PhaseState` は不変(連想は割り込み式、ADR-0011 §1 / M3-PR4 確立)

### 使用制限の効果

- [DZ-231] When player A holds Card `"00"` in hand and `PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, _)` in influences, `PlayCardAction(CardId.Of("00"))` shall be **illegal**(`IsLegalMove` returns `false`)while phase is `WaitingForPlay`。
- [DZ-232] When player A's `EndTurnAction` rotates current player to B, then B's `EndTurnAction` rotates back to A, the `DecrementInfluencesForCurrentPlayer` step in A's first `EndTurnAction`(ADR-0020 後は `Turn.Next` 前に実行)shall decrement A's UsageRestriction influence `RemainingCount` from 1 to 0 and remove it. After A returns to its next own phase, `PlayCardAction(CardId.Of("00"))` shall become legal(同条件下で `IsLegalMove` → `true`、FDS ≥ 100 等他条件は別 DZ で担保)。ADR-0020 後の補足:旧仕様(M2-PR5)では A の 2 度目の自フェーズ開始時 Tick で count -1 で即除去だったが、新仕様では A の 1 度目の `EndTurnAction` 冒頭 Decrement で除去される。プレイヤー体験としては「翌々自フェーズで合法化される」点が共通(N+2 フェーズ目)で戦略的影響なし。

### 使用条件(FDS ≥ 100)

- [DZ-233] When `PlayCardAction(CardId.Of("00"))` is evaluated against a session whose current player holds `TotalPoints < 100` (and no `UsageRestrictionMarkerEffect` influence、つまり制限解除済の状態)、`IsLegalMove` shall return `false`(`RequiresMinimumTotalPointsMarkerEffect(100)` を最上位効果列に持つカードは閾値未満で illegal)。
- [DZ-234] When the same action is evaluated with `TotalPoints == 100`、`IsLegalMove` shall return `true`(inclusive 境界:`≥ 100` で発動可)。

### 夜効果(早期勝利)

- [DZ-235] When `PlayCardAction(CardId.Of("00"))` is applied during `Clock.IsNight`(`RoundNumber` 1〜16)by player A with `TotalPoints ≥ 100`(connectable 制限解除済)、`Apply` shall result in `Outcome == WinnerOutcome(A)`(夜 + FDS ≥ 100 で早期勝利、`EarlyWinTriggerEffect` 評価成立、ADR-0010 §5 / 本 ADR §7)。

### 朝効果(自 SDP -80)

- [DZ-236] When `PlayCardAction(CardId.Of("00"))` is applied during `Clock.IsMorning`(`RoundNumber` 17〜21)by player A、`Apply` shall result in `SDP[A] -= 80`、`Outcome == null`(朝は `EarlyWinTriggerEffect` が no-op、`AdjustSdpEffect(Self, -80)` のみ発動)。

### キーワード対応(統合確認)

- [DZ-237] When player A's Card `"00"` is targeted by player B's `CounterAction(_, CardId.Of("00"))` during `WaitingForCounterResponse`、`IsLegalMove` shall return `false`(夜効果列に Frenzy キーワード含む `KeywordedEffect` を持つため、ADR-0011 §4.3 / M3-PR5b の `HasKeywordInEffects(effects, Keyword.Frenzy)` 検出経路)。
- [DZ-238] When player A's `AbandonAction(_, CardIndex == index_of_dream)` targets Card `"00"` in hand、`IsLegalMove` shall return `false`(本能キーワード含む、ADR-0011 §4.2 / M3-PR5a の `HasKeywordInEffects(effects, Keyword.Instinct)` 検出経路)。

## Unwanted Behaviors

- (DZ-231 / DZ-233 で illegal-move 化を網羅、追加 Unwanted なし)

## 定数依存

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| `DrowZzzVictoryConstants.EarlyWinScoreThreshold` (= 100) | L2 | M3-PR1 で導入、夜の早期勝利閾値。夢の使用条件 FDS ≥ 100 と同値で **意図的に一致**(夜の早期勝利を狙う設計、JIT 確定 2026-05-14) |
| `DrowZzzAssociationConstants.AssociationThreshold` (= 80) | L2 | M3-PR4 で導入、連想閾値(夢を引くための条件) |

数値 `-80`(朝効果 SDP delta)は本カード固有値(JIT 確定 2026-05-12)で、定数集約しない(L3 個別カード設計値)。

数値 `1`(`DrowZzzRule.ApplyAssociate` 内で付与する `PlayerInfluence` の `RemainingCount` 初期値)は N=2 前提のハードコード(相手 1 フェーズ経由後の自フェーズ Tick で除去される設計、ADR-0011 §6 JIT 確定 2026-05-14)。N>2 拡張時に対応が必要で、`docs/todo.md` で追跡する(M3-PR6 code-reviewer W-4 / P-6 反映 2026-05-14)。

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §6 / §7(本カードのスコープ)/ §1(連想機構)/ §4(キーワード)
- 前提機構: [`../association.md`](../association.md)(連想)/ [`../keyword-abilities.md`](../keyword-abilities.md)(キーワード)/ [`../victory-conditions.md`](../victory-conditions.md)(早期勝利)
- 前提効果: [`../effects/associatable-marker-effect.md`](../effects/associatable-marker-effect.md) / [`../effects/early-win-trigger.md`](../effects/early-win-trigger.md) / [`../effects/time-of-day-branch.md`](../effects/time-of-day-branch.md) / [`../effects/keyworded-effect.md`](../effects/keyworded-effect.md) / [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md)
- 本 PR で新設する効果: [`../effects/requires-minimum-total-points-marker.md`](../effects/requires-minimum-total-points-marker.md) / [`../effects/usage-restriction-marker.md`](../effects/usage-restriction-marker.md)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RequiresMinimumTotalPointsMarkerEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/UsageRestrictionMarkerEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加、no-op × 2)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalPlayCard` 拡張 + `ApplyAssociate` 拡張)
- テスト(本 PR): `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DreamCardTests.cs`
- シナリオ: [`00-dream.feature`](00-dream.feature)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-228 | (テスト免除: Ubiquitous) | 登録構造は `DreamCardTests` ヘルパーで保証、initial deck 非含は別カードの DeckBuildTests 経由(本 PR では catalog 登録のみテスト) |
| DZ-229 | (テスト免除: Ubiquitous) | 効果列の構造的性質、`AssociateAction` の合法性は DZ-230 経路で実証 |
| DZ-230 | `Given_FDS80_When_夢を連想_Then_手札末尾に夢が追加される` + `Then_UsageRestriction影響が1件付与される` + `Then_付与影響のTickEffectがUsageRestrictionMarker` + `Then_付与影響のRemainingCountが1` + `Then_付与影響のTriggerがOwnPhaseStart` + `Then_PhaseStateは不変` | 6 件に分割(Medium)。Influence 件数だけでなく中身(TickEffect 型 / RemainingCount / Trigger)も検証(M3-PR6 code-reviewer W-1 反映 2026-05-14) |
| DZ-231 | `Given_UsageRestriction影響保有_When_夢のIsLegalMove_Then_false` | 使用制限の効果 |
| DZ-232 | `Given_自フェーズTick後_When_夢のIsLegalMove_Then_true` | Tick 後の最終状態(Influence なし)を直接構築して assertion(EndTurn 連鎖の再現は M2-PR5 で確立した Tick 経路の独立テストでカバー、本テストは「Influence なしで合法化される」を 1 件で検証) |
| DZ-233 | `Given_FDS99_使用制限なし_When_夢のIsLegalMove_Then_false` | 閾値未満 |
| DZ-234 | `Given_FDS100_使用制限なし_When_夢のIsLegalMove_Then_true` | inclusive 境界 |
| DZ-235 | `Given_夜のRound_FDS100以上_When_夢をプレイ_Then_OutcomeがWinner` | 早期勝利統合 |
| DZ-236 | `Given_朝のRound_使用制限なし_When_夢をプレイ_Then_自分のSDPがマイナス80` + `Then_Outcomeはnull` | 2 件 |
| DZ-237 | `Given_p1が夢をプレイ済_WaitingForCounter_When_p2がCounterActionで夢をtarget_Then_IsLegalMoveがfalse` | Frenzy 統合 |
| DZ-238 | `Given_手札に夢_When_夢のCardIndexでAbandonAction_Then_IsLegalMoveがfalse` | Instinct 統合 |
