# CLAUDE.md (drowsy-unity)

このファイルは drowsy-unity プロジェクト固有の Claude / 開発規約を定義する。
ユーザーグローバル CLAUDE.md (`~/.claude/CLAUDE.md`) と矛盾する箇所は本ファイルが優先される。

---

## 1. コメント・ドキュメントの言語

ユーザーグローバル規約は「コメント・コミットメッセージは英語」を採用しているが、本プロジェクトでは以下に上書きする。

| 対象 | 言語 |
| ---- | ---- |
| コード内コメント (`//`, `/* */`, `///`) | **日本語** |
| docstring / XML doc comment | **日本語** |
| コミットメッセージ本文 | **日本語** |
| PR / Issue 本文 | **日本語** |
| README / 設計ドキュメント | **日本語** |
| コード識別子(クラス・メソッド・変数名) | **英語**(従来通り) |
| コミットメッセージの type (feat / fix / docs / refactor / test / chore / build / ci) | **英語**(従来通り) |
| 設定ファイル内のキー・値 | **英語**(YAML / JSON の仕様上必須) |

例:
```csharp
// 山札からカードを 1 枚引いて手札に加える
public Card DrawTopCard(IRandomSource rng) { ... }
```

## 2. テンプレ由来ファイルの扱い

`.gitignore` (`github/gitignore` 由来) / `.gitattributes` (`gitattributes/gitattributes` 由来) のように外部公式テンプレートを採用しているファイルは、ライセンス由来のヘッダーコメント・出典情報・テンプレ本体のコメントを改変しない(将来テンプレを sync する際の競合を避ける)。本プロジェクト固有で追加するセクションのコメントのみ日本語で記述する。

## 3. ユーザーグローバル規約の継承

ユーザーグローバル CLAUDE.md (`~/.claude/CLAUDE.md`) の以下の規約は本プロジェクトでも全て適用する。

- 完了時の必須報告フォーマット
- 出力衛生(個人情報・Public/Private 境界)
- 禁止事項(機密コミット禁止、`*.backup-*` への書き込み禁止、`--no-verify` 禁止 等)
- メモリ運用(auto memory / episodic-memory の 2 層)
- TDD ループと作業フロー
- パッケージ管理優先順位 (JS/TS は bun、Python は uv)

## 4. プロジェクト固有事項

- ターゲット: Unity 6000.4.6f1 / URP 17.4.0 / WebGL 主体
- アーキテクチャ: Domain / Application / Infrastructure / Presentation の asmdef 分割を Phase 0 で構築済(`Assets/_Project/Scripts/` 配下)
- DI / 非同期 / Reactive: **VContainer 1.17.0 + UniTask 2.5.10 + R3 1.3.0** を採用(MessagePipe は WebGL/IL2CPP の Open Generics 制約と R3 単体で代替可能なため不採用)
- Unity Cloud Services: 利用しない方針(`organizationId` / `cloudProjectId` は空欄を維持)。Unity Editor を起動した際に自動再リンクされた場合は速やかに Unlink すること

## 5. アーキテクチャ依存ルール

`Assets/_Project/Scripts/` は Clean Architecture 寄りの 4 層 + Bootstrap 構成を採用する。依存方向: `Bootstrap → {Infrastructure, Presentation} → Application → Domain`(逆方向は不可)。

- **Domain** は純粋 C# (`noEngineReferences: true`) で UnityEngine への依存を持たない
- 内側のレイヤは外側を知らない(Domain は Application を知らない、Application は Infrastructure を知らない)
- **Infrastructure → Application** の参照は「Application が定義したインターフェースを Infrastructure が実装する」(Ports & Adapters パターン)目的のみ。Application の具象 UseCase クラスを Infrastructure から直接呼ばない
- **Presentation → Application** も同様にインターフェース経由。Domain への直接参照も許可するが View に Domain エンティティを直接バインドせず Presenter / DTO 経由を推奨
- 依存方向の違反は Roslyn Analyzer による静的検出と asmdef の `noEngineReferences` / `references` 設定で多層防御する(詳細は「7. 機械検知方針」および `docs/architecture/dependency-rules.md`)

## 6. テスト方針

詳細は [`docs/testing-strategy.md`](docs/testing-strategy.md) を参照。要点のみ記載。

### 仕様記述

- **EARS** で機能要件を `docs/specs/<layer>/<module>/<feature>.md` に記述する。5 パターン (Ubiquitous / Event-driven / State-driven / Unwanted / Optional) を使い分ける
- **Gherkin** で受け入れシナリオを `docs/specs/.../<feature>.feature` に併記(`# language: ja` 推奨)
- 仕様ファイルは `Assets/` の外側(`docs/`)に置き、Unity アセット化を避ける
- テンプレートは `docs/specs/.template.md` / `docs/specs/.template.feature`

### テスト実装

- NUnit 標準。メソッド名は `Given_X_When_Y_Then_Z` 形式(日本語可)
- AAA (Arrange-Act-Assert) パターン徹底、`// Given / When / Then` コメント区切り
- 1 テスト 1 アサーション原則

### テスト分類(全テストに `[Category]` を 2 つ以上必須)

| 軸 | カテゴリ |
| ---- | ---- |
| Size | `Small` / `Medium` / `Large` |
| Type | `Normal` / `SemiNormal` / `Abnormal` / `SuperNormal` |

例: `[Test, Category("Small"), Category("Normal")]`

### カバレッジ目標(`com.unity.testtools.codecoverage` で計測)

| レイヤ | C0 (Statement) | 重要分岐の網羅 |
| ---- | ---- | ---- |
| Domain | **95%+** | **MC/DC 相当のケース設計を必須**。`docs/specs/.../<feature>.md` にケース表を併記 |
| Application | 80% | 主要 UseCase の正常 / 異常分岐 |
| Infrastructure | 60% | I/O 系はモックで主要経路のみ |
| Presentation | 計測対象外 | MonoBehaviour 中心、手動 QA / E2E でカバー |

> 注: `com.unity.testtools.codecoverage` v1.3.0 時点で C1 (Branch Coverage) は **未実装**(常に 0、公式ドキュメント明記)。そのため C0 計測 + MC/DC ケース設計で C1 相当を担保する。

CI で SonarQube 互換 Cobertura XML を出力し、PR で差分カバレッジを可視化する。
カバレッジ閾値割れは CI を失敗させる(後続フェーズで実装)。

### 要件トレーサビリティ

各 EARS 要件には `[<MODULE>-<NUMBER>]` 形式の一意 ID を付与する(例: `PILE-005`、`CARD-004`、`RND-001`)。

| レイヤ | 記法 |
| ---- | ---- |
| EARS Markdown | `[<ID>]` を要件文の先頭に。`[Ubiquitous]` / `[Optional]` マーカー併記でテスト免除 |
| Gherkin .feature | `@<ID>` をシナリオの直前に(複数可: `@PILE-003 @PILE-004`) |
| NUnit テスト | `[Property("Requirement", "<ID>")]` 属性(複数可) |

機械検証は lefthook の `traceability` フックが pre-commit で実行(`scripts/check-traceability.sh`)。
詳細は `docs/testing-strategy.md` §4 を参照。

### TDD ループ

Red(失敗テストを書く)→ Green(最小実装)→ Refactor。バグ修正は再現テストから書き始める。

## 7. 機械検知方針

「機械的にすべて検知」を実現するため、検知レイヤを多層に配置する。

### 検知レイヤ全体像

| レイヤ | 検知タイミング | 担当 |
| ---- | ---- | ---- |
| **IDE タイプ中** | リアルタイム | Roslyn Analyzer (C# 構文・命名・null・async) + `.editorconfig` |
| **ファイル保存時** | エディタ依存 | `.editorconfig` (フォーマット) |
| **`git add` → pre-commit** | commit 直前 | lefthook (gitleaks / dotnet format / **dotnet build** / Conventional Commits 等)。`dotnet build` で型解決エラー(CS0246 / CS1614 等) + Roslyn Analyzer 警告を Unity Editor 起動なしに約 4 秒で検出(M4-PR2 後の chore で追加 2026-05-13)|
| **`git commit`** | commit 直前 | lefthook commit-msg |
| **`git push`** | (pre-push なし) | CI に委譲 |
| **手動 / オンデマンド** | 必要時 | `scripts/run-unity-tests.sh [EditMode|PlayMode]`(Unity CLI `-batchmode -runTests` wrap、NUnit テスト実行)/ `scripts/refresh-unity-assets.sh`(`-batchmode -quit` で `.meta` + `.csproj` 機械再生成、Editor 閉鎖時用)。いずれも数十秒〜数分かかるため pre-commit には統合せず手動 / CI レイヤーで実行(M4-PR2 後の chore で追加 2026-05-13)|
| **GitHub Actions / GameCI** | push 後 | dotnet build(Roslyn 再実行)/ Unity Test Runner / カバレッジ閾値(Phase 6 で本格整備、`scripts/run-unity-tests.sh` を CI 内で呼ぶ想定)|
| **branch protection** | PR マージ前 | Required status checks |

### 検知対象一覧(担当別)

| 検知項目 | 担当 |
| ---- | ---- |
| 機密漏洩 (handle / Cloud ID / API key) | gitleaks (lefthook) |
| ProjectSettings.asset の Cloud 値混入 | カスタムスクリプト (lefthook) |
| C# フォーマット崩れ | `dotnet format --verify-no-changes` (lefthook) |
| **C# 型解決エラー / コンパイルエラー(CS0246 / CS1614 等)** | **`dotnet build drowsy-unity.slnx` (lefthook pre-commit、M4-PR2 後の chore で追加)** |
| C# 命名規則 / null 安全 / async 命名 | Roslyn Analyzer (Microsoft.CodeAnalysis.NetAnalyzers + Roslynator)、`dotnet build` 経由で警告 / エラーを pre-commit にも反映 |
| `using UnityEngine` を Domain で禁止 | asmdef `noEngineReferences: true` (物理保証) |
| Conventional Commits 違反 | lefthook commit-msg |
| `[Test]` に Size / Type Category 必須 | lefthook 簡易 grep (Phase 0) → カスタム Roslyn Analyzer (Phase 1+) |
| 新規 Domain `*.cs` 追加時の対応 EARS / .feature 必須 | lefthook カスタムスクリプト |
| EARS 要件 ID ↔ NUnit Test Property の双方向整合性 | lefthook カスタムスクリプト (`check-traceability.sh`) |
| **NUnit テスト実行(EditMode / PlayMode)** | **`scripts/run-unity-tests.sh` 手動 / CI(M4-PR2 後の chore で追加、Unity CLI `-batchmode -runTests` wrap、数十秒〜数分のため lefthook 非統合)** |
| カバレッジ閾値割れ | Code Coverage パッケージ + GitHub Actions |
| `--no-verify` バイパス | ユーザーグローバル設定で物理 deny |

### Roslyn Analyzer 構成

公開 Analyzer のみ NuGetForUnity 経由で導入:
- `Microsoft.CodeAnalysis.NetAnalyzers`(Phase 0 から導入、CA-prefix)
- `Microsoft.Unity.Analyzers`(Phase 0 から導入、UNT-prefix、Unity 公式)
- `Roslynator.Analyzers`(M4 期 chore で導入、RCS-prefix、ADR-0013、baseline silent + 段階的 warning 化)

`.editorconfig` で severity を制御。重要な規約は `error` レベルに引き上げる。
Roslynator は `dotnet_analyzer_diagnostic.category-roslynator.severity = silent` を baseline とし、
個別ルールの段階的 warning 化は `docs/todo.md`「Roslynator RCS ルールの段階的有効化」で追跡する。
カスタム Analyzer (本プロジェクト固有のテスト規約) は Phase 2 以降で検討。

### lefthook 構成

`lefthook.yml` で以下のフックを管理:
- pre-commit: 並列実行(gitleaks / dotnet format / **dotnet build** / カスタムスクリプト群)
- commit-msg: Conventional Commits 検証

詳細は `lefthook.yml` 本体を参照。

### Unity Test Runner / AssetDatabase Refresh の CLI 実行

#### `scripts/run-unity-tests.sh [EditMode|PlayMode]`

Unity Editor を起動せずに Test Runner を実行できる(`Unity -batchmode -quit -runTests` を wrap)。出力は `Temp/test-results/<Platform>.{xml,log}`。lefthook には統合せず(数十秒〜数分のため pre-commit に重すぎる、JIT 確定 2026-05-13)、以下のタイミングで利用する:

- 実装中の手動検証(EditMode テストの全件 PASS 確認)
- M4 以降 Infrastructure 層を触る PR のローカル最終確認
- CI(GameCI、Phase 6 整備時)で push / PR 毎に自動実行

#### `scripts/refresh-unity-assets.sh`

Unity Editor を起動せずに `.meta` / `.csproj` を機械的に再生成する(`Unity -batchmode -quit` を wrap)。**Unity Editor が閉じている時に限り動作** する(Unity は 1 プロジェクト 1 プロセス制約)。`dotnet build` が csproj 最新を要求するため、新規 `.cs` 追加後に csproj を即時同期させたい場合の選択肢。

#### `.meta` / `.csproj` のワークフロー指針(JIT 確定 2026-05-13)

| 状況 | 推奨ワークフロー |
| ---- | ---- |
| **Editor 常駐(VS Code / IDE と並行)** | **Auto-refresh が標準**。`.meta` / `.csproj` は Unity Editor が自動更新するため CLI script 不要、Unity 公式推奨ワークフロー |
| **Editor 閉鎖 / CI 自動化** | `scripts/refresh-unity-assets.sh` で機械再生成(数十秒)、その後 `dotnet build` / `scripts/run-unity-tests.sh` が走れる |
| **Editor 起動中に CLI script を試す** | **不可**(Unity 競合エラー、Editor を閉じるか Auto-refresh を待つ)|

## 8. ワークフロー: PR ベース

すべての変更は feature ブランチ経由で main に取り込む。main への直接 push は GitHub の branch protection rule で物理的に禁止する。詳細は [`docs/workflow.md`](docs/workflow.md)。

### ブランチ戦略

| ブランチ | 用途 | 命名 |
| ---- | ---- | ---- |
| `main` | 常にデプロイ可能な状態。**直接 push 禁止** | — |
| `feature/<short-description>` | 新機能 | `feature/pile-domain` |
| `fix/<short-description>` | 不具合修正 | `fix/draw-empty-pile` |
| `chore/<short-description>` | 環境整備・依存更新 | `chore/upgrade-r3` |
| `docs/<short-description>` | ドキュメント | `docs/architecture-rules` |
| `refactor/<short-description>` | リファクタ | `refactor/rename-pile-to-deck` |

### 1 PR の単位

- 1 PR = 1 論理的変更(複数関心事を 1 PR にまとめない)
- レビュー粒度を保つため、PR は小さく頻繁に作る
- PR description は `.github/pull_request_template.md` のチェックリストを満たす

### Self-Review(個人開発)

個人開発でも以下のいずれかでレビュー証跡を残す:

1. **Claude Code の code-reviewer subagent** によるレビュー(結果を PR コメントに添付、推奨)
2. **`/ultrareview`** (重要な PR の場合、課金を伴うフルレビュー)
3. **GitHub UI で Self-Approval は不可**(GitHub 仕様で自分の PR に Approve できないため)

### Phase 別の強制レベル

| Phase | branch protection 設定 |
| ---- | ---- |
| **0(現在)** | Require PR / Require conversation resolution / No force push |
| **6(CI 整備後)** | 上記 + Require status checks(lint / test / coverage / security) |

Phase 0 段階では CI 未整備のため Required status checks は空。
代わりに **lefthook の pre-commit を必ず通す**(ローカル責任) + PR template チェックリストで担保する。

## 9. 定数管理方針

詳細は [`docs/architecture/constants-management.md`](docs/architecture/constants-management.md)。要点のみ。

### 階層モデル

| 階層 | 種類 | 実装場所 |
| ---- | ---- | ---- |
| L1 | 数学的・物理的不変量 | Domain `<Module>Constants` クラスの `const` |
| L2 | ドメイン上の真の不変量 | 同上 |
| L3 | ゲームバランス調整可能値 | `IGameConfig` interface (Domain) + ScriptableObject (Infrastructure) |
| L4 | ユーザー設定 | `IUserSettings` + PlayerPrefs / save file (Phase 2 以降) |
| L5 | 環境固有値 | `csc.rsp` define シンボル / Unity ビルド設定 |

### 規約

- **マジックナンバー禁止**: `if (count > 5)` のような数値リテラル直書きを避け、必ず命名された定数化する(自明リテラル `0`/`1`/`-1`/`""`/`null` を除く)
- **L1/L2**: `Drowsy.Domain.<Module>.<Module>Constants.<NAME>`(`const` のみ)
- **L3**: `IGameConfig` interface 経由で DI 注入(テスト容易性 + Designer-friendly)
- 機械検知: `.editorconfig` で `CA1802`(`Use literals where appropriate`)を warning 化、Phase 2 以降でカスタム Roslyn Analyzer を検討

### 仕様書での表明

各機能の EARS Markdown 末尾に「定数依存」セクションを設け、依存する定数を列挙する(`docs/specs/.template.md` 参照)。

## 10. Claude (本セッション) の振る舞い規範

私(Claude)は以下を必ず実施:
- ステージング後に `code-reviewer` subagent でレビュー → 指摘反映 → commit
- 機密 grep / JSON validate / lefthook 動作確認
- commit 後は push、PR 作成、Self-Review チェックリスト記入まで一貫して支援
- main への直接 push を試みない(branch protection 有効化前でも feature ブランチ経由)

## 11. 意思決定記録 (ADR)

アーキテクチャ判断・機械的ガードレールの新設撤去・既存設計を覆す変更は ADR (Architecture Decision Record) として残す。運用規約とインデックスは [`docs/adr/README.md`](docs/adr/README.md) に集約する。

- 配置: `docs/adr/NNNN-kebab-case-title.md`(4 桁ゼロ埋め連番、欠番禁止)
- 必須セクション: Context / Decision / Consequences / Related
- Status 語彙: Proposed / Accepted / Rejected / Withdrawn / Deprecated / Superseded by NNNN
- 起票時の Status: 原則 `Accepted`(議論を残したい場合のみ `Proposed` で起票し合意後に書き換える。詳細は `docs/adr/README.md`)
- コミット規約: `docs(adr): <日本語説明>`(Conventional Commits)
- Decider 欄に本名・新規連絡先・自分の handle literal を書かない(ユーザーグローバル「出力衛生」継承)
- Public 文書から Private リソースへのリンクを書かない(Public/Private 境界規約継承)

ADR を起票しない判断: 単純なバグ修正・typo・1 ファイル内リファクタ・パッケージのマイナー更新・軽微な文言調整。

### 確立済み ADR 一覧

| ADR | スコープ | 概要 |
| ---- | ---- | ---- |
| [ADR-0001](docs/adr/0001-adr-operations.md) | 本運用 | ADR の配置 / 必須セクション / Status 語彙 / コミット規約 |
| [ADR-0002](docs/adr/0002-phase1-domain-boundaries.md) | Phase 1 Domain 拡張 | 集約境界・Card 抽象・Immutability・Player 想定 |
| [ADR-0003](docs/adr/0003-todo-operations.md) | TODO 運用 | `docs/todo.md` の運用規約 |
| [ADR-0004](docs/adr/0004-init-setter-polyfill.md) | `IsExternalInit` polyfill | `record + init + with` を Domain で使えるようにする |
| [ADR-0005](docs/adr/0005-phase2-roadmap-drowzzz.md) | Phase 2 Roadmap | 本命ゲーム DrowZzz の段階的縦串実装、M1〜M5 マイルストーン |
| [ADR-0006](docs/adr/0006-m1-detail-application-interfaces.md) | M1 詳細 | 汎用 Application interface(`IGameAction` / `IGameRule` / `ICardCatalog`)+ DrowZzz 最小実装 |
| [ADR-0007](docs/adr/0007-m2-detail-card-effects.md) | M2 カード効果 | `IEffect` + `EffectInterpreter` + サブセット先行スコープ + 継続影響 |
| [ADR-0008](docs/adr/0008-m2-drowzzz-clock-and-night-morning.md) | M2 `DrowZzzClock` | 21:00 開始 / 21 ターン / 夜・朝フェーズ、Session の computed プロパティ経由 |
| [ADR-0009](docs/adr/0009-m2-m3-dp-and-victory-conditions.md) | M2-M3 DP 機構 + 勝利条件 | FDP / DDP / SDP の 3 種別、持ち点 = 合計 (computed)、用語規約(ターン / フェーズ / PhaseState)|
| [ADR-0010](docs/adr/0010-m3-game-termination-and-victory-determination.md) | M3 ゲーム終了判定 + 勝者決定 | `IGameRule.IsTerminated` / `GetWinner` 追加、`GameOutcome` (`WinnerOutcome` / `DrawOutcome`) 新設、早期勝利 `EarlyWinTriggerEffect` + Round 21 完了検出、引き分け仕様(TotalPoints 等値で `DrawOutcome`、tiebreaker なし)|
| [ADR-0011](docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md) | M3 詳細(拡張)「夢」カード + ゲームメカニクス拡張 | 6 機構(連想 / 放棄 / ベッド破損 / キーワード能力 / ターン構造詳細化 / カード No.00「夢」)を統合整理、M3-PR2〜PR6 に PR 分割、ADR-0010 §5 の発動条件を本 ADR §7 で拡張(覆さず文脈追加)|
| [ADR-0012](docs/adr/0012-m4-scriptableobject-and-persistence.md) | M4 詳細 ScriptableObject 化 + 永続化 + ユーザー設定 | メインスコープ(ScriptableObjectCardCatalog / IGameConfig SO 実装 / IEffect の SO 表現 / 既存 3 カード移行)+ サブスコープ(GameState JSON 永続化 / IUserSettings PlayerPrefs)、M4-PR1〜PR7 に PR 分割、ADR-0007 §5「SO 化は M4」と ADR-0011 §「ADR-0012 候補」の確約を実装計画化 |
| [ADR-0013](docs/adr/0013-roslynator-adoption.md) | Roslynator.Analyzers の導入(機械検知レイヤ拡張)| CLAUDE.md §7 と実態の長期乖離(ADR-0007 / ADR-0011 で先送り済)を解消し、`Roslynator.Analyzers` 4.15.0 を NuGetForUnity 経由で導入。baseline は `category-roslynator.severity = silent` で既存コードへの影響なし、個別 RCS ルールの段階的 warning 化は後続 PR で `docs/todo.md` 追跡、同時に CLAUDE.md §7 / docs/testing-strategy.md / README.md の Analyzer 一覧を実態整合化(`Microsoft.Unity.Analyzers` の欠落も訂正)|
| [ADR-0014](docs/adr/0014-start-game-usecase-cardcatalog-removal.md) | `StartGameUseCase` から `ICardCatalog<IEffect>` 依存を削除する | M4 完了時の JIT 再評価で、ADR-0007 §3「設計上の割り切り」が予告した条件(`StartGameUseCase` がカード情報を本当に必要としない事実の確定)に到達したと判断。`_catalog` の dead 依存を解消し constructor を 2 引数 `(IRandomSource, IGameConfig)` に変更。ADR-0006 §3 / ADR-0007 §3 は Status 維持で部分的更新として扱い、機械検知レイヤ(IDE0052 / RCS1213 等の unread / unused 系)との false positive 衝突を排除 |
| [ADR-0015](docs/adr/0015-nullable-reference-types-not-adopting.md) | Nullable Reference Types (NRT) を採らない | PR #12 の CS8632 経緯 + M4 完了時の影響範囲再評価(プロダクション 87 ファイル中 30 ファイル(34%)で `ArgumentNullException` を参照、テスト 27 ファイルと合わせて全 57 ファイル)で、 NRT を現時点で採らないと判断。既存 null 戦略(`ArgumentNullException` + `record + init + value ?? throw` + Abnormal テスト網羅)で実用的な null 安全性は確立済、 NRT 静的検証の追加価値が修正コスト(87 ファイル touch)と Unity 6 互換性検証コストを上回らない。再評価条件(M5 Bootstrap で外部 API クライアント / シリアライザ等の null 多発コードが入る時点等)を明示しておき、 発生時に Superseded by で覆す |
| [ADR-0016](docs/adr/0016-m5-bootstrap-presentation.md) | M5 詳細 — Bootstrap / Presentation 統合(VContainer + UniTask + R3) | M5(Phase 2 最終マイルストーン)の DI 統合方針を確定。VContainer 2 階層 LifetimeScope(Project / Game)+ 登録対象と寿命表 + MVP(View interface + Pure C# Presenter)+ UI Toolkit + R3 ReactiveProperty + UniTask I/O。`IDrowZzzGameSessionSerializer` interface 抽出(M4-PR5 同期 API 維持 + SaveAsync / LoadAsync 追加、Application 配置で Ports & Adapters)、1 シーン構成(Main.unity)、N=2 ホットシート、Auto-save は EndTurn 後のみ、Inspector `[SerializeField]` 注入。M5-PR1〜PR8 の 8 PR 分割計画 + 各 PR の JIT 確認項目を明示、ADR-0015 §「再評価条件」第 1 項を M5-PR8 完成時点に位置付け |
| [ADR-0017](docs/adr/0017-player-roster-vcontainer-collection-workaround.md) | PlayerRoster wrapper 型導入 — VContainer collection resolution と IReadOnlyList<T> 予約型問題 | M5-PR4 で導入した `RegisterInstance<IReadOnlyList<PlayerId>>(BuildPlayers())` が VContainer 1.x の `CollectionInstanceProvider.Match`(`IEnumerable<>` / `IReadOnlyList<>` を予約型として扱い `RegisterInstance` を上書き)により実質空配列に化けていた問題を M5 UI 実機検証で発覚。Application 層に `sealed record PlayerRoster(IReadOnlyList<PlayerId> Players)` を新設し、`RegisterInstance(new PlayerRoster(BuildPlayers()))` + Presenter ctor 7 番目引数を `PlayerRoster roster` に差し替えて回避。新規 EARS モジュール `ROSTER`(001〜004 / 4 件)+ Presenter PRES-014 を `players null` → `roster null` に意味置換(要件 ID 安定性最優先で番号変更なし)。ADR-0016 §2 表に `PlayerRoster` / `Pile (initialDeck)` 行を追記し M5-PR4 完成記録に「実装後発覚 → ADR-0017 で修正」を注記 |
| [ADR-0018](docs/adr/0018-cardtypeid-cardid-instance-separation.md) | CardTypeId と CardId(instance)の分離 — Hand 重複検出問題の根本対処 | ADR-0017 PR #95 マージ後の M5 BootAsync 実機検証で発覚した `Hand.Add` 重複検出エラーへの根本対処。Phase 1 設計の `CardId` が「catalog の lookup key」と「Hand 内 unique 識別子」を兼ねる二重意味問題を解消するため、業界デファクト(Card Type / Card Instance 分離)に沿って `CardTypeId` を新設(catalog key)+ `CardId` を `record CardId(CardTypeId TypeId, int Instance)` 複合型に refactor。catalog API(`ICardCatalog<TEffect>.Get/TryGet/GetEffects`)は `CardTypeId` 引数に変更、Bootstrap.`BuildInitialDeck` で `CardId.Of(typeId, i)` で unique instance 生成して Hand 配布時の重複検出を構造的に回避。`Hand` の unique 制約(HAND-003 / 005)は CardId instance unique で正当化されるため EARS 文言は維持、CARD-006/007/008 は新 API に合わせて意味置換、新規 `CTYPE` モジュール(001〜005 / 5 件、005 は `#` 含む文字列の `ArgumentException`)新設、永続化 `CardIdJsonConverter` を `"<typeId>#<instance>"` schema に対応。100+ ファイル touch の大規模 breaking change だが Phase 3 以降の長期保守性を優先 |
| [ADR-0019](docs/adr/0019-associated-card-ids-session-field.md) | DrowZzzGameSession に AssociatedCardIds フィールド追加 — No.04「静寂を纏う」着手前の設計基盤 | No.04 仕様「連想された手段は選択不可」を honor するため、`DrowZzzGameSession` に `AssociatedCardIds: IReadOnlyCollection<CardId>` フィールド + `IsAssociated(CardId)` O(1) ヘルパーを追加(内部 `HashSet<CardId>`、防御コピー)。`ApplyAssociate` で **Add のみ・Remove なし**(永続記録、Hand 移動と独立)。`PersistedSessionV1` に nullable `AssociatedCardIds` フィールド追加(schemaVersion 1 維持、`NullValueHandling.Ignore` で旧 v1 後方互換)。PR ①(本 ADR、設計基盤)+ PR ②(No.04 本体実装)に分割、PR ① 単独では consumer なし(意図的な段階分割) |
| [ADR-0020](docs/adr/0020-influence-count-decrement-timing.md) | Influence の RemainingCount 減算タイミングを EndTurn へ移行 — カウント1 Marker 機能化 | カード No.09「強引過ぎる一手」のカウント1 Marker(`RestrictAllUsageAndAbandonInfluenceMarkerEffect`)導入で顕在化した、現行 Tick 仕様の構造的バグ(自フェーズ開始時に `TickEffect` 適用 → 直後 count -1 → 0 除去、の順により Marker 系 count=1 が `IsLegalMove` 参照前に消える)を解消。`ApplyEndTurn` 冒頭(PendingClear 後、`Turn.Next` 前)に `DecrementInfluencesForCurrentPlayer`(旧 current の Influences すべて count -1、0 で除去)を追加、`TickInfluencesForCurrentPlayer` は `TickEffect` 適用のみに縮小。「カウント N = 自フェーズ N 回機能」が初めて全 Influence 種別で統一、count ≥ 2 のカード(No.02/04/06/07/08)は発動回数不変。既存テスト 6 件以上の中間状態 assertion(`DZ-176`/`DZ-177`/`DZ-283`/`DZ-285` 等)更新、ADR-0007 §1.5 / ADR-0011 §5 順序保証は部分更新で Supersede せず |
| [ADR-0021](docs/adr/0021-endturn-stuck-escape-valve.md) | EndTurnAction の全フェーズ合法化条件 — stuck 化 Marker 保有時の脱出弁 | No.09(`RestrictAllUsageAndAbandonInfluenceMarkerEffect`)+ No.10(`RestrictDrawCardInfluenceMarkerEffect`)導入で顕在化した進行不能化(stuck)問題への対応。`EndTurnAction` の合法条件を「`PhaseState == WaitingForEndTurn` **OR** 現プレイヤーが stuck 化 Marker を保有」に拡張。新規 `HasAnyStuckCausingInfluence` ヘルパー(`RestrictAllUsageAndAbandon` / `RestrictDrawCard` の OR 検出、ホワイトリスト方式)で stuck 状態のみ全フェーズで EndTurn 合法化。通常プレイ時は従来通り `WaitingForEndTurn` のみ合法でゲームバランス維持。UI は常時 EndTurnButton 表示 + `IsLegalMove` 結果で enable/disable bind するパターンで「ボタンは常にある、通常はすべてのアクションが終わってから押せる」(オーナー JIT 2026-05-17)を honor。既存 force-play.md DZ-311 の意味を「stuck 脱出弁」に拡張 |

PR 別の完成記録 / 進行中マイルストーンの詳細は **各 ADR の §完成記録 / §Implementation Notes** を Single Source of Truth として参照する(CLAUDE.md ではミラーしない)。

### Phase 進捗

- **Phase 1**(Domain 拡張): **完結**(ADR-0002、Domain 9 クラス C0 100%、NUnit 205 件)
- **Phase 2**(DrowZzz 本命実装): **完結**(ADR-0005、M1〜M5 完成、Phase 2 完了の最小定義 5 軸全達成、M5-PR8 = 2026-05-16)
  - **M1**(ターン進行 + カードプレイ最小骨格): 完成(ADR-0006、PR #22〜#28)
  - **M2**(カード効果): **完結** — M2-PR1(効果インフラ)/ M2-PR2(Clock)/ M2-PR3(SDP + カード No.01)/ M2-PR4(DDP + 自動抽選)/ M2-PR5(継続影響 + カード No.02)で ADR-0007 サブセット先行スコープ達成。M2-PR6 以降に予定していた後続効果カードの一部は M3-PR6 で No.00「夢」が追加され、Phase 2 完結時点で M2 サブセットスコープ完結扱い(M5-PR8 で清算、ADR-0007 / ADR-0009 / ADR-0011 のサブセット先行方針と整合)
  - **M3**(勝利条件 / 終了処理 + ゲームメカニクス拡張): **完結**(ADR-0010 + ADR-0011、7 PR で完成:M3-PR1 終了判定 / PR2 ベッド破損 / PR3 放棄 / PR4 連想 / PR5a〜c キーワード基盤 + Counter + 反撃の反撃 / PR6 カード No.00「夢」+ Marker 2 種 + 連想後使用制限機構、NUnit Property 352 件、Session ctor 10 引数化、Marker effect 方式 + 再帰 walk vs 最上位 scan + 2 役兼用パターン確立)
  - **M4**(永続化 / SO 化 + ユーザー設定): **完結**(ADR-0012、M4-PR1〜PR7 + CLI 機械検知レイヤ整備 PR #63 で完成。M4-PR1(SO catalog 骨格)/ M4-PR2(EffectAsset + AdjustSdp)/ M4-PR3(残り 11 派生型 + wrapper 再帰 + 中間型 2 件)/ M4-PR4(既存 3 カード SO ↔ InMemory 同値性検証)/ M4-PR5(`DrowZzzGameSession` JSON 永続化:Newtonsoft.Json + JsonConverter "type" discriminator + PersistedSessionV1 DTO + link.xml + IsExternalInit polyfill)/ M4-PR6(`IUserSettings` + `PlayerPrefsUserSettings`:Domain.asmdef に R3.dll precompiledReferences + ReactiveProperty<T> Observable 公開)/ M4-PR7(M4 完成 PR:`DrowZzzGameConfigAsset` SO 追加実装 + Designer ワークフロー実証(`docs/architecture/designer-workflow.md` + Custom PropertyDrawer `EffectAssetReferenceDrawer`)+ WebGL Build `Result: Succeeded` 59 秒で確認(`docs/architecture/webgl-il2cpp-verification.md`)+ Build Profile 共有資産化 + ADR-0016 §7.1 実態整合訂正)。M4 累計 NUnit Property unique 431 件、EARS 94 件(INF-001〜079 + USR-001〜027)、SO 型 2 + 実 .asset 2 + Serializable POCO 17、新 asmdef 2(`Drowsy.Infrastructure.Tests` / `Drowsy.Infrastructure.Editor`)、Phase 2 完了の最小定義 5 軸中 4 軸を充足(残るは「Play モード操作」のみ → M5))
  - **M5**(Bootstrap / Presentation): **完結**(ADR-0016、M5-PR1〜PR8 + PR #94 / #95 / #96 で完成。VContainer 2 階層 LifetimeScope + MVP Pure C# Presenter + UI Toolkit + R3 `Subject<T>` + UniTask 永続化 + Auto-save Final + GameOutcome の UI 反映)。M5-PR7 後の Unity Play モード実機検証で連鎖発覚した根本問題 2 件は PR #95 ADR-0017(PlayerRoster wrapper、VContainer `IReadOnlyList<T>` 予約型回避)+ PR #96 ADR-0018(CardTypeId 新設 + CardId 複合型 refactor、Hand 重複検出根本対処、Drowsy.Domain 100% カバレッジ)で根本対処、M5-PR8 で WebGL Build `Result: Success` 52.5 秒(M4-PR7 59 秒からほぼ誤差範囲)+ Play モード全機能動作確認 + Phase 2 完結処理を完了

- **Phase 3**(N>2 拡張 / 本格 UI / 世界観統合 / Networking 等): **未着手**(着手判断は別 ADR(候補 ADR-0019)で Phase 3 ロードマップ起票後に再評価)

## 12. TODO 追跡

ADR / EARS / .feature の重さに達しない小規模タスク(技術的負債・改善・後追い chore)は [`docs/todo.md`](docs/todo.md) で追跡する。運用規約の根拠 / テンプレート / 入れる対象・入れない対象の詳細は [ADR-0003](docs/adr/0003-todo-operations.md) および `docs/todo.md` 冒頭の運用ルールセクションに集約する(本 §12 と詳細を二重管理しない)。

- 配置: `docs/todo.md`(完了済み 30 件超で `docs/todo-archive.md` に切り出し)
- 状態管理: 未着手 / 進行中 / 完了済み の 3 セクション
- 完了処理: エントリを「完了済み」へ移動し `Related` に完了 PR / コミット番号を追記(削除しない)
- コミット規約: `docs(todo): <日本語説明>`(Conventional Commits)

詳細(テンプレート / 優先度 / 入れる対象・入れない対象)は ADR-0003 / `docs/todo.md` を参照。
