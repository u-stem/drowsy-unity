# ADR-0005: Phase 2 Roadmap — DrowZzz の段階的縦串実装

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-10 |
| Decider | プロジェクトオーナー |

## Context

Phase 1 完結により、Domain は「ルールロジックを含まない、純粋な状態モデル + 状態遷移関数」として完成し、全 9 クラスで C0 100% を達成した(ADR-0002 / PR #18 / PR #19 でクロージャ済)。Phase 2 では本命ゲーム **DrowZzz** を Domain を活用してフルスタック実装するフェーズに入る。

### DrowZzz の輪郭(プロジェクトオーナーから共有)

| 項目 | 内容 |
| ---- | ---- |
| ジャンル | カードを使うボードゲーム |
| プレイヤー数 | 現状 N=2(将来 N>2 拡張を視野) |
| コアメカニクス | カードを引いて場に出す → 出したカードの効果によって何かが起きる |
| 勝敗条件 | 複雑(本 ADR の範囲外、M3 で仕様化) |
| 進め方の方針 | **ロジック先行**(Application 層 / ルール / 効果 / 勝敗を先に完成、Presentation は後で最小限) |
| 既存資産 | ルールメモが存在(コード化されていない、体系化もこれから) |
| 規模 | 長期プロジェクト |

### Phase 2 着手前に必要な意思決定

1. **練習用ゲーム(神経衰弱等)を挟むか、本命に直接向かうか**
2. **リポジトリ戦略**(同 repo / 別 repo / モノレポ)
3. **タイトル公開の範囲**(`DrowZzz` をそのまま使うか、コードネームか)
4. **段階的進め方の単位**(マイルストーン分割 / 各 M の完了基準)
5. **ルールメモの体系化方針**(EARS / Gherkin 化のタイミング)

これらが未決の状態で個別 PR を進めると、後から大きな refactor が発生するリスクがある。Phase 1 で ADR-0002 が果たした役割と同様、Phase 2 全体のスコープを ADR で固める必要がある。

## Decision

### 1. 練習用ゲームを挟まず DrowZzz を直接縦串実装

**本命構想が具体化済み**(プロジェクトオーナーが「具体的なルール / スコープあり」と明示)であるため、神経衰弱等の練習用ゲームを Phase 2 に挟まない。Domain は Phase 1 で「どんなカードゲームでも使える汎用語彙」として設計済みのため、本命ゲームを直接実装しても Domain 自体は変更されない。Application 層以上の実装はゲームごとに別ファイル / 別 namespace で並べられるため、将来別ゲームを追加する場合も既存 DrowZzz 実装は temporary asset にならない。

### 2. リポジトリ戦略: drowsy-unity 同居

DrowZzz と汎用エンジン基盤を **同 `drowsy-unity` リポジトリに同居** させる。別 repo / モノレポ / UPM 分離は採らない。

- 個人開発・1 ゲーム特化のデファクト
- 「複数ゲームに転用」「エンジンを OSS 化 / 商品化」は本 ADR 時点で予定なし(YAGNI)
- 将来エンジン分離が必要になった時点で別 ADR を起票してリファクタする道は残す

### 3. 名前空間と配置の階層化

汎用 interface(Phase 2 で導入)と DrowZzz 固有実装を namespace で分離する。

| 場所 | 役割 |
| ---- | ---- |
| `Drowsy.Domain.*`(既存、Phase 1 完結) | 集約モデル(本 PR では変更しない) |
| `Drowsy.Domain.Games.DrowZzz.*` | DrowZzz 固有の Domain 値オブジェクト(必要時のみ、原則 Application 層で吸収) |
| `Drowsy.Application` | ゲーム非依存 interface 群(`IGameRule` / `IGameAction` / `ICardCatalog` など)+ 共通 UseCase。**汎用 interface か DrowZzz 固有かの線引きは ADR-0006 で確定する**(本 ADR は配置先のみ規定) |
| `Drowsy.Application.Games.DrowZzz.*` | DrowZzz の `IGameRule` / `IGameAction` 具体実装、DrowZzz 固有 UseCase |
| `Drowsy.Infrastructure.Games.DrowZzz.*` | `ScriptableObjectCardCatalog` (DrowZzz 用 CardData 群)、永続化 |
| `Drowsy.Presentation.Games.DrowZzz.*` | DrowZzz の View / Presenter |
| `docs/specs/games/drowzzz/` | DrowZzz の EARS / Gherkin |
| `docs/games/drowzzz/`(任意) | DrowZzz のルール解説 / 設計判断(ADR で扱わない範囲、必要に応じて) |

### 4. タイトル `DrowZzz` を Public のまま使う

Public リポジトリ `drowsy-unity` の ADR / コード / spec で `DrowZzz` を固有名詞として使う(コードネーム置換は採らない)。

- プロジェクトオーナーが Public 公開を選択
- **タイトル Public 化のリスク評価**: 現時点ではルールメモのみ存在し、世界観 / アート / 個別カードのフレーバーテキストなど機密性の高い情報はリポジトリ内に未配置。タイトル名 `DrowZzz` のみを Public に晒すことによる競合リスクは許容できると判断
- 「世界観 / アート方向性 / マネタイズ戦略」など Public 化に違和感がある詳細は **本 ADR の範囲外** とし、ADR-0001 が定めた識別子規約・Public/Private 境界規約に準じて将来扱う(ADR-0002 ではなく ADR-0001 が境界規約の根拠)

### 5. マイルストーン分割

段階的縦串。各マイルストーンで動く MVP を出す。1 マイルストーン = 複数 PR(1 PR = 1 論理変更を維持)。

| # | スコープ | 完了基準 | 主な対象 layer |
| ---- | ---- | ---- | ---- |
| **M1** | ターン進行 + カードプレイの最小骨格 | GameState を入力に「次の合法な操作」が決定できる、N=2 で初期配布 → カードプレイ → ターン交代が動く | Application(`IGameRule` / `IGameAction` 確定、UseCase 3 つ) |
| **M2** | カード効果の段階的実装 | ルールメモの主要効果カテゴリ(ドロー / 場操作 / 相手影響等)が動く、1 PR = 1 効果カテゴリ | Application + Infrastructure(`ICardCatalog` SO 化) |
| **M3** | 勝敗判定 | ルールメモの勝敗条件が EARS 化済、`IGameRule.IsTerminated` / `GetWinner` で勝者確定 | Application |
| **M4** | Infrastructure 永続化 | GameState のセーブ / ロードが動く(JSON ベース) | Infrastructure |
| **M5** | Bootstrap + Presentation 最小 | Unity Play モードで DrowZzz が遊べる(最小 UI / コンソール出力可)、VContainer LifetimeScope + UniTask + R3 を実利用 | Bootstrap + Presentation |
| (M6) | N>2 拡張 / UI 本実装 / 世界観統合 | **Phase 2 スコープ外**(本 ADR では着手判断しない、Phase 2 完了後に Phase 3 として別 ADR で再評価) | 全層 |

各 M の **詳細 interface / API** は当該 M 着手前に専用 ADR(ADR-0006 以降)で確定する。本 ADR-0005 は Phase 2 全体のスコープを固めるメタ ADR と位置付ける。

### 6. ルール最適化と並行 EARS 化

ルールメモは現状コード化 / 体系化されていない。Phase 2 進行中に以下のサイクルで段階的に EARS / Gherkin 化する:

1. 各 PR で対応するルール部分を `docs/specs/games/drowzzz/<feature>.{md,feature}` に EARS / Gherkin 化
2. 実装中にルール最適化が必要なら同 PR 内で spec も更新する
3. 「動かしてみて変える」を前提に、過剰な抽象化は避ける(YAGNI)
4. ルール最適化に伴う既存 EARS の変更は通常通り PR で履歴を残す(spec 自体が版管理対象)

### 7. Phase 2 完了の最小定義

DrowZzz が以下の状態に達したとき Phase 2 完了とする:

- [x] N=2 で起動できる(M5-PR4 で Bootstrap が `PlayerRoster` 経由で N=2 ホットシートを構築、ADR-0017 PlayerRoster wrapper で VContainer 統合確立)
- [x] ルールメモの主要部分が動作する(M1 + M2 + M3 完了相当 — M1 ターン進行、M2 効果インフラ + サブセット 3 種、M3 終了判定 + 連想・放棄・ベッド破損・キーワード能力・夢カード)
- [x] 勝敗判定が出る(M3-PR1 終了判定 + `GameOutcome` `WinnerOutcome`/`DrawOutcome`、M5-PR7 で `RenderOutcome` UI 反映)
- [x] 永続化が動く(M4 完了相当 — M4-PR5 `DrowZzzGameSession` JSON 永続化 + M4-PR6 `IUserSettings` PlayerPrefs、M5-PR5 で Auto-save 統合、ADR-0018 `CardIdJsonConverter` schema 更新)
- [x] Unity Play モードで人間が操作できる(M5 完了相当、最小 UI で OK — M5-PR3 UI Toolkit UXML/USS、M5-PR4 Render 本実装、M5-PR6 設定 UI、M5-PR7 GameOutcome 表示。PR #95 ADR-0017 / PR #96 ADR-0018 で Play 動作の根本問題を解消、M5-PR8 で WebGL Build `Result: Success` も確認)

→ **5 軸全達成、Phase 2 完結**(M5-PR8 = 2026-05-16、ADR-0016 §11 完成記録に詳細)。

複数モード / N>2 / 本格 UI / アート / マネタイズは **Phase 3 以降** とする(ADR-0005 では確定しない)。Phase 3 ロードマップは別 ADR(候補 ADR-0019)で起票予定。

## Consequences

### Positive

- 動くものが早く出る(M1 で最小縦串が成立)
- Domain の汎用性が DrowZzz 実装で実証される
- ルール最適化と実装が並行できる(段階的縦串の利点)
- 別 repo 分離 / コードネーム化の前倒しコストを避ける
- Application 層 interface が「実際の必要性」から育つ(Premature Abstraction を避ける)
- Phase 2 着手前にスコープが固まり、各 M ごとに完了判断が明確

### Negative

- 汎用エンジン分離が将来必要になった時点でリファクタコストが発生する(必要なら別 ADR で判断)
- DrowZzz 固有実装が Application / Infrastructure / Presentation に増えると assembly が肥大化する可能性
  - **緩和**: `*.Games.DrowZzz.*` サブネームスペースで物理的に分離、ファイル単位で見通せる
- ルールメモの段階的 EARS 化により Phase 2 進行中に spec が頻繁に更新される
  - **緩和**: 各 PR で対応部分のみ EARS 化、全体仕様化を後回しにしない / トレーサビリティスクリプトで未対応 ID を機械検出
- DrowZzz が Public で開発されるため、世界観 / アート / 商標等は別途 Private 領域での扱いを判断する必要

### Neutral

- M1 詳細(IGameRule / IGameAction の interface 形)は本 ADR では確定せず、ADR-0006 で扱う
- Phase 2 完了後の Phase 3 範囲(N>2 / 本格 UI / 演出 等)は本 ADR の射程外

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| 練習用シンプルゲーム(神経衰弱等)を先に作って本命へ | 本命構想が具体化済みで refactor 負債が出るだけ。Domain は変わらず Application 層はゲームごとに別実装が自然なため練習挟みのメリットが薄い |
| 横串で Application interface を先に整備 (Application 層 interface だけ作る) | 「動くもの」が遅れ interface 設計が空想的になる(Premature Abstraction)。実装から育つ方が筋が良い |
| `drowsy-unity` をエンジン専用に分離し DrowZzz を別 repo に | 個人開発・1 ゲーム特化での過剰分離、UPM パッケージ化のメンテコスト高、複数ゲーム転用予定なし |
| モノレポ(`engine/` + `games/drowzzz/`) | 中規模以上の studio 向け、現状規模で過剰、Unity プロジェクト構造とも合わない |
| 全機能一気実装(段階的でない縦串) | プロジェクトオーナー方針に反する、PR 粒度制御困難、ルール最適化余地が消える |
| `DrowZzz` をコードネーム `GameA` で進める | プロジェクトオーナーが `DrowZzz` そのままを選択、リリース時の置換コストも省ける |
| Presentation を M1 の直後に入れる(見える MVP を先に) | ロジック先行というプロジェクトオーナー方針に反する、ルール最適化が未熟なまま UI 投資すると refactor 負債が大きい |

## Implementation Notes

### M1 着手前: ADR-0006 を起票

M1 (ターン進行 + カードプレイの最小骨格) の詳細は ADR-0006 で確定する。少なくとも以下を扱う:

- `IGameRule` interface の最小 API(C# 表記。例: `bool IsLegalMove(GameState state, IGameAction action)` / `GameState Apply(GameState state, IGameAction action)` 等)
- `IGameAction` の表現(ポリモーフィック record 階層 / Discriminated Union 風など)
- `ICardCatalog` の最小 API(M2 で SO 化、M1 では in-memory でも可)
- 共通 UseCase 構成(`StartGameUseCase` / `PlayCardUseCase` / `EndTurnUseCase`)
- 依存性注入の具体方針(Phase 2 では VContainer の Bootstrap は M5 まで遅らせ、M1〜M4 は Pure C# でテスト可能な構造にする)

### Phase 2 進行中の TODO 追跡

ADR-0003 で確立した `docs/todo.md` 運用に従い、Phase 2 で発生する後追い chore は TODO エントリ化する。Phase 1 完結時点で残る TODO(Roslynator 不整合 / NRT 検討)も Phase 2 中の任意タイミングで消化する。

### ルールメモの取り扱い

ルールメモは現状ローカル(リポジトリ内に未配置)。Phase 2 進行中に以下のいずれかで体系化:

- 各 M の作業中に対応部分を `docs/specs/games/drowzzz/<feature>.md` に EARS 化(段階的)
- 必要なら `docs/games/drowzzz/rule-overview.md`(spec ではなく解説資料)を作成

ルールメモを Public リポジトリに直接コピーする前に、Public 化に違和感がある内容(世界観 / 個別カードのフレーバーテキスト等)を選別する一手間が必要(都度判断)。

**運用ルール**: 各 M 着手時(または M 内の各 PR 着手時)に「対応するルールメモ部分を EARS 化済か」を確認し、未対応分は当該 PR で `docs/specs/games/drowzzz/<feature>.{md,feature}` に体系化する。EARS 化が完了したルール部分は、ローカルメモから該当箇所を削除または「(Spec 化済: <ファイルパス>)」と記録する(同じ内容を 2 箇所に持たない、Single Source of Truth)。Phase 2 完結時点でローカルメモ側に残るのは EARS 化対象外の機密情報(世界観 / アート / フレーバー)のみ、という状態を目指す。

### ステータス更新

本 ADR-0005 を Accepted で起票した時点で、README ステータスバナーを「Phase 1 完結 / Phase 2 着手前」 → 「Phase 1 完結 / Phase 2 計画確定(ADR-0005)、M1 着手前」 に更新する(本 PR 同梱)。

### M1 完成記録(2026-05-11、ADR-0006 経由で達成)

ADR-0006 の M1 着手 PR 群(M1-PR1〜PR7、PR #22〜#28)を順次マージし、本 ADR §M1 の Definition of Done を達成した。

| Definition | 達成方法 / PR |
| ---- | ---- |
| GameState を入力に「次の合法な操作」が決定できる | `DrowZzzRule.IsLegalMove` の switch 4 種(StartGame / Draw / Play / EndTurn)、M1-PR3〜PR6 |
| N=2 で初期配布 → カードプレイ → ターン交代が動く | `StartGameUseCase` (M1-PR3) + `ApplyActionUseCase` (M1-PR6) を Draw → Play → EndTurn で連鎖、M1-PR7 統合テストで end-to-end 検証 |
| Application 層 interface 確定 (`IGameRule` / `IGameAction` / `ICardCatalog`) | 全て M1-PR1 で確定 |
| UseCase 構成 (`StartGameUseCase` + `ApplyActionUseCase` のハイブリッド) | M1-PR3 + M1-PR6 |

完成時点の数値: NUnit 334 件全緑(Domain 205 + Application 129)、Application C0 100%、EARS 235 件、ADR 6 件。

次の段階: **M2(カード効果実装)** 着手前に新規 ADR(ADR-0007 等)で M2 スコープを確定する(ADR-0006 §M1 着手 PR 群と同じ手順)。

### 段階的縦串と PR 粒度の両立

段階的縦串は「マイルストーンごとに動く MVP」を出すアプローチだが、各 M を 1 PR で実装するわけではない。1 PR = 1 論理変更の規約に従い、各 M は複数 PR(目安 3〜10 PR)に分割する。M 内の PR 粒度は ADR-0006 以降で具体化する。

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — Phase 1 で確立した Domain を Phase 2 で活用する
- 前提: [ADR-0003 TODO 運用と docs/todo.md の新設](0003-todo-operations.md) — Phase 2 進行中の chore 追跡
- 前提: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — record + init + with パターンを Phase 2 で本格利用
- 後続: ADR-0006(M1 詳細、起票予定)
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §5 アーキテクチャ依存ルール / §6 テスト方針 / §11 ADR 運用 / §12 TODO 追跡
- 関連: 本 PR で `README.md` ステータスバナー / ADR インデックス / `docs/adr/README.md` / CLAUDE.md §11 を同時更新
