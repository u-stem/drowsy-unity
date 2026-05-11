# DrowZzz セットアップ (M1-PR3)

ゲーム開始時のセットアップ(先後ランダム決定 / FDP 抽選 / 初期手札配布 / セッション構築)を行う `StartGameUseCase` の振る舞いを定義する。

## 概要

ADR-0006 §3 / §M1-PR3 / §M1 ルール の決定に基づく。`StartGameUseCase` は唯一「セッション未生成状態」から `DrowZzzGameSession` を構築する特殊 UseCase。`ApplyActionUseCase` 系統とは独立(`IGameRule.Apply` ではなく直接呼ぶ)。

ADR-0006 §Implementation Notes §StartGameUseCase の `IsLegalMove` 経由での扱い に従い、`DrowZzzRule.IsLegalMove(session, StartGameAction)` は **常に `false`** を返す(セッション既存状態でゲーム開始は不可)。

## 普遍要件 (Ubiquitous)

- [DZ-018] [Ubiquitous] The `StartGameUseCase` shall be a sealed class in the `Drowsy.Application.Games.DrowZzz` namespace with constructor injection of `IRandomSource`, `ICardCatalog`, and `IGameConfig`.

## 事象駆動要件 (Event-driven)

- [DZ-019] When `StartGameUseCase.Execute(players, initialDeck)` is invoked with valid arguments, the result `GameState.Players.Count` shall equal `players.Count`.
- [DZ-020] When `Execute(players, initialDeck)` is invoked, the result `FirstDrowsyPoints` keys shall equal the input `players` `PlayerId` set.
- [DZ-021] When `Execute(players, initialDeck)` is invoked, each `FirstDrowsyPoints` value shall be drawn from `IGameConfig.FdpPool` without duplication.
- [DZ-022] When `Execute(players, initialDeck)` is invoked, each player's `Hand` shall contain exactly 5 cards drawn from `initialDeck`.
- [DZ-023] When `Execute(players, initialDeck)` is invoked, cards shall be dealt in interleaved order (1 card per player per cycle for 5 cycles).
- [DZ-024] When `Execute(players, initialDeck)` is invoked, the result `Deck.Count` shall equal `initialDeck.Count - 5 * players.Count`.
- [DZ-025] When `Execute(players, initialDeck)` is invoked, the result `PhaseState` shall be `WaitingForDraw`.
- [DZ-026] When `Execute(players, initialDeck)` is invoked, the result `Turn` shall be `TurnState.Initial(0)` (`TurnNumber = 1`, `CurrentPlayerIndex = 0`).
- [DZ-027] When `Execute` is called twice with identical arguments and `IRandomSource` instances yielding identical sequences, the results shall be equal (Deterministic Replay).

## 異常要件 (Unwanted)

- [DZ-028] If `Execute(null, initialDeck)` is called, then it shall throw `ArgumentNullException`.
- [DZ-029] If `Execute(emptyPlayers, initialDeck)` is called, then it shall throw `ArgumentException`.
- [DZ-030] If `Execute(playersWithDuplicateId, initialDeck)` is called, then it shall throw `ArgumentException`.
- [DZ-031] If `Execute(players, null)` is called, then it shall throw `ArgumentNullException`.
- [DZ-032] If `Execute(players, deckWithFewerCardsThan(5 * players.Count))` is called, then it shall throw `ArgumentException`.
- [DZ-033] If `players.Count > IGameConfig.FdpPool.Count`, then `Execute` shall throw `InvalidOperationException` (FDP プール不足).
- [DZ-037] If `Execute(playersWithNullElement, initialDeck)` is called (i.e. `players` contains a `null` `PlayerId`), then it shall throw `ArgumentException`.

## DrowZzzRule.IsLegalMove の StartGameAction 分岐

- [DZ-034] When `DrowZzzRule.IsLegalMove(session, StartGameAction)` is called, it shall return `false` (StartGameAction はセッション未生成用で、`StartGameUseCase` 経由で扱う、ADR-0006 §Implementation Notes §StartGameUseCase の `IsLegalMove` 経由での扱い)。

## 定数依存

| 定数 | 階層 | 由来 |
| ---- | ---- | ---- |
| 初期手札枚数 = 5 | L2(DrowZzz の最小ルール、ADR-0006 §M1 で確定) | 本 PR では `StartGameUseCase` 内に直接 `const int InitialHandSize = 5;` を埋め込み(M1 範囲では `IGameConfig` への移管は見送り、Phase 2 後半で要検討) |
| FDP プール `[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]` | L3(ゲームバランス調整可能値) | `IGameConfig.FdpPool`(stub 実装は `Tests/Application.Tests/Stubs/StubGameConfig.cs`、本物実装は M2 以降の SO ベース) |

## Implementation Notes

- **DrowZzz 本物山札のカード総数**: N=2 想定で **56 枚**(2026-05-11 プロジェクトオーナー JIT 共有)。M1-PR3 時点では `StartGameUseCase` の `initialDeck` 引数として外部から受け取る形を採用しており、UseCase 内部では枚数を強制しない(配布に必要な最小限 `5 * players.Count` のみ検証)。本物カードデータ導入は M2 以降。N>2 への山札スケーリング規則は Phase 3 候補。
- **先後ランダム決定**: `IRandomSource` で Players 配列を Fisher-Yates シャッフルし、先頭プレイヤーを「先行」とする。シャッフル後の `Players` 順がそのまま `GameState.Players` に格納される。
- **FDP 抽選順**: シャッフル後の `Players` 順(= 先行プレイヤーから順)に `FdpPool` から被りなく抽選する。`FdpPool` を一括 Fisher-Yates シャッフルして先頭 N 個を Players 順に割り当てる方式と等価。
- **`ICardCatalog` の利用**: M1-PR3 段階では `StartGameUseCase` 内部で `ICardCatalog` を **読まない**(`CardData` の解決は手札を「場に出す」M1-PR5 以降で必要、本 PR では `CardId` の移動のみ)。ただし constructor injection は維持(将来 M1-PR5 以降で参照される + ADR-0006 §3 通り)。

## 関連

- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/StartGameUseCase.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalMove(StartGameAction → false)` 分岐追加)
  - `Assets/_Project/Scripts/Domain/Configuration/IGameConfig.cs`(`FdpPool` プロパティ追加、本 PR で完了)
- テスト(本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/StartGameUseCaseTests.cs`
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/DrowZzzRuleTests.cs`(DZ-034 追加)
  - `Assets/_Project/Scripts/Tests/Application.Tests/Stubs/StubGameConfig.cs`(stub 実装)
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §3 / §M1-PR3 / §Implementation Notes
- 関連 spec: [`skeleton.md`](skeleton.md)(DZ-001〜017、本 PR では DZ-012 のテスト名のみ更新)
- 関連 spec: [`docs/specs/domain/configuration/game-config.md`](../../domain/configuration/game-config.md)(CFG-101 で FdpPool 追加)
- 後続: M1-PR4 (`DrawCardAction` の Apply 実装)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-018 | (テスト免除: Ubiquitous) | `sealed class StartGameUseCase` のコンストラクタ宣言で構造的に保証 |
| DZ-019 | `Given_有効な引数_When_StartGameUseCase_Execute_Then_PlayersCountが入力と一致する` | |
| DZ-020 | `Given_有効な引数_When_Execute_Then_FirstDrowsyPointsキー集合がplayersと一致する` | |
| DZ-021 | 1 ID 2 テスト分割: `Then_FirstDrowsyPointsの値が全てFdpPoolに含まれる` / `Then_FirstDrowsyPointsの値が全て互いに異なる` | 1 テスト 1 アサーション原則 |
| DZ-022 | `Given_有効な引数_When_Execute_Then_各プレイヤーの手札が5枚` | |
| DZ-023 | 1 ID 2 テスト分割: `Given_IdentityRandom_When_Execute_Then_Players0の手札が奇数位カード` / `Given_IdentityRandom_When_Execute_Then_Players1の手札が偶数位カード` | `IdentityRandom` で seed 非依存に Players[0]/[1] 各々の手札順を検証 |
| DZ-024 | `Given_有効な引数_When_Execute_Then_山札残り枚数が初期マイナス10` | |
| DZ-025 | `Given_有効な引数_When_Execute_Then_PhaseStateがWaitingForDraw` | |
| DZ-026 | `Given_有効な引数_When_Execute_Then_TurnがInitial0と等価` | |
| DZ-027 | `Given_同一引数と同一rng列_When_Executeを2回呼ぶ_Then_結果が等価` | Deterministic Replay |
| DZ-028 | `Given_playersにnull_When_Execute_Then_ArgumentNullExceptionを投げる` | |
| DZ-029 | `Given_空のplayers_When_Execute_Then_ArgumentExceptionを投げる` | |
| DZ-030 | `Given_重複PlayerIdのplayers_When_Execute_Then_ArgumentExceptionを投げる` | |
| DZ-031 | `Given_initialDeckにnull_When_Execute_Then_ArgumentNullExceptionを投げる` | |
| DZ-032 | `Given_山札枚数が5×Players未満_When_Execute_Then_ArgumentExceptionを投げる` | |
| DZ-033 | `Given_PlayersCountがFdpPoolより多い_When_Execute_Then_InvalidOperationExceptionを投げる` | |
| DZ-034 | `Given_DrowZzzRule_When_StartGameActionでIsLegalMoveを呼ぶ_Then_falseを返す` | skeleton.md DZ-012 の境界外として追加 |
| DZ-037 | `Given_playersにnull要素を含む_When_Execute_Then_ArgumentExceptionを投げる` | DZ-028(players=null 全体)の補完、null 要素混入も同等に弾く |
