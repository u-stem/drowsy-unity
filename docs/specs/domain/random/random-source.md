# IRandomSource / XorShiftRandom(乱数源)

決定的な乱数を提供する抽象インターフェースとその XorShift32 実装。

## 概要

`IRandomSource` はゲームロジックで使う乱数の抽象。テスト時に seed を固定することで再現可能なゲーム挙動を保証する。`XorShiftRandom` はその純 C# 実装で、UnityEngine 非依存。

## 普遍要件 (Ubiquitous)

- [RND-001] The `IRandomSource` shall provide `NextInt(min, maxExclusive)` returning an integer `i` such that `min <= i < maxExclusive`.
- [RND-002] The `XorShiftRandom` shall produce a deterministic sequence given the same seed.

## 事象駆動要件 (Event-driven)

- [RND-003] When `XorShiftRandom` is constructed with a seed and `NextInt` is called repeatedly, the sequence shall be reproducible across multiple instances with the same seed.

## 異常要件 (Unwanted)

- [RND-004] If `NextInt(min, maxExclusive)` is called with `maxExclusive <= min`, then the implementation shall throw `ArgumentException`.

## 任意要件 (Optional)

- [RND-005] [Optional] Where `seed == 0` is passed to `XorShiftRandom`, the constructor shall internally substitute `seed = 1` to avoid the degenerate fixed point of XorShift32 (state of all zeros yields zero forever).

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Random/IRandomSource.cs` / `XorShiftRandom.cs`
- テスト: `Assets/_Project/Scripts/Tests/Domain.Tests/Random/XorShiftRandomTests.cs`
- シナリオ: `random-source.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| RND-001 | `Given_seed42_When_NextIntを範囲内で100回呼ぶ_Then_全て範囲内の整数を返す` | |
| RND-002 | `Given_同じseedの2つのインスタンス_When_NextIntを繰り返し呼ぶ_Then_同じ系列を生成` | |
| RND-003 | `Given_同じseedの2つのインスタンス_When_NextIntを繰り返し呼ぶ_Then_同じ系列を生成` | RND-002 と同テストでカバー(再現性は両方の側面) |
| RND-004 | `Given_maxExclusiveがminより小さい_When_NextInt_Then_ArgumentException` / `Given_maxExclusiveがminと同じ_When_NextInt_Then_ArgumentException` | |
| RND-005 | `Given_seed0とseed1_When_NextIntを呼ぶ_Then_両者の系列は完全一致` | |

ID 規約全体は [`docs/testing-strategy.md`](../../../testing-strategy.md) を参照。
