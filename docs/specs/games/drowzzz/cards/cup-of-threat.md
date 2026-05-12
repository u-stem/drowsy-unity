# カード No.01「コップ一杯の脅威」 (M2-PR3)

DrowZzz の最初の効果付きカード(プロジェクトオーナー JIT 共有 2026-05-11)。夜と朝で完全に逆転した効果を持つことで、ADR-0009「戦略示唆」(夜=寝かせまい / 朝=眠くさせる)の対称性を体現する。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.01 |
| 名前 | コップ一杯の脅威 |
| 初期山札枚数 | 2 |
| CardId | `"01"`(JIT 共有: シンプルな数字 ID 採用、M4 SO 化時に再評価) |
| 効果構造 | `TimeOfDayBranchEffect` 1 件で夜・朝の効果列をまとめて持つ |

### 効果

プレイ時、現在時刻に応じて以下の効果が発動する。

#### 夜(`Clock.IsNight`、Turn 1〜16)

- 自分の SDP が 4 減る
- 自分は山札から手段(カード)を 1 枚引く
- 相手の SDP が 10 減る

#### 朝(`Clock.IsMorning`、Turn 17〜21)

- 自分の SDP が 4 減る
- 相手の SDP が 10 増える

戦略的解釈:
- **夜**: 「自分も少し寝るが相手をもっと寝かせまいとし、手数を確保する」 → 夜の戦略「相手を寝かせまい(相手 DP↓)」と整合
- **朝**: 「自分は少し起きるが相手はもっと眠くなる」 → 朝の戦略「相手を眠くさせる(相手 DP↑)」と整合

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側 (CardData は名前のみ、属性は空)
new KeyValuePair<CardId, CardData>(CardId.Of("01"), new CardData("コップ一杯の脅威", new Dictionary<string, int>()))

// effects 側 (TimeOfDayBranchEffect 1 件で夜・朝を分岐)
new KeyValuePair<CardId, IReadOnlyList<IEffect>>(CardId.Of("01"), new IEffect[]
{
    new TimeOfDayBranchEffect(
        nightEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),      // 自分 SDP -4
            new DrawCardEffect(SdpTarget.Self, 1),        // 自分が山札から 1 枚引く
            new AdjustSdpEffect(SdpTarget.Opponent, -10), // 相手 SDP -10
        },
        morningEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),      // 自分 SDP -4
            new AdjustSdpEffect(SdpTarget.Opponent, 10),  // 相手 SDP +10
        }
    ),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-125] [Ubiquitous] Card `"01"` shall be registered in the initial `InMemoryCardCatalog` (or initial deck factory) with name `"コップ一杯の脅威"` and a single `TimeOfDayBranchEffect` containing the night/morning effect lists specified above.

## 事象駆動要件 (Event-driven)

- [DZ-126] When Card `"01"` is played by player A on the opponent player B while `session.Clock.IsNight` is `true`, the resulting session shall reflect `SDP[A] -= 4`, A 's hand gaining 1 top card from the deck, and `SDP[B] -= 10`.
- [DZ-127] When Card `"01"` is played by player A on the opponent player B while `session.Clock.IsMorning` is `true`, the resulting session shall reflect `SDP[A] -= 4` and `SDP[B] += 10`(no additional draw).

## 定数依存

該当なし。数値 `-4` / `-10` / `+10` / `+1` はカードデータ固有(JIT 共有)で、定数集約しない(L3 個別カード設計値)。

## 関連

- ADR: [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../../adr/0009-m2-m3-dp-and-victory-conditions.md) §「戦略示唆」 — 夜・朝戦略反転の根拠
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/draw-card-effect.md`](../effects/draw-card-effect.md) / [`../effects/time-of-day-branch.md`](../effects/time-of-day-branch.md)
- 実装(本 PR、カード登録):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/CupOfThreatCardTests.cs`(統合テスト + InMemoryCardCatalog 登録例)
- シナリオ: `cup-of-threat.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-125 | (テスト免除: Ubiquitous) | 登録例は `CupOfThreatCardTests` のヘルパーに記述、構造的に保証 |
| DZ-126 | DZ-126 は 1 テスト 1 アサーション原則で 3 件に分割: `Given_夜のRound_When_Card01をプレイ_Then_自分のSDPがマイナス4` / `Given_夜のRound_When_Card01をプレイ_Then_相手のSDPがマイナス10` / `Given_夜のRound_When_Card01をプレイ_Then_自分の手札に山札topがドローされる` | 統合テスト(Category("Medium")) |
| DZ-127 | DZ-127 も 3 件に分割: `Given_朝のRound_When_Card01をプレイ_Then_自分のSDPがマイナス4` / `Given_朝のRound_When_Card01をプレイ_Then_相手のSDPがプラス10` / `Given_朝のRound_When_Card01をプレイ_Then_ドロー効果は発動しない` | 統合テスト(Category("Medium")) |
