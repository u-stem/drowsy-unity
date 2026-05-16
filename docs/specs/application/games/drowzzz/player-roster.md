# PlayerRoster (Application 層 wrapper)

このファイルは `PlayerRoster` の不変条件と防御を EARS で記述する。
ADR-0017「PlayerRoster wrapper 型導入 — VContainer collection resolution と IReadOnlyList<T> 予約型問題」で確定したスコープに対応する。

配置先: `docs/specs/application/games/drowzzz/player-roster.md`

---

## 概要

`Drowsy.Application.Games.DrowZzz.PlayerRoster` は DrowZzz の対戦参加プレイヤー Id 列を保持する `sealed record`。
VContainer 1.x の `CollectionInstanceProvider.Match(IEnumerable<>, IReadOnlyList<>)` が `IReadOnlyList<PlayerId>` を予約型として扱い `RegisterInstance` を上書きする問題への wrapper として導入された(詳細は ADR-0017)。

## 普遍要件 (Ubiquitous)

- [ROSTER-001] [Ubiquitous] The `PlayerRoster` shall be a `sealed record` holding `IReadOnlyList<PlayerId> Players` as an `init`-only property.

## 事象駆動要件 (Event-driven)

- [ROSTER-004] When the ctor is called with non-empty `players`, the `PlayerRoster` shall expose them via the `Players` property in original order.

## 異常要件 (Unwanted)

- [ROSTER-002] If the ctor is called with `players = null`, then the `PlayerRoster` shall throw `ArgumentNullException`.
- [ROSTER-003] If the ctor is called with `players.Count == 0`, then the `PlayerRoster` shall throw `ArgumentException`(`ArgumentNullException` ではない厳密一致)。

## 関連

- 確定 ADR: [ADR-0017 PlayerRoster wrapper 型導入](../../../../adr/0017-player-roster-vcontainer-collection-workaround.md)
- 関連 ADR: [ADR-0016 §2 登録対象と寿命](../../../../adr/0016-m5-bootstrap-presentation.md) / §3.2 Presenter
- 実装: `Assets/_Project/Scripts/Application/Games/DrowZzz/PlayerRoster.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/PlayerRosterTests.cs`
- シナリオ: `player-roster.feature`(同ディレクトリ)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| ROSTER-001 | (テスト免除: Ubiquitous) | `sealed record` 定義はコンパイル時保証 |
| ROSTER-002 | Given_playersNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| ROSTER-003 | Given_playersEmpty_When_Ctor_Then_ArgumentException | Abnormal(`ArgumentException` 厳密一致) |
| ROSTER-004 | Given_nonEmptyPlayers_When_Ctor_Then_PlayersPreservesOrder | Normal(順序保持) |
