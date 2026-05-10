# drowsy-unity

Unity 6 で開発する 2D カードゲーム。汎用カードゲームエンジン基盤を先に構築し、具体的なゲームルールは ScriptableObject + Rule クラスで差し込む設計を採用する。

## ステータス

**Phase 1(Domain 拡張)完結** / Phase 2(Application 層 + ゲームルール差込)着手前

| 項目 | 値 |
| ---- | ---- |
| マージ済 PR | 18 |
| NUnit テスト | 189 件全緑 |
| Domain C0 カバレッジ | 100%(427/427 行、87/87 メソッド、全 9 クラス) |
| EARS 要件 ID | 124(CARD 8 / CDATA 17 / HAND 22 / PILE 17 / PLAYER 18 / GS 22 / TURN 12 / RND 5 / CFG 3) |
| ADR | 4(運用 / Phase 1 設計 / TODO 運用 / IsExternalInit polyfill) |

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

## ディレクトリ構成

```
Assets/_Project/Scripts/
  Domain/
    Cards/          CardId / CardData / Hand / Pile
    Configuration/  IGameConfig (Phase 2 以降で具体プロパティを追加予定)
    Players/        PlayerId / PlayerState
    Game/           GameState / TurnState
    Compat/         IsExternalInit (C# 9 init setter 用 polyfill、ADR-0004)
    Random/         IRandomSource / XorShiftRandom
    Drowsy.Domain.asmdef  (noEngineReferences: true)
  Application/      Drowsy.Application.asmdef        (Phase 2 以降で実装)
  Infrastructure/   Drowsy.Infrastructure.asmdef     (Phase 2 以降で実装)
  Presentation/     Drowsy.Presentation.asmdef       (Phase 2 以降で実装)
  Bootstrap/        Drowsy.Bootstrap.asmdef          (Phase 2 以降で実装)
  Tests/Domain.Tests/
    Cards/          CardIdTests / CardDataTests / HandTests / PileTests
    Players/        PlayerIdTests / PlayerStateTests
    Game/           GameStateTests / TurnStateTests
    Random/         XorShiftRandomTests
docs/
  testing-strategy.md
  workflow.md
  todo.md
  architecture/{dependency-rules,constants-management}.md
  adr/{README,0001-adr-operations,0002-phase1-domain-boundaries,0003-todo-operations,0004-init-setter-polyfill}.md
  specs/
    .template.{md,feature}
    domain/
      cards/{card-id,card-data,hand,pile}.{md,feature}
      configuration/game-config.{md,feature}
      players/{player-id,player-state}.{md,feature}
      game/{game-state,turn-state}.{md,feature}
      random/random-source.{md,feature}
scripts/
  check-{cloud-credentials,test-categories,spec-files,traceability}.sh
.github/pull_request_template.md
lefthook.yml / .gitleaks.toml / .editorconfig
CLAUDE.md / .gitattributes / .gitignore
```

## 必要環境

| ツール | バージョン | 用途 |
| ---- | ---- | ---- |
| Unity Hub + Editor | 6000.4.6f1 | 開発エディタ |
| .NET SDK | 8 以降(10 動作確認済) | `dotnet format` |
| [`lefthook`](https://github.com/evilmartians/lefthook) | 2.x | Git pre-commit / commit-msg フック |
| [`gitleaks`](https://github.com/gitleaks/gitleaks) | 8.x | 機密検出 |
| [`gh`](https://cli.github.com/) | 任意 | PR 作成 / 確認(GitHub CLI) |
| [`uv`](https://github.com/astral-sh/uv) | 任意 | unity-mcp 連携時の Python 実行(Phase 2 以降想定) |
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

# 5. NuGetForUnity の Analyzer を確認
#    Unity Editor で NuGet > Manage NuGet Packages
#    Microsoft.Unity.Analyzers 1.26.0 と
#    Microsoft.CodeAnalysis.NetAnalyzers 10.0.203 が Installed か確認

# 6. テスト実行(Window > General > Test Runner > EditMode > Run All)
#    → 189 ケース全緑

# 7. (任意) カバレッジ確認(Window > Analysis > Code Coverage)
#    Enable Code Coverage 有効化 → Editor 再起動 → Generate from Tests
#    → Drowsy.Domain C0 100%(全 9 クラス)
```

## 開発フロー

詳細は [`docs/workflow.md`](docs/workflow.md)。

- **PR ベース**: main 直 push は branch protection (Rulesets) で物理ブロック
- ブランチ: `feature/<name>` / `fix/<name>` / `chore/<name>` / `docs/<name>` / `refactor/<name>` / `test/<name>`
- PR description は [`.github/pull_request_template.md`](.github/pull_request_template.md) に従う
- **Conventional Commits**: `<type>: <日本語説明>`(type は英語、本文日本語)
- **Squash Merge のみ**(linear history 強制)
- **Self-Review** チェックリスト + Claude Code `code-reviewer` subagent / `/ultrareview` のいずれか

## 機械検知 6 層

詳細は [`CLAUDE.md`](CLAUDE.md)「7. 機械検知方針」。

| レイヤ | 検知タイミング | 担当 |
| ---- | ---- | ---- |
| IDE タイプ中 | リアルタイム | Roslyn Analyzer + `.editorconfig` |
| ファイル保存時 | エディタ依存 | `.editorconfig` |
| `git add` → pre-commit | commit 直前 | lefthook 6 commands |
| `git commit` | commit 直前 | lefthook commit-msg(Conventional Commits) |
| `git push` | — | (Phase 6 で CI 整備予定) |
| PR マージ前 | branch protection | GitHub Rulesets(linear history / PR 必須) |

pre-commit の内訳:

| フック | 目的 |
| ---- | ---- |
| `gitleaks` | 機密情報(handle / Cloud ID / API key)検出 |
| `cloud-credentials` | `ProjectSettings.asset` の Unity Cloud 値再混入検出 |
| `test-categories` | NUnit `[Test]` に Size + Type の `[Category]` 必須化 |
| `spec-files` | Domain `.cs` 追加時に対応する EARS 仕様の存在確認(`Compat/` は ADR-0004 で除外) |
| `dotnet-format` | C# フォーマット整合性 |
| `traceability` | EARS 要件 ID ↔ NUnit `[Property("Requirement", ...)]` の双方向整合 |

## テスト戦略

詳細は [`docs/testing-strategy.md`](docs/testing-strategy.md)。

- **仕様駆動(SBE)**: EARS Markdown + Gherkin `.feature`(`docs/specs/` 配下)
- **TDD**: Red → Green → Refactor。バグ修正は再現テストから
- **Category 必須**: Size(Small/Medium/Large)+ Type(Normal/SemiNormal/Abnormal/SuperNormal)
- **カバレッジ目標**: Domain **C0 95%+**(現状 **100%**)、Application 80%、Infrastructure 60%、Presentation 計測対象外
- **トレーサビリティ**: 各 EARS 要件に `[<MODULE>-<NUMBER>]` ID、テストに `[Property("Requirement", "<ID>")]` で機械検証

## 定数管理

詳細は [`docs/architecture/constants-management.md`](docs/architecture/constants-management.md)。

| 階層 | 種類 | 実装場所 |
| ---- | ---- | ---- |
| L1 / L2 | 数学的・ドメイン上の不変量 | Domain `<Module>Constants` の `const` |
| L3 | ゲームバランス調整可能値 | `IGameConfig` interface (Domain) + ScriptableObject (Infrastructure) |
| L4 | ユーザー設定 | `IUserSettings` + PlayerPrefs(Phase 2 以降) |
| L5 | 環境固有値 | Unity ビルド設定 / `csc.rsp` define シンボル |

マジックナンバー禁止。CA1802 を warning 化済。

## 意思決定記録(ADR)

| # | タイトル | Status |
| ---- | ---- | ---- |
| [0001](docs/adr/0001-adr-operations.md) | ADR Operations | Accepted |
| [0002](docs/adr/0002-phase1-domain-boundaries.md) | Phase 1 Domain 拡張の集約境界と概念モデル | Accepted |
| [0003](docs/adr/0003-todo-operations.md) | TODO 運用と docs/todo.md の新設 | Accepted |
| [0004](docs/adr/0004-init-setter-polyfill.md) | C# 9 init setter / record with 式のための IsExternalInit polyfill 採用 | Accepted |

詳細は [`docs/adr/README.md`](docs/adr/README.md)。

## ドキュメント目次

- [`CLAUDE.md`](CLAUDE.md) — プロジェクト規約(全 12 章、§11 ADR 運用 / §12 TODO 追跡を含む)
- [`docs/testing-strategy.md`](docs/testing-strategy.md) — テスト戦略詳細(EARS / Gherkin / Category / カバレッジ / トレーサビリティ)
- [`docs/workflow.md`](docs/workflow.md) — PR ワークフロー(ブランチ戦略 / Self-Review / branch protection 設定)
- [`docs/todo.md`](docs/todo.md) — 後追い chore / 技術的負債の追跡(運用は ADR-0003)
- [`docs/architecture/dependency-rules.md`](docs/architecture/dependency-rules.md) — 4 層 + Bootstrap の責任と違反例
- [`docs/architecture/constants-management.md`](docs/architecture/constants-management.md) — 定数管理 L1〜L5
- [`docs/adr/`](docs/adr/) — 意思決定記録 ADR-0001〜0004
- [`docs/specs/`](docs/specs/) — 機能ごとの EARS + Gherkin

## ライセンス

未定(Phase 2 以降で決定)。
