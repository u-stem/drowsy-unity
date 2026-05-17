# ADR-0022: InfluenceTrigger 拡張 — Reactive Influence(アクション後発動型)の追加

- Status: Accepted
- Date: 2026-05-17
- Decider: -

---

## Context

カード No.12「偽りの太陽」(オーナー JIT 確定 2026-05-17)が以下の仕様を持つ:

- 夜効果:Self SDP -4 / Opponent SDP +6 + 甲(自分)にこの手段が持つ影響を付与
- 朝効果:Self SDP -4 / Opponent SDP +18(影響付与なし)
- **影響**:プレイヤー(=影響保有者)は **手段を使用したなら SDP-10、放棄をしたなら SDP+5**
- カウント:Perpetual(永続)

このうち **影響**部分は、既存 InfluenceTrigger(`OwnPhaseStart` のみ)では表現できない。既存トリガーは「自フェーズ開始時に Tick」のみで、「**保有者のアクション実行後**に Tick」する仕組みが存在しないため、`InfluenceTrigger` を拡張する必要がある。

### 既存設計の確認

M2-PR5(ADR-0007 §1.5)で確立した `PlayerInfluence` は:

```csharp
public sealed record PlayerInfluence(InfluenceTrigger Trigger, IEffect TickEffect, int RemainingCount);
```

`InfluenceTrigger` enum は `OwnPhaseStart` のみで、Tick タイミングは `DrowZzzRule.TickInfluencesForCurrentPlayer`(`ApplyEndTurn` の `Turn.Next` 後、ADR-0020 で減算タイミング分離)。

### 新たな Reactive 概念の必要性

No.12 の「使用したら / 放棄したら」は、`PlayCardAction` / `AbandonAction` の **アクション実行後** に発動する必要がある。これを表現する 3 つの設計案:

| 案 | 概要 | 評価 |
| ---- | ---- | ---- |
| A | `InfluenceTrigger` を拡張(`OnOwnPlayCardAfter` / `OnOwnAbandonAfter` 追加) | 既存設計の自然な拡張、Reactive の意味論を Trigger 値で表現 |
| B | `DrowZzzGameSession` に新規 `ReactiveEffects` フィールド追加 | 新概念導入で複雑化、Persistence 影響大 |
| C | ApplyPlayCard / ApplyAbandon 内でカード固有処理として手書き | scalability なし、汎用性なし |

→ 案 A 採用。InfluenceTrigger 拡張は既存メカニズムの自然な延長で、将来同様のカード追加時にトリガー値追加で対応可能。

### 「付与直後の本アクション自体への適用」問題

オーナー JIT(2026-05-17)で「**本カード付与直後の同一ターンで、本カードプレイしたアクションには影響が適用されない**」と確定。

実装上の選択肢:
- (a) `ApplyPlayCard` の **冒頭** で walk → 本カード処理前なので影響対象外、後続のアクションのみ対象(直感的でない:「カード使用後」が「カード使用前」になる)
- (b) `ApplyPlayCard` の **末尾** で walk + snapshot ベース → 本カードで付与された influence は対象外、既存 influence は発動。**「使用後」の意味論を honor し、かつ自己適用回避** を両立

→ 案 (b) snapshot ベース末尾 walk を採用。

## Decision

### 1. InfluenceTrigger 拡張

`InfluenceTrigger` enum に 2 値追加:

```csharp
public enum InfluenceTrigger
{
    /// <summary>影響保有者の自フェーズ開始時に発動(M2-PR5 確立)。</summary>
    OwnPhaseStart,

    /// <summary>影響保有者の `PlayCardAction` 実行直後に発動(ADR-0022 で導入、No.12「偽りの太陽」)。</summary>
    OnOwnPlayCardAfter,

    /// <summary>影響保有者の `AbandonAction` 実行直後に発動(ADR-0022 で導入、No.12「偽りの太陽」)。</summary>
    OnOwnAbandonAfter,
}
```

### 2. Reactive Tick の実行タイミング

`DrowZzzRule.ApplyPlayCard` / `ApplyAbandon` の **末尾**(SDP 効果適用 + Hand 操作 + PendingCounteredEffects 確定後)に Reactive Tick walk を追加:

```csharp
private DrowZzzGameSession ApplyPlayCard(DrowZzzGameSession session, PlayCardAction action)
{
    // 1. 既存 influences を snapshot(walk 対象の不変リスト)
    int currentIndex = session.GameState.Turn.CurrentPlayerIndex;
    var currentPlayerId = session.GameState.Players[currentIndex].Id;
    var preInfluences = session.Influences[currentPlayerId];  // PlayCard 開始時の list

    // 2. 既存処理(SDP 効果列の interpreter 評価、Hand 操作、ApplyInfluence 等)
    var afterApplied = /* 既存 ApplyPlayCard 処理 */;

    // 3. snapshot に対して OnOwnPlayCardAfter walk
    //    本 PlayCard で新規付与された influence は preInfluences に含まれないため対象外
    afterApplied = ApplyReactiveInfluencesAfter(afterApplied, preInfluences, InfluenceTrigger.OnOwnPlayCardAfter);
    return afterApplied;
}
```

### 3. 新規 effect 2 件

| effect 名 | Trigger | 用途 |
| ---- | ---- | ---- |
| `AdjustSdpAfterPlayCardEffect(int Delta)` | `OnOwnPlayCardAfter` | 保有者の SDP に Delta 加算(No.12 では Delta=-10) |
| `AdjustSdpAfterAbandonEffect(int Delta)` | `OnOwnAbandonAfter` | 保有者の SDP に Delta 加算(No.12 では Delta=+5) |

両方とも `Delta` フィールドあり(再利用性、将来「使用時 SDP+3」のような派生カードで再利用可能、No.02「緑の侵攻」の `AdjustSdpEffect(SdpTarget, int Delta)` と同パターン)。

### 4. snapshot ベース walk のセマンティクス

`ApplyPlayCard` 冒頭で `session.Influences[currentPlayerId]` を読み取り(IReadOnlyList、Immutable なので snapshot として安全)、末尾でこの **list 内の影響のみ** を walk する。本 PlayCard で `ApplyInfluence` により追加された新規 influence は post-PlayCard session の `Influences[currentPlayerId]` には含まれるが、snapshot には含まれないため walk 対象外となる。

これにより「本カードで付与された影響を本カード自身で発動」を構造的に回避(オーナー JIT「付与時のアクション自体は除外」を honor)。

### 5. Decrement / EndTurn 経路への影響

ADR-0020 で確立した `DecrementInfluencesForCurrentPlayer`(ApplyEndTurn 冒頭、旧 current の全 Influences を count -1)は **トリガー種別を区別しない**(全 Influences に対して一律 -1)。Reactive Influence(`OnOwnPlayCardAfter` / `OnOwnAbandonAfter`)も同様に毎フェーズ -1 されるが、本 PR では Perpetual のみ(int.MaxValue)を使用するため実質除去されない。

将来 Reactive Influence で count=N(N < Perpetual)カードを導入する場合は、**「Reactive Influence は使用回数で除去するか、フェーズ経過で除去するか」** の設計判断を別 ADR で確定する。本 ADR では Perpetual のみサポート。

## Consequences

### Positive

- No.12「偽りの太陽」の「使用したら SDP-10 / 放棄したら SDP+5」セマンティクスが既存 `PlayerInfluence` の構造拡張で実装可能
- 将来の Reactive 系カード(「ドロー時に SDP+N」「連想時に SDP-N」等)も同パターンで `InfluenceTrigger` 値追加 + 専用 effect 追加で対応可能
- snapshot ベース walk で「付与時の本アクション自身への適用回避」を構造的に保証(明示的なガード不要)
- 既存 `OwnPhaseStart` トリガーのカード(No.02 / No.04 / No.06 / No.07 / No.08 / No.09 / No.10 / No.11)への影響なし(switch / walk ロジックが OwnPhaseStart 専用経路で完結)

### Negative

- `InfluenceTrigger` enum 値が 3 倍化(1 → 3)、将来 Reactive トリガー追加で更に増える可能性
- `ApplyPlayCard` / `ApplyAbandon` 内で「冒頭 snapshot + 末尾 walk」の 2 ステップが必要、コード量増加
- Persistence:`InfluenceTrigger` は int 値で JSON 保存(`PlayerInfluenceAsset._trigger`)、enum 値追加で旧 v1 JSON との後方互換性は維持されるが、新トリガー値を持つ session を旧コードでロードすると `_trigger` が認識できない(将来の互換性要件)

### Neutral

- `IGameRule` interface 変更なし
- `DrowZzzGameSession` のフィールド変更なし
- ADR-0020(Decrement タイミング)/ ADR-0021(EndTurn 合法化条件)に影響なし

## Alternatives Considered

### 不採用案 A: `DrowZzzGameSession` に新規 `ReactiveEffects` フィールド追加

`PlayerInfluence` とは別の概念として `Dictionary<PlayerId, IReadOnlyList<ReactiveEffect>>` を新設。

| 観点 | 評価 |
| ---- | ---- |
| 概念の独立性 | ✓ Reactive と Periodic Tick を構造で区別 |
| Persistence 影響 | ✗ DrowZzzGameSession の新フィールド + `PersistedSessionV1` 更新(schemaVersion bump 検討) |
| 既存メカニズムの活用 | ✗ Influence 機構の発動 / 除去ロジックを ReactiveEffect で重複実装 |
| 「Influence と ReactiveEffect の違いがプレイヤーに見えるか」| ✗ ゲーム設計上は両者とも「影響」、内部区別は実装詳細 |

→ 既存 Influence の自然な拡張で十分、新概念導入は過剰、不採用。

### 不採用案 B: `ApplyPlayCard` / `ApplyAbandon` 冒頭で walk(本アクション処理前)

`ApplyPlayCard` の冒頭(SDP/Hand 変更前)で walk → 本カード処理前なので新規付与 influence は存在せず自然に対象外。

| 観点 | 評価 |
| ---- | ---- |
| 自己適用回避 | ✓ 構造的に保証 |
| セマンティクス | ✗ 「使用したら」を「使用前に」と解釈することになり、テキスト直訳と乖離 |
| 効果順序 | ✗ Reactive 効果が本カード効果の前に走る、SDP 値の解釈が曖昧化(例:Reactive SDP-10 → 本カード SDP-4 = 順序問題) |

→ 末尾 walk + snapshot のほうが意味論的に正確、不採用。

### 不採用案 C: カード固有処理として `ApplyPlayCard` / `ApplyAbandon` に手書き分岐

```csharp
if (action.Card.TypeId.Equals(CardTypeId.Of("12"))) {
    // No.12 専用の Reactive 効果適用
}
```

| 観点 | 評価 |
| ---- | ---- |
| 実装の簡潔性 | △ No.12 のみなら少ない、複数カードに広がると指数的に複雑化 |
| 拡張性 | ✗ 新規 Reactive カードごとに DrowZzzRule を直接編集 |
| カード設計の汎用性 | ✗ カードデータ(catalog asset)で表現できず、コード変更必須 |

→ scalability ゼロ、不採用。

### 不採用案 D: `InfluenceTrigger.OnAnyOwnAction` 汎用トリガー + EffectInterpreter で action 種別判定

1 つの新トリガーで複数アクションを表現、EffectInterpreter 内で `EffectContext.ActionType` 経由で分岐。

| 観点 | 評価 |
| ---- | ---- |
| enum 拡張サイズ | ✓ 1 値追加で済む |
| EffectContext 拡張 | ✗ `ActionType` フィールド追加が必要、既存 EffectContext.Default の意味論変更 |
| カード設計の明示性 | ✗ effect 側で「PlayCard か Abandon か」を判定するロジックを書く必要、カードデータの表現力低下 |
| EffectInterpreter の責務 | ✗ 「effect 適用」の責務に「アクション種別判定」が混入 |

→ 案 A(トリガー値で区別)のほうが effect の責務分離が明確、不採用。

## Related

- カード仕様: [`docs/specs/games/drowzzz/cards/false-sun.md`](../specs/games/drowzzz/cards/false-sun.md)(本 PR で同時起票、No.12「偽りの太陽」本体実装)
- ADR-0007 §1.5「継続影響(Influence)」: `PlayerInfluence` / `InfluenceTrigger` の基本機構、本 ADR で `InfluenceTrigger` のみ拡張
- ADR-0020「Influence の RemainingCount 減算タイミング」: `DecrementInfluencesForCurrentPlayer` の責務、Reactive Influence も対象に含まれる(全 Influences 一律 -1)
- 実装: `Assets/_Project/Scripts/Application/Games/DrowZzz/Influences/InfluenceTrigger.cs`(enum 拡張)+ `DrowZzzRule.cs`(`ApplyPlayCard` / `ApplyAbandon` の Reactive walk 経路 + `ApplyReactiveInfluencesAfter(session, snapshot, trigger)` 共通ヘルパー新設)+ 新規 effect 2 件(Application + SO Asset)
- EARS: 新規 No.12 EARS(`false-sun.md` / `.feature`)
- 関連カード:[No.02「緑の侵攻」](../specs/games/drowzzz/cards/green-invasion.md)(`AdjustSdpEffect(SdpTarget, int Delta)` の Delta 値プリケジ、本 ADR の `AdjustSdpAfterPlayCardEffect(int Delta)` / `AdjustSdpAfterAbandonEffect(int Delta)` と同パターン)
