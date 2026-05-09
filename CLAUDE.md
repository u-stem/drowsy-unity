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
- 依存方向の違反は Roslyn Analyzer による静的検出と asmdef の `noEngineReferences` / `references` 設定で多層防御する(詳細は §7、`docs/architecture/dependency-rules.md`)

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

| レイヤ | C0 | C1 | 重要分岐 |
| ---- | ---- | ---- | ---- |
| Domain | 95%+ | **100%** | MC/DC 相当のケース表を `docs/specs/.../<feature>.md` に併記 |
| Application | 80% | 80% | — |
| Infrastructure | 60% | 50% | — |
| Presentation | 計測対象外 | — | — |

CI で SonarQube 互換 Cobertura XML を出力し、PR で差分カバレッジを可視化する。
カバレッジ閾値割れは CI を失敗させる(後続フェーズで実装)。

### TDD ループ

Red(失敗テストを書く)→ Green(最小実装)→ Refactor。バグ修正は再現テストから書き始める。

## 7. 機械検知方針

「機械的にすべて検知」を実現するため、検知レイヤを多層に配置する。

### 検知レイヤ全体像

| レイヤ | 検知タイミング | 担当 |
| ---- | ---- | ---- |
| **IDE タイプ中** | リアルタイム | Roslyn Analyzer (C# 構文・命名・null・async) + `.editorconfig` |
| **ファイル保存時** | エディタ依存 | `.editorconfig` (フォーマット) |
| **`git add` → pre-commit** | commit 直前 | lefthook (gitleaks / dotnet format / Conventional Commits 等) |
| **`git commit`** | commit 直前 | lefthook commit-msg |
| **`git push`** | (pre-push なし) | CI に委譲 |
| **GitHub Actions / GameCI** | push 後 | dotnet build (Roslyn 再実行) / Unity Test Runner / カバレッジ閾値 |
| **branch protection** | PR マージ前 | Required status checks |

### 検知対象一覧(担当別)

| 検知項目 | 担当 |
| ---- | ---- |
| 機密漏洩 (handle / Cloud ID / API key) | gitleaks (lefthook) |
| ProjectSettings.asset の Cloud 値混入 | カスタムスクリプト (lefthook) |
| C# フォーマット崩れ | `dotnet format --verify-no-changes` (lefthook) |
| C# 命名規則 / null 安全 / async 命名 | Roslyn Analyzer (Microsoft.CodeAnalysis.NetAnalyzers + Roslynator) |
| `using UnityEngine` を Domain で禁止 | asmdef `noEngineReferences: true` (物理保証) |
| Conventional Commits 違反 | lefthook commit-msg |
| `[Test]` に Size / Type Category 必須 | lefthook 簡易 grep (Phase 0) → カスタム Roslyn Analyzer (Phase 1+) |
| 新規 Domain `*.cs` 追加時の対応 EARS / .feature 必須 | lefthook カスタムスクリプト |
| カバレッジ閾値割れ | Code Coverage パッケージ + GitHub Actions |
| `--no-verify` バイパス | ユーザーグローバル設定で物理 deny |

### Roslyn Analyzer 構成

公開 Analyzer のみ Phase 0 で導入(NuGetForUnity 経由):
- `Microsoft.CodeAnalysis.NetAnalyzers`
- `Roslynator.Analyzers`

`.editorconfig` で severity を制御。重要な規約は `error` レベルに引き上げる。
カスタム Analyzer (本プロジェクト固有のテスト規約) は Phase 1 以降で検討。

### lefthook 構成

`lefthook.yml` で以下のフックを管理:
- pre-commit: 並列実行(gitleaks / dotnet format / カスタムスクリプト群)
- commit-msg: Conventional Commits 検証

詳細は `lefthook.yml` 本体を参照。
