# TurnState

ゲームのターン進行状態(ターン番号と現在ターンプレイヤー)を表す不変値オブジェクト。

## 概要

`TurnState` は以下の 2 フィールドを保持する。

| フィールド | 型 | 意味 |
| ---- | ---- | ---- |
| `TurnNumber` | `int` | ゲーム開始から数えたターン番号(**1 始まり**、ゲーム開始時 = ターン 1) |
| `CurrentPlayerIndex` | `int` | `GameState.Players` における現在ターンプレイヤーの 0 始まり index |

`record` として実装し、内部に辞書 / 配列を持たないため auto-equals / auto-`GetHashCode` / `==` / `!=` / `Equals(object)` がそのまま値同値で動作する(ADR-0002 / `PlayerState` 同様の判断軸)。

ターン進行は `Next(int playerCount)` メソッドで `TurnNumber + 1` / `CurrentPlayerIndex = (current + 1) % playerCount` を行う(ユーザー合意済の方針)。

`CurrentPlayerIndex` の `GameState.Players` 範囲との整合性は `TurnState` 単体では検証できない(`TurnState` は `Players` を知らない)ため、`GameState` のコンストラクタで `0 ≤ Turn.CurrentPlayerIndex < Players.Count` を検証する(GS-022)。

## 普遍要件 (Ubiquitous)

- [TURN-001] [Ubiquitous] The `TurnState` shall be immutable.
- [TURN-002] [Ubiquitous] The `TurnState` shall expose `TurnNumber` (`int`) and `CurrentPlayerIndex` (`int`) as read-only properties.

## 事象駆動要件 (Event-driven)

- [TURN-003] When `new TurnState(turnNumber, currentPlayerIndex)` is called with valid values, the constructed instance shall hold the same values.
- [TURN-004] When `TurnState.Initial(playerIndex)` is called with `playerIndex >= 0`, it shall return a `TurnState` with `TurnNumber = 1` and `CurrentPlayerIndex = playerIndex`.
- [TURN-005] When `Next(playerCount)` is called, the result shall have `TurnNumber + 1`.
- [TURN-006] When `Next(playerCount)` is called, the result shall have `CurrentPlayerIndex = (current + 1) % playerCount` (0 始まり、巻き戻り対応)。
- [TURN-007] When two `TurnState` instances are compared with `Equals` or `==`, they shall be equal if and only if both `TurnNumber` and `CurrentPlayerIndex` match.
- [TURN-008] When `GetHashCode` is called on two equal `TurnState` instances, they shall return the same hash value.

## 異常要件 (Unwanted)

- [TURN-009] If `new TurnState(turnNumber, ...)` is called with `turnNumber < 1`, then the constructor shall throw `ArgumentOutOfRangeException`.
- [TURN-010] If `new TurnState(..., currentPlayerIndex)` is called with `currentPlayerIndex < 0`, then the constructor shall throw `ArgumentOutOfRangeException`.
- [TURN-011] If `TurnState.Initial(playerIndex)` is called with `playerIndex < 0`, then it shall throw `ArgumentOutOfRangeException`.
- [TURN-012] If `Next(playerCount)` is called with `playerCount <= 0`, then it shall throw `ArgumentOutOfRangeException`.
- [TURN-013] If `Next(playerCount)` would overflow `TurnNumber` past `int.MaxValue`, then it shall throw `OverflowException` (`checked` 算術)。実運用では DrowZzzClockConstants が 21 ターンで終了するため到達しないが、Domain 単体で `int.MaxValue` が渡された際のサイレントオーバーフローを防ぐ。

## 実装メモ

### record 採用(GameState との非対称)

`TurnState` は内部に辞書 / 配列を持たないため `record` の auto-equals が値同値で正しく動く(`PlayerState` と同じ判断軸)。GameState は `IReadOnlyList<PlayerState>` を保持するため Equals を手動 override したが、`TurnState` は不要。

### TurnNumber の起点

ゲーム開始時 = ターン 1(1 始まり)。0 を「ゲーム開始前」のような状態に使う設計も可能だが、本 PR では「`TurnState` は常に進行中のゲーム状態を表す」と定義し、`TurnNumber >= 1` を不変条件とする(TURN-009)。

### CurrentPlayerIndex の範囲検証

`TurnState` 単体では下限(`>= 0`、TURN-010)のみ検証。上限(`< Players.Count`)は GameState 側の責務(GS-022 で別途検証)。これにより `TurnState` 単体は GameState から独立して使える。

### Next(playerCount) の循環

`(current + 1) % playerCount` で次プレイヤーに進む。`current = playerCount - 1` のときは `0` に戻り、TurnNumber は単調増加する。

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Game/TurnState.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Game/TurnStateTests.cs`
- シナリオ: `turn-state.feature`
- 設計根拠: [`docs/adr/0002-phase1-domain-boundaries.md`](../../../adr/0002-phase1-domain-boundaries.md)(Player N 人想定 / Domain 全体 immutable)
- 連携: [`game-state.md`](game-state.md)(GS-020〜027 で GameState に Turn フィールドを追加)
- DrowZzz での用語解釈: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../adr/0006-m1-detail-application-interfaces.md) §7(本仕様の `TurnState.TurnNumber` を DrowZzz では「フェーズ番号」(ADR-0009 §用語規約で旧称「サブターン番号」から訂正)として解釈し、DrowZzz の「ターン」(ADR-0009 §用語規約で旧称「ラウンド」から訂正)は `(TurnNumber + 1) / 2` で計算する。Domain 仕様自体は変更せず、ゲーム固有の用語マッピングを Application 層 ADR-0006 §7 で確定)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| TURN-001 | (テスト免除: Ubiquitous) | `record` + init-only で構造保証 |
| TURN-002 | (テスト免除: Ubiquitous) | record auto property で構造保証 |
| TURN-003 | `Given_有効な値_When_コンストラクタ_Then_TurnNumberが入力と同じ` / `Given_有効な値_..._Then_CurrentPlayerIndexが入力と同じ` | 1 テスト 1 アサーションで分離 |
| TURN-004 | `Given_playerIndex0_When_Initial_Then_TurnNumber1とCurrentPlayerIndex0` / `Given_playerIndex2_When_Initial_Then_TurnNumber1とCurrentPlayerIndex2` | factory の挙動を 2 ケース |
| TURN-005 | `Given_TurnState_When_Next_Then_TurnNumberが1増える` | |
| TURN-006 | `Given_中間プレイヤー_When_Next_Then_次のIndex` / `Given_最終プレイヤー_When_Next_Then_Indexが0に戻る` | 通常進行 + 巻き戻り |
| TURN-007 | `Given_同じTurnNumberとIndex_When_Equals_Then_等価` / `Given_異なるTurnNumber_..._Then_非等価` / `Given_異なるIndex_..._Then_非等価` | record auto-equals が値同値 |
| TURN-008 | `Given_等価な2つのTurnState_When_GetHashCode_Then_同じ値を返す` | |
| TURN-009 | `Given_turnNumber0_When_コンストラクタ_Then_ArgumentOutOfRangeException` | |
| TURN-010 | `Given_currentPlayerIndex負_When_コンストラクタ_Then_ArgumentOutOfRangeException` | |
| TURN-011 | `Given_playerIndex負_When_Initial_Then_ArgumentOutOfRangeException` | |
| TURN-012 | `Given_playerCount0_When_Next_Then_ArgumentOutOfRangeException` / `Given_playerCount負_When_Next_Then_ArgumentOutOfRangeException` | 0 と負の両ケース |
| TURN-013 | `Given_TurnNumberがint最大値_When_Next_Then_OverflowException` | checked 算術でサイレントオーバーフローを防止 |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
