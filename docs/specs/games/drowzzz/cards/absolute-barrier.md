# カード No.19「絶対障壁」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 18 新規カード追加**(2026-05-18 オーナー JIT 確定)。**「ゲーム開始時自動連想 marker」機構の初導入カード**(ADR-0024)。No.17「見掛け倒しの障壁」の上位版で、**Counter + Frenzy 両キーワード持ち**(反撃カードでありながら反撃を受けない)、かつ **共通山札に含まれず先行プレイヤーが開始時に自動連想で 1 枚だけ持つ**特殊カード。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.19 |
| 名前 | 絶対障壁 |
| CardTypeId | `"19"` |
| 初期山札枚数 | **共通山札 0 枚**(`Bootstrap.BuildInitialDeck` で本 marker 持ちカードは除外、ADR-0024 §3)|
| 入手経路 | **先行プレイヤーのゲーム開始時自動連想のみ**(1 ゲーム 1 枚、後手は引けない)|
| 効果構造 | 2 件最上位:`KeywordedEffect([Counter, Frenzy], AdjustSdpEffect(Self, 0))` + `AssociateToFirstPlayerOnGameStartEffect()` |
| 新規導入概念 | `AssociateToFirstPlayerOnGameStartEffect` 新規 marker(ADR-0024)+ `StartGameUseCase` への `ICardCatalog<IEffect>` 再依存(ADR-0014 部分 Supersede)|

## 効果

- **「反撃」(Counter キーワード)**:M3-PR5b/c の Counter 機構(ADR-0011 §4.3)経由で、相手プレイヤーの `PlayCardAction` を `WaitingForCounterResponse` フェーズで無効化可能(本カードを Counter として使用)
- **「狂乱」(Frenzy キーワード)**:本カード自身に対して相手プレイヤーが反撃しようとしても illegal(ADR-0011 §4.5、`HasKeywordInEffects(effects, Frenzy)` で検出)
- **「ゲーム開始時、先行プレイヤーはこの手段を連想する」**:`StartGameUseCase.Execute` の catalog 全 entry 走査で本 marker を検出し、先行プレイヤー(`shuffledPlayers[0]`)の Hand に `CardId.Of("19", 0)` で追加 + `AssociatedCardIds`(ADR-0019)に記録

戦略的解釈:
- 先行プレイヤーの開幕アドバンテージ(無敵反撃カード 1 枚)
- No.17「見掛け倒しの障壁」の上位版:Counter で反撃可能 + Frenzy で自身は反撃されない(=絶対に通る反撃)
- 共通山札 0 枚で後手プレイヤーには永遠に入らない(先行優位の絶対化)
- AssociatedCardIds に記録されるため、No.04「静寂を纏う」系の特定カード使用禁止 TargetCardId にも選べない(連想由来除外、ADR-0019)
- 戦術的に「いつ使うか」が問われる希少カード(反撃を 1 度だけ確実に通せる手段)

## ADR-0024 の決定事項

ADR-0024 で確定した機構:

1. 新規 marker `AssociateToFirstPlayerOnGameStartEffect`(フィールドなし record、`EffectInterpreter` で no-op、評価は `StartGameUseCase` 経路のみ)
2. `StartGameUseCase` ctor に `ICardCatalog<IEffect>` を **再追加**(ADR-0014 部分 Supersede、ADR-0014 Status は維持)
3. `StartGameUseCase.Execute` 内で catalog 全 entry の最上位 effects 列を scan(`HasFirstPlayerAssociationEffectInTopLevel`)し、該当カードを先行プレイヤー Hand.Add + AssociatedCardIds 記録
4. `Bootstrap.BuildInitialDeck` で本 marker 持ちカードを共通山札から除外(先行プレイヤー限定の入手経路を仕組みで担保)
5. 永続化:`EffectJsonConverter` に `"AssociateToFirstPlayerOnGameStart"` discriminator(フィールドなし)、`AssociateToFirstPlayerOnGameStartEffectAsset` 新規 SO(`KeywordedEffectAsset.Inner` には入らず最上位独立 effect として配置)

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("19"), new CardData("絶対障壁", new Dictionary<string, int>()))

// effects 側(2 件最上位、最上位の順序は任意だが慣例として KeywordedEffect → AssociateToFirstPlayer)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("19"), new IEffect[]
{
    new KeywordedEffect(new[] { Keyword.Counter, Keyword.Frenzy }, new AdjustSdpEffect(SdpTarget.Self, 0)),
    new AssociateToFirstPlayerOnGameStartEffect(),
})
```

### 「反撃・狂乱」両キーワードを 1 件にまとめる根拠

`KeywordedEffect.Keywords` は `IReadOnlyList<Keyword>` で複数キーワード持ちを表現可能(ADR-0011 §4)。同一カードに「Counter で反撃カード扱い」+「Frenzy で被反撃不可」を両立させるため、1 件の `KeywordedEffect([Counter, Frenzy], ...)` で表現する(別 2 件に分けると Inner ダミーが 2 倍になる、ADR-0024 §「カード仕様確定」)。

### `AdjustSdpEffect(Self, 0)` Inner ダミーの根拠

`KeywordedEffect.Inner` は null 不可(M3-PR5a で確立)。Counter / Frenzy キーワード付与のみが目的の場合、Inner は副作用なし効果で OK。No.17「見掛け倒しの障壁」と同じパターン(`AdjustSdpEffect(Self, 0)` は SDP 変動なし)。

## 普遍要件 (Ubiquitous)

- [DZ-389] [Ubiquitous] Card `"19"` shall be registered with name `"絶対障壁"` and 2 top-level effects: `KeywordedEffect([Keyword.Counter, Keyword.Frenzy], AdjustSdpEffect(Self, 0))` + `AssociateToFirstPlayerOnGameStartEffect()`. `HasKeywordInEffects(effects, Counter)` shall return `true` and `HasKeywordInEffects(effects, Frenzy)` shall return `true`.

## ゲーム開始時自動連想 (StartGameUseCase 経路)

- [DZ-390] When `StartGameUseCase.Execute` is invoked with a catalog containing Card `"19"`(`AssociateToFirstPlayerOnGameStartEffect` を最上位 effect として持つ), the resulting session shall reflect Card `"19"` (CardId.Of("19", 0)) in the **first player's Hand**(`shuffledPlayers[0]`、Fisher-Yates shuffle 後の先行プレイヤー).
- [DZ-391] When `StartGameUseCase.Execute` is invoked with a catalog containing Card `"19"`, the resulting session's `AssociatedCardIds` shall contain `CardId.Of("19", 0)`(ADR-0019 連想由来記録と整合、No.04「静寂を纏う」系の TargetCardId 対象外になる).
- [DZ-392] When `StartGameUseCase.Execute` is invoked with a catalog **not** containing any card with `AssociateToFirstPlayerOnGameStartEffect`, the resulting session's first player Hand shall **not** contain Card `"19"` and `AssociatedCardIds` shall **not** contain `CardId.Of("19", 0)`(本 marker 持ちカードが catalog に無い場合は自動連想されない).
- [DZ-393] When Card `"19"` is in catalog, the second player(後手)'s Hand shall **not** contain Card `"19"`(先行プレイヤー限定の入手経路).

## Counter キーワード経路(反撃カードとしての利用)

- [DZ-394] When p2 is in `WaitingForCounterResponse` (relative to p1's PlayCardAction of a non-Frenzy card X), p2 plays `CounterAction(Counter=Card "19", Target=X)`, `IsLegalMove` shall return `true`(本カードを反撃カードとして使用可能、ADR-0011 §4.3).
- [DZ-395] When the above `CounterAction` is applied, Card `"19"` shall move from p2's Hand to Discard and X shall move from Field to Discard(無効化セマンティクス C、ADR-0011 §4.3.3、No.17 と同経路).

## Frenzy キーワード経路(本カードへの反撃不可)

- [DZ-396] When Card `"19"` is in Field after p1's PlayCardAction and p2 attempts `CounterAction(Counter=AnyOtherCounterCard, Target=Card "19")` during `WaitingForCounterResponse`, `IsLegalMove` shall return `false`(Frenzy 持ちは反撃不可、ADR-0011 §4.5).

## カードデータ整合性

- [INF-164] When `ScriptableObjectCardCatalog` is constructed with the SO representation of Card `"19"`「絶対障壁」, name / effects shall equal `InMemoryCardCatalog`.
  - SO 経路:`KeywordedEffectAsset([Counter, Frenzy], AdjustSdpEffectAsset(Self, 0))` + `AssociateToFirstPlayerOnGameStartEffectAsset()` の 2 件最上位
  - record 値同値:`KeywordedEffect.Keywords[Counter, Frenzy] + Inner = AdjustSdpEffect(Self, 0)` + `AssociateToFirstPlayerOnGameStartEffect()`

## 関連

- ADR:
  - [`docs/adr/0024-associate-to-first-player-on-game-start.md`](../../../../adr/0024-associate-to-first-player-on-game-start.md)(本カード起点の新規 ADR、開始時自動連想 marker + StartGameUseCase catalog 再依存)
  - [`docs/adr/0014-start-game-usecase-cardcatalog-removal.md`](../../../../adr/0014-start-game-usecase-cardcatalog-removal.md)(本 ADR で部分的に覆す、Status は維持)
  - [`docs/adr/0019-associated-card-ids-session-field.md`](../../../../adr/0019-associated-card-ids-session-field.md)(連想由来カード記録経路、本カードは初期連想で同経路を流用)
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4 / §4.5(キーワード能力、本カードは Counter + Frenzy 両持ち)
- 前提効果: [`../effects/keyworded-effect.md`](../effects/keyworded-effect.md)
- 既存類似カード(Counter キーワード持ち、本カードと機構共有):[`./facade-barrier.md`](./facade-barrier.md)(No.17、Frenzy なし通常版)
- 既存類似カード(Frenzy 持ち、本カードと機構共有):[`./untouchable-realm.md`](./untouchable-realm.md)(No.06)/ [`./realm-beyond-wisdom.md`](./realm-beyond-wisdom.md)(No.07)/ [`./clockwork-winter-general.md`](./clockwork-winter-general.md)(No.11)/ [`./00-dream.md`](./00-dream.md)(No.00)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AssociateToFirstPlayerOnGameStartEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(no-op case 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`(ctor 3 引数 + catalog scan)
  - `Assets/_Project/Scripts/Application/ICardCatalog.cs`(`RegisteredCardTypeIds` プロパティ追加)
  - `Assets/_Project/Scripts/Application/Catalog/InMemoryCardCatalog.cs`(`RegisteredCardTypeIds` 実装)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/AssociateToFirstPlayerOnGameStartEffectAsset.cs`(新規 SO)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(`AssociateToFirstPlayerOnGameStart` discriminator 追加)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(BuildInitialDeck 除外フィルタ + コメント 19→20 種更新)
  - `Assets/_Project/Scripts/Bootstrap/GameLifetimeScope.cs`(StartGameUseCase ctor 3 引数 doc 更新)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.19 entry + rid 5500/5501/5502)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/AbsoluteBarrierCardTests.cs`(新規、DZ-394〜396、Counter / Frenzy 経路。DZ-390〜393 は `StartGameUseCaseTests.cs` 側)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/StartGameUseCaseTests.cs`(既存 ctor 3 引数更新 + DZ-390〜393 開始時連想検証追加)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/AbsoluteBarrierCardCatalogTests.cs`(新規、INF-164)
- シナリオ: `absolute-barrier.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-389 | (テスト免除: Ubiquitous) | catalog 登録 + Counter / Frenzy キーワード性質は AbsoluteBarrierCardTests + CatalogTests で構造保証 |
| DZ-390 | `Given_Card19登録catalog_When_StartGameUseCaseExecute_Then_先行プレイヤーHandに含まれる` | 開始時連想 |
| DZ-391 | `Given_Card19登録catalog_When_StartGameUseCaseExecute_Then_AssociatedCardIdsに含まれる` | ADR-0019 連想由来記録 |
| DZ-392 | `Given_Card19なしcatalog_When_StartGameUseCaseExecute_Then_先行プレイヤーHand_AssociatedCardIds共にCard19なし` | marker なしカードは自動連想されない |
| DZ-393 | `Given_Card19登録catalog_When_StartGameUseCaseExecute_Then_後手プレイヤーHandに含まれない` | 先行限定 |
| DZ-394 | `Given_WaitingForCounter_NonFrenzyTarget_When_Card19でCounterAction_Then_IsLegalMoveがtrue` | Counter 経路、No.17 と共通 |
| DZ-395 | `Given_WaitingForCounter_When_Card19でCounterAction_Then_Card19とTargetがDiscardへ` | Apply 経路 |
| DZ-396 | `Given_Card19がField_When_OtherCounterCardで反撃_Then_IsLegalMoveがfalse` | Frenzy 反撃不可 |
| INF-164 | `AbsoluteBarrierCardCatalogTests.Given_*` 2 件 | SO 同等性検証(KeywordedEffect 2 keyword + AssociateToFirstPlayer marker)|
