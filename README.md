# drowsy-unity

Unity 6 で開発する 2D カードゲーム。汎用カードゲームエンジン基盤を先に構築し、具体的なゲームルールは ScriptableObject + Rule クラスで差し込む設計を採用する。

## ステータス

**Phase 1(Domain 拡張)完結** / **Phase 2(DrowZzz 本命実装)完結**(2026-05-16、M5-PR8 = ADR-0005 §7 Phase 2 完了の最小定義 5 軸全達成)/ **Phase 3 未着手**(別 ADR で起票予定)

| 項目 | 値 |
| ---- | ---- |
| Phase 2 完了マイルストーン | M1(ターン進行 + カードプレイ最小骨格)/ M2(カード効果 + サブセット 3 種)/ M3(終了判定 + ゲームメカニクス拡張)/ M4(永続化 + SO + ユーザー設定)/ M5(Bootstrap + Presentation + WebGL Build) 全完了 |
| WebGL Build | **Result: Success**(M5-PR8 検証、完全 52.5 秒 / Incremental 9.6 秒 / 88 MB、M4-PR7 時点 59 秒からほぼ誤差範囲) |
| Unity Test Runner | EditMode 全テスト PASS(M5-PR8 commit `2c2a01d` で確認) |
| Domain C0 カバレッジ | **100%**(全 12 クラス、465/465 lines、102/102 methods、ADR-0018 = PR #96 で達成) |
| ADR | 18(運用 / Phase 1 設計 / TODO / polyfill / Phase 2 Roadmap / M1〜M5 詳細 / Roslynator / NRT 不採用 / StartGameUseCase catalog 削除 / PlayerRoster wrapper / CardTypeId 分離) |

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

```
Assets/_Project/Scripts/
  Domain/
    Cards/          CardId / CardData / Hand / Pile
    Configuration/  IGameConfig (FdpPool 追加済、M3 で MaxRoundNumber 追加予定)
    Players/        PlayerId / PlayerState
    Game/           GameState / TurnState
    Compat/         IsExternalInit (C# 9 init setter 用 polyfill、ADR-0004)
    Random/         IRandomSource / XorShiftRandom
    Drowsy.Domain.asmdef  (noEngineReferences: true)
  Application/
    IGameAction.cs / IGameRule.cs / ICardCatalog.cs (汎用 Application interface、M1-PR1)
    Catalog/        InMemoryCardCatalog (M1-PR2)
    Compat/         IsExternalInit (record 利用のため、M1-PR2)
    Games/DrowZzz/  DrowZzzAction (4 種) / DrowZzzGameSession / DrowZzzTurnPhase /
                    DrowZzzRule / StartGameUseCase / ApplyActionUseCase (M1-PR2〜PR6)
    Drowsy.Application.asmdef
  Infrastructure/   Drowsy.Infrastructure.asmdef     (M2 以降で SO 実装)
  Presentation/     Drowsy.Presentation.asmdef       (M5 以降で UI 実装)
  Bootstrap/        Drowsy.Bootstrap.asmdef          (M5 以降で VContainer 統合)
  Tests/
    Domain.Tests/
      Cards/        CardIdTests / CardDataTests / HandTests / PileTests
      Players/      PlayerIdTests / PlayerStateTests
      Game/         GameStateTests / TurnStateTests
      Random/       XorShiftRandomTests
    Application.Tests/
      Catalog/      InMemoryCardCatalogTests
      Compat/       IsExternalInit (assembly 境界用コピー)
      Games/DrowZzz/ DrowZzzGameSessionTests / DrowZzzRuleTests /
                    PlayCardActionTests / StartGameUseCaseTests / ApplyActionUseCaseTests
      Stubs/        StubGameConfig / IdentityRandom (テストヘルパー)
      Integration/  M1IntegrationTests (M1-PR7、end-to-end)
      IGameActionTests / IGameRuleTests / ICardCatalogTests (M1-PR1)
docs/
  testing-strategy.md
  workflow.md
  todo.md
  architecture/{dependency-rules,constants-management}.md
  adr/{README,0001-adr-operations,0002-phase1-domain-boundaries,0003-todo-operations,
       0004-init-setter-polyfill,0005-phase2-roadmap-drowzzz,
       0006-m1-detail-application-interfaces}.md
  specs/
    .template.{md,feature}
    domain/
      cards/{card-id,card-data,hand,pile}.{md,feature}
      configuration/game-config.{md,feature}
      players/{player-id,player-state}.{md,feature}
      game/{game-state,turn-state}.{md,feature}
      random/random-source.{md,feature}
    application/
      {game-action,game-rule,card-catalog,in-memory-card-catalog,
       apply-action-usecase}.{md,feature}
    games/drowzzz/
      {skeleton,setup,draw,play,end-turn,integration}.{md,feature}
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

# 5. NuGetForUnity の Analyzer を確認 / Restore
#    Unity Editor で NuGet > Manage NuGet Packages
#    Microsoft.Unity.Analyzers 1.26.0 /
#    Microsoft.CodeAnalysis.NetAnalyzers 10.0.203 /
#    Roslynator.Analyzers 4.15.0 が Installed か確認
#    (未 Installed の場合は `Restore Packages` ボタンで packages.config から展開)

# 6. テスト実行(Window > General > Test Runner > EditMode > Run All)
#    → 334 ケース全緑(Domain 205 / Application 129)

# 7. (任意) カバレッジ確認(Window > Analysis > Code Coverage)
#    Enable Code Coverage 有効化 → Editor 再起動 → Generate from Tests
#    → Drowsy.Domain + Drowsy.Application 共に C0 100%(M1 完成時点)
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
- **カバレッジ目標**: Domain **C0 95%+**(現状 **100%**)、Application **80%+**(M1 完成時点 **100%**)、Infrastructure 60%、Presentation 計測対象外
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
| [0005](docs/adr/0005-phase2-roadmap-drowzzz.md) | Phase 2 Roadmap — DrowZzz の段階的縦串実装 | Accepted |
| [0006](docs/adr/0006-m1-detail-application-interfaces.md) | M1 詳細 — 汎用 Application 層 interface と DrowZzz 最小実装の設計 | Accepted |

詳細は [`docs/adr/README.md`](docs/adr/README.md)。

## ドキュメント目次

- [`CLAUDE.md`](CLAUDE.md) — プロジェクト規約(全 12 章、§11 ADR 運用 / §12 TODO 追跡を含む)
- [`docs/testing-strategy.md`](docs/testing-strategy.md) — テスト戦略詳細(EARS / Gherkin / Category / カバレッジ / トレーサビリティ)
- [`docs/workflow.md`](docs/workflow.md) — PR ワークフロー(ブランチ戦略 / Self-Review / branch protection 設定)
- [`docs/todo.md`](docs/todo.md) — 後追い chore / 技術的負債の追跡(運用は ADR-0003)
- [`docs/architecture/dependency-rules.md`](docs/architecture/dependency-rules.md) — 4 層 + Bootstrap の責任と違反例
- [`docs/architecture/constants-management.md`](docs/architecture/constants-management.md) — 定数管理 L1〜L5
- [`docs/adr/`](docs/adr/) — 意思決定記録 ADR-0001〜0006
- [`docs/specs/`](docs/specs/) — 機能ごとの EARS + Gherkin(`domain/` / `application/` / `games/drowzzz/`)

## ライセンス

未定(Phase 2 以降で決定)。
