# カード No.03「身体にいいもの」 (Phase 2 完結後追加)

DrowZzz Phase 2 完結後の **初の新規カード追加**(2026-05-17 オーナー JIT 確定)。No.01「コップ一杯の脅威」(時間帯分岐)と No.02「緑の侵攻」(継続影響付与)を組み合わせ、さらに「永続影響」概念(`InfluenceConstants.Perpetual`)を初導入する。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.03 |
| 名前 | 身体にいいもの |
| CardTypeId | `"03"` |
| 初期山札枚数 | 1(Phase 3 本物デッキ枚数、M5 簡易デッキでは uniform 20 で生成)|
| 効果構造 | `TimeOfDayBranchEffect` 1 件で夜・朝の効果列を保有 |
| 新規導入概念 | `InfluenceConstants.Perpetual`(永続影響、`int.MaxValue` をマジック値とする) |

## 効果

プレイ時、現在時刻に応じて以下の効果が発動する。

### 夜(`Clock.IsNight`、Turn 1〜16)

- 自分の SDP が 20 減る
- 相手の SDP が 5 増える
- 自分にこのカードの **影響 x** を与える(永続)

### 朝(`Clock.IsMorning`、Turn 17〜21)

- 自分の SDP が 10 減る
- 相手の SDP が 5 増える
- 自分にこのカードの **影響 y** を与える(永続)

## カード固有「影響 x / y」

| 観点 | 影響 x(夜分岐由来) | 影響 y(朝分岐由来) |
| ---- | ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart` | `InfluenceTrigger.OwnPhaseStart` |
| Tick 効果 | `AdjustSdpEffect(SdpTarget.Self, +4)` | `AdjustSdpEffect(SdpTarget.Self, -6)` |
| 残発動回数 | `InfluenceConstants.Perpetual` (= `int.MaxValue`、永続) | 同上 |

### 永続表現の根拠

`InfluenceConstants.Perpetual = int.MaxValue` を「実質枯渇しないマジック値」として用いる。採用経緯・代替案比較・枯渇不能の論証は [`InfluenceConstants.Perpetual` の xmldoc](../../../../../Assets/_Project/Scripts/Application/Games/DrowZzz/Influences/InfluenceConstants.cs) を単一情報源とする(SoT 集約、code-reviewer P-2 反映 2026-05-17)。

戦略的解釈:
- **夜**: 即時 SDP -20 + 相手 +5 の重コスト = 短期的には自分が大きく起きるが、毎自フェーズ +4 で長期的に SDP を戻していく自己回復ビルド
- **朝**: 即時 SDP -10 + 相手 +5 + 毎自フェーズ -6 = 朝の戦略「相手を眠くさせる」前に自分が完全覚醒する高速覚醒ビルド(朝は残りターン数が少ないため -6 の累積も限定的)

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側 (CardData は名前のみ、属性は空)
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("03"), new CardData("身体にいいもの", new Dictionary<string, int>()))

// effects 側 (TimeOfDayBranchEffect 1 件で夜・朝を分岐)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("03"), new IEffect[]
{
    new TimeOfDayBranchEffect(
        nightEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -20),
            new AdjustSdpEffect(SdpTarget.Opponent, 5),
            new ApplyInfluenceEffect(
                SdpTarget.Self,
                new PlayerInfluence(
                    InfluenceTrigger.OwnPhaseStart,
                    new AdjustSdpEffect(SdpTarget.Self, 4),
                    InfluenceConstants.Perpetual)),
        },
        morningEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -10),
            new AdjustSdpEffect(SdpTarget.Opponent, 5),
            new ApplyInfluenceEffect(
                SdpTarget.Self,
                new PlayerInfluence(
                    InfluenceTrigger.OwnPhaseStart,
                    new AdjustSdpEffect(SdpTarget.Self, -6),
                    InfluenceConstants.Perpetual)),
        }
    ),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-246] [Ubiquitous] Card `"03"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"身体にいいもの"` and a single `TimeOfDayBranchEffect` containing the night/morning effect lists specified above.

## 事象駆動要件 (Event-driven)

- [DZ-247] When Card `"03"` is played by player A on the opponent player B while `session.Clock.IsNight` is `true`, the resulting session shall reflect `SDP[A] -= 20`.
- [DZ-248] When Card `"03"` is played by player A on the opponent player B while `session.Clock.IsNight` is `true`, the resulting session shall reflect `SDP[B] += 5`.
- [DZ-249] When Card `"03"` is played by player A while `session.Clock.IsNight` is `true`, A's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, +4), Perpetual)` entry (= "影響 x" / Night-derived).
- [DZ-250] When Card `"03"` is played by player A on the opponent player B while `session.Clock.IsMorning` is `true`, the resulting session shall reflect `SDP[A] -= 10`.
- [DZ-251] When Card `"03"` is played by player A on the opponent player B while `session.Clock.IsMorning` is `true`, the resulting session shall reflect `SDP[B] += 5`.
- [DZ-252] When Card `"03"` is played by player A while `session.Clock.IsMorning` is `true`, A's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, -6), Perpetual)` entry (= "影響 y" / Morning-derived).

## Tick 評価のシナリオ

- [DZ-253] When A holds "影響 x"(`PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, +4), Perpetual)`) and a phase rotation makes A the new current player, the resulting session shall reflect `SDP[A] += 4` and A's "影響 x" shall have `RemainingCount == Perpetual - 1`(= `int.MaxValue - 1`、永続継続)。
- [DZ-254] When A holds "影響 y"(`PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, -6), Perpetual)`) and a phase rotation makes A the new current player, the resulting session shall reflect `SDP[A] -= 6` and A's "影響 y" shall have `RemainingCount == Perpetual - 1`。

## 定数依存

- **L2(ドメイン上の真の不変量、Application `InfluenceConstants`)**:
  - `InfluenceConstants.Perpetual` = `int.MaxValue`(永続影響を表すマジック値、本カード初導入)
- **L3(カード設計値、本カード固有)**:
  - 夜 SDP 即時変動: 自分 -20 / 相手 +5
  - 朝 SDP 即時変動: 自分 -10 / 相手 +5
  - 影響 x Tick 効果値: SDP +4(自分)
  - 影響 y Tick 効果値: SDP -6(自分)

上記 L3 値はカード固有設計値のため定数集約しない(L3 = ゲームバランス調整可能値の方針、CLAUDE.md §9)。

## 関連

- ADR:
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」 — Influence 概念の起点
  - [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../../adr/0009-m2-m3-dp-and-victory-conditions.md) §「戦略示唆」 — 夜・朝戦略反転の根拠
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md) / [`../effects/time-of-day-branch.md`](../effects/time-of-day-branch.md)
- 前提モデル: [`../influences/influence-model.md`](../influences/influence-model.md)
- 既存類似カード:
  - [`./cup-of-threat.md`](./cup-of-threat.md)(No.01、時間帯分岐パターン)
  - [`./green-invasion.md`](./green-invasion.md)(No.02、Influence 付与パターン)
- 実装(本 PR、統合テスト):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/GoodForBodyCardTests.cs`
- SO 同等性テスト:
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/GoodForBodyCardCatalogTests.cs`
- シナリオ: `good-for-body.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-246 | (テスト免除: Ubiquitous) | 登録例は `GoodForBodyCardTests` のヘルパー + `GoodForBodyCardCatalogTests` で構造的に保証 |
| DZ-247 | `Given_夜のフェーズ_When_Card03をプレイ_Then_自分のSDPがマイナス20` | 統合テスト(Category("Medium")) |
| DZ-248 | `Given_夜のフェーズ_When_Card03をプレイ_Then_相手のSDPがプラス5` | 統合テスト(Category("Medium")) |
| DZ-249 | `Given_夜のフェーズ_When_Card03をプレイ_Then_自分のInfluencesに永続Plus4が追加される` | 統合テスト(Category("Medium")) |
| DZ-250 | `Given_朝のフェーズ_When_Card03をプレイ_Then_自分のSDPがマイナス10` | 統合テスト(Category("Medium")) |
| DZ-251 | `Given_朝のフェーズ_When_Card03をプレイ_Then_相手のSDPがプラス5` | 統合テスト(Category("Medium")) |
| DZ-252 | `Given_朝のフェーズ_When_Card03をプレイ_Then_自分のInfluencesに永続Minus6が追加される` | 統合テスト(Category("Medium")) |
| DZ-253 | `Given_永続影響Plus4を保有_p1current_When_p1がEndTurn適用_Then_p2自フェーズ開始でTickなし` + `Given_p2が永続影響Plus4を保有_p1current_When_p1がEndTurnでp2フェーズへ_Then_p2のSDPがプラス4` + `Then_p2の影響RemainingCountがPerpetualマイナス1` | 3 件に分割(他プレイヤー無影響 + Tick 値 + RemainingCount 減算) |
| DZ-254 | `Given_p2が永続影響Minus6を保有_p1current_When_p1がEndTurnでp2フェーズへ_Then_p2のSDPがマイナス6` + `Then_p2の影響RemainingCountがPerpetualマイナス1` | 2 件に分割 |
