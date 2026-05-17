# カード No.06「牙の届かぬ領域」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 5 新規カード追加**(2026-05-17 オーナー JIT 確定)。**Frenzy(狂乱)キーワード持ち** + **ベッド破損 SDP 変動 2 倍化 Influence** を相手に付与する戦術カード。No.04 / No.05 と同じく PR ② 以降の effect 設計パターンを踏襲しつつ、ベッド破損計算経路に介入する初の Influence 機構を導入する。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.06 |
| 名前 | 牙の届かぬ領域 |
| CardTypeId | `"06"` |
| 初期山札枚数 | 1(オーナー JIT 確定 2026-05-17、No.03 と同じレア枠)|
| 効果構造 | 最上位 3 件:`AdjustSdpEffect(Self, -12)` + `AdjustSdpEffect(Opponent, -4)` + `KeywordedEffect([Frenzy], ApplyInfluenceEffect(Opponent, BedDamage2xInfluence))` |
| 新規導入概念 | `DoubleBedDamageSdpInfluenceMarkerEffect`(ベッド破損 SDP 変動 2 倍化、`UsageRestrictionMarkerEffect` と同パターン)|

## `KeywordedEffect` 包み方の補足(P-1 反映 2026-05-17)

`KeywordedEffect` は単一 inner 制約(IEffect 1 件のみ inner として保持可能)。本カードでは「カード全体に Frenzy 性質を付与する」のが意図であり、Frenzy が `ApplyInfluenceEffect` 自体を修飾しているわけではない。`DrowZzzRule.HasKeywordInEffects` は **効果列のいずれかに Frenzy が含まれれば true** という OR 検出方式のため、`KeywordedEffect([Frenzy], <任意の IEffect>)` を 1 件含めることで「カード全体が Frenzy 性質を持つ」と扱える(既存 No.00「夢」と同パターン、ADR-0011 §4.5)。

## 効果

時間帯非依存(夜・朝両方で同じ効果、ChoiceEffect / TimeOfDayBranchEffect なし):

- 自分の SDP が 12 減る
- 相手の SDP が 4 減る
- 自分(actor)はカード全体に **Frenzy(狂乱)キーワード** を付与(`HasKeywordInEffects` で OR 検出 → 相手の Counter Action illegal 化、ADR-0011 §4.5)
- 相手にこのカード固有の **影響(カウント 4)** を付与

## カード固有「影響」

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart`(影響保有者の自フェーズ開始時)|
| Tick 効果 | `DoubleBedDamageSdpInfluenceMarkerEffect`(session 不変、判別用 marker)|
| 残発動回数 | 4(4 フェーズ寿命)|
| Semantics | **存在時:ベッド破損による SDP 変動が 2 倍**。`ApplyBedDamageToCurrentPlayer` 内の `sdpDamage = bedDamage / 5` の結果が 2 倍化(例: BedDamages=40% で通常 SDP -8 → 本影響中 SDP -16)|

### 将来拡張への配慮(オーナー JIT 共有 2026-05-17)

「ベッド破損 SDP をプラスに変えるカード」が今後紹介される予定。本 2 倍化 marker は `ApplyBedDamageToCurrentPlayer` の計算結果に対する 2 倍化として実装するため、将来「プラス変換 effect」が追加された場合も計算経路 1 箇所(`sdpDamage` 算出後)で共通 honor される(プラス +2 → +4 のような自然な拡張)。

### 複数保有時 / 境界ケースの注記(code-reviewer W-1 / W-2 反映 2026-05-17)

- **複数保有時**:同一プレイヤーが本 `DoubleBedDamageSdpInfluenceMarkerEffect` Influence を **複数保有していても 2 倍止まり**(`HasDoubleBedDamageInfluence` は bool 検出のため累積乗算なし)。N=2 × 初期 1 枚デッキ前提では実発生想定外だが、将来カード数増加時の誤解防止として明示。
- **BedDamage 軽微(1〜4%)**:`bedDamage / 5 = 0`(整数除算)で sdpDamage = 0、2 倍化しても 0 のまま no-op。「2 倍化 Influence は BedDamage が 5% 以上ある時のみ実効」セマンティクスで設計上問題なし(テスト対象外、`ApplyBedDamageToCurrentPlayer` の `sdpDamage == 0` ガードで session 不変返却)。

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("06"), new CardData("牙の届かぬ領域", new Dictionary<string, int>()))

// 影響定義
var BedDamage2xInfluence = new PlayerInfluence(
    InfluenceTrigger.OwnPhaseStart,
    new DoubleBedDamageSdpInfluenceMarkerEffect(),
    4);

// effects 側(最上位 3 件、3 件目を Frenzy 包みでカード全体に Frenzy 性質付与)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("06"), new IEffect[]
{
    new AdjustSdpEffect(SdpTarget.Self, -12),
    new AdjustSdpEffect(SdpTarget.Opponent, -4),
    // KeywordedEffect は単一 inner 制約(IEffect 1 件のみ)のため、ApplyInfluenceEffect を inner として包んでカード全体に Frenzy 性質を表現。
    // HasKeywordInEffects は OR 検出のため、効果列のいずれかに Frenzy 含めば「カード全体 Frenzy」として扱われる。
    new KeywordedEffect(new[] { Keyword.Frenzy },
        new ApplyInfluenceEffect(SdpTarget.Opponent, BedDamage2xInfluence)),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-278] [Ubiquitous] Card `"06"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"牙の届かぬ領域"` and the **3 top-level effects** specified above. The third effect shall be a `KeywordedEffect([Frenzy], ApplyInfluenceEffect(...))` so that `HasKeywordInEffects(effects, Frenzy)` returns `true`(カード全体に Frenzy 性質)。

## 事象駆動要件 (Event-driven)

- [DZ-279] When Card `"06"` is played by player A on player B, the resulting session shall reflect `SDP[A] -= 12`(時間帯非依存)。
- [DZ-280] When Card `"06"` is played by player A on player B, the resulting session shall reflect `SDP[B] -= 4`(時間帯非依存)。
- [DZ-281] When Card `"06"` is played by player A on player B, B's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, DoubleBedDamageSdpInfluenceMarkerEffect, 4)` entry.

## 合法性判定(`IsLegalMove` 拡張、既存パターン)

- [DZ-282] When Card `"06"` is targeted by `CounterAction`(対戦相手による反撃)during `WaitingForCounterResponse`, `IsLegalMove` shall return `false`(Frenzy 持ちカードは反撃を受けない、ADR-0011 §4.5、既存 No.00「夢」と同パターン)。

## Tick 評価のシナリオ(ベッド破損 2 倍化)

- [DZ-283] When B holds `PlayerInfluence(OwnPhaseStart, DoubleBedDamageSdpInfluenceMarkerEffect, 4)` and a phase rotation makes B the new current player while `BedDamages[B] = 40%`, the resulting session shall reflect `SDP[B] -= 16`(通常の `40 / 5 = 8` を 2 倍化、Influence の RemainingCount は 4 → 3 に減算)。
- [DZ-284] When B does **not** hold any `DoubleBedDamageSdpInfluenceMarkerEffect` Influence and a phase rotation makes B the new current player while `BedDamages[B] = 40%`, the resulting session shall reflect `SDP[B] -= 8`(通常の `bedDamage / 5`、2 倍化なし)。
- [DZ-285] When B holds the 2x Influence with `RemainingCount = 1` and a phase rotation Tick fires, B's influence shall be removed (`RemainingCount` 1 → 0 で除去、`UsageRestrictionMarkerEffect` と完全対称)。

## 定数依存

- **L3(カード設計値、本カード固有)**:
  - 自分 SDP 即時変動: -12
  - 相手 SDP 即時変動: -4
  - 影響 RemainingCount: 4(4 フェーズ寿命)
- **L2(ベッド破損計算、既存)**:
  - `DrowZzzBedConstants.BedDamageRatePerSdp = 5`(`bedDamage / 5` の除数、既存)
  - 2 倍化乗数 `2`(本カード固有、計算経路ハードコード)

## 関連

- ADR:
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §3「ベッド破損」+ §4.5「Frenzy = 反撃を受けない」
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md) / [`../effects/keyworded-effect.md`](../effects/keyworded-effect.md)
- 前提効果(本 PR で実装、spec md 未作成):`DoubleBedDamageSdpInfluenceMarkerEffect`(`Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/DoubleBedDamageSdpInfluenceMarkerEffect.cs` 参照、xmldoc に Design 記述あり)
- 既存類似カード:
  - [`./00-dream.md`](./00-dream.md)(No.00、Frenzy / Instinct 両キーワード持ち)
  - [`./green-invasion.md`](./green-invasion.md)(No.02、ApplyInfluenceEffect で相手に Influence 付与)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/DoubleBedDamageSdpInfluenceMarkerEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyBedDamageToCurrentPlayer` 拡張 + `HasDoubleBedDamageInfluence`)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(no-op marker case)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/DoubleBedDamageSdpInfluenceMarkerEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/UntouchableRealmCardTests.cs`
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/UntouchableRealmCardCatalogTests.cs`
- シナリオ: `untouchable-realm.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-278 | (テスト免除: Ubiquitous) | catalog 登録 + Frenzy 性質は `UntouchableRealmCardTests` のヘルパー + `UntouchableRealmCardCatalogTests` で構造的に保証 |
| DZ-279 | `Given_任意フェーズ_When_Card06をプレイ_Then_自分のSDPがマイナス12` | 統合テスト |
| DZ-280 | `Given_任意フェーズ_When_Card06をプレイ_Then_相手のSDPがマイナス4` | 統合テスト |
| DZ-281 | `Given_任意フェーズ_When_Card06をプレイ_Then_相手のInfluencesにBedDamage2xが付与される` | 統合テスト |
| DZ-282 | `Given_p2手札にCard06_When_p1がCounterActionで対象_Then_IsLegalMoveがfalse` | Frenzy = 反撃不可 |
| DZ-283 | `Given_p2が2xInfluence保有_BedDamage40%_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス16` + `Then_Influenceの残カウントが3になる` | 2 件に分割(2 倍化 SDP + RemainingCount 減算) |
| DZ-284 | `Given_p2が2xInfluence非保有_BedDamage40%_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス8` | 通常計算経路の非リグレッション |
| DZ-285 | `Given_p2が2xInfluenceカウント1保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluences件数が0` | 寿命 0 到達除去 |
