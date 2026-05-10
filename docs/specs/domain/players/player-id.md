# PlayerId

プレイヤーの一意識別子を表す不変値オブジェクト。

## 概要

`PlayerId` は string をラップした値オブジェクトで、空白文字列・null での生成を禁止する。`record` による値同等性を持つ(`CardId` と完全対称な API パターン)。

## 普遍要件 (Ubiquitous)

- [PLAYER-001] [Ubiquitous] The `PlayerId` shall be immutable.
- [PLAYER-002] [Ubiquitous] The `PlayerId` shall expose its underlying `Value` as a read-only string.
- [PLAYER-003] Two `PlayerId` instances with the same `Value` shall be considered equal.

## 事象駆動要件 (Event-driven)

- [PLAYER-004] When `PlayerId.Of(value)` is called with a non-empty, non-whitespace string, the `PlayerId` shall return a new instance whose `Value` equals the input.
- [PLAYER-005] When `ToString()` is called, the `PlayerId` shall return its `Value`.

## 異常要件 (Unwanted)

- [PLAYER-006] If `PlayerId.Of(null)` is called, then the `PlayerId` shall throw `ArgumentException`.
- [PLAYER-007] If `PlayerId.Of("")` is called, then the `PlayerId` shall throw `ArgumentException`.
- [PLAYER-008] If `PlayerId.Of("   ")` (whitespace only) is called, then the `PlayerId` shall throw `ArgumentException`.

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Players/PlayerId.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Players/PlayerIdTests.cs`
- シナリオ: `player-id.feature`
- 設計根拠: [`docs/adr/0002-phase1-domain-boundaries.md`](../../../adr/0002-phase1-domain-boundaries.md) (Player N 人想定)
- 参照実装: [`docs/specs/domain/cards/card-id.md`](../cards/card-id.md) (同じ API パターン、CardId と対称)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| PLAYER-001 | (テスト免除: Ubiquitous) | `sealed record` で構造的に保証 |
| PLAYER-002 | (テスト免除: Ubiquitous) | `Value { get; }` で構造的に保証 |
| PLAYER-003 | `Given_同じ値の2つのPlayerId_When_等価比較_Then_等価` / `Given_異なる値の2つのPlayerId_When_等価比較_Then_非等価` | record の自動生成等価性 |
| PLAYER-004 | `Given_有効な値_When_Ofを呼ぶ_Then_インスタンスが生成され_Valueは入力と同じ` | |
| PLAYER-005 | `Given_PlayerId_When_ToString_Then_Valueを返す` | |
| PLAYER-006 | `Given_null_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる` | |
| PLAYER-007 | `Given_空文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる` | |
| PLAYER-008 | `Given_空白のみの文字列_When_Ofを呼ぶ_Then_ArgumentExceptionを投げる` | |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
