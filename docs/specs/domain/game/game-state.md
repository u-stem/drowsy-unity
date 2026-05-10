# GameState

ゲーム全体の状態を表す不変ルート集約。Phase 1 では `Players` / `Deck` / `Discard` / `Field` の 4 フィールドを保持する(`Turn` は PR-5 で `TurnState` と一緒に追加予定)。

## 概要

`GameState` は以下のフィールドを保持する:

```
GameState (root, immutable record)
├── Players  : IReadOnlyList<PlayerState>   (順序付き、PlayerId 重複拒否)
├── Deck     : Pile                         (山札)
├── Discard  : Pile                         (捨て札)
└── Field    : Pile                         (場、Pile.Empty で「場なし」を表現)
```

`record class` として実装し、`init` setter + バッキングフィールド + `value ?? throw` で **コンストラクタ + `with` 式の両経路で null 防御** が走る(PR-3 PlayerState と同パターン、ADR-0004 polyfill 前提)。

`Equals` / `GetHashCode` は record の auto-generated を上書きし、`Players` を順序付きシーケンス同値で比較する(record auto-equals は `IReadOnlyList<PlayerState>` を参照同値で比較するため値同値が壊れる)。`Deck` / `Discard` / `Field` はそれぞれ `Pile.Equals(Pile)`(順序付きシーケンス同値、TODO-1 で導入済み)に委譲する。

`with` 式での状態更新は `gameState with { Deck = newDeck }` のような単一フィールド更新で簡潔に書ける(本要件で Phase 1 ルート集約として確立)。

## 普遍要件 (Ubiquitous)

- [GS-001] [Ubiquitous] The `GameState` shall be immutable.
- [GS-002] [Ubiquitous] The `GameState` shall expose `Players` (`IReadOnlyList<PlayerState>`), `Deck` (`Pile`), `Discard` (`Pile`), and `Field` (`Pile`) as read-only properties.

## 事象駆動要件 (Event-driven)

- [GS-003] When `new GameState(players, deck, discard, field)` is called with valid arguments, the constructed instance shall hold the same values via a defensive copy of `players`.
- [GS-004] When two `GameState` instances are compared with `Equals`, they shall be equal if and only if `Players` (in order, by value) and each of `Deck` / `Discard` / `Field` are value-equal.
- [GS-005] When `GetHashCode` is called on two equal `GameState` instances, they shall return the same hash value.
- [GS-006] When two `GameState` references are compared with `operator==` or `operator!=`, the result shall be value-equal consistent with `Equals`. Both null operands compare as equal; one-sided null compares as not equal.
- [GS-007] When `Equals(object)` is called with `null` or an argument that is not a `GameState`, the `GameState` shall return `false`.
- [GS-008] When `state with { Deck = newDeck }` (or any other field) is evaluated, the result shall be a new instance with the updated field and unchanged others.
- [GS-009] When the source `players` list is mutated after construction, `GameState.Players` shall remain unchanged (defensive copy).

## 異常要件 (Unwanted)

- [GS-010] If `players` is null, then the constructor shall throw `ArgumentNullException`.
- [GS-011] If `deck` is null, then the constructor shall throw `ArgumentNullException`.
- [GS-012] If `discard` is null, then the constructor shall throw `ArgumentNullException`.
- [GS-013] If `field` is null, then the constructor shall throw `ArgumentNullException`.
- [GS-014] If `players` contains a null `PlayerState`, then the constructor shall throw `ArgumentException`.
- [GS-015] If `players` contains two `PlayerState` instances with the same `PlayerId`, then the constructor shall throw `ArgumentException`.
- [GS-016] If `state with { Players = null }` is evaluated, then the `init` setter shall throw `ArgumentNullException`.
- [GS-017] If `state with { Deck = null }` is evaluated, then the `init` setter shall throw `ArgumentNullException`.
- [GS-018] If `state with { Discard = null }` is evaluated, then the `init` setter shall throw `ArgumentNullException`.
- [GS-019] If `state with { Field = null }` is evaluated, then the `init` setter shall throw `ArgumentNullException`.

## 実装メモ

### record の Equals(GameState) override

`record` は `Equals(T)` / `GetHashCode` / `==` / `!=` / `Equals(object)` を auto-generated する。本実装は `Equals(GameState)` と `GetHashCode` のみを上書きし、`Players` の順序付きシーケンス同値を実現する。`==` / `!=` / `Equals(object)` は record の標準実装が `Equals(GameState)` を呼ぶため自動的に正しく動く。`sealed record` のため `virtual` 修飾子は不要。

### 防御コピーと PlayerId 重複検証

コンストラクタ内で `players` を 1 度走査して以下を検証:
1. null 要素を含まない(GS-014)
2. PlayerId の重複がない(GS-015)
3. 配列に変換して内部保持(GS-009 の防御コピー)

走査中に `HashSet<PlayerId>` で重複検出。`PlayerId` は `record` で値同値性があるため `HashSet` 内で正しく動作する。

### Pile の値同値性依存

`GameState.Equals` は `Deck.Equals(Deck)` 等で `Pile.Equals(Pile)` を呼ぶ。これは TODO-1(PR #15)で追加した順序付きシーケンス同値に依存する。Pile に値同値性が無い PR-2 までは GameState の Equals が正しく動作しなかったため、TODO-1 を PR-3 / PR-4 の前に消化したのは正しい順序だった。

### N=1 / N=2 のテスト

ADR-0002 の「N 人プレイヤー想定」を本要件で具体化する。GS-004 のテストでは N=1(単一プレイヤーの GameState 同士の値同値)と N=2(2 プレイヤーの GameState 同士の値同値、順序異 → 非等価、Players 数異 → 非等価)を両方カバー。

### Phase 1 残作業

PR-5 (`TurnState`) で本 GameState に `Turn` プロパティを追加する。`init` setter + null 検証 + Equals/GetHashCode の更新が必要。本 PR の範囲外。

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Game/GameState.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Game/GameStateTests.cs`
- シナリオ: `game-state.feature`
- 設計根拠: [`docs/adr/0002-phase1-domain-boundaries.md`](../../../adr/0002-phase1-domain-boundaries.md)(集約境界 / N 人想定 / Domain 全体 immutable)
- 依存技術: [`docs/adr/0004-init-setter-polyfill.md`](../../../adr/0004-init-setter-polyfill.md)(`record + init + with` 利用の前提)
- 依存仕様: [`pile.md`](../cards/pile.md)(`Deck` / `Discard` / `Field` の値同値性)、[`hand.md`](../cards/hand.md)、[`player-state.md`](../players/player-state.md)、[`player-id.md`](../players/player-id.md)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| GS-001 | (テスト免除: Ubiquitous) | `record class` + `init`-only バッキングで構造保証 |
| GS-002 | (テスト免除: Ubiquitous) | `IReadOnlyList<PlayerState>` / `Pile` プロパティで構造保証 |
| GS-003 | `Given_有効な4引数_When_コンストラクタ_Then_Playersが入力と同じ` / `Given_有効な4引数_..._Then_Deckが入力と同じ` / `Given_有効な4引数_..._Then_Discardが入力と同じ` / `Given_有効な4引数_..._Then_Fieldが入力と同じ` | 1 テスト 1 アサーションで 4 フィールドを分離 |
| GS-004 | `Given_全フィールド一致_..._Then_等価` (N=1) / `Given_2人で全フィールド一致_..._Then_等価` (N=2) / `Given_Players順序異_..._Then_非等価` / `Given_Players数異_..._Then_非等価` / `Given_Deck異_..._Then_非等価` / `Given_Discard異_..._Then_非等価` / `Given_Field異_..._Then_非等価` / `Given_同一インスタンス_..._Then_等価` / `Given_Equalsにnull_..._Then_false` | N=1 / N=2 + 4 フィールドそれぞれ異 + Players 順序異 + ReferenceEquals + null |
| GS-005 | `Given_等価な2つのGameState_When_GetHashCode_Then_同じ値を返す` | |
| GS-006 | `Given_等価な2つのGameState_When_operator_等価_Then_true` / `Given_非等価_..._operator_等価_Then_false` / `Given_非等価_..._operator_非等価_Then_true` / `Given_両方null_..._operator_等価_Then_true` / `Given_片方nullで他方非null_..._operator_等価_Then_false` (左 null) / `Given_左側非nullで右側null_..._operator_等価_Then_false` (右 null) | |
| GS-007 | `Given_null_When_Equalsオブジェクト_Then_false` / `Given_異なる型_When_Equalsオブジェクト_Then_false` | |
| GS-008 | `Given_GameState_When_with式でDeckを差し替え_Then_新インスタンスのDeckが新値` / `Given_GameState_When_with式でDeckを差し替え_Then_Playersは不変` / `Given_GameState_When_with式でDeckを差し替え_Then_Discardは不変` / `Given_GameState_When_with式でDeckを差し替え_Then_Fieldは不変` / `Given_GameState_When_with式でDeckを差し替え_Then_元インスタンスは不変` | with の 5 観点(新 Deck + 他 3 フィールド不変 + 元インスタンス不変) |
| GS-009 | `Given_生成後にソースリストを変更_When_Players参照_Then_影響を受けない` | 防御コピー |
| GS-010 | `Given_nullPlayers_When_コンストラクタ_Then_ArgumentNullException` | |
| GS-011 | `Given_nullDeck_When_コンストラクタ_Then_ArgumentNullException` | |
| GS-012 | `Given_nullDiscard_When_コンストラクタ_Then_ArgumentNullException` | |
| GS-013 | `Given_nullField_When_コンストラクタ_Then_ArgumentNullException` | |
| GS-014 | `Given_null要素を含むplayers_When_コンストラクタ_Then_ArgumentException` | |
| GS-015 | `Given_重複PlayerIdを含むplayers_When_コンストラクタ_Then_ArgumentException` | |
| GS-016 | `Given_GameState_When_with式でPlayersをnullに_Then_ArgumentNullException` | |
| GS-017 | `Given_GameState_When_with式でDeckをnullに_Then_ArgumentNullException` | |
| GS-018 | `Given_GameState_When_with式でDiscardをnullに_Then_ArgumentNullException` | |
| GS-019 | `Given_GameState_When_with式でFieldをnullに_Then_ArgumentNullException` | |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
