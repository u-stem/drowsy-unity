# DrawCardEffect (M2-PR3)

カード使用者(または将来的に被使用者)が山札から指定枚数を手札に引く効果。

## 概要

「コップ一杯の脅威」(No.01)夜効果の「甲: 山札から手段を 1 枚引く」を表現するため、M2-PR3 で導入。`SdpTarget Target` + `int Count` の組み合わせで、誰が何枚引くかを表現する。

M2-PR3 範囲では `Target = SdpTarget.Self` のみ実装(現プレイヤーが追加で引く)。`Target = SdpTarget.Opponent` は将来拡張(現状 ADR-0009 仕様カードでは出現しない)。

仕様:
- `Pile.Draw()` を `Count` 回繰り返し、現プレイヤーの `Hand` に追加
- 山札が空になった時点で残りはスキップ(例外を投げない、現実装の `DrawCardAction.Apply` と同じ態度)
- 山札枯渇は ADR-0007 §「山札枯渇」で「初期配布 10 + 通常 Draw 42 + DrawCardEffect 追加分 ≤ 56」を維持(本 PR の「コップ一杯の脅威」夜効果で +最大 2 ドロー、合計 54 ≤ 56、`docs/todo.md` の枯渇監視 TODO 継続)

## 普遍要件 (Ubiquitous)

- [DZ-114] [Ubiquitous] The `DrawCardEffect` shall be a `sealed record` with positional `(SdpTarget Target, int Count)` implementing `IEffect`, declared in `Drowsy.Application.Games.DrowZzz.Effects` namespace.

## 事象駆動要件 (Event-driven)

- [DZ-115] When `EffectInterpreter.Apply` is called with `DrawCardEffect(Self, 1)` and pile has at least 1 card, the current player shall draw the top card into their hand.
- [DZ-116] When `EffectInterpreter.Apply` is called with `DrawCardEffect(Self, N)` and pile has at least N cards, the current player shall draw the top N cards (in order) into their hand.

## 異常要件 (Unwanted)

- [DZ-117] If `EffectInterpreter.Apply` is called with `DrawCardEffect(Self, N)` and the pile becomes empty before N cards are drawn, then the effect shall stop drawing without throwing (graceful degradation, matches `DrawCardAction.Apply` behavior).

## 任意要件 (Optional)

- [DZ-118] [Optional] `DrawCardEffect(Opponent, _)` は本 PR の効果実装範囲外(現状想定カードでは未使用、`docs/todo.md` で将来実装を追跡)。Apply 時には `NotImplementedException` を投げる選択肢もあるが、M2-PR3 段階では unreachable として `EffectInterpreter` の本体実装で対応する(case 内で `target == Self` のみを処理し、Opponent 引数が来た場合は明示的に例外を投げる方が将来の防御として有用)。

## 定数依存

該当なし。`Count` はカードデータ固有(JIT 共有)、定数集約しない。

## 関連

- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §「山札枯渇」 — DrawCardEffect 追加時の枯渇計算を更新
- ADR: [`docs/adr/0006-m1-detail-application-interfaces.md`](../../../../adr/0006-m1-detail-application-interfaces.md) §M1-PR4 — `DrawCardAction.Apply` 既存ロジック(本機能と同じ「山札→手札」移動を流用)
- 実装 (本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/DrawCardEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs` (case 追加)
- テスト (本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/DrawCardEffectTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-114 | (テスト免除: Ubiquitous) | record 宣言で構造的に保証 |
| DZ-115 | `Given_TargetSelf_Count1_When_Apply_Then_手札にトップカードが1枚追加される` | 1 枚ドロー |
| DZ-116 | `Given_TargetSelf_Count3_When_Apply_Then_手札に上位3枚が追加される` | 複数枚ドロー |
| DZ-117 | `Given_山札が2枚でCount3_When_Apply_Then_手札に2枚追加され例外を投げない` | 枯渇時 graceful |
| DZ-118 | (テスト免除: Optional) | M2-PR3 で `DrawCardEffect(Opponent, _)` は `NotImplementedException`、テストは `Given_TargetOpponent_When_Apply_Then_NotImplementedExceptionを投げる` で明示防御 |
