# カード No.13「最後の砦Ⅰ」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 11 新規カード追加**(2026-05-17 オーナー JIT 確定)。**自動連想の初導入**(`AssociateSpecificCardEffect`)で、Choice2 でプレイすると「最後の砦Ⅱ」(No.14)を自動的に Hand に追加する。No.13 → No.14 → No.15 の **エスカレーション 3 連鎖** の起点カード。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.13 |
| 名前 | 最後の砦Ⅰ |
| CardTypeId | `"13"` |
| 初期山札枚数 | 2(オーナー JIT 確定 2026-05-17、No.13/14/15 各 2 枚)|
| 効果構造 | `ChoiceEffect` 1 件最上位(2 分岐:Choice1 / Choice2)|
| 新規導入概念 | `AssociateSpecificCardEffect(CardTypeId)`(カード効果による自動連想、Hand 重複時 Instance 自動採番)|

## 効果

### Choice1: 攻撃寄り(+6 / -10)

- 自分の SDP が 6 増える(自分が目覚める = 不利方向だが)
- 相手の SDP が 10 減る(相手を眠らせる = 攻撃)
- **連想なし**(Choice1 では No.14 を追加しない)

待って、`+6 / -10` は自分 +6 / 相手 -10。Drowzzz では SDP マイナスが「眠くなる」方向で勝利条件と整合(持ち点低い方が勝ち)、SDP プラスが「目覚める」方向で不利。

- Choice1 は **自己コスト +6(目覚める)+ 相手 -10(眠らせる)** で、差分 16 ポイントの攻撃寄り選択

### Choice2: 連鎖型(-4 / -10 + 連想)

- 自分の SDP が 4 減る(自分が眠くなる = 有利方向)
- 相手の SDP が 10 減る(相手を眠らせる = 攻撃)
- **甲(自分)が「最後の砦Ⅱ」(No.14)を自動連想** で Hand に追加

戦略的解釈:
- Choice1 は **強力な単発攻撃**(差分 16 ポイント差)、Choice2 は **少しマイルドな攻撃 + 次手段(No.14)獲得**
- 「最後の砦」連鎖でエスカレーション:Ⅰ → Ⅱ → Ⅲ と進むほど効果が大きくなる(自分のコストは Choice2 で固定 -4、相手の SDP 減量は -10 → -20 → -30 と倍々)
- Choice2 の連想は **AssociationThreshold(>=80)を要しない** 自動連想(オーナー JIT 2026-05-17:「カード効果として自動連想、他條件不要」)
- 連想された No.14 は `AssociatedCardIds` に永続記録され、No.04「静寂を纏う」の「連想由来除外」対象になる

## 自動連想 effect の設計(`AssociateSpecificCardEffect`)

新規 effect `AssociateSpecificCardEffect(CardTypeId TargetCardTypeId)` を本 PR で初導入。既存 `AssociateAction`(M3-PR4、ADR-0011 §1)とは別経路:

| 観点 | `AssociateAction` | `AssociateSpecificCardEffect` |
| ---- | ---- | ---- |
| 起動経路 | プレイヤーの能動アクション | カード効果(`PlayCardAction` 中の effect 評価) |
| 合法性条件 | `TotalPoints >= 80` + 対象カードに `AssociatableMarkerEffect` | **なし(自動連想、他條件不要)** |
| Hand 重複時の挙動 | `Hand.Contains(action.Card)` で illegal-move | **Instance 番号を自動採番**(`CardId.Of(typeId, n)` で unique 化、HAND-005 重複検出を構造的に回避)|
| AssociatedCardIds 記録 | あり(ADR-0019) | **あり**(同一仕様で記録、No.04 連想由来除外対象に)|
| PhaseState 影響 | 不変(割り込み式) | 不変(カード効果内、`PlayCardAction` 全体の PhaseState 遷移は別) |

### Hand 重複時の Instance 採番

「最後の砦Ⅰ」を 2 回プレイすると、2 回とも Choice2 を選んだ場合に Hand に「最後の砦Ⅱ」を 2 枚追加する必要がある。`AssociateSpecificCardEffect` は:

1. `CardId.Of("14", 0)` を試行 → Hand にあれば次へ
2. `CardId.Of("14", 1)` を試行 → なければ採用
3. ...

を `newInstance` で順次採番。Instance 番号が Hand 内 unique であれば HAND-005 重複検出に違反しない(ADR-0018 で確立した「CardId = `(CardTypeId, Instance)` 複合型」の正しい運用)。

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("13"), new CardData("最後の砦Ⅰ", new Dictionary<string, int>()))

// effects 側(ChoiceEffect 1 件最上位、2 分岐)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("13"), new IEffect[]
{
    new ChoiceEffect(new IReadOnlyList<IEffect>[]
    {
        // Choice1: +6 / -10、連想なし
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, 6),
            new AdjustSdpEffect(SdpTarget.Opponent, -10),
        },
        // Choice2: -4 / -10 + 「最後の砦Ⅱ」連想
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),
            new AdjustSdpEffect(SdpTarget.Opponent, -10),
            new AssociateSpecificCardEffect(CardTypeId.Of("14")),
        },
    }),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-345] [Ubiquitous] Card `"13"` shall be registered in the initial `InMemoryCardCatalog` with name `"最後の砦Ⅰ"` and a single top-level `ChoiceEffect` containing 2 branches as specified.

## 事象駆動要件 (Event-driven)

- [DZ-346] When Card `"13"` is played with `Choice == 0`, the resulting session shall reflect `SDP[A] += 6` and `SDP[B] -= 10`(連想なし)。
- [DZ-347] When Card `"13"` is played with `Choice == 1`, the resulting session shall reflect `SDP[A] -= 4` and `SDP[B] -= 10`。
- [DZ-348] When Card `"13"` is played with `Choice == 1`, A's Hand shall gain `CardId.Of(CardTypeId.Of("14"), 0)`(連想 No.14)。
- [DZ-349] When Card `"13"` is played with `Choice == 1`, the resulting session's `AssociatedCardIds` shall contain the newly associated `CardId.Of(CardTypeId.Of("14"), 0)`(ADR-0019 連想由来永続記録)。
- [DZ-350] When Card `"13"` is played **twice** with `Choice == 1` each, A's Hand shall gain `CardId.Of("14", 0)` then `CardId.Of("14", 1)`(Instance 自動採番、HAND-005 重複検出回避)。

## 関連

- 新規 effect:`AssociateSpecificCardEffect`(本 PR で導入、spec md 未作成、xmldoc に Design 記述あり)
- 前提効果:[`../effects/adjust-sdp.md`](../effects/adjust-sdp.md)
- ADR:
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §1「連想」(本 effect は AssociateAction とは別経路だが、Hand 追加 + AssociatedCardIds 記録の semantics は ADR-0019 と整合)
  - [`docs/adr/0019-associated-card-ids-session-field.md`](../../../../adr/0019-associated-card-ids-session-field.md)(`AssociatedCardIds` 記録の永続性、本 effect でも同じ仕様)
  - [`docs/adr/0018-cardtypeid-cardid-instance-separation.md`](../../../../adr/0018-cardtypeid-cardid-instance-separation.md)(CardId = `(CardTypeId, Instance)` 複合型、Instance 自動採番の根拠)
- 関連カード(同 PR で追加):
  - [`./last-bastion-2.md`](./last-bastion-2.md)(No.14、Choice2 で No.15 連想)
  - [`./last-bastion-3.md`](./last-bastion-3.md)(No.15、終端、Choice2 でも連想なし)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AssociateSpecificCardEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case + `ApplyAssociateSpecificCard` ヘルパー)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/AssociateSpecificCardEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.13/14/15 entry + rid 1100〜1124)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「13 → 16 種」)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/LastBastion1CardTests.cs`(新規)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/LastBastion2CardTests.cs`(新規)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/LastBastion3CardTests.cs`(新規)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/LastBastion{1,2,3}CardCatalogTests.cs`(SO 同等性、INF-156/157/158)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/EffectJsonConverterTests.cs`(INF-159、Round-Trip)
- シナリオ: `last-bastion-1.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-345 | (テスト免除: Ubiquitous) | catalog 登録は `LastBastion1CardTests` ヘルパー + `LastBastion1CardCatalogTests` で構造保証 |
| DZ-346 | `Given_任意フェーズ_When_Card13をChoice0でプレイ_Then_自分のSDPがプラス6` + `Then_相手のSDPがマイナス10` | 2 件に分割 |
| DZ-347 | `Given_任意フェーズ_When_Card13をChoice1でプレイ_Then_自分のSDPがマイナス4` + `Then_相手のSDPがマイナス10` | 2 件に分割 |
| DZ-348 | `Given_任意フェーズ_When_Card13をChoice1でプレイ_Then_HandにNo14が追加される` | 自動連想 |
| DZ-349 | `Given_任意フェーズ_When_Card13をChoice1でプレイ_Then_AssociatedCardIdsにNo14が記録される` | 連想由来永続記録 |
| DZ-350 | `Given_Card13を2回Choice1でプレイ_Then_HandにNo14がInstance0と1の2件追加される` | Instance 自動採番 |
