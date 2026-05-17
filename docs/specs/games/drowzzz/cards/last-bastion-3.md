# カード No.15「最後の砦Ⅲ」 (Phase 2 完結後)

「最後の砦Ⅰ→Ⅱ→Ⅲ」エスカレーション 3 連鎖の **終端カード**。Choice2 を選んでも **連想なし**(更なる連鎖はなし)で、攻撃量は最大の -30。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.15 |
| 名前 | 最後の砦Ⅲ |
| CardTypeId | `"15"` |
| 初期山札枚数 | 2(オーナー JIT 確定 2026-05-17、ただし No.14 連想で追加供給あり)|
| 効果構造 | `ChoiceEffect` 1 件最上位(2 分岐、Choice2 も連想なしの SDP 変動のみ)|

## 効果

### Choice1: 最大コスト型攻撃(+10 / -10)

- 自分の SDP が 10 増える(エスカレーション最大コスト)
- 相手の SDP が 10 減る
- 連想なし

### Choice2: 最終攻撃(-4 / -30)

- 自分の SDP が 4 減る
- 相手の SDP が **30** 減る(連鎖最強攻撃)
- **連想なし**(終端、エスカレーションはここで打ち止め)

戦略的解釈:
- Choice2 で **No.13 Choice2 → No.14 Choice2 → No.15 Choice2** と連鎖した場合の累積:
  - 自分:`-4 × 3 = -12`(自己コスト)
  - 相手:`-10 + -20 + -30 = -60`(攻撃量、累積)
  - **差分 48 ポイント** の大攻撃 + ターン消費 3 回(連鎖中に他カード使用なしの前提)
- Choice1 は終端段階の保険(連鎖を打ち切って相対的に控えめな攻撃で済ませる)

## カードデータ表現

```csharp
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("15"), new CardData("最後の砦Ⅲ", new Dictionary<string, int>()))

new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("15"), new IEffect[]
{
    new ChoiceEffect(new IReadOnlyList<IEffect>[]
    {
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, 10),
            new AdjustSdpEffect(SdpTarget.Opponent, -10),
        },
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),
            new AdjustSdpEffect(SdpTarget.Opponent, -30),
            // 連想なし(終端カード)
        },
    }),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-355] [Ubiquitous] Card `"15"` shall be registered with name `"最後の砦Ⅲ"` and a `ChoiceEffect` containing 2 branches as specified. Choice2 branch shall **not** contain `AssociateSpecificCardEffect`(連想なし、終端)。

## 事象駆動要件 (Event-driven)

- [DZ-356] When Card `"15"` is played with `Choice == 0`, the resulting session shall reflect `SDP[A] += 10` and `SDP[B] -= 10`。
- [DZ-357] When Card `"15"` is played with `Choice == 1`, the resulting session shall reflect `SDP[A] -= 4` and `SDP[B] -= 30`。
- [DZ-358] When Card `"15"` is played with `Choice == 1`, A's Hand shall remain unchanged regarding No.13/14/15 cards(連想なし、終端確認)。

## 関連

- 関連カード:[`./last-bastion-2.md`](./last-bastion-2.md)(No.14、本カードを連想する起点)
- 新規 effect 設計詳細は [`./last-bastion-1.md`](./last-bastion-1.md) §「自動連想 effect の設計」を参照
- 実装:`Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset` rid 1120〜1124

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-355 | (テスト免除: Ubiquitous) | catalog 登録は LastBastion3CardTests + CatalogTests で保証、Choice2 連想なしも構造的に確認 |
| DZ-356 | `Given_任意フェーズ_When_Card15をChoice0でプレイ_Then_自分SDPプラス10` + `Then_相手SDPマイナス10` | 2 件に分割 |
| DZ-357 | `Given_任意フェーズ_When_Card15をChoice1でプレイ_Then_自分SDPマイナス4` + `Then_相手SDPマイナス30` | 2 件に分割 |
| DZ-358 | `Given_任意フェーズ_When_Card15をChoice1でプレイ_Then_HandにNo13_14_15は追加されない` | 終端確認(連想なし) |
