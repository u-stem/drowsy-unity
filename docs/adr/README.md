# Architecture Decision Records (ADR)

本プロジェクトの主要な設計判断を、後から経緯を辿れる形で記録する。
「3 年後の自分に聞かれて答えに窮する判断」を残すことが目的。

---

## いつ ADR を起票するか

- アーキテクチャ全体に波及する設計判断(集約境界、レイヤ責務、Immutability ポリシー など)
- 機械的ガードレール(hooks / lefthook / CI / Roslyn Analyzer / asmdef 設定)を新設・撤去するとき
- 既存設計を覆す変更
- 「なぜこうしなかったのか」を将来問われたときに記憶では答えられないと予想されるとき

逆に以下では起票しない:
- 単純なバグ修正・typo 直し
- 1 ファイル内で完結するリファクタ
- パッケージのマイナー更新(破壊的変更を伴わないもの)
- 軽微な文言調整

## ファイル名規則

```
docs/adr/NNNN-kebab-case-title.md
```

- `NNNN`: 4 桁ゼロ埋めの連番(`0001`, `0002`, ...)
- 欠番禁止。撤回しても番号は残す
- 英小文字・ハイフン区切りの kebab-case

## Status 語彙

| Status | 意味 |
| ---- | ---- |
| `Proposed` | 議論中。Decision が暫定または未確定 |
| `Accepted` | 採用済み。現在有効な決定 |
| `Rejected` | 不採用。検討したが採らなかった案を記録として残す |
| `Withdrawn` | 起票者が取り下げた |
| `Deprecated` | 役割を終えた(後続 ADR で置き換えなし) |
| `Superseded by NNNN` | 後続 ADR で置き換えられた |

新規 ADR は基本 `Accepted` で起票する。議論を残したい場合のみ `Proposed` を使い、合意後に `Accepted` へ書き換える。

## 必須セクション

すべての ADR は以下 4 セクションを必ず埋める。

1. `Context` — なぜこの判断が必要になったか(背景・制約・代替案の事情)
2. `Decision` — 何を決めたか(曖昧さなく記述。条件分岐があるなら表で示す)
3. `Consequences` — 結果として生じる影響を Positive / Negative / Neutral の 3 区分で
4. `Related` — 関連 ADR 番号、関連 Phase / コミット / ファイル、関連ドキュメント

任意セクション(複雑な決定で推奨):
- `Alternatives Considered` — 検討した他案を 1〜2 行ずつ
- `Implementation Notes` — 実装時の注意事項

## 起票手順

1. 直近 ADR 番号 + 1 を採番(`ls docs/adr/[0-9]*.md | tail -1` で確認)
2. `NNNN-kebab-case-title.md` を作成、必須セクション + Status を埋める
3. 本 README のインデックス表に `| [NNNN](NNNN-kebab-case-title.md) | タイトル | Status |` 形式で 1 行追加(概要列は持たない、ADR-0026)
4. 既存 ADR を覆す場合は旧 ADR の Status を `Superseded by NNNN` に更新
5. feature ブランチで commit、PR 作成(`docs(adr): ...` 形式)
6. PR description に判断の要旨と影響範囲を記載
7. Self-Review(`code-reviewer` subagent または `/ultrareview`)を実施し、結果を PR コメントに添付してマージ

## 個人情報・Public/Private 境界

- Decider 欄に本名・新規連絡先・自分の handle literal を書かない(`プロジェクトオーナー` のような抽象語を使う)
- Public 文書から Private リソース(URL / git remote)へのリンクを書かない
- 旧設計に言及するときは「Private リソースで参照不能」と明記する

## インデックス

| # | タイトル | Status |
| ---- | ---- | ---- |
| [0001](0001-adr-operations.md) | ADR Operations | Accepted |
| [0002](0002-phase1-domain-boundaries.md) | Phase 1 Domain 拡張の集約境界と概念モデル | Accepted |
| [0003](0003-todo-operations.md) | TODO 運用と docs/todo.md の新設 | Accepted |
| [0004](0004-init-setter-polyfill.md) | C# 9 init setter / record with 式のための IsExternalInit polyfill 採用 | Accepted |
| [0005](0005-phase2-roadmap-drowzzz.md) | Phase 2 Roadmap — DrowZzz の段階的縦串実装 | Accepted |
| [0006](0006-m1-detail-application-interfaces.md) | M1 詳細 — 汎用 Application 層 interface と DrowZzz 最小実装の設計 | Accepted |
| [0007](0007-m2-detail-card-effects.md) | M2 詳細 — カード効果メカニズム(`IEffect` + `EffectInterpreter`)とサブセット先行スコープ | Accepted |
| [0008](0008-m2-drowzzz-clock-and-night-morning.md) | M2 — DrowZzzClock 概念と「夜・朝」フェーズの導入 | Accepted |
| [0009](0009-m2-m3-dp-and-victory-conditions.md) | M2-M3 — DP 機構(FDP / DDP / SDP)と持ち点・勝利条件 | Accepted |
| [0010](0010-m3-game-termination-and-victory-determination.md) | M3 詳細 — ゲーム終了判定と勝者決定 | Accepted |
| [0011](0011-m3-dream-card-and-game-mechanics-expansion.md) | M3 詳細(拡張) — 「夢」カード + ゲームメカニクス拡張 | Accepted |
| [0012](0012-m4-scriptableobject-and-persistence.md) | M4 詳細 — ScriptableObject 化 + 永続化 + ユーザー設定 | Accepted |
| [0013](0013-roslynator-adoption.md) | Roslynator.Analyzers の導入(機械検知レイヤ拡張) | Accepted |
| [0014](0014-start-game-usecase-cardcatalog-removal.md) | `StartGameUseCase` から `ICardCatalog<IEffect>` 依存を削除する | Accepted |
| [0015](0015-nullable-reference-types-not-adopting.md) | Nullable Reference Types (NRT) を採らない | Accepted |
| [0016](0016-m5-bootstrap-presentation.md) | M5 詳細 — Bootstrap / Presentation 統合(VContainer + UniTask + R3) | Accepted |
| [0017](0017-player-roster-vcontainer-collection-workaround.md) | PlayerRoster wrapper 型導入 — VContainer collection resolution と IReadOnlyList<T> 予約型問題 | Accepted |
| [0018](0018-cardtypeid-cardid-instance-separation.md) | CardTypeId と CardId(instance)の分離 — Hand 重複検出問題の根本対処 | Accepted |
| [0019](0019-associated-card-ids-session-field.md) | DrowZzzGameSession に AssociatedCardIds フィールド追加 — No.04「静寂を纏う」着手前の設計基盤 | Accepted |
| [0020](0020-influence-count-decrement-timing.md) | Influence の RemainingCount 減算タイミングを EndTurn へ移行 — カウント1 Marker 機能化 | Accepted |
| [0021](0021-endturn-stuck-escape-valve.md) | EndTurnAction の全フェーズ合法化条件 — stuck 化 Marker 保有時の脱出弁 | Accepted |
| [0022](0022-reactive-influence-trigger-extension.md) | InfluenceTrigger 拡張 — Reactive Influence(アクション後発動型)の追加 | Accepted |
| [0023](0023-echo-keyword-and-reuse-influence-source.md) | Echo キーワード + PlayerInfluence.OriginEffects + ReuseInfluenceSourceEffect の導入 | Accepted |
| [0024](0024-associate-to-first-player-on-game-start.md) | AssociateToFirstPlayerOnGameStart marker + StartGameUseCase の ICardCatalog 再依存 | Accepted |
| [0025](0025-play-or-abandon-branch-effect.md) | PlayOrAbandonBranchEffect 導入 — カード固有の放棄効果機構 | Accepted |
| [0026](0026-documentation-and-comment-conciseness-policy.md) | ドキュメント・コメント簡潔性ポリシー | Accepted |
| [0027](0027-claude-md-role-redefinition.md) | CLAUDE.md の役割再定義(規約 SSoT・状態を docs へ分離) | Accepted |
