# DrowZzz Clock (M2-PR2)

DrowZzz のゲーム内時間 `DrowZzzClock` 値オブジェクトと、「夜・朝」フェーズ判定 (`IsNight` / `IsMorning`) を提供する機能。

## 概要

ADR-0008 の決定に基づき、`Drowsy.Application.Games.DrowZzz` namespace に `record class DrowZzzClock(int RoundNumber)` を追加し、`DrowZzzGameSession.Clock` を `new DrowZzzClock(CurrentRound)` を返す computed プロパティとして組み込む。

- 真の単一情報源は `TurnState.TurnNumber`(`Clock.RoundNumber` / `DrowZzzGameSession.CurrentRound` は同じ式 `(TurnNumber + 1) / 2` から派生)
- 24 時間制 mod 24 表記、開始 21:00 / 1 ターン = 30 分 / 全 21 ターン
- 夜 = Turn 1〜16(21:00〜04:30)、朝 = Turn 17〜21(05:00〜07:00)
- 用途は M2-PR3 以降の効果 record(夜だけ / 朝だけ発動するカード等)の発動条件

「ターン」「フェーズ」「PhaseState」の用語規約は ADR-0009 §「用語規約」に従う。実装名は現状 `RoundNumber` / `CurrentRound` を維持し(後方互換)、`Clock.TurnNumber` / `CurrentTurn` への改名は `docs/todo.md` で別 PR 追跡。

## 普遍要件 (Ubiquitous)

- [DZ-089] [Ubiquitous] The `DrowZzzClock` shall be a `sealed record class` declared in the `Drowsy.Application.Games.DrowZzz` namespace with `RoundNumber` (`int`) as the sole positional parameter.
- [DZ-090] [Ubiquitous] The `DrowZzzClock.Hour` shall be a computed property defined as `(21 + (RoundNumber - 1) / 2) % 24`.
- [DZ-091] [Ubiquitous] The `DrowZzzClock.Minute` shall be a computed property defined as `((RoundNumber - 1) % 2) * 30`.
- [DZ-092] [Ubiquitous] The `DrowZzzClock.IsNight` shall be a computed property returning `RoundNumber is >= 1 and <= 16`.
- [DZ-093] [Ubiquitous] The `DrowZzzClock.IsMorning` shall be a computed property returning `RoundNumber is >= 17 and <= 21`.
- [DZ-097] [Ubiquitous] The `DrowZzzGameSession.Clock` shall return `new DrowZzzClock(CurrentRound)` such that `session.Clock.RoundNumber == session.CurrentRound` holds structurally.

## 事象駆動要件 (Event-driven)

- [DZ-094] When `DrowZzzClock.Hour` and `DrowZzzClock.Minute` are evaluated at the round boundaries (Round 1 / 16 / 17 / 21), they shall yield the time values 21:00 / 04:30 / 05:00 / 07:00 respectively.
- [DZ-095] When `DrowZzzClock.IsNight` is evaluated at the night-side boundaries, it shall return `true` for Round 1 and Round 16, and `false` for Round 17.
- [DZ-096] When `DrowZzzClock.IsMorning` is evaluated at the morning-side boundaries, it shall return `true` for Round 17 and Round 21, and `false` for Round 16.

## 状態駆動要件 (State-driven)

- [DZ-098] While `RoundNumber > 21` is observed (out of the playable round range; reachable only as a defensive value before M3 introduces the `IsTerminated` guard, ADR-0008 §5 / ADR-0009 §6), both `DrowZzzClock.IsNight` and `DrowZzzClock.IsMorning` shall return `false`.

## MC/DC ケース表

`DrowZzzClock.IsNight` および `DrowZzzClock.IsMorning` は単一の `RoundNumber` から導出され複合論理条件ではないため MC/DC ケースは不要(`is >= X and <= Y` は単一プロパティの範囲判定で 2 つの境界値テストで網羅できる、Phase 1 `Pile` の境界値テストと同じ判断軸)。

## 定数依存

CLAUDE.md §9「マジックナンバー禁止」/「L1/L2 は `<Module>Constants` クラスの `const`」に従い、本機能が依存する定数を `DrowZzzClockConstants` に `const` として集約する(本 PR 内で対処、ADR-0008 §1 サンプル訂正同梱)。

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| 開始時刻 21:00 | L2 | `DrowZzzClockConstants.StartHour = 21`(ADR-0008 §Context、ゲーム開始時刻はゲームの真の不変量) |
| 1 ターンあたりのフェーズ数 = 2 | L2 | `DrowZzzClockConstants.PhasesPerRound = 2`(1 ターン = N=2 フェーズの構造的整数倍関係、N>2 拡張は Phase 3 候補) |
| 1 フェーズあたりの分数 = 30 | L2 | `DrowZzzClockConstants.MinutesPerPhase = 30`(1 ターン = 30 分の Minute 桁) |
| 24 時間制 mod 24 | L1 | `DrowZzzClockConstants.HoursPerDay = 24`(時間表記の数学的不変量) |
| 夜の範囲下限 1 | L2 | `DrowZzzClockConstants.NightStartRound = 1`(21:00 = Round 1) |
| 夜の範囲上限 16 | L2 | `DrowZzzClockConstants.NightEndRound = 16`(ADR-0009 §「Clock 仕様の境界訂正」、21:00〜04:30 を表現するターン境界) |
| 朝の範囲下限 17 | L2 | `DrowZzzClockConstants.MorningStartRound = 17`(ADR-0009 §「Clock 仕様の境界訂正」、05:00) |
| 朝の範囲上限 21 | L2 | `DrowZzzClockConstants.MorningEndRound = 21`(ADR-0009 §「Clock 仕様の境界訂正」、07:00 は最終プレイ可能ターン) |

`RoundNumber - 1` の `1` literal は「1-indexed → 0-indexed の自明変換」として CLAUDE.md §9 自明リテラル例外(`0`/`1`/`-1`/`""`/`null`)を適用し、`DrowZzzClockConstants` には切り出さない。L3 (ゲームバランス調整) で動かす値は存在しない(時刻 / 1 ターン尺は仕様上の真の不変量、`docs/architecture/constants-management.md` L1/L2 階層)。

## 関連

- ADR: [`docs/adr/0008-m2-drowzzz-clock-and-night-morning.md`](../../../adr/0008-m2-drowzzz-clock-and-night-morning.md) — 本機能の核心 ADR、§1 で `DrowZzzClock` の実装案を確定、§2 で session への computed プロパティ組み込みを確定、§5 で `RoundNumber > 21` の挙動を確定
- ADR: [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../adr/0009-m2-m3-dp-and-victory-conditions.md) — Clock 21 ターン化の境界訂正 / 用語規約 / 早期勝利の論理式(`session.Clock.IsNight && ...`)で本機能の `IsNight` / `IsMorning` を参照する想定
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §2.2 / §M1-PR2 — `DrowZzzGameSession.CurrentRound` の式 `(TurnNumber + 1) / 2` と N=2 専用性、本機能の `Clock.RoundNumber` が同義となる土台
- 実装 (本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzClock.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzClockConstants.cs`(CLAUDE.md §9 マジックナンバー禁止規約に従い `DrowZzzClock` 依存定数 8 件を集約)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs`(`Clock` computed プロパティ追加)
- テスト (本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzClockTests.cs` (DZ-090, DZ-091, DZ-092, DZ-093, DZ-094, DZ-095, DZ-096, DZ-098)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzGameSessionTests.cs` (DZ-097)
- シナリオ: `clock.feature`
- 関連既存仕様: [`skeleton.md`](skeleton.md) — DZ-010 `CurrentRound` 式が本機能の `Clock.RoundNumber` と同義(DZ-097 で表明)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-089 | `Given_同じRoundNumberの2つのDrowZzzClock_When_等価比較_Then_等価` / `Given_異なるRoundNumberの2つのDrowZzzClock_When_等価比較_Then_非等価` | 構造定義(`public sealed record DrowZzzClock(int RoundNumber)`)は宣言で保証されるが、ADR-0008 §4 スコープ表「Equals / GetHashCode の単体テスト」に従い positional record の auto-generated 値同値性に対する regression guard を 2 件残す(将来 positional から手動 Equals 実装に切り替わる際の防護) |
| DZ-090 | `Given_RoundNumberが1_When_Hourを取得_Then_21を返す` / `Given_RoundNumberが2_When_Hourを取得_Then_21を返す` / `Given_RoundNumberが16_When_Hourを取得_Then_4を返す` / `Given_RoundNumberが17_When_Hourを取得_Then_5を返す` / `Given_RoundNumberが21_When_Hourを取得_Then_7を返す` | 計算式の境界値カバレッジ |
| DZ-091 | `Given_RoundNumberが1_When_Minuteを取得_Then_0を返す` / `Given_RoundNumberが2_When_Minuteを取得_Then_30を返す` / `Given_RoundNumberが16_When_Minuteを取得_Then_30を返す` / `Given_RoundNumberが17_When_Minuteを取得_Then_0を返す` / `Given_RoundNumberが21_When_Minuteを取得_Then_0を返す` | 計算式の境界値カバレッジ |
| DZ-092 | `Given_RoundNumberが1_When_IsNightを取得_Then_true` / `Given_RoundNumberが16_When_IsNightを取得_Then_true` | 夜判定の構造的性質 |
| DZ-093 | `Given_RoundNumberが17_When_IsMorningを取得_Then_true` / `Given_RoundNumberが21_When_IsMorningを取得_Then_true` | 朝判定の構造的性質 |
| DZ-094 | DZ-090 / DZ-091 のテスト群のうち Round 1 / 16 / 17 / 21 の 4 境界に `[Property("Requirement", "DZ-094")]` を併記 | Hour 4 件 + Minute 4 件 = 8 件で境界 4 点(21:00 / 04:30 / 05:00 / 07:00)をカバー、Round 2 は Hour / Minute それぞれ 1 件のみで DZ-090 / DZ-091 専用 |
| DZ-095 | `Given_RoundNumberが17_When_IsNightを取得_Then_false`(夜・朝境界線テスト、夜側の偽値) | 真側(Round 1 / 16)は DZ-092 のテスト 2 件で別途カバー |
| DZ-096 | `Given_RoundNumberが16_When_IsMorningを取得_Then_false`(夜・朝境界線テスト、朝側の偽値) | 真側(Round 17 / 21)は DZ-093 のテスト 2 件で別途カバー |
| DZ-097 | `Given_TurnNumber1のSession_When_Clock_RoundNumberを取得_Then_CurrentRoundと一致` / `Given_TurnNumber31のSession_When_Clock_RoundNumberを取得_Then_CurrentRoundと一致` / `Given_TurnNumber41のSession_When_Clock_RoundNumberを取得_Then_CurrentRoundと一致` | N=2 で Turn 1 / 31 / 41 は Round 1 / 16 / 21 に相当(夜の最初 / 夜の終端 / 朝の最終) |
| DZ-098 | `Given_RoundNumberが22_When_IsNightを取得_Then_false` / `Given_RoundNumberが22_When_IsMorningを取得_Then_false` | M3 で `IsTerminated` がガードする前の過渡的防御値 |
