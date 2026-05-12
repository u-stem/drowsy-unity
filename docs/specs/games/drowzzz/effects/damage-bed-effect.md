# `DamageBedEffect` (M3-PR2)

ADR-0011 §3 で確定したベッド破損率増加トリガーの効果 record。

## 構造

```csharp
public sealed record DamageBedEffect(SdpTarget Target, int Percent) : IEffect;
```

- `Target`: 破損を与える対象プレイヤー(`SdpTarget.Self` / `SdpTarget.Opponent`)
- `Percent`: 破損率の増加幅(**常に 5 の正の倍数**、ADR-0011 §3 JIT 確定 2026-05-12)

## 普遍要件 (Ubiquitous)

- [DZ-193] [Ubiquitous] `DamageBedEffect` は `sealed record` で、positional `(SdpTarget Target, int Percent)` を持ち `IEffect` を実装する。

## 構築時検証

- [DZ-194] When `DamageBedEffect` is constructed with `Percent <= 0` or `Percent % DrowZzzBedConstants.BedDamageRatePerSdp != 0`, the constructor shall throw `ArgumentException`. The same protection applies to `with { Percent = ... }` mutation.

## Apply の挙動

- [DZ-195] When `DamageBedEffect(Target, Percent)` is applied, the target player's `BedDamages` value shall be increased by `Percent`. Other players' `BedDamages` shall remain unchanged.
- [DZ-196] When the increased `BedDamages` value would exceed `DrowZzzBedConstants.MaxBedDamagePercent`(= 100), the value shall be clamped to `MaxBedDamagePercent`.

## 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| `Percent` を任意 int で受ける | JIT 確定「常に 5 の倍数」を record 検証で強制する方が筋(CLAUDE.md §9 マジックナンバー禁止、`DrowZzzBedConstants` 集約と整合)|
| 修繕効果も `DamageBedEffect(Percent = -20)` で表現 | 「破損を与える」と「修繕する」は意味的に別軸、修繕は `AbandonChoice.RepairBed` の責務(M3-PR3、ADR-0011 §2)|
| 破損率の下限(0%)もクランプ機構を導入 | `Percent > 0` 検証で構造的に減算が発生しないため、下限クランプは不要 |

## 関連

- ADR: [`docs/adr/0011-m3-dream-card-and-game-mechanics-expansion.md`](../../../adr/0011-m3-dream-card-and-game-mechanics-expansion.md) §3
- 親仕様: [`../bed-damage.md`](../bed-damage.md)
- 実装(本 PR):
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/DamageBedEffect.cs`
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/Effects/EffectInterpreter.cs`(case 追加)
  - `Assets/_Project/Scripts/Application/Games/DrowZzz/DrowZzzBedConstants.cs`(L2 const 集約)
- テスト(本 PR): `Assets/_Project/Scripts/Tests/Application.Tests/Games/DrowZzz/Effects/DamageBedEffectTests.cs`

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| DZ-193 | `Given_有効な引数_..._Then_Targetが入力と一致する` / `..._Then_Percentが入力と一致する` | 構築の正常系 |
| DZ-194 | `Given_Percentが5の倍数でない_..._Then_ArgumentExceptionを投げる` / `..._0_..._Then_ArgumentExceptionを投げる` / `..._負値_..._Then_ArgumentExceptionを投げる` / `Given_withで不正なPercent_..._Then_ArgumentExceptionを投げる` | 構築時検証 + with 経由 |
| DZ-195 | `..._TargetSelf_Percent20_..._p1のBedDamagesが20に増加` / `..._TargetOpponent_..._p2のBedDamagesが20に増加` / `..._既存破損30_..._累積50` / `..._TargetSelf_..._Then_他プレイヤーのBedDamagesは不変` | Apply 挙動 |
| DZ-196 | `..._既存破損90_..._100でクランプ` / `..._既存破損100_..._100のまま` | 上限クランプ |
