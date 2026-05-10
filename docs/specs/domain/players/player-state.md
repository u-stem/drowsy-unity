# PlayerState

1 プレイヤーの状態(識別子と手札)を表す不変値オブジェクト。

## 概要

`PlayerState` は `PlayerId` と `Hand` を保持する不変値オブジェクト。Phase 1 の最小構成として `PlayerId + Hand` のみを持つ(Score / Name 等は Phase 2 以降で必要性が明示されてから拡張)。`record class` として実装し、auto-equals が `PlayerId` (record) と `Hand` (`IEquatable<Hand>`) のそれぞれの値同値性を呼ぶことで全体として値同値となる。

`With` 系メソッド(`WithHand` 等)はビルトインの `with` 式で代替する(record の標準機能)。

## 普遍要件 (Ubiquitous)

- [PLAYER-009] [Ubiquitous] The `PlayerState` shall be immutable.
- [PLAYER-010] [Ubiquitous] The `PlayerState` shall expose `Id` (`PlayerId`) and `Hand` (`Hand`) as read-only properties.

## 事象駆動要件 (Event-driven)

- [PLAYER-011] When `new PlayerState(id, hand)` is called with non-null `id` and `hand`, the constructed instance shall hold the same `Id` and `Hand`.
- [PLAYER-012] When two `PlayerState` instances are compared with `Equals` or `==`, they shall be equal if and only if both `Id` and `Hand` are value-equal.
- [PLAYER-013] When `GetHashCode` is called on two equal `PlayerState` instances, they shall return the same hash value.
- [PLAYER-014] When `with { Hand = newHand }` (record `with` expression) is evaluated, the result shall be a new `PlayerState` instance with the updated `Hand` and unchanged `Id`.

## 異常要件 (Unwanted)

- [PLAYER-015] If `new PlayerState(null, hand)` is called, then the constructor shall throw `ArgumentNullException`.
- [PLAYER-016] If `new PlayerState(id, null)` is called, then the constructor shall throw `ArgumentNullException`.
- [PLAYER-017] If `state with { Id = null }` is evaluated, then the `init` setter shall throw `ArgumentNullException`.
- [PLAYER-018] If `state with { Hand = null }` is evaluated, then the `init` setter shall throw `ArgumentNullException`.

## 実装メモ

### record class を選んだ理由

`PlayerState` は内部に辞書や mutable コレクションを持たず、`PlayerId` (record) と `Hand` (IEquatable<Hand>) を保持するのみ。両者ともそれぞれ独自に値同値で比較されるため、`record class` の auto-equals が `PlayerId.Equals` / `Hand.Equals(Hand)` を呼んで全体で値同値となる。

ADR-0002 で `CardData` / `Hand` / `Pile` を `sealed class + IEquatable` にした理由は「内部辞書 / 配列フィールドが record auto-equals では参照同値になり値同値が壊れる」だったが、`PlayerState` は内部にそうしたフィールドを持たないため record で安全。`CardId` / `PlayerId` も同じ理由で record。

### N 人想定のテスト

PlayerState 単体は 1 プレイヤーを表現するため、N 人想定は後続 PR-4 (GameState) で `IReadOnlyList<PlayerState>` を保持して実現する。本 PR では PlayerState 単体の挙動(N=1 に相当)に加え、複数 PlayerState を生成して値同値性が独立に動作することを確認するテスト(N=2 相当)を 1 件含める。

### null 検証パターン(コンストラクタ + with 式の両経路)

実装は **`init` setter にバッキングフィールド + `value ?? throw new ArgumentNullException(nameof(value))` のパターン** を採用する。これにより:

- コンストラクタ経由(`new PlayerState(id, hand)`)→ コンストラクタ内で `Id = id` を呼ぶと `init` setter が走り null 検証(PLAYER-015 / PLAYER-016)
- `with` 式経由(`state with { Hand = newHand }`)→ コンパイラが `init` setter を呼ぶため同じ null 検証が走る(PLAYER-017 / PLAYER-018)

両経路で一貫して null 防御が効くため、明示的コンストラクタの `if (id is null) throw ...` パターン(`with` 式に効かない)よりも安全。実装の詳細は `Assets/_Project/Scripts/Domain/Players/PlayerState.cs` を参照。

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Players/PlayerState.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Players/PlayerStateTests.cs`
- シナリオ: `player-state.feature`
- 設計根拠: [`docs/adr/0002-phase1-domain-boundaries.md`](../../../adr/0002-phase1-domain-boundaries.md) (Player N 人想定 / Domain 全体 immutable)
- 内部参照: [`hand.md`](../cards/hand.md) (Hand の値同値性)、[`player-id.md`](player-id.md)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| PLAYER-009 | (テスト免除: Ubiquitous) | `record class` + init-only プロパティで構造的に保証 |
| PLAYER-010 | (テスト免除: Ubiquitous) | record の自動 init-only プロパティで構造的に保証 |
| PLAYER-011 | `Given_有効なIdとHand_When_コンストラクタ_Then_Idが入力と同じ` / `Given_有効なIdとHand_When_コンストラクタ_Then_Handが入力と同じ` | 1 テスト 1 アサーション原則のため Id と Hand を分離 |
| PLAYER-012 | `Given_同じIdと同じHandの2つのPlayerState_When_Equals_Then_等価` / `Given_異なるId_When_Equals_Then_非等価` / `Given_異なるHand_When_Equals_Then_非等価` / `Given_独立した2人のPlayerState_When_Equals_Then_非等価` | N=1 同値 / Id 異 / Hand 異 / N=2 独立性(非等価)を網羅 |
| PLAYER-013 | `Given_等価な2つのPlayerState_When_GetHashCode_Then_同じ値を返す` | |
| PLAYER-014 | `Given_PlayerState_When_with式でHandを差し替え_Then_新インスタンスのIdは不変` / `Given_PlayerState_When_with式でHandを差し替え_Then_新インスタンスのHandが差し替え済み` / `Given_PlayerState_When_with式でHandを差し替え_Then_元インスタンスのHandは不変` | record `with` 式の標準動作を 3 観点に分離(新 Id / 新 Hand / 元不変) |
| PLAYER-015 | `Given_nullId_When_コンストラクタ_Then_ArgumentNullException` | コンストラクタ経由 → init setter で検証 |
| PLAYER-016 | `Given_nullHand_When_コンストラクタ_Then_ArgumentNullException` | コンストラクタ経由 → init setter で検証 |
| PLAYER-017 | `Given_PlayerState_When_with式でIdをnullに_Then_ArgumentNullException` | with 式経由 → 同じ init setter で検証 |
| PLAYER-018 | `Given_PlayerState_When_with式でHandをnullに_Then_ArgumentNullException` | with 式経由 → 同じ init setter で検証 |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
