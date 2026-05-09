# IGameConfig(ゲームバランス設定の抽象)

ゲームバランス調整可能値の抽象 interface。Domain で純粋宣言、Infrastructure で ScriptableObject 実装、Bootstrap で DI 注入。

## 概要

`IGameConfig` は L3(ゲームバランス調整可能値、`docs/architecture/constants-management.md` 参照)を表す抽象 interface。具体的な値(初期手札数、最大ライフ等)はゲームデザイナーが Inspector から調整可能で、Domain 層は interface 経由でのみ値を参照する。これにより:

- ゲームバランス調整がコード変更不要(ScriptableObject Inspector で完結)
- テスト時に Mock / Stub で異なる値を注入可能
- Domain は UnityEngine 依存なしで balance 値を扱える

## 普遍要件 (Ubiquitous)

- [CFG-001] [Ubiquitous] The `IGameConfig` shall be a pure interface in the `Drowsy.Domain.Configuration` namespace.
- [CFG-002] [Ubiquitous] The `IGameConfig` shall not depend on `UnityEngine` (Domain asmdef の `noEngineReferences: true` で物理保証).
- [CFG-003] [Ubiquitous] The `IGameConfig` shall expose only read-only properties (no setters).

## 事象駆動要件 (Event-driven)

Phase 1 以降で具体プロパティを追加する際に、各プロパティに対する Event-driven 要件を別 ID(CFG-101 〜 等)で追記する。

(現状 Phase 0 では具体プロパティなし)

## 関連

- 実装: `Assets/_Project/Scripts/Domain/Configuration/IGameConfig.cs`(Phase 0 では空ひな形)
- 設計: `docs/architecture/constants-management.md`(L1〜L5 階層、運用)
- 実装(将来): `Assets/_Project/Scripts/Infrastructure/Configuration/GameConfigSO.cs`(Phase 1)
- DI 登録(将来): `Assets/_Project/Scripts/Bootstrap/AppLifetimeScope.cs`(Phase 1)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| CFG-001 | (テスト免除: Ubiquitous) | namespace + interface 宣言で構造的に保証 |
| CFG-002 | (テスト免除: Ubiquitous) | asmdef `noEngineReferences: true` で物理保証 |
| CFG-003 | (テスト免除: Ubiquitous) | interface に setter を定義しない構造で保証 |

Phase 1 以降で具体プロパティを追加した際は、対応するテストケースを追加し本表を更新する。
