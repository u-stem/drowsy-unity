# 機械検知方針

「機械的にすべて検知」を実現するため、検知レイヤを多層に配置する。本書は CLAUDE.md「機械検知」章の詳細版(Single Source of Truth)。

## 検知レイヤ全体像

| レイヤ | 検知タイミング | 担当 |
| ---- | ---- | ---- |
| IDE タイプ中 | リアルタイム | Roslyn Analyzer(C# 構文・命名・null・async)+ `.editorconfig` |
| ファイル保存時 | エディタ依存 | `.editorconfig`(フォーマット) |
| `git add` → pre-commit | commit 直前 | lefthook(gitleaks / dotnet format / dotnet build / カスタムスクリプト群) |
| `git commit` | commit 直前 | lefthook commit-msg(Conventional Commits) |
| `git push` | (pre-push なし) | CI に委譲 |
| 手動 / オンデマンド | 必要時 | `scripts/run-unity-tests.sh` / `scripts/refresh-unity-assets.sh` |
| GitHub Actions / GameCI | push 後 | dotnet build / Unity Test Runner / カバレッジ閾値(Phase 6 で整備) |
| branch protection | PR マージ前 | Required status checks |

- `dotnet build` は型解決エラー(CS0246 / CS1614 等)と Roslyn Analyzer 警告を Unity Editor 起動なしに数秒で検出する。
- `scripts/*.sh` は数十秒〜数分かかるため pre-commit に統合せず、手動 / CI レイヤーで実行する。

## 検知対象一覧(担当別)

| 検知項目 | 担当 |
| ---- | ---- |
| 機密漏洩(handle / Cloud ID / API key) | gitleaks(lefthook) |
| `ProjectSettings.asset` の Cloud 値混入 | カスタムスクリプト(lefthook) |
| C# フォーマット崩れ | `dotnet format --verify-no-changes`(lefthook) |
| C# 型解決エラー / コンパイルエラー | `dotnet build drowsy-unity.slnx`(lefthook pre-commit) |
| C# 命名規則 / null 安全 / async 命名 | Roslyn Analyzer(NetAnalyzers + Roslynator、dotnet build 経由) |
| `using UnityEngine` を Domain で禁止 | asmdef `noEngineReferences: true`(物理保証) |
| Conventional Commits 違反 | lefthook commit-msg |
| `[Test]` に Size / Type Category 必須 | lefthook grep(将来カスタム Analyzer) |
| 新規 Domain `*.cs` の対応 EARS / .feature 必須 | lefthook カスタムスクリプト |
| EARS 要件 ID ↔ NUnit Property の整合 | lefthook `check-traceability.sh` |
| NUnit テスト実行(EditMode / PlayMode) | `scripts/run-unity-tests.sh`(手動 / CI) |
| カバレッジ閾値割れ | Code Coverage パッケージ + GitHub Actions |
| `--no-verify` バイパス | ユーザーグローバル設定で物理 deny |

## Roslyn Analyzer 構成

NuGetForUnity 経由で公開 Analyzer のみ導入する。

- `Microsoft.CodeAnalysis.NetAnalyzers`(CA-prefix)
- `Microsoft.Unity.Analyzers`(UNT-prefix、Unity 公式)
- `Roslynator.Analyzers`(RCS-prefix、ADR-0013)

運用:

- `.editorconfig` で severity を制御し、重要な規約は `error` に引き上げる。
- Roslynator は `dotnet_analyzer_diagnostic.category-roslynator.severity = silent` を baseline とする。
- 個別 RCS ルールの段階的 warning 化は `docs/todo.md` で追跡する。
- 本プロジェクト固有のカスタム Analyzer は将来検討する。

## lefthook 構成

- pre-commit: 並列実行(gitleaks / dotnet format / dotnet build / カスタムスクリプト群)
- commit-msg: Conventional Commits 検証
- 詳細は `lefthook.yml` 本体を参照する。

## CLI スクリプト

### `scripts/run-unity-tests.sh [EditMode|PlayMode]`

Unity Editor を起動せずに Test Runner を実行する(`Unity -batchmode -quit -runTests` の wrap)。

- 出力: `Temp/test-results/<Platform>.{xml,log}`
- 数十秒〜数分かかるため lefthook には統合しない。
- 用途: 実装中の手動検証 / Infrastructure 層を触る PR のローカル最終確認 / CI(Phase 6)

### `scripts/refresh-unity-assets.sh`

Unity Editor を起動せずに `.meta` / `.csproj` を機械再生成する(`Unity -batchmode -quit` の wrap)。

- Unity Editor が閉じている時に限り動作する(1 プロジェクト 1 プロセス制約)。
- 用途: 新規 `.cs` 追加後に csproj を即時同期したい場合。

### `.meta` / `.csproj` のワークフロー指針

| 状況 | 推奨ワークフロー |
| ---- | ---- |
| Editor 常駐(IDE と並行) | Auto-refresh が標準。CLI script 不要(Unity 公式推奨) |
| Editor 閉鎖 / CI 自動化 | `scripts/refresh-unity-assets.sh` で再生成後に build / test |
| Editor 起動中に CLI script | 不可(Unity 競合。Editor を閉じるか Auto-refresh を待つ) |
