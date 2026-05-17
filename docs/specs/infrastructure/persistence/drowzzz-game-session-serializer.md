# DrowZzzGameSessionSerializer

## 概要

`Drowsy.Infrastructure.Persistence.DrowZzzGameSessionSerializer` は `DrowZzzGameSession`(10 引数 record class)を JSON ファイルに保存・読み込みする責務を持つ。Newtonsoft.Json(`com.unity.nuget.newtonsoft-json`)+ カスタム `JsonConverter` 群(IEffect / GameOutcome polymorphic + 単純 value object)+ `PersistedSessionV1` DTO を介在させ、schemaVersion 1 の flat JSON 構造で round-trip する。

ADR-0012 §7「`DrowZzzGameSession` JSON 永続化(サブスコープ、M4-PR5)」+ M4-PR5 着手時の JIT 確定(2026-05-13)に基づく実装。

## 普遍要件 (Ubiquitous)

- [INF-048] [Ubiquitous] The `DrowZzzGameSessionSerializer` shall save and load only schemaVersion 1 JSON.(異常系の検証は INF-065 で代行)
- [INF-049] The serializer shall use UTF-8 encoding without BOM for both Save and Load.
- [INF-050] The `EffectJsonConverter` shall round-trip all 12 `IEffect` derived types (`AdjustSdpEffect` / `ApplyInfluenceEffect` / `RemoveInfluenceEffect` / `DrawCardEffect` / `DamageBedEffect` / `EarlyWinTriggerEffect` / `ChoiceEffect` / `TimeOfDayBranchEffect` / `KeywordedEffect` / `RequiresMinimumTotalPointsMarkerEffect` / `UsageRestrictionMarkerEffect` / `AssociatableMarkerEffect`) via the `"type"` discriminator.
- [INF-139] The `EffectJsonConverter` shall round-trip the 2 `IEffect` derived types introduced in ADR-0019 PR ②(`RestrictSpecificCardInfluenceEffect` / `ApplyTargetedRestrictionEffect`)via the `"type"` discriminator(`"RestrictSpecificCardInfluence"` / `"ApplyTargetedRestriction"`)。前者は `CardTypeId` の string 値を JSON 上で保持し、後者は `Target`(SdpTarget enum)+ `RemainingCount`(int)を保持する。code-reviewer P-4 反映 2026-05-17。
- [INF-141] The `EffectJsonConverter` shall round-trip `StackHandCardOnDeckTopEffect`(No.05「喧騒を纏う」、2026-05-17 で導入)via the `"type"` discriminator(`"StackHandCardOnDeckTop"`)。`Source`(SdpTarget enum)のみを JSON 上で保持する。
- [INF-143] The `EffectJsonConverter` shall round-trip `DoubleBedDamageSdpInfluenceMarkerEffect`(No.06「牙の届かぬ領域」、2026-05-17 で導入)via the `"type"` discriminator(`"DoubleBedDamageSdpInfluenceMarker"`)。フィールドなし marker のため discriminator のみ JSON 上に保持。
- [INF-146] The `EffectJsonConverter` shall round-trip 2 派生型 introduced in No.07 / No.08(`InvertBedDamageSdpInfluenceMarkerEffect` / `RemoveInvertBedDamageInfluenceEffect`、2026-05-17 で導入)via the `"type"` discriminator(`"InvertBedDamageSdpInfluenceMarker"` / `"RemoveInvertBedDamageInfluence"`)。前者はフィールドなし marker、後者は `Target`(SdpTarget enum)のみを JSON 上で保持する。
- [INF-148] The `EffectJsonConverter` shall round-trip `RestrictAllUsageAndAbandonInfluenceMarkerEffect`(No.09「強引過ぎる一手」、2026-05-17 で導入、ADR-0020 と同 PR)via the `"type"` discriminator(`"RestrictAllUsageAndAbandonInfluenceMarker"`)。フィールドなし marker のため discriminator のみ JSON 上に保持。本 marker を TickEffect として持つ `PlayerInfluence(RemainingCount=1)` の永続化サポートは既存 `ApplyInfluenceEffect` + `PlayerInfluence` の経路で担保され、save/load 後も次の自フェーズで本 marker walk が機能する(ADR-0020 後の count=1 Marker セマンティクスを永続化経路でも保つ)。
- [INF-150] The `EffectJsonConverter` shall round-trip `RestrictDrawCardInfluenceMarkerEffect`(No.10「安直過ぎる一手」、2026-05-17 で導入、ADR-0021 と同 PR)via the `"type"` discriminator(`"RestrictDrawCardInfluenceMarker"`)。フィールドなし marker のため discriminator のみ JSON 上に保持。本 marker を TickEffect として持つ `PlayerInfluence(RemainingCount=1)` の永続化サポートは既存 `ApplyInfluenceEffect` + `PlayerInfluence` の経路で担保され、save/load 後も次の自フェーズで本 marker walk が機能する(ADR-0020 後の count=1 Marker セマンティクスを永続化経路でも保つ)。stuck 化脱出弁(ADR-0021)は IsLegalMove ロジック側の責務で、永続化スキーマは変更なし。
- [INF-152] The `EffectJsonConverter` shall round-trip `AdjustSdpByHandCountEffect`(No.11「機械仕掛けの冬将軍」、2026-05-17 で導入)via the `"type"` discriminator(`"AdjustSdpByHandCount"`)。フィールドなし(動的計算は EffectInterpreter 内で session から取得)のため discriminator のみ JSON 上に保持。本 effect を TickEffect として持つ `PlayerInfluence(RemainingCount=Perpetual)` の永続化は既存 `ApplyInfluenceEffect` + `PlayerInfluence` の経路で担保され、save/load 後も次の自フェーズの Tick で動的計算が正しく実行される(保有者の現在の Hand.Count を session 復元後に参照)。
- [INF-154] The `EffectJsonConverter` shall round-trip `AdjustSdpAfterPlayCardEffect(int Delta)`(No.12「偽りの太陽」、2026-05-17 で導入、ADR-0022 と同 PR)via the `"type"` discriminator(`"AdjustSdpAfterPlayCard"`)+ `delta` フィールド(int)。本 effect を TickEffect として持つ Reactive `PlayerInfluence(OnOwnPlayCardAfter, ..., Perpetual)` の永続化サポートは既存 `ApplyInfluenceEffect` + `PlayerInfluence` の経路で担保され、save/load 後も次の保有者の PlayCardAction で snapshot ベース Reactive walk が機能する。
- [INF-155] The `EffectJsonConverter` shall round-trip `AdjustSdpAfterAbandonEffect(int Delta)`(No.12「偽りの太陽」、2026-05-17 で導入、ADR-0022 と同 PR)via the `"type"` discriminator(`"AdjustSdpAfterAbandon"`)+ `delta` フィールド(int)。INF-154 と完全対称、AbandonAction の Reactive 経路に対応。
- [INF-051] The `GameOutcomeJsonConverter` shall round-trip both `WinnerOutcome` and `DrawOutcome` via the `"type"` discriminator.

## 事象駆動要件 (Event-driven)

- [INF-052] When `Save(session, path)` is invoked, the serializer shall create the parent directory if it does not exist.
- [INF-053] When `Save(session, path)` is invoked and the file already exists at `path`, the serializer shall overwrite the existing file.
- [INF-054] When `Save(session, path)` succeeds and `Load(path)` is then invoked on the same `path`, the returned `DrowZzzGameSession` shall equal the original `session` via `DrowZzzGameSession.Equals`.
- [INF-055] When a wrapper effect (`ChoiceEffect` / `TimeOfDayBranchEffect` / `KeywordedEffect`) is serialized, the converter shall recursively resolve nested `IEffect` instances using the registered `EffectJsonConverter`.
- [INF-056] When `GameOutcome` is `WinnerOutcome(PlayerId)`, the JSON shall be `{"type": "Winner", "winner": "<PlayerId.Value>"}`.
- [INF-057] When `GameOutcome` is `DrawOutcome`, the JSON shall be `{"type": "Draw"}`.
- [INF-058] When `GameOutcome` is `null` (game not yet terminated), the JSON `outcome` field shall be `null`.
- [INF-059] When `DefaultSavePath(fileName)` is invoked, the serializer shall return `Path.Combine(Application.persistentDataPath, "drowzzz", fileName)`.
- [INF-083] When `SaveAsync(session, path)` succeeds and `LoadAsync(path)` is then invoked on the same `path`, the returned `DrowZzzGameSession` shall equal the original `session` via `DrowZzzGameSession.Equals`(M5-PR5、同期 `Save` / `Load` の `UniTask.RunOnThreadPool` ラップ版).

## 状態駆動要件 (State-driven)

- [INF-060] While the file at `path` does not exist, `Load(path)` shall throw `FileNotFoundException`.
- [INF-087] While the file at `path` does not exist, `LoadAsync(path)` shall throw `FileNotFoundException`(M5-PR5、同期 `Load` と同じ例外契約。引数 null / 空白の `ArgumentException` は `RunOnThreadPool` 投入前に同期 throw されるのに対し、本例外は `Load` 本体で投げられる).

## 異常要件 (Unwanted)

- [INF-061] If `session` is `null`, `Save(session, path)` shall throw `ArgumentNullException`.
- [INF-062] If `path` is `null`, empty, or whitespace, `Save(session, path)` shall throw `ArgumentException`.
- [INF-063] If `path` is `null`, empty, or whitespace, `Load(path)` shall throw `ArgumentException`.
- [INF-064] If the JSON content is malformed, `Load(path)` shall throw `InvalidDataException` wrapping the underlying `JsonException`.
- [INF-065] If `schemaVersion` is anything other than 1, `Load(path)` shall throw `InvalidDataException` indicating the unsupported version.
- [INF-066] If an effect JSON object lacks the `"type"` discriminator, the converter shall throw `JsonSerializationException`.
- [INF-067] If an effect JSON object has an unknown `"type"` discriminator value, the converter shall throw `JsonSerializationException` naming the unknown value.
- [INF-068] If a `GameOutcome` JSON object lacks the `"type"` discriminator, the converter shall throw `JsonSerializationException`.
- [INF-069] If a `GameOutcome` JSON object has an unknown `"type"` discriminator value, the converter shall throw `JsonSerializationException` naming the unknown value.
- [INF-070] If any required property (`GameState` / `FirstDrowsyPoints` / `DrawDrowsyPoints` / `SecondDrowsyPoints` / `DdpPool` / `Influences` / `BedDamages` / `PendingCounteredEffects`) is missing from the deserialized DTO, `PersistedSessionV1.ToDomain()` shall throw `InvalidOperationException` naming the missing property.
- [INF-071] If `fileName` passed to `DefaultSavePath` is `null`, empty, or whitespace, the method shall throw `ArgumentException`.
- [INF-084] If `session` is `null`, `SaveAsync(session, path)` shall throw `ArgumentNullException`(M5-PR5、同期 `Save` と同じ例外契約).
- [INF-085] If `path` is `null`, empty, or whitespace, `SaveAsync(session, path)` shall throw `ArgumentException`(M5-PR5).
- [INF-086] If `path` is `null`, empty, or whitespace, `LoadAsync(path)` shall throw `ArgumentException`(M5-PR5).
- [INF-134] If an `IEffect` JSON object lacks a required property other than `"type"`(例: `AdjustSdp` の `delta`, `Keyworded` の `inner` 等), the converter shall throw `JsonSerializationException` naming the missing key(Infra W-1 post-Phase2 レビュー反映、`NullReferenceException` 隠蔽を防ぐ).
- [INF-135] If a wrapper effect JSON object lacks a required nested array property(例: `TimeOfDayBranch` の `nightEffects`/`morningEffects`, `Choice` の `branches`), the converter shall throw `JsonSerializationException` naming the missing key(Infra W-2 post-Phase2 レビュー反映、配列型エラーより診断価値の高い欠落キー名を返す).

## 定数依存

該当なし(本機能は schema バージョン定数のみで、`PersistedSessionV1.SchemaVersion = 1` literal を `DrowZzzGameSessionSerializer.Load` の検証ロジックで参照する)。

## 関連

- 実装:
  - `Assets/_Project/Scripts/Infrastructure/Persistence/DrowZzzGameSessionSerializer.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/DrowZzzJsonSettings.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Models/PersistedSessionV1.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/EffectJsonConverter.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/GameOutcomeJsonConverter.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/CardIdJsonConverter.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/PlayerIdJsonConverter.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/PileJsonConverter.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/HandJsonConverter.cs`
  - `Assets/_Project/Scripts/Infrastructure/Persistence/Converters/DdpPoolJsonConverter.cs`
- テスト: `Assets/_Project/Scripts/Tests/Infrastructure.Tests/Persistence/`
- IL2CPP 型保持: `Assets/_Project/Scripts/Infrastructure/link.xml`
- シナリオ: `drowzzz-game-session-serializer.feature`(同ディレクトリ)
- ADR: [ADR-0012 §7](../../../adr/0012-m4-scriptableobject-and-persistence.md)

## トレーサビリティ

| 要件 ID | カバーするテスト(実メソッド名)| 備考 |
| ---- | ---- | ---- |
| INF-048 | (テスト免除: Ubiquitous、異常系は INF-065 で代行) | schemaVersion 1 のみ生成 = 構造的保証 |
| INF-049 | `Given_Save後_When_ファイルバイト先頭を確認_Then_UTF8_BOMが付かない` | UTF-8 BOM なし |
| INF-050 | `EffectJsonConverterTests` 配下の 12 派生型 round-trip テスト + `Given_AdjustSdpEffect_When_Serialize_Then_typeはAdjustSdp` | 12 派生型網羅 |
| INF-139 | `EffectJsonConverterTests.Given_RestrictSpecificCardInfluenceEffect_When_RoundTrip_Then_等価` + `Given_ApplyTargetedRestrictionEffect_When_RoundTrip_Then_等価` 2 件 | ADR-0019 PR ② 追加 2 派生型 round-trip |
| INF-141 | `EffectJsonConverterTests.Given_StackHandCardOnDeckTopEffect_When_RoundTrip_Then_等価` | No.05「喧騒を纏う」追加派生型 round-trip |
| INF-143 | `EffectJsonConverterTests.Given_DoubleBedDamageSdpInfluenceMarkerEffect_When_RoundTrip_Then_等価` | No.06「牙の届かぬ領域」追加派生型 round-trip(フィールドなし marker)|
| INF-146 | `EffectJsonConverterTests.Given_InvertBedDamageSdpInfluenceMarkerEffect_When_RoundTrip_Then_等価` + `Given_RemoveInvertBedDamageInfluenceEffect_When_RoundTrip_Then_等価` 2 件 | No.07 / No.08 追加派生型 round-trip(フィールドなし marker + Target enum)|
| INF-148 | `EffectJsonConverterTests.Given_RestrictAllUsageAndAbandonInfluenceMarkerEffect_When_RoundTrip_Then_等価` | No.09「強引過ぎる一手」追加派生型 round-trip(フィールドなし marker)、ADR-0020 と同 PR |
| INF-150 | `EffectJsonConverterTests.Given_RestrictDrawCardInfluenceMarkerEffect_When_RoundTrip_Then_等価` | No.10「安直過ぎる一手」追加派生型 round-trip(フィールドなし marker)、ADR-0021 と同 PR |
| INF-152 | `EffectJsonConverterTests.Given_AdjustSdpByHandCountEffect_When_RoundTrip_Then_等価` | No.11「機械仕掛けの冬将軍」追加派生型 round-trip(フィールドなし、動的計算 TickEffect) |
| INF-154 | `EffectJsonConverterTests.Given_AdjustSdpAfterPlayCardEffect_When_RoundTrip_Then_等価` | No.12「偽りの太陽」追加派生型 round-trip(Delta フィールドあり、Reactive TickEffect)、ADR-0022 と同 PR |
| INF-155 | `EffectJsonConverterTests.Given_AdjustSdpAfterAbandonEffect_When_RoundTrip_Then_等価` | No.12「偽りの太陽」追加派生型 round-trip(Delta フィールドあり、Reactive TickEffect)、ADR-0022 と同 PR |
| INF-051 | `Given_WinnerOutcome_When_RoundTrip_Then_等価` / `Given_DrawOutcome_When_RoundTrip_Then_等価` | 2 派生型網羅 |
| INF-052 | `Given_親ディレクトリ未作成_When_Save_Then_自動作成される` | |
| INF-053 | `Given_既存ファイル_When_Save_Then_上書きされる` | |
| INF-054 | `Given_全機能入りSession_When_SaveしてLoad_Then_元Sessionと等価` | round-trip 主経路 |
| INF-055 | `Given_3段ネストWrapper_When_RoundTrip_Then_最深まで等価` | wrapper 再帰(Keyworded → Choice → Keyworded → AdjustSdp) |
| INF-056 | `Given_Outcome_WinnerOutcome_When_Save_Then_typeとwinnerが書き出される` | |
| INF-057 | `Given_Outcome_DrawOutcome_When_Save_Then_typeのみ書き出される` | |
| INF-058 | `Given_Outcome_null_When_SaveしてLoad_Then_Outcomeはnull` | |
| INF-059 | `Given_fileName_When_DefaultSavePath_Then_persistentDataPath_drowzzz_fileNameを返す` / `Given_fileName省略_When_DefaultSavePath_Then_session_jsonが既定値` | EditMode で `Application.persistentDataPath` 利用可 |
| INF-060 | `Given_存在しないpath_When_Load_Then_FileNotFoundException` | |
| INF-061 | `Given_sessionがnull_When_Save_Then_ArgumentNullException` | |
| INF-062 | `Given_Save_path_null_When_呼ぶ_Then_ArgumentException` / `Given_Save_path_空白_When_呼ぶ_Then_ArgumentException` | |
| INF-063 | `Given_Load_path_null_When_呼ぶ_Then_ArgumentException` / `Given_Load_path_空白_When_呼ぶ_Then_ArgumentException` | |
| INF-064 | `Given_破損JSON_When_Load_Then_InvalidDataException` | |
| INF-065 | `Given_schemaVersion不一致_When_Load_Then_InvalidDataException` | |
| INF-066 | `Given_typeフィールド欠落_When_Deserialize_Then_JsonSerializationException` (EffectJsonConverterTests) | |
| INF-067 | `Given_未知のtype値_When_Deserialize_Then_JsonSerializationException` (EffectJsonConverterTests) | |
| INF-068 | `Given_typeフィールド欠落_When_Deserialize_Then_JsonSerializationException` (GameOutcomeJsonConverterTests) | |
| INF-069 | `Given_未知のtype値_When_Deserialize_Then_JsonSerializationException` (GameOutcomeJsonConverterTests) | |
| INF-070 | `Given_GameStateプロパティ欠落_When_Load_Then_InvalidOperationException` | |
| INF-071 | `Given_fileName_null_When_DefaultSavePath_Then_ArgumentException` / `Given_fileName_空白_When_DefaultSavePath_Then_ArgumentException` | |
| INF-083 | `Given_MinimalSession_When_SaveAsyncしてLoadAsync_Then_元Sessionと等価` / `Given_全機能入りSession_When_SaveAsyncしてLoadAsync_Then_元Sessionと等価` | 非同期 round-trip、`DrowZzzGameSessionSerializerAsyncTests`(M5-PR5)|
| INF-084 | `Given_sessionがnull_When_SaveAsync_Then_ArgumentNullException` | 同期 throw(RunOnThreadPool 投入前)|
| INF-085 | `Given_SaveAsync_path無効_When_呼ぶ_Then_ArgumentException` | TestCase null / empty / whitespace |
| INF-086 | `Given_LoadAsync_path無効_When_呼ぶ_Then_ArgumentException` | TestCase null / empty / whitespace |
| INF-087 | `Given_存在しないpath_When_LoadAsync_Then_FileNotFoundException` | `Load` 本体(ThreadPool 上)で throw、`Assert.ThrowsAsync` で捕捉 |
| INF-134 | `Given_AdjustSdpでdeltaキー欠落_When_Deserialize_Then_JsonSerializationException` (EffectJsonConverterTests) | Infra W-1 反映 |
| INF-135 | `Given_TimeOfDayBranchでnightEffectsキー欠落_When_Deserialize_Then_JsonSerializationException` (EffectJsonConverterTests) | Infra W-2 反映 |
