# カード No.14「最後の砦Ⅱ」 (Phase 2 完結後)

「最後の砦Ⅰ→Ⅱ→Ⅲ」エスカレーション 3 連鎖の **中間カード**。No.13「最後の砦Ⅰ」の Choice2 で自動連想されて Hand に追加される(通常山札からドローも可能、初期山札 2 枚)。Choice2 で「最後の砦Ⅲ」(No.15)を更に連想する 2 段目連鎖。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.14 |
| 名前 | 最後の砦Ⅱ |
| CardTypeId | `"14"` |
| 初期山札枚数 | 2(オーナー JIT 確定 2026-05-17、ただし No.13 連想で追加供給あり)|
| 効果構造 | `ChoiceEffect` 1 件最上位(2 分岐:Choice1 / Choice2)|

## 効果

### Choice1: 強化版攻撃(+8 / -10)

- 自分の SDP が 8 増える(No.13 Choice1 の +6 から増加 = コスト上昇)
- 相手の SDP が 10 減る(No.13 Choice1 と同じ)
- 連想なし

### Choice2: 連鎖加速(-4 / -20 + 連想)

- 自分の SDP が 4 減る(No.13 Choice2 と同じ自己コスト維持)
- 相手の SDP が **20** 減る(No.13 Choice2 の -10 から倍化 = 攻撃力強化)
- **甲(自分)が「最後の砦Ⅲ」(No.15)を自動連想** で Hand に追加

戦略的解釈:
- Choice1 はコスト増 +2 で攻撃量は据え置き → 「Choice1 で打ち止め」は損な選択になりがち
- Choice2 は攻撃量倍化(-10 → -20)+ 次手段(No.15)獲得 = エスカレーション継続インセンティブ
- 「最後の砦Ⅲ」を獲得することで No.15 の Choice2(-30)に繋げる連鎖戦術

## カードデータ表現

```csharp
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("14"), new CardData("最後の砦Ⅱ", new Dictionary<string, int>()))

new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("14"), new IEffect[]
{
    new ChoiceEffect(new IReadOnlyList<IEffect>[]
    {
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, 8),
            new AdjustSdpEffect(SdpTarget.Opponent, -10),
        },
        new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),
            new AdjustSdpEffect(SdpTarget.Opponent, -20),
            new AssociateSpecificCardEffect(CardTypeId.Of("15")),
        },
    }),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-351] [Ubiquitous] Card `"14"` shall be registered with name `"最後の砦Ⅱ"` and a `ChoiceEffect` containing 2 branches as specified.

## 事象駆動要件 (Event-driven)

- [DZ-352] When Card `"14"` is played with `Choice == 0`, the resulting session shall reflect `SDP[A] += 8` and `SDP[B] -= 10`。
- [DZ-353] When Card `"14"` is played with `Choice == 1`, the resulting session shall reflect `SDP[A] -= 4` and `SDP[B] -= 20`。
- [DZ-354] When Card `"14"` is played with `Choice == 1`, A's Hand shall gain `CardId.Of("15", 0)` and `AssociatedCardIds` shall contain it。

## 関連

- 関連カード:[`./last-bastion-1.md`](./last-bastion-1.md)(No.13、本カードを連想する起点)/ [`./last-bastion-3.md`](./last-bastion-3.md)(No.15、本カードが連想する終端)
- 新規 effect 設計詳細は [`./last-bastion-1.md`](./last-bastion-1.md) §「自動連想 effect の設計」を参照
- 実装:`Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset` rid 1110〜1115

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-351 | (テスト免除: Ubiquitous) | catalog 登録は LastBastion2CardTests + CatalogTests で保証 |
| DZ-352 | `Given_任意フェーズ_When_Card14をChoice0でプレイ_Then_自分SDPプラス8` + `Then_相手SDPマイナス10` | 2 件に分割 |
| DZ-353 | `Given_任意フェーズ_When_Card14をChoice1でプレイ_Then_自分SDPマイナス4` + `Then_相手SDPマイナス20` | 2 件に分割 |
| DZ-354 | `Given_任意フェーズ_When_Card14をChoice1でプレイ_Then_HandにNo15追加_AssociatedCardIdsにも記録` | 自動連想 + ADR-0019 記録 |
