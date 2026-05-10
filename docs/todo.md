# TODO

リポジトリ内で完結する小規模タスクの追跡リスト。意思決定 (ADR) や仕様 (EARS) の重さに達しないが、確実に着手すべき後追い chore・技術的負債の解消・既存規約との不整合修正・静的解析や Unity 計測で発覚した改善項目を一元化する。

運用ルール / テンプレート選定の根拠は [ADR-0003 TODO 運用](adr/0003-todo-operations.md) を参照。本ファイル冒頭は ADR の Decision を要約したもの。

---

## 運用ルール(要約)

### エントリテンプレート

```markdown
- [ ] **<Title (imperative form)>** `priority: high|medium|low`
  - **Why**: なぜ必要か、何が問題で、何を解決するか
  - **Done when**: 受け入れ条件(完了の判定基準。複数可)
  - **Related**: 関連 ADR / EARS / PR / コミット番号 / 関連ファイル
  - **Notes**: 補足、未確定事項、参考リンク (任意)
```

### 優先度

| priority | 意味 |
| ---- | ---- |
| `high` | 次の主要 PR 着手前に解消すべきブロッカー級 |
| `medium` | 数 PR 内に解消する目安、Phase の節目を超えない |
| `low` | 任意のタイミング、Phase の節目で再評価 |

### 入れる対象 / 入れない対象

| 入れる | 入れない |
| ---- | ---- |
| 機械的 chore(< 1 PR で完結) | 仕様変更(EARS で記述) |
| 後回し可能で発見時に手を止めるほどでない作業 | 設計判断(ADR で記述) |
| 既存規約 / 既存設計の物理反映 | 現在 PR で対応する TODO(PR description / commit message に書く) |
| code-reviewer 指摘のうち本 PR 範囲外 | 即時修正可能な typo / 1 行の軽微なリファクタ |
| Unity 計測 / 静的解析で発覚した未対応分岐 | 1 PR で完結しない大きな機能(Issue ベース or 新 ADR) |

### 状態管理 / 完了処理

- 状態は **未着手** / **進行中** / **完了済み** の 3 セクションで管理
- 着手時は「未着手」→「進行中」へ移動
- 完了時は「進行中」→「完了済み」へ移動し、`Related` に完了 PR / コミット番号を追記する(**削除しない**、振り返り・トレーサビリティのため)
- 完了済みエントリが 30 件を超えたら `docs/todo-archive.md` に切り出し

### コミット規約

`docs(todo): <日本語説明>`(Conventional Commits)。本筋 PR と同一 commit でも独立 commit でも可。

### 識別子規約

- 本名・新規連絡先・自分の handle literal を書かない
- Public 文書から Private リソースへのリンクを書かない

---

## 未着手

- [ ] **Pile に値同値性を追加する** `priority: medium`
  - **Why**: PR-2 で Hand に値同値性(`Equals` / `GetHashCode` / `operator==` / `!=`)を導入したが、既存 `Pile` は参照同値のまま残っている。Domain 集合型(`Pile` / `Hand` / `CardData`)を全て値同値で揃え、後続 PR(PR-4 GameState など)での比較を一貫させる
  - **Done when**:
    - `Pile` に `Equals(Pile)` / `Equals(object)` / `GetHashCode` / `operator==` / `operator!=` を順序依存シーケンス同値で override
    - `IEquatable<Pile>` 実装を追加
    - `PileTests` に対応テスト追加(同順序同要素 / 順序異 / カード異 / 枚数異 / 同一参照 / null / n=0 / Equals(object) null・異型 / operator== の両 null・片 null × 2)
    - `pile.md` に等値性関連の要件を追加(PILE-NNN 新規採番)
    - Domain C0 カバレッジ 100% を維持
    - `Pile.cs` の XML doc remarks に値同値性方針を追記
  - **Related**: [ADR-0002](adr/0002-phase1-domain-boundaries.md) §「Domain 集合型の値同値性方針」, PR #13 (Hand 値同値導入)
  - **Notes**: PR-3 (PlayerState) または PR-4 (GameState) 着手前までに完了させたい。`Pile` の Phase 0 実装は immutable なので追加のみで済み破壊的変更は不要

- [ ] **Roslynator.Analyzers の導入 or CLAUDE.md §7 訂正** `priority: low`
  - **Why**: CLAUDE.md §7「Roslyn Analyzer 構成」に `Roslynator.Analyzers` が公開 Analyzer として導入予定と記載されているが、現状 NuGetForUnity (`Assets/Packages/`) に未配置。ドキュメントと実態が乖離しており、新規参加者(将来の自分含む)が混乱する
  - **Done when**:
    - 以下のいずれか A / B を選び、結果がリポジトリに反映済み:
      - 選択 A: `Roslynator.Analyzers` を NuGetForUnity 経由で導入し、`.editorconfig` に severity 設定を追加
      - 選択 B: CLAUDE.md §7 / README の Analyzer 一覧から `Roslynator.Analyzers` を削除し、`Microsoft.Unity.Analyzers` + `Microsoft.CodeAnalysis.NetAnalyzers` のみに訂正
    - 設計判断レベル(導入する/しない)であれば ADR-0004 として記録。CLAUDE.md 訂正のみで済む選択 B なら同 PR に判断根拠コメントを残し ADR は不要
  - **Related**: [`CLAUDE.md`](../CLAUDE.md) §7「Roslyn Analyzer 構成」
  - **Notes**: Phase 1 中の任意のタイミングで判断。導入しない場合の Phase 1 後半までに訂正だけは入れたい

- [ ] **NRT (Nullable Reference Types) 有効化を検討する** `priority: low`
  - **Why**: PR-1 (CardData) で `CardData?` / `object?` のアノテーション 7 箇所に対し CS8632 警告が発生し、既存パターン(NRT 無効)に揃えて `?` を削除した経緯がある。Domain 全体で null 安全な API を表現したい場合、NRT 有効化が筋。判断は設計判断レベルになる可能性あり(ADR-0004 候補)
  - **Done when**:
    - NRT 有効化のコスト・利益を評価(全 Domain ファイルへの `?` アノテーション付与、Pile / CardId への影響、Unity 6 / 当該 Mono 版の互換性確認)
    - 採用する場合: `csc.rsp` または `.editorconfig` で NRT 有効化、Domain 全体に nullable annotation を導入、既存 NUnit テスト全 Green を維持、Domain C0 95%+ を維持、ADR で判断記録
    - 不採用の場合: ADR で「NRT を採らない理由」を記録(同じ問題を繰り返さないため)
  - **Related**: PR #12 (CardData) のレビュー過程で発生した CS8632 警告対応, [`CLAUDE.md`](../CLAUDE.md) §7
  - **Notes**: 必要性が高まった時点で再検討。Phase 2 以降で外部 API クライアント等の null 多発コードが入る前に判断したい

## 進行中

(着手中のエントリをここに移動する)

## 完了済み

(完了したエントリをここに移動し、`Related` に完了 PR / コミット番号を追記)
