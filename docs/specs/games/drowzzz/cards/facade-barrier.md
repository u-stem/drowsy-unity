# カード No.17「見掛け倒しの障壁」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 16 新規カード追加**(2026-05-17 オーナー JIT 確定)。**Counter キーワード機構を使う初の本物の反撃カード**(M3-PR5b/c で導入された Counter キーワード機構は、これまでテスト用ダミー `c_counter` のみで実カードがなかった、ADR-0011 §4.3)。SDP 変動なし、Counter キーワード付与のみのシンプルな最小カード。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.17 |
| 名前 | 見掛け倒しの障壁 |
| CardTypeId | `"17"` |
| 初期山札枚数 | 3(オーナー JIT 確定 2026-05-17、反撃を頻繁に使うための多枚枠、No.08 と同じ)|
| 効果構造 | `KeywordedEffect([Keyword.Counter], dummy AdjustSdpEffect(Self, 0))` の 1 件最上位 |
| 新規導入概念 | なし(既存 Counter キーワード機構をそのまま使う初の本物カード)|

## 効果

- **Counter キーワードのみ付与**(`HasKeywordInEffects` で OR 検出 → 本カードが Counter キーワード持ちと認識される)
- SDP 変動なし(`AdjustSdpEffect(Self, 0)` ダミー、`Choice` なし)
- 通常 `PlayCardAction` で使うと SDP 0 で Hand → Field → Discard 移動するのみ(意味なし)
- 本来の用法は **`CounterAction` 経路**(M3-PR5b 経路 1 / M3-PR5c 経路 2、ADR-0011 §4.3 / §4.4)

戦略的解釈:
- 「見掛け倒し」フレーバー:単独では何の効果もない(SDP 0)が、反撃として使うと相手カードを無効化する「守りの幻影」
- 初期 3 枚と多枚で、ゲーム中複数回の反撃機会を確保
- 相手の高威力カード(No.09 強引過ぎる一手 -10/+10、No.11 機械仕掛けの冬将軍 -4/-8 + 永続 Influence 等)を打ち消す戦術カード

## Counter キーワードの既存機構(本カード固有の挙動なし)

ADR-0011 §4.3 / §4.4 で M3-PR5b/c に確立された既存機構をそのまま使う:

### 経路 1:反撃(`WaitingForCounterResponse` フェーズ)
- 相手プレイヤーが `PlayCardAction` を打って Counter キーワード持ちカード(本カード等)を保有していると、`PhaseState` が `WaitingForCounterResponse` に遷移(`HasCounterCardInHand` 検出)
- 反撃側プレイヤーが `CounterAction(Counter=本カード, Target=Field.Cards[0])` を打つ
- Counter カード(本カード)を Hand → Discard、Target カード(相手の Field 先頭)を Field → Discard へ移動
- `PendingCounteredEffects` に `(CounterCard=本カード, OriginalCard=相手カード, OriginalEffects=相手効果列)` を末尾追加
- `PhaseState` を `WaitingForEndTurn` に戻す(元プレイヤーのターン進行を継続)

### 経路 2:反撃の反撃(自フェーズ中、Pending 非空)
- 自ターンの `WaitingForEndTurn` フェーズで、`PendingCounteredEffects` 最後エントリ(=自分が打ち消されたカードの記録)に対して反撃の反撃が可能
- `CounterAction(Counter=本カード, Target=相手の Counter カード)` で相手の Counter を打ち消し、Pending の OriginalEffects を遡及発動(自分のオリジナルカード効果が再発動)

### Frenzy への対抗不可
- Target カードに `Keyword.Frenzy` が含まれる場合は反撃不可(`IsLegalCounter` で illegal、ADR-0011 §4.5)
- 例:No.06「牙の届かぬ領域」/ No.07「知恵の及ばぬ領域」/ No.11「機械仕掛けの冬将軍」は Frenzy 持ちで反撃を受けない

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("17"), new CardData("見掛け倒しの障壁", new Dictionary<string, int>()))

// effects 側(KeywordedEffect 1 件、Counter キーワード + ダミー effect)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("17"), new IEffect[]
{
    new KeywordedEffect(new[] { Keyword.Counter },
        new AdjustSdpEffect(SdpTarget.Self, 0)),  // ダミー(Counter キーワード付与のための wrapper)
})
```

### `AdjustSdpEffect(SdpTarget.Self, 0)` ダミーの根拠

`KeywordedEffect` は `Inner` フィールドに 1 件の `IEffect` を必須(`KeywordedEffectAsset` の `Inner` も null 不可)。Counter キーワード付与のみが目的の場合、Inner は **副作用なしの no-op 効果** で OK。

`AdjustSdpEffect(SdpTarget.Self, 0)` は EffectInterpreter で:
- Delta == 0 → SDP 変動なし(計算上は `kv.Value + 0 = kv.Value`)
- session 値同等(SecondDrowsyPoints Dictionary 再生成のみで等価値)

これは過去テスト(`DreamCardTests.DZ-237`、`ForcePlayCardTests.DZ-309`、`UntouchableRealmCardTests.DZ-282` 等)で使われている標準パターン(`c_counter` ダミーカード)を本番カードに昇格した形。

## 普遍要件 (Ubiquitous)

- [DZ-369] [Ubiquitous] Card `"17"` shall be registered with name `"見掛け倒しの障壁"` and a single top-level `KeywordedEffect([Counter], AdjustSdpEffect(Self, 0))` effect. `HasKeywordInEffects(effects, Counter)` shall return `true`(カード全体に Counter キーワード性質)。

## 事象駆動要件 (Event-driven、通常 PlayCard)

- [DZ-370] When Card `"17"` is played by player A via normal `PlayCardAction`, the resulting session shall reflect `SDP[A] == 0` and `SDP[B] == 0`(SDP 変動なし、本カードは単独では効果なし)。
- [DZ-371] When Card `"17"` is played by player A via normal `PlayCardAction`, A's Hand shall lose Card `"17"` and Field shall gain Card `"17"` at the top position(PlayCardAction 直後の中間状態 = Field 先頭、ADR-0006 §M1-PR5。後続の EndTurn / 別アクションを経て Field → Discard へ進むが、本 DZ-371 は PlayCard 直後のみを検証、code-reviewer P-3 反映 2026-05-18)。

## 合法性判定(`CounterAction` 経路 1:反撃、`WaitingForCounterResponse`)

- [DZ-372] When Field has any non-Frenzy card and `PhaseState == WaitingForCounterResponse`, B(reverse player)が Card `"17"` を Counter として `CounterAction(Counter=Card17, Target=Field[0])` を打ったとき `IsLegalMove` shall return `true`。
- [DZ-373] When Field has Card `"06"`「牙の届かぬ領域」(Frenzy 持ち)and B が Card `"17"` で反撃しようとすると, `IsLegalMove` shall return `false`(Frenzy 持ちは反撃不可、ADR-0011 §4.5)。

## Apply 経路(`ApplyCounter`、既存機構の動作確認)

- [DZ-374] When `CounterAction(Counter=Card17, Target=Field[0])` is applied during `WaitingForCounterResponse`, the resulting session shall move Card `"17"` from B's Hand to Discard and the Target card from Field to Discard(無効化セマンティクス C、ADR-0011 §4.3.3)。
- [DZ-375] When the same `CounterAction` is applied, the resulting session's `PendingCounteredEffects` shall gain 1 entry with `(CounterCard=Card17, OriginalCard=Target, OriginalEffects=Target's effect list)`(M3-PR5c、反撃の反撃のための記録)。
- [DZ-376] When `CounterAction` is applied, the resulting session's `PhaseState` shall be `WaitingForEndTurn`(元プレイヤーのターン進行に戻る)。

## 関連

- ADR:
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4.3「反撃(Counter)」+ §4.4「反撃の反撃」+ §4.5「Frenzy = 反撃を受けない」(既存機構、本カードは consumer)
- 前提効果: [`../effects/keyworded-effect.md`](../effects/keyworded-effect.md) / [`../effects/adjust-sdp.md`](../effects/adjust-sdp.md)
- 既存類似カード(Counter キーワード持ち):過去はテスト用ダミー `c_counter` のみ(`DreamCardTests.DZ-237` / `ForcePlayCardTests.DZ-309` / `UntouchableRealmCardTests.DZ-282`)、本カードが **初の本物の Counter カード**
- 既存類似カード(Frenzy 持ち、本カードで反撃不可):[`./untouchable-realm.md`](./untouchable-realm.md)(No.06)/ [`./realm-beyond-wisdom.md`](./realm-beyond-wisdom.md)(No.07)/ [`./clockwork-winter-general.md`](./clockwork-winter-general.md)(No.11)/ [`./00-dream.md`](./00-dream.md)(No.00)
- 実装(本 PR):
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.17 entry + rid 1300〜1301、新規 effect 不要)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「17 → 18 種」)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/FacadeBarrierCardTests.cs`(新規、DZ-370〜376)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/FacadeBarrierCardCatalogTests.cs`(新規、SO 同等性、INF-162)
- シナリオ: `facade-barrier.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-369 | (テスト免除: Ubiquitous) | catalog 登録 + Counter キーワード性質は FacadeBarrierCardTests + CatalogTests で構造保証 |
| DZ-370 | `Given_任意フェーズ_When_Card17をPlayCardAction_Then_SDP変動なし` | 通常 PlayCardAction(両者 SDP 0)|
| DZ-371 | `Given_任意フェーズ_When_Card17をPlayCardAction_Then_HandからRemove_FieldにAdd` | 通常 PlayCard の Hand/Field 操作 |
| DZ-372 | `Given_WaitingForCounter_NonFrenzyTarget_When_Card17でCounterAction_Then_IsLegalMoveがtrue` | Counter 合法性 |
| DZ-373 | `Given_WaitingForCounter_FrenzyTarget_When_Card17でCounterAction_Then_IsLegalMoveがfalse` | Frenzy 持ちは反撃不可 |
| DZ-374 | `Given_WaitingForCounter_When_Card17でCounterAction_Then_Card17とTargetがDiscardへ` | Apply 経路:Hand/Field/Discard 操作 |
| DZ-375 | `Given_WaitingForCounter_When_Card17でCounterAction_Then_PendingCounteredEffectsに1件追加` | Pending 記録(M3-PR5c)|
| DZ-376 | `Given_WaitingForCounter_When_Card17でCounterAction_Then_PhaseStateがWaitingForEndTurn` | フェーズ遷移 |
