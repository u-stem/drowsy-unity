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
| EARS 要件 ID ↔ NUnit Test Property の双方向整合性 | lefthook カスタムスクリプト (`check-traceability.sh`) |
| カバレッジ閾値割れ | Code Coverage パッケージ + GitHub Actions |
| `--no-verify` バイパス | ユーザーグローバル設定で物理 deny |

### Roslyn Analyzer 構成

公開 Analyzer のみ Phase 0 で導入(NuGetForUnity 経由):
- `Microsoft.CodeAnalysis.NetAnalyzers`
- `Roslynator.Analyzers`

`.editorconfig` で severity を制御。重要な規約は `error` レベルに引き上げる。
カスタム Analyzer (本プロジェクト固有のテスト規約) は Phase 2 以降で検討。

### lefthook 構成

`lefthook.yml` で以下のフックを管理:
- pre-commit: 並列実行(gitleaks / dotnet format / カスタムスクリプト群)
- commit-msg: Conventional Commits 検証

詳細は `lefthook.yml` 本体を参照。

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

| ADR | スコープ | 主要決定 / 完成記録 |
| ---- | ---- | ---- |
| ADR-0001 | 本運用 | ADR の配置 / 必須セクション / Status 語彙 / コミット規約 |
| ADR-0002 | Phase 1 Domain 拡張 | 集約境界・Card 抽象・Immutability・Player 想定 |
| ADR-0003 | TODO 運用 | `docs/todo.md` の運用規約 |
| ADR-0004 | `IsExternalInit` polyfill | `record + init + with` を Domain で使えるようにする |
| ADR-0005 | Phase 2 Roadmap | 本命ゲーム DrowZzz の段階的縦串実装、M1〜M5 マイルストーン |
| ADR-0006 | M1 詳細 | 汎用 Application interface(`IGameAction` / `IGameRule` / `ICardCatalog`)+ DrowZzz 最小実装 |
| ADR-0007 | M2 詳細(カード効果) | `IEffect` + `EffectInterpreter` + サブセット先行スコープ(下表参照) |
| ADR-0008 | M2 `DrowZzzClock` | 21:00 開始 / 21 ラウンド / 夜・朝フェーズ、Session の computed プロパティ経由(下表参照) |
| ADR-0009 | M2-M3 DP 機構 + 勝利条件 | FDP / DDP / SDP の 3 種別、持ち点 = 合計 (computed)、用語規約刷新(下表参照) |

### ADR-0007 / 0008 / 0009 の進行中マイルストーン

#### ADR-0007(M2 効果メカニズム)

- **M2-PR1**(PR #30、2026-05-11、完成): `IEffect` / `EffectInterpreter` / `ICardCatalog<TEffect>` の効果インフラ整備完成
- **M2-PR3**(PR #35、2026-05-11、完成): §1.4「他者影響系 actor 拡張」を `enum SdpTarget` 引数方式で JIT 確定
- **M2-PR4**(PR #37、2026-05-11、完成): §3「`DrowZzzRule` constructor 引数」を「`IRandomSource` を注入しない、2 引数維持」で JIT 確定(`StartGameUseCase` 事前 Shuffle で代替)

#### ADR-0008(M2 Clock 概念)

設計要点:

- Session の computed プロパティ経由、真の単一情報源は `TurnNumber`
- 21:00 開始 / 21 ラウンド / 夜 21:00〜04:30(Round 1〜16)/ 朝 05:00〜07:00(Round 17〜21)
- ADR-0009 起票時に Clock 21 ラウンド化を境界訂正

完成記録:

- **M2-PR2**(PR #33、2026-05-11、完成):
  - `DrowZzzClock` + `DrowZzzClockConstants`(8 const、L1/L2 集約)+ `Session.Clock` computed
  - 仕様 ID DZ-089〜DZ-098、NUnit 23 件追加(`DrowZzzClockTests` 20 件 + DZ-097 同義性 3 件)
  - 起票時 §1 サンプル `sealed record class` → `sealed record` 訂正 + §1 に `const` ブロック追記 + §7 「テスト免除」→「regression guard 実装」訂正同梱
- **M2-PR3**(PR #35、2026-05-11、完成): §8「Clock を参照する効果」を `TimeOfDayBranchEffect` ラッパー record で JIT 確定

#### ADR-0009(M2-M3 DP 機構 + 勝利条件)

設計要点:

- 持ち点 = FDP + DDP + SDP の合計(computed)
- DDP プール 39 枚共有(13 種 × 3 枚、起票時「36 枚」表記は計算誤記、M2-PR4 PR で訂正)、Turn 5/9/13/17/21 で抽選
- 早期勝利: 夜の間 Turn 1〜16(= `Clock.IsNight`)+ 持ち点 ≥ 100 + 就寝カード
- 終了時勝利: 持ち点低い方
- 勝敗判定本格実装は M3
- ボードゲーム一般用語(ターン / フェーズ / PhaseState)に整理し `DrowZzzTurnPhase` → `DrowZzzPhaseState` リネーム同梱
- ADR-0007 / 0008 の境界訂正(Clock 21 ターン化)同梱

完成記録:

- **M2-PR3**(PR #35、2026-05-11、完成): SDP 機構 + `TotalPoints` (FDP+SDP) + カード No.01「コップ一杯の脅威」を実装
- **M2-PR4**(PR #37、2026-05-11、完成):
  - DDP 機構 + `DdpPool` 値オブジェクト(Application 層、`Pile` と同 API パターン)
  - `DdpPoolConstants`(7 const + `DrawRounds` static readonly)
  - `EndTurnAction.Apply` 内自動抽選機構(Turn 5/9/13/17/21、N=2 で 2 枚抽選累積)
  - `TotalPoints` を 3 項合計(FDP+DDP+SDP)に拡張
  - ADR-0009 計算誤記訂正(36→39)同梱
  - NUnit +33 件 / TestCase 展開で +41 ケース
- **M3 以降の範囲外項目**: `IGameRule.IsTerminated` / `GetWinner` / `EarlyWinTriggerEffect`

### Phase 進捗バナー

#### Phase 1(Domain 拡張)— 完結済み

- ADR-0002 実装順序表 6 項目すべて完了
- Domain 全 9 クラス C0 100%、NUnit 205 件全緑

#### Phase 2 — 進行中(ADR-0005、M1〜M5 マイルストーン分割)

**M1(ターン進行 + カードプレイの最小骨格)— 完成済み**(ADR-0006、M1-PR1〜PR7 完了):

- N=2 のセットアップ + Draw → Play → EndTurn ループが動作
- Application C0 100%
- 効果なし / 勝敗なし / UI なしの ADR-0005 §M1 Definition of Done 達成

**M2(カード効果実装)— 進行中**(ADR-0007 起票済):

- **M2-PR1**(PR #30): `IEffect` 効果インフラ整備済
- **M2-PR2**(PR #33、ADR-0008): `DrowZzzClock` + `DrowZzzClockConstants` 実装済
- **M2-PR3**(PR #35):
  - SDP 機構 + `SdpTarget` enum + 効果 record 3 種(`AdjustSdpEffect` / `DrawCardEffect` / `TimeOfDayBranchEffect`)
  - カード No.01「コップ一杯の脅威」2 枚 + `InMemoryCardCatalog` 2 段 constructor
  - ADR-0009 SDP 機構実装、ADR-0007 §1.4 / ADR-0008 §8 JIT 確定同梱、NUnit +37 件
- **M2-PR4**(PR #37):
  - DDP 機構 + `DdpPool` 値オブジェクト + `DdpPoolConstants` L2 const 集約
  - `EndTurnAction.Apply` 内自動抽選機構(Turn 5/9/13/17/21)
  - `TotalPoints` 3 項合計(FDP+DDP+SDP)拡張
  - ADR-0009 計算誤記訂正(プール枚数 36→39)同梱
  - ADR-0009 DDP 機構実装、ADR-0007 §3 `DrowZzzRule` constructor 2 引数維持 JIT 確定同梱
  - NUnit +33 件 / TestCase 展開で +41 ケース
- **M2-PR5 以降**: 後続効果カードをサブセット先行、`ScriptableObjectCardCatalog` 化は M4 に送る、山札枯渇は現状想定下では発生しない計算根拠を ADR-0007 §「山札枯渇」に明記

## 12. TODO 追跡

ADR / EARS / .feature の重さに達しない小規模タスク(技術的負債・改善・後追い chore)は [`docs/todo.md`](docs/todo.md) で追跡する。運用規約の根拠 / テンプレート / 入れる対象・入れない対象の詳細は [ADR-0003](docs/adr/0003-todo-operations.md) および `docs/todo.md` 冒頭の運用ルールセクションに集約する(本 §12 と詳細を二重管理しない)。

- 配置: `docs/todo.md`(完了済み 30 件超で `docs/todo-archive.md` に切り出し)
- 状態管理: 未着手 / 進行中 / 完了済み の 3 セクション
- 完了処理: エントリを「完了済み」へ移動し `Related` に完了 PR / コミット番号を追記(削除しない)
- コミット規約: `docs(todo): <日本語説明>`(Conventional Commits)

詳細(テンプレート / 優先度 / 入れる対象・入れない対象)は ADR-0003 / `docs/todo.md` を参照。
