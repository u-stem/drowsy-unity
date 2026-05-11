# DrowZzz DP 機構 (M2-PR4 / DDP 範囲)

DrowZzz の DP 3 種(FDP / DDP / SDP)のうち、M2-PR4 で **DDP**(Draw Drowsy Point、隠し情報・累積式・2 時間ごとに共有プールから抽選)を `DrowZzzGameSession` に導入し、`EndTurnAction` の Apply 内でターン境界の自動抽選機構を実装する。

## 概要

ADR-0009 §「DP 種別」/ §「DDP プールの構造」/ §「DDP 抽選タイミング」で確定した DDP 仕様を実装する:

| DP 種別 | 性質 | 実装状況 |
| ---- | ---- | ---- |
| FDP | First Drowsy Point: ゲーム開始時に `IGameConfig.FdpPool` から抽選、隠し情報、不変 | M1-PR3 実装済(ADR-0006) |
| SDP | Second Drowsy Point: 各プレイヤーの行動で変動、公開情報、初期値 0 | M2-PR3 実装済(ADR-0009) |
| **DDP** | **Draw Drowsy Point: 2 時間ごと(計 5 回)に共有プールから抽選、隠し情報、累積式** | **本機能(M2-PR4)** |

ADR-0009 §「持ち点」: **持ち点 = FDP + DDP + SDP**。本 PR で `TotalPoints(PlayerId)` を FDP + DDP + SDP の 3 項合計に拡張する(M2-PR3 までは FDP + SDP の 2 項のみ)。

## DDP プール構造(ADR-0009 §「DDP プールの構造」)

| 項目 | 値 |
| ---- | ---- |
| プール枚数 | 39 枚(13 種 × 3 枚) |
| 値の種類 | -30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30(13 種、5 刻み) |
| プール保持 | プレイヤー間で共有(1 名が抽選した値は他名が同じ値を引けない、ただし重複可) |
| 抽選方式 | `IRandomSource.NextInt` で残プールから先頭 1 枚を取り出す(`StartGameUseCase` で Shuffle 済み前提) |

## DDP 抽選タイミング(ADR-0009 §「DDP 抽選タイミング」)

Turn 5 / 9 / 13 / 17 / 21 の **開始時**(計 5 回、N=2 で各回 2 枚抽選 = 計 10 枚消費、プール 39 枚に対して余裕 29 枚)。

ターン境界の検出は `EndTurnAction.Apply` 後の `newTurn.CurrentPlayerIndex == 0`(全プレイヤー 1 巡完了)で行う。検出後、新ターン番号 `nextSession.Clock.RoundNumber` が抽選対象なら N 枚を抽選してプレイヤー順に配分する(ADR-0009 §4 採用案 A)。

## 設計判断(本 PR で確定)

### `DdpPool` 値オブジェクトの新設

ADR-0009 §3 では「`Pile` 型を再利用」と書かれているが、`Pile` は `CardId[]` を保持するカード山札専用型で、整数プール (-30〜+30 の値列) を直接持つには semantic 違反になる(`CardId` は `string Value` ベースの識別子型)。本 PR では **`Drowsy.Application.Games.DrowZzz` namespace に専用 `DdpPool` 値オブジェクトを新設**する。

- API は `Pile` と同パターン(`Shuffle(IRandomSource)` / `Draw()` / `Equals` 順序付きシーケンス同値 / `GetHashCode`)
- 内部表現は `int[]`(`CardId` 経由のラップを避け、型から「整数 DP 値の列」が読み取れる)
- 型シグネチャから「DDP プール」が明示され、`Pile` (カード山札) との取り違いが構造的に防がれる
- 不採用案: `Pile` をジェネリック化(`Pile<T>`) — Domain 既存型の改修は影響範囲が広く、本 PR スコープ外
- 不採用案: `IReadOnlyList<int>` を直接持つ — Shuffle / Draw API がプロパティに付かず、利用側で重複ロジックが生まれる
- 不採用案: `Pile` で `CardId.Of($"DDP_{value}")` ラップ — `CardId` が DP 値の表現を兼ねることで意味が壊れる

ADR-0009 §3 の「Pile 型を再利用」は「Pile と同パターンを再利用」と読み替え、本 PR で `DdpPool` 専用型を導入する。

### `DrowZzzRule` constructor 引数は ADR-0007 §3 の 2 引数を維持(rng は注入しない)

ADR-0009 §4 で「`DrowZzzRule` constructor に `IRandomSource` を追加するか、`EndTurnAction` 側で別経路を作るかの選択肢」が JIT 判断とされた。本 PR では **`DrowZzzRule` constructor 引数は 2 引数 (`ICardCatalog<IEffect>` + `EffectInterpreter`) を維持し、`IRandomSource` を追加しない**判断を採用する。理由:

- **`StartGameUseCase` で `DdpPool` を事前 Shuffle 済**: `new DdpPool(_config.DdpPool).Shuffle(_rng)` でゲーム開始時に 1 回だけ rng を消費し、以降の `DdpPool.Draw()` は先頭から取り出すだけの決定論的操作になる。Rule 内で rng を使う必要がない
- ADR-0007 §3「`DrowZzzRule` constructor 引数は `ICardCatalog<IEffect>` / `EffectInterpreter` のみ」を破壊せずに済む(既存 PR / テストの constructor 呼び出しが変更不要)
- 不採用案 A: `DrowZzzRule` constructor に rng を追加(ADR-0009 §4 の候補案)→ 上記理由で不要、過剰結合
- 不採用案 B: 専用 `DdpDrawService` を作って `DrowZzzRule.Apply` 内で呼ぶ → Action 数を増やさない方針(ADR-0006 §M1)と整合させるならば内部完結する方が筋
- 不採用案 C: `EndTurnAction` payload に rng を持たせる → Action は値オブジェクトで rng は infrastructure 依存、責務分離違反

## 普遍要件 (Ubiquitous)

### DrowZzzGameSession の DDP / DdpPool

- [DZ-128] [Ubiquitous] The `DrowZzzGameSession` shall expose a `DrawDrowsyPoints` (`IReadOnlyDictionary<PlayerId, int>`) init-only property whose key set is structurally equal to `GameState.Players.Select(p => p.Id)`.
- [DZ-129] [Ubiquitous] The `DrowZzzGameSession` shall expose a `DdpPool` (`DdpPool`) init-only property holding the residual shared pool from which DDP draws are made.

### DdpPool 値オブジェクト

- [DZ-146] [Ubiquitous] The `DdpPool` shall be a `sealed class` declared in the `Drowsy.Application.Games.DrowZzz` namespace, mirroring the `Pile` API surface (`Shuffle(IRandomSource)` / `Draw()` / `Equals` ordered sequence equality / `GetHashCode`).
- [DZ-147] [Ubiquitous] The `DdpPool.Values` shall expose `IReadOnlyList<int>` of remaining DDP values (head = next-to-draw).
- [DZ-153] [Ubiquitous] The `DdpPool.Equals(DdpPool)` shall return `true` if and only if the two pools share an ordered sequence-equal `Values` list.

### DDP プール構造

- [DZ-154] [Ubiquitous] The default DDP pool exposed by `StubGameConfig.DdpPool` shall contain exactly 36 elements covering 13 values `{-30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30}` each repeated 3 times (ADR-0009 §「DDP プールの構造」).

### IGameConfig 拡張(CFG)

- [CFG-103] [Ubiquitous] The `IGameConfig` shall expose `DdpPool` as `IReadOnlyList<int>` (read-only, used by `StartGameUseCase` to initialize `DrowZzzGameSession.DdpPool` after `Shuffle`).

## 事象駆動要件 (Event-driven)

### DrowZzzGameSession 構築 / with 式

- [DZ-130] When `DrowZzzGameSession` is constructed with valid `DrawDrowsyPoints`, the property value shall be retained (defensive copy of the source dictionary).
- [DZ-138] When `DrowZzzGameSession.TotalPoints(playerId)` is computed (M2-PR4 以降), it shall return `FirstDrowsyPoints[playerId] + DrawDrowsyPoints[playerId] + SecondDrowsyPoints[playerId]`.

### StartGameUseCase の DDP 初期化

- [DZ-139] When `StartGameUseCase.Execute` returns a fresh `DrowZzzGameSession`, every player's `DrawDrowsyPoints` shall be `0`.
- [DZ-140] When `StartGameUseCase.Execute` returns a fresh `DrowZzzGameSession`, the `DdpPool.Values` shall be the `IGameConfig.DdpPool` Fisher-Yates shuffled with the injected `IRandomSource` (count preserved, multiset identity preserved).

### EndTurnAction の自動抽選

- [DZ-141] When `EndTurnAction.Apply` advances `TurnState` to a new round number ∈ `{5, 9, 13, 17, 21}` at a turn boundary (`CurrentPlayerIndex == 0`), the rule shall draw exactly N (= player count) values from the head of `DdpPool` and add them in player order to each player's `DrawDrowsyPoints`.
- [DZ-142] When `EndTurnAction.Apply` advances `TurnState` to a new round number ∉ `{5, 9, 13, 17, 21}`, the rule shall leave `DdpPool` and `DrawDrowsyPoints` unchanged.
- [DZ-144] When two DDP draws occur for the same player across multiple draw rounds (e.g., Round 5 and Round 9), the player's `DrawDrowsyPoints[playerId]` shall be the sum of all drawn values.

### DdpPool API

- [DZ-150] When `DdpPool.Draw()` is called on a non-empty pool, it shall return `(int Drawn, DdpPool Remaining)` where `Drawn == Values[0]` and `Remaining.Values` equals the original `Values` with the head element removed.
- [DZ-152] When `DdpPool.Shuffle(rng)` is called with a deterministic `IRandomSource`, the result shall be a Fisher-Yates permutation that preserves the multiset of `Values`.

## 異常要件 (Unwanted)

### DrowZzzGameSession 構築 / with 式

- [DZ-131] If `DrowZzzGameSession` is constructed with `null` `DrawDrowsyPoints`, then it shall throw `ArgumentNullException`.
- [DZ-132] If `DrowZzzGameSession` is constructed with `DrawDrowsyPoints` whose key set does not match `GameState.Players.Select(p => p.Id)`, then it shall throw `ArgumentException`.
- [DZ-133] If `DrowZzzGameSession` is constructed with `null` `DdpPool`, then it shall throw `ArgumentNullException`.
- [DZ-134] If `with { DrawDrowsyPoints = null }` is applied to an existing `DrowZzzGameSession`, then it shall throw `ArgumentNullException`.
- [DZ-135] If `with { DrawDrowsyPoints = ... }` whose key set does not match the existing `GameState.Players` `PlayerId` set is applied, then it shall throw `ArgumentException`.
- [DZ-136] If `with { DdpPool = null }` is applied to an existing `DrowZzzGameSession`, then it shall throw `ArgumentNullException`.

### DdpPool API

- [DZ-148] If `DdpPool` is constructed with `null` source enumerable, then it shall throw `ArgumentNullException`.
- [DZ-149] If `DdpPool.Draw()` is called on an empty pool, then it shall throw `InvalidOperationException`.
- [DZ-151] If `DdpPool.Shuffle(null)` is called, then it shall throw `ArgumentNullException`.

## 任意要件 (Optional)

- [DZ-137] [Optional] DDP values may be negative (no 0 floor): DDP は本 PR でも **負値許容** とし、0 で floor しない(SDP と同じ判断、ADR-0009 §「戦略示唆」「持ち点低い方が勝ち」と整合)。

## MC/DC ケース表

`EndTurnAction.Apply` 内の DDP 抽選トリガーは複合条件 `isLegalEndTurn && isTurnBoundary && isDdpDrawRound`:

| # | isLegalEndTurn | isTurnBoundary (CurrentPlayerIndex == 0) | isDdpDrawRound (∈ {5,9,13,17,21}) | 期待結果 | 備考 |
| ---- | ---- | ---- | ---- | ---- | ---- |
| 1 | T | T | T | DDP 抽選実行 | 全真(Turn 5/9/13/17/21 開始) |
| 2 | F | * | * | InvalidOperationException | a が独立に結果決定 (`WaitingForEndTurn` 以外) |
| 3 | T | F | * | DDP 不変、PhaseState のみ進行 | b が独立(N=2 でターン半ば: CurrentPlayerIndex == 1) |
| 4 | T | T | F | DDP 不変、ターン進行のみ | c が独立(Turn 2,3,4,6,...) |

`*` は短絡評価により評価されない条件。
- ケース 1 → DZ-141 で網羅(各 Round 5/9/13/17/21 でシナリオアウトライン)
- ケース 2 → 既存 DrowZzzRuleTests の `EndTurnAction WaitingForEndTurn 以外 → InvalidOperationException` で網羅(M1-PR6 実装済)
- ケース 3 → DZ-143 で網羅(N=2 で先手 EndTurn → CurrentPlayerIndex=1、DDP/DdpPool 不変)
- ケース 4 → DZ-142 で網羅

## 状態駆動要件 (State-driven)

- [DZ-143] While `TurnState.CurrentPlayerIndex == 1` (= N=2 で先手 1 巡完了したがターン未完、ターン境界ではない), `EndTurnAction.Apply` shall not perform any DDP draw even if the round number is in `{5, 9, 13, 17, 21}` (ターン境界での 1 回のみ抽選する保証).

## 定数依存

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| DDP 抽選対象ターン番号 {5, 9, 13, 17, 21} | L2 | `DdpPoolConstants.DrawRounds`(static readonly `IReadOnlyList<int>`、ADR-0009 §「DDP 抽選タイミング」)|
| DDP プール初期値の最小値 -30 | L2 | `DdpPoolConstants.MinValue = -30`(L2 真の不変量、ゲームバランスではなく仕様の境界) |
| DDP プール初期値の最大値 +30 | L2 | `DdpPoolConstants.MaxValue = 30` |
| DDP プール初期値の刻み 5 | L2 | `DdpPoolConstants.Step = 5` |
| DDP プール初期値の各値あたり枚数 3 | L2 | `DdpPoolConstants.CopiesPerValue = 3` |
| DDP 抽選値の初期値 0(各プレイヤー)| 自明リテラル | CLAUDE.md §9 例外(`0` は切り出し不要、`StartGameUseCase` 内で直書き) |
| DDP プールが const 不可な配列 | 静的初期化 | `DdpPoolConstants.BuildDefaultPool()` 静的メソッド(`const` 不可な `IReadOnlyList<int>` のため `static readonly` + helper) |

## 関連

- ADR: [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../adr/0009-m2-m3-dp-and-victory-conditions.md) §「DDP プールの構造」§「DDP 抽選タイミング」§「持ち点」§4「DDP 抽選タイミングと進行ロジック」 — 本機能の根拠
- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../adr/0007-m2-detail-card-effects.md) §3 — `DrowZzzRule` constructor 引数は本 PR で `ICardCatalog<IEffect>` + `EffectInterpreter` の 2 引数を維持(ADR-0009 §4 で挙げた「rng を Rule に注入する案」は本 PR で「`StartGameUseCase` の事前 Shuffle で十分」と再評価し採用しない、PR description に記録)
- ADR: [`docs/adr/0008-m2-drowzzz-clock-and-night-morning.md`](../../../adr/0008-m2-drowzzz-clock-and-night-morning.md) §5 — Round 21 完了処理は M3、本 PR では DDP 抽選のみ
- 既存仕様: [`dp-mechanism.md`](dp-mechanism.md)(M2-PR3 / SDP 範囲) — 本 PR で `TotalPoints` を 3 項合計に拡張する起点
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DdpPool.cs`(新規、値オブジェクト)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DdpPoolConstants.cs`(新規、L2 const 集約)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzGameSession.cs`(`DrawDrowsyPoints` / `DdpPool` フィールド追加、`TotalPoints` 3 項拡張、Equals/GetHashCode 更新、コンストラクタ 4 → 6 引数)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`(DDP 初期化 + `DdpPool` 初期 Shuffle)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`ApplyEndTurn` 内に DDP 抽選ロジック追加、constructor 引数は ADR-0007 §3 の 2 引数を維持)
  - `Assets/_Project/Scripts/Domain/Configuration/IGameConfig.cs`(`DdpPool: IReadOnlyList<int>` 追加)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Stubs/StubGameConfig.cs`(デフォルト 39 要素プール)
- テスト(本 PR):
  - `DdpPoolTests`: DZ-148 / DZ-149 / DZ-150 / DZ-151 / DZ-152
  - `DrowZzzGameSessionTests` 追加: DZ-130 / DZ-131 / DZ-132 / DZ-133 / DZ-134 / DZ-135 / DZ-136 / DZ-137 / DZ-138
  - `StartGameUseCaseTests` 追加: DZ-139 / DZ-140
  - `DrowZzzRuleTests` 追加: DZ-141 / DZ-142 / DZ-143 / DZ-144
  - `StubGameConfigTests` 追加: DZ-154
- シナリオ: `dp-mechanism-ddp.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-128 | (テスト免除: Ubiquitous) | `record` の init-only プロパティ宣言で構造的に保証 |
| DZ-129 | (テスト免除: Ubiquitous) | 同上 |
| DZ-130 | `Given_有効なDDP_When_DrowZzzGameSessionを生成_Then_DrawDrowsyPointsが入力と一致する` | コンストラクタ値保持 |
| DZ-131 | `Given_DDPにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる` | null 防御 |
| DZ-132 | `Given_DDPのキーがPlayersと不一致_When_DrowZzzGameSessionを生成_Then_ArgumentExceptionを投げる` | cross-field 検証 |
| DZ-133 | `Given_DdpPoolにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる` | null 防御 |
| DZ-134 | `Given_既存Session_When_with_DDPにnull_Then_ArgumentNullExceptionを投げる` | with 式 null 防御 |
| DZ-135 | `Given_既存Session_When_with_DDPをキー不一致に変更_Then_ArgumentExceptionを投げる` | with 式 cross-field 検証 |
| DZ-136 | `Given_既存Session_When_with_DdpPoolにnull_Then_ArgumentNullExceptionを投げる` | with 式 null 防御 |
| DZ-137 | `Given_負のDDP値_When_DrowZzzGameSessionを生成_Then_保持される` | 0 floor なし |
| DZ-138 | `Given_FDP100DDP5SDP10_When_TotalPointsを取得_Then_115を返す` 等(2 件以上) | TotalPoints 3 項合計 |
| DZ-139 | `Given_StartGameUseCase_When_Execute_Then_全プレイヤーのDDPが0で初期化される` | DDP 初期値 |
| DZ-140 | `Given_StartGameUseCase_When_Execute_Then_DdpPoolがShuffle済みの36要素を保持` | DdpPool 初期化 |
| DZ-141 | `Given_Turn4でEndTurn_When_先手後手共にEndTurn完了_Then_Turn5開始時にDDP抽選実行` 等(5 件、各 Round で) | 抽選タイミング |
| DZ-142 | `Given_Turn2でEndTurn_When_先手後手共にEndTurn完了_Then_DDPとDdpPool不変` | 抽選対象外ターン |
| DZ-143 | `Given_Turn4の先手EndTurn_When_CurrentPlayerIndex0→1_Then_DDP不変` | 状態駆動: ターン境界以外 |
| DZ-144 | `Given_Turn4とTurn8でEndTurn_When_2回のDDP抽選完了_Then_DrawDrowsyPointsが累積される` | 累積式 |
| DZ-146 | (テスト免除: Ubiquitous) | `sealed class` 宣言で構造的に保証(Pile と同パターン) |
| DZ-147 | (テスト免除: Ubiquitous) | `Values: IReadOnlyList<int>` プロパティ宣言で構造的に保証 |
| DZ-148 | `Given_nullを渡す_When_DdpPoolを生成_Then_ArgumentNullExceptionを投げる` | null 防御 |
| DZ-149 | `Given_空のDdpPool_When_Drawを呼ぶ_Then_InvalidOperationExceptionを投げる` | 空 Pool 防御 |
| DZ-150 | `Given_3要素のDdpPool_When_Drawを呼ぶ_Then_先頭要素と残2要素のPoolを返す` | Draw 仕様 |
| DZ-151 | `Given_nullのrng_When_Shuffleを呼ぶ_Then_ArgumentNullExceptionを投げる` | null 防御 |
| DZ-152 | `Given_決定的rng_When_Shuffleを呼ぶ_Then_Fisher_Yates結果のDdpPoolを返す` | Shuffle 仕様 |
| DZ-153 | (テスト免除: Ubiquitous) | `Equals` override + 順序付きシーケンス比較で構造的に保証(間接的にテスト) |
| DZ-154 | `Given_StubGameConfig_When_DdpPoolを取得_Then_36要素の規定パターン` | デフォルトプール |
| CFG-103 | (テスト免除: Ubiquitous) | `IGameConfig.DdpPool` の signature で構造的に保証 |
