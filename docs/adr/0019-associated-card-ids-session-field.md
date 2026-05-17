# ADR-0019: DrowZzzGameSession に AssociatedCardIds フィールド追加 — No.04「静寂を纏う」着手前の設計基盤

- Status: Accepted
- Date: 2026-05-17
- Decider: -

---

## Context

カード No.04「静寂を纏う」(オーナー JIT 確定 2026-05-17)が以下の仕様を持つ:

- 自分(actor)が **相手の手札(乙の思考)から 1 枚を選択** し、相手にその CardId を使用禁止にする継続影響を付与する
- ただし **「連想した手段は選択不可」**(連想は「夢」など特殊な方法で手札に加えられた CardId を指す)

これを実装するには **「連想で手札に加えられた CardId」と「通常 Draw で手札に加えられた CardId」を区別** する手段が必要だが、現状の `DrowZzzGameSession` / `Hand` / `Pile` / `PlayerInfluence` には連想由来かどうかの記録が **一切存在しない**。

`DrowZzzRule.ApplyAssociate`(`Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs:422` 周辺)は `Hand.Add(action.Card)` で連想 CardId を手札に追加するのみで、「連想由来」の情報は session に痕跡を残さない設計だった(ADR-0011 §1 連想 = 特殊 Draw、当時は「連想由来」を区別する要件がなかった)。

## Decision

`DrowZzzGameSession` に **`AssociatedCardIds: IReadOnlySet<CardId>`** フィールドを追加し、**`ApplyAssociate` 時に連想 CardId を本集合に永続記録** する。

### 1. フィールド仕様

| 項目 | 値 |
| ---- | ---- |
| 名前 | `AssociatedCardIds` |
| 公開型 | `IReadOnlyCollection<CardId>`(内部 `HashSet<CardId>`、防御コピー、`Pile` / `Hand` と同じ array/dict ベース防御コピー方針) |
| 補助 API | `bool IsAssociated(CardId)`(内部 `HashSet` 経由で O(1) 検索、LINQ Contains の O(n) を回避) |
| 初期値 | 空集合(`new HashSet<CardId>()`) |
| ctor 引数位置 | **末尾、optional**(`IReadOnlyCollection<CardId> associatedCardIds = null` → null は空集合へ復元) |
| 不変条件 | null 不可(ctor 内で `?? new HashSet<CardId>()` で正規化、空集合は許容) |

**型選定の補足**:`IReadOnlySet<T>` は .NET 5+ 専用で `netstandard2.1`(Unity 6 ターゲット)では未提供のため、公開型は `IReadOnlyCollection<CardId>` + `IsAssociated(CardId)` メソッドの組み合わせで「集合の不変公開 + O(1) membership check」を表現する(2026-05-17 PR ① 実装時に確定)。

### 2. ライフサイクル: **永続記録**

- `ApplyAssociate` で **Add のみ**、**Remove なし**
- 連想された CardId は **手札から Discard / Field に移動しても集合に残る**
- 同じ CardId を 2 回連想(理論上ありえない、`AssociateAction` 時点で Hand 重複防御済 ADR-0018)しても **HashSet 性質で冪等**

### 3. 後方互換性(Persistence)

- `PersistedSessionV1` に `AssociatedCardIds: List<CardId>` フィールド追加
- **schemaVersion は 1 維持**(bump しない)
- **Newtonsoft.Json の `NullValueHandling.Ignore` + nullable** で、旧 v1 JSON(`AssociatedCardIds` フィールドなし)読み込み時に null → 空 set として復元
- **`DrowZzzGameSessionSerializer.LoadAsync` で空集合 fallback** を明示し、後方互換性を構造的に保証
- **新規 session の空集合は JSON 上 `"AssociatedCardIds":[]` として serialize される**(`NullValueHandling.Ignore` は null だけ対象、空 list はそのまま出力)。これにより旧 v1 JSON(フィールド欠落)と新 v1 JSON(空集合 = `[]`)で diff が生まれるが、機能的差異はなし(両者とも `ToDomain` 時に空集合へ復元)。INF-135(メモリ内 null)+ INF-136(JSON 文字列フィールド欠落)の 2 経路で後方互換性をカバー(code-reviewer W-2 反映 2026-05-17)

### 4. No.04 実装(PR ②)との関係

本 ADR は **PR ① 設計基盤** のみを記録する。No.04「静寂を纏う」本体の実装(新規 effect record / `PlayCardAction.TargetCardId` 拡張 / `IsLegalPlayCard` で AssociatedCardIds 除外チェック / SO catalog 登録 / EARS)は **PR ② で別途実装** し、PR ① 完成後に独立 ADR / 直接実装で進める。

PR ① 単独では `AssociatedCardIds` は記録されるだけで利用されない(no consumer)。これは意図的:設計基盤(session スキーマ変更)と消費者実装(No.04)を分離することで、PR レビュー粒度を保ち、リグレッションリスクを最小化する。

## Consequences

### Positive

- No.04「静寂を纏う」の仕様(連想由来除外)を honor できる
- Newtonsoft.Json の nullable + NullValueHandling.Ignore で旧 v1 JSON の後方互換性を schema bump なしで担保
- 「連想由来」概念を session に明示することで、将来同様の要件(連想由来カードに特殊ボーナス etc.)を持つカード設計に対応可能
- 永続記録 lifecycle は最もシンプル(Add のみ、Remove ロジック不要)で、AbandonAction / PlayCardAction での手札操作と独立して維持できる

### Negative

- `DrowZzzGameSession` の constructor 引数が +1 拡張(末尾 optional でほぼ無痕跡だが、Persistence 経路と直接 ctor 呼び出しテストには影響)
- 内部状態 1 つ増えるため `auto-equals` / `WithUnchecked` / `ValidateAndCopy*` 系の更新が必要
- 60+ Session 系テストの間接影響(SessionFactory 経由なら無痕跡、直接 ctor 呼び出しテストは数件影響)
- 「連想由来」の意味が永続記録 = 「現在手札にある連想由来」とずれる(現手札に連想由来カードがあるか?を判定するには Hand × AssociatedCardIds の積集合計算が必要)。現状の No.04 仕様(「連想由来は選択不可」)は永続記録セマンティクスと整合(過去に連想された CardId は永久に「連想由来」属性を持つ)

### Neutral

- 本 ADR で session スキーマを変更するが、No.04 実装まで AssociatedCardIds に consumer が存在しない(PR ① 単独では「空 set を渡し続ける」状態)。本 ADR は **「PR ② No.04 実装に必須の前提条件を先行整備」** という意図的な段階的設計であり、レビュー粒度のための分割

### 補足:本 PR で意図的に見送った最適化

**`ApplyAssociate` 内の `with { AssociatedCardIds = newSet }` 経路で防御コピーが二重に走る** 件(`new HashSet<CardId>` で 1 回 + init setter 内 `ValidateAndCopyAssociatedCardIds` で 1 回)を、GameState の `WithPlayersUnchecked`(post-Phase2 アルゴリズム最適化レビュー Top-2)と同様の internal unchecked path で解消することを **PR ① では見送る** 判断(code-reviewer W-1 反映 2026-05-17)。

理由:
- PR ① 単独では AssociatedCardIds に consumer がなく、`ApplyAssociate` 呼び出しはテスト経路 + 既存連想ゲームプレイのみ(発火頻度は低い)
- internal unchecked ctor / `WithAssociatedCardIdsUnchecked` 追加は DrowZzzGameSession のインフラ拡張で本 PR 範囲を超える
- GameState `WithPlayersUnchecked` と完全整合させる作業は別 chore PR(post-Phase2 アルゴリズム最適化レビュー第 N 弾)で一括対処するのが筋
- 性能インパクトはゲーム規模では実質無視できる(1 連想 = 数 ms オーダーの追加、ターンあたり 0-1 回発火)

将来対応:`docs/todo.md` の post-Phase2 アルゴリズム最適化系 TODO の文脈で扱う(本 ADR では新規 TODO エントリは起こさず、本補足が記録の SoT)。

### 補足:N>2 拡張時の TargetCardId 解決(PR ② code-reviewer W-1 反映)

PR ② 追加の `DrowZzzRule.IsLegalPlayCard` 拡張は **N=2 前提**(相手プレイヤー = current 以外の唯一の Player)で `action.TargetCardId` が相手 Hand に含まれるかを判定する。

N>2 拡張時の論点:
- `TargetCardId` が複数プレイヤーの手札にまたがる可能性 → どのプレイヤーの手札を「target」とみなすか?
- カード側で `Target: SdpTarget` の意味を「対象プレイヤー」に拡張するか(`SdpTarget` enum 自体が `Self` / `Opponent` の 2 値で N>2 を表現できない)
- `PlayCardAction` に `TargetPlayerId: PlayerId?` の追加が必要になる可能性

本件は Phase 3 ロードマップ ADR(候補 ADR-0020+)で N>2 拡張全体と合わせて再評価する(`docs/todo.md` 肥大化回避方針で本 ADR §補足に集約)。

### 補足:null 入力許容の意図と注意点

`DrowZzzGameSession` ctor / init setter の `AssociatedCardIds = null` は `ValidateAndCopyAssociatedCardIds` 内で **silent に空集合へ正規化** される(`ArgumentNullException` を投げない)。これは ctor 末尾 optional default null + Persistence 経路の null fallback と整合させた意図的設計(code-reviewer W-3 反映 2026-05-17)。

注意点:
- 他の防御コピーヘルパー(`ValidateAndCopyPendingCounteredEffects` / `ValidateAndCopyInfluences` 等)は null を `ArgumentNullException` でフェイルファストするため、本フィールドのみ非対称設計
- PR ② 以降で直接 ctor 呼び出しが増えた際、null を誤って渡すと「期待した CardId 集合が無視されて空集合になる」サイレントバグの温床になりうる
- 検出経路:既存 EARS DZ-257 が `IsAssociated(null)` を `ArgumentNullException` で防御するため、空集合化された session で「実は連想が記録されていなかった」バグはテストで間接的に検出される(空集合への lookup は false を返すため、誤って連想スキップしたケースが期待値違反として浮上する)

## Alternatives Considered

### 不採用案 A: `Hand` 自体に「連想由来」フラグを持たせる

`Hand` 内の `CardId` リストを `(CardId, bool IsFromAssociation)` のタプルに拡張。

| 観点 | 評価 |
| ---- | ---- |
| 「現在手札にある連想由来」セマンティクスとの整合 | ✓ 直感的(手札から消えたら自動消滅) |
| Hand の汎用性破壊 | ✗ Hand は他ゲームでも使う Domain 型(ADR-0002 / Phase 1)、DrowZzz 固有概念を Domain に混入させる |
| Persistence 影響 | ✗ Hand の JSON schema 変更(HandJsonConverter 全書き直し)、後方互換性検討も別途必要 |
| ADR-0018 (CardId 再定義)直後の Hand 構造変更 | ✗ M5 完結直後の安定化期に Domain 再変更はリスク高 |
| 多重連想時の挙動 | ✗ 同じ CardId を 2 回連想したら 2 つの bool フラグが生まれて混乱 |

→ Domain 純粋性 + Persistence 影響規模 + Hand 汎用性破壊で不採用。

### 不採用案 B: `AssociatedCardIds` を `DrowZzzAssociationConstants` 等の静的領域に持つ

session ではなく static 状態として保持。

| 観点 | 評価 |
| ---- | ---- |
| マルチセッション安全性 | ✗ 致命的(同プロセス内で 2 ゲーム並行不可能) |
| Persistence | ✗ static 状態は session JSON 経路から外れる、save/load で失われる |
| Pure function 設計 | ✗ `DrowZzzRule.Apply` が static 状態に書き込むのは Immutability 方針(CLAUDE.md §5)違反 |

→ アーキテクチャ原則違反で不採用。

### 不採用案 C: PR ① + PR ② を 1 PR で同時実装

| 観点 | 評価 |
| ---- | ---- |
| PR レビュー粒度 | ✗ 1500-2000 行追加、レビュー負荷高 |
| リグレッションリスク | ✗ session スキーマ変更と新規 effect 群を同時投入、問題切り分け困難 |
| ADR 起票タイミング | ✓ 1 ADR で完結する利点はある |

→ レビュー粒度を優先して 2 PR 分割(本 ADR は PR ① のみカバー、PR ② は別 ADR or 直接実装)。

## Related

- カード仕様: [`docs/specs/games/drowzzz/cards/sound-of-silence.md`](../specs/games/drowzzz/cards/sound-of-silence.md)(PR ② で起票済、No.04「静寂を纏う」本体実装。PR ① merge 時点では未起票で broken link 状態だったが PR ② で解消)
- 連想機構: [ADR-0011 §1](0011-m3-dream-card-and-game-mechanics-expansion.md)、`Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs:422` `ApplyAssociate`
- 永続化: [ADR-0012 §M4-PR5](0012-m4-scriptableobject-and-persistence.md)、`Assets/_Project/Scripts/Infrastructure/Persistence/Models/PersistedSessionV1.cs`
- CardId 設計: [ADR-0018](0018-cardtypeid-cardid-instance-separation.md)
- session スキーマ:`Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs`
- EARS: DZ-255 / DZ-256 / DZ-257 / DZ-258(`docs/specs/games/drowzzz/association.md`)、INF-134 / INF-135 / INF-136(`docs/specs/infrastructure/persistence/persisted-session-v1.md`)
