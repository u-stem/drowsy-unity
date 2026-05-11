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

- [ ] **`StartGameUseCase` から未使用の `ICardCatalog` 依存削除を検討する** `priority: low`
  - **Why**: ADR-0006 §3 で「constructor injection は維持」と判断し、M1-PR3 で `StartGameUseCase` constructor に `ICardCatalog` を含めたが、M1 範囲で実は一切参照していない(`StartGameUseCase.cs` remarks に「本 PR (M1-PR3) では参照しない」と明記)。ADR-0007 §3 で M2-PR1 にて `ICardCatalog<IEffect>` へジェネリック化すると、`StartGameUseCase` が `IEffect` を内部利用しないにもかかわらず型引数を constructor シグネチャに持つ「設計上の割り切り」が発生する。ADR-0006 §3 を覆す変更になるため本 ADR-0007 スコープ外としたが、SO 化(M4)時に `StartGameUseCase` がカード情報を本当に必要としないことが確定したら依存削除を別 PR / 別 ADR で再評価したい
  - **Done when**:
    - 以下のいずれかが選択され、結果がリポジトリに反映済み:
      - 選択 A: `StartGameUseCase` から `ICardCatalog` 依存を削除(constructor 引数 + フィールド削除)、必要なら ADR-0006 の決定を覆す ADR を追加で起票
      - 選択 B: ADR-0007 §3「設計上の割り切り」を維持し、本 TODO を「採用しない理由」を Notes に追記して完了済み移動
    - M4 着手時または SO 化判断時に再評価し、本 TODO を進行中 → 完了済みに移動
  - **Related**: [ADR-0006 §3](adr/0006-m1-detail-application-interfaces.md)、[ADR-0007 §3 「`StartGameUseCase` の型引数結合」](adr/0007-m2-detail-card-effects.md)、`Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs:44`
  - **Notes**: M2-PR1 では `ICardCatalog<IEffect>` 型引数変更のみで通すため、本 TODO は急がない。M4 (SO 化) 検討と同時期に再評価

- [ ] **M2 効果追加時の「ドロー総数 ≤ 山札サイズ」確認(M2 各 PR の Self-Review 項目)** `priority: medium`
  - **Why**: ADR-0007 §「山札枯渇」で「現状の数値前提下(N=2 × MaxRound 20 × 1 Draw + 初期配布 10 = 50 ≤ 山札 56)では枯渇は発生しない」と確定したが、M2 で「ドロー枚数を増やす効果」(例: `DrawCardsEffect(2)` のように 1 ターンに複数枚ドローさせる効果)が追加された場合、ドロー総数が山札サイズを超える可能性がある。各 M2-PR で計算前提が崩れていないかチェックする運用を残す
  - **Done when**:
    - 各 M2-PR(効果 record 追加 PR)の Self-Review チェックリストに「ドロー総数 ≤ 山札サイズ - 初期配布」確認項目を追加(`.github/pull_request_template.md` 拡張、または各 PR の description に明示)
    - M2 完成 PR 時点で本エントリを「完了済み」に移動し、最終的に成立していた数値(ドロー総数 / 山札サイズ)を記録
    - 計算前提が崩れる場合は枯渇シナリオの仕様 ADR(再シャッフル / ゲーム終了 / その他)を別途起票する旨を本 TODO の Notes に追記
  - **Related**: [ADR-0007 §「山札枯渇」](adr/0007-m2-detail-card-effects.md)、[ADR-0006 §6](adr/0006-m1-detail-application-interfaces.md)、M2-PR2 以降(進行中)
  - **Notes**: M1 完成時点の数値: 初期配布 10、Draw 数 40(N=2 × 20 ラウンド × 1)、合計 50 ≤ 山札 56(余裕 6 枚)。M2 で 1 ターン複数 Draw 効果が入った時点で再計算が必要

- [ ] **Roslynator.Analyzers の導入 or CLAUDE.md §7 訂正** `priority: low`
  - **Why**: CLAUDE.md §7「Roslyn Analyzer 構成」に `Roslynator.Analyzers` が公開 Analyzer として導入予定と記載されているが、現状 NuGetForUnity (`Assets/Packages/`) に未配置。ドキュメントと実態が乖離しており、新規参加者(将来の自分含む)が混乱する
  - **Done when**:
    - 以下のいずれか A / B を選び、結果がリポジトリに反映済み:
      - 選択 A: `Roslynator.Analyzers` を NuGetForUnity 経由で導入し、`.editorconfig` に severity 設定を追加
      - 選択 B: CLAUDE.md §7 / README の Analyzer 一覧から `Roslynator.Analyzers` を削除し、`Microsoft.Unity.Analyzers` + `Microsoft.CodeAnalysis.NetAnalyzers` のみに訂正
    - 設計判断レベル(導入する/しない)であれば ADR-0004 として記録。CLAUDE.md 訂正のみで済む選択 B なら同 PR に判断根拠コメントを残し ADR は不要
  - **Related**: [`CLAUDE.md`](../CLAUDE.md) §7「Roslyn Analyzer 構成」
  - **Notes**: Phase 1 中の任意のタイミングで判断。導入しない場合の Phase 1 後半までに訂正だけは入れたい

- [ ] **`turn-state.md` から ADR-0006 §7 への相互参照を追加** `priority: low`
  - **Why**: ADR-0006 §7 で Phase 1 `TurnState.TurnNumber` を「サブターン番号」と解釈し、DrowZzz の「ターン (=ラウンド)」は `(TurnNumber + 1) / 2` で計算する旨を確定した。一方で ADR-0006 は「`turn-state.md` 本体には手を入れない」(後方互換維持) と判断したため、`turn-state.md` 単独の読者には DrowZzz 用語との対応関係が見えない
  - **Done when**:
    - `docs/specs/domain/game/turn-state.md` の「関連」セクションに「DrowZzz での用語解釈: [ADR-0006 §7](../../../adr/0006-m1-detail-application-interfaces.md)」を 1 行追記
    - 機械検証(traceability / spec-files)が通過
  - **Related**: [ADR-0006 §7](adr/0006-m1-detail-application-interfaces.md)、PR #20 (本 TODO の発生源)
  - **Notes**: 本 PR (ADR-0006 起票) のスコープ外にした理由は code-reviewer S-2 指摘で「双方向参照は別 PR に切り出す方が筋」と判断したため。M1-PR1 着手前または同時に対応する

- [ ] **`IGameConfig.MaxRoundNumber` を追加(M3 着手 PR 内で消化予定)** `priority: medium`
  - **Why**: ADR-0006 §1.4 で「M3 着手 PR で `MaxRoundNumber` プロパティを `IGameConfig` に追加し、ゲーム終了判定 (`TurnState.TurnNumber > MaxRoundNumber × Players.Count`、ADR-0006 §7) に利用する」と確定済。M1 完成までは利用機会がないため M3 まで保留してきたが、忘却防止のため TODO エントリ化
  - **Done when**:
    - `IGameConfig.MaxRoundNumber` プロパティ追加(`Drowsy.Domain.Configuration`、`int` 型、DrowZzz 値 = 20)
    - CFG-102 を `docs/specs/domain/configuration/game-config.md` に追加
    - `StubGameConfig` (Tests/Stubs/) のデフォルト値追加
    - M3 ゲーム終了判定 (`IsTerminated(session)` 等) で参照
  - **Related**: [ADR-0006 §1.4](adr/0006-m1-detail-application-interfaces.md)、[ADR-0006 §7](adr/0006-m1-detail-application-interfaces.md)、M3 着手 PR (将来)
  - **Notes**: ADR-0006 §M1 着手 PR 群 §M1-PR2 補足の「M3 で個別追加」方針通り

- [ ] **`ApplyActionUseCase` / `DrowZzzRuleTests` の共通テストヘルパー抽出** `priority: low`
  - **Why**: M1-PR6 reviewer 指摘 P-2 と M1-PR7 着手時に確認した課題。`ApplyActionUseCaseTests.NewSession` と `DrowZzzRuleTests.NewSession` がほぼ同一実装で重複している。M2 でテストが増えると保守コストが上がる
  - **Done when**:
    - `Tests/Application.Tests/Stubs/SessionFactory.cs`(または `TestSessionBuilder.cs`)等の共通ヘルパーを新設
    - `DrowZzzRuleTests` / `ApplyActionUseCaseTests` / `M1IntegrationTests` を共通ヘルパーに切替
    - 既存テスト全緑を維持
  - **Related**: M1-PR6 reviewer 指摘 P-2 (PR #27 コメント)、M1-PR7 reviewer 指摘 (PR #28 コメント)
  - **Notes**: M2 着手前または M2 中の早い段階で対応すると保守コストが上がる前に統一できる

- [ ] **NRT (Nullable Reference Types) 有効化を検討する** `priority: low`
  - **Why**: PR-1 (CardData) で `CardData?` / `object?` のアノテーション 7 箇所に対し CS8632 警告が発生し、既存パターン(NRT 無効)に揃えて `?` を削除した経緯がある。Domain 全体で null 安全な API を表現したい場合、NRT 有効化が筋。判断は設計判断レベルになる可能性あり(ADR-0004 候補)
  - **Done when**:
    - NRT 有効化のコスト・利益を評価(全 Domain ファイルへの `?` アノテーション付与、Pile / CardId への影響、Unity 6 / 当該 Mono 版の互換性確認)
    - 採用する場合: `csc.rsp` または `.editorconfig` で NRT 有効化、Domain 全体に nullable annotation を導入、既存 NUnit テスト全 Green を維持、Domain C0 95%+ を維持、ADR で判断記録
    - 不採用の場合: ADR で「NRT を採らない理由」を記録(同じ問題を繰り返さないため)
  - **Related**: PR #12 (CardData) のレビュー過程で発生した CS8632 警告対応, [`CLAUDE.md`](../CLAUDE.md) §7
  - **Notes**: 必要性が高まった時点で再検討。Phase 2 以降で外部 API クライアント等の null 多発コードが入る前に判断したい

- [ ] **`DrowZzzGameSession.CurrentRound` を `Clock.RoundNumber` 経由に整理(後方互換維持リファクタ)** `priority: low`
  - **Why**: ADR-0008 で `DrowZzzClock` 値オブジェクトを導入し、`session.Clock.RoundNumber == session.CurrentRound` の同義関係を確定した。ADR-0008 §3 では「`CurrentRound` は変更しない(後方互換維持)」と判断したため、現状は両者が独立に同じ計算式 `(TurnNumber + 1) / 2` を保持している。将来 `DrowZzzGameSession.CurrentRound => Clock.RoundNumber` の薄いショートカットに置き換えると概念が一本化される
  - **Done when**:
    - `DrowZzzGameSession.cs` の `CurrentRound` 計算プロパティ実装を `=> Clock.RoundNumber` に書き換え
    - 既存テスト DZ-010(3 件)が `CurrentRound` を呼び続けても緑のまま
    - EARS `docs/specs/games/drowzzz/skeleton.md` (DZ-010) の式表現を「`Clock.RoundNumber` 経由」に整合
    - N>2 拡張時の対応(`Clock` 側の `Hour`/`Minute` 計算式更新)と同タイミングで実施するか別途検討
  - **Related**: [ADR-0008 §3](adr/0008-m2-drowzzz-clock-and-night-morning.md)、[ADR-0006 §M1-PR2 / DZ-010](adr/0006-m1-detail-application-interfaces.md)、`Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs:82`
  - **Notes**: M2 / M3 内のいずれかの PR で機械的に実施可能(1 行のリファクタ + テスト緑維持)。N>2 対応と同時に行うのが筋(`Hour`/`Minute` の N=2 前提も同じタイミングで再評価)

## 進行中

(着手中のエントリをここに移動する)

## 完了済み

- [x] **Pile に値同値性を追加する** `priority: medium`
  - **Why**: PR-2 で Hand に値同値性(`Equals` / `GetHashCode` / `operator==` / `!=`)を導入したが、既存 `Pile` は参照同値のまま残っていた。Domain 集合型(`Pile` / `Hand` / `CardData`)を全て値同値で揃え、後続 PR(PR-4 GameState など)での比較を一貫させる
  - **Done when** (all met):
    - `Pile` に `Equals(Pile)` / `Equals(object)` / `GetHashCode` / `operator==` / `operator!=` を順序依存シーケンス同値で override(Hand と完全対称)
    - `IEquatable<Pile>` 実装を追加
    - `PileTests` に対応テスト追加(同順序同要素 / 順序異 / カード異 / 枚数異 / 同一参照 / null / n=0 / Equals(object) null・異型 / operator== の両 null・片 null × 2)
    - `pile.md` に PILE-014〜017 を追加
    - Domain C0 カバレッジ 100% を維持
    - `Pile.cs` の XML doc remarks に値同値性方針を追記
  - **Related**: [ADR-0002](adr/0002-phase1-domain-boundaries.md) §「Domain 集合型の値同値性方針」, PR #13 (Hand 値同値導入), 本 PR (TODO-1 完了 PR、マージ後に番号追記)
  - **Notes**: ADR-0003 で確立した TODO 運用の初回完了適用
