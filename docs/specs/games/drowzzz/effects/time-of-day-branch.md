# TimeOfDayBranchEffect (M2-PR3)

「同じカードが夜と朝で異なる効果を発動する」を 1 効果 record で表現するラッパー record。

## 概要

「コップ一杯の脅威」(No.01)のように **同一カードが夜と朝で完全に違う効果列を持つ** ケースを 1 ストラクチャで表現するため、M2-PR3 で導入(ADR-0008 §8 「Clock を参照する効果の最初の登場時に JIT 確定」の確定形)。

設計:
- 単一 record `TimeOfDayBranchEffect(IReadOnlyList<IEffect> NightEffects, IReadOnlyList<IEffect> MorningEffects)` で夜・朝の効果列を保持
- `EffectInterpreter.Apply` が `session.Clock.IsNight` / `IsMorning` を判定し、該当する effect 列を `Aggregate` で逐次評価する
- 夜でも朝でもない時刻(`Round 22+` 等、ADR-0008 §5 過渡的範囲)では **どちらの効果列も評価しない**(no-op)

採用理由(ADR-0008 §8 JIT 判断):
- カードデータ(InMemoryCardCatalog の `Effects`)で 1 行に「夜・朝」両方を読める
- 「夜限定カード」「朝限定カード」も `MorningEffects: []` / `NightEffects: []` で同じ record で表現可能
- 不採用 案: `WhenNightEffect` / `WhenMorningEffect` を 2 record にすると、カード Effects 列が長くなる
- 不採用 案: `ICardCatalog` 動的返却 → ICardCatalog シグネチャの再設計が大きい(ADR-0007 §3 / §6 と非互換)

## 普遍要件 (Ubiquitous)

- [DZ-119] [Ubiquitous] The `TimeOfDayBranchEffect` shall be a `sealed record` with `(IReadOnlyList<IEffect> NightEffects, IReadOnlyList<IEffect> MorningEffects)` properties implementing `IEffect`, declared in `Drowsy.Application.Games.DrowZzz.Effects` namespace.

## 事象駆動要件 (Event-driven)

- [DZ-120] When `EffectInterpreter.Apply` is called with `TimeOfDayBranchEffect(night, morning)` and `session.Clock.IsNight` is `true`, the `night` effects shall be aggregated (left-to-right) into the session.
- [DZ-121] When `EffectInterpreter.Apply` is called with `TimeOfDayBranchEffect(night, morning)` and `session.Clock.IsMorning` is `true`, the `morning` effects shall be aggregated (left-to-right) into the session.

## 状態駆動要件 (State-driven)

- [DZ-122] While `session.Clock.IsNight` and `session.Clock.IsMorning` are both `false` (e.g. `RoundNumber > 21` 過渡的範囲、ADR-0008 §5 / DZ-098), the `TimeOfDayBranchEffect` shall be a no-op (return session unchanged).

## 異常要件 (Unwanted)

- [DZ-123] If `TimeOfDayBranchEffect` is constructed with `null` `NightEffects`, then it shall throw `ArgumentNullException`.
- [DZ-124] If `TimeOfDayBranchEffect` is constructed with `null` `MorningEffects`, then it shall throw `ArgumentNullException`.

## 定数依存

該当なし。

## 関連

- ADR: [`docs/adr/0008-m2-drowzzz-clock-and-night-morning.md`](../../../../adr/0008-m2-drowzzz-clock-and-night-morning.md) §8 — Clock 参照効果の最初の登場時に JIT 確定する事項、本機能で確定
- 前提仕様: [`../clock.md`](../clock.md) — `DrowZzzClock.IsNight` / `IsMorning`
- 実装 (本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/TimeOfDayBranchEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs` (case 追加)
- テスト (本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/TimeOfDayBranchEffectTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-119 | (テスト免除: Ubiquitous) | record 宣言で構造的に保証 |
| DZ-120 | `Given_夜のClock_NightEffectsSdpPlus10_When_Apply_Then_SDPが10増加する` | 夜分岐 |
| DZ-121 | `Given_朝のClock_MorningEffectsSdpPlus10_When_Apply_Then_SDPが10増加する` | 朝分岐 |
| DZ-122 | `Given_Round22のClock_When_Apply_Then_sessionが変化しない` | 過渡的範囲 |
| DZ-123 | `Given_NightEffectsにnull_When_生成_Then_ArgumentNullExceptionを投げる` | null 防御 |
| DZ-124 | `Given_MorningEffectsにnull_When_生成_Then_ArgumentNullExceptionを投げる` | null 防御 |
