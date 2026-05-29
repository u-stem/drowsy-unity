# CLAUDE.md (drowsy-unity)

プロジェクト固有の開発規約。グローバルのCLAUDE.mdと矛盾する箇所は本ファイルが優先される。

**本ファイルの役割**: 不変の規約と `docs/` への地図のみを置き、変動する状態は持たない。
各章の詳細は `docs/` 側をSingle Source of Truthとする。

- プロジェクト概要・バージョン・セットアップ: [`README.md`](README.md)
- Phase進捗: [`docs/roadmap.md`](docs/roadmap.md)
- 意思決定記録: [`docs/adr/README.md`](docs/adr/README.md)

---

## 1. 言語規約

| 対象 | 言語 |
| ---- | ---- |
| コード内コメント / docstring / XML doc | 日本語 |
| コミットメッセージ本文 / PR / Issue 本文 | 日本語 |
| README / 設計ドキュメント | 日本語 |
| コード識別子(クラス・メソッド・変数名) | 英語 |
| コミットメッセージの type(feat / fix / docs / refactor / test / chore / build / ci) | 英語 |
| 設定ファイルのキー・値 | 英語 |

例: `// 山札からカードを 1 枚引いて手札に加える`

**コメントに書かないこと(ADR-0026)**:

- コード内コメント(`//` / `/* */` / `///`)に ADR 番号や設計経緯を書かない
- コメントは技術的な「なぜ / 何を / どうやって」に絞る
- 設計経緯の SSoT は ADR 本体。コード ↔ ADR の紐付けは git blame / commit message / PR で辿る
- コミットメッセージへの ADR 番号付与は従来どおり推奨(本ポリシーの対象外)

## 2. アーキテクチャ規約

詳細: [`docs/architecture/dependency-rules.md`](docs/architecture/dependency-rules.md)

- `Assets/_Project/Scripts/` は Clean Architecture 寄りの 4 層 + Bootstrap 構成
- 依存方向: `Bootstrap → {Infrastructure, Presentation} → Application → Domain`(逆方向は不可)
- Domain は純粋 C#(`noEngineReferences: true`)で UnityEngine 非依存
- Infrastructure / Presentation → Application はインターフェース経由(Ports & Adapters)。具象 UseCase を直接呼ばない
- View に Domain エンティティを直接バインドせず Presenter / DTO 経由
- DI / 非同期 / Reactive は VContainer + UniTask + R3 を採用(MessagePipe は WebGL/IL2CPP の Open Generics 制約 + R3 で代替可のため不採用)
- Unity Cloud Services は利用しない(`organizationId` / `cloudProjectId` は空欄維持。自動再リンクされたら速やかに Unlink)
- 外部テンプレ由来ファイル(`.gitignore` / `.gitattributes`)は出典コメント・テンプレ本体を改変しない(sync 競合回避。固有追加分のみ日本語コメント)

## 3. テスト規約

詳細: [`docs/testing-strategy.md`](docs/testing-strategy.md)

- 仕様駆動: EARS Markdown + Gherkin `.feature` を `docs/specs/<layer>/<module>/` に記述
- TDD: Red → Green → Refactor。バグ修正は再現テストから書き始める
- NUnit。メソッド名 `Given_X_When_Y_Then_Z`、AAA パターン、1 テスト 1 アサーション
- 全テストに Category 2 軸必須: Size(Small / Medium / Large)+ Type(Normal / SemiNormal / Abnormal / SuperNormal)
- カバレッジ目標: Domain C0 95%+ / Application 80% / Infrastructure 60% / Presentation は手動 QA
- トレーサビリティ: EARS 要件 `[<MODULE>-<NUMBER>]` ↔ NUnit `[Property("Requirement", "<ID>")]`(lefthook で機械検証)

## 4. 機械検知

詳細: [`docs/machine-detection.md`](docs/machine-detection.md) + [`lefthook.yml`](lefthook.yml)

- IDE → 保存 → pre-commit → commit-msg → CI → branch protection の多層防御
- pre-commit(lefthook): gitleaks / dotnet format / dotnet build / カスタムスクリプト群を並列実行
- Roslyn Analyzer: NetAnalyzers + Microsoft.Unity.Analyzers + Roslynator(ADR-0013)
- `--no-verify` はユーザーグローバル設定で物理 deny
- CI 未整備のため、現状は **lefthook の pre-commit を必ず通す**ことがローカル責任

## 5. ワークフロー

詳細: [`docs/workflow.md`](docs/workflow.md)

- PR ベース。main 直接 push は branch protection で物理ブロック
- ブランチ命名: `feature/` / `fix/` / `chore/` / `docs/` / `refactor/` / `test/`
- 1 PR = 1 論理的変更(複数関心事をまとめない)。小さく頻繁に作る
- Conventional Commits: `<type>: <日本語説明>`(type は英語)
- PR description は [`.github/pull_request_template.md`](.github/pull_request_template.md) に従う
- Self-Review: `code-reviewer` subagent または `/ultrareview`(GitHub 仕様で自分の PR に Approve 不可)

## 6. 定数管理

詳細: [`docs/architecture/constants-management.md`](docs/architecture/constants-management.md)

- L1 / L2(数学的・ドメイン上の不変量)→ Domain `<Module>Constants` の `const`
- L3(ゲームバランス調整値)→ `IGameConfig` interface + ScriptableObject
- L4(ユーザー設定)→ `IUserSettings` + PlayerPrefs
- L5(環境固有値)→ ビルド設定 / `csc.rsp`
- マジックナンバー禁止(自明な `0` / `1` / `-1` / `""` / `null` を除く)。`.editorconfig` で CA1802 を warning 化
- 各機能の EARS Markdown 末尾に「定数依存」セクションを設ける

## 7. ADR / TODO 運用

ADR 一覧: [`docs/adr/README.md`](docs/adr/README.md) / TODO: [`docs/todo.md`](docs/todo.md)(運用は ADR-0003)

ADR を起票する判断: アーキテクチャ判断 / 機械的ガードレールの新設・撤去 / 既存設計を覆す変更 / 将来「なぜこうしなかったか」を記憶で答えられない判断。起票しない: バグ修正・typo・1 ファイル内リファクタ・パッケージのマイナー更新・軽微な文言調整。

- 配置: `docs/adr/NNNN-kebab-case-title.md`(4 桁ゼロ埋め連番、欠番禁止)
- 必須セクション: Context / Decision / Consequences / Related
- Status 語彙: Proposed / Accepted / Rejected / Withdrawn / Deprecated / Superseded by NNNN(起票時は原則 Accepted)
- コミット規約: `docs(adr): <日本語説明>` / `docs(todo): <日本語説明>`
- 索引・コメントの簡潔性は ADR-0026 を遵守(索引は番号 + タイトル + Status のみ)
- Decider 欄に本名・連絡先・handle literal を書かない。Public 文書から Private リソースへリンクしない

## 8. Claude(本セッション)の振る舞い規範

- ステージング後に `code-reviewer` subagent でレビュー → 指摘反映 → commit
- 機密 grep / lefthook 動作確認を行う
- commit 後は push、PR 作成、Self-Review チェックリスト記入まで一貫して支援
- main への直接 push を試みない(branch protection 有効化前でも feature ブランチ経由)

## 9. グローバル規約の継承

ユーザーグローバル CLAUDE.md (`~/.claude/CLAUDE.md`) の以下を本プロジェクトでも適用する。

- 完了時の必須報告フォーマット
- 出力衛生(個人情報・Public/Private 境界)
- 禁止事項(機密コミット禁止 / `*.backup-*` 書き込み禁止 / `--no-verify` 禁止 等)
- メモリ運用(auto memory / episodic-memory の 2 層)
- TDD ループと作業フロー
- パッケージ管理優先順位(JS/TS は bun、Python は uv)
