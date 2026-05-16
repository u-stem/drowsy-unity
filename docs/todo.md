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

- [ ] **Presenter 単体テストの C0 カバレッジ計測対象を「計測対象外」→「Presenter のみ計測対象」に格上げ** `priority: low`
  - **Why**: 現状 `docs/testing-strategy.md` で Presentation 層は「計測対象外」だが、M5-PR2〜PR7 で `DrowZzzGamePresenter`(Pure C#)に多数のテストを書いており、計測可能。`Drowsy.Presentation` のうち MonoBehaviour(`DrowZzzGameView`)を除外し、Presenter / Binder などの Pure C# クラスのみ計測対象にすれば、追加価値が出る
  - **Done when**:
    - `docs/testing-strategy.md` Presentation 行を「計測対象外」→「Presenter / Binder 等 Pure C# のみ計測対象、MonoBehaviour 除外」に更新
    - Code Coverage 設定で `Drowsy.Presentation` を IncludeAssemblies に追加 + 適切な assemblyFilters
    - 計測結果が Drowsy.Domain と同等の粒度で得られることを確認
  - **Related**: [ADR-0016](adr/0016-m5-bootstrap-presentation.md) §「TODO 候補」、[`docs/testing-strategy.md`](testing-strategy.md)

- [ ] **`Builds/Web/Build/` の累積出力整理(M4-PR7 / M5-PR8 跨ぎ)** `priority: low`
  - **Why**:M5-PR8 で WebGL Build を実行した結果、新出力 `Builds/Web/Build/WebGL/`(2026-05-16、88 MB)が `Builds/Web/Build/*.{data,framework.js,loader.js,wasm}`(2026-05-14 M4-PR7 時点、約 84 MB)と並存している。Build Profile 設定差で出力構造が変わったための累積。CI 整備(WebGL Build CI 整備 TODO 参照)と合わせて、`Builds/` ディレクトリの管理方針(`.gitignore` 構造 / 出力先固定 / 古い出力のクリーンアップ手順)を整理する
  - **Done when**:
    - `.gitignore` の `Builds/` 配下の取り扱い見直し(本 PR 時点で `Builds/` は untracked のまま、`.gitignore` で全体除外確認)
    - Build Profile を「単一出力先 `Builds/WebGL/`」に統一して累積を防ぐ(Unity Editor で Build Profile 編集)
    - 既存の古い出力(`Builds/Web/Build/*.{data,framework.js,loader.js,wasm}` 直下分)を手動削除
  - **Related**: [ADR-0016 §11 M5-PR8 完成記録](adr/0016-m5-bootstrap-presentation.md)「既知の改善候補」、[`docs/architecture/webgl-il2cpp-verification.md`](architecture/webgl-il2cpp-verification.md) §「検証結果(M5-PR8)」、WebGL Build CI 整備 TODO

- [ ] **UI Toolkit `DataBinding`(Unity 2023+ API)への切替評価** `priority: low`
  - **Why**: 現状 `DrowZzzGameView` は MVP パターン + 手動バインディング(`UserSettingsBinder` 等)で実装しているが、Unity 2023+ の UI Toolkit `DataBinding` API を使えば boilerplate を削減できる可能性。M5 で MVP を選んだのは ADR-0016 §3 で「Pure C# 単体テスト性」を優先したため、`DataBinding` への切替は **Pure C# テスト性を維持できるか**を含めて Phase 3 で評価
  - **Done when**:
    - `DataBinding` API の Pure C# テスト可能性を PoC で検証
    - 既存 `UserSettingsBinder` テスト相当の項目が `DataBinding` API で同等に書けるか確認
    - 切替判断(採用 / 不採用 / 部分採用)を ADR で記録
  - **Related**: [ADR-0016](adr/0016-m5-bootstrap-presentation.md) §3 / §「TODO 候補」

- [ ] **EARS / Gherkin の英語表記「Round X」概念用語を「ターン X」に統一(第 2 弾)** `priority: low`
  - **Why**: 2026-05-16 chore PR `chore/todo-batch-cleanup` で日本語カナ「ラウンド」→「ターン」の機械的置換は完了したが、`.md` / `.feature` 内の英語表記 `Round 21` / `Round 22` / `Round 1〜16` / `夜の Round` / `朝の Round` 等(概念用語)が複数ファイルに残存。テストメソッド名(`Given_Round21最終フェーズで...` / `Given_夜のRound_When_...` 等)と連動する箇所は実装側のテストファイル名と一緒に書き換える必要があるため、本 PR スコープ外として残置
  - **Done when**:
    - 対象ファイル(`docs/specs/games/drowzzz/victory-conditions.md` / `cards/00-dream.md` / `cards/00-dream.feature` / `cards/cup-of-threat.feature` / `cards/cup-of-threat.md` / `counter-counter.md` / `bed-damage.md` / `dp-mechanism-ddp.md` / `effects/early-win-trigger.md` / `effects/time-of-day-branch.md` / `effects/time-of-day-branch.feature` / `presentation/games/drowzzz/presenter-skeleton.md` 等)の英語表記 `Round X` を「ターン X」に統一
    - テストメソッド名(`Given_Round21...` / `Given_夜のRound_...` 等)は実装側のテストファイル名と一緒にリネーム(`CupOfThreatCardTests` / `DrowZzzRuleTests` 等)
    - EARS 英語パート(DZ-190 / DZ-191 等の英語条件文中の `Round 21` / `Round 22`)は別 ADR で全 EARS 統一方針を確定してから対応(本 TODO は日本語パートと連動するテスト名のみ)
    - 実装識別子(`RoundNumber` / `Clock.RoundNumber` / `MaxRoundNumber` 等)は維持(別 TODO「`Clock.RoundNumber` / `CurrentRound` を `TurnNumber` / `CurrentTurn` に改名」で扱う)
    - traceability チェック通過(EARS ID 維持)
  - **Related**: 起点 PR(2026-05-16 chore/todo-batch-cleanup、code-reviewer W-1 反映)、完了済みエントリ「EARS / Gherkin 全体の「ラウンド」→「ターン」用語統一(機械的リネーム)」
  - **Notes**: 第 1 弾は日本語カナ + feature ファイル限定で完了。第 2 弾は .md 内の英語表記 + テストメソッド名リネームでスコープが膨らむため別 PR で扱う

- [ ] **`Window > Analysis` サブメニュー(Build Profiler / Build Report Inspector 等)の押下不能要因調査(Phase 6 候補)** `priority: low`
  - **Why**: M4-PR7 第 6 弾(Web Desktop Development Build 初回成功、commit `5536a85` 2026-05-14)後、プロジェクトオーナーが `Window > Analysis` サブメニューを開こうとしたが、すべてのボタン(Build Profiler / Build Report Inspector / Frame Debugger 等)が **押せない状態** だったと報告。原因仮説:(1) Editor が Compile 中 / Domain Reload 中で一時無効化、(2) 必要な Profiling パッケージが `manifest.json` に未登録、(3) Unity 6 で Build Report の場所が更に変わっている(`Window > Analysis > Build Profiler` ではなく別パス)。M4-PR7 完成記録としては Build Report での型保持確認スクショなしで進める判断(オーナー確認済 2026-05-14)だが、Phase 6(CI 整備)で WebGL Build の自動検証を導入する前に再現性を確認したい
  - **Done when**:
    - `Window > Analysis` 押下不能の再現条件を特定(Compile 中 / Domain Reload 中 / パッケージ未登録 / 別パス 等)
    - 必要なら `manifest.json` に `com.unity.profiling.core` 等を追加 or 別パス(`Window > Analysis > Build Profiler` Unity 6 改名後の場所)を `docs/architecture/webgl-il2cpp-verification.md` に反映
    - Build Report 経由で `Drowsy.Domain` / `Drowsy.Application` / `Drowsy.Infrastructure` / `Newtonsoft.Json` の型保持を確認できた状態を Phase 6 CI で機械検証可能化
    - 結果がリポジトリに反映済み(本 TODO を「完了済み」へ移動)
  - **Related**: [ADR-0012 §M4-PR7 完成記録](adr/0012-m4-scriptableobject-and-persistence.md)、[`docs/architecture/webgl-il2cpp-verification.md`](architecture/webgl-il2cpp-verification.md) §「手順 3: 型保持の検証」、M4-PR7 第 6 弾 commit `5536a85`
  - **Notes**: 本検証なしでも Build 自体が `Result: Succeeded`(Error 0)で完了したため、`link.xml` の効果は **間接的に担保**(型剥がしで Build エラーが出るなら Succeeded にならない)。Phase 6 CI で直接検証経路を確立するまでは「Build 通過 = link.xml 動作」を間接証拠として運用

- [ ] **`DrowZzzGameConfigAsset.OnValidate` の ADR-0012 §4 検証 3 件追加実装(M5 以降)** `priority: low`
  - **Why**: M4-PR7 第 1 弾(`DrowZzzGameConfigAsset` 実装)では ADR-0012 §4「Designer 検証(`OnValidate`、初期推奨)」3 件のうち **「null / 空」検出のみ**(INF-078 / INF-079)を実装し、残り 3 件は先送り(M4-PR7 code-reviewer W-3 反映)。理由:`_fdpPool.Length >= N` の N が SO 側ハードコードになり Phase 3 N>2 拡張で再修正必要 / `_fdpPool` 重複なしは `StartGameUseCase` 側で実行時 `ArgumentException` 担保済(Designer フィードバック即時性のみが論点)/ `_ddpPool` 合計値 0 は LogWarning のみで build を妨げず仕様の硬さも弱い。本番経路の fail-fast は確保済みのため `priority: low`
  - **Done when**:
    - `_fdpPool.Length >= プレイヤー数 N`(N=2 ハードコード or `IGameConfig` 経由動的化を判断)の検証を `OnValidate` に追加、INF-080 で採番、テスト追加
    - `_fdpPool` 重複なし検証を `OnValidate` に追加(`Debug.LogError` + Asset リンク)、INF-081 で採番、テスト追加
    - `_ddpPool` 合計値 0 検証を `OnValidate` に追加(`Debug.LogWarning` のみ、build を妨げない)、INF-082 で採番、テスト追加
    - `docs/specs/infrastructure/game-config-asset.md` の「ADR-0012 §4 検証の縮退と先送り」セクションを「実装完了」に書き換え
    - 結果がリポジトリに反映済み(本 TODO を「完了済み」へ移動、`Related` に PR 番号追記)
  - **Related**: [ADR-0012 §4](adr/0012-m4-scriptableobject-and-persistence.md)、[`docs/specs/infrastructure/game-config-asset.md`](specs/infrastructure/game-config-asset.md)、`Assets/_Project/Scripts/Infrastructure/Configuration/DrowZzzGameConfigAsset.cs`(XML コメント `remarks` で先送り明記)、M4-PR7 code-reviewer W-3 反映
  - **Notes**: 本番経路では `StartGameUseCase` が抽選時に `ArgumentException` を投げるため、本 OnValidate 強化は Designer の Inspector 編集中の即時フィードバック改善のみが対象(M5 / Phase 3 で UI 体験を煮詰める時点で再評価が筋)

- [ ] **M4 範囲のカバレッジ 100% 未達箇所の特定 + 補完テスト追加(次 PR 候補:M4-PR7 完成 PR 同梱 or 別 chore PR)** `priority: medium`
  - **Why**: M4-PR6 完成後の Unity Test Runner + `com.unity.testtools.codecoverage` 計測でカバレッジ 100% 未達箇所がオーナー目視で確認された(2026-05-13、本 TODO 起票 PR #72 同梱の M4-PR6 完成記録セッション)。
    - **確認済の事実**: オーナーが Coverage Window 上で「100% に達していない箇所がある」と目視確認
    - **未確認の事実**: 具体的にどのクラス / どの経路かは未計測(本 TODO 着手時に Cobertura レポート取得から開始)
    - **対象範囲想定**: CLAUDE.md §6 カバレッジ目標(Domain 95%+ / Application 80% / Infrastructure 60%)に対し、本 M4-PR1〜PR6 で実装された Domain / Infrastructure 新規クラス群(`IUserSettings` / `PlayerPrefsUserSettings` / `ScriptableObjectCardCatalog` / `EffectAsset` 派生 12 種 + 中間型 2 種 / `DrowZzzGameSessionSerializer` + 7 Converter / `PersistedSessionV1` DTO)を起点に未到達経路を抽出
  - **Done when**:
    - `com.unity.testtools.codecoverage` で C0(Statement)カバレッジレポートを取得し、Domain / Infrastructure 各クラスごとに未到達経路を抽出
    - レイヤ目標(Domain 95%+ / Infrastructure 60%)を満たす補完テストを追加(`[TestCase]` でケース増やすか、新規テストメソッド追加)
    - 100% 必達ではなく **CLAUDE.md §6 目標値を満たすこと** を完了条件とする(残った未到達経路は Cobertura レポート上の理由付きで許容)
    - 結果がリポジトリに反映済み(本 TODO を「完了済み」へ移動、`Related` に PR 番号追記)
  - **Related**: [ADR-0012 §M4-PR6 完成記録](adr/0012-m4-scriptableobject-and-persistence.md)、[CLAUDE.md §6 カバレッジ目標](../CLAUDE.md)、Unity Test Runner + `com.unity.testtools.codecoverage` の手動計測手順は `docs/testing-strategy.md` 参照
  - **Notes**: M4-PR6 完成 PR #71 マージ後にオーナー目視で発覚、本 PR(M4-PR6 完成記録)で TODO 化して追跡。次の PR(M4-PR7 完成 PR 同梱 or 別 chore PR)で対応する判断はオーナー JIT、現時点では「未着手」として登録のみ
    - **2026-05-13 部分対応**: Claude 推測ベースで明確な未到達と思われる 2 経路を補完(`chore: M4 範囲のカバレッジ補完` PR で実施)
      - **#A LanguageCodes.IsSupported 直接テスト**: USR-005 を `[Ubiquitous]` → 通常要件化、`LanguageCodesTests.cs` 新規(Domain.Tests/Configuration、`[TestCase]` 6 ケース + null 単独 1 メソッド)で `IsSupported("ja")` / `("en")` / `("zh")` / `("JA")` / `("ja-JP")` / `("")` / `(null)` の 7 経路を直接機械検証
      - **#B PlayerPrefsUserSettings.Dispose() 冪等性**: USR-027 新規(「二重 Dispose は silent no-op、内部 ReactiveProperty<T> を二度 Dispose しない」)+ `Given_既Dispose_When_2回目Dispose_Then_冪等で例外なし` テスト追加
    - **未完了の残作業**: オーナー側で Unity Test Runner + `com.unity.testtools.codecoverage` Window を実機計測し、本 #A / #B 以外の未到達経路を Cobertura レポートで特定 → 必要なら追加補完 PR(本 TODO は M4-PR7 完成 PR まで継続追跡)

- [ ] **N>2 拡張時の `UsageRestrictionMarkerEffect` Influence の `RemainingCount` 再評価** `priority: low`
  - **Why**: M3-PR6 で「夢」カードの使用制限を `PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, 1)` で表現した(ADR-0011 §6 JIT 確定 2026-05-14、`DrowZzzRule.ApplyAssociate` 内)。`RemainingCount=1` は N=2 前提で「相手 1 フェーズ経由後の自フェーズ Tick で除去」のセマンティクスを実現する値。N>2 拡張(Phase 3 候補)では「相手 N-1 フェーズ経由」になるため再評価が必要
  - **Done when**:
    - N>2 サポート設計が議論される時点で本 TODO を進行中に移動し、選択肢を整理:
      - 選択 A: `RemainingCount` を `players.Count - 1` で動的化(`DrowZzzRule.ApplyAssociate` 内で計算)
      - 選択 B: 「次の自フェーズ」を別機構(マーカー Influence + 専用 Tick ロジック)で表現
      - 選択 C: N=2 固定運用を ADR で明文化(本 TODO は完了済みへ)
    - 結果がリポジトリに反映済み(実装変更 or ADR 明記 or 本 TODO の Notes に「採用しない理由」記載)
  - **Related**: [ADR-0011 §6](adr/0011-m3-dream-card-and-game-mechanics-expansion.md)、`Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyAssociate` 内 `RemainingCount=1` ハードコード)、M3-PR6 code-reviewer W-4 反映(2026-05-14)
  - **Notes**: 現スコープ(N=2)では問題なし。Phase 3 で N>2 拡張を本格検討する時点で再評価

- [ ] **`UsageRestrictionMarkerEffect` の 2 役兼用設計の将来分離検討** `priority: low`
  - **Why**: M3-PR6 で `UsageRestrictionMarkerEffect` を 2 役兼用 marker として導入(ADR-0011 §6):(1) カード効果列内 = `AssociateAction.Apply` の Influence 付与 trigger、(2) `PlayerInfluence.TickEffect` = `IsLegalPlayCard` の illegal フラグ。両文脈で「no-op 評価」という同じ semantics を返すため現時点で矛盾はないが、将来「Influence 側だけ別の semantics(例: Tick 時に追加効果)」が必要になった場合、型分離コストが大きくなる
  - **Done when**:
    - 後続カードで「同じマーカーを効果列内 / Influence の両方で使うが、Influence 側だけ追加 semantics を持たせたい」要件が出てきた時点で本 TODO を進行中に移動し、以下を選択:
      - 選択 A: `UsageRestrictionMarkerEffect`(効果列用)と `UsageRestrictionInfluenceEffect`(Influence 用)に型分離
      - 選択 B: 2 役兼用を維持し、Tick 時の追加 semantics は別 effect record で表現
      - 選択 C: 後続カードが現れなければ本 TODO を完了済みへ(2 役兼用維持の理由を Notes に記録)
    - 結果がリポジトリに反映済み
  - **Related**: [ADR-0011 §6](adr/0011-m3-dream-card-and-game-mechanics-expansion.md)、`Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/UsageRestrictionMarkerEffect.cs`、M3-PR6 code-reviewer P-1 反映(2026-05-14)
  - **Notes**: 現スコープでは 2 役兼用で問題なし(xmldoc / spec md / EffectInterpreter コメントの 3 箇所で意図文書化済)。将来のカード仕様共有時に再評価

- [ ] **DDP / SDP / FDP の正式名変更可能性** `priority: low`
  - **Why**: プロジェクトオーナーから「FDP / DDP / SDP の名前はいずれも変更可能性あり」と共有済。M2 中の任意のタイミングで正式名が JIT 確定する可能性がある。識別子(record / フィールド名 / EARS / .feature)の変更を伴う場合は機械的リファクタを別 PR で実施
  - **Done when**:
    - 正式名が JIT 確定する
    - 影響範囲(`DrowZzzGameSession` フィールド名 / `IGameConfig.DdpPool` プロパティ名 / EARS 記述 / Tests fixture)を機械的に書き換え
    - ADR-0009 / ADR-0006 の関連箇所に新正式名を反映(注釈で旧称を残す)
  - **Related**: [ADR-0009 §1 / §Negative](adr/0009-m2-m3-dp-and-victory-conditions.md)、[ADR-0006 §2.2](adr/0006-m1-detail-application-interfaces.md)
  - **Notes**: 命名変更時期は不明、M2 完成 PR までに確定すれば一括変更、それ以降は M3 着手時に再評価

- [ ] **`Clock.RoundNumber` / `CurrentRound` を `TurnNumber` / `CurrentTurn` に改名(用語規約整合リファクタ)** `priority: low`
  - **Why**: ADR-0009 §「用語規約」で「ターン(大単位、30 分)/ フェーズ(中単位、1 プレイヤー 1 行動)/ PhaseState(待ち状態)」をボードゲーム一般用語(Eurogame 寄り)で確定したが、実装名は依然 `Clock.RoundNumber` / `DrowZzzGameSession.CurrentRound` のまま(本 PR スコープを限定するため実装名リネームは別 PR に分離)。用語規約と実装名の不整合は中長期で読みづらさを生む。`M1IntegrationTests.PlayOnePhase` / `PlayPhases` は ADR-0009 同 PR で改名済(旧 `PlayOneSubturn` / `PlaySubturns`)、本 TODO 対象外
  - **Done when**:
    - `DrowZzzClock.RoundNumber` → `TurnNumber` に改名(Application 層、`record class DrowZzzClock(int TurnNumber)`)
    - `DrowZzzGameSession.CurrentRound` → `CurrentTurn` に改名
    - 既存テスト DZ-010(3 件)が `CurrentRound` / `Clock.RoundNumber` を呼び続けても緑のまま(リネーム反映後)
    - EARS / .feature `docs/specs/games/drowzzz/skeleton.md`(DZ-010)の式表現を新名へ整合
    - Domain `TurnState.TurnNumber`(フェーズ番号を保持、汎用名)との命名衝突を XML doc / 注釈で明示
  - **Related**: [ADR-0009 §6.5](adr/0009-m2-m3-dp-and-victory-conditions.md)、[ADR-0008 §6](adr/0008-m2-drowzzz-clock-and-night-morning.md)、[ADR-0006 §M1-PR2 / DZ-010](adr/0006-m1-detail-application-interfaces.md)
  - **Notes**: N>2 対応(Hour / Minute 計算式の N=2 専用脱却)と同タイミングで実施するのが筋(`Hour`/`Minute` の N=2 前提も同じタイミングで再評価)。Domain `TurnState` の改名は ADR-0002 「Domain ゲーム非依存」と要相談で別 ADR


## 進行中

- [ ] **M2 効果追加時の「ドロー総数 ≤ 山札サイズ」確認(M2 各 PR の Self-Review 項目)** `priority: medium`
  - **Why**: ADR-0007 §「山札枯渇」で「現状の数値前提下(N=2 × MaxRound 21 × 1 Draw + 初期配布 10 = 52 ≤ 山札 56)では枯渇は発生しない」と確定したが(ADR-0009 起票時に MaxRound 20→21 へ訂正反映)、M2 で「ドロー枚数を増やす効果」(例: `DrawCardsEffect(2)` のように 1 ターンに複数枚ドローさせる効果)が追加された場合、ドロー総数が山札サイズを超える可能性がある。各 M2-PR で計算前提が崩れていないかチェックする運用を残す
  - **Done when**:
    - ✓ 各 M2-PR(効果 record 追加 PR)の Self-Review チェックリストに「ドロー総数 ≤ 山札サイズ - 初期配布」確認項目を追加(`.github/pull_request_template.md` 拡張、本 TODO 着手 PR で完了)
    - ⬜ M2 完成 PR 時点で本エントリを「完了済み」に移動し、最終的に成立していた数値(ドロー総数 / 山札サイズ)を記録
    - ⬜ 計算前提が崩れる場合は枯渇シナリオの仕様 ADR(再シャッフル / ゲーム終了 / その他)を別途起票する旨を本 TODO の Notes に追記
  - **Related**: [ADR-0007 §「山札枯渇」](adr/0007-m2-detail-card-effects.md)、[ADR-0006 §6](adr/0006-m1-detail-application-interfaces.md)、M2-PR2 以降(進行中)、本 TODO 着手 PR(chore: M2 Self-Review 項目化、2026-05-13)
  - **Notes**: 現時点の数値(ADR-0009 起票後): 初期配布 10、Draw 数 42(N=2 × 21 ラウンド × 1)、合計 52 ≤ 山札 56(余裕 4 枚)。M2 で 1 ターン複数 Draw 効果が入った時点で再計算が必要
    - **2026-05-13 部分対応(本 TODO 着手 PR)**: `.github/pull_request_template.md` の「該当する場合のみ」セクションに「M2 効果追加時:ドロー総数 ≤ 山札サイズ - 初期配布」項目を追加し、本 PR 着手時点で完成済の M2-PR1〜PR5 以降(M2-PR6 以降の効果追加 PR / M2 完成 PR)で機械的に確認される運用を確立。残る Done when 2 件(数値最終記録 / ADR 起票方針追記)は M2 完成 PR 時に対応

- [ ] **DDP プール枯渇可能性チェック(M2 各 PR の Self-Review 項目)** `priority: medium`
  - **Why**: ADR-0009 §「DDP プール構造」で「13 種 × 3 枚 = 39 枚」(起票時「36 枚」表記は計算誤記、M2-PR4 PR で訂正済)「Round 5/9/13/17/21 で計 5 回抽選 × N=2 = 10 枚抽選」と確定したが、将来「DDP を追加抽選する効果」が登場した場合に総抽選回数がプール容量を超える可能性がある。各 M2-PR で効果が DDP 抽選を増やす変動を含む場合に再計算が必要
  - **Done when**:
    - ✓ 各 M2-PR(DDP 抽選に影響する effect 追加 PR)の Self-Review チェックリストに「DDP 総抽選 ≤ プール 39」確認項目を追加(`.github/pull_request_template.md` 拡張、本 TODO 着手 PR で完了)
    - ⬜ M2 完成 PR 時点で本エントリを「完了済み」に移動し、最終的に成立していた数値(総抽選数 / プール容量)を記録
    - ⬜ プール枯渇シナリオが発生する場合は別 ADR(プール拡張 / 再シャッフル / 抽選失敗時の挙動)を起票する旨を本 TODO Notes に追記
  - **Related**: [ADR-0009 §3](adr/0009-m2-m3-dp-and-victory-conditions.md)、[ADR-0007 §「山札枯渇」](adr/0007-m2-detail-card-effects.md)(類似構造の山札枯渇チェック TODO)、本 TODO 着手 PR(chore: M2 Self-Review 項目化、2026-05-13)
  - **Notes**: 現状想定下の総抽選 = N=2 × 5 = 10 枚 ≤ プール 39 枚(余裕 29 枚)
    - **2026-05-13 部分対応(本 TODO 着手 PR)**: `.github/pull_request_template.md` の「該当する場合のみ」セクションに「M2 DDP 抽選効果追加時:DDP 総抽選回数 ≤ プール 39 枚」項目を追加し、本 PR 着手時点で完成済の M2-PR1〜PR5 以降(DDP 抽選効果追加 PR / M2 完成 PR)で機械的に確認される運用を確立。残る Done when 2 件(数値最終記録 / ADR 起票方針追記)は M2 完成 PR 時に対応

- [ ] **`SessionFactory` 共通ヘルパーへの M2-PR5 以降 13+ fixture の段階的統合** `priority: low`
  - **Why**: 2026-05-13 chore PR で `ApplyActionUseCaseTests` / `DrowZzzRuleTests` を `Drowsy.Application.Tests.Stubs.SessionFactory` に統合済(`using static SessionFactory` 経路)。一方、M2-PR5 以降の fixture 群(`CounterActionTests` / `AssociateActionTests` / `AbandonActionTests` / `EffectInterpreterTests` / `CupOfThreatCardTests` / `GreenInvasionCardTests` / `DreamCardTests` / `CounterCounterTests` / `DrowZzzGameSessionTests` / `Effects/*Tests` 計 13+ fixture)にも類似 `NewSession` 重複が広がっており、各 fixture 固有の引数追加が発生する可能性あり
  - **Done when**:
    - ⬜ 各 fixture を 1〜数件ずつ段階的に `SessionFactory.NewSession` 経由に切替(`using static SessionFactory` パターン)— **第 1 弾(2 fixture)完了済、後続 PR で継続**
    - ⬜ 必要に応じて `SessionFactory.NewSession` の引数を拡張(各 fixture 固有の引数を吸収できるかケースバイケースで判断、引数 11〜15 個まで増える可能性)
    - ⬜ 既存テスト全緑を維持
    - ⬜ 全 fixture 統合完了で本 TODO を「完了済み」に移動(または「採用しない」判断時は理由を Notes に明記)
  - **Related**: 起点 PR(2026-05-13 chore: テストヘルパー抽出)、`docs/todo.md` 同 PR 完了済みエントリ「`ApplyActionUseCase` / `DrowZzzRuleTests` の共通テストヘルパー抽出」、`Assets/_Project/Scripts/Tests/Application.Tests/Stubs/SessionFactory.cs`、第 1 弾 PR(2026-05-13 chore: SessionFactory 統合第 1 弾、PR #79)
  - **Notes**: 段階的拡張の方がレビュー負担小、`using static SessionFactory` で呼び出し側の修正コストは小さい。各 fixture 固有の引数追加で `SessionFactory` の引数が肥大化した場合は `TestSessionBuilder`(fluent API)パターンへのリファクタを検討
    - **2026-05-13 第 1 弾(本 TODO 進行、PR #79)**: `DrowZzzGameSessionTests` / `EffectInterpreterTests` の 2 fixture を `SessionFactory.NewSession()` 経由に統合(`using static` パターン)。両 fixture ともローカル `NewSession()` ヘルパーが SessionFactory のデフォルト引数値と完全一致するため、引数拡張なしで切替可能。dotnet build 0 警告 / 0 エラー確認済(Unity Test Runner 緑確認はオーナー側)
    - **2026-05-16 第 2 弾(本 TODO 進行、chore/todo-batch-cleanup PR)**: `EarlyWinTriggerEffectTests` / `AdjustSdpEffectTests` の 2 fixture を統合。`SessionFactory.NewSession` に `fdp` / `sdp` パラメータ + `Dp(p1, p2)` builder を新設し、`fdp: Dp(p1: 100)` / `sdp: Dp(p1: 5)` のような明示渡しで FDP / SDP 制御を可能化。dotnet build 0 警告 / 0 エラー確認済(Unity Test Runner 緑確認はオーナー側)
    - **残対象**: `CounterActionTests` / `AssociateActionTests`(`NewSession` + `NewSessionWithBedDamage` の 2 件)/ `AbandonActionTests` / `CupOfThreatCardTests`(`NewSessionWithCardInHand`)/ `GreenInvasionCardTests`(`NewSessionWithCardInHand`)/ `DreamCardTests`(`NewSessionWithDreamInHand` + `NewSessionWithoutDream`)/ `CounterCounterTests`(`NewSessionAfterCounter`)/ Effects 配下の残 9 件(`ApplyInfluenceEffect` / `AssociatableMarkerEffect` / `ChoiceEffect` / `DamageBedEffect` / `DrawCardEffect` / `KeywordedEffect` / `RemoveInfluenceEffect` / `RequiresMinimumTotalPointsMarkerEffect` / `TimeOfDayBranchEffect` / `UsageRestrictionMarkerEffect`)。これらは各 fixture 固有の引数(Hand に特定カード / 特定 phase / 特定 BedDamage 等)を持つため、 SessionFactory.NewSession の引数拡張または fixture 個別の事後セットアップ helper として段階的に対応

- [ ] **Roslynator RCS ルールの段階的有効化(baseline silent → 個別 warning 化)** `priority: low`
  - **Why**: [ADR-0013](adr/0013-roslynator-adoption.md) で `Roslynator.Analyzers` 4.15.0 を導入したが、既存コードへの影響を制御するため baseline `dotnet_analyzer_diagnostic.category-roslynator.severity = silent` で開始した。Roslynator は 200+ ルールを提供しており、コードシンプリフィケーション / リファクタリング系の主要ルール(例: RCS1003 If statement should not be on a single line、RCS1018 Add accessibility modifiers、RCS1090 Add call to ConfigureAwait 等)を段階的に warning / error 化することで機械検知レイヤの実効性を高めたい
  - **Done when**:
    - ⬜ 個別ルールを 1 PR あたり 3〜5 件程度ずつ warning / error 化(`.editorconfig` で `dotnet_diagnostic.RCSxxxx.severity` を追記)— **第 1 弾完了(2026-05-13)、後続 PR で継続**
    - ⬜ 各 warning 化 PR で既存コードの違反箇所を修正(またはルールを `silent` に戻す判断を Notes に記録)
    - ⬜ Phase 整備段階で「Roslynator ルールセットの確定版」を `.editorconfig` で表明し、本 TODO を完了済みへ移動(または ADR-0013 を `Superseded by` で更新)
  - **Related**: [ADR-0013](adr/0013-roslynator-adoption.md)、[`.editorconfig`](../.editorconfig) の「Roslynator.Analyzers (RCS-prefix)」セクション、[Roslynator 公式 ルール一覧](https://josefpihrt.github.io/docs/roslynator/analyzers)、本 TODO 着手 PR(chore: Roslynator RCS 段階的有効化 第 1 弾、2026-05-13)
  - **Notes**: 一度に大量のルール有効化は修正コストが大きいので、PR あたり数ルールずつ進める方針。影響大なルールは別途検討、必要なら本 TODO に細分エントリを追加する
    - **2026-05-13 第 1 弾(本 TODO 着手 PR)**: 影響範囲の小さい 4 ルールを `warning` 化、いずれも既存コード違反 0 件で導入:
      - `RCS1018` アクセス修飾子の明示
      - `RCS1049` 冗長な boolean 比較の簡略化
      - `RCS1163` 未使用パラメータ検出
      - `RCS1170` read-only auto-property 化
    - **2026-05-16 第 2 弾(本 TODO 進行、chore/todo-batch-cleanup PR)**: フォーマット / 冗長性除去系 3 ルールを `warning` 化、いずれも既存コード違反 0 件で導入(dotnet build 0 警告 / 0 エラー確認済):
      - `RCS1036` 連続する空行を削除(冗長な空行)
      - `RCS1037` 末尾空白の削除(`.editorconfig` `trim_trailing_whitespace = true` と整合)
      - `RCS1097` 冗長な `ToString()` 呼び出しの削除
    - **第 1 弾から除外したルール(後続検討)**:
      - `RCS1213`(未使用 private メンバー):`OnEnable` / `OnValidate` / `Awake` / `Start` / `Update` 等の **Unity ライフサイクルメソッド**を Roslynator が認識せず false positive(`ScriptableObjectCardCatalog.cs:56,63` の 2 件で検証済)。Unity ライフサイクルメソッド名単位の suppression(`[UsedImplicitly]` 属性付与 / 個別 `#pragma warning disable` / EditorConfig section override 等)を別 PR で評価する

## 完了済み

- [x] **WebGL Build を CI で自動化(GameCI 経由)** `priority: medium`
  - **Why**: M5-PR8 で WebGL Build `Result: Success` を確認したが、現状はオーナー実機作業。Phase 3 以降の継続的検証のため、CI(GitHub Actions / GameCI)で push / PR ごとに自動 Build したい
  - **Done when** (本 PR 範囲):
    - ✓ `.github/workflows/webgl-build.yml` 新設、GameCI `unity-builder@v4` で WebGL Build を実行(push to main / PR to main / workflow_dispatch)
    - ✓ `actions/upload-artifact@v4` で `build/WebGL` を 90 日保管、`if-no-files-found: error` で空 artifact を CI 失敗扱い
    - ✓ `actions/cache@v4` で Library/ を `Assets/**` / `Packages/**` / `ProjectSettings/**` の hash key + restore-keys フォールバックでキャッシュ
    - ✓ `docs/architecture/webgl-il2cpp-verification.md` に「Phase 6 CI 経路」セクションを追記(secret 設定手順 / cache 戦略 / Status check 活用 / Pro License 切替)
    - ✓ Personal License 前提(Pro License 切替コメント付)、code-reviewer 4 件反映(timeout 30→90 / concurrency main 保護 / lfs false / buildsPath 明示 / fetch-depth 1)
  - **オーナー側の残作業(本 PR マージ後)**:
    - ⬜ Unity Personal License の `.ulf` を `game-ci/unity-request-activation-file` で取得し GitHub Secrets(`UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD`)に登録(`webgl-build.yml` 末尾コメント手順)
    - ⬜ 初回 Build 走行確認(40〜60 分想定)+ 2 回目以降のキャッシュヒット確認(10〜15 分想定)
    - ⬜ Status check として `Build WebGL (Unity 6000.4.6f1)` を branch protection の Required status checks に追加(CLAUDE.md §8 Phase 6 該当、CI 整備の主役)
    - ⬜ Build artifact サイズ / 時間の変化を `docs/architecture/webgl-il2cpp-verification.md` に追記(自動化は後続 TODO、本 PR は CI 経路確立まで)
  - **Related**: [ADR-0016](adr/0016-m5-bootstrap-presentation.md) §「TODO 候補」、[`docs/architecture/webgl-il2cpp-verification.md`](architecture/webgl-il2cpp-verification.md) §「Phase 6 CI 経路」、本完了 PR(ci/webgl-build-gameci、2026-05-16)、code-reviewer W-1〜W-3 + S-4〜S-6 反映

- [x] **ADR-0007 / ADR-0009 / ADR-0011 に「M2 サブセット先行スコープは M2-PR5 で達成、Phase 2 完結時点で M2 ステータスを完結扱い」Note 追記** `priority: low`
  - **Why**: M5-PR8 で CLAUDE.md §11 M2 ステータスを「進行中」→「完結」に清算したが、関連 ADR 側からの逆リンク注記は M5-PR8 スコープ外として残した(影響範囲を絞るため)。Phase 3 着手前に整合性を取りたい
  - **Done when** (all met):
    - ✓ ADR-0007 / ADR-0009 / ADR-0011 の冒頭(タイトル直下)に「M2 / M3 完結の帰結」Note を追記し、ADR-0005 §7 / ADR-0016 §11 M5-PR8 完成記録 / `CLAUDE.md` §11 へのリンクを集約
    - ✓ ADR-0007 は §4 サブセット先行スコープ、ADR-0009 は M2-PR3〜PR5(SDP / DDP)+ M3-PR1〜PR6(勝利条件)、ADR-0011 は 6 機構の M3-PR2〜PR6 完成にそれぞれ言及
  - **Related**: [ADR-0007](adr/0007-m2-detail-card-effects.md)、[ADR-0009](adr/0009-m2-m3-dp-and-victory-conditions.md)、[ADR-0011](adr/0011-m3-dream-card-and-game-mechanics-expansion.md)、[ADR-0016 §11 M5-PR8 完成記録](adr/0016-m5-bootstrap-presentation.md)、本完了 PR(chore/todo-batch-cleanup、2026-05-16)

- [x] **`CardIdJsonConverter` の負値 instance / 不正 schema 経路に Persistence テストを追加** `priority: low`
  - **Why**: ADR-0018 / code-reviewer 提案 6 反映。現状 `int.TryParse(instancePart)` が成功して `CardId.Of(typeId, -5)` が `ArgumentOutOfRangeException` を投げた場合、`catch (ArgumentException)` で `JsonSerializationException` に wrap されるが、本経路のテストが存在しない(実行時にのみ確認可能)。schema 違反系テストを Infrastructure.Tests/Persistence に追加して、診断性とリグレッション防止を担保したい
  - **Done when** (all met):
    - ✓ `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/CardIdJsonConverterTests.cs` 新設(7 テストメソッド / TestCase 含め 14 経路を網羅:round-trip 3 件 + null token / 空・空白 3 件 / `#` 欠如 / 非 int 3 件 / 負値 2 件 / typeId 空)
    - ✓ 関連 EARS を新規 spec ファイル `docs/specs/infrastructure/persistence/card-id-json-converter.md` に追加(INF-088 Ubiquitous + INF-089 normal + INF-090〜094 Abnormal 計 7 件)
    - ✓ traceability チェック通過(仕様 ID 588 件 / テスト Property ID 492 件、INF-088〜094 すべて検出)
    - ✓ dotnet build 0 警告 / 0 エラー確認済
  - **Related**: [ADR-0018](adr/0018-cardtypeid-cardid-instance-separation.md) §8、`Assets/_Project/Scripts/Infrastructure/Persistence/Converters/CardIdJsonConverter.cs`、本完了 PR(chore/todo-batch-cleanup、2026-05-16)

- [x] **EARS / Gherkin 全体の「ラウンド」→「ターン」用語統一(機械的リネーム)** `priority: low`
  - **Why**: ADR-0009 で「ターン = 大単位、フェーズ = 中単位」を確定したが、EARS / .feature 内の「ラウンド」「N=2 サブターン」等の旧用語が一部残っている(M2-PR4 PR では実装名 `RoundNumber` を維持する関係で部分置換に留めた、ADR-0009 §6.5)。仕様文書全体を新用語に統一する機械的リファクタ
  - **Done when** (本 PR 範囲):
    - ✓ `docs/specs/` 配下の **日本語カナ表記**「ラウンド」/「サブターン」を「ターン」/「フェーズ」に機械的置換(`integration.feature` / `integration.md` / `bed-damage.md` / `clock.feature` / `clock.md` / `end-turn.md` / `victory-conditions.feature`)
    - ✓ `victory-conditions.feature` 内の英語表記 `Round 21` / `Round 22` / `Round=1` / `Round=17` / `newRound=21` も「ターン X」に置換(code-reviewer W-1 反映)
    - ✓ 実装識別子参照(`Clock.RoundNumber` / `MaxRoundNumber` / `NightEndRound` 等)は維持(改名は別 TODO「`Clock.RoundNumber` / `CurrentRound` を `TurnNumber` / `CurrentTurn` に改名」で扱う)
    - ✓ `turn-state.md:66` の旧称「サブターン番号」「ターン(=ラウンド)」は ADR-0009 §用語規約による訂正である旨を明示(旧称参照を意図的に保持)
    - ✓ `victory-conditions.feature:8` の「ターン = ラウンド」用語規約説明は「ターン = 大単位 30 分」に書き換え(旧用語マッピング廃止)
    - ✓ traceability チェック通過(EARS ID 維持)
  - **本 PR スコープ外として残置(次 PR 候補、TODO 化)**:
    - ⬜ `.md` ファイル内の英語表記 `Round X` / `夜の Round` / `朝の Round`(概念表記、`victory-conditions.md` / `00-dream.md` / `00-dream.feature` / `cup-of-threat.feature` / `cup-of-threat.md` / `counter-counter.md` / `bed-damage.md` / `dp-mechanism-ddp.md` / `effects/early-win-trigger.md` / `effects/time-of-day-branch.md` / `effects/time-of-day-branch.feature` / `presentation/games/drowzzz/presenter-skeleton.md` 等)。テストメソッド名(`Given_Round21最終フェーズで...` 等)と連動するため別 PR で実装側のテスト名と一緒に書き換えるのが筋
    - ⬜ EARS 英語パート(DZ-190 / DZ-191 等の `Round 21` / `Round 22` 表記)は「日本語用語規約」とは別文脈(英語の概念表記)で、書き換えるなら全 EARS を統一的に英語側でも整理する別 ADR / 別 PR で扱う
  - **Related**: [ADR-0009 §6.5 / §「用語規約」](adr/0009-m2-m3-dp-and-victory-conditions.md)、本完了 PR(chore/todo-batch-cleanup、2026-05-16)、code-reviewer W-1 反映(2026-05-16)
  - **Notes**: 実装名リネーム(`Clock.RoundNumber` → `TurnNumber`)は別 TODO で継続追跡(N>2 拡張と同タイミングで実施するのが筋)

- [x] **Presenter テストのターン進行セットアップを共通ヘルパーへ切り出す** `priority: low`
  - **Why**: M5-PR4 / M5-PR5 で追加した `DrowZzzGamePresenterTests` の PRES-019(Auto-save)等は Given セクションで `StartGameUseCase.Execute` → `FireDrawClicked` → `FirePlayClicked(手札[0])` と複数操作を経て `WaitingForEndTurn` に到達している。これは Presenter の「Handler → AutoSave パイプライン」検証範囲を超えて Application 層の状態遷移知識(WaitingForDraw → Draw → WaitingForPlay → Play → WaitingForEndTurn)をテストが直接使っており、Given ステップ自体が壊れるリスクがあった(M5-PR5 テストハング修正レビュー code-reviewer T-1)
  - **Done when** (all met):
    - ✓ `DrowZzzGamePresenterTests` に共通 `Boot(ctx)` / `AdvanceToWaitingForEndTurn(ctx)` private static helper を追加(Application 層の状態遷移知識を 1 箇所に集約)
    - ✓ PRES-019 / PRES-020 / PRES-021 の Given セクションを `Boot(ctx)` + 必要に応じ `AdvanceToWaitingForEndTurn(ctx)` 呼び出しに置換
    - ✓ dotnet build 0 警告 / 0 エラー確認済(Unity Test Runner 緑確認はオーナー側)
  - **Related**: M5-PR5 テストハング修正レビュー(code-reviewer T-1)、`Assets/_Project/Scripts/Tests/Presentation.Tests/Games/DrowZzz/DrowZzzGamePresenterTests.cs`(PRES-019 等)、[ADR-0016 §10](adr/0016-m5-bootstrap-presentation.md)、本完了 PR(chore/todo-batch-cleanup、2026-05-16)
  - **Notes**: 他テスト(PRES-016 / 017 / 018 / 031 / 032 等)への `Boot(ctx)` 適用は本 PR スコープ外、将来同 fixture で追加テストが増えた時点で都度適用予定

- [x] **NRT (Nullable Reference Types) 有効化を検討する** `priority: low`
  - **Why**: PR-1 (CardData) で `CardData?` / `object?` のアノテーション 7 箇所に対し CS8632 警告が発生し、既存パターン(NRT 無効)に揃えて `?` を削除した経緯がある。Domain 全体で null 安全な API を表現したい場合、NRT 有効化が筋。判断は設計判断レベルになる可能性あり(ADR-0004 候補)
  - **Done when** (resolved with 不採用案):
    - ✓ M4 完了時の JIT 判断で **不採用** を採用、[ADR-0015](adr/0015-nullable-reference-types-not-adopting.md) を起票
    - ✓ 影響範囲評価:プロダクション 87 ファイル(中 30 ファイル(34%)で `ArgumentNullException` 参照)+ テスト約 60 ファイル(中 27 ファイルで `ArgumentNullException` 参照、合計 57 ファイル)、 NRT 有効化で全 annotation 付与 + Unity 6 × NRT 互換性検証が必要、修正コストが追加価値を上回ると判断
    - ✓ 「NRT を採らない理由」を ADR-0015 に記録(再評価条件 4 件を明示:M5 Bootstrap の null 多発コード / 他ゲーム追加時の型契約圧 / Unity / Roslyn 側の進化 / 既存 null 戦略の限界観測)
    - ✓ 結果がリポジトリに反映済み(コード変更なし、 ADR-0015 + CLAUDE.md §11 + docs/adr/README.md インデックス + 本 TODO 完了処理のみ)
  - **Related**: [ADR-0015](adr/0015-nullable-reference-types-not-adopting.md)、PR #12 (CardData) のレビュー過程で発生した CS8632 警告対応, [`CLAUDE.md`](../CLAUDE.md) §7、本完了 PR(chore: NRT 不採用判断 + ADR-0015 起票、2026-05-13)
  - **Notes**: 再評価条件発生時に別 ADR で本 ADR-0015 を `Superseded by` で覆す前提。 ADR は永続的な禁則ではなく現時点の判断を記録するもの

- [x] **`StartGameUseCase` から未使用の `ICardCatalog` 依存削除を検討する** `priority: low`
  - **Why**: ADR-0006 §3 で「constructor injection は維持」と判断し、M1-PR3 で `StartGameUseCase` constructor に `ICardCatalog` を含めたが、M1 範囲で実は一切参照していない(`StartGameUseCase.cs` remarks に「本 PR (M1-PR3) では参照しない」と明記)。ADR-0007 §3 で M2-PR1 にて `ICardCatalog<IEffect>` へジェネリック化すると、`StartGameUseCase` が `IEffect` を内部利用しないにもかかわらず型引数を constructor シグネチャに持つ「設計上の割り切り」が発生する。ADR-0006 §3 を覆す変更になるため本 ADR-0007 スコープ外としたが、SO 化(M4)時に `StartGameUseCase` がカード情報を本当に必要としないことが確定したら依存削除を別 PR / 別 ADR で再評価したい
  - **Done when** (resolved with 選択 A):
    - ✓ M4 完了時の JIT 判断で **選択 A(削除)** を採用、[ADR-0014](adr/0014-start-game-usecase-cardcatalog-removal.md) を起票
    - ✓ `StartGameUseCase` から `ICardCatalog<IEffect>` 依存を削除(constructor 引数 + `_catalog` フィールド + 関連 using を除去、constructor は 2 引数 `(IRandomSource rng, IGameConfig config)` 化)
    - ✓ 呼び出し側 2 箇所(`StartGameUseCaseTests.NewUseCase` / `M1IntegrationTests.NewUseCases`)を 2 引数 constructor に追従修正
    - ✓ ADR-0006 §3 / ADR-0007 §3 は Status `Accepted` 維持で部分的更新として扱い、ADR-0014 の Related で参照経路を残す
    - ✓ 結果がリポジトリに反映済み(dotnet build 0 警告 / 0 エラー / 5.34 秒、Unity Test Runner 実機緑確認はオーナー側)
  - **Related**: [ADR-0014](adr/0014-start-game-usecase-cardcatalog-removal.md)、[ADR-0006 §3](adr/0006-m1-detail-application-interfaces.md)、[ADR-0007 §3 「`StartGameUseCase` の型引数結合」](adr/0007-m2-detail-card-effects.md)、`Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`、本完了 PR(chore: StartGameUseCase ICardCatalog 依存削除、2026-05-13)
  - **Notes**: ADR-0007 §3 が予告した「M4 完了時の再評価」を本 PR で達成。M5 Bootstrap で DI 統合する際は 2 引数 constructor をそのまま利用、 ADR-0014 の判断を覆す必要が出てきた場合は別 ADR で記録

- [x] **Roslynator.Analyzers の導入 or CLAUDE.md §7 訂正** `priority: low`
  - **Why**: CLAUDE.md §7「Roslyn Analyzer 構成」に `Roslynator.Analyzers` が公開 Analyzer として導入予定と記載されているが、現状 NuGetForUnity (`Assets/Packages/`) に未配置。ドキュメントと実態が乖離しており、新規参加者(将来の自分含む)が混乱する
  - **Done when** (resolved with 選択 A):
    - ✓ JIT 判断で **選択 A(導入)** を採用、[ADR-0013](adr/0013-roslynator-adoption.md) を起票
    - ✓ `Roslynator.Analyzers` 4.15.0 を NuGetForUnity 経由で導入(`Assets/packages.config` 追記、Unity Editor 起動 + Restore で `Assets/Packages/Roslynator.Analyzers.4.15.0/` 展開)
    - ✓ `.editorconfig` に `dotnet_analyzer_diagnostic.category-roslynator.severity = silent` を baseline として追加(既存コードへの影響なし、個別ルールの段階的 warning 化は後続 TODO で追跡)
    - ✓ 同時に CLAUDE.md §7 / docs/testing-strategy.md / README.md の Analyzer 一覧を実態整合化(`Microsoft.Unity.Analyzers` の §7 欠落も訂正)
    - ✓ 結果がリポジトリに反映済み
  - **Related**: [ADR-0013](adr/0013-roslynator-adoption.md)、[`CLAUDE.md`](../CLAUDE.md) §7「Roslyn Analyzer 構成」、本完了 PR(chore: Roslynator.Analyzers 導入、2026-05-13)
  - **Notes**: 個別 RCS ルールの段階的 warning 化は新規 TODO「Roslynator RCS ルールの段階的有効化(baseline silent → 個別 warning 化)」で継続追跡。本 TODO は導入判断 + 実態整合化レベルで完了

- [x] **`ApplyActionUseCase` / `DrowZzzRuleTests` の共通テストヘルパー抽出** `priority: low`
  - **Why**: M1-PR6 reviewer 指摘 P-2 と M1-PR7 着手時に確認した課題。`ApplyActionUseCaseTests.NewSession` と `DrowZzzRuleTests.NewSession` がほぼ同一実装で重複している。M2 でテストが増えると保守コストが上がる
  - **Done when** (all met):
    - ✓ `Tests/Application.Tests/Stubs/SessionFactory.cs` を新設(`NewSession` / `NewRule` / `NewDeck` の 3 共通ヘルパーを `public static class` で集約、`DrowZzzRuleTests.NewSession` のスーパーセット引数版)
    - ✓ `DrowZzzRuleTests` / `ApplyActionUseCaseTests` を共通ヘルパーに切替(両 fixture から重複ヘルパー削除 + `using static Drowsy.Application.Tests.Stubs.SessionFactory;` で呼び出し側コードは無修正で互換性維持)
    - ✓ 既存テスト全緑を維持(`dotnet build drowsy-unity.slnx` 0 エラー / 0 警告、Unity Editor Test Runner 確認はオーナー側で実機検証予定)
    - **N/A**: 当初 Done when「`M1IntegrationTests` を共通ヘルパーに切替」は、本 fixture が `NewSession` を持たず `NewPlayers` / `NewDeck(int count)` / `NewUseCases` / `PlayOnePhase` / `PlayPhases` の別ヘルパー構成のため対象外(Done when の文言誤記、本完了済み移動で追記訂正)
  - **Related**: M1-PR6 reviewer 指摘 P-2(PR #27 コメント)、M1-PR7 reviewer 指摘(PR #28 コメント)、本完了 PR(chore: テストヘルパー抽出、2026-05-13)
  - **Notes**: 本 PR は Done when 起票時(M1-PR6/7 時点)の 2 fixture(`DrowZzzRuleTests` / `ApplyActionUseCaseTests`)に範囲限定。M2-PR5 以降の fixture 群(13+ fixture)にも類似 `NewSession` 重複が広がっているが、本 PR スコープ外。**段階的拡張は本 PR で新規未着手 TODO 「`SessionFactory` 共通ヘルパーへの M2-PR5 以降 13+ fixture の段階的統合」を起票して追跡**(`priority: low`、code-reviewer S-1 反映 2026-05-13)。`using static SessionFactory` パターンで呼び出し側の修正コストは小さく、後続 PR で段階的に統合可能

- [x] **`turn-state.md` から ADR-0006 §7 への相互参照を追加** `priority: low`
  - **Why**: ADR-0006 §7 で Phase 1 `TurnState.TurnNumber` を「サブターン番号」と解釈し、DrowZzz の「ターン (=ラウンド)」は `(TurnNumber + 1) / 2` で計算する旨を確定した。一方で ADR-0006 は「`turn-state.md` 本体には手を入れない」(後方互換維持) と判断したため、`turn-state.md` 単独の読者には DrowZzz 用語との対応関係が見えない
  - **Done when** (all met):
    - ✓ `docs/specs/domain/game/turn-state.md` の「関連」セクションに「DrowZzz での用語解釈: [ADR-0006 §7](../../../adr/0006-m1-detail-application-interfaces.md)(本仕様の `TurnState.TurnNumber` を DrowZzz では『サブターン番号』として解釈し、DrowZzz の「ターン(=ラウンド)」は `(TurnNumber + 1) / 2` で計算する。Domain 仕様自体は変更せず、ゲーム固有の用語マッピングを Application 層 ADR-0006 §7 で確定)」を 1 行追記
    - ✓ 機械検証(traceability)が通過(仕様 ID 512 / Property ID 423、変更なし)
  - **Related**: [ADR-0006 §7](adr/0006-m1-detail-application-interfaces.md)、PR #20(本 TODO の発生源、ADR-0006 起票時の code-reviewer S-2 指摘)、本完了 PR(chore housekeeping、2026-05-13)
  - **Notes**: ADR-0006 起票時に code-reviewer S-2「双方向参照は別 PR に切り出す方が筋」と指摘され先送りされていた件、本セッションで chore housekeeping として解消(2026-05-13)

- [x] **`DrowZzzGameSession.CurrentRound` を `Clock.RoundNumber` 経由に整理(後方互換維持リファクタ)** `priority: low`
  - **Why**: ADR-0008 で `DrowZzzClock` 値オブジェクトを導入し、`session.Clock.RoundNumber == session.CurrentRound` の同義関係を確定した。ADR-0008 §3 では「`CurrentRound` は変更しない(後方互換維持)」と判断したため、現状は両者が独立に同じ計算式 `(TurnNumber + 1) / 2` を保持している。将来 `DrowZzzGameSession.CurrentRound => Clock.RoundNumber` の薄いショートカットに置き換えると概念が一本化される
  - **Done when** (all met):
    - ✓ `DrowZzzGameSession.cs` の `CurrentRound` 計算プロパティ実装を `=> Clock.RoundNumber` に書き換え、計算式 `(_gameState.Turn.TurnNumber + 1) / 2` は `Clock` プロパティ側に集約(計算式の真の単一情報源は `Clock` プロパティに一本化)
    - ✓ 既存テスト DZ-010(3 件)が `CurrentRound` を呼び続けても緑のまま(`dotnet build` 0 エラー、Property API は不変)
    - ✓ EARS `docs/specs/games/drowzzz/skeleton.md` (DZ-010) の式表現を「`Clock.RoundNumber` 経由」に整合(2026-05-13 todo.md 完了反映を注釈で記録)
    - ✓ N>2 拡張時の対応(`Clock` 側の `Hour`/`Minute` 計算式更新)は本 PR スコープ外、別途 TODO「N>2 拡張時の `UsageRestrictionMarkerEffect` Influence の `RemainingCount` 再評価」+ TODO「`Clock.RoundNumber` / `CurrentRound` を `TurnNumber` / `CurrentTurn` に改名」で継続追跡
  - **Related**: [ADR-0008 §3](adr/0008-m2-drowzzz-clock-and-night-morning.md)、[ADR-0006 §M1-PR2 / DZ-010](adr/0006-m1-detail-application-interfaces.md)、`Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs:287-303`(`Clock` プロパティに計算式集約 + `CurrentRound` ショートカット化)、本完了 PR(chore housekeeping、2026-05-13)
  - **Notes**: ADR-0008 §3 の「`CurrentRound` は変更しない(後方互換維持)」決定を覆すリファクタ。Property 公開 API は不変、内部計算経路のみ Clock 経由に一本化。N>2 拡張時の更なる再評価は別 TODO で継続追跡(本 PR では `Clock` プロパティの計算式 `(TurnNumber + 1) / 2` の N=2 前提は維持、Phase 3 で再評価)

- [x] **INF-019 `EffectAsset.ToDomain()` 失敗時の skip 経路の本格テスト追加(M4-PR3 で対応)** `priority: medium`
  - **Why**: M4-PR2 で `EffectAsset` 基底 + `AdjustSdpEffectAsset` を導入したが、`AdjustSdpEffect(SdpTarget, int)` は positional record に防御がなく `ArgumentException` を投げる自然な経路がないため INF-019 を Optional マーカーで先送り。M4-PR3 で `KeywordedEffect(IReadOnlyList<Keyword>, IEffect)` の `Inner` null 経路 / `RequiresMinimumTotalPointsMarkerEffect(int)` の `Threshold <= 0` 経路など `ArgumentException` を投げる派生型が複数導入される → これらの `ToDomain()` 失敗を catalog 経路で skip + `Debug.LogError` 発火する動作を本格テスト化する
  - **Done when** (all met):
    - ✓ M4-PR3 で `KeywordedEffectAsset.Inner` null 経路の `ScriptableObjectCardCatalogTests` 拡張(`Given_KeywordedEffectAssetのInnerがnull_When_GetEffects_Then_skip以外要素が残る`)で `LogAssert.Expect` ベースの本格テスト追加
    - ✓ INF-019 の `[Optional]` マーカーを外して通常 EARS に昇格、トレーサビリティ機械検証の対象に戻す(ADR-0012 §M4-PR3 完成記録「INF-019 Optional 解除 ✓」)
    - ✓ 結果がリポジトリに反映済み
  - **Related**: [ADR-0012 §M4-PR3 完成記録](adr/0012-m4-scriptableobject-and-persistence.md)、PR #65(M4-PR3、merged `37d5f1c`)、[`docs/specs/infrastructure/effect-assets.md`](specs/infrastructure/effect-assets.md) INF-019、M4-PR2 code-reviewer P-2 反映(2026-05-13)
  - **Notes**: M4-PR3 完了で本 TODO 解消(本完了済み移動は housekeeping PR で適用、2026-05-13)

- [x] **`IGameConfig.MaxRoundNumber` を追加(M3 着手 PR 内で消化予定)** `priority: medium`
  - **Why**: ADR-0006 §1.4 で「M3 着手 PR で `MaxRoundNumber` プロパティを `IGameConfig` に追加し、ゲーム終了判定に利用する」と確定済だった
  - **Done when** (resolved with alternate decision):
    - ✓ ADR-0010 §8 で「`MaxRoundNumber` は `IGameConfig` に追加しない、`DrowZzzClockConstants.MaxRoundNumber = 21` を維持(L2 = ドメイン上の真の不変量に分類、Clock 構造に紐づく)」と最終確定
    - ✓ 当初の Done when 4 項目(IGameConfig 追加 / CFG-102 採番 / StubGameConfig 追加 / M3 終了判定での参照)はすべて **不採用方向** で解消、ADR-0010 §「不採用案」表に「`IGameConfig.MaxRoundNumber` プロパティ追加」を記録
    - ✓ 結果がリポジトリに反映済み(`DrowZzzClockConstants.MaxRoundNumber` 維持、`IGameConfig` に追加なし)
  - **Related**: [ADR-0010 §8](adr/0010-m3-game-termination-and-victory-determination.md)、[ADR-0006 §1.4 / §7](adr/0006-m1-detail-application-interfaces.md)、M3-PR1 実装 PR #42 / 完成記録 PR #46
  - **Notes**: 当初 ADR-0006 で予約された方針を ADR-0010 で再評価 → 不採用に切替(L3 ゲームバランス調整可能値ではなく L2 構造的不変量と再分類)。本 TODO は「Done when の代替案達成」で完了済み(2026-05-13)

- [x] **早期勝利カードの仕様 JIT 確定待ち(M2-PR3+ で実装)** `priority: medium`
  - **Why**: ADR-0009 §5 で「早期勝利は『特定の効果タイプを持つカード』のプレイで起こる」と確定したが、カード ID / カード名 / 効果 record 名 / 効果フィールド / 効果意味は M2-PR3+ で JIT 共有
  - **Done when** (all met):
    - ✓ ADR-0010 §5 で `EarlyWinTriggerEffect : IEffect`(`Drowsy.Application.Games.DrowZzz.Effects`、引数なし record)を確定、`PlayCardAction.Apply` 内で `Clock.IsNight && TotalPoints[currentPlayer] >= EarlyWinScoreThreshold` を確認して `Outcome = WinnerOutcome` 設定
    - ✓ M3-PR1 完成 PR で `EarlyWinTriggerEffect` 効果 record 実装 + ゲーム終了判定統合
    - ✓ ADR-0011 §7 で「夢」カード(No.00)の効果列内に `KeywordedEffect([Frenzy, Instinct], EarlyWinTriggerEffect)` の形で統合、M3-PR6 で完成
    - ✓ EARS / .feature で要件 ID 採番(`docs/specs/games/drowzzz/cards/dream-card.md` 等)
  - **Related**: [ADR-0010 §5](adr/0010-m3-game-termination-and-victory-determination.md)、[ADR-0011 §7](adr/0011-m3-dream-card-and-game-mechanics-expansion.md)、M3-PR1 実装 PR #42 / 完成記録 PR #46、M3-PR6 実装 PR #57 / 完成記録 PR #58
  - **Notes**: 「持ち点 ≥ 100(`EarlyWinScoreThreshold`)」「Round 1〜16(`Clock.IsNight`)」「カードプレイ」の 3 条件は ADR-0010 §5 で確定通り。M3 完結済(CLAUDE.md §11 「M3:**完結**」)で本 TODO 解消(2026-05-13)

- [x] **`IRandomSource` の `DrowZzzRule` / `EndTurnAction` 経路への注入判断(M2-PR4 着手時に JIT)** `priority: medium`
  - **Why**: ADR-0009 §4 で確定した「DDP 抽選を `EndTurnAction.Apply` 内で行う」案は `IRandomSource` を `DrowZzzRule` または `EndTurnAction` の経路に注入する必要がある。ADR-0007 §3 で確定した「`DrowZzzRule` constructor 引数は `ICardCatalog<IEffect>` / `EffectInterpreter` のみ」を破壊する可能性があった
  - **Done when** (resolved with alternate decision):
    - ✓ M2-PR4 着手時の JIT で **案 D「`DdpPool.Shuffle(IRandomSource rng)` を `DdpPool` 値オブジェクト自身に注入」** を採用(当初挙げた案 A / B / C いずれも不採用)
    - ✓ `DrowZzzRule` constructor は ADR-0007 §3 の確定通り `ICardCatalog<IEffect>` / `EffectInterpreter` のみ(破壊なし、ADR-0007 改訂不要)
    - ✓ `IRandomSource` の利用箇所は `StartGameUseCase` のみに局所化(constructor 受け取り `StartGameUseCase.cs:43,50` + 先後決定の Fisher-Yates Shuffle `:72` + FDP 抽選 Shuffle `:75` + `DdpPool.Shuffle` 呼び出し `:124`)、`DdpPool.Shuffle(IRandomSource rng)` 自体は `DdpPool.cs:85` に実装
    - ✓ 結果がリポジトリに反映済み(M2-PR4 完成 PR)
  - **Related**: [ADR-0009 §4](adr/0009-m2-m3-dp-and-victory-conditions.md)、[ADR-0007 §3](adr/0007-m2-detail-card-effects.md)、M2-PR4 実装 PR #37 / 完成記録 PR #38
  - **Notes**: 「`DdpPool` 値オブジェクトに `IRandomSource` を注入する `Shuffle` メソッドを持たせる」設計が、当初 3 案より自然(値オブジェクト責務 + 純粋関数)。`DrowZzzRule` / `EndTurnAction` への `IRandomSource` 注入は **不要** となり、`StartGameUseCase` 起動時に `DdpPool` を Fisher-Yates Shuffle 済の状態で構築 → 以降は `DdpPool.Draw()` の純粋関数経由で抽選する設計に結実(2026-05-13 完了済み移動)

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
