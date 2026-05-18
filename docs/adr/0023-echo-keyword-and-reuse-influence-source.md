# ADR-0023: Echo キーワード + PlayerInfluence.OriginEffects + ReuseInfluenceSourceEffect の導入

- Status: Accepted
- Date: 2026-05-18
- Decider: -

---

## Context

カード No.18「対抗手段」(オーナー JIT 確定 2026-05-18)が以下の仕様を持つ:

- **甲効果**:プレイヤー(=本カード使用者)が「受けている影響(`PlayerInfluence`)から 1 つを選択し、その影響を持つ手段(=発生源カード)を再使用する」
- 再使用時に選択した影響は除去される(オーナー JIT 補足 2026-05-18)
- 「反撃」はカードテキスト上の特殊効果分類(フレーバー)であって、機構上の `Keyword.Counter`(M3-PR5b)とは独立した別概念

### 既存設計の確認

M2-PR5(ADR-0007 §1.5)で確立した `PlayerInfluence` は:

```csharp
public sealed record PlayerInfluence(InfluenceTrigger Trigger, IEffect TickEffect, int RemainingCount);
```

これは「Tick タイミングで `TickEffect` を 1 回適用、`RemainingCount` が 0 に到達したら除去」という意味論で、**発生源カードに関する情報を保持しない**。No.18 が要求する「影響の発生源カードを再使用」を実現するには、`PlayerInfluence` が「発生源カードの効果列のスナップショット」を保持する必要がある。

Counter キーワード機構(M3-PR5b/c, ADR-0011 §4.3/§4.4)は「相手 `PlayCardAction` を `WaitingForCounterResponse` フェーズで無効化する」reactive 経路で、本カードの「**自フェーズの通常 `PlayCardAction` 経路で発動 + 受けている影響を選択 + その源カードを再 EffectInterpreter**」とは経路もセマンティクスも別物。

### 仕様確定(オーナー JIT 2026-05-18)

| 観点 | 確定内容 |
| ---- | ---- |
| 影響範囲 | 本カード使用者自身の `Influences` のみ(相手の影響は対象外) |
| 再使用の起点 | 再使用者(=本カード使用者)を `SdpTarget.Self` 起点に |
| 再使用の単位 | 発生源カードの **全効果列を再 `EffectInterpreter`** |
| 選択影響の挙動 | 再使用後、選択した影響は **除去**(consume、再使用しなかった場合も含めて 1 回限り) |
| 「反撃」の機構上位置 | カードテキスト分類タグ(フレーバー)、機構 Keyword とは独立 |
| Keyword 化 | `Keyword.Echo` を新規追加(将来同種カード追加時の汎用判別用) |

### 新規概念の選択肢

「発生源カードを再使用」を実現する設計案:

| 案 | 概要 | 評価 |
| ---- | ---- | ---- |
| A | `PlayerInfluence` に `OriginEffects: IReadOnlyList<IEffect>` 追加 + 専用 `IEffect` で「現プレイヤーが選択した影響の OriginEffects を本プレイヤー Self 起点で再 EffectInterpreter」 | 既存設計の自然な拡張、最小限のスキーマ追加で実現可能 |
| B | `DrowZzzGameSession` に新規 `InfluenceOrigins: Dictionary<...>` フィールド追加 | スキーマ大幅変更、Influence と Origin が同期しないバグの温床 |
| C | カード固有処理として `DrowZzzRule.ApplyPlayCard` に手書き分岐 | scalability なし、新規同種カード追加時に毎回 case 追加 |

→ **案 A 採用**。`PlayerInfluence` への新規フィールド追加は既存全 Influence 付与経路(9 カード + helper)に touch する breaking change だが、影響範囲は機械的に把握可能で、Phase 3 以降の同種カード追加時にスキーマ変更不要で対応可能。

### Reuse の連鎖と循環参照問題

「発生源カードの全効果列を再 EffectInterpreter」と「Reuse 中に新規 Influence が付与される」場合の振る舞いを定義しないと、無限ループや循環参照(カード effect 列 → 自身を含む)になり得る。

| 論点 | 確定 |
| ---- | ---- |
| Reuse 中に `ReuseInfluenceSourceEffect` を踏んだら | **no-op**(再帰防止、`ReuseInfluenceSourceEffect` 自体は no-op) |
| Reuse 中に `ChoiceEffect` を踏んだら | **`Branches[0]` 固定**(Reuse 時の Choice 再選択は仕様外、最小実装) |
| Reuse 中に `KeywordedEffect` を踏んだら | **Inner を再帰評価**(EffectInterpreter のデフォルト挙動と同じ) |
| Reuse 中に付与される新規 Influence の OriginEffects | **`Array.Empty<IEffect>()`**(連鎖 Reuse 防止) |
| OriginEffects が空 list の Influence を選択した | 再 EffectInterpreter は何もしないが、Influence は除去(consume) |
| 現プレイヤーの Influences が空のとき本カードを Play | `IsLegalPlayCard` で illegal(無対象なら使えない) |

### OriginEffects の動的詰め

カタログ側(`InMemoryCardCatalog` / SO Catalog)で `PlayerInfluence(..., OriginEffects: <カードの効果列>)` と書くと **循環参照になる**(カード自身の effects 列の中の `ApplyInfluenceEffect` が、その effects 列全体を参照する)。

→ **解決策**: カタログ側では `OriginEffects = Array.Empty<IEffect>()` で生成し、`EffectInterpreter` が Influence 付与を実行するときに **`EffectContext.CurrentCardEffects` から動的に詰める**(`with { OriginEffects = ... }`)。

`DrowZzzRule.ApplyPlayCard` でカードの effects 列全体を `EffectContext.CurrentCardEffects` に詰めて、interpreter まで透過する。これにより:
- 既存カードカタログ定義は OriginEffects に touch 不要(`Array.Empty<IEffect>()` で生成 → 動的注入)
- `ApplyInfluenceEffect` / `ApplyTargetedRestrictionEffect` / `ConditionalApplyOrClearInfluencesEffect`(Apply 経路)の 3 箇所で context.CurrentCardEffects を Influence の OriginEffects に上書き

Reuse 中の Influence 付与は context.CurrentCardEffects = `Array.Empty<IEffect>()` を渡して動的注入を空 list にする → 連鎖 Reuse は機構的に成立するが効果は空 = no-op。

## Decision

### 1. `Keyword.Echo` を enum に追加

```csharp
public enum Keyword
{
    Frenzy,
    Instinct,
    Counter,
    Echo,    // ADR-0023:受けている影響を選んで源カードを再使用する効果(No.18 で初導入)
}
```

判別用属性で、評価時には副作用なし(`KeywordedEffect` の Inner を逐次評価するのみ、既存 `Counter` / `Frenzy` / `Instinct` と同パターン)。`HasKeywordInEffects(effects, Echo)` は将来同種カード追加時の汎用判別用に利用可能。

### 2. `PlayerInfluence` に `OriginEffects` フィールド追加

```csharp
public sealed record PlayerInfluence
{
    public InfluenceTrigger Trigger { get; init; }
    public IEffect TickEffect { get; init; }
    public int RemainingCount { get; init; }
    public IReadOnlyList<IEffect> OriginEffects { get; init; }  // ADR-0023 追加

    // 3 引数 ctor は後方互換(OriginEffects = Array.Empty)
    // 4 引数 ctor は新規(originEffects null fallback で Array.Empty、旧 v1 JSON 互換用)
}
```

**Equals override の挙動**:Equals / GetHashCode は **Trigger / TickEffect / RemainingCount の 3 フィールドのみ**で判定する。`OriginEffects` は equality 対象外。

理由:
- `OriginEffects` は Reuse 用の補助データであり、Influence の本質的アイデンティティではない(同じ trigger + 同じ tick effect + 同じ count なら、生成元カードが何であれ振る舞いは同じ)
- equality に OriginEffects を含めると、既存 PlayerInfluence 依存テスト 40+ 箇所が「期待値 PlayerInfluence(OriginEffects = 空) vs actual PlayerInfluence(OriginEffects = カードの effects)」で全件破壊される(動的注入は EffectInterpreter 経由で起こるため、テスト期待値で OriginEffects を明示するのは現実的でない)
- OriginEffects の検証が必要なテストは `.OriginEffects.Count` / `.OriginEffects[i]` を直接 assert する(本 PR の `DZ-386` / `DZ-388` がその形式)

カタログ定義側では `OriginEffects = Array.Empty<IEffect>()` で生成し、`EffectInterpreter` 経由で動的に詰める。

### 3. `EffectContext.CurrentCardEffects` 追加

```csharp
public sealed record EffectContext(
    int InfluenceRemovalIndex,
    CardId TargetCardId = null,
    IReadOnlyList<IEffect> CurrentCardEffects = null)
{
    // null と空 list を等価扱い(Default で null OK)
    public static EffectContext Default { get; } = new EffectContext(0);
}
```

`DrowZzzRule.ApplyPlayCard` でカードの effects 列を取得して `CurrentCardEffects` に詰める。Tick 経路 / Reactive 経路 / Reuse 中の context は CurrentCardEffects = null(=空 list 扱い、Influence の OriginEffects は空になる)。

### 4. `ReuseInfluenceSourceEffect` 新規 IEffect 追加

```csharp
public sealed record ReuseInfluenceSourceEffect : IEffect;
```

マーカーレコード。`EffectInterpreter` は本 effect の case を追加するが、case 内では **no-op**(`session` 不変返却)— 実評価は `DrowZzzRule.ApplyPlayCard` の専用ヘルパーで行う(ChoiceEffect と同じ「rule 評価層で unwrap される」パターン)。

### 5. `DrowZzzRule.ApplyPlayCard` の Echo 解決経路

`PlayCardAction.Choice` を「保有 Influence の index 選択」用に流用(本カード No.18 は `ChoiceEffect` を持たないため衝突なし)。

```
ApplyPlayCard 内:
  effects 列内に ReuseInfluenceSourceEffect が含まれる場合:
    1. currentPlayer の Influences リストを取得
    2. action.Choice を index として Influences[choice] を取得
    3. その Influence の OriginEffects を ApplyEffectsAsReuse ヘルパーで再評価
       - ChoiceEffect → Branches[0] を再帰評価
       - KeywordedEffect → Inner を再帰評価
       - ReuseInfluenceSourceEffect → skip(no-op)
       - その他 → EffectInterpreter.Apply(session, effect, contextWithEmptyCurrentCardEffects) に委譲
    4. currentPlayer の Influences から index の影響を除去
  effects 列内の他 effect は通常通り EffectInterpreter.Apply
```

### 6. `IsLegalPlayCard` 拡張

カードの effects 列内に `ReuseInfluenceSourceEffect` が(`KeywordedEffect` 越しも含めて)含まれる場合の追加条件:

- 現プレイヤーの `Influences` が空 → `IsLegalMove` は false(無対象なら使えない)
- `action.Choice` が範囲外(`Choice < 0` or `Choice >= Influences.Count`)→ `IsLegalMove` は false

### 7. 既存 Influence 付与経路の touch

OriginEffects を動的に詰める箇所(`EffectInterpreter` 内):

| 経路 | 動的注入 |
| ---- | ---- |
| `ApplyApplyInfluence` | `effect.Influence with { OriginEffects = context.CurrentCardEffects ?? Array.Empty<IEffect>() }` を末尾追加 |
| `ApplyApplyTargetedRestriction` | 構築する `restrictionInfluence` の OriginEffects に同上を詰める |
| `ApplyConditionalApplyOrClearInfluences`(Apply 経路) | `effect.InfluenceToApply with { OriginEffects = context.CurrentCardEffects ?? Array.Empty<IEffect>() }` を末尾追加 |

### 8. 永続化 / SO 化

| 観点 | 確定 |
| ---- | ---- |
| `PersistedSessionV1` schemaVersion | **bump 不要**(Dictionary 型表現は不変、`PlayerInfluence` JSON 内側の構造変更のみ) |
| `JsonConverter` での OriginEffects 旧 JSON 互換 | nullable + `?? Array.Empty<IEffect>()` fallback(ADR-0019 `AssociatedCardIds` パターンと同方針) |
| `PlayerInfluenceAsset` | **変更不要**。OriginEffects は `EffectInterpreter` 経由の動的注入(`context.CurrentCardEffects`)で詰められるため、SO 定義側に `_originEffects` フィールドを持たせる必要はない(循環参照回避設計と整合、カタログ定義で OriginEffects を書かない方針)|
| `ReuseInfluenceSourceEffectAsset`(新規 SO) | `KeywordedEffectAsset` の `Inner` として参照可能にする |
| `PlayerInfluenceJsonConverter`(新規 Infrastructure converter) | `PersistedSessionV1.Influences` deserialize 経路の Newtonsoft 自動 ctor 選択が複数 ctor + 引数なし ctor 不在で失敗する問題を回避するため、専用 converter で PascalCase schema を制御 + 旧 v1 JSON 後方互換(`OriginEffects` キー欠落時 `Array.Empty` フォールバック)|

## Consequences

### 正

- `PlayerInfluence` に発生源情報を持たせる構造が確立し、将来「影響の発生源」を必要とするカード(例:「源カードを破棄する」「源カードと同種を山札から検索」等)が同じパターンで実装可能
- `Keyword.Echo` 追加により将来の同種カード(別バリアントの Echo カード)が `HasKeywordInEffects` で機械判別可能
- 既存カードへの影響は最小(OriginEffects は動的注入で、カタログ定義側は touch 不要)

### 負

- `PlayerInfluence` の 4 フィールド化により、既存テスト 40+ 箇所が等値比較で破壊される(全箇所で OriginEffects 引数追加が必要)
- Reuse 中の `ChoiceEffect` を `Branches[0]` 固定で扱うのは仕様簡素化のため、将来「Choice 再選択」が必要になった場合は追加機構が要る
- 連鎖 Reuse は機構的に成立するが効果は空 list で no-op(意図的制約、Phase 3 候補で再評価)

### 中立

- Phase 3 候補:
  - 連鎖 Reuse の有効化(OriginEffects 継承戦略の確定)
  - Reuse 時の Choice 再選択機構(`PlayCardAction.NestedChoice` のような追加フィールド)
  - 「相手の影響を選択」「全プレイヤーの影響を選択」拡張(本カードは「自分の影響のみ」)

## Related

- ADR:
  - [ADR-0007](./0007-m2-detail-card-effects.md) §1.5「継続影響(Influence)」(本 ADR が拡張する基盤)
  - [ADR-0011](./0011-m3-dream-card-and-game-mechanics-expansion.md) §4「キーワード能力」(`Keyword` enum 設計、本 ADR が `Echo` 追加で拡張)
  - [ADR-0019](./0019-associated-card-ids-session-field.md)(nullable + `Array.Empty` fallback パターン、本 ADR が永続化で同パターンを採用)
  - [ADR-0020](./0020-influence-count-decrement-timing.md)(Influence カウント減算タイミング、本 ADR で除去経路を増やすが既存 EndTurn 減算と独立)
  - [ADR-0022](./0022-reactive-influence-trigger-extension.md)(`InfluenceTrigger` 拡張、本 ADR は別ベクトルでの Influence 拡張)
- 実装(本 PR、`feat/card-no18-countermeasure`):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Influences/PlayerInfluence.cs`(`OriginEffects` 追加 + Equals override)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/Keyword.cs`(`Echo` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/ReuseInfluenceSourceEffect.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectContext.cs`(`CurrentCardEffects` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(3 経路で OriginEffects 動的注入 + `ReuseInfluenceSourceEffect` case no-op 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyPlayCard` の Echo 解決経路 + `IsLegalPlayCard` の対象/index 検証)
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`(OriginEffects シリアライズ + 旧 JSON 互換)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/PlayerInfluenceAsset.cs`(`_originEffects` 追加)
  - `Assets/_Project/Scripts/Infrastructure/Games/DrowZzz/Effects/ReuseInfluenceSourceEffectAsset.cs`(新規 SO)
  - `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset`(No.18 entry 追加)
  - `Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs`(カード種数コメント「18 → 19 種」)
- 仕様(本 PR):
  - `docs/specs/games/drowzzz/cards/countermeasure.md` / `.feature`(No.18 EARS + Gherkin)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/CountermeasureCardTests.cs`(新規)
  - `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Games/DrowZzz/Cards/CountermeasureCardCatalogTests.cs`(新規)
  - 既存 PlayerInfluence 依存 40+ ファイル(OriginEffects 引数追加)
