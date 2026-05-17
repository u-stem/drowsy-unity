# カード No.08「廻るための知恵」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 7 新規カード追加**(2026-05-17 オーナー JIT 確定)。No.07「知恵の及ばぬ領域」と **対をなす influence カード**:選択式 ChoiceEffect で自他いずれかに「ベッド破損 SDP 符号反転」**永続** Influence を付与。Instinct(本能)キーワード持ち(放棄対象不可)。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.08(オーナー JIT 当初 30 → 連番揃えで 08 に確定 2026-05-17)|
| 名前 | 廻るための知恵 |
| CardTypeId | `"08"` |
| 初期山札枚数 | 3(オーナー指定 3 枚、本 PR で唯一の複数初期山札カード)|
| 効果構造 | 最上位 `ChoiceEffect`(2 分岐)、各 branch 内 `ApplyInfluenceEffect` を `KeywordedEffect([Instinct], _)` で包む(`ChoiceEffect` を最上位に置かないと `ApplyPlayCard` の unwrap が機能しない、2026-05-17 開発中の DZ-294/302 失敗から判明)|
| キーワード | Instinct(本能、放棄対象不可、ADR-0011 §4.2) |
| 新規導入概念 | `InvertBedDamageSdpInfluenceMarkerEffect`(ベッド破損 SDP 符号反転 marker、保有数奇偶判定)|

## 効果

時間帯非依存、ChoiceEffect 2 分岐(`PlayCardAction.Choice` で選択):

### 選択 1(自分強化型、`Choice == 0`)

- 自分 SDP **±0**(変動なし)
- 相手 SDP **+5**(相手を眠くさせる)
- 自分に **「ベッド破損 SDP +/- 逆転」永続影響** を付与

### 選択 2(相手押し付け型、`Choice == 1`)

- 自分 SDP **+5**(自分を眠くする = 自分のベッド破損リスクを増やす? それとも単なるコスト?)
- 相手 SDP **±0**(変動なし)
- 相手に **「ベッド破損 SDP +/- 逆転」永続影響** を付与

## カード固有「影響」(自他いずれか付与)

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart`(影響保有者の自フェーズ開始時)|
| Tick 効果 | `InvertBedDamageSdpInfluenceMarkerEffect`(session 不変、判別用 marker)|
| 残発動回数 | `InfluenceConstants.Perpetual`(永続、No.03「身体にいいもの」と同じ)|
| Semantics | **存在時:ベッド破損による SDP の +/- が逆転**(現状の `sdpDamage = bedDamage / 5` の符号を反転、SDP -8 → SDP +8 = 回復方向)|

### 複数保有時の奇偶判定(オーナー JIT 確定 2026-05-17)

「廻るための知恵は 3 枚なので 1 プレイヤーに対して 3 つ同じ影響が付く可能性がある → そのたびに +/- が変わる」セマンティクス:

- 保有 0 件: 通常(SDP -N、減算)
- 保有 1 件: **反転**(SDP +N、回復)
- 保有 2 件: **元に戻る**(SDP -N、減算)
- 保有 3 件: **反転**(SDP +N、回復)

実装は `ApplyBedDamageToCurrentPlayer` 内で `CountInvertBedDamageInfluence` で件数カウントし、`count % 2 == 1` なら符号反転。**No.06「2 倍化」(bool 検出、複数保有時も 2 倍止まり)とは対照的な設計**(本 marker 専用、`CountInvertBedDamageInfluence`)。

### No.06「2 倍化」との組み合わせ(オーナー JIT 確定 2026-05-17)

両者重複保有時は **「逆転 → 2 倍化」** の順:
- 通常 sdpDamage = 8 → 反転 = -8 → 2 倍化 = -16 → 適用で SDP += 16(回復強化)
- これにより「ベッド破損が進めば進むほど大きく回復」というスケール効果が表現される

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("08"), new CardData("廻るための知恵", new Dictionary<string, int>()))

// 影響定義(逆転 marker、永続)
var InvertBedDamageInfluence = new PlayerInfluence(
    InfluenceTrigger.OwnPhaseStart,
    new InvertBedDamageSdpInfluenceMarkerEffect(),
    InfluenceConstants.Perpetual);

// effects 側(最上位 ChoiceEffect、各 branch 内 ApplyInfluenceEffect を Keyworded([Instinct]) で包む。
//  ChoiceEffect を最上位に置かないと ApplyPlayCard の unwrap が機能せず NotImplementedException になる
//  No.04 / No.05 / No.06 の「KeywordedEffect 内 nested 問題」と同根、2026-05-17 開発中 DZ-294/302 失敗で確認)。
//  Instinct 性質は HasKeywordInEffects 再帰 walk(ChoiceEffect → Branches → KeywordedEffect)で OR 検出される。
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("08"), new IEffect[]
{
    new ChoiceEffect(new IEffect[][]
    {
        // 選択 1: 自分 ±0 / 相手 +5 / 自分に永続反転影響(Keyworded で包んでカード全体に Instinct 性質付与)
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Opponent, 5),
            new KeywordedEffect(new[] { Keyword.Instinct },
                new ApplyInfluenceEffect(SdpTarget.Self, InvertBedDamageInfluence)),
        },
        // 選択 2: 自分 +5 / 相手 ±0 / 相手に永続反転影響
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, 5),
            new KeywordedEffect(new[] { Keyword.Instinct },
                new ApplyInfluenceEffect(SdpTarget.Opponent, InvertBedDamageInfluence)),
        },
    }),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-293] [Ubiquitous] Card `"08"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"廻るための知恵"` and a single top-level `ChoiceEffect` containing 2 branches. Each branch's `ApplyInfluenceEffect` shall be wrapped in `KeywordedEffect([Instinct], _)` so that `HasKeywordInEffects(effects, Instinct)` returns `true` via recursive walk(`ChoiceEffect.Branches → KeywordedEffect`、カード全体に Instinct 性質、AbandonAction 対象不可)。`ChoiceEffect` を最上位に置く理由は `ApplyPlayCard` の unwrap が最上位 walk のみのため(2026-05-17 開発中の DZ-294/302 失敗で構造修正)。

## 事象駆動要件 (Event-driven)

- [DZ-294] When Card `"08"` is played by player A on player B with `Choice == 0`, the resulting session shall reflect `SDP[B] += 5`(自分 SDP は ±0)。
- [DZ-295] When Card `"08"` is played by player A on player B with `Choice == 0`, A's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffect, Perpetual)` entry(自分に永続反転影響を付与)。
- [DZ-296] When Card `"08"` is played by player A on player B with `Choice == 1`, the resulting session shall reflect `SDP[A] += 5`(相手 SDP は ±0)。
- [DZ-297] When Card `"08"` is played by player A on player B with `Choice == 1`, B's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffect, Perpetual)` entry(相手に永続反転影響を付与)。

## 合法性判定 + Tick / 奇偶判定

- [DZ-298] When Card `"08"` is targeted by `AbandonAction(_, CardIndex)` while the index matches a Card `"08"` in hand, `IsLegalMove` shall return `false`(Instinct = 放棄対象不可、ADR-0011 §4.2、既存 DZ-238 同パターン)。
- [DZ-299] When player A holds 1 `InvertBedDamageSdpInfluenceMarkerEffect` influence and `BedDamages[A] = 40%`, a phase rotation Tick on A shall result in `SDP[A] += 8`(40/5=8 の符号反転、回復方向)。
- [DZ-300] When player A holds 2 `InvertBedDamageSdpInfluenceMarkerEffect` influences and `BedDamages[A] = 40%`, a phase rotation Tick on A shall result in `SDP[A] -= 8`(奇偶判定で 2 件 = 元に戻る、通常の減算)。
- [DZ-301] When player A holds 1 `InvertBedDamageSdpInfluenceMarkerEffect` + 1 `DoubleBedDamageSdpInfluenceMarkerEffect` influence and `BedDamages[A] = 40%`, a phase rotation Tick on A shall result in `SDP[A] += 16`(逆転 → 2 倍化、SDP -8 → +8 → +16、オーナー JIT 確定順序)。
- [DZ-302] When Card `"08"` is requested with `Choice == 2`(範囲外、Branches.Count == 2 のため), `IsLegalMove` shall return `false`(既存 ChoiceEffect 範囲外検証)。

## 関連

- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響」+ [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4.2「Instinct」
- 対をなすカード: [`./realm-beyond-wisdom.md`](./realm-beyond-wisdom.md)(No.07、本カードを消滅対象 / 使用禁止対象とする)
- 既存類似カード: [`./untouchable-realm.md`](./untouchable-realm.md)(No.06、ベッド破損 Influence 2 倍化、本カードと組み合わせ可)+ [`./00-dream.md`](./00-dream.md)(No.00、Instinct 持ち)+ [`./green-invasion.md`](./green-invasion.md)(No.02、ChoiceEffect 2 分岐)+ [`./good-for-body.md`](./good-for-body.md)(No.03、永続影響 `InfluenceConstants.Perpetual`)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/InvertBedDamageSdpInfluenceMarkerEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyBedDamageToCurrentPlayer` 拡張 + `CountInvertBedDamageInfluence`)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(marker case)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/InvertBedDamageSdpInfluenceMarkerEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/CirculatingWisdomCardTests.cs`
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/CirculatingWisdomCardCatalogTests.cs`
- シナリオ: `circulating-wisdom.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-293 | (テスト免除: Ubiquitous) | catalog 登録 + Instinct 性質は `CirculatingWisdomCardTests` のヘルパー + `CirculatingWisdomCardCatalogTests` で構造的に保証 |
| DZ-294 | `Given_任意フェーズ_When_Card08をChoice0でプレイ_Then_相手のSDPがプラス5` | 統合テスト |
| DZ-295 | `Given_任意フェーズ_When_Card08をChoice0でプレイ_Then_自分のInfluencesにInvertBedDamageが追加` | 統合テスト |
| DZ-296 | `Given_任意フェーズ_When_Card08をChoice1でプレイ_Then_自分のSDPがプラス5` | 統合テスト |
| DZ-297 | `Given_任意フェーズ_When_Card08をChoice1でプレイ_Then_相手のInfluencesにInvertBedDamageが追加` | 統合テスト |
| DZ-298 | `Given_手札にCard08_When_Card08のCardIndexでAbandonAction_Then_IsLegalMoveがfalse` | Instinct = 放棄不可、DZ-238 同パターン |
| DZ-299 | `Given_p2がInvert1件保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがプラス8` | 1 件保有で反転、回復方向 |
| DZ-300 | `Given_p2がInvert2件保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス8` | 2 件保有で元に戻る(奇偶判定) |
| DZ-301 | `Given_p2がInvert1件Double1件保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがプラス16` | 逆転 → 2 倍化順、オーナー JIT 確定 |
| DZ-302 | `Given_Card08をChoice2_範囲外_When_IsLegalMove_Then_false` | ChoiceEffect 範囲外、DZ-175 と同パターン |
