# カード No.20「至上の喜び」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 19 新規カード追加**(2026-05-18 オーナー JIT 確定)。**「カード固有放棄効果」機構の初導入カード**(ADR-0025)。プレイ時の大規模 SDP 差(+20/-20)+ 自爆 Marker と、放棄時の控えめ両得 SDP(+4/+6)を併せ持つ、攻めるか降りるかの戦術カード。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.20 |
| 名前 | 至上の喜び |
| CardTypeId | `"20"` |
| 初期山札枚数 | 2(オーナー JIT 確定 2026-05-18、強力効果ゆえに少枚枠)|
| 効果構造 | 1 件最上位:`PlayOrAbandonBranchEffect(PlayEffects[3], AbandonEffects[2])` |
| 新規導入概念 | `PlayOrAbandonBranchEffect` 新規 wrapper(ADR-0025)|

## 効果

### プレイ時(「御業」+20/-20 + 甲影響)

3 件、PlayEffects 列の順序で評価:

1. **Self SDP +20**(`AdjustSdpEffect(SdpTarget.Self, +20)`)
2. **Opponent SDP -20**(`AdjustSdpEffect(SdpTarget.Opponent, -20)`)
3. **甲(Self に影響付与、カウント 1)**:`ApplyInfluenceEffect(Self, PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffect, 1))`
   - 影響内容:「存在時、手段の使用や放棄をすることができない」(No.09「強引過ぎる一手」と同じ Marker、ADR-0020)
   - count=1 で次の自分のフェーズ 1 回機能 → 1 ターンスキップ相当の自爆効果

### 放棄時(「御業・放棄」+4/+6)

2 件、`AbandonChoice` 適用後に追加発動(累積モデル、ADR-0025 §「AbandonChoice との関係」):

1. **Self SDP +4**(`AdjustSdpEffect(SdpTarget.Self, +4)`)
2. **Opponent SDP +6**(`AdjustSdpEffect(SdpTarget.Opponent, +6)`)

つまり放棄時の総効果は:
- `AbandonChoice.GainSdp` 選択 → Self SDP +5(既存) + Self+4 / Opp+6(本カード固有)= Self +9 / Opp +6
- `AbandonChoice.RepairBed` 選択 → Bed -20%(既存) + Self+4 / Opp+6(本カード固有)

戦略的解釈:
- プレイ時:**「20 点差を作るが次自分のフェーズで詰む」** = 終盤の決定打 or 序盤の博打
- 放棄時:**「両者プラスだが相対的に -2 差(Opp +6 - Self +4)」** + AbandonChoice の追加効果 → 「使わないが捨てたくない」場面で消極的に選ぶ
- 「御業」フレーバーは「神々しい絶頂、しかし反動も巨大」のニュアンス

## ADR-0025 の決定事項

ADR-0025 で確定した機構:

1. 新規 wrapper `PlayOrAbandonBranchEffect(PlayEffects, AbandonEffects)`(`TimeOfDayBranchEffect` と同パターン、両 list 1 件以上必須)
2. `EffectInterpreter` で **no-op**(評価は rule 評価層で unwrap)
3. `DrowZzzRule.ApplyPlayCard`:effects walk 内で本 effect を検出したら **PlayEffects のみ** 逐次 EffectInterpreter
4. `DrowZzzRule.ApplyAbandon`:既存 `AbandonChoice` 適用(SDP+5 / Bed-20%)**後**、放棄カードの effects 最上位 scan で本 effect を検出したら **AbandonEffects のみ** 逐次 EffectInterpreter(累積モデル、AbandonChoice を上書きしない)
5. `EffectJsonConverter`:`"PlayOrAbandonBranch"` discriminator(`playEffects` / `abandonEffects` 配列、`TimeOfDayBranch` と同パターン)
6. `PlayOrAbandonBranchEffectAsset` 新規 SO(`TimeOfDayBranchEffectAsset` と同パターン、`[SerializeReference] EffectAsset[] _playEffects` / `_abandonEffects`)

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("20"), new CardData("至上の喜び", new Dictionary<string, int>()))

// effects 側(1 件最上位の PlayOrAbandonBranchEffect)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("20"), new IEffect[]
{
    new PlayOrAbandonBranchEffect(
        playEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, +20),
            new AdjustSdpEffect(SdpTarget.Opponent, -20),
            new ApplyInfluenceEffect(SdpTarget.Self, new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictAllUsageAndAbandonInfluenceMarkerEffect(),
                RemainingCount: 1)),
        },
        abandonEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, +4),
            new AdjustSdpEffect(SdpTarget.Opponent, +6),
        }),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-397] [Ubiquitous] Card `"20"` shall be registered with name `"至上の喜び"` and a single top-level `PlayOrAbandonBranchEffect` whose `PlayEffects` contains 3 effects (`AdjustSdp(Self,+20)`, `AdjustSdp(Opponent,-20)`, `ApplyInfluence(Self, PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffect, 1))`) and whose `AbandonEffects` contains 2 effects (`AdjustSdp(Self,+4)`, `AdjustSdp(Opponent,+6)`).

## プレイ時の効果(`ApplyPlayCard` 経路、PlayEffects unwrap)

- [DZ-398] When Card `"20"` is played by player A via `PlayCardAction`, the resulting session shall reflect `SDP[A] = initial[A] + 20` and `SDP[B] = initial[B] - 20`(Self+20 / Opponent-20)。
- [DZ-399] When Card `"20"` is played by player A, A's `Influences` shall contain a new `PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffect, RemainingCount=1)` at the end of the list(甲影響付与、ADR-0020 count=1 Marker 機能化と整合)。
- [DZ-400] When Card `"20"` is played by player A, A's Hand shall lose Card `"20"` and Field shall gain Card `"20"` at the top position(PlayCardAction 直後の中間状態)。

## 放棄時の効果(`ApplyAbandon` 経路、AbandonEffects unwrap)

- [DZ-401] When Card `"20"` is abandoned by player A with `AbandonChoice.GainSdp`, the resulting session shall reflect `SDP[A] = initial[A] + 5 + 4 = +9` and `SDP[B] = initial[B] + 6`(AbandonChoice GainSdp +5 + カード固有 Self+4 / Opp+6)。
- [DZ-402] When Card `"20"` is abandoned by player A with `AbandonChoice.RepairBed`(`BedDamages[A] > 0`), the resulting session shall reflect `BedDamages[A] = max(0, initial[A] - 20)` and `SDP[A] = +4` / `SDP[B] = +6`(AbandonChoice RepairBed -20% + カード固有 Self+4 / Opp+6)。
- [DZ-403] When Card `"20"` is abandoned by player A, A's Hand shall lose Card `"20"` and Discard shall gain Card `"20"`(放棄経路の Hand/Discard 移動)。
- [DZ-404] When Card `"20"` is abandoned by player A, A's `Influences` shall **not** gain any new `PlayerInfluence`(放棄経路では甲影響は付与されない、AbandonEffects に ApplyInfluenceEffect を含まないため)。

## カードデータ整合性

- [INF-165] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"20"`「至上の喜び」, name / effects shall equal `InMemoryCardCatalog`.
  - SO 経路:`PlayOrAbandonBranchEffectAsset` 1 件最上位、PlayEffects(`AdjustSdpEffectAsset` ×2 + `ApplyInfluenceEffectAsset(PlayerInfluenceAsset(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffectAsset, 1))`)、AbandonEffects(`AdjustSdpEffectAsset` ×2)
  - record 値同値:`PlayOrAbandonBranchEffect` の順序保持シーケンス Equals(`TimeOfDayBranchEffect` と同パターン)で両 list 全要素一致

## 関連

- ADR:
  - [`docs/adr/0025-play-or-abandon-branch-effect.md`](../../../../adr/0025-play-or-abandon-branch-effect.md)(本カード起点の新規 ADR、`PlayOrAbandonBranchEffect` wrapper + `ApplyAbandon` 経路への catalog 依存追加)
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §2「放棄(代替ターン行動)」(本 ADR で拡張する基盤)
  - [`docs/adr/0020-influence-count-decrement-timing.md`](../../../../adr/0020-influence-count-decrement-timing.md)(`RestrictAllUsageAndAbandonInfluenceMarkerEffect` count=1 機能化、本カード甲影響が同 Marker を使用)
- 前提効果: [`../effects/apply-influence.md`](../effects/apply-influence.md)
- 既存類似カード(同 Marker 使用):[`./force-play.md`](./force-play.md)(No.09「強引過ぎる一手」、同 Marker を Opponent に付与する PR ペア)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/PlayOrAbandonBranchEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(no-op case)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyPlayCard` PlayEffects unwrap + `ApplyAbandon` AbandonEffects unwrap)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/PlayOrAbandonBranchEffectAsset.cs`(新規 SO)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(`PlayOrAbandonBranch` discriminator 追加)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.20 entry + rid 5600/5601/5602/5603/5604/5605/5606)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(コメント 20→21 種 + No.20=2 枚)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/SupremeJoyCardTests.cs`(新規、DZ-398〜404)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/SupremeJoyCardCatalogTests.cs`(新規、INF-165)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/EffectJsonConverterTests.cs`(`PlayOrAbandonBranchEffect` round-trip 追加)
- シナリオ: `supreme-joy.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-397 | (テスト免除: Ubiquitous) | catalog 登録 + 効果列構造は SupremeJoyCardTests + CatalogTests で構造保証 |
| DZ-398 | `Given_Card20_When_PlayCardAction_Then_SDP_Self20_Opp_M20` | プレイ時 SDP |
| DZ-399 | `Given_Card20_When_PlayCardAction_Then_Self_Influences_Restrict_Marker1件` | 甲影響付与 |
| DZ-400 | `Given_Card20手札_When_PlayCardAction_Then_Hand_Remove_Field_Add` | Hand / Field |
| DZ-401 | `Given_Card20_When_AbandonGainSdp_Then_Self_SDP_9_Opp_SDP_6` | 放棄 GainSdp 累積 |
| DZ-402 | `Given_Card20_BedDamages_When_AbandonRepairBed_Then_Bed_M20_Self_SDP_4_Opp_SDP_6` | 放棄 RepairBed 累積 |
| DZ-403 | `Given_Card20手札_When_Abandon_Then_Hand_Remove_Discard_Add` | 放棄経路 Hand/Discard |
| DZ-404 | `Given_Card20_When_Abandon_Then_Influences_変動なし` | 放棄では甲影響なし |
| INF-165 | `SupremeJoyCardCatalogTests.Given_*` 2 件 | SO 同等性検証 |
