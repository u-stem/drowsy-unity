# ADR-0008: M2 — DrowZzzClock 概念と「夜・朝」フェーズの導入

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-11 |
| Decider | プロジェクトオーナー |

## Context

ADR-0007 で M2(カード効果実装)の効果インフラ(`IEffect` / `EffectInterpreter` / `ICardCatalog<IEffect>` ジェネリック化)を確定し、M2-PR1 で骨格を完成させた。M2-PR2 以降の個別効果 record 着手前に、プロジェクトオーナーから DrowZzz の「ゲーム内時間」と「夜・朝」フェーズの仕様前提が共有された(JIT 共有、ADR-0007 §1.2 / §6 の運用継続)。

本 ADR は **M2 でのゲーム内時間 (Clock) 概念導入と「夜・朝」フェーズ判定の設計** を確定する。ADR-0006 §2.2 で確立された `DrowZzzGameSession.CurrentRound` 計算プロパティとの整合を保ちつつ、M2-PR3 以降の効果 record(夜だけ / 朝だけ発動するカード等)が参照可能な API を提供する。

ADR-0005 / ADR-0006 / ADR-0007 で既に決まっている前提:

- 縦串で本命ゲーム DrowZzz を直接実装、M1 完成 / M2 着手中(ADR-0005 / ADR-0007)
- N=2 想定(N>2 は Phase 3 候補、ADR-0006 §2.2 / §Negative)
- `TurnState.TurnNumber` はフェーズ番号(1 プレイヤー 1 アクション単位、`EndTurnAction.Apply` で +1、ADR-0006 §2.2 / §M1-PR6 で「サブターン」と呼ばれていた概念、ADR-0009 §「用語規約」で「フェーズ」へ刷新)
- 1 ターン = N フェーズ = 全プレイヤーが 1 巡(ADR-0006 §M1-PR2 では「1 ラウンド = N サブターン」と表記、ADR-0009 で用語刷新)
- `DrowZzzGameSession.CurrentRound` 計算プロパティが M1-PR2 で実装済(`(TurnNumber + 1) / 2`、N=2 専用、ADR-0006 §M1-PR2 / DZ-010)
- M2 効果は `PlayCardAction.Apply` 中に同期発動(ADR-0007 §3)
- 山札枯渇は現状想定下では発生しない(ADR-0007 §7、初期配布 10 + 総 Draw 42 = 52 ≤ 56、ADR-0009 起票時に Clock 21 ラウンド化に伴い再検算)

### プロジェクトオーナーから JIT 共有された仕様前提(2026-05-11)

| 項目 | 値 |
| ---- | ---- |
| ゲーム内時間の概念名 | 時計 (Clock) |
| 1 ターン = 1 ラウンド | 30 分 |
| 開始時刻 | 21:00 |
| 終了時刻 | 07:00(Round 21 開始時刻 = 最終プレイラウンドの時計表示。Round 21 完了直後にゲーム終了処理に入る) |
| プレイ可能ラウンド数 | **21**(Round 1〜21、各 30 分。当初共有「計 20 ターン」を ADR-0009 起票時に「21 ターン」へ訂正、§Implementation Notes §「ADR-0009 起票時の Clock 訂正」参照) |
| 夜の定義 | 21:00 〜 04:30(時刻区間、ラウンド 1〜16 が該当) |
| 朝の定義 | 05:00 〜 07:00(時刻区間、ラウンド 17〜21 が該当、Round 21 = 07:00 は最終プレイラウンドで `IsMorning = true`)|
| 時計値の最大 | 07:00(Round 21 開始時刻)。07:30 は時計仕様上存在しない(Round 22 への遷移は起こらず、ゲーム終了処理に入る、ADR-0009 §6 / 本 ADR §5) |
| 用途 | M2 以降のカード効果の発動条件(夜だけ / 朝だけ発動するカード等)|

検算:
- 21:00 + (k - 1) × 30 分 (mod 24) = ラウンド k の時刻
- ラウンド 1: 21:00 (夜) / ラウンド 16: 04:30 (夜の最終) / ラウンド 17: 05:00 (朝) / ラウンド 20: 06:30 (朝) / ラウンド 21: 07:00 (朝の最終プレイラウンド)
- ラウンド 21 完了直後: ゲーム終了処理に入る(時計値 07:30 は仕様上存在しない、ADR-0009 §6)

## Decision

### 1. DrowZzzClock 値オブジェクト(`sealed record DrowZzzClock(int RoundNumber)`)

namespace は `Drowsy.Application.Games.DrowZzz` に配置する(`DrowZzzGameSession` と同階層、DrowZzz 固有の概念のため汎用化はしない。汎用化の必要性が出るのは別ゲーム追加時、YAGNI、ADR-0007 §1.1 と同じ判断)。

```csharp
namespace Drowsy.Application.Games.DrowZzz;

// CLAUDE.md §9「マジックナンバー禁止」/「L1/L2 は `<Module>Constants` クラスの `const`」に従い、
// 数値リテラル(21 / 24 / 30 / 2 / 1 / 16 / 17 / 21)は DrowZzzClockConstants に切り出す
// (M2-PR2 実装時 code-reviewer 指摘反映で追加。起票時サンプルは literal 直書きだったが訂正)。
public static class DrowZzzClockConstants
{
    public const int StartHour         = 21;  // L2 ゲーム開始時刻 (24 時間制)
    public const int HoursPerDay       = 24;  // L1 24 時間制 mod
    public const int MinutesPerPhase   = 30;  // L2 1 フェーズあたりの分数
    public const int PhasesPerRound    = 2;   // L2 1 ターン = N=2 フェーズ
    public const int NightStartRound   = 1;   // L2 夜の開始ターン (21:00)
    public const int NightEndRound     = 16;  // L2 夜の終了ターン (04:30)
    public const int MorningStartRound = 17;  // L2 朝の開始ターン (05:00)
    public const int MorningEndRound   = 21;  // L2 朝の終了ターン (07:00、最終プレイ可能)
}

// 注: 起票時サンプルは `sealed record class` だったが、M2-PR2 実装時に C# 9 LangVersion
// (Drowsy.Application.csproj `<LangVersion>9.0</LangVersion>`)制約と既存
// `sealed record` パターン(DrowZzzGameSession / StartGameAction 等)整合のため
// `sealed record` に訂正(意味は同一、`record class` は C# 10 構文の冗長表記)。
public sealed record DrowZzzClock(int RoundNumber)
{
    /// <summary>時(0〜23、24 時間制 mod で正規化)</summary>
    public int Hour =>
        (DrowZzzClockConstants.StartHour
         + (RoundNumber - 1) / DrowZzzClockConstants.PhasesPerRound)
        % DrowZzzClockConstants.HoursPerDay;

    /// <summary>分(0 または MinutesPerPhase)</summary>
    public int Minute =>
        ((RoundNumber - 1) % DrowZzzClockConstants.PhasesPerRound)
        * DrowZzzClockConstants.MinutesPerPhase;

    /// <summary>夜判定(21:00 〜 04:30、ラウンド 1〜16)</summary>
    public bool IsNight =>
        RoundNumber
            is >= DrowZzzClockConstants.NightStartRound
            and <= DrowZzzClockConstants.NightEndRound;

    /// <summary>朝判定(05:00 〜 07:00、ラウンド 17〜21。Round 21 = 07:00 は最終プレイラウンド)</summary>
    public bool IsMorning =>
        RoundNumber
            is >= DrowZzzClockConstants.MorningStartRound
            and <= DrowZzzClockConstants.MorningEndRound;
}
```

設計指針:

- **正規化方針(プロジェクトオーナー JIT 確定)**: 24 時間制 mod 24 で表記する(例: ラウンド 7 = 24:00 ではなく 00:00 として `Hour=0 Minute=0`)。これは「24:30」のような一般的でない表記を避け、表示時の読みやすさを優先する判断。
- **状態は `RoundNumber` のみ**: `Hour` / `Minute` / `IsNight` / `IsMorning` は computed プロパティで導出する。複数の真の値が存在することによる不整合リスクを構造的に排除する(Parse-don't-validate と整合)。
- **定数集約 (CLAUDE.md §9)**: 数値リテラル 8 種は `DrowZzzClockConstants` static class の `public const int` に集約する(L1: `HoursPerDay`、L2: 残り 7 件)。Domain ではなく Application 層に配置するのは「夜は 21:00〜04:30」「21 ラウンド」が DrowZzz 固有のゲームルールで Domain ゲーム非依存原則(ADR-0002)と整合させるため(本 ADR §Alternatives「Clock を Domain 配下に配置」却下と同じ判断)。`RoundNumber - 1` の `1` のみ「1-indexed → 0-indexed の自明変換」として CLAUDE.md §9 自明リテラル例外を適用。
- **`record` + positional**: `RoundNumber` 1 つだけが positional 引数で、null 防御は不要(int の non-nullable で構造的に保証)。`with` 式は `RoundNumber` の書き換えに利用可。`record` キーワードは C# 9 で `record class` と同義(M2-PR2 実装時に LangVersion 9.0 制約と既存パターン整合で `sealed record` に統一)。
- **N=2 専用**: `Hour` / `Minute` の式は「1 ターン = N=2 フェーズ = 30 分」前提(ADR-0009 用語規約)。`PhasesPerRound` を const にしたが、N>2 拡張時は計算式そのもの(`(RoundNumber - 1) / PhasesPerRound` の意味付け)の再評価が必要(Phase 3 候補、ADR-0006 §Negative を継承)。

### 2. `DrowZzzGameSession` への組み込み(computed プロパティ)

`DrowZzzClock` は `DrowZzzGameSession` の **computed プロパティ**として提供する。session に独立な `Clock` フィールドは持たせない。

```csharp
// DrowZzzGameSession.cs に追加
public DrowZzzClock Clock => new DrowZzzClock(CurrentRound);
```

#### 案 X(computed)を採用した理由

| 観点 | 案 X(computed、採用)| 不採用案(positional フィールド)|
| ---- | ---- | ---- |
| 真の単一情報源 | `TurnState.TurnNumber` のみ | `TurnNumber` と `Clock.RoundNumber` の 2 箇所 |
| 不変条件 | 構造的に保証(`Clock.RoundNumber = CurrentRound`)| init setter で `Clock.RoundNumber == CurrentRound` を検証する必要あり |
| `StartGameUseCase` 変更 | 不要(Turn が初期化されれば Clock も自動)| `Clock = DrowZzzClock.Initial()` を明示的に設定が必要 |
| `EndTurnAction.Apply` 変更 | 不要(Turn が進めば Clock も自動)| ラウンド境界判定 + `Clock.Advance()` を明示的に呼ぶ必要あり |
| 既存 `CurrentRound`(DZ-010、ADR-0006)との重複 | なし(同じ式を Clock も使う)| あり |
| 将来「時計だけ進める」効果への拡張余地 | なし(positional フィールド化の破壊的変更が必要)| あり(現状想定外、YAGNI)|

**採用: 案 X**(computed プロパティ)。M2 範囲の仕様(「ターン = 時間」「ラウンド 1 増 = 30 分経過」の固定対応)では真の単一情報源を `TurnNumber` に保ち、Clock を派生表現とするのが筋。将来「時計だけ進める」効果が登場するまでは案 X を維持し、その時点で別 ADR で破壊的変更を判断する。

`DrowZzzGameSession.Equals` / `GetHashCode` は computed プロパティを比較対象に含めない(既存実装の `_gameState` / `_turnPhase` / `_firstDrowsyPoints` の同値比較のみで Clock も自動的に一致するため、二重カウント不要)。

### 3. 既存 `CurrentRound` プロパティの扱い

ADR-0006 §M1-PR2 / DZ-010 で導入された `DrowZzzGameSession.CurrentRound` は **変更しない**(後方互換維持)。

| 観点 | 扱い |
| ---- | ---- |
| `DrowZzzGameSession.CurrentRound` の式 | `(_gameState.Turn.TurnNumber + 1) / 2`(維持) |
| `DrowZzzClock.RoundNumber` との関係 | 同義(`session.Clock.RoundNumber == session.CurrentRound` が常に成立、Ubiquitous で表明) |
| 既存テスト(DZ-010、3 件) | 維持(`session.CurrentRound` を参照する経路はそのまま動く) |
| 既存 EARS(`docs/specs/games/drowzzz/skeleton.md` DZ-010) | 維持(M2-PR2 では更新せず、Clock 仕様は別 EARS `clock.md` で扱う) |
| 推奨される新規利用 | `session.Clock.RoundNumber`(Clock 由来の他プロパティと並列利用するため一貫性が高い) |

将来 `CurrentRound` を `Clock.RoundNumber` への薄いショートカット(`public int CurrentRound => Clock.RoundNumber`)に書き換える判断は本 ADR の範囲外。同義性が明示されている以上、リファクタは M2 / M3 内の機械的タスクとして `docs/todo.md` で追跡する(本 ADR 起票 PR 同梱、§Implementation Notes §「TODO 登録」参照)。

### 4. M2-PR2 スコープ(Clock 概念導入のみ)

ADR-0007 §M2-PR1 と同等の最小骨格 PR として M2-PR2 を切る。

| 観点 | M2-PR2 で扱う |
| ---- | ---- |
| `DrowZzzClock.cs` 新規作成 | ○ |
| `DrowZzzGameSession.Clock` computed プロパティ追加 | ○ |
| `Clock` の単体テスト(Hour / Minute / IsNight / IsMorning / `Equals` / `GetHashCode`) | ○ |
| `DrowZzzGameSession.Clock` の統合テスト(`Clock.RoundNumber == CurrentRound` 等)| ○ |
| EARS / Gherkin(`docs/specs/games/drowzzz/clock.{md,feature}` 新規) | ○ |
| `StartGameUseCase` / `DrowZzzRule` / `EndTurnAction` 本体実装の変更 | **不要**(Turn が進めば Clock も自動更新、案 X の利点) |
| Clock を参照する効果 record(`NightOnlyDrawEffect` 等)| **M2-PR3 以降**(1 PR = 1 effect record、ADR-0007 §6 継続) |
| `RoundNumber > 21` 到達時の挙動(IsTerminated / 例外 / EndTurn ガード)| **M3 範囲**(時計値 07:30 は仕様上存在しない、Round 21 完了でゲーム終了処理、§5 / ADR-0009 §6 を参照)|

### 5. `RoundNumber > 21` への振る舞い(時計仕様上存在しない、ゲームフロー上は到達しない)

**プロジェクトオーナー JIT 確定**(ADR-0009 起票時 2026-05-11 訂正反映):

- プレイ可能ラウンドは Round 1〜21(21 ラウンド)。Round 21 = 07:00 は最終プレイラウンド
- Round 21 完了直後にゲーム終了処理に入り、**時計値 07:30(= Round 22 相当)は仕様上存在しない**
- ゲーム終了処理(`IGameRule.IsTerminated` / `GetWinner` / `EndTurnAction.Apply` でのターン進行ガード)は **M3** で実装(ADR-0009 §6)
- M2-PR2 段階では Clock 自体は computed で TurnNumber から派生するため、テスト上 `RoundNumber > 21` を呼んでも数学的計算結果は返る(`Hour` / `Minute` は mod 24 で正常値、`IsNight` / `IsMorning` は両方 `false` の防御値)。これは M3 で IsTerminated がガードするまでの過渡的な挙動として許容する

不採用案:
- 案 A: `RoundNumber > 21` で `Clock` が例外を投げる → 本 ADR §2 採用案では `Clock` は computed プロパティで Advance メソッド自体がない、例外を投げる箇所がない
- 案 B: `RoundNumber == 21` で `Hour` / `Minute` が凍結 → 計算式が条件分岐で複雑化、テストでも特殊扱いが必要、M3 で IsTerminated が明示的にガードする設計と齟齬
- 案 C: `RoundNumber > 21` でも `IsMorning = true` を返す → 時計仕様上 07:30+ は存在しないため、計算結果として意味のある「朝」を返すと誤読を招く

### 6. 用語整理 — 「ターン」/ 「フェーズ」/ 「PhaseState」(ADR-0009 起票時にボードゲーム一般用語へ刷新)

ADR-0006 §M1-PR2 で「ラウンド = 全プレイヤー 1 巡、サブターン = 1 プレイヤー 1 アクション」と整理済だったが、ADR-0009 §「用語規約」でボードゲーム一般用語(Eurogame 寄り)に整理された。本 ADR §6 もこれに追随して更新する。

| 用語(ADR-0009 確定) | 値 | 実装名(現状) | 旧用語(ADR-0006)|
| ---- | ---- | ---- | ---- |
| **フェーズ**(1 プレイヤー 1 行動)| `TurnState.TurnNumber`(1〜42 を取る、N=2 × 21 ターン、ADR-0009 起票時に Clock 21 ターン化に伴い訂正)| `TurnState.TurnNumber`(Domain 汎用名、改名は別 ADR 候補)| サブターン |
| **ターン**(時計の進行単位、30 分)| `DrowZzzGameSession.CurrentRound` = `Clock.RoundNumber`(1〜21)| `CurrentRound` / `Clock.RoundNumber`(実装名は別 PR で `CurrentTurn` / `Clock.TurnNumber` への改名を `docs/todo.md` で追跡)| ラウンド |
| **PhaseState**(フェーズ内の待ち状態) | `DrowZzzGameSession.PhaseState`(WaitingForDraw / WaitingForPlay / WaitingForEndTurn)| ADR-0009 同 PR で `DrowZzzTurnPhase` → `DrowZzzPhaseState` にリネーム済 | (`DrowZzzTurnPhase` enum と同義、改名のみ) |
| ゲーム内 1 ターン = 30 分 | プロジェクトオーナー JIT 確定(2026-05-11) | 本 ADR §Context | — |

CLAUDE.md / コード内では現状「`RoundNumber` / `CurrentRound`」と書く(実装名のリネームは `docs/todo.md` で別 PR 追跡)。日本語仕様文書 / カード設計記述では「ターン / フェーズ / PhaseState」を使う(ADR-0009 用語規約に従う)。

### 7. Clock 同義性の Ubiquitous 表明

`session.Clock.RoundNumber == session.CurrentRound` は構造的に常に成立する(両者が同じ式 `(TurnNumber + 1) / 2` を経由する)。これを EARS [Ubiquitous] として明示する。

§4 のスコープ表に従い、本同義性は構造的保証 + regression guard としての確認テストの二段構えで担保する(`DrowZzzGameSessionTests` に DZ-097 として TurnNumber=1 / 31 / 41 → Round 1 / 16 / 21 の 3 ケース、M2-PR2 実装時に「[Ubiquitous] でもテスト存在は妨げない」「将来 `CurrentRound` を `Clock.RoundNumber` への薄いショートカットに置き換えるリファクタの regression guard として価値がある」と判断)。起票時表記「テスト免除する」は本 PR で訂正反映。

### 8. 効果 record が Clock を参照する API(M2-PR3 以降の前提のみ確定)

M2-PR3 以降で「夜だけ発動する効果」「朝だけ発動する効果」を実装する際、`EffectInterpreter.Apply(session, effect)` が `session.Clock.IsNight` / `session.Clock.IsMorning` を参照する。具体的な効果 record 設計(条件発動 record にラップする / 効果自身が判定する / ICardCatalog 側で条件付き返却する)は **個別効果着手時に JIT 判断** する(ADR-0007 §1.4 の他者影響系 actor 拡張案と同じ判断スタイル)。

本 ADR では Clock 値オブジェクトの API(IsNight / IsMorning)を提供することのみ確定する。

## Consequences

### Positive

- ゲーム内時間 / 夜・朝フェーズが値オブジェクト + computed で型安全に表現される
- 真の単一情報源(`TurnNumber`)を保つことで、Clock との不整合バグが構造的に発生しない
- M2-PR2 のスコープが最小(本体実装 2 ファイル + テスト 1 ファイル + EARS 2 ファイル、`StartGameUseCase` / `DrowZzzRule` / `EndTurnAction` の変更不要)
- 既存 `CurrentRound`(ADR-0006 / DZ-010)との後方互換が完全に維持される
- M2-PR3 以降の「夜カード / 朝カード」効果実装で `session.Clock.IsNight` 等を直接参照できる
- 24 時間制 mod 24 表記により Hour 表示が 0〜23 に正規化され、UI / デバッグ時の可読性が高い

### Negative

- `RoundNumber > 21` 到達時の防御を M3 まで残すため、誤って 22 ラウンド目を回そうとした場合 `Clock.IsNight` / `IsMorning` が両方 `false` を返す「夜でも朝でもない」状態が発生し得る
  - **緩和**: M3 で `IGameRule.IsTerminated` を実装し、`EndTurnAction.Apply` で Round 21 完了時にゲーム終了処理に入り Round 22 への遷移をガードする(本 ADR §5 / ADR-0009 §6)。M2 進行中はテストカバレッジで「RoundNumber=21 まで」を明示しておくことで挙動を可視化
- `DrowZzzClock` を computed プロパティとして取得するため、頻繁にアクセスすると毎回 `new DrowZzzClock(CurrentRound)` のアロケーションが発生する
  - **緩和**: M2 範囲(Pure C# / Application 層)では性能要件は皆無。M5 以降(Presentation / フレーム毎参照)で要計測、必要なら struct 化 / cached プロパティ化を別 ADR で判断
- `CurrentRound` と `Clock.RoundNumber` の 2 つの参照経路が残るため、Public API として「どちらを使うべきか」がコードベース上で完全には統一されない
  - **緩和**: 本 ADR §3 で「新規利用は `Clock.RoundNumber` 推奨」と明示。`CurrentRound` を Clock 経由のショートカットに置き換えるリファクタは `docs/todo.md` で追跡(将来 PR で機械的に実施)
- 「時計だけ進める」「特定ラウンドにジャンプする」効果は本 ADR の computed 設計では表現できない
  - **緩和**: 現状仕様で想定外(YAGNI)。仮にそのような効果が登場した時点で、Clock を positional フィールド化する破壊的変更を別 ADR で判断
- N>2 拡張時に `Hour` / `Minute` の計算式(N=2 前提)を再評価する必要がある
  - **緩和**: ADR-0006 §Negative で「N>2 拡張は Phase 3」と既に表明済、本 ADR も同方針を継承(`docs/todo.md` の既存エントリで追跡)

### Neutral

- ADR-0007 §8(M2 範囲外)の「勝敗判定 → M3」と整合しつつ、本 ADR では「Clock 進行は M2、ラウンド上限到達時の `IsTerminated` / `GetWinner` / `EndTurn` ガードは M3」と境界を細分化(M3 着手 ADR-0009 候補で扱う前提)
- Clock 仕様の EARS は新規ファイル `docs/specs/games/drowzzz/clock.md` に集約し、既存 `skeleton.md`(DZ-010)からは「Clock 仕様は別ファイル」と相互参照する
- Effect record が Clock を参照する具体的な API(条件発動 record ラップ vs 効果自身の判定 vs ICardCatalog 条件付き返却)は本 ADR で確定せず、最初の関連効果着手時に JIT 判断

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| Clock を Domain 配下 (`Drowsy.Domain.Game.GameClock`) に配置 | 「夜は 21:00〜04:30」「朝は 05:00〜07:00」「21 ラウンド」は DrowZzz 固有のゲームルールで Domain ゲーム非依存原則(ADR-0002)と整合しない、Premature Abstraction を避ける(別ゲーム追加時に汎用化判断、ADR-0007 §1.1 と同じ判断) |
| Clock を session の positional フィールド追加 | 真の情報源が `TurnNumber` と `Clock.RoundNumber` の 2 箇所に分散、init setter で不変条件検証が必要、`StartGameUseCase` / `EndTurnAction` の変更が必要、将来「時計だけ進める」効果(現状想定外)のための拡張余地は YAGNI |
| Clock 型を作らず Session に Hour / Minute / IsNight / IsMorning を直接 computed | 型抽象がなく、効果 record が Clock を引数として受け取る API 設計時に session の特定プロパティに依存することになる、概念の凝集が損なわれる |
| Clock を `record class DrowZzzClock(int Hour, int Minute)` で持つ | Hour / Minute が独立に変動できる印象を与え、「1 ラウンド = 30 分」の整数倍関係が暗黙化、計算式の単一性が崩れる |
| Clock を `record class DrowZzzClock(int ElapsedMinutes)` で持つ | ラウンド境界判定で `ElapsedMinutes / 30` のような変換が常に必要、ラウンド単位の効果実装で扱いにくい |
| 連続時刻表記(21:00 〜 30:30、24 を超える Hour 表記) | 「24:30」「30:30」は一般的な時刻表記として違和感、UI / デバッグ時の可読性が劣る、24 時間制 mod 24 で十分 |
| `RoundNumber > 21` で `Advance()` が例外 | 案 X(computed)では Advance メソッド自体が存在せず構造的に成立しない、M2 範囲では「進めるだけ」のシンプル方針が筋 |
| `RoundNumber == 21` で進まない(凍結)| 検出困難、M3 で IsTerminated が明示的に処理する設計と齟齬 |
| 「夜・朝」判定を Effect record 自身の条件式に持たせる(Clock 抽象を作らない)| 効果が複数登場するたびに同じ判定式が散らばる、DRY 違反、Clock 抽象を 1 箇所に集約する方が筋 |
| `IsNight` / `IsMorning` 以外に「夕方」「深夜」等の細分化を最初から導入 | 現状仕様で「夜と朝」のみ定義、Premature Abstraction、追加が必要になった時点で別 PR / 別 ADR で拡張 |
| `CurrentRound` を `Clock.RoundNumber` への薄いショートカットに置き換え(`public int CurrentRound => Clock.RoundNumber`)| 本 ADR と無関係の機械的リファクタ、別 PR で扱う方が PR 粒度として適切、`docs/todo.md` で追跡 |

## Implementation Notes

### M2-PR2 着手 PR の構成

1. **本 ADR-0008 起票 PR**(独立、本 PR):
   - `docs/adr/0008-m2-drowzzz-clock-and-night-morning.md` 新規
   - `docs/adr/README.md` のインデックスに 1 行追加
   - `CLAUDE.md` §11 の確立済 ADR 列に「ADR-0008(M2 詳細: DrowZzzClock 概念と『夜・朝』フェーズの導入)」を追加
   - `docs/todo.md` に「`CurrentRound` を `Clock.RoundNumber` 経由に整理(後方互換維持リファクタ)」TODO 追加

2. **M2-PR2 実装 PR**(本 ADR マージ後の別 PR):
   - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzClock.cs` 新規(`sealed record DrowZzzClock(int RoundNumber)` + 計算プロパティ、M2-PR2 実装時に `record class` から訂正)
   - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs` に `Clock` computed プロパティ 1 行 + XML doc 追加
   - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzClockTests.cs` 新規(Hour / Minute / IsNight / IsMorning / Equals / GetHashCode の単体テスト)
   - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzGameSessionTests.cs` に `Clock.RoundNumber == CurrentRound` 同義性テスト追加
   - `docs/specs/games/drowzzz/clock.{md,feature}` 新規(DZ-089〜 の連番採番)
   - `docs/specs/games/drowzzz/skeleton.md`(DZ-010、既存)に Clock との相互参照を追記

3. **M2-PR3 以降**(個別効果 record、ADR-0007 §6 継続):
   - 1 PR = 1 effect record、JIT 共有方式
   - Clock を参照する効果(夜だけ / 朝だけ発動)の最初の登場時に、効果 record と Clock 参照 API を JIT 確定

### 影響範囲(M2-PR2 で同時更新するファイル)

| ファイル | 変更 |
| ---- | ---- |
| `Application/Games/DrowZzz/DrowZzzClock.cs`(新規) | `sealed record DrowZzzClock(int RoundNumber)` + computed プロパティ(M2-PR2 実装時に `record class` から訂正) |
| `Application/Games/DrowZzz/DrowZzzGameSession.cs` | `public DrowZzzClock Clock => new DrowZzzClock(CurrentRound);` + XML doc |
| `Tests/Application.Tests/Games/DrowZzz/DrowZzzClockTests.cs`(新規) | 単体テスト |
| `Tests/Application.Tests/Games/DrowZzz/DrowZzzGameSessionTests.cs` | Session 経由テスト追加 |
| `docs/specs/games/drowzzz/clock.md`(新規) | DZ-089〜 |
| `docs/specs/games/drowzzz/clock.feature`(新規) | @DZ-089〜 |
| `docs/specs/games/drowzzz/skeleton.md` | DZ-010 セクションに Clock 相互参照を追記(任意、当該 PR で扱う) |
| `docs/todo.md` | TODO 追加(本 ADR 起票 PR 同梱、後述) |

### 要件 ID prefix(M2 範囲)

ADR-0007 §「要件 ID prefix」を継承:

| Prefix | 範囲 | 配置 |
| ---- | ---- | ---- |
| `DZ-` | DrowZzz 固有ルール(M2-PR2 では Clock 仕様 / 夜・朝判定 / Session.Clock 同義性) | `docs/specs/games/drowzzz/clock.md` |

M2-PR1 完成時点で APP- は 001〜038、DZ- は 001〜088 が連続採番済。M2-PR2 で `DZ-089〜` を `docs/specs/games/drowzzz/clock.md` に新規採番開始する。

### TODO 登録(本 ADR 起票 PR 同梱)

`docs/todo.md` に以下を追加:

- **`CurrentRound` を `Clock.RoundNumber` 経由に整理**(後方互換維持リファクタ):本 ADR §3 で「`CurrentRound` は維持」と確定したが、将来 `DrowZzzGameSession.CurrentRound => Clock.RoundNumber` の薄いショートカットに置き換えると概念が一本化される。M2 / M3 内のいずれかの PR で機械的リファクタを実施(N>2 対応とのタイミング次第)

### Phase 2 の進捗バナー更新(本 ADR 起票 PR では行わない)

ADR-0007 §「Phase 2 の進捗バナー更新」の運用に倣い、本 ADR 起票 PR では `README.md` / `CLAUDE.md` §11 の Phase 2 バナーは「M2 着手中(ADR-0007 起票済)」表示のまま維持し、CLAUDE.md §11 の確立済 ADR 列に「ADR-0008」を追加するのみとする。M2 完成 PR(M2-PR-N)で「M2 完成」に切り替える。

**M2-PR2 完成記録 PR での運用補足(2026-05-11 追記)**: M2-PR の単独 PR 完成時に `CLAUDE.md` §11 Phase 2 バナーを「M2 着手中」→「M2 進行中」のような事実反映(完成済 PR 番号の追記等)に更新するのは、本規約の「M2 完成への切り替え」には該当せず、事実に即した自然な進捗更新として許容する。Phase 2 バナーを「M2 完成」に切り替えるのは引き続き M2 最終 PR(M2-PR-N)のみ。

### ADR-0009 起票時の Clock 訂正(2026-05-11)

ADR-0009 起票時に、プロジェクトオーナーから以下の訂正が JIT 共有された:

| 訂正項目 | 旧 | 新 |
| ---- | ---- | ---- |
| プレイ可能ラウンド数 | 20(Round 1〜20)| **21**(Round 1〜21、Round 21 = 07:00 は最終プレイラウンド)|
| 朝の範囲(`IsMorning`) | Round 17〜20(05:00〜06:30)| **Round 17〜21(05:00〜07:00)** |
| Round 21 の扱い | 「ゲーム終了境界、`IsMorning = false`」 | **「最終プレイラウンド、`IsMorning = true`」** |
| Round 22 以上の扱い | 「M3 で IsTerminated がガード」 | **「時計値 07:30 は仕様上存在しない、Round 22 への遷移はゲームフロー上起こらない」** |

訂正の経緯: 当初共有「計 20 ターン」の解釈に齟齬があり、プロジェクトオーナーが「7 時もプレイは可能」と訂正したことで「プレイ可能ラウンド = 21」「Round 21 完了直後にゲーム終了処理」が確定。本 ADR の Decision を直接編集する形で訂正履歴を git 履歴で保持し、Status は `Accepted` のまま維持(ADR-0001 「合意後に書き換える」運用、ADR-0007 §Related 末尾を ADR-0008 起票 PR で訂正した先例と同パターン)。

### M2-PR2 完成記録(2026-05-11)

**完成 PR**: PR #33 `feat(app): DrowZzzClock 値オブジェクトと夜・朝フェーズ判定を実装 (M2-PR2 完成)`(merged `47a6947`、既存 ADR-0006 §M1 着手 PR 群と同じ表記スタイル)

#### Definition of Done 達成項目(本 ADR §4「M2-PR2 スコープ」表対応)

| スコープ項目 | 達成状況 |
| ---- | ---- |
| `DrowZzzClock.cs` 新規作成(Hour / Minute / IsNight / IsMorning computed) | ✓ |
| `DrowZzzGameSession.Clock` computed プロパティ追加(案 X) | ✓ |
| 単体テスト(Hour / Minute / IsNight / IsMorning / Equals / GetHashCode) | ✓ `DrowZzzClockTests` 20 件(Hour 5 / Minute 5 / IsNight 3 / IsMorning 3 / Round 22 防御 2 / Equals 2) |
| Session 経由統合テスト(`Clock.RoundNumber == CurrentRound` 等) | ✓ `DrowZzzGameSessionTests` DZ-097 3 件追加(合計 23 件、ターン総数 4 増) |
| EARS / Gherkin(`clock.{md,feature}` 新規、DZ-089〜) | ✓ DZ-089〜DZ-098 採番完了 |
| `StartGameUseCase` / `DrowZzzRule` / `EndTurnAction` 本体実装変更 | ✓ なし(案 X computed の利点を享受) |

#### 同 PR で追加した予定外の成果物

- **`DrowZzzClockConstants.cs`**(新規 8 `const`、L1: `HoursPerDay`、L2: 残り 7 件): プロジェクトオーナー指摘「マジックナンバーに近い状態」を受けて、`DrowZzzClock` 内の literal 8 種を CLAUDE.md §9「L1/L2 は `<Module>Constants` クラスの `const`」規約に整合する形で集約。当初の ADR §1 サンプル(literal 直書き)では §9 規約に乖離があった
- **`skeleton.md` DZ-010 関連節**:`clock.md` への相互参照 1 行追記

#### 同 PR で同梱した本 ADR 訂正(2 段階のレビューで段階的に反映)

1. **§1 サンプルコード訂正**: `sealed record class` → `sealed record`(C# 9 LangVersion 制約 + 既存 `sealed record` パターン整合、code-reviewer subagent Must Fix 指摘反映)
2. **§1 サンプルコード追記**: `DrowZzzClockConstants` 8 const ブロック + 設計指針「定数集約 (CLAUDE.md §9)」追加(プロジェクトオーナー指摘反映)
3. **§7 表記訂正**: 「テスト免除する」→「[Ubiquitous] 表明 + regression guard としてテスト実装」(§4 スコープ表との内部矛盾解消、code-reviewer Should Fix 指摘反映)
4. **§1 ヘッダ + §Implementation Notes 残留訂正**(本完成記録 PR 同梱、code-reviewer 第二レビュー指摘反映): §1 セクションヘッダ / §M2-PR2 着手 PR の構成 / §影響範囲 表に残っていた旧表記 `record class DrowZzzClock(int RoundNumber)` を `sealed record DrowZzzClock(int RoundNumber)` に統一(§Alternatives 表 L236/L237 の「不採用案として比較」用途の `record class` 記述は意図的に維持)

Status は `Accepted` のまま維持(ADR-0001 「合意後に書き換える」運用、訂正履歴は git 履歴で追跡)。

#### M2-PR2 進行中の学び(将来の参考)

- **`record class` vs `record`**: C# 9 LangVersion 制約下で `record class` キーワードは利用不可(C# 10 構文)。ADR 起票時のサンプルが C# 10 構文だった場合、実装時に C# 9 への翻訳 + ADR 訂正を同 PR で同梱する運用が ADR-0007 §Related 末尾訂正パターンと整合
- **マジックナンバー禁止(CLAUDE.md §9)の境界**: literal 直書きが許容されるのは「自明リテラル `0`/`1`/`-1`/`""`/`null`」のみで、L1(数学的不変量)/ L2(ドメイン不変量)はすべて `<Module>Constants` クラスの `const` 化が必須。ADR のサンプル表記が literal で許容しているように見えても、実装段階で再評価して `const` 化する必要がある。本 ADR は §1 サンプルコードを訂正する形で対応(2 段階目訂正)
- **二段階レビュー反映の有効性**: code-reviewer subagent(機械的観点)+ プロジェクトオーナー(規約遵守 / コンセプト観点)の二段で指摘を回収することで、CLAUDE.md / ADR 遵守の網羅性が上がる。一段目で構造的問題(C# 9 整合 / テスト網羅性)、二段目で規約乖離(§9 マジックナンバー)を解消する流れが効率的
- **記述 PR と実装 PR の分離による訂正コスト最小化**: 本 ADR は起票 PR(#31)と実装 PR(#33)を分離した結果、起票時の仕様前提誤り(20 ラウンド → 21 ラウンド、ADR-0009 起票時訂正)と実装制約誤り(`record class` → `record`、§1 サンプル literal → `const`)の両方を実装 PR で同梱訂正できた。後続 ADR でも同分離方針を継続

#### 後続(M2-PR3 以降)

ADR-0007 §6 / 本 ADR §8 の JIT 共有方式継続。Clock を参照する最初の効果 record(夜カード or 朝カード)登場時に、効果 record と Clock 参照 API を JIT 確定する。

### M2 全体の完成記録の追記タイミング(M2 最終 PR で実施)

M2-PR2 完成記録は §M2-PR2 完成記録(2026-05-11)に追記済(本 §直上)。M2 全体の完成記録(M2-PR3〜M2-PR-N の効果 record 群を含む)は **ADR-0007** の Implementation Notes に追記する(本 ADR ではない、ADR-0007 §M2 完成記録の追記タイミング 参照)。本 ADR は M2-PR2 単独 PR のスコープに閉じる。

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — Domain ゲーム非依存原則 / `record + init + with` Immutability パターン
- 前提: [ADR-0003 TODO 運用](0003-todo-operations.md) — 本 ADR §Implementation Notes §「TODO 登録」
- 前提: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — `record + init + with` パターンを `DrowZzzClock` でも利用
- 前提: [ADR-0005 Phase 2 Roadmap](0005-phase2-roadmap-drowzzz.md) — Phase 2 / M2 の位置づけ、ロジック先行 / Presentation 後回し
- 前提: [ADR-0006 M1 詳細](0006-m1-detail-application-interfaces.md) §2.2 / §M1-PR2 — `DrowZzzGameSession.CurrentRound` の式と N=2 専用性、本 ADR で Clock を載せる土台
- 前提: [ADR-0007 M2 詳細](0007-m2-detail-card-effects.md) §1.2 / §3 / §6 / §8 — M2 効果インフラ、JIT 共有方式、M2 範囲外リスト、本 ADR が「ラウンド進行は M2、上限到達は M3」と境界を細分化
- 関連: [`docs/specs/games/drowzzz/skeleton.md`](../specs/games/drowzzz/skeleton.md) — DZ-010 `CurrentRound` 既存仕様、M2-PR2 で Clock との相互参照を追記
- 関連: [`docs/specs/games/drowzzz/clock.md`](../specs/games/drowzzz/clock.md) — M2-PR2 で新規作成、DZ-089〜
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §5 アーキテクチャ依存ルール / §6 テスト方針 / §11 ADR 運用 / §12 TODO 追跡
- 後続: M2-PR2(本 ADR の核心実装、`DrowZzzClock` + `DrowZzzGameSession.Clock` computed)
- 後続: M2-PR3 以降(個別効果 record、Clock を参照する効果の登場時に JIT 共有で API 確定)
- 後続: [ADR-0009 M2-M3 — DP 機構と勝利条件](0009-m2-m3-dp-and-victory-conditions.md)(本 ADR の境界訂正を §「ADR-0008 訂正項目」で同梱)
- 後続: ADR-0010 候補(M3 詳細、起票予定 — `IsTerminated` / `GetWinner` の本格実装、ラウンド上限到達時のゲーム終了処理ガード、Round 22 への遷移を防ぐ EndTurnAction の M3 拡張)
