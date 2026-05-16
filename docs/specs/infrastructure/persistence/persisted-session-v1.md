# PersistedSessionV1

## 概要

`Drowsy.Infrastructure.Persistence.Models.PersistedSessionV1` は schemaVersion 1 の `DrowZzzGameSession` 永続化用 DTO。`internal sealed record` で `DrowZzzGameSession.FromDomain / ToDomain` 変換を担う(ADR-0012 §7 由来、M4-PR5 で導入)。

DTO 介在の理由:`DrowZzzGameSession` は `IReadOnlyDictionary<PlayerId, int>` を 5 dictionary 持つが、Newtonsoft.Json のデフォルトでは `PlayerId` を Dictionary key にする `TypeConverter` 解決が手間。本 DTO は文字列キー dictionary に変換することで Domain 型に Newtonsoft 依存を持ち込まずに済ませる(`InternalsVisibleTo("Drowsy.Infrastructure.Tests")` でテストから参照可)。

B-5 第 1 弾(Infrastructure カバレッジ補完、2026-05-16)で単体テストを追加。

## 普遍要件 (Ubiquitous)

- [INF-122] [Ubiquitous] `PersistedSessionV1` shall be `internal sealed record` with `SchemaVersion` (`int`, default `1`) and 9 init-only properties corresponding 1:1 to `DrowZzzGameSession` ctor 10 arguments(`GameState` / `FirstDrowsyPoints` / `DrawDrowsyPoints` / `SecondDrowsyPoints` / `DdpPool` / `Influences` / `PhaseState` / `Outcome` / `BedDamages` / `PendingCounteredEffects`)。

## 事象駆動要件 (Event-driven)

- [INF-123] When `PersistedSessionV1.FromDomain(session)` is invoked with a valid `DrowZzzGameSession`, the resulting DTO shall have:
  - `SchemaVersion == 1`
  - `GameState`、`DdpPool`、`PhaseState`、`Outcome`、`PendingCounteredEffects` が session と同じ参照
  - 5 dictionary(FDP / DDP / SDP / Influences / BedDamages)の各 `PlayerId` キーが `PlayerId.Value` 文字列キーに変換
  - 全 dictionary の要素数 / 値が session と一致
- [INF-124] When `PersistedSessionV1.ToDomain()` is invoked on a valid DTO, the resulting `DrowZzzGameSession` shall equal the original session via `DrowZzzGameSession.Equals`(`FromDomain` → `ToDomain` の往復)。

## 異常要件 (Unwanted)

- [INF-125] If `PersistedSessionV1.FromDomain(null)` is called, the method shall throw `ArgumentNullException` with parameter name `"session"`。
- [INF-126] If `ToDomain()` is called on a DTO with `GameState == null`, the method shall throw `InvalidOperationException` referencing the missing property.
- [INF-127] If `ToDomain()` is called on a DTO with `FirstDrowsyPoints / DrawDrowsyPoints / SecondDrowsyPoints / DdpPool / Influences / BedDamages / PendingCounteredEffects` のいずれかが `null`, the method shall throw `InvalidOperationException` referencing the missing property.

## 関連

- 実装: `Assets/_Project/Scripts/Infrastructure/Persistence/Models/PersistedSessionV1.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/PersistedSessionV1Tests.cs`
- 関連 spec: [`drowzzz-game-session-serializer.md`](drowzzz-game-session-serializer.md)(本 DTO を介在させる serialize / deserialize 経路)
- ADR: ADR-0012 §7「`DrowZzzGameSession` JSON 永続化」

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| INF-122 | (テスト免除: Ubiquitous) | `internal sealed record` + `init` only は宣言で構造保証 |
| INF-123 | `Given_有効session_When_FromDomain_Then_DTOがsessionと一致` | 5 dictionary の string key 変換 + 全要素一致 |
| INF-124 | `Given_FromDomainで生成したDTO_When_ToDomain_Then_元sessionと等価` | round-trip 等価性 |
| INF-125 | `Given_sessionがnull_When_FromDomain_Then_ArgumentNullException` | `session` パラメータ防御 |
| INF-126 | `Given_GameStateがnull_When_ToDomain_Then_InvalidOperationException` | EnsureNotNull(GameState) |
| INF-127 | `Given_必須propertyがnull_When_ToDomain_Then_InvalidOperationException`(TestCase 7 件:FDP / DDP / SDP / DdpPool / Influences / BedDamages / PendingCounteredEffects)| EnsureNotNull の各分岐 |
