# カード No.18「対抗手段」 (Phase 2 完結後)

DrowZzz Phase 2 完結後の **第 17 新規カード追加**(2026-05-18 オーナー JIT 確定)。**Echo キーワード機構の初導入カード**(ADR-0023)。「受けている影響から 1 つを選び、その発生源カードを再使用する + 選択影響は除去」効果を持つ初のカード。「反撃」はカードテキスト上の特殊効果分類(フレーバー)で、機構上の Counter キーワード(M3-PR5b/c)とは独立。

## 概要

| 観点 | 値 |
| ---- | ---- |
| カード番号 | No.18 |
| 名前 | 対抗手段 |
| CardTypeId | `"18"` |
| 初期山札枚数 | 3(オーナー JIT 確定 2026-05-18、Echo を頻繁に使うための多枚枠、No.17「見掛け倒しの障壁」と同じ)|
| 効果構造 | `KeywordedEffect([Keyword.Echo], ReuseInfluenceSourceEffect())` の 1 件最上位 |
| 新規導入概念 | `Keyword.Echo` 追加 + `PlayerInfluence.OriginEffects` 追加 + `ReuseInfluenceSourceEffect` 追加(ADR-0023)|

## 効果

- **「反撃」(カードテキスト上のフレーバー分類)**:現プレイヤーが保有している `PlayerInfluence` のうち 1 つを `PlayCardAction.Choice` で選択
- 選択した `PlayerInfluence.OriginEffects`(その影響を生成したカードの効果列スナップショット)を **本プレイヤー `SdpTarget.Self` 起点で再 `EffectInterpreter`**
- 再使用後、**選択した影響は除去**(consume、`RemainingCount` 残量に関わらず 1 回限り)
- 現プレイヤーの `Influences` が空の場合は **illegal**(`IsLegalMove` で false、無対象なら使えない)
- `PlayCardAction.Choice` が `Influences` 範囲外の場合も **illegal**

戦略的解釈:
- 相手から付与された強力な負影響(例:No.11「機械仕掛けの冬将軍」の Hand.Count 連動 SDP 減、No.04「静寂を纏う」の特定カード使用禁止)を逆利用して相手にも同じ仕打ちを返す戦術カード
- 自分から自分にかけた正影響(例:No.02 緑の侵攻 の Self SDP+α)も Reuse 可能(Self 起点で再発動するため自己強化的に動く)
- 影響の `OriginEffects` が空 list(連鎖 Reuse 由来、または旧 v1 JSON 由来)の場合は再使用しても何も起きないが Influence は除去(consume)される

## Echo キーワード機構(ADR-0023)

### 効果列スナップショット(`OriginEffects`)の動的注入

`PlayerInfluence` に `OriginEffects: IReadOnlyList<IEffect>` フィールドが追加され、Influence 生成時に **EffectInterpreter** が `EffectContext.CurrentCardEffects`(= 現在評価中のカードの効果列スナップショット)から動的に詰める。これにより:

- カタログ定義側は `OriginEffects` に touch 不要(`Array.Empty<IEffect>()` で生成 → 動的注入)
- 既存全 Influence 付与カード(No.02/04/06/07/08/09/10/11/12 等)はカタログ定義変更なし
- Reuse 中の Influence 付与は context.CurrentCardEffects = null を渡し、新規 Influence の OriginEffects は空 list(連鎖 Reuse 防止)

### `ApplyEchoReuse` 解決ヘルパー

`DrowZzzRule.ApplyPlayCard` が効果列 walk 内で `ReuseInfluenceSourceEffect`(直接 or `KeywordedEffect` 越し)を踏むと、専用ヘルパー `ApplyEchoReuse` を呼ぶ:

1. 現プレイヤーの `Influences[action.Choice]` を取得
2. その `OriginEffects` を Self 起点(= 本カード使用者)で順次再評価:
   - `ChoiceEffect` → `Branches[0]` 固定で再帰(Reuse 時の Choice 再選択は仕様外)
   - `KeywordedEffect` → Inner 再帰
   - `ReuseInfluenceSourceEffect` → no-op(再帰防止)
   - その他 → 通常 `EffectInterpreter.Apply`(reuseContext.CurrentCardEffects = null で連鎖 Reuse 防止)
3. 現プレイヤーの `Influences` から index の影響を除去

### `IsLegalPlayCard` 拡張

カードの効果列に Reuse marker が含まれる場合の追加検証:

- 現プレイヤーの `Influences` が空 → illegal
- `action.Choice` が `Influences` 範囲外 → illegal

## カードデータ表現(InMemoryCardCatalog 登録形)

```csharp
// entries 側
new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("18"), new CardData("対抗手段", new Dictionary<string, int>()))

// effects 側(KeywordedEffect 1 件、Echo キーワード + ReuseInfluenceSourceEffect)
new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("18"), new IEffect[]
{
    new KeywordedEffect(new[] { Keyword.Echo }, new ReuseInfluenceSourceEffect()),
})
```

### `Keyword.Echo` 付与の根拠

`KeywordedEffect` は `Inner` フィールドに 1 件の `IEffect` を必須(`KeywordedEffectAsset` の `Inner` も null 不可)。本カードでは Inner = `ReuseInfluenceSourceEffect`(マーカー、interpreter で no-op、実評価は `DrowZzzRule.ApplyPlayCard` の `ApplyEchoReuse` ヘルパー)。Keyword.Echo は将来同種カード追加時の汎用判別用に `HasKeywordInEffects(effects, Echo)` で利用可能。

## 普遍要件 (Ubiquitous)

- [DZ-377] [Ubiquitous] Card `"18"` shall be registered with name `"対抗手段"` and a single top-level `KeywordedEffect([Keyword.Echo], ReuseInfluenceSourceEffect())` effect. `HasKeywordInEffects(effects, Echo)` shall return `true`(カード全体に Echo キーワード性質)。

## 合法性判定(`IsLegalPlayCard`)

- [DZ-378] When player A's `Influences` is empty and A attempts to play Card `"18"`, `IsLegalMove` shall return `false`(無対象なら使えない、ADR-0023 §6)。
- [DZ-379] When player A has 1 or more `Influences` and A plays Card `"18"` with `PlayCardAction.Choice` in valid range `[0, Influences.Count)`, `IsLegalMove` shall return `true`(対象あり + Choice 範囲内なら合法)。
- [DZ-380] When player A has 1 or more `Influences` and A plays Card `"18"` with `PlayCardAction.Choice` out of range (`Choice < 0` or `Choice >= Influences.Count`), `IsLegalMove` shall return `false`(Choice 範囲外なら illegal)。

## Apply 経路(`ApplyEchoReuse`、Echo 解決)

- [DZ-381] When Card `"18"` is played by player A with `Choice=i` and A's `Influences[i]` has `OriginEffects = [AdjustSdpEffect(Self, +5)]`, the resulting session shall reflect `SDP[A] == initialSDP[A] + 5`(Self 起点で再使用、本カード使用者の SDP に +5)。
- [DZ-382] When Card `"18"` is played by player A with `Choice=i` and A's `Influences[i]` has `OriginEffects = [AdjustSdpEffect(Opponent, -3)]`, the resulting session shall reflect `SDP[B] == initialSDP[B] - 3`(Self 起点で Opponent = 相手プレイヤー B、本カード使用者から見た Opponent)。
- [DZ-383] When Card `"18"` is played by player A with `Choice=i`, A's `Influences[i]` shall be removed (consume), and the resulting `Influences[A]` shall have `Count == initialCount - 1`(再使用後の Influence 除去)。
- [DZ-384] When Card `"18"` is played by player A with `Choice=i` and A's `Influences[i]` has `OriginEffects = []` (empty), the resulting session shall reflect no SDP / Bed / Influence side-effects beyond the Influence removal(空 list は no-op、ただし Influence は除去)。
- [DZ-385] When Card `"18"` is played by player A, A's Hand shall lose Card `"18"` and Field shall gain Card `"18"` at the top position(PlayCardAction 直後の中間状態 = Field 先頭、ADR-0006 §M1-PR5 / facade-barrier.md DZ-371 と同パターン)。

## Reuse 連鎖防止 / 再帰防止

- [DZ-386] When Card `"18"` is played by player A with `Choice=i` and A's `Influences[i]` has `OriginEffects = [ApplyInfluenceEffect(Self, ...)]` (Reuse 中の新規 Influence 付与), the resulting new `Influences[A].Last().OriginEffects` shall be `Array.Empty<IEffect>()`(連鎖 Reuse 防止、ADR-0023 §5)。
- [DZ-387] When Card `"18"` is played by player A with `Choice=i` and A's `Influences[i]` has `OriginEffects = [ReuseInfluenceSourceEffect()]` (Reuse 自身), the resulting session shall reflect no additional side-effects(再帰防止、ADR-0023 §5)。

## OriginEffects 動的注入(他カードへの波及)

- [DZ-388] When player A plays a card whose effects include `ApplyInfluenceEffect(Self, influence)`, the resulting `Influences[A].Last().OriginEffects` shall equal A's played card's `effects` snapshot(`EffectInterpreter` が `context.CurrentCardEffects` から動的注入、ADR-0023 §7)。

## 関連

- ADR:
  - [`docs/adr/0023-echo-keyword-and-reuse-influence-source.md`](../../../../adr/0023-echo-keyword-and-reuse-influence-source.md)(本カード起点の新規 ADR、Echo キーワード + OriginEffects 動的注入 + Reuse 解決ヘルパー)
  - [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」(本カードが拡張する基盤)
  - [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §4「キーワード能力」(`Keyword` enum 設計、本カードが `Echo` 追加で拡張)
- 前提効果: [`../effects/keyworded-effect.md`](../effects/keyworded-effect.md) / [`../effects/apply-influence.md`](../effects/apply-influence.md)
- 既存類似カード(Counter キーワード持ち、本カードと無関係):[`./facade-barrier.md`](./facade-barrier.md)(No.17、機構的に別経路)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/Keyword.cs`(`Echo` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/ReuseInfluenceSourceEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectContext.cs`(`CurrentCardEffects` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(3 経路で OriginEffects 動的注入 + `ReuseInfluenceSourceEffect` case no-op 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Influences/PlayerInfluence.cs`(`OriginEffects` 追加 + Equals override)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyPlayCard` の Echo 解決経路 + `IsLegalPlayCard` の対象/index 検証)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(OriginEffects シリアライズ + 旧 JSON 互換)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/ReuseInfluenceSourceEffectAsset.cs`(新規 SO)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.18 entry + rid 5400/5401)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「18 → 19 種」+ No.18 = 3 枚)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/CountermeasureCardTests.cs`(新規、DZ-377〜388)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/CountermeasureCardCatalogTests.cs`(新規、SO 同等性、INF-163)
- シナリオ: `countermeasure.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-377 | (テスト免除: Ubiquitous) | catalog 登録 + Echo キーワード性質は CountermeasureCardTests + CatalogTests で構造保証 |
| DZ-378 | `Given_Influences空_When_Card18をPlayCard_Then_IsLegalMoveがfalse` | Influences 空時 illegal |
| DZ-379 | `Given_Influences1件_Choice0_When_Card18をPlayCard_Then_IsLegalMoveがtrue` | 合法経路 |
| DZ-380 | `Given_Influences1件_Choice1_When_Card18をPlayCard_Then_IsLegalMoveがfalse` | Choice 範囲外 illegal |
| DZ-381 | `Given_InfluenceOriginEffectsSelfSDPプラス5_When_Card18をPlayCard_Then_自分SDPプラス5` | Self 起点 SDP 適用 |
| DZ-382 | `Given_InfluenceOriginEffectsOpponentSDPマイナス3_When_Card18をPlayCard_Then_相手SDPマイナス3` | Self 起点 → Opponent 解決 |
| DZ-383 | `Given_Influences1件_When_Card18をPlayCard_Then_選択Influence除去` | consume セマンティクス |
| DZ-384 | `Given_OriginEffects空list_When_Card18をPlayCard_Then_副作用なしInfluenceのみ除去` | 空 list = no-op + consume |
| DZ-385 | `Given_Card18手札_When_PlayCardAction_Then_HandからRemove_FieldにAdd` | Hand/Field 操作 |
| DZ-386 | `Given_OriginEffectsApplyInfluence_When_Card18をPlayCard_Then_新Influence_OriginEffects空list` | 連鎖 Reuse 防止 |
| DZ-387 | `Given_OriginEffectsReuseInfluenceSource_When_Card18をPlayCard_Then_副作用なし` | 再帰防止 |
| DZ-388 | `Given_ApplyInfluence持ちカード_When_PlayCard_Then_新Influence_OriginEffectsはカード効果列` | OriginEffects 動的注入 |
