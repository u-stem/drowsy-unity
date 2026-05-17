# カード No.09「強引過ぎる一手」 (Phase 2 完結後、ADR-0020 と同 PR)

DrowZzz Phase 2 完結後の **第 7 新規カード追加**(2026-05-17 オーナー JIT 確定)。**御業(高火力 SDP 変動)** + **カウント1 の使用・放棄禁止 Marker 影響** を相手に付与する戦術カード。本カードのカウント1 Marker 機能化を動機として ADR-0020 で「Influence の `RemainingCount` 減算タイミングを `EndTurn` 冒頭へ移行」を確定した(本 PR 同梱)。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.09 |
| 名前 | 強引過ぎる一手 |
| CardTypeId | `"09"` |
| 初期山札枚数 | 2(オーナー JIT 確定 2026-05-17、Phase 3 本物デッキ。M5 簡易デッキでは uniform 20 維持)|
| 効果構造 | 最上位 3 件:`AdjustSdpEffect(Self, -10)` + `AdjustSdpEffect(Opponent, +10)` + `ApplyInfluenceEffect(Opponent, ForcePlayInfluence)` |
| 新規導入概念 | `RestrictAllUsageAndAbandonInfluenceMarkerEffect`(`PlayCardAction` / `CounterAction` / `AbandonAction` の 3 アクション全般を illegal 化する Marker)|

## 効果

時間帯非依存(夜・朝両方で同じ効果、`ChoiceEffect` / `TimeOfDayBranchEffect` なし):

- 自分の SDP が 10 減る(「御業」=高火力、自分が眠くなる方向に大きく振る)
- 相手の SDP が 10 増える(「御業」相方、相手が目覚める方向に大きく振る)
- 相手にこのカード固有の **影響(カウント 1)** を付与

戦略的解釈:
- 自分 SDP -10 という大きなコストを払い、相手の **次の自フェーズ全体を「使用・放棄不可」で封じる** ことで、相手のターンを実質スキップ + 相手 SDP +10 で覚醒方向に振る攻撃カード
- 「強引な一手」というフレーバー通り、自己負担を払って相手の主要選択肢を全部削る最強の妨害カード
- ただし相手は **連想 / EndTurnAction(+ 必須経路 `DrawCardAction` / `PassCounterAction`)** は実行可能(進行不能化回避)

## 「御業」の用語注記(オーナー JIT 共有 2026-05-17)

「御業」は人智を超えた力によって発動するものという位置づけで、**基本的に火力の高いカード全般が御業と称される**。本カードでは `AdjustSdpEffect(Self, -10) + AdjustSdpEffect(Opponent, +10)` の SDP 変動量 10(各方向)が「御業」と表現される根拠。メカニクス上の特別キーワード(`Frenzy` / `Instinct` / `Counter` のような)ではなく、単に高火力 SDP 変動の通称。

## カード固有「影響」

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart`(影響保有者の自フェーズ開始時)|
| Tick 効果 | `RestrictAllUsageAndAbandonInfluenceMarkerEffect`(session 不変、判別用 marker)|
| 残発動回数 | **1**(1 フェーズ寿命、ADR-0020 で count=1 Marker が「次の自フェーズ全体で機能 + 自身の EndTurn で除去」のセマンティクスを獲得した直後の初の consumer)|
| Semantics | **存在時:`PlayCardAction` / `CounterAction` / `AbandonAction` の 3 アクションを `IsLegalMove` で illegal 化**。`AssociateAction` / `EndTurnAction` / `DrawCardAction` / `PassCounterAction` は例外的に許可(進行不能化回避)|

### 禁止対象アクションの詳細

| アクション | 禁止可否 | 判定経路 |
| ---- | ---- | ---- |
| `PlayCardAction` | **不可** | `DrowZzzRule.IsLegalPlayCard` 内で `HasUsageAndAbandonRestrictionInfluence(session.Influences[currentPlayerId])` true なら false |
| `CounterAction`(経路 1: 相手フェーズの反撃)| **不可** | `IsLegalCounterAsCounter` 内で counter プレイヤー(currentPlayer の相手)の Influences を walk して判定 |
| `CounterAction`(経路 2: 自フェーズの反撃の反撃)| **不可** | `IsLegalCounterAsCounterCounter` 内で current プレイヤーの Influences を walk して判定 |
| `AbandonAction` | **不可** | `DrowZzzRule.IsLegalAbandon` 内で current プレイヤーの Influences を walk して判定 |
| `AssociateAction` | 可 | `IsLegalAssociate` は本 Marker をチェックしない(オーナー JIT 確定 2026-05-17、テキストは「使用や放棄」のみ言及、連想は明示禁止なし)|
| `EndTurnAction` | **全フェーズ可**(ADR-0021)| `IsLegalEndTurn` で「`WaitingForEndTurn` OR `HasAnyStuckCausingInfluence`」、本 Marker は stuck 化 Marker のため `WaitingForDraw` / `WaitingForPlay` / `WaitingForEndTurn` すべてで合法(`WaitingForPlay` での合法化が脱出弁として特に重要、`PlayCardAction` / `AbandonAction` 両方禁止のため)|
| `DrawCardAction` | 可 | `WaitingForDraw` フェーズで常に合法(必須経路)|
| `PassCounterAction` | 可 | `WaitingForCounterResponse` フェーズで常に合法(反撃応答スキップ、進行不能化回避)|

### カウント1 = 1 フェーズ寿命の意味論(ADR-0020 後)

ADR-0020(本 PR 同梱)で `RemainingCount` 減算タイミングを `ApplyEndTurn` 冒頭(`Turn.Next` 前)に移動した結果、本カードのカウント1 Marker は:

1. **付与**:A が No.09 をプレイ → A の `ApplyPlayCard` 内で B(Opponent)の Influences に追加(count=1)
2. **A EndTurn**:`DecrementInfluencesForCurrentPlayer(A)` は A の Influences のみ操作、B Influences は不変
3. **Turn.Next → B が new current**
4. **B フェーズ開始時 Tick**:`TickEffect`(本 Marker は no-op)を適用、`RemainingCount` は変えない
5. **B のフェーズ全体**:`IsLegalPlayCard` / `IsLegalCounter` / `IsLegalAbandon` が本 Marker を walk して 3 アクションを illegal 化 → B は `AssociateAction` / `EndTurnAction` / `DrawCardAction` のみ可能
6. **B EndTurn**:`DecrementInfluencesForCurrentPlayer(B)` で B の本 Marker Influence が count 1→0 で除去
7. **Turn.Next → A 自フェーズ**:B Influences には本 Marker なし(除去済み)

この semantics は **旧 Tick 仕様(M2-PR5、フェーズ開始時に count -1)** では構造的に成立しない:旧仕様だと B フェーズ開始時の Tick で count 1→0 になり即除去 → B フェーズ中の `IsLegalMove` 参照時には既に Marker が消滅。詳細は ADR-0020 §Context を参照。

### 複数保有時の挙動(オーナー JIT 共有 2026-05-17)

本 Marker を同一プレイヤーが複数保有していても、`HasUsageAndAbandonRestrictionInfluence` は bool 検出のため挙動は変わらない(1 件以上で禁止、ゼロ件で許可)。N=2 × 初期 2 枚デッキ前提では複数同時保有も理論上可能だが、illegal 化判定は冪等。各 Influence は独立に count 減算されるため、複数保有時は最後の 1 件が除去されるまで illegal 化が継続する。

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("09"), new CardData("強引過ぎる一手", new Dictionary<string, int>()))

// 影響定義
var ForcePlayInfluence = new PlayerInfluence(
    InfluenceTrigger.OwnPhaseStart,
    new RestrictAllUsageAndAbandonInfluenceMarkerEffect(),
    1);

// effects 側(最上位 3 件、`KeywordedEffect` ラッパー不要 — Frenzy / Instinct / Counter のキーワード性質なし)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("09"), new IEffect[]
{
    new AdjustSdpEffect(SdpTarget.Self, -10),
    new AdjustSdpEffect(SdpTarget.Opponent, 10),
    new ApplyInfluenceEffect(SdpTarget.Opponent, ForcePlayInfluence),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-303] [Ubiquitous] Card `"09"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"強引過ぎる一手"` and the **3 top-level effects** specified above(`AdjustSdpEffect(Self, -10)` / `AdjustSdpEffect(Opponent, +10)` / `ApplyInfluenceEffect(Opponent, ForcePlayInfluence)`)。

## 事象駆動要件 (Event-driven)

- [DZ-304] When Card `"09"` is played by player A on player B, the resulting session shall reflect `SDP[A] -= 10`(時間帯非依存)。
- [DZ-305] When Card `"09"` is played by player A on player B, the resulting session shall reflect `SDP[B] += 10`(時間帯非依存)。
- [DZ-306] When Card `"09"` is played by player A on player B, B's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffect, 1)` entry.

## 合法性判定(`IsLegalMove` 拡張、ADR-0020 と同 PR)

- [DZ-307] When B holds `PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffect, 1)` and B tries to play any card via `PlayCardAction`, `IsLegalMove` shall return `false`(CardTypeId 非依存、すべての PlayCardAction が illegal)。
- [DZ-308] When B holds the marker influence and B tries to perform `AbandonAction(choice)` for any choice, `IsLegalMove` shall return `false`(放棄不可)。
- [DZ-309] When B holds the marker influence and B tries to perform `CounterAction` during A's `WaitingForCounterResponse`(経路 1)or `WaitingForEndTurn`(経路 2、自フェーズの反撃の反撃), `IsLegalMove` shall return `false`(両経路で禁止、オーナー JIT 確定 2026-05-17:「使用」に CounterAction も含む)。
- [DZ-310] When B holds the marker influence and B tries to perform `AssociateAction(card)` during B's own phase, `IsLegalMove` shall return its **normal value**(本 marker は AssociateAction を illegal 化しない、テキスト「使用や放棄」に連想は含まれない)。
- [DZ-311] When B holds the marker influence and B tries to perform `EndTurnAction`, `IsLegalMove` shall return `true` **regardless of `PhaseState`**(ADR-0021:stuck 化 Marker 保有時の全フェーズ合法化、進行不能化回避の脱出弁)。具体的には `WaitingForEndTurn`(通常合法経路)+ `WaitingForPlay`(本 marker により PlayCard / Abandon 両方禁止で stuck 化するフェーズ、ADR-0021 で追加合法化)+ `WaitingForDraw`(stuck 化はしないが ADR-0021 で一律全フェーズ合法化対象)で `true`。

## ライフサイクル / Tick 評価のシナリオ(ADR-0020 後の count=1 Marker)

ADR-0020 で count -1 タイミングを `ApplyEndTurn` 冒頭(Turn.Next 前)に移動した結果、count=1 Marker が次の自フェーズ全体で機能する。

- [DZ-312] When A's EndTurn rotates current to B after A played Card `"09"`(B が本 marker count=1 保有開始), B's influence shall remain with `RemainingCount = 1` during B's entire phase(B Tick は count 不変、IsLegalPlayCard / IsLegalCounter / IsLegalAbandon で本 marker walk が機能)。
- [DZ-313] When B's own EndTurn fires while B holds the marker influence with `RemainingCount = 1`, `DecrementInfluencesForCurrentPlayer(B)` shall remove the marker from B's influences (1 → 0 で除去)。次の A 自フェーズ開始時には本 marker は存在しない。

## 定数依存

- **L3(カード設計値、本カード固有)**:
  - 自分 SDP 即時変動: -10
  - 相手 SDP 即時変動: +10
  - 影響 RemainingCount: 1(1 フェーズ寿命、ADR-0020 後初の Marker 系 count=1 採用カード)
- **L2 / L1**: 該当なし(本カード独自の計算経路はなく、既存 `AdjustSdpEffect` / `ApplyInfluenceEffect` / `RestrictAllUsageAndAbandonInfluenceMarkerEffect` の組み合わせのみ)

## 関連

- ADR:
  - [`docs/adr/0020-influence-count-decrement-timing.md`](../../../../adr/0020-influence-count-decrement-timing.md)(本 PR 同梱、本カードの count=1 Marker 機能化が動機)
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」(意味論レベルでは ADR-0007 を維持、減算タイミングのみ ADR-0020 で更新)
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §5「順序保証」(ADR-0020 で `Influence Decrement` ステップを追加)
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md)
- 前提効果(本 PR で実装、spec md 未作成):`RestrictAllUsageAndAbandonInfluenceMarkerEffect`(`Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RestrictAllUsageAndAbandonInfluenceMarkerEffect.cs` 参照、xmldoc に Design 記述あり)
- 既存類似カード:
  - [`./sound-of-silence.md`](./sound-of-silence.md)(No.04、特定 CardTypeId 1 個に限定した使用禁止、`RestrictSpecificCardInfluenceEffect` 経由)
  - [`./00-dream.md`](./00-dream.md)(No.00、連想後の全カード使用制限、`UsageRestrictionMarkerEffect` 経由)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RestrictAllUsageAndAbandonInfluenceMarkerEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`HasUsageAndAbandonRestrictionInfluence` 新設 + `IsLegalPlayCard` / `IsLegalAbandon` / `IsLegalCounterAsCounter` / `IsLegalCounterAsCounterCounter` 拡張 + `DecrementInfluencesForCurrentPlayer` 新設 + `ApplyEndTurn` 順序更新 + `TickInfluencesForCurrentPlayer` から count -1 削除)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(no-op marker case)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/RestrictAllUsageAndAbandonInfluenceMarkerEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.09 entry + rid 970〜973 追加)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「9 → 10 種」)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/ForcePlayCardTests.cs`(新規)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/ForcePlayCardCatalogTests.cs`(新規、SO ↔ InMemory 同値性)
  - 既存テスト 6 件以上更新(`GreenInvasionCardTests` / `UntouchableRealmCardTests` / `SoundOfSilenceCardTests` / `GoodForBodyCardTests` の中間状態 assertion)
- シナリオ: `force-play.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-303 | (テスト免除: Ubiquitous) | catalog 登録は `ForcePlayCardTests` のヘルパー + `ForcePlayCardCatalogTests` で構造的に保証 |
| DZ-304 | `Given_任意フェーズ_When_Card09をプレイ_Then_自分のSDPがマイナス10` | 統合テスト |
| DZ-305 | `Given_任意フェーズ_When_Card09をプレイ_Then_相手のSDPがプラス10` | 統合テスト |
| DZ-306 | `Given_任意フェーズ_When_Card09をプレイ_Then_相手のInfluencesにForcePlayMarkerが付与される` | 統合テスト |
| DZ-307 | `Given_p2が本Markerカウント1保有_When_p2がPlayCardActionでIsLegalMove_Then_false` | illegal 化(PlayCardAction)|
| DZ-308 | `Given_p2が本Markerカウント1保有_When_p2がAbandonActionでIsLegalMove_Then_false` | illegal 化(AbandonAction)|
| DZ-309 | `Given_p2が本Markerカウント1保有_When_p2がCounterActionでIsLegalMove_Then_false`(経路 1 + 経路 2 の 2 件に分割)| illegal 化(CounterAction 両経路)|
| DZ-310 | `Given_p2が本Markerカウント1保有_When_p2がAssociateActionでIsLegalMove_Then_true` | AssociateAction は許可(連想可)|
| DZ-311 | `Given_p2が本Markerカウント1保有_When_p2がEndTurnActionでIsLegalMove_Then_true` + `Given_p2が本Markerカウント1保有_WaitingForPlay_When_EndTurnAction_Then_stuck脱出弁で合法` | ADR-0021 stuck 脱出弁(WaitingForEndTurn + WaitingForPlay 両方で true)|
| DZ-312 | `Given_p2が本Markerカウント1保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluencesはカウント1で残存` | ADR-0020 後の count=1 Marker は p2 フェーズで機能 |
| DZ-313 | `Given_p2が本Markerカウント1保有_p2current_When_p2EndTurn_Then_p2のInfluences件数が0` | p2 自身の EndTurn 冒頭 Decrement で除去 |
