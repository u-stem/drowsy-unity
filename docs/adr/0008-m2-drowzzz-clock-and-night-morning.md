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

### 1. DrowZzzClock 値オブジェクト(`record class DrowZzzClock(int RoundNumber)`)

namespace は `Drowsy.Application.Games.DrowZzz` に配置する(`DrowZzzGameSession` と同階層、DrowZzz 固有の概念のため汎用化はしない。汎用化の必要性が出るのは別ゲーム追加時、YAGNI、ADR-0007 §1.1 と同じ判断)。

```csharp
namespace Drowsy.Application.Games.DrowZzz;

public sealed record class DrowZzzClock(int RoundNumber)
{
    /// <summary>時(0〜23、24 時間制 mod 24 で正規化)</summary>
    public int Hour => (21 + (RoundNumber - 1) / 2) % 24;

    /// <summary>分(0 または 30)</summary>
    public int Minute => ((RoundNumber - 1) % 2) * 30;

    /// <summary>夜判定(21:00 〜 04:30、ラウンド 1〜16)</summary>
    public bool IsNight => RoundNumber is >= 1 and <= 16;

    /// <summary>朝判定(05:00 〜 07:00、ラウンド 17〜21。Round 21 = 07:00 は最終プレイラウンド)</summary>
    public bool IsMorning => RoundNumber is >= 17 and <= 21;
}
```

設計指針:

- **正規化方針(プロジェクトオーナー JIT 確定)**: 24 時間制 mod 24 で表記する(例: ラウンド 7 = 24:00 ではなく 00:00 として `Hour=0 Minute=0`)。これは「24:30」のような一般的でない表記を避け、表示時の読みやすさを優先する判断。
- **状態は `RoundNumber` のみ**: `Hour` / `Minute` / `IsNight` / `IsMorning` は computed プロパティで導出する。複数の真の値が存在することによる不整合リスクを構造的に排除する(Parse-don't-validate と整合)。
- **`record class` + positional**: `RoundNumber` 1 つだけが positional 引数で、null 防御は不要(int の non-nullable で構造的に保証)。`with` 式は `RoundNumber` の書き換えに利用可。
- **N=2 専用**: `Hour` / `Minute` の式は「1 ターン = N=2 フェーズ = 30 分」前提(ADR-0009 用語規約)。N>2 拡張時は計算式の再評価が必要(Phase 3 候補、ADR-0006 §Negative を継承)。

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

`session.Clock.RoundNumber == session.CurrentRound` は構造的に常に成立する(両者が同じ式 `(TurnNumber + 1) / 2` を経由する)。これを EARS [Ubiquitous] として明示し、テスト免除する。

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
   - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzClock.cs` 新規(`record class DrowZzzClock(int RoundNumber)` + 計算プロパティ)
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
| `Application/Games/DrowZzz/DrowZzzClock.cs`(新規) | `record class DrowZzzClock(int RoundNumber)` + computed プロパティ |
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

### ADR-0009 起票時の Clock 訂正(2026-05-11)

ADR-0009 起票時に、プロジェクトオーナーから以下の訂正が JIT 共有された:

| 訂正項目 | 旧 | 新 |
| ---- | ---- | ---- |
| プレイ可能ラウンド数 | 20(Round 1〜20)| **21**(Round 1〜21、Round 21 = 07:00 は最終プレイラウンド)|
| 朝の範囲(`IsMorning`) | Round 17〜20(05:00〜06:30)| **Round 17〜21(05:00〜07:00)** |
| Round 21 の扱い | 「ゲーム終了境界、`IsMorning = false`」 | **「最終プレイラウンド、`IsMorning = true`」** |
| Round 22 以上の扱い | 「M3 で IsTerminated がガード」 | **「時計値 07:30 は仕様上存在しない、Round 22 への遷移はゲームフロー上起こらない」** |

訂正の経緯: 当初共有「計 20 ターン」の解釈に齟齬があり、プロジェクトオーナーが「7 時もプレイは可能」と訂正したことで「プレイ可能ラウンド = 21」「Round 21 完了直後にゲーム終了処理」が確定。本 ADR の Decision を直接編集する形で訂正履歴を git 履歴で保持し、Status は `Accepted` のまま維持(ADR-0001 「合意後に書き換える」運用、ADR-0007 §Related 末尾を ADR-0008 起票 PR で訂正した先例と同パターン)。

### M2 完成記録の追記タイミング(後続 PR で実施)

本 ADR-0008 でも ADR-0007 と同じく、起票 PR では Implementation Notes §M2-PR2 着手記録は **空欄のまま**。M2-PR2 完成時 / M2-PR-N(M2 最終 PR)で完成日 / Definition of Done 達成方法 / 最終的な PR 群一覧を本 ADR に追記する(ADR-0007 §M2 完成記録の追記タイミング と同パターン)。

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
