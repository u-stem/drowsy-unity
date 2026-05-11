# AdjustSdpEffect (M2-PR3)

`SecondDrowsyPoints` を加減する効果 record。M2-PR3 で導入する最初の actor 拡張(Self / Opponent)効果。

## 概要

ADR-0007 §1.4「他者影響系 actor 拡張」の JIT 確定として、`SdpTarget` enum(`Self` / `Opponent`)を `AdjustSdpEffect` の positional 引数に取る。`Delta` は加減算量(正値 = 増、負値 = 減、0 = no-op)。

仕様:
- Target=Self: 現プレイヤー(`GameState.Turn.CurrentPlayerIndex`)の SDP を `Delta` 増減
- Target=Opponent: N=2 想定で「現プレイヤー以外のもう 1 人」の SDP を `Delta` 増減
- 0 floor なし: 結果が負値になっても許容(DZ-109 と整合、ADR-0009「持ち点低い方が勝ち」)

## 普遍要件 (Ubiquitous)

- [DZ-110] [Ubiquitous] The `AdjustSdpEffect` shall be a `sealed record` with positional `(SdpTarget Target, int Delta)` implementing `IEffect`, declared in `Drowsy.Application.Games.DrowZzz.Effects` namespace.

## 事象駆動要件 (Event-driven)

- [DZ-111] When `EffectInterpreter.Apply` is called with `AdjustSdpEffect(Self, delta)`, the `SecondDrowsyPoints[currentPlayer]` shall increase by `delta`.
- [DZ-112] When `EffectInterpreter.Apply` is called with `AdjustSdpEffect(Opponent, delta)` (N=2), the `SecondDrowsyPoints[opponent]` shall increase by `delta` where `opponent` is the unique non-current player.
- [DZ-113] When `EffectInterpreter.Apply` is called with `AdjustSdpEffect(_, negativeDelta)` and the resulting value would be negative, the resulting `SecondDrowsyPoints` value shall be retained as negative (no 0 floor, DZ-109 と整合).

## 定数依存

該当なし。`Delta` はカードデータ(JIT 共有)固有の数値で、定数集約しない(L3 個別カード設計値)。

## 関連

- ADR: [`docs/adr/0007-m2-detail-card-effects.md`](../../../../adr/0007-m2-detail-card-effects.md) §1.4 — 他者影響系 actor 拡張の JIT 確定(本 PR で確定済)
- ADR: [`docs/adr/0009-m2-m3-dp-and-victory-conditions.md`](../../../../adr/0009-m2-m3-dp-and-victory-conditions.md) — SDP 機構と「持ち点低い方が勝ち」
- 前提仕様: [`../dp-mechanism.md`](../dp-mechanism.md) — SDP プロパティと `SdpTarget` enum
- 実装 (本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/AdjustSdpEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加)
- テスト (本 PR):
  - `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/AdjustSdpEffectTests.cs`
- シナリオ: `adjust-sdp.feature`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-110 | (テスト免除: Ubiquitous) | `public sealed record AdjustSdpEffect(SdpTarget Target, int Delta) : IEffect` 宣言で構造的に保証 |
| DZ-111 | `Given_TargetSelf_Delta10_When_Apply_Then_現プレイヤーSDPが10増加` | Self ケース |
| DZ-112 | `Given_TargetOpponent_Delta10_When_Apply_Then_相手のSDPが10増加` | Opponent ケース、N=2 |
| DZ-113 | `Given_SDP0_DeltaMinus5_When_Apply_Then_SDPがマイナス5になる` | 負値許容(0 floor なし) |
