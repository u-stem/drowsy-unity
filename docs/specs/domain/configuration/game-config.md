# IGameConfig(ゲームバランス設定の抽象)

ゲームバランス調整可能値の抽象 interface。Domain で純粋宣言、Infrastructure で ScriptableObject 実装、Bootstrap で DI 注入。

## 概要

`IGameConfig` は L3(ゲームバランス調整可能値、`docs/architecture/constants-management.md` 参照)を表す抽象 interface。具体的な値(初期手札数、最大ライフ等)はゲームデザイナーが Inspector から調整可能で、Domain 層は interface 経由でのみ値を参照する。これにより:

- ゲームバランス調整がコード変更不要(ScriptableObject Inspector で完結)
- テスト時に Mock / Stub で異なる値を注入可能
- Domain は UnityEngine 依存なしで balance 値を扱える

## 普遍要件 (Ubiquitous)

- [CFG-001] [Ubiquitous] The `IGameConfig` shall be a pure interface in the `Drowsy.Domain.Configuration` namespace(ADR-0002 / `IGameConfig.cs`)。
- [CFG-002] [Ubiquitous] The `IGameConfig` shall not depend on `UnityEngine` (Domain asmdef の `noEngineReferences: true` で物理保証)。
- [CFG-003] [Ubiquitous] The `IGameConfig` shall expose only read-only properties (no setters)。
- [CFG-101] [Ubiquitous] The `IGameConfig` shall expose `FdpPool` as `IReadOnlyList<int>` (read-only)(ADR-0006 §1.4 / §M1、M1-PR3 で追加)。
- [CFG-103] [Ubiquitous] The `IGameConfig` shall expose `DdpPool` as `IReadOnlyList<int>` (read-only)(ADR-0009 §「DDP プールの構造」、M2-PR4 で追加。`StartGameUseCase` で `Shuffle` 後 `DrowZzzGameSession.DdpPool` に格納する)。

## 事象駆動要件 (Event-driven)

Phase 2 以降で具体プロパティを追加する際に、各プロパティに対する Event-driven 要件を CFG-101 〜 系統で追記する。

| プロパティ | 追加 PR | 関連要件 ID | 備考 |
| ---- | ---- | ---- | ---- |
| `FdpPool` | M1-PR3 | CFG-101 | DrowZzz の現行値は `[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]`、ADR-0006 §1.4 / §M1 |
| `DdpPool` | M2-PR4(本 PR) | CFG-103 | DrowZzz の現行値は `{-30, -25, ..., +30}`(13 種)× 3 枚 = 39 要素、ADR-0009 §「DDP プールの構造」(起票時「36 枚」表記は計算誤記、M2-PR4 PR で 39 に訂正)|
| `MaxRoundNumber` | M3 着手 PR(予定) | CFG-102(予定) | ゲーム終了判定、DrowZzz の現行値は `21`(ADR-0009 §「Clock 仕様の境界訂正」) |

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Configuration/IGameConfig.cs`(Phase 0 で空ひな形を導入、Phase 1 完結時点でも具体プロパティ未追加)
- 設計: `docs/architecture/constants-management.md`(L1〜L5 階層、運用)
- 実装(将来): `Assets/_Project/Scripts/Infrastructure/Configuration/GameConfigSO.cs`(Phase 2)
- DI 登録(将来): `Assets/_Project/Scripts/Bootstrap/AppLifetimeScope.cs`(Phase 2)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| CFG-001 | (テスト免除: Ubiquitous) | namespace + interface 宣言で構造的に保証 |
| CFG-002 | (テスト免除: Ubiquitous) | asmdef `noEngineReferences: true` で物理保証 |
| CFG-003 | (テスト免除: Ubiquitous) | interface に setter を定義しない構造で保証 |
| CFG-101 | (テスト免除: Ubiquitous) | `IGameConfig.FdpPool` の signature で構造的に保証(値の妥当性は `StartGameUseCase` の利用テストで間接的に検証) |
| CFG-103 | (テスト免除: Ubiquitous) | `IGameConfig.DdpPool` の signature で構造的に保証(値の妥当性は `StubGameConfigTests` の DZ-154 と `StartGameUseCase` 利用テストの DZ-140 で間接的に検証) |

Phase 2 以降で具体プロパティを追加した際は、対応するテストケースを追加し本表を更新する。
