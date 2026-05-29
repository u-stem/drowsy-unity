# drowsy-unity

Unity 6 で開発する 2D カードゲーム。汎用カードゲームエンジン基盤を先に構築し、具体的なゲームルールは ScriptableObject + Rule クラスで差し込む設計を採用する。

## ステータス

- **Phase 1**(Domain 拡張): 完結
- **Phase 2**(DrowZzz 本命実装): 完結(2026-05-16)
- **Phase 3**: 未着手

進捗・マイルストーン・ビルド / テスト状況の詳細は [`docs/roadmap.md`](docs/roadmap.md) を参照。

## ターゲット

| 項目 | 設定 |
| ---- | ---- |
| Unity Editor | 6000.4.6f1 |
| Render Pipeline | URP 17.4.0 |
| 主対象プラットフォーム | WebGL |
| 副対象プラットフォーム | StandaloneOSX(Phase 2 以降で再評価) |
| マルチプレイ | 初期対象外(将来 Mirror / Netcode for GameObjects を選定) |

## アーキテクチャ

Clean Architecture 寄りの 4 層 + Bootstrap 構成。詳細は [`docs/architecture/dependency-rules.md`](docs/architecture/dependency-rules.md)。

```
Bootstrap (DI 登録、VContainer)
   ↓ depends on
Infrastructure (永続化・I/O)   Presentation (UI / View)
   ↓                              ↓
Application (UseCase、UniTask)
   ↓
Domain (純粋ロジック、UnityEngine 非依存)
```

`Drowsy.Domain` は asmdef の `noEngineReferences: true` により UnityEngine への依存が物理ブロックされる。

## 使用ライブラリ

| ライブラリ | バージョン | 用途 |
| ---- | ---- | ---- |
| [VContainer](https://github.com/hadashiA/VContainer) | 1.17.0 | DI コンテナ |
| [UniTask](https://github.com/Cysharp/UniTask) | 2.5.10 | 非同期(async/await) |
| [R3](https://github.com/Cysharp/R3) | 1.3.0 | リアクティブ拡張(UniRx 後継) |
| [Code Coverage](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.3) | 1.3.0 | C0 カバレッジ計測(C1 は v1.3.0 時点で未実装) |
| [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) | 4.5.0 | NuGet パッケージ管理 |
| [Microsoft.Unity.Analyzers](https://www.nuget.org/packages/Microsoft.Unity.Analyzers) | 1.26.0 | Unity 公式 Roslyn Analyzer |
| [Microsoft.CodeAnalysis.NetAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.NetAnalyzers) | 10.0.203 | .NET 公式 Analyzer(IDE 内のみ機能) |
| [Roslynator.Analyzers](https://www.nuget.org/packages/Roslynator.Analyzers) | 4.15.0 | コミュニティ Analyzer(200+ ルール、baseline silent、ADR-0013) |

## ディレクトリ構成

| パス | 内容 |
| ---- | ---- |
| `Assets/_Project/Scripts/` | ソース(4 層 + Bootstrap + Tests)。層と依存方向は [`docs/architecture/dependency-rules.md`](docs/architecture/dependency-rules.md) |
| `docs/` | ドキュメント(下記「ドキュメント目次」を参照) |
| `scripts/` | lefthook 検査スクリプト + Unity CLI wrapper |
| ルート | `lefthook.yml` / `.gitleaks.toml` / `.editorconfig` / `drowsy-unity.slnx` |

## 必要環境

| ツール | バージョン | 用途 |
| ---- | ---- | ---- |
| Unity Hub + Editor | 6000.4.6f1 | 開発エディタ |
| .NET SDK | 8 以降(10 動作確認済) | `dotnet format` |
| [`lefthook`](https://github.com/evilmartians/lefthook) | 2.x | Git pre-commit / commit-msg フック |
| [`gitleaks`](https://github.com/gitleaks/gitleaks) | 8.x | 機密検出 |
| [`gh`](https://cli.github.com/) | 任意 | PR 作成 / 確認(GitHub CLI) |
| [`uv`](https://github.com/astral-sh/uv) | 任意 | unity-mcp 連携時の Python 実行(将来導入時) |
| [`chezmoi`](https://www.chezmoi.io/) | 任意 | グローバル dotfiles 管理 |

## セットアップ手順

```bash
# 1. リポジトリを clone
git clone https://github.com/u-stem/drowsy-unity
cd drowsy-unity

# 2. Git フックを有効化
lefthook install

# 3. global git config を noreply email に切替(初回のみ、推奨)
#    GitHub Settings > Emails で "Block command line pushes that expose my email" 有効化済前提
git config --global user.email "<numericId>+<your-handle>@users.noreply.github.com"

# 4. Unity Hub からプロジェクトを開く(初回はパッケージダウンロードに数分)

# 5. NuGetForUnity の Analyzer を確認 / Restore
#    Unity Editor で NuGet > Manage NuGet Packages
#    Microsoft.Unity.Analyzers 1.26.0 /
#    Microsoft.CodeAnalysis.NetAnalyzers 10.0.203 /
#    Roslynator.Analyzers 4.15.0 が Installed か確認
#    (未 Installed の場合は `Restore Packages` ボタンで packages.config から展開)

# 6. テスト実行(Window > General > Test Runner > EditMode > Run All)
#    → EditMode 全テスト緑(Domain / Application / Infrastructure / Presentation)

# 7. (任意) カバレッジ確認(Window > Analysis > Code Coverage)
#    Enable Code Coverage 有効化 → Editor 再起動 → Generate from Tests
#    → Domain 100% を維持、Application / Infrastructure / Presentation も計測対象(目標は CLAUDE.md §6 参照)
```

## 開発フロー

詳細は [`docs/workflow.md`](docs/workflow.md)。

- **PR ベース**: main 直 push は branch protection (Rulesets) で物理ブロック
- ブランチ: `feature/<name>` / `fix/<name>` / `chore/<name>` / `docs/<name>` / `refactor/<name>` / `test/<name>`
- PR description は [`.github/pull_request_template.md`](.github/pull_request_template.md) に従う
- **Conventional Commits**: `<type>: <日本語説明>`(type は英語、本文日本語)
- **Squash Merge のみ**(linear history 強制)
- **Self-Review** チェックリスト + Claude Code `code-reviewer` subagent / `/ultrareview` のいずれか

## 機械検知

多層防御(IDE → pre-commit → commit-msg → CI → branch protection)で規約を機械的に検知する。詳細は [`docs/machine-detection.md`](docs/machine-detection.md)。

- pre-commit(lefthook): gitleaks / dotnet format / dotnet build / カスタムスクリプト群
- commit-msg: Conventional Commits 検証
- CI 未整備のため、現状は pre-commit を必ず通すことがローカル責任

## テスト戦略

詳細は [`docs/testing-strategy.md`](docs/testing-strategy.md)。

- 仕様駆動: EARS + Gherkin(`docs/specs/`)、TDD は Red → Green → Refactor
- 全テストに Size + Type の Category 必須
- カバレッジ目標: Domain C0 95%+ / Application 80%+ / Infrastructure 60%+(Presentation は手動 QA)
- トレーサビリティ: EARS 要件 ID ↔ NUnit `[Property("Requirement", ...)]`

## 定数管理

詳細は [`docs/architecture/constants-management.md`](docs/architecture/constants-management.md)。

- L1 / L2(不変量): Domain `<Module>Constants` の `const`
- L3(バランス値): `IGameConfig` + ScriptableObject
- L4(ユーザー設定): `IUserSettings` + PlayerPrefs
- L5(環境固有): Unity ビルド設定 / `csc.rsp`
- マジックナンバー禁止(`.editorconfig` の CA1802 warning)

## 意思決定記録(ADR)

[`docs/adr/README.md`](docs/adr/README.md) を参照。

## ドキュメント目次

- [`CLAUDE.md`](CLAUDE.md) — プロジェクト規約(言語 / アーキ / テスト / 機械検知 / ワークフロー / 定数 / ADR・TODO 運用)
- [`docs/roadmap.md`](docs/roadmap.md) — Phase 進捗
- [`docs/testing-strategy.md`](docs/testing-strategy.md) — テスト戦略詳細(EARS / Gherkin / Category / カバレッジ / トレーサビリティ)
- [`docs/machine-detection.md`](docs/machine-detection.md) — 機械検知の多層防御(lefthook / Roslyn Analyzer / CLI スクリプト)
- [`docs/workflow.md`](docs/workflow.md) — PR ワークフロー(ブランチ戦略 / Self-Review / branch protection 設定)
- [`docs/todo.md`](docs/todo.md) — 後追い chore / 技術的負債の追跡(運用は ADR-0003)
- [`docs/architecture/dependency-rules.md`](docs/architecture/dependency-rules.md) — 4 層 + Bootstrap の責任と違反例
- [`docs/architecture/constants-management.md`](docs/architecture/constants-management.md) — 定数管理 L1〜L5
- [`docs/adr/README.md`](docs/adr/README.md) — 意思決定記録の索引(全 27 件)
- [`docs/specs/`](docs/specs/) — 機能ごとの EARS + Gherkin(`domain/` / `application/` / `games/drowzzz/`)

## ライセンス

未定(Phase 2 以降で決定)。
