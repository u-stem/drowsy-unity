# ADR-0003: TODO 運用と docs/todo.md の新設

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-10 |
| Decider | プロジェクトオーナー |

## Context

Phase 1 進行中、ADR / EARS / .feature の重さに達しないが将来確実に着手すべき小規模 chore タスクが複数発生した。

- ADR-0002 で「Pile に値同値性を後追いで追加する」と記録したが、起票忘れ防止の追跡手段がない
- 本セッションで「`Roslynator.Analyzers` が CLAUDE.md §7 に記載があるが NuGet パッケージは未配置」という不整合が発覚
- CardData PR-1 で発生した「NRT (Nullable Reference Types) 有効化の検討」も chore 候補として残った

これらは:
1. **設計判断 (ADR) ではない** — 既に決定済みの方針の物理反映、あるいは既存運用と CLAUDE.md の整合修正
2. **仕様変更 (EARS) でもない** — Domain ロジックや要件の変更を伴わない
3. **現在の PR で対応すべきものでもない** — 1 PR = 1 論理変更の規約に従い、本筋の PR スコープに含めるとレビュー粒度が崩れる

ADR-0001 で「GitHub Issue は流れて消える(close 後の検索性が低い)」「リポジトリと履歴が分離される」「オフラインで参照しにくい」と評価し、意思決定記録はリポジトリ内に置くと決めた。同じ評価軸を TODO 追跡に当てはめると、リポジトリ内ファイルでの追跡が一貫性がある。

## Decision

本プロジェクトでは TODO を以下の規約で運用する。詳細は [`docs/todo.md`](../../todo.md) の冒頭「運用ルール」セクションに集約する。

### 全体構造

| 項目 | 値 |
| ---- | ---- |
| 配置 | `docs/todo.md`(単一ファイル) |
| アーカイブ | 完了済みエントリ 30 件超で `docs/todo-archive.md` に切り出し |
| 状態管理 | 未着手 / 進行中 / 完了済み の 3 セクションを単一ファイル内に併設 |
| 完了処理 | エントリを「完了済み」セクションへ移動し、完了 PR / コミット番号を `Related` に追記する(**削除しない**、振り返り・トレーサビリティのため) |

### TODO エントリ テンプレート

```markdown
- [ ] **<Title (imperative form, "〜する")>** `priority: high|medium|low`
  - **Why**: なぜ必要か、何が問題で、何を解決するか
  - **Done when**: 受け入れ条件(完了の判定基準。複数可、箇条書きで)
  - **Related**: 関連 ADR / EARS / PR / コミット番号 / 関連ファイル
  - **Notes**: 補足、未確定事項、参考リンク (任意)
```

優先度の意味:

| priority | 意味 |
| ---- | ---- |
| `high` | 次の主要 PR 着手前に解消すべきブロッカー級 |
| `medium` | 数 PR 内に解消する目安、Phase の節目を超えない |
| `low` | 任意のタイミング、Phase の節目で再評価 |

### 入れる対象 / 入れない対象

| 入れる(対象) | 入れない(対象外) |
| ---- | ---- |
| 機械的 chore(< 1 PR で完結) | 仕様変更(EARS で記述) |
| 後回し可能で発見時に手を止めるほどでない作業 | 設計判断(ADR で記述) |
| 既存規約 / 既存設計の物理反映 | 現在 PR で対応する TODO(PR description / commit message に書く) |
| code-reviewer 指摘のうち本 PR 範囲外なもの | 即時修正可能な typo / 1 行の軽微なリファクタ(直接対応) |
| Unity 計測 / 静的解析で発覚した未対応分岐 | 1 PR で完結しない大きな機能(Issue ベース or 新 ADR) |

### コミット規約

- TODO 追加 / 編集 / 完了マークは `docs(todo): <日本語説明>`(Conventional Commits)
- TODO の追加は本筋 PR と同一 commit でも、独立 commit でも可(粒度はレビュー時間とのバランスで判断)

### 識別子規約 / Public/Private 境界

ADR-0001 / 0002 と同様、`docs/todo.md` および本 ADR-0003 で:
- 本名・新規連絡先・自分の handle literal を書かない
- Public 文書から Private リソース(URL / git remote)へのリンクを書かない

## Consequences

### Positive

- ADR で予約した「後追い PR」がリポジトリ内で機械的に追跡できる(`grep -E "^\s*- \[ \]" docs/todo.md` で未着手エントリ行を一覧、Markdown 行頭マッチで Consequences 等の説明文を誤検出しない)
- オフラインで参照可能、git 履歴で版管理される
- ADR / 仕様 / TODO がすべてリポジトリ内で完結し、Public/Private 境界が一貫
- 完了処理で「削除せず移動」を採用するため振り返り・PR 紐付けの追跡性が保たれる
- ADR の重さを必要としない小タスクの一元化により、CLAUDE.md / ADR が肥大化しない
- 個人開発で複数プロダクト並行運用するなか、休止後の自分が文脈を即座に復元できる

### Negative

- GitHub Issue UI のラベル / マイルストーン / アサイン機能を使えない(本プロジェクトはプロジェクトオーナー単独運用なので影響軽微)
- 完了処理は手動(チェックボックス + セクション移動)、close の自動化はなし
- `docs/todo.md` のエントリ数が増えると視認性が低下する → 30 件アーカイブルールで緩和
- TODO の発生源が散逸しやすい(ADR Related / code-reviewer 指摘 / セッション中の気付き)→ 「発生時に必ず追記する」を運用ルールで明示

### Neutral

- 本 ADR と `docs/todo.md` を 1 PR でセット起票することで、運用判断と実体ファイルの整合性が即座に取れる
- 将来 GitHub Issue の必要性が高まれば、ADR-0003 を `Superseded by NNNN` で更新する余地がある(本 ADR は「現時点での判断」と位置付ける)

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| GitHub Issue で追跡 | ADR-0001 の評価軸(流れて消える / リポジトリ分離 / オフライン参照不可)が同じく当てはまる。本プロジェクトはオーナー単独運用で UI / アサイン機能の必要性も低い |
| ADR-0002 の追記のみで完結(他に追跡手段を作らない) | 1 件だけならシンプルだが、Roslynator / NRT 検討など複数の TODO 候補が既に存在し、追跡先の集約場所がないと忘却リスクが高い |
| ADR-0003 を作らず `docs/todo.md` 単体で運用ルールを記述 | 軽量だが、運用判断(なぜ Issue ではなくファイル管理か / なぜこのテンプレートか)の根拠が版管理から欠落し、将来の方針変更時に経緯不明となる。ADR-0001 / 0002 と対称構造を取る方が一貫性が高い |
| 単純 checkbox `- [ ] X` のみのテンプレート | 軽量だが情報量不足。3 ヶ月後の自分が文脈を失う。`Why` / `Done when` 欠落で「やるべきか / 終わったか」の判断が曖昧になる |
| Issue 風(Title / Body / Acceptance Criteria) | 構造化されすぎる。ADR の重さに近づき、TODO の軽量さが失われる |
| User Story 形式(As a / I want / So that) | 個人開発・Domain TODO に対して過剰。機能要件向けの形式 |
| `due: before-PR-N` トリガと `area: domain|infra|docs|meta` タグの併記 | TODO 件数が少ない間は過剰。30 件超に達した時点で再検討する余地はある |
| `time: 30m` などの見積もり | 個人開発で見積もりの精度を担保するメリットが薄く、入力コストに見合わない |
| 採番(`TODO-001` 等)で機械検証可能化 | TODO は流動性が高く採番が破綻しやすい。ADR(不変判断)とは性質が異なる |
| GitHub Projects v2(Kanban / ロードマップ UI) | リポジトリ外サービス依存・オフライン参照不可 という ADR-0001 / 本 ADR の評価軸が同じく当てはまる。ボード UI の利点も個人開発・3 状態管理(未着手/進行中/完了済み)では薄い |
| GitHub Milestones | Issue を起票する前提となる集約手段。本 ADR は Issue を不採用としたため Milestones も同時に不採用 |

## Implementation Notes

本 ADR を起票する PR で同時に行う初回作業(以後は ADR 本文を更新するだけで運用変更が完結する):

- `docs/todo.md` を新設し、冒頭に本 ADR の Decision を要約した「運用ルール」セクションを置く(詳細根拠は本 ADR を参照、二重管理を避ける一方向フロー)
- 初期 3 エントリを記録: Pile 値同値性追加(ADR-0002 由来) / Roslynator.Analyzers 不整合 / NRT 有効化検討
- CLAUDE.md 末尾に「12. TODO 追跡」セクションを新設し、本 ADR / `docs/todo.md` への参照リンクと最低限のキーワードのみを記載(入れる対象・入れない対象の詳細表は本 ADR / `docs/todo.md` 側に集約し三重管理を避ける)

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md) — TODO 追跡をリポジトリ内に置く判断軸を継承
- 関連: [ADR-0002 Phase 1 Domain 拡張](0002-phase1-domain-boundaries.md) §「Domain 集合型の値同値性方針」 — 初期 TODO エントリの根拠の 1 つ
- 実体: [`docs/todo.md`](../../todo.md) — 運用ルール要約とエントリ集
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §11(ADR 運用)/ §12(TODO 追跡、本 PR で新設)
