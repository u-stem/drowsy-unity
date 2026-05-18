# ADR-0024: AssociateToFirstPlayerOnGameStart marker + StartGameUseCase の ICardCatalog 再依存

- Status: Accepted
- Date: 2026-05-18
- Decider: -

---

## Context

カード No.19「絶対障壁」(オーナー JIT 確定 2026-05-18)が以下の仕様を持つ:

- **「反撃・狂乱」**:Counter キーワード(M3-PR5b/c)+ Frenzy キーワード(ADR-0011 §4.5)の両方を持つ反撃カード(No.17「見掛け倒しの障壁」+ Frenzy で反撃を受けない上位版)
- **ゲーム開始時、先行のプレイヤーはこの手段を連想する**(共通山札からは引けない、先行プレイヤー限定の入手経路)
- 1 ゲーム 1 枚限定(先行プレイヤー初期連想のみが入手経路)

このうち「反撃・狂乱」部分は既存 `KeywordedEffect([Counter, Frenzy], dummy)` 1 件最上位で表現可能(機構変更なし)だが、「ゲーム開始時、先行プレイヤーへの自動連想」は **既存にない経路**:

- M1 で確立した `StartGameUseCase` は「先後ランダム決定 + FDP 抽選 + 初期手札配布(山札からの 5 サイクル交互配布)」のみ
- ADR-0019 で確立した自動連想機構(`AssociateAction` / `AssociateSpecificCardEffect`)は **ゲーム進行中**のプレイヤーアクション由来であり、ゲーム開始時の自動連想は対象外

### 表現方法の選択肢

「ゲーム開始時、先行プレイヤーへの自動連想」を表現する 3 案:

| 案 | 概要 | 評価 |
| ---- | ---- | ---- |
| A | 新規 effect marker `AssociateToFirstPlayerOnGameStartEffect`(カード自身の effects 列に含める)+ StartGameUseCase が catalog 全 entry を scan して該当 marker 持ちカードを先行プレイヤーへ自動連想 | カード自身がメタデータを保持、将来同種カード追加時にカードデータ追加だけで対応可能 |
| B | StartGameUseCase 引数に `firstPlayerInitialAssociations: IReadOnlyList<CardTypeId>` 追加(Bootstrap で `[CardTypeId.Of("19")]` を渡す) | データと設定が分散、Bootstrap 側にカード固有知識を持たせる |
| C | StartGameUseCase 内に「カード "19" 専用」のハードコード | 汎用性なし、将来同種カード追加時に都度 refactor |

→ **案 A 採用**。カード自身が「自分はゲーム開始時に先行プレイヤーへ自動連想されるべき」というメタデータを `IEffect` として保持する設計が、既存の effect marker パターン(`AssociatableMarkerEffect` / `RequiresMinimumTotalPointsMarkerEffect` / `UsageRestrictionMarkerEffect`)と整合する。

### ADR-0014 との関係(部分 Supersede)

[ADR-0014](./0014-start-game-usecase-cardcatalog-removal.md) で「`StartGameUseCase` から `ICardCatalog<IEffect>` 依存を削除」と決定済(M4 完了時の dead 依存解消)。本 ADR-0024 はこの決定を **部分的に覆す**(`StartGameUseCase` に `ICardCatalog<IEffect>` を再追加):

- ADR-0014 起票時点(2026-05-15 頃)で本機構(「ゲーム開始時の自動連想」)は未予見
- ADR-0014 が依拠した条件(`StartGameUseCase` がカード情報を本当に必要としない事実)は本 ADR-0024 で覆る(catalog の全 entry を scan して marker effect を持つカードを抽出する必要が生まれる)
- ADR-0014 §「再評価条件」には明示的な閾値はなかったが、新規メカニクス追加で catalog 知識が必要になった本ケースは妥当な再評価契機

ADR-0014 の Status は **Accepted のまま維持**(本 ADR-0024 で部分的に覆ったことを明記、Superseded by には設定しない)。理由:
- ADR-0014 の精神(dead 依存の排除)は維持される(本機構の必要性が生まれた以上、依存は dead ではなく live)
- catalog 依存の再追加は「本機構の必要性が生まれた結果の正当な依存」であり、ADR-0014 で削除した「未使用 dead 依存」とは性質が異なる

### 入手経路ポリシー

本カードは **共通山札に含めない**(後手プレイヤーは引けない):

- Bootstrap.`BuildInitialDeck` で catalog 全 entry を走査する経路に、`AssociateToFirstPlayerOnGameStartEffect` を最上位 effect として持つカードを **除外**するフィルタを追加
- 入手経路は「ゲーム開始時の先行プレイヤー自動連想」**のみ**(1 ゲーム 1 枚限定)
- これにより「絶対障壁は先行プレイヤーの開幕アドバンテージ」という設計意図を仕組みレベルで担保

## Decision

### 1. 新規 marker `AssociateToFirstPlayerOnGameStartEffect`

```csharp
public sealed record AssociateToFirstPlayerOnGameStartEffect : IEffect;
```

マーカーレコード。`EffectInterpreter` は本 effect の case を追加するが、case 内では **no-op**(`session` 不変返却)— 実評価は `StartGameUseCase.Execute` 内で catalog 全 entry を scan する形で行う(`ChoiceEffect` / `ReuseInfluenceSourceEffect` と同じ「rule / use-case 評価層で unwrap される」パターン)。

### 2. `StartGameUseCase` に `ICardCatalog<IEffect>` を再追加

ctor を 2 引数 → **3 引数**に変更:

```csharp
public StartGameUseCase(IRandomSource rng, IGameConfig config, ICardCatalog<IEffect> catalog)
```

`Execute` 内で以下の処理を **初期手札配布の後・GameState 構築の前**に追加:

```
foreach (var typeId in catalog.RegisteredCardTypeIds)
{
    var effects = catalog.GetEffects(typeId);
    if (HasFirstPlayerAssociationEffectInTopLevel(effects))
    {
        var newCardId = CardId.Of(typeId, 0);
        hands[0] = hands[0].Add(newCardId);  // 先行プレイヤー = shuffledPlayers[0]
        associatedCardIds.Add(newCardId);    // ADR-0019 連想由来記録
    }
}
```

注:`ICardCatalog<TEffect>` に本 ADR で `RegisteredCardTypeIds: IReadOnlyCollection<CardTypeId>` プロパティを追加し、`StartGameUseCase` / `Bootstrap.BuildInitialDeck` の双方で全 entry 列挙の単一情報源とする(`ScriptableObjectCardCatalog` には既に同名プロパティ有り、`InMemoryCardCatalog` に新規追加)。

走査スコープは **最上位 effects のみ**(wrapper 内側は再帰しない、`AssociatableMarkerEffect` / `RequiresMinimumTotalPointsMarkerEffect` と同方針、ADR-0011 §6 / `HasKeywordInEffect`)。

### 3. `Bootstrap.BuildInitialDeck` の除外フィルタ

`AssociateToFirstPlayerOnGameStartEffect` 持ちのカードは共通 deck に含めない:

```
foreach (var entry in catalog.GetAll())
{
    var effects = catalog.GetEffects(entry.Key);
    if (HasFirstPlayerAssociationEffectInTopLevel(effects))
    {
        continue;  // 共通山札からは引けない、先行プレイヤー自動連想のみが入手経路
    }
    for (int i = 0; i < CopiesPerCardForM5Deck; i++) deckCards.Add(...);
}
```

### 4. `AssociatedCardIds` への記録(ADR-0019 整合)

開始時自動連想されたカード ID は `DrowZzzGameSession.AssociatedCardIds` に追加する。これにより:

- No.04「静寂を纏う」系の「連想由来カードを TargetCardId に取れない」仕様(ADR-0019)が本カードにも適用される(=対象として選べない、絶対障壁を狙い撃ちで使用禁止にできない)
- 「連想された手段」の単一情報源(`session.IsAssociated(cardId)`)に統合される

### 5. 永続化 / SO

| 観点 | 確定 |
| ---- | ---- |
| `EffectJsonConverter` | `"AssociateToFirstPlayerOnGameStart"` discriminator(フィールドなし marker、`EarlyWinTrigger` 等と同パターン)|
| `AssociateToFirstPlayerOnGameStartEffectAsset` | 新規 SO(フィールドなし marker、`UsageRestrictionMarkerEffectAsset` 等と同パターン)|
| `PersistedSessionV1` schemaVersion | bump 不要(新規 effect 追加のみ、既存スキーマ変更なし)|

## Consequences

### 正

- カード自身がメタデータを保持する設計により、将来同種カード(例:「両プレイヤー開始時連想」「後手プレイヤー専用開始時連想」)追加時もカードデータの追加だけで対応可能(scope に応じて新規 marker 追加)
- `AssociatedCardIds` 統合により、既存の「連想由来除外」機構が本カードにも自動的に適用される(機構的整合性)
- Bootstrap 側のデッキ構築は catalog scan + フィルタで自動的に正しいデッキを構築(本カードのハードコード不要)

### 負

- `StartGameUseCase` の `ICardCatalog<IEffect>` 依存再追加で、`StartGameUseCaseTests` 既存テストの ctor シグネチャ更新が必要(ADR-0014 後に確立した 2 引数前提のテスト全件)
- ADR-0014 の決定を部分的に覆すため、設計判断の追跡可読性が下がる(本 ADR-0024 で経緯を明示することで補う)
- ADR-0014 「StartGameUseCase は本質的にカード情報を必要としない」の判断は本 ADR-0024 で **「ゲーム開始時のカード scan が必要なメカニクス追加で覆る」** と覆す

### 中立

- Phase 3 候補:
  - 「両プレイヤー開始時連想」「後手プレイヤー専用開始時連想」等の派生 marker(`AssociateToBothPlayersOnGameStartEffect` / `AssociateToSecondPlayerOnGameStartEffect`)
  - 「ゲーム開始時 N 枚連想」(`AssociateToFirstPlayerOnGameStartEffect(count: int)` パラメータ化)
  - 開始時連想カードの順序保証(複数該当時の Hand 追加順序を catalog 定義順 / カードナンバー順で固定するか)

## Related

- ADR:
  - [ADR-0014](./0014-start-game-usecase-cardcatalog-removal.md) — 本 ADR で部分的に覆す(catalog 依存再追加、Status は維持)
  - [ADR-0019](./0019-associated-card-ids-session-field.md) — 連想由来カードの記録(本 ADR は同経路を流用)
  - [ADR-0011](./0011-m3-dream-card-and-game-mechanics-expansion.md) §4「キーワード能力」+ §4.5「Frenzy = 反撃を受けない」(本カードが Counter + Frenzy 両方持ち、機構変更なし)
- 実装(本 PR、`feat/card-no19-absolute-barrier`):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AssociateToFirstPlayerOnGameStartEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(no-op case 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`(ctor 3 引数 + catalog scan + 先行プレイヤー自動連想)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/AssociateToFirstPlayerOnGameStartEffectAsset.cs`(新規 SO)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(`AssociateToFirstPlayerOnGameStart` discriminator 追加)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(`BuildInitialDeck` 除外フィルタ + StartGameUseCase 3 引数 Register)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.19 entry + rid 5500/5501/5502)
- 仕様(本 PR):
  - `docs/specs/games/drowzzz/cards/absolute-barrier.md` / `.feature`(No.19 EARS + Gherkin)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/StartGameUseCaseTests.cs`(既存 ctor 更新 + 開始時連想検証追加)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/AbsoluteBarrierCardTests.cs`(新規)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/AbsoluteBarrierCardCatalogTests.cs`(新規、INF-164)
