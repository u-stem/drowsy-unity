# IDrowZzzGameSessionSerializer (Application 層 Persistence interface 抽出) (M5-PR1)

このファイルは `IDrowZzzGameSessionSerializer` interface の契約を EARS で記述する。
ADR-0016 §5.2「`IDrowZzzGameSessionSerializer` interface 抽出」で確定した interface 契約のうち、
Application 層側で観察可能な振る舞い(引数検査・例外契約・Async/Sync 等価性)を要件として整理する。

実装側(`Drowsy.Infrastructure.Persistence.DrowZzzGameSessionSerializer`)の JSON シリアライズ詳細は
別仕様(M4-PR5 完成時点の Infrastructure 仕様)で扱い、本仕様は **interface 契約** のみをスコープとする。

配置先: `docs/specs/application/persistence/session-serializer-interface.md`

---

## 概要

`Drowsy.Application.Persistence.IDrowZzzGameSessionSerializer` は `DrowZzzGameSession` を永続化するための
Application 層の抽象。Ports & Adapters パターンに従い Application 側で interface を定義し、Infrastructure 側で
具象実装(Newtonsoft.Json ベース、M4-PR5 完成)を提供する。同期 API は M4-PR5 既存の 43 テスト互換のため
維持し、非同期 API は M5 で追加する。

## 普遍要件 (Ubiquitous)

- [APP-044] [Ubiquitous] The `IDrowZzzGameSessionSerializer` shall expose both synchronous (`Save` / `Load`) and asynchronous (`SaveAsync` / `LoadAsync`) APIs with consistent exception contracts.

## 異常要件 (Unwanted)

### 同期 API

- [APP-045] If `Save` is called with `session = null`, then the serializer shall throw `ArgumentNullException`.
- [APP-046] If `Save` is called with `path` that is `null`, empty, or whitespace-only, then the serializer shall throw `ArgumentException`.
- [APP-048] If `Load` is called with `path` that is `null`, empty, or whitespace-only, then the serializer shall throw `ArgumentException`.
- [APP-049] If `Load` is called with a `path` that has no stored session, then the serializer shall throw `FileNotFoundException`.

### 非同期 API

- [APP-050] If `SaveAsync` is called with `session = null`, then the serializer shall throw `ArgumentNullException`.
- [APP-051] If `SaveAsync` is called with `path` that is `null`, empty, or whitespace-only, then the serializer shall throw `ArgumentException`.
- [APP-053] If `SaveAsync` is called with a `CancellationToken` that is already cancelled, then the serializer shall throw `OperationCanceledException`.
- [APP-054] If `LoadAsync` is called with `path` that is `null`, empty, or whitespace-only, then the serializer shall throw `ArgumentException`.
- [APP-055] If `LoadAsync` is called with a `path` that has no stored session, then the serializer shall throw `FileNotFoundException`.
- [APP-056] If `LoadAsync` is called with a `CancellationToken` that is already cancelled, then the serializer shall throw `OperationCanceledException`.

## 事象駆動要件 (Event-driven)

- [APP-047] When `Save(session, path)` is followed by `Load(path)`, the serializer shall return a session equivalent to the saved one (verified via in-memory fake serializer for contract-level round-trip).
- [APP-052] When `SaveAsync(session, path)` is followed by `LoadAsync(path)`, the serializer shall return a session equivalent to the saved one (verified via in-memory fake serializer for contract-level round-trip).

## 関連

- 確定 ADR: [ADR-0016 §5.2 `IDrowZzzGameSessionSerializer` interface 抽出](../../../adr/0016-m5-bootstrap-presentation.md)
- 関連 ADR: [ADR-0012 §7](../../../adr/0012-m4-scriptableobject-and-persistence.md)(具象 Serializer の M4-PR5 確立)、[ADR-0015](../../../adr/0015-nullable-reference-types-not-adopting.md)(`LoadAsync` non-null 戻り値の根拠)
- 実装(interface): `Assets/_Project/Scripts/Application/Persistence/IDrowZzzGameSessionSerializer.cs`
- 実装(Infrastructure 具象): `Assets/_Project/Scripts/Infrastructure/Persistence/DrowZzzGameSessionSerializer.cs`
- 実装(Application.Tests fake): `Assets/_Project/Scripts/Tests/Application.Tests/Stubs/InMemoryDrowZzzGameSessionSerializer.cs`
- テスト: `Assets/_Project/Scripts/Tests/Application.Tests/Persistence/IDrowZzzGameSessionSerializerContractTests.cs`
- シナリオ: `session-serializer-interface.feature`(同ディレクトリ)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| APP-044 | (テスト免除: Ubiquitous) | interface の構造的性質(コンパイル時に保証) |
| APP-045 | Save_session_null で ArgumentNullException | Abnormal |
| APP-046 | Save_path_null_または_空白のみ で ArgumentException | Abnormal |
| APP-047 | Save_then_Load で同一 session が返る | Normal |
| APP-048 | Load_path_null_または_空白のみ で ArgumentException | Abnormal |
| APP-049 | Load_未保存path で FileNotFoundException | Abnormal |
| APP-050 | SaveAsync_session_null で ArgumentNullException | Abnormal |
| APP-051 | SaveAsync_path_null_または_空白のみ で ArgumentException | Abnormal |
| APP-052 | SaveAsync_then_LoadAsync で同一 session が返る | Normal |
| APP-053 | SaveAsync_cancelledToken で OperationCanceledException | Abnormal |
| APP-054 | LoadAsync_path_null_または_空白のみ で ArgumentException | Abnormal |
| APP-055 | LoadAsync_未保存path で FileNotFoundException | Abnormal |
| APP-056 | LoadAsync_cancelledToken で OperationCanceledException | Abnormal |
