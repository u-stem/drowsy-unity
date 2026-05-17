# カード No.05「喧騒を纏う」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 4 新規カード追加**(2026-05-17 オーナー JIT 確定)。No.04「静寂を纏う」と対をなす **対人介入カード**:自分の手札から 1 枚を選び共通山札の top に押し込む = 「次のターンの相手のドロー」を操作する戦術カード。No.04 と同じく **連想由来カードは選択不可** で、PR #115 で整備した `AssociatedCardIds` を 2 つ目の consumer として利用する。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.05 |
| 名前 | 喧騒を纏う |
| CardTypeId | `"05"` |
| 初期山札枚数 | **2**(Phase 3 本物デッキ、M5 簡易デッキでは uniform 20 維持。No.04 と同じ枚数、オーナー JIT 確定 2026-05-17)|
| 効果構造 | **2 件最上位**:(1) `TimeOfDayBranchEffect`(時間帯依存の SDP 変動のみ)+ (2) `StackHandCardOnDeckTopEffect(Self)`(時間帯非依存、最上位配置、No.04 設計判断と同パターン)|
| 新規導入概念 | `StackHandCardOnDeckTopEffect`(指定 Source プレイヤーの手札 → 共通 Deck top への移動) |

## 効果

プレイ時、現在時刻に応じて以下の効果が発動する。**actor=甲(自分)** が自分の手札から 1 枚選択(連想由来除外)し、共通山札 top に置く。

### 夜(`Clock.IsNight`、Turn 1〜16)

- 自分の SDP が 8 減る
- 相手の SDP が 18 減る(夜の戦略「相手を寝かせまい」、大幅減で起こす)
- 自分の手札から 1 枚を選択し共通山札 top に置く(`PlayCardAction.TargetCardId`)
  - **連想された手段は選択不可**(`AssociatedCardIds` 含有 CardId は除外)

### 朝(`Clock.IsMorning`、Turn 17〜21)

- 自分の SDP が 4 減る
- 相手の SDP が 12 増える(朝の戦略「相手を眠くさせる」、大幅増で寝かせる)
- 自分の手札から 1 枚を選択し共通山札 top に置く(同上)

戦略的解釈:
- **夜**:自分軽微負担(-8)+ 相手大幅起き(-18)+ 自分の不利カード(SDP マイナス系)を山札 top に押し込む → 次ターン開始時に相手が DrawCardAction で引く = 不利カードを相手に押し付け
- **朝**:自分軽微負担(-4)+ 相手大幅眠く(+12)+ 自分の不利カードを相手に押し付け
- **No.04 との対比**:No.04 = 相手の手札の特定カードを 2 ターン使用禁止(妨害)、No.05 = 自分の手札の特定カードを相手に引かせる(押し付け)。両者で **「カードの動き」を制御する戦術カード対** を形成

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側 (CardData は名前のみ、属性は空)
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("05"), new CardData("喧騒を纏う", new Dictionary<string, int>()))

// effects 側(最上位 2 件、No.04 と同設計判断:ApplyTargetedRestrictionEffect / StackHandCardOnDeckTopEffect を最上位配置)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("05"), new IEffect[]
{
    // (1) TimeOfDayBranchEffect:時間帯依存の SDP 変動のみ
    new TimeOfDayBranchEffect(
        nightEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -8),
            new AdjustSdpEffect(SdpTarget.Opponent, -18),
        },
        morningEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),
            new AdjustSdpEffect(SdpTarget.Opponent, 12),
        }
    ),
    // (2) StackHandCardOnDeckTopEffect:時間帯非依存の山札 top 押し込み(最上位、夜/朝両方で発動)
    new StackHandCardOnDeckTopEffect(SdpTarget.Self),
})
```

### `StackHandCardOnDeckTopEffect` を最上位に置く理由

No.04「静寂を纏う」と同じ設計判断:`DrowZzzRule.IsLegalPlayCard` の効果列 walk は **最上位のみ**(M3-PR6 で確定した「nested は walk しない」方針、`HasKeywordInEffect` と同方針)のため、nested 配置だと `StackHandCardOnDeckTopEffect` 検出経路が動作せず TargetCardId 必須検証 / 連想由来除外検証 / Source 手札所持検証が **静的に通って illegal 化されないバグ** になる(No.04 開発時に DZ-264/265/266 失敗で発覚した教訓を踏襲)。最上位配置で両方解決。

## 普遍要件 (Ubiquitous)

- [DZ-269] [Ubiquitous] Card `"05"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"喧騒を纏う"` and **2 top-level effects**: a `TimeOfDayBranchEffect`(SDP 変動のみ) + a `StackHandCardOnDeckTopEffect(SdpTarget.Self)`.

## 事象駆動要件 (Event-driven)

- [DZ-270] When Card `"05"` is played by player A with `TargetCardId = c`(c in A's hand) while `session.Clock.IsNight` is `true`, the resulting session shall reflect `SDP[A] -= 8`.
- [DZ-271] When Card `"05"` is played by player A with `TargetCardId = c` while `session.Clock.IsNight` is `true`, the resulting session shall reflect `SDP[B] -= 18`.
- [DZ-272] When Card `"05"` is played by player A with `TargetCardId = c` while `session.Clock.IsNight` is `true`, A's hand shall no longer contain `c` and the common deck's top shall be `c`.
- [DZ-273] When Card `"05"` is played by player A with `TargetCardId = c` while `session.Clock.IsMorning` is `true`, the resulting session shall reflect `SDP[A] -= 4`, `SDP[B] += 12`, and A's hand shall no longer contain `c` while the deck top shall be `c`.

## 合法性判定(`IsLegalMove` 拡張)

- [DZ-274] When Card `"05"` is played without specifying `TargetCardId`(= null), `IsLegalMove` shall return `false`(StackHandCardOnDeckTopEffect 含むカードは TargetCardId 必須)。
- [DZ-275] When Card `"05"` is played with `TargetCardId = c` where `c` is **not in the current player's hand**(Source=Self), `IsLegalMove` shall return `false`(対象カード不在の選択は無効)。
- [DZ-276] When Card `"05"` is played with `TargetCardId = c` where `c` is contained in `session.AssociatedCardIds`(連想由来), `IsLegalMove` shall return `false`(連想由来カードは選択不可、ADR-0019)。
- [DZ-277] When Card `"05"` is played with `TargetCardId = action.Card`(プレイ中のカード自体を選択), `IsLegalMove` shall return `false`。プレイ中のカードは Field に移動するため Deck top に重ねる対象として矛盾、仕様の暗黙解釈を明示化(2026-05-17 code-reviewer W-2 反映)。同じ条件は No.04(`ApplyTargetedRestrictionEffect`)にも適用される(`IsLegalTargetCardId` ヘルパー共通化)。

## 定数依存

- **L3(カード設計値、本カード固有)**:
  - 夜 SDP 即時変動: 自分 -8 / 相手 -18
  - 朝 SDP 即時変動: 自分 -4 / 相手 +12

上記 L3 値はカード固有設計値のため定数集約しない(L3 = ゲームバランス調整可能値の方針、CLAUDE.md §9)。

## 関連

- ADR:
  - [`docs/adr/0019-associated-card-ids-session-field.md`](../../../../adr/0019-associated-card-ids-session-field.md) — AssociatedCardIds 設計基盤(PR ①)+ 連想由来除外パターン
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.3 EffectInterpreter / §1.5「継続影響(Influence)」
  - [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../../adr/0009-m2-m3-dp-and-victory-conditions.md) §「戦略示唆」
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/time-of-day-branch.md`](../effects/time-of-day-branch.md)
- 前提効果(本 PR で実装、spec md は未作成):`StackHandCardOnDeckTopEffect`(`Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/StackHandCardOnDeckTopEffect.cs` 参照、xmldoc に Design 記述あり)。Effect 単体仕様 md は将来 effect 単体仕様の網羅整理 PR で起票予定(No.04 と同方針)
- 既存類似カード:
  - [`./sound-of-silence.md`](./sound-of-silence.md)(No.04、相手の手札の特定カードを 2 ターン使用禁止)
  - [`./good-for-body.md`](./good-for-body.md)(No.03、永続 Influence 付与)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/StackHandCardOnDeckTopEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalPlayCard` 拡張 + `IsLegalTargetCardId` ヘルパー共通化)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(1 case + `ApplyStackHandCardOnDeckTop`)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/StackHandCardOnDeckTopEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/CommotionCardTests.cs`
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/CommotionCardCatalogTests.cs`
- シナリオ: `commotion.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-269 | (テスト免除: Ubiquitous) | catalog 登録は `CommotionCardTests` のヘルパー + `CommotionCardCatalogTests` で構造的に保証 |
| DZ-270 | `Given_夜のフェーズ_When_Card05をプレイ_Then_自分のSDPがマイナス8` | 統合テスト |
| DZ-271 | `Given_夜のフェーズ_When_Card05をプレイ_Then_相手のSDPがマイナス18` | 統合テスト |
| DZ-272 | `Given_夜のフェーズ_When_Card05をプレイ_Then_自分の手札から対象が除去される` + `Then_共通山札のtopが対象になる` | 2 件分割(手札 Remove + Deck top 配置)|
| DZ-273 | `Given_朝のフェーズ_When_Card05をプレイ_Then_*` 3 件(SDP[A]-4 / SDP[B]+12 / 手札 Remove + Deck top)| 統合テスト |
| DZ-274 | `Given_TargetCardIdなし_When_Card05をIsLegalMove_Then_false` | IsLegalPlayCard 防御 |
| DZ-275 | `Given_自分手札にないTargetCardId_When_Card05をIsLegalMove_Then_false` | Source=Self の手札所持検証 |
| DZ-276 | `Given_AssociatedCardIds含有のTargetCardId_When_Card05をIsLegalMove_Then_false` | 連想由来除外(PR ① consumer)|
| DZ-277 | `Given_TargetCardIdがプレイ中のCardと同一_When_Card05をIsLegalMove_Then_false` | プレイ中カード自体を対象に不可、`IsLegalTargetCardId` 共通ヘルパーで No.04 にも適用 |
