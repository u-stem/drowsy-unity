# カード No.02「緑の侵攻」 (M2-PR5)

DrowZzz の最初の選択式カード(プロジェクトオーナー JIT 共有 2026-05-11)。「**継続影響**」概念を導入し、選択肢ごとに「自分が即時コストを払う / 相手に即時負担を強いる」の 2 系統の振る舞いを切り替える。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.02 |
| 名前 | 緑の侵攻 |
| CardId | `"02"` |
| 効果構造 | `ChoiceEffect` 1 件で 2 選択肢を保有 |
| 新規導入概念 | 選択式カード(`ChoiceEffect`)/ 継続影響付与(`ApplyInfluenceEffect`)/ 継続影響除去(`RemoveInfluenceEffect`) |

## 効果

プレイ時、自分(プレイヤー)は以下のいずれかを選ぶ。

### 選択 1

- 自分の SDP が 6 減る
- 相手の持つ影響から 1 つを選択し、それを消滅させる
- 相手にこのカードの影響を与える

### 選択 2

- 相手の SDP が 6 増える
- 自分の持つ影響から 1 つを選択し、それを消滅させる
- 自分にこのカードの影響を与える

JIT 確定事項(2026-05-11):
- 数値「6」は actor=Self / actor=Opponent の SDP 即時変動を指す(`AdjustSdpEffect` の引数として表現)
- 「影響を 1 つ消滅させる」の対象選択方式は **プレイヤーが index を指定する**(UI 上は選択肢呈示、`PlayCardAction.InfluenceRemovalIndex` で action に index を持たせる)

## カード固有「影響」

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart`(影響保有者の自フェーズ開始時、ボードゲーム用語の Phase 単位)|
| Tick 効果 | `AdjustSdpEffect(SdpTarget.Self, -5)`(保有者の SDP -5)|
| 残発動回数 | 3(0 到達時に除去、JIT 確定: 「カウント = 発動回数」、ターン経過数ではない)|

戦略的解釈:
- **選択1(攻撃的)**: 自分が -6 SDP のコストを払い、相手に長期的負担(3 ターンで -15 SDP 相当)を強いる
- **選択2(防御的)**: 自分の既存影響を解除しつつ新規影響を引き受ける(リセット行動)。相手にも即時 +6 SDP の負担

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側 (CardData は名前のみ、属性は空)
new KeyValuePair<CardId, CardData>(CardId.Of("02"), new CardData("緑の侵攻", new Dictionary<string, int>()))

// effects 側 (ChoiceEffect 1 件で 2 分岐を保有)
new KeyValuePair<CardId, IReadOnlyList<IEffect>>(CardId.Of("02"), new IEffect[]
{
    new ChoiceEffect(new IEffect[][]
    {
        // 選択1
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -6),
            new RemoveInfluenceEffect(SdpTarget.Opponent),
            new ApplyInfluenceEffect(SdpTarget.Opponent, GreenInvasionInfluence),
        },
        // 選択2
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Opponent, 6),
            new RemoveInfluenceEffect(SdpTarget.Self),
            new ApplyInfluenceEffect(SdpTarget.Self, GreenInvasionInfluence),
        },
    }),
})

// 影響定義
var GreenInvasionInfluence = new PlayerInfluence(
    InfluenceTrigger.OwnPhaseStart,
    new AdjustSdpEffect(SdpTarget.Self, -5),
    3);
```

## 普遍要件 (Ubiquitous)

- [DZ-170.0] [Ubiquitous] Card `"02"` shall be registered in the initial `InMemoryCardCatalog` with name `"緑の侵攻"` and a single `ChoiceEffect` containing the 2 branches specified above.

## 事象駆動要件 (Event-driven)

- [DZ-170] When Card `"02"` is played by player A on player B with `Choice == 0`, the resulting session shall reflect `SDP[A] -= 6`.
- [DZ-171] When Card `"02"` is played by A on B with `Choice == 0`, B's influence list shall gain 1 new `GreenInvasionInfluence` entry.
- [DZ-172] When Card `"02"` is played by A on B with `Choice == 0` and `InfluenceRemovalIndex == 0`, while B currently holds 1 existing influence, the resulting session shall have exactly 1 influence in B's list (the newly applied one, replacing the removed existing).
- [DZ-173] When Card `"02"` is played by A on B with `Choice == 1`, the resulting session shall reflect `SDP[B] += 6`.
- [DZ-174] When Card `"02"` is played by A on B with `Choice == 1`, A's influence list shall gain 1 new `GreenInvasionInfluence` entry.

## Unwanted Behaviors

- [DZ-175] When Card `"02"` is requested with `Choice == 2`(範囲外、Branches.Count == 2 のため)、`IsLegalMove` shall return `false`.

## Tick 評価のシナリオ

- [DZ-176] When B holds `GreenInvasionInfluence(count=3)` and A's EndTurn rotates the current player to B, the resulting session shall reflect `SDP[B] -= 5` and B's influence shall have `RemainingCount == 2`.
- [DZ-177] When B holds `GreenInvasionInfluence(count=1)` and A's EndTurn rotates current to B, the influence shall be removed (B's list becomes empty after the tick).
- [DZ-178] When both A and B hold influences and A's EndTurn rotates current to B, only B's influence shall be ticked (A's `RemainingCount` is unchanged at 3).

## 定数依存

該当なし。数値 `-6` / `+6` / `-5`(SDP delta)/ `3`(カウント)はカードデータ固有値(JIT 共有)で、定数集約しない(L3 個別カード設計値)。

## 関連

- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」
- 前提モデル: [`../influences/influence-model.md`](../influences/influence-model.md)
- 前提効果: [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md) / [`../effects/remove-influence.md`](../effects/remove-influence.md) / [`../effects/choice-effect.md`](../effects/choice-effect.md)
- 実装(本 PR、統合テスト):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/GreenInvasionCardTests.cs`
- シナリオ: `green-invasion.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-170 | `Given_p1current_When_Card02をChoice0でプレイ_Then_p1のSDPがマイナス6` | 統合 Medium |
| DZ-171 | `Given_p1current_When_Card02をChoice0でプレイ_Then_p2のInfluencesに1件追加` | 同上 |
| DZ-172 | `Given_p2に既存影響1件_When_Choice0_index0で除去後新規付与_Then_p2のInfluences件数が1` + `Then_残った影響は新規の方` | 2 件に分割 |
| DZ-173 | `Given_p1current_When_Card02をChoice1でプレイ_Then_p2のSDPが6` | 統合 Medium |
| DZ-174 | `Given_p1current_When_Card02をChoice1でプレイ_Then_p1のInfluencesに1件追加` | 同上 |
| DZ-175 | `Given_Card02をChoice2_範囲外_When_IsLegalMove_Then_false` | 範囲外防御 |
| DZ-176 | `Given_p2に影響カウント3_p1ターン終了_When_EndTurn_Then_p2のSDPがマイナス5` + `Then_p2のInfluenceカウントが2` | 2 件に分割 |
| DZ-177 | `Given_p2に影響カウント1_p1ターン終了_When_EndTurn_Then_p2のInfluences件数が0` | 除去境界 |
| DZ-178 | `Given_p1とp2に各影響カウント3_p1ターン終了_When_EndTurn_Then_p1のInfluenceカウントは不変3` | 他プレイヤー保護 |
