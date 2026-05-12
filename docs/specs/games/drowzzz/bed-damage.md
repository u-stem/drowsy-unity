# ベッド破損機構 (M3-PR2)

ADR-0011 §3 で確定したベッド破損率の状態保持と、自フェーズ開始時の SDP マイナス計算を集約する仕様。

## 概要

- **配置**: `DrowZzzGameSession.BedDamages: IReadOnlyDictionary<PlayerId, int>`(各プレイヤーごとの破損率)
- **範囲**: 0〜100%(`DrowZzzBedConstants.MinBedDamagePercent` / `MaxBedDamagePercent`)
- **増加トリガー**: `DamageBedEffect`(M3-PR2 で導入、ADR-0011 §3 JIT 確定:カード固有値 + 常に 5 の倍数)
- **修繕**: M3-PR3 の `AbandonAction(AbandonChoice.RepairBed)` で -20%(下限 0%)
- **SDP マイナス換算**: `bedDamage / 5`(整数除算、`DrowZzzBedConstants.BedDamageRatePerSdp`)

## 普遍要件 (Ubiquitous)

- [DZ-198] [Ubiquitous] `DrowZzzGameSession.BedDamages` は `IReadOnlyDictionary<PlayerId, int>` 型で、キー集合は `GameState.Players` の `PlayerId` 集合と一致する(cross-field 検証、FDP / DDP / SDP / Influences と同パターン)。
- [DZ-198] [Ubiquitous] `BedDamages` の各値は `[0, 100]` の範囲を満たす(構築時 / `with` 経由いずれでも検証、範囲外は `ArgumentException`)。

## 事象駆動要件 (Event-driven)

- [DZ-197] When `EndTurnAction` is applied and the new current player has `BedDamages[currentPlayer] > 0`, the resulting session shall have `SecondDrowsyPoints[currentPlayer]` decreased by `BedDamages[currentPlayer] / DrowZzzBedConstants.BedDamageRatePerSdp`(integer division, `BedDamageRatePerSdp = 5`).
- [DZ-197] When `EndTurnAction` is applied and the new current player has `BedDamages[currentPlayer] == 0`, the resulting session shall leave `SecondDrowsyPoints` unchanged (no-op, graceful)。
- [DZ-197] When `EndTurnAction` is applied, the previous current player's `BedDamages` shall not trigger any SDP change in the resulting session (only the **new** current player's bed damage triggers).

## 評価順序(ADR-0011 §5「順序保証」と整合)

`DrowZzzRule.ApplyEndTurn` 内の順序:

1. **ベッド破損 SDP マイナス**(本仕様)
2. DDP 抽選(該当ラウンド、M2-PR4)
3. 影響 Tick(M2-PR5)
4. Round 21 完了検出 + Outcome 設定(M3-PR1)

## 定数依存

- `DrowZzzBedConstants.BedDamageRatePerSdp = 5`(L2 不変量、ADR-0011 §3 / 本仕様)
- `DrowZzzBedConstants.MinBedDamagePercent = 0` / `MaxBedDamagePercent = 100`(同上)

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §3
- 関連: [`effects/damage-bed-effect.md`](effects/damage-bed-effect.md)(破損率増加効果)
- 後続 PR: M3-PR3 で `AbandonAction(AbandonChoice.RepairBed)` 修繕機構を追加

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-197 | `DrowZzzRuleTests.Given_p2ベッド破損20_..._Then_p2のSDPがマイナス4` / `..._破損0_..._Then_p2のSDPは不変` / `..._破損100_..._Then_p2のSDPがマイナス20` / `..._p1ベッド破損40_..._Then_p1のSDPは不変` | 自フェーズ開始時計算 |
| DZ-198 | `DrowZzzGameSessionTests` の Bed プロパティ関連テスト(値保持 / null / cross-field / 負値 / 100 超 / Equals 寄与)| 6 テスト |
