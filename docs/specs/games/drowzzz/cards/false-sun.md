# カード No.12「偽りの太陽」 (Phase 2 完結後、ADR-0022 と同 PR)

DrowZzz Phase 2 完結後の **第 10 新規カード追加**(2026-05-17 オーナー JIT 確定)。**Reactive Influence(アクション後発動型)の初導入** で、夜にプレイすると永続的に「使用したら SDP-10 / 放棄したら SDP+5」の影響を自分に背負う戦術カード。ADR-0022「InfluenceTrigger 拡張」で `OnOwnPlayCardAfter` / `OnOwnAbandonAfter` 2 値を追加し本カードの consumer となる。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.12 |
| 名前 | 偽りの太陽 |
| CardTypeId | `"12"` |
| 初期山札枚数 | 2(オーナー JIT 確定 2026-05-17、Phase 3 本物デッキ。M5 簡易デッキでは uniform 20 維持)|
| 効果構造 | `TimeOfDayBranchEffect`(夜分岐 4 件 / 朝分岐 2 件)|
| 新規導入概念 | `AdjustSdpAfterPlayCardEffect(int Delta)` / `AdjustSdpAfterAbandonEffect(int Delta)`(Reactive TickEffect、ADR-0022 と同 PR)|

## 効果

### 夜(`Clock.IsNight`、Round 1〜16)

- 自分の SDP が 4 減る
- 相手の SDP が 6 増える
- 甲(自分)に **2 件の永続 Reactive Influence** を付与:
  - `PlayerInfluence(OnOwnPlayCardAfter, AdjustSdpAfterPlayCardEffect(-10), Perpetual)` — 自分の **次回以降** の PlayCardAction 後に SDP-10
  - `PlayerInfluence(OnOwnAbandonAfter, AdjustSdpAfterAbandonEffect(+5), Perpetual)` — 自分の **次回以降** の AbandonAction 後に SDP+5

### 朝(`Clock.IsMorning`、Round 17〜21)

- 自分の SDP が 4 減る
- 相手の SDP が 18 増える(大きく覚醒させる)
- **影響付与なし**(朝は即時 SDP 変動のみ、永続影響を背負わない)

戦略的解釈:
- **夜:自分 -4 のコスト + 相手 +6 のささやかな攻撃 + 永続自己制約**。短期コストは小さいが、永続的に「カード使用ごとに -10 / 放棄ごとに +5」の制約を背負う。攻撃的プレイヤーには **加速ボーナス**(使用 -10 で SDP がどんどん減る = 眠くなる)、消極的プレイヤーには **逆風**(放棄ごとに +5 = 目覚める)
- **朝:即時 SDP -4/+18 のみ**(永続影響なし)、大火力単発攻撃カード
- 「偽りの太陽」フレーバー = 夜に「太陽が見える(=朝が来た)」と錯覚するが、実は永続影響を背負っているという「偽り」
- 朝に使うと「本物の太陽」を呼び込む(=相手を大覚醒させる)が、永続影響はなし

## 「カウント」記述なし = Perpetual の解釈

テキスト「カウント」記述なし = `InfluenceConstants.Perpetual`(int.MaxValue)で永続(No.08「廻るための知恵」/ No.11「機械仕掛けの冬将軍」と同パターン)。

## 付与直後の本カードプレイへの非適用

オーナー JIT 確定 2026-05-17:**付与直後の同一ターンで、本カードプレイした PlayCardAction には影響が適用されない**。

実装は ADR-0022 §4「snapshot ベース walk」で構造的に保証:`ApplyPlayCard` 冒頭で `session.Influences[currentPlayerId]` を snapshot し、末尾で snapshot を walk → 本 PlayCard で新規付与された影響は snapshot 外で対象外。

## カード固有「影響」(2 件)

| 観点 | PlayCard 後 SDP-10 | Abandon 後 SDP+5 |
| ---- | ---- | ---- |
| トリガー | `InfluenceTrigger.OnOwnPlayCardAfter` | `InfluenceTrigger.OnOwnAbandonAfter` |
| Tick 効果 | `AdjustSdpAfterPlayCardEffect(Delta=-10)` | `AdjustSdpAfterAbandonEffect(Delta=+5)` |
| 残発動回数 | `InfluenceConstants.Perpetual`(永続)| `InfluenceConstants.Perpetual`(永続)|
| 発動箇所 | `DrowZzzRule.ApplyPlayCard` 末尾の `ApplyReactiveInfluencesAfter` walk | `DrowZzzRule.ApplyAbandon` 末尾の `ApplyReactiveInfluencesAfter` walk |
| Semantics | 保有者の自フェーズで PlayCardAction を実行するたびに保有者の SDP に Delta=-10 を加算 | 保有者の AbandonAction(GainSdp / RepairBed 両方)実行ごとに SDP に Delta=+5 を加算 |

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("12"), new CardData("偽りの太陽", new Dictionary<string, int>()))

// 影響定義(2 件、永続 Reactive)
var PlayCardReactiveInfluence = new PlayerInfluence(
    InfluenceTrigger.OnOwnPlayCardAfter,
    new AdjustSdpAfterPlayCardEffect(-10),
    InfluenceConstants.Perpetual);

var AbandonReactiveInfluence = new PlayerInfluence(
    InfluenceTrigger.OnOwnAbandonAfter,
    new AdjustSdpAfterAbandonEffect(5),
    InfluenceConstants.Perpetual);

// effects 側(TimeOfDayBranchEffect 1 件最上位、夜 4 件 / 朝 2 件)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("12"), new IEffect[]
{
    new TimeOfDayBranchEffect(
        nightEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),
            new AdjustSdpEffect(SdpTarget.Opponent, 6),
            new ApplyInfluenceEffect(SdpTarget.Self, PlayCardReactiveInfluence),
            new ApplyInfluenceEffect(SdpTarget.Self, AbandonReactiveInfluence),
        },
        morningEffects: new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),
            new AdjustSdpEffect(SdpTarget.Opponent, 18),
        }
    ),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-334] [Ubiquitous] Card `"12"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"偽りの太陽"` and a single top-level `TimeOfDayBranchEffect` containing the night/morning branches specified above.

## 事象駆動要件 (Event-driven、夜)

- [DZ-335] When Card `"12"` is played by player A on player B during night phase, the resulting session shall reflect `SDP[A] -= 4`。
- [DZ-336] When Card `"12"` is played by player A on player B during night phase, the resulting session shall reflect `SDP[B] += 6`。
- [DZ-337] When Card `"12"` is played by player A during night phase, A's influence list shall gain 2 new influences:`PlayerInfluence(OnOwnPlayCardAfter, AdjustSdpAfterPlayCardEffect(-10), Perpetual)` と `PlayerInfluence(OnOwnAbandonAfter, AdjustSdpAfterAbandonEffect(+5), Perpetual)`。

## 事象駆動要件 (Event-driven、朝)

- [DZ-338] When Card `"12"` is played by player A on player B during morning phase, the resulting session shall reflect `SDP[A] -= 4`。
- [DZ-339] When Card `"12"` is played by player A on player B during morning phase, the resulting session shall reflect `SDP[B] += 18`。
- [DZ-340] When Card `"12"` is played by player A during morning phase, A's influence list shall remain unchanged(朝は影響付与なし)。

## Reactive Influence 評価のシナリオ(ADR-0022)

- [DZ-341] When A holds `PlayerInfluence(OnOwnPlayCardAfter, AdjustSdpAfterPlayCardEffect(-10), Perpetual)` and A executes a `PlayCardAction` for any other card (not Card "12"), the resulting session shall reflect `SDP[A] -= 10`(本 Reactive walk が機能)。
- [DZ-342] When A holds `PlayerInfluence(OnOwnAbandonAfter, AdjustSdpAfterAbandonEffect(+5), Perpetual)` and A executes `AbandonAction(Choice=GainSdp)`, the resulting session shall reflect `SDP[A] += 10`(`GainSdp +5` 既存効果 + Reactive `+5` の合算)。`Choice=RepairBed` の場合は Reactive `+5` のみが SDP に反映され、ベッド修繕は `BedDamages` に影響する(別シナリオでカバー)。
- [DZ-343] When A executes a `PlayCardAction` for Card "12" during night phase (本カード自身のプレイ), the resulting session shall **not** reflect the Reactive SDP-10(snapshot ベース walk で付与時の本アクション自身は対象外、ADR-0022 §4)。
- [DZ-344] When B(opponent)holds the Reactive influences and A executes `PlayCardAction`, B's SDP shall **not** change(本 Reactive は保有者 A 自身のアクションでのみ発動、B のアクションでは発動しない)。

## 定数依存

- **L3(カード設計値、本カード固有)**:
  - 夜 SDP 即時変動:自分 -4 / 相手 +6
  - 朝 SDP 即時変動:自分 -4 / 相手 +18
  - Reactive 値:PlayCard 後 -10 / Abandon 後 +5
  - 影響 RemainingCount:`InfluenceConstants.Perpetual`(int.MaxValue、永続)

## 関連

- ADR:
  - [`docs/adr/0022-reactive-influence-trigger-extension.md`](../../../../adr/0022-reactive-influence-trigger-extension.md)(本 PR 同梱、Reactive Influence Trigger 拡張)
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」(意味論レベルでは ADR-0007 を維持)
  - [`docs/adr/0020-influence-count-decrement-timing.md`](../../../../adr/0020-influence-count-decrement-timing.md)(Decrement は全 Influence trigger 一律適用、Reactive Influence も Perpetual で実質除去なし)
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md) / [`../effects/time-of-day-branch.md`](../effects/time-of-day-branch.md)
- 既存類似カード:
  - [`./cup-of-threat.md`](./cup-of-threat.md)(No.01、TimeOfDayBranchEffect 同パターン)
  - [`./circulating-wisdom.md`](./circulating-wisdom.md)(No.08、Perpetual Influence)
  - [`./clockwork-winter-general.md`](./clockwork-winter-general.md)(No.11、Perpetual Influence + Tick 動的計算)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AdjustSdpAfterPlayCardEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AdjustSdpAfterAbandonEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Influences/InfluenceTrigger.cs`(enum 2 値追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyPlayCard` / `ApplyAbandon` の snapshot + Reactive walk + `ApplyReactiveInfluencesAfter` ヘルパー新設)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(2 case + `ApplyAdjustSdpDeltaForCurrentPlayer` ヘルパー)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/AdjustSdpAfterPlayCardEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/AdjustSdpAfterAbandonEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(2 dispatch case)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.12 entry + rid 1000〜1009 追加)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「12 → 13 種」)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/FalseSunCardTests.cs`(新規、DZ-335〜344)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/FalseSunCardCatalogTests.cs`(新規、SO 同等性、INF-153)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/EffectJsonConverterTests.cs`(INF-154/155、Round-Trip)
- シナリオ: `false-sun.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-334 | (テスト免除: Ubiquitous) | catalog 登録は `FalseSunCardTests` のヘルパー + `FalseSunCardCatalogTests` で構造的に保証 |
| DZ-335 | `Given_夜_When_Card12をプレイ_Then_自分のSDPがマイナス4` | 統合 |
| DZ-336 | `Given_夜_When_Card12をプレイ_Then_相手のSDPがプラス6` | 統合 |
| DZ-337 | `Given_夜_When_Card12をプレイ_Then_自分のInfluencesに2件追加される` | 統合 |
| DZ-338 | `Given_朝_When_Card12をプレイ_Then_自分のSDPがマイナス4` | 統合 |
| DZ-339 | `Given_朝_When_Card12をプレイ_Then_相手のSDPがプラス18` | 統合 |
| DZ-340 | `Given_朝_When_Card12をプレイ_Then_自分のInfluencesは不変空` | 統合 |
| DZ-341 | `Given_p1が本ReactiveInfluence保有_When_他カードをプレイ_Then_p1のSDPがマイナス10` | Reactive walk |
| DZ-342 | `Given_p1が本ReactiveInfluence保有_When_AbandonAction_Then_p1のSDPがプラス10`(GainSdp 経路、合算 +10)+ `Given_p1が本ReactiveInfluence保有_When_AbandonActionRepairBed_Then_p1のSDPがプラス5`(RepairBed 経路、Reactive のみ +5、P-7 反映)| Reactive walk(Choice 2 経路で分離検証)|
| DZ-343 | `Given_夜_p1が本カードプレイ_Then_本カードプレイ自体にReactiveが適用されない` | snapshot ベース walk |
| DZ-344 | `Given_p2が本ReactiveInfluence保有_When_p1がPlayCardAction_Then_p2のSDPは不変` | 他プレイヤー保護 |
