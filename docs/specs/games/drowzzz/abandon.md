# 放棄機構(`AbandonAction`)(M3-PR3)

ADR-0011 §2 で確定した「放棄(代替ターン行動)」機構の仕様。`PlayCardAction` の代わりに `WaitingForPlay` フェーズで選択可能な action。

## 概要

| 観点 | 値 |
| ---- | ---- |
| Action | `AbandonAction(AbandonChoice Choice, int CardIndex = 0)` |
| 選択肢 | `AbandonChoice.GainSdp`(SDP +5)/ `AbandonChoice.RepairBed`(ベッド -20%、下限 0%)|
| 合法フェーズ | `WaitingForPlay`(`PlayCardAction` と同じフェーズで選択)|
| 必要条件 | 手札 1 枚以上 + `CardIndex` 範囲内 + (RepairBed なら BedDamages > 0%)|
| Apply 後フェーズ | `WaitingForEndTurn` |

## 普遍要件 (Ubiquitous)

- [DZ-199.0] [Ubiquitous] `AbandonAction` は `sealed record` で `(AbandonChoice Choice, int CardIndex = 0)` を持ち、`DrowZzzAction` の派生型である。

## 合法性判定(`IsLegalMove`)

- [DZ-199] When `AbandonAction` is evaluated with `session.PhaseState == WaitingForPlay` and the current player's hand has at least 1 card and `CardIndex` is in `[0, Hand.Count)`, `IsLegalMove` shall return `true`. Otherwise `false`.
- [DZ-200] When `AbandonAction(Choice == AbandonChoice.RepairBed)` is evaluated, additionally `BedDamages[currentPlayer] > 0%` must be satisfied for `IsLegalMove` to return `true`(JIT 確定 2026-05-13、(b) 不可選択を採用)。
- [DZ-200] When `AbandonAction(Choice == AbandonChoice.GainSdp)` is evaluated with `BedDamages[currentPlayer] == 0`, `IsLegalMove` shall still return `true`(GainSdp はベッド状態に依存しない)。

## 状態遷移(`Apply`)

- [DZ-201] When `AbandonAction(_, CardIndex)` is applied, the current player's hand shall lose the card at `Hand.Cards[CardIndex]`, and `GameState.Discard` shall gain that card via `AddTop`. The `PhaseState` shall transition to `WaitingForEndTurn`.
- [DZ-202] When `AbandonAction(AbandonChoice.GainSdp, _)` is applied, the current player's `SecondDrowsyPoints` shall increase by `DrowZzzBedConstants.AbandonSdpGain`(= 5)。
- [DZ-203] When `AbandonAction(AbandonChoice.RepairBed, _)` is applied, the current player's `BedDamages` shall decrease by `DrowZzzBedConstants.BedRepairPercent`(= 20%), with `Math.Max(MinBedDamagePercent, ...)` 下限クランプ(0%).

## 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| 修繕 0% 時に SDP+5 にフォールバック | JIT 確定で「(b) 不可選択」を採用、フォールバックは UI / Rule 責務混在 |
| 修繕 0% 時も合法(無駄選択許容) | 同上、JIT 確定で「不可選択」採用 |
| 捨て対象カードを Rule 内ランダム抽選 | JIT 確定で「(i) プレイヤー選択(index 指定)」を採用 |
| 捨て対象を FIFO / LIFO 固定 | 同上、プレイヤー操作の自由度を確保 |

## 「本能(Instinct)」キーワードとの関係(M3-PR5 で追加予定)

本 PR(M3-PR3)範囲では `CardIndex` 制約は「範囲内 + 手札非空」のみ。M3-PR5(キーワード能力)で「本能」キーワードを持つ効果列を含むカードを `IsLegalMove` で除外する追加制約が入る(ADR-0011 §4)。

## 定数依存

- `DrowZzzBedConstants.AbandonSdpGain = 5`(L2、ADR-0011 §2 / 本 PR で集約)
- `DrowZzzBedConstants.BedRepairPercent = 20`(L2、ADR-0011 §2 / 本 PR で集約)
- `DrowZzzBedConstants.MinBedDamagePercent = 0`(下限クランプ用、M3-PR2 から継続)

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §2
- 関連: [`bed-damage.md`](bed-damage.md)(ベッド破損機構)/ M3-PR5(キーワード能力、本能との連携)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzAction.cs`(`AbandonAction` 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/AbandonChoice.cs`(新規)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzRule.cs`(`IsLegalAbandon` / `ApplyAbandon`)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzBedConstants.cs`(`BedRepairPercent` / `AbandonSdpGain` 追加)
- テスト(本 PR): `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/AbandonActionTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-199 | `IsLegalMove` 関連 4 件(WaitingForPlay 正常 / WaitingForDraw / 手札 0 / CardIndex 範囲外)+ Apply 防御例外 1 件 | 合法性基本 |
| DZ-200 | RepairBed 関連 3 件(ベッド 0 で false / 20% で true / GainSdp はベッド 0 でも true)+ Apply 防御例外 1 件 | 修繕 0% 不可選択 |
| DZ-201 | Apply 関連 4 件(手札 1 枚減 / Discard 追加 / CardIndex 1 で c2 / PhaseState 遷移) | 手札 → Discard 移動 |
| DZ-202 | `Given_SDP10_GainSdp_..._Then_SDPが15になる` | SDP +5 |
| DZ-203 | RepairBed 3 件(40 → 20 / 10 → 0 クランプ / 100 → 80) | 修繕 -20% + 下限クランプ |
