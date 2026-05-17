# カード No.07「知恵の及ばぬ領域」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 6 新規カード追加**(2026-05-17 オーナー JIT 確定)。No.08「廻るための知恵」と **対をなすカウンタカード**:相手が保有する「廻るための知恵」由来 influence を 1 件消滅 + 「廻るための知恵」使用禁止 influence(カウント 4)を相手に付与。No.06 と同じく Frenzy(狂乱)キーワード持ち。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.07 |
| 名前 | 知恵の及ばぬ領域 |
| CardTypeId | `"07"` |
| 初期山札枚数 | 1(オーナー JIT、推定レア枠)|
| 効果構造 | 最上位 4 件:`AdjustSdpEffect(Self, -6)` + `AdjustSdpEffect(Opponent, +5)` + `RemoveInvertBedDamageInfluenceEffect(Opponent)` + `KeywordedEffect([Frenzy], ApplyInfluenceEffect(Opponent, RestrictNo08Influence))` |
| キーワード | Frenzy(反撃不可、No.06 と同パターン) |
| 新規導入概念 | `RemoveInvertBedDamageInfluenceEffect`(特定 marker 型の Influence を 1 件削除)|

## 効果

時間帯非依存(夜・朝両方で同じ効果、Frenzy 包み):

- 自分の SDP が 6 減る
- 相手の SDP が 5 増える(相手を眠くさせる)
- 相手の Influences から **`InvertBedDamageSdpInfluenceMarkerEffect` を TickEffect に持つ Influence を 1 件削除**(該当なしなら graceful no-op)
- 相手にカウント 4 の **「廻るための知恵(No.08)使用禁止」** 影響を付与(`RestrictSpecificCardInfluenceEffect(CardTypeId.Of("08"))` を TickEffect として持つ Influence)

## カード固有「影響」

| 観点 | 値 |
| ---- | ---- |
| トリガー | `InfluenceTrigger.OwnPhaseStart` |
| Tick 効果 | `RestrictSpecificCardInfluenceEffect(CardTypeId.Of("08"))`(No.04 で導入済の既存型を流用、対象 CardTypeId 固定値で構築)|
| 残発動回数 | 4(4 フェーズ寿命)|
| Semantics | **存在時:No.08「廻るための知恵」を使用できない**(`IsLegalPlayCard` で `HasSpecificCardRestrictionInfluence` 経由 illegal 化、No.04 と同経路)|

### 設計判断:既存 `RestrictSpecificCardInfluenceEffect` 流用

No.04「静寂を纏う」は `ApplyTargetedRestrictionEffect` で **動的に** `PlayCardAction.TargetCardId.TypeId` を読んで影響を構築する。本 No.07 は **静的に** `CardTypeId.Of("08")` を固定値として埋め込む。既存 `RestrictSpecificCardInfluenceEffect(CardTypeId target)` 型は CardTypeId をフィールドとして持つ汎用設計のため、本カードで「カード設計時に固定値を埋め込む」用途にそのまま流用可能。

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("07"), new CardData("知恵の及ばぬ領域", new Dictionary<string, int>()))

// 影響定義(No.08 使用禁止、カウント 4)
var RestrictNo08Influence = new PlayerInfluence(
    InfluenceTrigger.OwnPhaseStart,
    new RestrictSpecificCardInfluenceEffect(CardTypeId.Of("08")),
    4);

// effects 側(最上位 4 件、4 件目を Frenzy 包みでカード全体に Frenzy 性質付与、No.06 と同パターン)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("07"), new IEffect[]
{
    new AdjustSdpEffect(SdpTarget.Self, -6),
    new AdjustSdpEffect(SdpTarget.Opponent, 5),
    new RemoveInvertBedDamageInfluenceEffect(SdpTarget.Opponent),
    new KeywordedEffect(new[] { Keyword.Frenzy },
        new ApplyInfluenceEffect(SdpTarget.Opponent, RestrictNo08Influence)),
})
```

## 普遍要件 (Ubiquitous)

- [DZ-286] [Ubiquitous] Card `"07"` shall be registered in the initial `InMemoryCardCatalog` (and `ScriptableObjectCardCatalog` for production) with name `"知恵の及ばぬ領域"` and the **4 top-level effects** specified above. The fourth effect shall be a `KeywordedEffect([Frenzy], ApplyInfluenceEffect(...))` so that `HasKeywordInEffects(effects, Frenzy)` returns `true`(カード全体に Frenzy 性質、No.06 と同パターン)。

## 事象駆動要件 (Event-driven)

- [DZ-287] When Card `"07"` is played by player A on player B, the resulting session shall reflect `SDP[A] -= 6`(時間帯非依存)。
- [DZ-288] When Card `"07"` is played by player A on player B, the resulting session shall reflect `SDP[B] += 5`(時間帯非依存)。
- [DZ-289] When Card `"07"` is played by player A on player B while B holds at least one `PlayerInfluence(_, InvertBedDamageSdpInfluenceMarkerEffect, _)`, the resulting session shall have B's influence list shortened by 1 entry(該当 marker 型の先頭 1 件が削除される)。
- [DZ-290] When Card `"07"` is played by player A on player B while B holds **no** `PlayerInfluence(_, InvertBedDamageSdpInfluenceMarkerEffect, _)`, the resulting session shall have B's influence list unchanged in length(削除対象なし、graceful no-op)。
- [DZ-291] When Card `"07"` is played by player A on player B, B's influence list shall gain one new `PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(CardTypeId.Of("08")), 4)` entry。

## 合法性判定(Counter 不可、No.06 と同パターン)

- [DZ-292] When Card `"07"` is targeted by `CounterAction` during `WaitingForCounterResponse`, `IsLegalMove` shall return `false`(Frenzy 持ちカードは反撃を受けない、ADR-0011 §4.5、No.06 DZ-282 と同パターン)。

## 関連

- ADR: [`docs/adr/0019-associated-card-ids-session-field.md`](../../../../adr/0019-associated-card-ids-session-field.md)(`RestrictSpecificCardInfluenceEffect` 流用)+ [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4.5「Frenzy」
- 対をなすカード: [`./circulating-wisdom.md`](./circulating-wisdom.md)(No.08、本カードが消滅対象 / 使用禁止対象とする)
- 既存類似カード: [`./untouchable-realm.md`](./untouchable-realm.md)(No.06、Frenzy + ベッド破損 Influence 同パターン)+ [`./sound-of-silence.md`](./sound-of-silence.md)(No.04、`RestrictSpecificCardInfluenceEffect` 動的版)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/RemoveInvertBedDamageInfluenceEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case + `ApplyRemoveInvertBedDamage`)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/RemoveInvertBedDamageInfluenceEffectAsset.cs`(新規)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(1 dispatch case)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/RealmBeyondWisdomCardTests.cs`
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/RealmBeyondWisdomCardCatalogTests.cs`
- シナリオ: `realm-beyond-wisdom.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-286 | (テスト免除: Ubiquitous) | catalog 登録 + Frenzy 性質は `RealmBeyondWisdomCardTests` のヘルパー + `RealmBeyondWisdomCardCatalogTests` で構造的に保証 |
| DZ-287 | `Given_任意フェーズ_When_Card07をプレイ_Then_自分のSDPがマイナス6` | 統合テスト |
| DZ-288 | `Given_任意フェーズ_When_Card07をプレイ_Then_相手のSDPがプラス5` | 統合テスト |
| DZ-289 | `Given_相手がInvertBedDamageInfluence保有_When_Card07をプレイ_Then_該当Influenceが1件削除される` | 統合テスト |
| DZ-290 | `Given_相手がInvertBedDamageInfluence非保有_When_Card07をプレイ_Then_相手のInfluences件数は不変` | graceful no-op |
| DZ-291 | `Given_任意フェーズ_When_Card07をプレイ_Then_相手のInfluencesにRestrictCard08が追加される` | 統合テスト |
| DZ-292 | `Given_Card07がField_WaitingForCounter_When_p2がCounterActionでCard07をtarget_Then_IsLegalMoveがfalse` | Frenzy = 反撃不可、DZ-282 と同パターン |
