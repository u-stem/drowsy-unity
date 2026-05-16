# Hand

プレイヤーの手札を表す不変値オブジェクト。順序付きユニーク `CardId` 集合で、追加順を保つ。

> **Note(ADR-0018 関連、2026-05-16)**:本 spec の `unique CardId 集合` 制約(HAND-003 / 005)は、[ADR-0018](../../../adr/0018-cardtypeid-cardid-instance-separation.md) で `CardId` が `(CardTypeId, int Instance)` 複合型に refactor されたことで意味が正当化された(catalog 種別の重複は `CardTypeId` 側で許容し、Hand 内では同種カードでも instance が異なれば共存可能)。EARS 文言の変更は不要。

## 概要

`Hand` は `CardId` を順序付きで保持する不変集合。同じ `CardId` の重複を許容しない(`CardId` は一意識別子)。`Add` / `Remove` 操作は新 `Hand` を返す純関数。`Equals` / `GetHashCode` / `operator==` / `operator!=` を順序付きシーケンス同値で override する(設計判断は ADR-0002 / 値同値性については本 PR で確定)。

`Pile` と内部実装パターンは類似するが、API は手札意味語彙(`Add` / `Remove` / `Contains`)で公開する独立クラス。

## 普遍要件 (Ubiquitous)

- [HAND-001] [Ubiquitous] The `Hand` shall be immutable.
- [HAND-002] [Ubiquitous] The `Hand` shall expose its cards as a read-only ordered list via `Cards` (`IReadOnlyList<CardId>`).

## 事象駆動要件 (Event-driven)

- [HAND-003] When `new Hand(cards)` is called with a non-null, distinct, non-null-element `cards`, the constructed instance shall hold the same elements in the same order via `Cards`.
- [HAND-004] When the static property `Hand.Empty` is accessed, it shall return a singleton whose `Count` is `0` and `IsEmpty` is `true`.
- [HAND-005] When `Add(card)` is called with a `card` not already in the hand, the `Hand` shall return a new instance whose `Cards` is the original sequence followed by `card`.
- [HAND-006] When `Remove(card)` is called with a `card` already in the hand, the `Hand` shall return a new instance whose `Cards` excludes `card` while preserving the relative order of remaining cards.
- [HAND-007] When `Contains(card)` is called and `card` exists in `Cards`, the `Hand` shall return `true`.
- [HAND-008] When `Contains(card)` is called and `card` does not exist in `Cards`, the `Hand` shall return `false`.
- [HAND-009] When `Count` is read, the `Hand` shall return the number of cards held.
- [HAND-010] When `IsEmpty` is read, the `Hand` shall return `true` if `Count == 0`, and `false` otherwise.
- [HAND-011] When two `Hand` instances are compared with `Equals`, they shall be equal if and only if their `Cards` sequences are equal in order and length.
- [HAND-012] When `GetHashCode` is called on two equal `Hand` instances, they shall return the same hash value.
- [HAND-013] When two `Hand` references are compared with `operator==` or `operator!=`, the result shall be value-equal consistent with `Equals`. Both null operands compare as equal; one-sided null compares as not equal.
- [HAND-014] When `Equals(object)` is called with `null` or an argument that is not a `Hand`, the `Hand` shall return `false`.

## 異常要件 (Unwanted)

- [HAND-015] If `cards` passed to the constructor is null, then the constructor shall throw `ArgumentNullException`.
- [HAND-016] If `cards` contains a null `CardId`, then the constructor shall throw `ArgumentException`.
- [HAND-017] If `cards` contains duplicate `CardId` values, then the constructor shall throw `ArgumentException`.
- [HAND-018] If `Add(null)` is called, then the `Hand` shall throw `ArgumentNullException`.
- [HAND-019] If `Add(card)` is called with a `card` already in the hand, then the `Hand` shall throw `ArgumentException`.
- [HAND-020] If `Remove(null)` is called, then the `Hand` shall throw `ArgumentNullException`.
- [HAND-021] If `Remove(card)` is called with a `card` not in the hand, then the `Hand` shall throw `ArgumentException`.
- [HAND-022] If `Contains(null)` is called, then the `Hand` shall throw `ArgumentNullException`.

## 実装メモ

### Pile との違い

| 観点 | Pile (山札 / 捨て札) | Hand (手札) |
| ---- | ---- | ---- |
| 重複 | 許容(同じ CardId が積まれることはないが API 上は許容) | 拒否(コンストラクタ・Add で例外) |
| 順序の意味 | Top / Bottom が明示的(AddTop / AddBottom / Draw) | 追加順を保つが Top/Bottom 概念は無い |
| 主操作 | AddTop / AddBottom / Draw / Shuffle | Add / Remove / Contains |
| 値同値性 | 現状なし(参照同値)。将来別 PR で揃える予定 | あり(順序付きシーケンス同値) |

### Equals / GetHashCode の実装方針

- `Equals(Hand)`: `ReferenceEquals` 短絡 → `null` チェック → `Count` 一致 → `Cards` の順序付きシーケンス比較
- `Equals(object)`: `is Hand other` パターンマッチで型不一致・null を `false` に帰着 (HAND-014)
- `GetHashCode`: 順序依存ハッシュ。`HashCode` struct (`new HashCode()`) に各 `CardId` を `.Add()` で順次合成し `.ToHashCode()` で値を取得(C# `System.HashCode` の標準パターン)
- `operator==`: `left is null ? right is null : left.Equals(right)` で null 安全

### 不変コレクションの表現

ADR-0002 と既存 `Pile` に揃え、`private readonly CardId[]` を内部に保持し `IReadOnlyList<CardId>` として公開する。`System.Collections.Immutable` は Unity 6 / 当該 Mono 版で利用不可のため独自パターン。

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Cards/Hand.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Cards/HandTests.cs`
- シナリオ: `hand.feature`
- 設計根拠: [`docs/adr/0002-phase1-domain-boundaries.md`](../../../adr/0002-phase1-domain-boundaries.md) (Hand を Pile と独立クラスに / Domain 全体 immutable / Player N 人想定)
- 参照実装: [`pile.md`](pile.md) (内部実装パターンの踏襲元)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| HAND-001 | (テスト免除: Ubiquitous) | `sealed class` + `private readonly` フィールドで構造的に保証 |
| HAND-002 | (テスト免除: Ubiquitous) | `Cards => _cards` (`IReadOnlyList`) で構造的に保証 |
| HAND-003 | `Given_有効なcards_When_コンストラクタ_Then_Cardsが入力と同じ順序で保持される` | |
| HAND-004 | `Given_HandEmpty_When_Count参照_Then_0` / `Given_HandEmpty_When_IsEmpty参照_Then_true` | シングルトン挙動を 2 観点で確認 |
| HAND-005 | `Given_既存にないCardId_When_Add_Then_末尾に追加された新Handが返る` | |
| HAND-006 | `Given_存在するCardId_When_Remove_..._残りの順序が保たれる` (中間) / `Given_先頭のCardId_When_Remove_..._先頭が除去` / `Given_末尾のCardId_When_Remove_..._末尾が除去` | Array.Copy 境界を網羅(中間 / 先頭 / 末尾) |
| HAND-007 | `Given_存在するCardId_When_Contains_Then_true` | |
| HAND-008 | `Given_存在しないCardId_When_Contains_Then_false` | |
| HAND-009 | `Given_2枚のHand_When_Count_Then_2` | |
| HAND-010 | `Given_空Hand_When_IsEmpty_Then_true` / `Given_1枚以上のHand_When_IsEmpty_Then_false` | |
| HAND-011 | `Given_同順序同要素のHand_When_Equals_Then_等価` / `Given_同枚数で異なる順序_When_Equals_Then_非等価` / `Given_同枚数で異なるカード_When_Equals_Then_非等価` / `Given_異なる枚数_When_Equals_Then_非等価` / `Given_同一インスタンス_When_Equals_Then_等価` / `Given_両方空Hand_When_Equals_Then_等価` / `Given_null_When_EqualsHand_Then_false` | n=0 / n=1 / n=2 のサイズ網羅 + ReferenceEquals 短絡 + null 引数 |
| HAND-012 | `Given_等価な2つのHand_When_GetHashCode_Then_同じ値を返す` / `Given_両方空Hand_When_GetHashCode_Then_同じ値を返す` | n=0 と n>0 のハッシュ一致を網羅 |
| HAND-013 | `Given_等価な2つのHand_When_operator_等価_Then_true` / `Given_非等価な2つのHand_When_operator_等価_Then_false` / `Given_非等価な2つのHand_When_operator_非等価_Then_true` / `Given_両方null_When_operator_等価_Then_true` / `Given_片方nullで他方非null_When_operator_等価_Then_false` (left=null) / `Given_左側非nullで右側null_When_operator_等価_Then_false` (right=null) | == 両側 null パターンの両経路を網羅 |
| HAND-014 | `Given_null_When_Equalsオブジェクト_Then_false` / `Given_異なる型_When_Equalsオブジェクト_Then_false` | |
| HAND-015 | `Given_null_When_コンストラクタ_Then_ArgumentNullException` | |
| HAND-016 | `Given_nullCardIdを含むcards_When_コンストラクタ_Then_ArgumentException` | |
| HAND-017 | `Given_重複CardIdを含むcards_When_コンストラクタ_Then_ArgumentException` | |
| HAND-018 | `Given_null_When_Add_Then_ArgumentNullException` | |
| HAND-019 | `Given_既存CardId_When_Add_Then_ArgumentException` | |
| HAND-020 | `Given_null_When_Remove_Then_ArgumentNullException` | |
| HAND-021 | `Given_不在CardId_When_Remove_Then_ArgumentException` | |
| HAND-022 | `Given_null_When_Contains_Then_ArgumentNullException` | Add/Remove 経由ではなく直接 `Contains(null)` を呼ぶケースをカバー |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
