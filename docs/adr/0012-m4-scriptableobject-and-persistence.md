# ADR-0012: M4 詳細 — ScriptableObject 化 + 永続化 + ユーザー設定

## Status

Accepted

## Decider(s)

drowsy-unity プロジェクトオーナー(個人開発)

## Date

2026-05-14

## Context

ADR-0005「Phase 2 Roadmap」で M4 のスコープを **「Infrastructure 永続化 / GameState のセーブ・ロードが動く(JSON ベース)」** と確定済。ADR-0007 §5 で **「`ICardCatalog` の SO 化は M4 に送る」** が追加で確定済(ADR-0006 §1.3 の「M2 で SO 化」を覆す形)。本 ADR は M4 マイルストーン着手前に **詳細な実装方針** を確定するもので、ADR-0006(M1 詳細)/ ADR-0007(M2 効果)/ ADR-0010(M3 終了判定)/ ADR-0011(M3 ゲームメカニクス拡張)と同じ「マイルストーン詳細 ADR」の系譜に位置付ける。

### M3 完成時点の到達状態(本 ADR 起票前提)

- **NUnit Property unique**:352 件(全 PR 緑、Domain 95%+ / Application 80% 達成見込み)
- **仕様 ID**:DZ-245 / APP-043 / CFG-103 / GS-105 まで採番(連番に欠番あり、ID は再利用しない)
- **確立済の主要機構**:
  - Domain 9 クラス(Pile / Hand / Card / CardId / PlayerId / GameState / TurnState / PlayerState / 等、Phase 1)
  - Application 層 8 マイルストーン分(M1〜M3、ADR-0006 / 0007 / 0008 / 0009 / 0010 / 0011)
  - Infrastructure 層は **空**(asmdef のみ、本 M4 で本格実装に着手)
- **Pure C# 哲学**:M1〜M4 は VContainer 機能を使わず Pure C# コンストラクタ injection(ADR-0006 §4)、M5 で VContainer LifetimeScope 統合
- **`InMemoryCardCatalog` の責務拡大**:M3-PR4(連想機構)以降「効果列の引き元」だけでなく **「カード生成元」**(連想専用カードは初期山札に含まれず catalog からのみ取得)も兼ねている(ADR-0011 §1 / 連想機構)

### M4 着手前に必要な意思決定

M3 範囲で確定した複数の前提が M4 で同時に効いてくるため、ADR-0005 §「M4」の 1 行記述では足りない:

1. **SO 化のメインスコープ**(プロジェクトオーナー優先順位、JIT 確定 2026-05-14):**ScriptableObjectCardCatalog + IGameConfig SO 化** をメイン。永続化(JSON)/ ユーザー設定(PlayerPrefs)はサブスコープで M4 後半 PR に配置
2. **`IEffect` の SO 表現方式**:`IEffect` は ADR-0007 で **マーカー interface**(派生 record 群)として確立。これを Unity SO で表現する場合、record の値同値性 / immutability と SO の参照同一性 / Inspector 編集の整合性が論点(本 ADR で初期推奨案を提示し、各 PR 着手時に JIT 確定)
3. **`ScriptableObjectCardCatalog` と `InMemoryCardCatalog` の併存**:Application.Tests は Pure C#(ADR-0006 §4)で `InMemoryCardCatalog` を使い続ける。本番経路は SO ベースに移行。両者の **同一 `ICardCatalog<IEffect>` interface を介した併存** をどう実現するか
4. **既存カード(No.01「コップ一杯の脅威」/ No.02「緑の侵攻」/ No.00「夢」)の SO 移行**:M2-PR3 / M2-PR5 / M3-PR6 で `InMemoryCardCatalog` ヘルパーに登録済の 3 枚を SO 化する。`KeywordedEffect` / `TimeOfDayBranchEffect` / `ChoiceEffect` / `RequiresMinimumTotalPointsMarkerEffect` / `UsageRestrictionMarkerEffect` 等の wrapper effect の SO 表現が論点
5. **永続化のスキーマ**:`DrowZzzGameSession`(10 引数、Influences / PendingCounteredEffects / BedDamages 含む)を JSON で表現する場合、`IEffect` 派生型の polymorphic serialization が論点(System.Text.Json / Newtonsoft.Json / カスタムの選択)
6. **ユーザー設定のスコープ**:`IUserSettings` で扱う最小項目(BGM 音量 / 言語 / etc.)を確定。Phase 2 範囲では何が最低限必要か

### 関連 ADR / 既存記述の集約

| ADR | 関連記述 |
| ---- | ---- |
| ADR-0005 §マイルストーン分割 | M4 = Infrastructure 永続化、JSON ベース、Infrastructure 層 |
| ADR-0005 §Phase 2 完了の最小定義 | 永続化が動く(M4 完了相当) |
| ADR-0005 §依存性注入 | M1〜M4 は Pure C#、M5 で VContainer |
| ADR-0006 §1.3 | (旧)M2 で SO 化 → ADR-0007 §5 で M4 に変更 |
| ADR-0006 §1.4 | `IGameConfig` の Phase 2 拡張、`FdpPool` / `DdpPool` 確定、`MaxRoundNumber` / `EarlyWinScoreThreshold` は IGameConfig 非対象(ADR-0010 §8 / §9) |
| ADR-0007 §5 | `ICardCatalog` の SO 化を M4 に送る根拠 4 項目(データ規模 / Designer / 永続化同時設計 / テスト容易性) |
| ADR-0011 §「ScriptableObject 化」 | `Keyword` enum / ベッド破損計算式の SO 移行は M4 で別 ADR で再評価 |
| ADR-0011 §「連想機構」末尾 | `ScriptableObjectCardCatalog` で「両者(効果列引き元 + カード生成元)を 1 SO に統合 vs 分離 SO はその時点で判断」と留保 |
| ADR-0011 §「後続」 | ADR-0012 候補(M4 永続化 / SO 化、本 ADR で起票) |
| ADR-0006 §4 | asmdef `Drowsy.Application` は VContainer / R3.Unity / UniTask を references に含むが M5 まで API 不使用 |
| `Drowsy.Infrastructure.asmdef` | 既存 references:`Drowsy.Domain` / `Drowsy.Application` / `UniTask` / `VContainer`、本 M4 で **`Drowsy.Infrastructure.Tests`** の新設要否を別途検討 |

## Decision

### 1. M4 スコープ整理(JIT 確定 2026-05-14)

M4 マイルストーンを以下の **メイン + サブ** 2 階層スコープに分割する:

#### メインスコープ(優先実装、M4-PR1〜PR4)

1. **`ScriptableObjectCardCatalog`** の Infrastructure 実装(`Drowsy.Infrastructure.Games.DrowZzz` namespace)
2. **`IGameConfig` の SO 実装**(`Drowsy.Infrastructure.Configuration` namespace、`DrowZzzGameConfigAsset` 等)
3. **`IEffect` の SO 表現**(`EffectInterpreter` 経由で従来の record と互換動作する変換層)
4. **既存カード No.00 / No.01 / No.02 の SO 移行**(Application.Tests は `InMemoryCardCatalog` 継続)

#### サブスコープ(後続実装、M4-PR5〜PR7)

5. **`DrowZzzGameSession` JSON 永続化**(Save/Load、`Drowsy.Infrastructure.Persistence` namespace)
6. **`IUserSettings` + PlayerPrefs**(`Drowsy.Infrastructure.Settings` namespace、最小項目 1〜3 件)
7. **M4 完成 PR**(統合確認 + Designer ワークフロー実証 + M4 完成記録)

#### スコープ外(Phase 2 完了基準に含まれない)

- Cloud Save(Steam Cloud / Google Play Save / iCloud)→ Phase 3 候補
- Deterministic Replay(`IRandomSource` の seed 永続化)→ サブスコープ §5 で JSON に seed を含めるかは JIT 確定
- マルチプロファイル(複数 save slot)→ Phase 3 候補
- セーブデータ移行 / バージョニング(将来仕様変更時のスキーマ migration)→ Phase 3 候補(本 M4 では `schemaVersion: 1` を JSON に含めるが、migration ロジック自体は実装しない)

#### 採用根拠(JIT 確定 2026-05-14)

| 項目 | 確定 | 理由 |
| ---- | ---- | ---- |
| メインスコープに **SO 化** を優先配置 | ✓ | プロジェクトオーナー JIT 「最も優先度高い領域」回答(2026-05-14)。M5(Bootstrap + Presentation)連携で Designer がカードデータを Unity Editor で編集する基盤になる |
| サブスコープに **永続化** を後置 | ✓ | ADR-0005 §「最小定義」の「永続化が動く(M4 完了相当)」を満たすが、SO 化基盤確定後の方が `IEffect` 派生型の polymorphic serialization 方針を統一できる |
| サブスコープに **`IUserSettings`** を最後配 | ✓ | L4 階層(CLAUDE.md §9)、Phase 2 範囲では BGM 音量等の最小項目で完結。SO 化 / 永続化との結合度は低く独立 PR で完結可能 |

### 2. `ScriptableObjectCardCatalog` の構造

#### 採用方針

`ICardCatalog<IEffect>` interface(M2-PR1 でジェネリック化、ADR-0007 §3)の SO 実装として、`Drowsy.Infrastructure.Games.DrowZzz.ScriptableObjectCardCatalog` を新設する。本 SO は「**カード ID から `CardData` を引く** + **カード ID から `IReadOnlyList<IEffect>` を引く**」の 2 責務を 1 SO に統合する設計を採用する(ADR-0011 §「連想機構」末尾の「両者を 1 SO に統合 vs 分離 SO」候補から **「1 SO 統合」** を選択、JIT 確定 2026-05-14)。

#### 構造(初期推奨、各 PR 着手時に JIT 確定)

```csharp
namespace Drowsy.Infrastructure.Games.DrowZzz
{
    [CreateAssetMenu(menuName = "Drowsy/DrowZzz/Card Catalog", fileName = "DrowZzzCardCatalog")]
    public sealed class ScriptableObjectCardCatalog : ScriptableObject, ICardCatalog<IEffect>
    {
        [SerializeField] private CardEntryAsset[] _entries;

        public CardData Get(CardId id) { /* _entries から引く */ }
        public bool TryGet(CardId id, out CardData data) { /* 同上 */ }
        public IReadOnlyList<IEffect> GetEffects(CardId id) { /* _entries から引く */ }
    }

    [Serializable]
    public sealed class CardEntryAsset
    {
        public string CardIdValue;       // CardId.Value(Inspector 編集可能)
        public string Name;              // CardData.Name
        public AttributeEntry[] Attributes;  // CardData.Attributes(Dictionary 表現)
        public EffectAsset[] Effects;    // 効果列(SO 化、本 ADR §3)
    }
}
```

「1 SO 統合」の利点(JIT 確定 2026-05-14):
- Designer が **1 Asset 上でカードデータ + 効果列を同時編集** できる(M5 Designer ワークフロー要件)
- `_entries` の Inspector 表示で **全カード一覧** が把握できる
- 重複 ID チェックを **OnValidate** で実行可能(編集時即時フィードバック)

「分離 SO」を不採用とした理由:
- カードデータ用 SO と効果列用 SO を別 Asset にすると、Designer が CardId で参照リンクを手動管理する必要があり、編集時のエラー(リンク切れ)が頻発
- ファイル数増加でメンテコスト増(現状 3 カード → 将来 N カードで `N + N` Asset)

#### `OnValidate` での重複 ID 検出(初期推奨)

`_entries` 内の `CardIdValue` 重複を **Unity Editor 上で即座に検出** する `OnValidate` 実装を含める。重複時は `Debug.LogError` で報告(Build は妨げないが Editor 編集中に気付ける)。CI(GameCI / Unity Test Runner)でも catalog 整合性テスト(`ScriptableObjectCardCatalogTests`)で重複検出を担保する。

### 3. `IEffect` の SO 表現方式(JIT 確認待ち項目あり)

#### 課題

`IEffect` は ADR-0007 §1.1 でマーカー interface として定義され、派生 record 群(`AdjustSdpEffect` / `DrawCardEffect` / `TimeOfDayBranchEffect` / `ChoiceEffect` / `ApplyInfluenceEffect` / `RemoveInfluenceEffect` / `EarlyWinTriggerEffect` / `DamageBedEffect` / `AssociatableMarkerEffect` / `KeywordedEffect` / `RequiresMinimumTotalPointsMarkerEffect` / `UsageRestrictionMarkerEffect`)が **immutable な値オブジェクト** として実装されている。`EffectInterpreter.Apply` は `effect switch { … }` で派生型ディスパッチする(M3-PR6 時点で 12 派生型)。

Unity の SO は **参照型 + Inspector 編集可能** という性質を持ち、record の値同値性 / immutability と semantics がずれる:
- record は同値性で比較され、`with` 式で immutable 更新
- SO は参照同一性で比較され、Inspector 編集で mutable

#### 初期推奨案(各 PR 着手時に JIT 確定)

**案 (a): `[Serializable]` POCO + 変換層**(初期推奨)
- 効果列を `[Serializable]` な POCO(`EffectAsset` 基底 + 派生 class)として SO 内に格納
- `ScriptableObjectCardCatalog.GetEffects` 内で `EffectAsset` → `IEffect` に変換して返す
- `EffectInterpreter` 側は変更なし(既存 record 群をそのまま受け取る)
- Unity の `[SerializeReference]` で polymorphic serialization を実現

**案 (b): `IEffect` 自体を SO 化**
- `AdjustSdpEffectAsset : ScriptableObject, IEffect` のように 1 派生型 = 1 SO クラス
- `_entries[i].Effects` を `IEffect[]`(`[SerializeReference]`)で持つ
- 利点:Inspector で個別 SO Asset 化可能
- 欠点:SO Asset 数が爆発(現状 12 派生型 × 利用回数で N 個)、参照管理の複雑化

**案 (c): JSON 文字列で効果列を表現**
- `_entries[i].EffectsJson` を string field として持ち、`GetEffects` で deserialize
- 利点:Inspector で raw 編集可能、永続化(M4 後半サブスコープ)と統一スキーマ
- 欠点:Designer が JSON 直接編集する UX が悪い(typo 検出不可)

JIT 確認待ち項目(各 PR 着手時):
- 採用案(a / b / c のいずれか、初期推奨 (a))
- 採用案が (a) の場合、`EffectAsset` の派生 class 構成(12 派生型分作るか、wrapper(`TimeOfDayBranchEffect` / `KeywordedEffect` / `ChoiceEffect`)の inner も SO 化するか)
- `[SerializeReference]` の `null` 防御(Unity 2020+ の `[SerializeReference]` は missing reference を null として復元するが、`KeywordedEffect.Inner` 等の null は ArgumentNullException を投げる設計)
- `OnValidate` でカード単位の効果列整合性チェック(例:`AssociatableMarkerEffect` 持ちカードは `UsageRestrictionMarkerEffect` を併設するか、等の business rule 検証は Application 層に残すか SO 側で先取りするか)

### 4. `IGameConfig` の SO 実装

#### 採用方針

`IGameConfig` は ADR-0006 §1.4 で **`Drowsy.Domain.Configuration` namespace** に signature 定義(`FdpPool` / `DdpPool` 2 プロパティ、`MaxRoundNumber` / `EarlyWinScoreThreshold` は IGameConfig 非対象)。本 M4 で **Infrastructure 側に `DrowZzzGameConfigAsset` SO** を新設し、本 SO を本番経路で利用する形に置き換える(`StubGameConfig` は Application.Tests 専用として継続利用、後述 §「Stub の継続利用」、M3-PR6 code-reviewer W-3 反映 2026-05-14)。

```csharp
namespace Drowsy.Infrastructure.Configuration
{
    [CreateAssetMenu(menuName = "Drowsy/DrowZzz/Game Config", fileName = "DrowZzzGameConfig")]
    public sealed class DrowZzzGameConfigAsset : ScriptableObject, IGameConfig
    {
        [SerializeField] private int[] _fdpPool;
        [SerializeField] private int[] _ddpPool;

        public IReadOnlyList<int> FdpPool => _fdpPool;
        public IReadOnlyList<int> DdpPool => _ddpPool;
    }
}
```

#### Designer 検証(`OnValidate`、初期推奨)

- `_fdpPool` の長さ ≥ N=2(プレイヤー数想定)
- `_fdpPool` 重複なし(`StartGameUseCase` が重複なし抽選を要求する仕様、ADR-0006 §1.4 / 仕様 ID CFG-101、M3-PR6 起票 PR code-reviewer P-3 反映 2026-05-14)
- `_ddpPool` の合計値が 0(デフォルト値:13 種 × 3 枚 = 39 要素、-30〜+30 の対称構造で合計ちょうど 0、ADR-0009 §「DDP プールの構造」)。Designer が値を変更する場合は意図的な非対称設計として **`Debug.LogWarning` 警告のみ**(Build は妨げず、設計意図次第で 0 ≠ も許容、M3-PR6 起票 PR code-reviewer W-6 反映 2026-05-14)

#### Stub の継続利用

`StubGameConfig`(Application.Tests/Stubs)は Pure C# テストで継続利用。本 M4 で削除しない:
- Application.Tests は SO を import すると Unity Editor 必須化、ADR-0006 §4「Pure C#」哲学を破壊
- `StubGameConfig` と `DrowZzzGameConfigAsset` は **同じ `IGameConfig` interface** を実装し、本番経路は SO・テスト経路は Stub、で振り分ける(Ports & Adapters)

### 5. `InMemoryCardCatalog` ↔ `ScriptableObjectCardCatalog` 併存戦略

#### 採用方針

両者を **同一 `ICardCatalog<IEffect>` interface 経由で併存** させ、注入箇所で振り分ける。

| 経路 | 採用 catalog | 理由 |
| ---- | ---- | ---- |
| Application.Tests(NUnit) | `InMemoryCardCatalog` | Pure C# 維持(ADR-0006 §4)、テスト独立性 |
| Infrastructure.Tests(NUnit、本 M4 で新設) | `ScriptableObjectCardCatalog` + `EditMode` Test | SO Asset の読み込みテスト、catalog 整合性検証 |
| 本番経路(M5 Bootstrap) | `ScriptableObjectCardCatalog` | Designer 編集対象、Asset 読み込み |

#### `InMemoryCardCatalog` の責務分離(本 M4 で確定)

`InMemoryCardCatalog` は **テスト専用** に明文化(現状 xmldoc に「M1〜M2 のテスト・skeleton 用途、永続化は M4 で `ScriptableObjectCardCatalog` 系の追加を検討」と記載)。本 M4 で実際に SO 化が完了するため、`InMemoryCardCatalog` の xmldoc を **「Application.Tests 専用、本番経路は `ScriptableObjectCardCatalog`」** に更新する。

#### `Drowsy.Infrastructure.Tests` asmdef の新設(本 M4 で確定)

現状 Infrastructure 層は実装 0 / テスト asmdef なし。本 M4 で `Drowsy.Infrastructure.Tests` asmdef を新設する:

```json
{
  "name": "Drowsy.Infrastructure.Tests",
  "references": [
    "Drowsy.Domain",
    "Drowsy.Application",
    "Drowsy.Infrastructure",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "autoReferenced": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "optionalUnityReferences": ["TestAssemblies"]
}
```

最小フィールド構成は既存 `Drowsy.Application.Tests.asmdef` と同等にする(`UNITY_INCLUDE_TESTS` constraint が Unity Test Runner にテストアセンブリを認識させるために必須、M3-PR6 起票 PR code-reviewer W-4 反映 2026-05-14)。EditMode テストとして実行(SO Asset 読み込みが Unity Editor 必須のため)。Application.Tests は PlayMode / EditMode 両対応の純粋 NUnit を維持。

### 6. 既存カード(No.00 / No.01 / No.02)の SO 移行

#### 採用方針

`InMemoryCardCatalog` ヘルパー(`CupOfThreatCardTests.NewCatalogWithCardOne()` / `GreenInvasionCardTests.NewCatalogWithCardTwo()` / `DreamCardTests.NewCatalogWithDream()`)で構築されている 3 カードを SO 化する。SO Asset として `Assets/_Project/Data/Cards/` 配下に配置(または Infrastructure 配下、JIT 確定)。

#### 移行優先順位(M4-PR4 で実施、JIT 確定 2026-05-14)

| カード | 効果構造 | SO 移行の難易度 |
| ---- | ---- | ---- |
| No.01「コップ一杯の脅威」 | `TimeOfDayBranchEffect`(`AdjustSdpEffect` / `DrawCardEffect` を nest)| 中(wrapper 1 段) |
| No.02「緑の侵攻」 | `ChoiceEffect`(`AdjustSdpEffect` / `RemoveInfluenceEffect` / `ApplyInfluenceEffect` を nest)+ `PlayerInfluence`(`AdjustSdpEffect` を TickEffect に持つ)| 高(wrapper + Influence + effect 多種) |
| No.00「夢」 | 4 effect 最上位 + 夜効果に `KeywordedEffect` + `EarlyWinTriggerEffect` nest + 朝効果 `AdjustSdpEffect` | **高**(M3 全機構統合、SO 化の集大成テスト) |

3 カードの SO 化完了で「`IEffect` 派生型 12 種すべてが SO 表現可能」が実証される(`UnknownEffect` 等の M2 範囲外型は除く)。

#### Application.Tests の継続

3 カードの `InMemoryCardCatalog` ヘルパーは Application.Tests に **残す**(SO Asset を Application.Tests で読まない、Pure C# 維持)。`Infrastructure.Tests` 側で SO Asset 読み込み + 同等動作の検証を別途実施(同じカード ID で SO ↔ InMemory が同じ効果を返すことを確認)。

### 7. `DrowZzzGameSession` JSON 永続化(サブスコープ、M4-PR5)

#### 採用方針

`DrowZzzGameSession`(10 引数、Influences / PendingCounteredEffects / BedDamages 含む)を JSON で表現する。`Drowsy.Infrastructure.Persistence.DrowZzzGameSessionSerializer` を新設し、`Save(string path, DrowZzzGameSession)` / `Load(string path) → DrowZzzGameSession` の対称な API を提供。

#### Serializer 選択(JIT 確認待ち、**初期推奨は Newtonsoft.Json**)

本プロジェクトは Unity 6 / `apiCompatibilityLevel: 6`(= .NET Standard 2.1)/ **WebGL 主体 / IL2CPP**(CLAUDE.md §4)。この前提下では Serializer 選択が以下のように制約される(M3-PR6 起票 PR の code-reviewer W-2 反映 2026-05-14):

| 選択肢 | 採用評価 |
| ---- | ---- |
| **Newtonsoft.Json**(Unity 公式パッケージ `com.unity.nuget.newtonsoft-json`)| **初期推奨**。AOT 対応は `link.xml` で型保持 + ILPostProcessor で実績多数、WebGL での実例豊富、`JsonConverter` 派生で `IEffect` polymorphic 対応可能 |
| **System.Text.Json**(.NET 標準) | 注意要:`[JsonDerivedType]` / `[JsonPolymorphic]` は .NET 7+ の API で、Unity 6 の `.NET Standard 2.1` ターゲットでは利用不可。さらに reflection ベース API は IL2CPP (WebGL) AOT で動作しない(MessagePipe を Open Generics 制約で不採用にした文脈と同じ、CLAUDE.md §4 / ADR-0006)。Source Generator 版は .NET 7+ 必須で本プロジェクト非対応 |
| **Unity JsonUtility** | Unity 純正だが polymorphic / `Dictionary` / `IReadOnlyList` 対応が貧弱、本ユースケースには不向き |
| **手書き serializer** | 完全制御だがメンテコスト高、`IEffect` 派生型 12 種の handling を自作する負担大 |

JIT 確認待ち項目(M4-PR5 着手時):
- Serializer 採用の最終確定(Newtonsoft.Json 初期推奨が妥当か、Phase 3 で .NET 9+ に上げる選択肢を取るか)
- `IEffect` 派生型の polymorphic discriminator(Newtonsoft.Json なら `$type` / カスタム `TypeNameHandling` / カスタム `JsonConverter`)
- **WebGL/IL2CPP (AOT) での動作確認手順**(`link.xml` 設定 + WebGL build での round-trip 検証)
- `IRandomSource` の seed 永続化(Deterministic Replay 対応、本 M4 スコープ内か Phase 3 か)
- セーブ先 path(`Application.persistentDataPath` 直下 / サブディレクトリ)
- 暗号化の要否(現状なし、Phase 3 で検討)

#### スキーマバージョニング(初期推奨)

JSON ルートに `"schemaVersion": 1` を含める。本 M4 では migration ロジックは実装せず、将来仕様変更(例:Session ctor 11 引数化)時に Phase 3 で migration 実装。

### 8. `IUserSettings` + PlayerPrefs(サブスコープ、M4-PR6)

#### 採用方針

L4 階層(CLAUDE.md §9)のユーザー設定を `IUserSettings` interface(`Drowsy.Domain.Configuration` namespace、`IGameConfig` と同居)+ PlayerPrefs 実装(`Drowsy.Infrastructure.Settings.PlayerPrefsUserSettings`)で表現する。

#### 最小項目(JIT 確認待ち、初期推奨)

Phase 2 範囲で必要な最小項目を JIT 確定:
- **BGM 音量**(0.0〜1.0、default 0.5)
- **SE 音量**(0.0〜1.0、default 0.5)
- **言語**(`ja` / `en`、default `ja`、Phase 3 で本格的 i18n を別途検討)

JIT 確認待ち項目(M4-PR6 着手時):
- 最小項目セット(上記 3 項目で十分か、追加項目あるか)
- 各項目の default 値
- PlayerPrefs キー命名(`drowsy.bgm` 等の prefix 戦略)
- 設定変更時の `R3 Observable<TValue>` 公開(M5 Presentation での購読を見越して、本 M4 で API として公開するか)

### 9. PR 分割計画

| PR | 内容 | 主な追加・変更 | 依存 |
| ---- | ---- | ---- | ---- |
| **M4-PR1** | `ScriptableObjectCardCatalog` 骨格(効果列なしカードのみ)+ `Drowsy.Infrastructure.Tests` asmdef 新設 | `ScriptableObjectCardCatalog.cs` / `CardEntryAsset.cs`(効果列フィールドはひな形のみ)/ `Infrastructure.Tests/Drowsy.Infrastructure.Tests.asmdef` / Editor テスト(空 catalog 生成 / 単一カード登録 / TryGet)| (M3 完成のみ) |
| **M4-PR2** | `IEffect` の SO 表現方式 確定 + 1 派生型(`AdjustSdpEffect`)の SO 対応 | `EffectAsset.cs` 基底 + `AdjustSdpEffectAsset.cs` / 変換層(`EffectAsset.ToDomain() → IEffect`)/ Editor テスト | M4-PR1 |
| **M4-PR3** | 全 11 派生型(`AdjustSdp` 除く)の SO 対応 + wrapper effect の Inner 表現 | `DrawCardEffectAsset` / `TimeOfDayBranchEffectAsset`(Inner 再帰)/ `ChoiceEffectAsset` / `KeywordedEffectAsset` / `ApplyInfluenceEffectAsset` / `RemoveInfluenceEffectAsset` / `EarlyWinTriggerEffectAsset` / `DamageBedEffectAsset` / `AssociatableMarkerEffectAsset` / `RequiresMinimumTotalPointsMarkerEffectAsset` / `UsageRestrictionMarkerEffectAsset` + 各 Editor テスト | M4-PR2 |
| **M4-PR4** | 既存カード No.00 / No.01 / No.02 を SO Asset 化 + `Infrastructure.Tests` で SO ↔ InMemory 同値性検証 | `Assets/_Project/Data/Cards/` (or 別配置) に 3 Asset 新規 / 統合テスト(同じ catalog 経由でカードプレイ結果が一致)| M4-PR3 |
| **M4-PR5** | `DrowZzzGameSession` JSON 永続化(Save/Load) | `DrowZzzGameSessionSerializer.cs` / `Drowsy.Infrastructure.Persistence` namespace / Editor テスト(round-trip / schema version)| M4-PR4 |
| **M4-PR6** | `IUserSettings` + PlayerPrefs | `IUserSettings.cs`(Domain.Configuration)/ `PlayerPrefsUserSettings.cs`(Infrastructure.Settings)/ PlayMode or EditMode テスト(PlayerPrefs 読み書き)| **M4-PR1 のみ**(PR1 で新設する `Drowsy.Infrastructure.Tests` asmdef を本 PR のテストで使用するため、SO 化(PR2〜PR4)/ 永続化(PR5)とは独立並行可能、M3-PR6 起票 PR code-reviewer P-4 反映 2026-05-14)|
| **M4-PR7**(M4 完成 PR) | 統合確認 + Designer ワークフロー実証 + M4 完成記録 + CLAUDE.md §11 バナー更新 | (本 ADR §M4 完成記録(全体)追加、PR description で Designer 編集サンプル提示)| 全 M4-PR |

PR 数の目安:**7 PR**(M3 と同規模、M3-PR5 のような分割は本 M4 では想定しないが、`IEffect` の 12 派生型 SO 対応が大きい場合 M4-PR3 を 3a / 3b / 3c に分割する可能性あり、その判断は M4-PR3 着手時)。

### 10. 不採用案

| 案 | 不採用理由 |
| ---- | ---- |
| `InMemoryCardCatalog` を削除し SO のみに統一 | Application.Tests が Pure C# 維持できなくなる(ADR-0006 §4 違反)、テスト独立性低下 |
| `IEffect` 自体を SO 化(案 b) | SO Asset 数爆発(12 派生型 × 利用回数で N 個)、Designer 参照管理の複雑化 |
| 効果列を JSON 文字列で表現(案 c) | Designer 直接編集の UX 悪化、typo 検出不可、`[SerializeReference]` 案より劣る |
| カードデータと効果列を分離 SO 化 | Designer の編集体験悪化(2 Asset 跨ぎ参照管理)、1 SO 統合の方が筋 |
| 永続化を M4-PR1 から先に着手 | SO 化基盤確定後の方が `IEffect` polymorphic serialization 方針を統一できる、JIT 確定で SO 化優先 |
| `IUserSettings` を Phase 3 に送る | M5 Presentation 設計で音量制御 UI / 言語切替が必要になる前に interface と PlayerPrefs 実装を確定しておく方が M5 着手をスムーズにする(前倒し設計の合理性)。M4 Infrastructure スコープで SO 化と並行実装することで PR 単位の独立性も保てる(M4-PR6 は他 PR と並列着手可能、M3-PR6 起票 PR code-reviewer P-2 反映 2026-05-14)|
| Cloud Save 対応を M4 に含める | Phase 2 スコープ外(ADR-0005)、Phase 3 候補 |
| Deterministic Replay の seed 永続化を M4 必須化 | サブスコープ §5 で JIT 確定、本 ADR では「seed 永続化を含めるかは M4-PR5 着手時に JIT 確定」と留保 |
| schemaVersion migration ロジックを本 M4 で実装 | 現状 schema バージョン 1 のみで migration 不要、Phase 3 で仕様変更時に実装 |

## Consequences

### Positive

- M4 範囲の Infrastructure 実装が明確化、M5 Bootstrap + Presentation 着手前に「カードデータが SO で編集可能 / Session が永続化される / ユーザー設定が PlayerPrefs に保存される」基盤が揃う
- `InMemoryCardCatalog` ↔ `ScriptableObjectCardCatalog` の併存戦略により、Application.Tests の Pure C# 哲学(ADR-0006 §4)を破壊せず本番経路だけ SO 化できる
- `IGameConfig` の SO 実装で Designer が `FdpPool` / `DdpPool` を Unity Editor で調整可能化(現状ハードコード値)
- 既存 3 カードの SO 移行で「`IEffect` 派生型 12 種すべてが SO 表現可能」が実証され、将来 M5 / Phase 3 でカード追加時の SO ワークフローが確立
- 永続化により Phase 2 完了基準(ADR-0005)を満たし、Phase 2 → Phase 3(N>2 拡張 / 本格 UI / 世界観統合)に進める
- `Drowsy.Infrastructure.Tests` 新設で Infrastructure 層のテストカバレッジを 60% 目標(CLAUDE.md §6)に向けて計測可能化

### Negative

- M4 範囲の規模が ADR-0005 起票時の想定「JSON 永続化のみ」より大きい(SO 化 + 永続化 + Settings = 約 7 PR / 数十ファイル)
  - **緩和**: PR 分割で進捗を可視化、各 PR 完成時点で動作する形に持っていく(M3 と同じ段階的縦串実装)
- `IEffect` の SO 表現(`EffectAsset` POCO + 変換層)が wrapper effect(`TimeOfDayBranchEffect` / `ChoiceEffect` / `KeywordedEffect`)の再帰的構造を扱うため、初期設計の複雑度が高い
  - **緩和**: M4-PR2 で 1 派生型(`AdjustSdpEffect`)のみ実装 → 設計検証後に M4-PR3 で全派生型展開、Red → Green → Refactor サイクルを保つ
- 「JIT 確認待ち」項目が多く、M4-PR2 / PR3 / PR5 / PR6 で都度プロジェクトオーナーから JIT 確認を求めるため、プロジェクトオーナーの負担が増える
  - **緩和**: 本 ADR で「初期推奨案」を明示し、JIT 確認は「初期推奨で進めて良いか」の確認に近づける(ADR-0011 と同パターン)

### Neutral

- 本 ADR は M3(ADR-0011)を **覆さない**:M3 で確定した全機構(連想 / キーワード / 早期勝利 / 時刻分岐 / Marker 方式)は M4 でそのまま SO 表現に移植される。`InMemoryCardCatalog` ↔ `ScriptableObjectCardCatalog` で同じ catalog 振る舞いが保たれる
- 本 ADR で確定した PR 分割計画は **目安** で、各 PR 着手時の JIT 確定で粒度調整あり(M3-PR5 の 5a / 5b / 5c 分割パターン継承)
- ADR-0006 §1.3「M2 で SO 化」+ ADR-0007 §5「M4 に変更」+ 本 ADR §1「M4 で実装」の 3 段階意思決定履歴を維持(各 ADR は当時のスナップショット、本 ADR が SSOT)

## Alternatives Considered

| 案 | 採用 / 不採用 | 理由 |
| ---- | ---- | ---- |
| ADR-0012 を起票せず ADR-0006 §1.3 / ADR-0007 §5 の記述で着手 | **不採用** | M4 全体の SSOT を分散させる、ADR-0011 で確立した「マイルストーン詳細 ADR」運用と非整合、JIT 確認項目の集約先がない |
| ADR-0012 で M4 全体を 1 ADR に集約 | **採用**(本 ADR) | ADR-0011 と同パターン、M4 全 7 PR の SSOT を 1 ADR に集約、JIT 確認項目を §で明示 |
| ADR-0012(SO 化)と ADR-0013(永続化)に分離 | **不採用** | サブスコープ §5 / §6 が独立 PR(M4-PR5 / PR6)で完結する程度の規模、別 ADR 起票のオーバーヘッドより 1 ADR 集約の SSOT 性を優先 |
| 本 M4 で `MaxRoundNumber` / `EarlyWinScoreThreshold` を IGameConfig に追加 | **不採用** | ADR-0010 §8 / §9 で「これらは L2 数学定数で IGameConfig 非対象」と確定済、本 ADR で覆さない |
| 本 M4 で `Keyword` enum を SO 化 | **不採用** | enum は L2 数学定数相当(`Frenzy` / `Instinct` / `Counter` の 3 値は固定)、SO 化は不要(ADR-0011 §「ScriptableObject 化」の留保を本 ADR で **不採用確定**)|
| 本 M4 で `DrowZzzBedConstants` / `DrowZzzClockConstants` / `DrowZzzAssociationConstants` 等を SO 化 | **不採用** | L2 数学定数(CLAUDE.md §9)、SO 化は L3 ゲームバランス値のみが対象(ADR-0011 §「ScriptableObject 化」の留保を本 ADR で **不採用確定**)|

## Implementation Notes

### 本 ADR 起票 PR 同梱の変更

- 本 ADR ファイル新設
- CLAUDE.md §11「確立済み ADR 一覧」に ADR-0012 行追加
- CLAUDE.md §11「Phase 進捗」M4 行を「未着手(次の対象)」→「進行中 — ADR-0012 起票済、M4-PR1 着手予定」に更新
- (本 ADR 起票 PR では実装変更 / テスト追加なし、純粋な設計確定 PR、ADR-0011 起票 PR と同パターン)

### M4-PR1 着手時の JIT 確認項目(本 ADR §2 関連)

- `ScriptableObjectCardCatalog` の Asset 配置パス(`Assets/_Project/Data/Cards/` / `Assets/_Project/Infrastructure/Resources/` / その他)
- `CardEntryAsset` の Inspector 編集 UX(Reorderable List / 通常配列)
- `OnValidate` での重複 ID 検出のエラー報告方式(`Debug.LogError` / `ValidationFailed` 例外 / 警告のみ)
- 本 PR 範囲では効果列フィールドはひな形(空配列固定)、効果対応は M4-PR2 / PR3

### M4-PR2 着手時の JIT 確認項目(本 ADR §3 関連)

- `IEffect` の SO 表現方式の最終確定(案 a / b / c、初期推奨 a)
- 採用案が (a) の場合、`EffectAsset` 基底 class の API(`abstract IEffect ToDomain()` 等)
- `AdjustSdpEffect` を本 PR で SO 対応する第一号にする理由(最も単純で wrapper なし)、他派生型は M4-PR3 で順次対応
- `[SerializeReference]` の Unity バージョン要件(Unity 6 で OK)

### M4-PR3 着手時の JIT 確認項目(本 ADR §3 関連)

- wrapper effect(`TimeOfDayBranchEffect` / `ChoiceEffect` / `KeywordedEffect`)の Inner 表現方式(`EffectAsset[]` / `[SerializeReference] IEffect[]` / その他)
- 11 派生型の SO 対応を 1 PR に詰めるか、3a / 3b / 3c に分割するか(着手時の規模見積もりで判断)
- `OnValidate` での効果列整合性チェック(business rule の SO 側先取りスコープ)

### M4-PR4 着手時の JIT 確認項目(本 ADR §6 関連)

- 3 カード(No.00 / No.01 / No.02)の SO Asset 配置順序(難易度の低い No.01 → No.02 → No.00 推奨)
- SO ↔ InMemory 同値性検証テストの構造(`InMemoryCardCatalog` 経由のテスト結果と `ScriptableObjectCardCatalog` 経由の結果を比較)
- 既存 `CupOfThreatCardTests` / `GreenInvasionCardTests` / `DreamCardTests` の維持(本 PR で SO 経由テストを Infrastructure.Tests に新設、Application.Tests は変更なし)

### M4-PR5 着手時の JIT 確認項目(本 ADR §7 関連)

- Serializer 採用(System.Text.Json / Newtonsoft / その他)
- `IEffect` 派生型の polymorphic discriminator 方式
- セーブ先 path / `Application.persistentDataPath` 直下 / サブディレクトリ
- `IRandomSource` の seed 永続化(Deterministic Replay)を M4 範囲に含めるか
- schemaVersion 1 の JSON ルート構造

### M4-PR6 着手時の JIT 確認項目(本 ADR §8 関連)

- `IUserSettings` の最小項目セット(BGM / SE / 言語の 3 項目で十分か)
- 各項目の default 値
- PlayerPrefs キー命名(`drowsy.bgm` 等の prefix)
- 設定変更の `R3 Observable<TValue>` 公開を本 M4 で行うか M5 に送るか

### M4-PR7(M4 完成 PR)着手時の項目

- M4 完成記録(全体)を本 ADR §M4 完成記録(全体)として追記(M3-PR6 完成 PR + 完成記録 PR #58 と同パターン)
- CLAUDE.md §11「Phase 進捗」M4 行を「完結 → M5 が次の対象」に更新
- Designer ワークフロー実証(Unity Editor 上でカードデータ編集 → ScriptableObjectCardCatalog 読み込み → Application 層で同一動作)のスクリーンショット / 説明を PR description に追加

### M4-PR1 完成記録(2026-05-13、ScriptableObjectCardCatalog 骨格 + Infrastructure.Tests asmdef 新設)

**完成 PR**: PR #60 `feat(infra): ScriptableObjectCardCatalog 骨格 + Infrastructure.Tests asmdef 新設 (M4-PR1)`(merged `fb6b629`、3 commit 同梱:実装 `ed7a48f` + コンパイル fix + .meta 同梱 `a5faeff` + 意図的発火コメント `afcf021`)。本 ADR §2 / §5 / §「M4-PR1 着手時の JIT 確認項目」で確定した M4 マイルストーン初の実装。

#### Definition of Done 達成項目(本 ADR §2 / §5 で確定した仕様の最初の実装)

| スコープ項目 | 達成状況 | 備考 |
| ---- | ---- | ---- |
| `ScriptableObjectCardCatalog` を `Drowsy.Infrastructure.Games.DrowZzz` namespace に新設 | ✓ | `ScriptableObject` 継承 + `ICardCatalog<IEffect>` 実装、`[CreateAssetMenu]` で Designer メニュー登録、`OnEnable` / `OnValidate` / `EnsureCacheBuilt` で cache 管理 |
| `CardEntryAsset` / `AttributeEntry` POCO 新設 | ✓ | `[Serializable] sealed class`、`[SerializeField] private` + public getter、`internal` ctor(`InternalsVisibleTo("Drowsy.Infrastructure.Tests")`)、INF-002 / INF-003 |
| `ICardCatalog<IEffect>` 3 メソッド実装 | ✓ | `Get`(`KeyNotFoundException`)/ `TryGet`(`bool + out`)/ `GetEffects`(本 PR 範囲は空配列固定、INF-008 Optional)、INF-004 〜 INF-007 |
| `OnValidate` 重複 ID 検出 | ✓ | `Debug.LogError(this)` で Asset リンク付き Console エラー、後勝ち適用、INF-009 |
| `Drowsy.Infrastructure.Tests` asmdef 新設 | ✓ | ADR §5 スニペット通り(`UNITY_INCLUDE_TESTS` constraint、既存 Application.Tests / Domain.Tests と統一フィールド)|
| EditMode テスト 12 件 | ✓ | `LogAssert.Expect` で `Debug.LogError` を assertion、`ScriptableObject.CreateInstance + SetEntriesForTest` パターン、12 件全 PASS(オーナー側 Unity Test Runner 確認 2026-05-13)|
| 仕様 md `docs/specs/infrastructure/card-catalog.md` 新設 | ✓ | INF-001 〜 INF-012 採番、INF prefix 初導入(testing-strategy.md §4.5 で前 PR #59 で予約済を本 PR で物理利用) |
| .meta + slnx 同梱(fix commit) | ✓ | Unity Editor 自動生成 .meta 11 件 + drowsy-unity.slnx の Infrastructure / Infrastructure.Tests csproj 追加を `a5faeff` で同梱、M3-PR5c / M3-PR6 で確立した「実装 PR 内 fix 同梱」運用継承 |

#### 仕様 ID / NUnit 増加

- 仕様 ID 新規採番(INF-001〜INF-012、12 件):
  - **INF-001 / 002 / 003**: `ScriptableObjectCardCatalog` / `CardEntryAsset` / `AttributeEntry` の structural Ubiquitous(テスト免除)
  - **INF-004**: `Get(登録済 id)` → CardData
  - **INF-005**: `Get(未登録 id)` → KeyNotFoundException
  - **INF-006**: `TryGet(登録済)` → true + CardData(2 件分割、1 テスト 1 アサーション)
  - **INF-007**: `TryGet(未登録)` → false + null(2 件分割、同上)
  - **INF-008**: `GetEffects` 空配列固定(Optional、M4-PR2 / PR3 で実装拡張、本 PR トレーサビリティ免除)
  - **INF-009**: `OnValidate` 重複 ID → `Debug.LogError`(`LogAssert.Expect` で assertion)
  - **INF-010**: `_entries == null` の graceful 動作(3 件:Get / TryGet / GetEffects)
  - **INF-011**: 空白 / null `CardIdValue` の skip + 他 entry 継続
  - **INF-012**: 構築失敗 entry の skip + Debug.LogError + 他 entry 継続
- NUnit Property unique: **+8 件 → 累計 360 件**(テスト件数 12 件、`[Ubiquitous]` INF-001 / 002 / 003 と `[Optional]` INF-008 はマーカー免除)

#### 本 PR で確定した ADR-0012 §「M4-PR1 着手時の JIT 確認項目」(2026-05-14 + 2026-05-13)

| 項目 | 確定内容 | 確定日 |
| ---- | ---- | ---- |
| Asset 配置パス | **`Assets/_Project/Data/Cards/`**(Designer 識別容易、Resources 非採用で Build サイズ膨張回避)| 2026-05-14 |
| `CardEntryAsset` の Inspector 編集 UX | **通常配列 `SerializeField CardEntryAsset[]`**(Reorderable List は Card 件数 10+ で再検討)| 2026-05-14 |
| `OnValidate` 重複 ID 報告方式 | **`Debug.LogError(this)`**(Asset リンク付き Console エラー + Build 妨げず + CI 二重化)| 2026-05-14 |
| 本 PR スコープ | **骨格のみ**(`GetEffects` ひな形空配列固定、効果列対応は M4-PR2 / PR3、INF-008 Optional)| 2026-05-14 |
| Console 赤色 LogError 表示の扱い | **`Debug.LogError` 維持 + 「意図的発火」コメント 3 箇所多層化**(`LogAssert.Expect` の Unity Test Framework 仕様、Console 赤化は PASS の証拠)| 2026-05-13 |

不採用案(再確認):
- `Resources/` フォルダ採用:Build サイズ膨張 + Unity 非推奨で不採用
- Reorderable List + CustomEditor:M4-PR1 軽量スコープに対して過剰、Card 件数 10+ で再検討
- OnValidate で Exception throw:Inspector がクラッシュ / 編集不能になるリスク、Unity 非推奨
- `Debug.LogWarning` 降格(本セッション JIT 提示時の代替案):Designer への通知強度を維持するため LogError を選択、Console 赤化は仕様として明示コメント化で対応

#### code-reviewer subagent 反映(警告 4 / 提案 5 → 8 件反映、1 件 Skip)

| ID | 種別 | 内容 | 反映 |
| ---- | ---- | ---- | ---- |
| W-1 | 警告 | `CardEntryAsset.ToCardData` の `default` KVP 経路が意図不明瞭 | ✓ `null` 要素は `InvalidOperationException` で明示防御 |
| W-2 | 警告 | INF-006 / INF-007 で `Assert.That(found && ...)` AND 結合(1 テスト 1 アサーション違反)| ✓ 2 件ずつに分割 |
| W-3 | 警告 | asmdef `optionalUnityReferences: ["TestAssemblies"]` が既存パターンと相違 | ✓ `[]` に統一(Application.Tests / Domain.Tests と一致) |
| W-4 | 警告 | `EnsureCacheBuilt` 防御コメント過剰 | ✓ Unity 6000.x 実情に即して補正 |
| P-1 | 提案 | `ToCardData` で `_attributes` 二重 null ガード | ✓ `Attributes` プロパティ経由に統一 |
| P-2 | 提案 | `catch (Exception)` が広すぎる | ✓ `catch (ArgumentException)` に絞る |
| P-3 | 提案 | 仕様 md INF-004「re-built from」記述が実装と乖離 | ✓ 「at cache build time」と明示 |
| P-4 | 提案 | `<see cref="AssemblyInfo"/>` がリンク先として意味なし | ✓ 文字列言及に変更 |
| P-5 | 提案 | `[Conditional("UNITY_EDITOR")]` で Build 時 IL 削除 | Skip — over-engineering 懸念、現スコープでは未採用、必要になった時点で導入 |

#### M4-PR1 進行中の学び

##### 学び 1: NUnit と UnityEngine の `PropertyAttribute` 衝突(Infrastructure.Tests 固有)

`Drowsy.Infrastructure.Tests` asmdef は `Drowsy.Infrastructure` を references するため `UnityEngine` が transitive に取り込まれる。さらに `ScriptableObject.CreateInstance` / `Debug.LogError` / `LogType` / `LogAssert.Expect` で `UnityEngine` の直接利用が必須。NUnit の `[Property("Requirement", ...)]` 属性も使うため `NUnit.Framework.PropertyAttribute` ⇔ `UnityEngine.PropertyAttribute` の名前衝突(CS0104)が発生。既存 Application.Tests / Domain.Tests は UnityEngine 非利用のため発生しなかった、**M4 Infrastructure.Tests 固有の問題**。

修正:テストファイル先頭に `using Property = NUnit.Framework.PropertyAttribute;` type alias を追加。`[Property("Requirement", "INF-NNN")]` の記述を維持しつつ衝突を解消できる。**M4-PR2 以降の Infrastructure.Tests でも雛形として継承**、user-level memory(`csharp-nunit-unityengine-property-conflict.md`)に永続化済。

##### 学び 2: Console 赤色 LogError の「意図的発火」を多層コメントで明示する設計パターン

Unity Test Framework の `LogAssert.Expect` は test pass/fail には影響しないが、**expect 済の LogError も Console には赤色スタックトレースとして残る**(消せない Unity 仕様)。本 PR の INF-009 / INF-012 テストは本番ロジックの `Debug.LogError` を意図的に発火させ `LogAssert.Expect` で消費する設計。Console を見た開発者(将来の自分・チームメンバー)が「これは失敗?」と誤解するリスクがある。

**3 箇所多層化パターンで解決**(`afcf021` commit):

1. 実装側 `Debug.LogError` 直前コメント(2 箇所):「テスト経路で意図的に発火、Console 赤化は PASS の証拠」+ 対応 INF テスト名明記で逆引き可能
2. テスト class xmldoc `<remarks>`:fixture 全体方針 + `LogAssert.Expect` 仕様の明示
3. 対象テストメソッド直前 inline コメント:Console からテスト名ジャンプ時の即時理解

`Debug.LogError` を `Debug.LogWarning` に降格する選択肢もあったが、Designer への通知強度(赤いエラー)を維持するため **LogError 維持 + コメント多層化** を採用(JIT 確定 2026-05-13)。M4-PR4 / M4-PR6 等で類似の意図的 LogError / LogWarning を発火させるテストが出る場合に同パターンを継承。

##### 学び 3: Unity Editor 起動が必要な「外部生成物」を fix commit で同梱するパターン継続

`.meta` ファイル(.cs / .asmdef / フォルダ生成物)+ `drowsy-unity.slnx`(Infrastructure / Infrastructure.Tests csproj 自動追加)は Unity Editor 起動なしでは生成されない。本 PR では実装 commit `ed7a48f` の後、ユーザー側 Unity Editor 起動 → 自動生成物発生 + コンパイルエラー判明 → fix commit `a5faeff` で:

- `using Property = NUnit.Framework.PropertyAttribute;` 追加(コンパイル fix)
- .meta 11 件 + slnx を同 commit に同梱

M3-PR5c PR #55(`55f67f6`)/ M3-PR6 PR #57(`483d724`)で確立した「実装 PR 内 fix 同梱」運用を本 PR でも継承。完成記録 PR(本 PR)を docs-only 純度に保つ運用が M4 範囲でも有効と実証。

##### 学び 4: Infrastructure 層 EditMode テストの基盤確立

本 PR で初めて `Drowsy.Infrastructure.Tests` asmdef を新設。EditMode 限定(`includePlatforms: ["Editor"]`)+ `UNITY_INCLUDE_TESTS` constraint で本番 Build に混入しない設計。`ScriptableObject.CreateInstance<T>` + `internal SetEntriesForTest` + `InternalsVisibleTo` で SO の Inspector 経路を経由せずテスト構築可能化。これらの確立パターンは M4-PR2(`AdjustSdpEffectAsset`)/ M4-PR3(全 11 派生型 EffectAsset)/ M4-PR4(既存 3 カードの SO 移行)/ M4-PR5(`DrowZzzGameSessionSerializer`)/ M4-PR6(`PlayerPrefsUserSettings`)で再利用される基盤。

### Phase 2 の進捗バナー更新(本完成記録 PR 同梱)

CLAUDE.md §11「Phase 進捗」M4 行を以下に更新:

- 旧: `**M4**(永続化 / SO 化 + ユーザー設定): **進行中** — ADR-0012 起票済、M4-PR1(ScriptableObjectCardCatalog 骨格 + Infrastructure.Tests 新設)着手予定`
- 新: `**M4**(永続化 / SO 化 + ユーザー設定): **進行中** — ADR-0012 起票済、M4-PR1 完成、M4-PR2(IEffect の SO 表現方式確定 + AdjustSdpEffect SO 対応)着手予定`

### M4-PR2 完成記録(2026-05-13、IEffect の SO 表現 + AdjustSdpEffectAsset)

**完成 PR**: PR #62 `feat(infra): IEffect の SO 表現 (案 a) + AdjustSdpEffectAsset を実装 (M4-PR2)`(merged `e3499a4`、2 commit 同梱:実装 + 初回コンパイル fix 込み `2ef7b2d` + using UnityEngine 追加 fix `7c7f963`)。本 ADR §3 / §「M4-PR2 着手時の JIT 確認項目」で確定した `IEffect` の SO 表現方式(案 a)と最初の SO 対応派生型 `AdjustSdpEffectAsset` を導入。

**関連 chore PR**: PR #63 `chore(workflow): dotnet build pre-commit 統合 + Unity CLI script 整備`(merged `619e60f`)。本 PR の Unity Editor 起動依存(CS0246 / CS1614 が Editor 起動まで気付けなかった)を契機に、CLI 機械検知レイヤを整備。詳細は本完成記録 §「進行中の学び 2」。

#### Definition of Done 達成項目(本 ADR §3 で確定した仕様の最初の実装)

| スコープ項目 | 達成状況 | 備考 |
| ---- | ---- | ---- |
| `EffectAsset` 基底 abstract class 新設 | ✓ | `[Serializable]` + `abstract IEffect ToDomain()`、`Drowsy.Infrastructure.Games.DrowZzz.Effects` namespace、INF-013 Ubiquitous |
| `AdjustSdpEffectAsset` sealed class 新設 | ✓ | `EffectAsset` 継承、`SdpTarget _target + int _delta` の 2 フィールド、`internal` ctor + `ToDomain` 実装、INF-014 / INF-016 |
| `CardEntryAsset` 拡張 | ✓ | `[SerializeReference] EffectAsset[] _effects` フィールド追加 + `Effects` プロパティ + `internal` ctor 4 引数化(末尾 default null で後方互換維持)、INF-015 |
| `ScriptableObjectCardCatalog.GetEffects` 本格化 | ✓ | `_effectsCache: Dictionary<CardId, IReadOnlyList<IEffect>>` 追加 + `RebuildCache` 内 `BuildEffectsFromAssets` 集計 + INF-008 Optional を空配列固定から SO ベース変換に拡張、INF-017 |
| `SerializeReference` null 要素 + `ToDomain` 失敗の graceful skip | ✓ | `BuildEffectsFromAssets` で null 要素 + `ArgumentException` を catch → skip + `Debug.LogError`、`InMemoryCardCatalog` パターン継承、INF-018 / INF-019 Optional |
| `AdjustSdpEffectAssetTests` 新設(EditMode) | ✓ | INF-016 値伝達 2 ケース(`SdpTarget.Self` / `Opponent` × `Delta`)、`Property` alias 雛形継承 |
| `ScriptableObjectCardCatalogTests` 拡張 | ✓ | INF-017 順序保持 + INF-018 null skip `LogAssert.Expect`(M4-PR1 学び 2「意図的発火コメント多層化」継承)|
| 仕様 md `effect-assets.md` 新設 | ✓ | INF-013 〜 INF-019(7 件、Ubiquitous 3 + 通常 EARS 3 + Optional 1) |

#### 仕様 ID / NUnit 増加

- 仕様 ID 新規採番(INF-013 〜 INF-019、7 件):
  - **INF-013** `EffectAsset` abstract class 構造(`[Serializable]` + `abstract IEffect ToDomain()`)+ Ubiquitous
  - **INF-014** `AdjustSdpEffectAsset` sealed class 構造(`EffectAsset` 継承 + 2 フィールド)+ Ubiquitous
  - **INF-015** `CardEntryAsset.Effects` プロパティ(null safe)+ Ubiquitous
  - **INF-016** `AdjustSdpEffectAsset.ToDomain` 値伝達(`SdpTarget` / `Delta` を `AdjustSdpEffect` record に渡す)
  - **INF-017** `GetEffects(登録済 id)` が `CardEntryAsset.Effects` 順序で `IEffect[]` を返す
  - **INF-018** `_effects` 内の null 要素は skip + `Debug.LogError`(`SerializeReference` の missing reference 防御)
  - **INF-019** [Optional] `EffectAsset.ToDomain()` の `ArgumentException` を catch → skip + `Debug.LogError`(M4-PR3 で wrapper effect の Inner null 経路追加時に本格テスト追加、`docs/todo.md` で追跡)
- NUnit Property unique: **+3 件 → 累計 363 件**(テスト件数は +4、`[Ubiquitous]` INF-013 / 014 / 015 と `[Optional]` INF-019 はマーカー免除)
  - 新規ファイル `AdjustSdpEffectAssetTests`(2 件:INF-016 × 2 ケース)+ `ScriptableObjectCardCatalogTests` 拡張(2 件:INF-017 / INF-018)

#### 本 PR で確定した ADR-0012 §「M4-PR2 着手時の JIT 確認項目」(2026-05-13)

| 項目 | 確定内容 |
| ---- | ---- |
| `IEffect` の SO 表現方式 | **案 (a) `[Serializable]` POCO + 変換層**(`EffectAsset` 基底 + 派生 sealed class、`ToDomain()` 変換)|
| `EffectAsset` 基底 class の API | **`abstract IEffect ToDomain()`**(`CardEntryAsset.ToCardData()` と同じ「To[Domain object]」命名)|
| 第一号 SO 対応 effect | **`AdjustSdpEffect`** のみ(wrapper なし、Marker でない、最もシンプル)|
| `[SerializeReference]` Unity バージョン | Unity 6 で OK(本 PR で前提化)|

不採用案(再確認):
- 案 (b) `IEffect` 自体を SO 化(SO Asset 数爆発、Designer 参照管理複雑化)
- 案 (c) JSON 文字列で効果列を表現(Inspector UX 悪化、typo 検出不可)
- `EffectAsset.ToDomain()` の `internal` 化(P-3、M4-PR3 着手時に wrapper effect の Inner 表現方針確定後再評価)
- `CardEntryAsset.Effects` の `internal` 化(P-4、同上)

#### code-reviewer subagent 反映(警告 3 / 提案 4 → 5 件反映、2 件 M4-PR3 着手時再評価)

| ID | 種別 | 内容 | 反映 |
| ---- | ---- | ---- | ---- |
| W-1 | 警告 | `CardEntryAsset` クラス xmldoc が M4-PR1 完了後の未来形のまま残存 | ✓ M4-PR2 完了形に書き換え(「効果列フィールドは追加済」)|
| W-2 | 警告 | `EnsureCacheBuilt` のコメントと実装の方向不一致 | ✓ 「両 cache は常に同期、どちらか null で再構築」と整合 |
| W-3 | 警告 | INF-018 テストの `LogAssert.Expect` 発火回数の前提が暗黙 | ✓ `CreateInstance` 時点で `_entries` null のため最初の OnEnable は早期 return、SetEntriesForTest で初発火を明示コメント |
| P-1 | 提案 | INF-018 / INF-019 仕様 md に「全要素 skip 時は空配列」未明記 | ✓ `BuildEffectsFromAssets` の `list.Count == 0` フォールバックを仕様 md に追記 |
| P-2 | 提案 | INF-019 Optional 解除のための M4-PR3 連携が不明確 | ✓ `docs/todo.md` に「INF-019 本格テスト追加(M4-PR3 wrapper effect Inner null 経路で発火)」エントリ追加 |
| P-3 | 提案 | `EffectAsset.ToDomain()` を `internal` 化検討 | Skip — **M4-PR3 着手時に再評価**(wrapper effect の Inner 表現が確定するまで `public` 維持)|
| P-4 | 提案 | `CardEntryAsset.Effects` を `internal` 化検討 | Skip — **M4-PR3 着手時に再評価**(同上)|

#### M4-PR2 進行中の学び

##### 学び 1: NUnit / UnityEngine `PropertyAttribute` 衝突は `using UnityEngine;` も必須(M4-PR1 雛形の追補)

M4-PR1 で確立した `using Property = NUnit.Framework.PropertyAttribute;` type alias は、`using UnityEngine;` を **直接書いた状態でのみ機能** する。本 PR の `AdjustSdpEffectAssetTests.cs` では `Debug` / `ScriptableObject` を直接使わないため `using UnityEngine;` を省略していたが、これだと C# attribute 構文 `[Property(...)]` の `Attribute` suffix 補完で `NUnit.Framework.PropertyAttribute` と `UnityEngine.PropertyAttribute` の 2 候補が分岐し CS1614 が発生(2026-05-13 ユーザー実機検証で判明、`7c7f963` で `using UnityEngine;` 追加して解消)。

user-level memory `csharp-nunit-unityengine-property-conflict.md` を更新し、**「両 using 必須、軽量テストでも UnityEngine 省略不可」** を雛形として永続化。M4-PR3 以降の Infrastructure.Tests 配下の全テストファイルで両 using を継承する。

##### 学び 2: `dotnet build` で Unity Editor 起動なしに CS エラーを CLI 検出できる(関連 PR #63 で機械検知レイヤ整備)

M4-PR2 で発生した 2 件の CS エラー(CS0246 `EffectAsset` 名前解決 / CS1614 `Property` 曖昧)は **どちらも Unity Editor 起動するまで気付けなかった**。実証で `dotnet build drowsy-unity.slnx` が **約 4 秒で全 6 csproj をビルド** し、CS エラー + Roslyn Analyzer 警告を Editor 起動なしに検出できることが判明。

関連 chore PR #63 で:
- `lefthook.yml` pre-commit に `dotnet-build` を追加(`dotnet-format` と並列、毎 commit に約 4 秒コスト追加)
- `scripts/run-unity-tests.sh`(Unity CLI `-batchmode -runTests` wrap、手動 / CI 用)
- `scripts/refresh-unity-assets.sh`(`-batchmode -quit` で `.meta` / `.csproj` 機械再生成)
- CLAUDE.md §7 機械検知方針を拡張(検知レイヤ全体像 + 検知対象 + `.meta` ワークフロー指針 3 セクション追加)

**M4-PR3 以降は本 PR で確立した dotnet build pre-commit が型解決エラーを事前検出する基盤** が整い、Unity Editor 起動を介さないコーディングフィードバックループが短縮された。

##### 学び 3: 「Editor 常駐 vs CLI batchmode」のワークフロー指針確立

ユーザー JIT 質問「`.meta` を Editor 起動なしで機械生成できるか / 毎回 Editor を開くのは健全か」(2026-05-13)への回答として、CLAUDE.md §7 に **`.meta` / `.csproj` ワークフロー指針** を新セクションで明示:

| 状況 | 推奨ワークフロー |
| ---- | ---- |
| Editor 常駐(VS Code / IDE と並行) | **Auto-refresh が標準**(Unity 公式推奨)|
| Editor 閉鎖 / CI 自動化 | `scripts/refresh-unity-assets.sh` で機械再生成 |
| Editor 起動中に CLI script を試す | 不可(Unity 1 プロセス制約)|

実証:Unity Editor 起動中に `-batchmode` を呼ぶと「複数の Unity を起動して同じプロジェクトを開くことはできません」エラーで失敗(M4-PR2 セッションで確認)。

##### 学び 4: 初期推奨案を ADR に明示する効果(案 a / b / c から (a) 採用)

ADR-0012 §3 で `IEffect` の SO 表現方式を 3 案(a / b / c)で比較し「**初期推奨 (a)**」を明示していたため、M4-PR2 着手時の JIT 確認が「初期推奨で進めて良いか」のシンプルな質問に収まった(2026-05-13 ユーザー JIT 確定 1 問で完了)。ADR-0011 起票時に確立した「初期推奨案を ADR に書く運用」(ADR-0007 §1.5 / ADR-0010 §「Implementation Notes」継承)が本 M4-PR2 でも有効と実証。M4-PR3 以降の JIT 確認(wrapper effect の Inner 表現 / 11 派生型展開分割)でも継承する。

### M4-PR3 完成記録(2026-05-13、残り 11 派生型 EffectAsset + 中間型 2 件)

**完成 PR**: PR #65 `feat(infra): 残り 11 派生型の EffectAsset + 中間型 2 件を実装 (M4-PR3)`(merged `37d5f1c`、1 commit に 13 派生型 / 中間型 + 21 テスト + 仕様 md 拡張を集約、約 987 行)。本 ADR §3 で確定した `IEffect` の SO 表現(案 a)を残り 11 派生型 + wrapper 再帰 + 2 中間型に展開し、**M4-PR4 で既存 3 カードの SO Asset 化を可能にする基盤を完成**。

#### Definition of Done 達成項目(本 ADR §3 / §「M4-PR3 着手時の JIT 確認項目」で確定した仕様の実装)

| スコープ項目 | 達成状況 | 備考 |
| ---- | ---- | ---- |
| 中間型 `PlayerInfluenceAsset` 新設 | ✓ | `InfluenceTrigger` + `[SerializeReference] EffectAsset _tickEffect` + `int RemainingCount`、再帰 ToDomain、INF-020 / INF-021 |
| 中間型 `EffectBranchAsset` 新設 | ✓ | `[SerializeReference] EffectAsset[]`、`ChoiceEffect.Branches` の 2 次元配列を表現(Unity が 2D 配列を直接 serialize できない制約への対応)、INF-022 |
| 非 wrapper 8 派生型 | ✓ | `DrawCardEffectAsset` / `ApplyInfluenceEffectAsset` / `RemoveInfluenceEffectAsset` / `EarlyWinTriggerEffectAsset` / `DamageBedEffectAsset` / `AssociatableMarkerEffectAsset` / `RequiresMinimumTotalPointsMarkerEffectAsset` / `UsageRestrictionMarkerEffectAsset`、INF-023 〜 INF-038 |
| wrapper 3 派生型(再帰 ToDomain) | ✓ | `TimeOfDayBranchEffectAsset`(夜・朝 2 配列の再帰 + `ConvertList` DRY) / `ChoiceEffectAsset`(2 次元中間型再帰 + Branches null 防御) / `KeywordedEffectAsset`(`Inner` null 防御で INF-019 本格化を支える)、INF-039 〜 INF-044 |
| INF-019 Optional 解除 | ✓ | `KeywordedEffectAsset.Inner` null 経路で catalog の `BuildEffectsFromAssets` graceful skip + Debug.LogError を本格テスト化、M4-PR2 持ち越し TODO 完了 |
| テスト 4 ファイル(21 テスト) | ✓ | `SimpleEffectAssetsTests`(13) / `PlayerInfluenceAssetTests`(2) / `WrapperEffectAssetsTests`(6) / `ScriptableObjectCardCatalogTests` 拡張(INF-019) |
| 仕様 md 拡張 | ✓ | `effect-assets.md` に INF-020〜044(25 件:Ubiquitous 16 + 通常 EARS 9)+ INF-019 昇格 + トレーサビリティテーブル拡張 |
| .meta 16 件同梱 | ✓ | Unity Editor Focus 時の Auto-refresh で生成、本実装 commit に同梱(M4-PR2 と異なり 1 commit 完結) |

#### 仕様 ID / NUnit 増加

- 仕様 ID 新規採番(INF-020 〜 INF-044、25 件 + INF-019 昇格):
  - **INF-020 / 022**:中間型 `PlayerInfluenceAsset` / `EffectBranchAsset` の Ubiquitous structural
  - **INF-021**:`PlayerInfluenceAsset.ToDomain` 値伝達(再帰 TickEffect 含む)
  - **INF-023 / 025 / 027 / 029 / 031 / 033 / 035 / 037**:非 wrapper 8 派生型の Ubiquitous structural
  - **INF-024 / 026 / 028 / 030 / 032 / 034 / 036 / 038**:非 wrapper 8 派生型の `ToDomain()` 値伝達(`ApplyInfluenceEffectAsset` は `PlayerInfluenceAsset` 経由で再帰)
  - **INF-039 / 041 / 043**:wrapper 3 派生型の Ubiquitous structural
  - **INF-040 / 042 / 044**:wrapper 3 派生型の `ToDomain()` 再帰 + null 異常系(`TimeOfDayBranchEffectAsset` は夜・朝対称テスト、`ChoiceEffectAsset` は Branches null 要素、`KeywordedEffectAsset` は Inner null)
  - **INF-019(昇格)**:Optional マーカー解除、`KeywordedEffectAsset.Inner` null 経路で本格テスト追加
- NUnit Property unique: **+13 件 → 累計 376 件**(テスト件数 +21、Ubiquitous 16 件 + null safe(INF-015 等)はマーカー免除)
  - 新規ファイル `SimpleEffectAssetsTests`(13) / `PlayerInfluenceAssetTests`(2) / `WrapperEffectAssetsTests`(6) + `ScriptableObjectCardCatalogTests` 拡張(1 件 INF-019)

#### 本 PR で確定した ADR-0012 §「M4-PR3 着手時の JIT 確認項目」(2026-05-13)

| 項目 | 確定内容 |
| ---- | ---- |
| wrapper effect の Inner 表現方式 | **`[SerializeReference] EffectAsset[]` で再帰**(`TimeOfDayBranchEffect` / `KeywordedEffect`)、`ChoiceEffect` の 2 次元は中間型 `EffectBranchAsset` 経由 |
| PR 分割 | **1 PR に 11 派生型まとめて**(M3-PR6 規模で code-reviewer カバー可能、JIT 確定の通り 3a / 3b / 3c 分割は採用せず) |
| OnValidate での business rule 検証 | **なし**(Application 層に一任、Ports & Adapters の責務分離維持、SO はデータ保持レイヤに徹する) |
| M4-PR2 持ち越し P-3 / P-4(`EffectAsset.ToDomain` / `CardEntryAsset.Effects` の `internal` 化)| **両方 `public` 維持**(M5 Bootstrap / Presentation 連携での参照可能性、`internal` 化は後退の余地として `docs/todo.md` には残さない) |

不採用案(再確認):
- 案 (b) `IEffect` 自体を SO 化(Asset 数爆発)
- 案 (c) JSON 文字列(Designer UX 悪化)
- `[SerializeReference] IEffect[]` 直接(Application 層への Unity 属性侵入、ADR-0006 §4「Pure C# 哲学」に違反)
- PR 3 分割(3a / 3b / 3c)(規模管理目的だが M3-PR6 規模で 1 PR カバー可能)
- OnValidate での business rule 検証(Application 層と二重実装、責務逸脱)

#### code-reviewer subagent 反映(警告 2 / 提案 4 → 6 件すべて反映)

| ID | 種別 | 内容 | 反映 |
| ---- | ---- | ---- | ---- |
| W-1 | 警告 | `ChoiceEffectAsset` Branches 内 null 要素の異常系テスト不在(`TimeOfDayBranchEffectAsset` と非対称)| ✓ `WrapperEffectAssetsTests` に対称テスト追加 |
| W-2 | 警告 | `ChoiceEffectAsset._branches` の `[SerializeField]` vs `[SerializeReference]` 不統一の説明不足 | ✓ docstring に「`EffectBranchAsset` sealed のため `[SerializeField]`、`EffectAsset[]` polymorphic のため `[SerializeReference]`」と根拠コメント追加 |
| P-1 | 提案 | `DrawCardEffectAsset` の Count 無検証ポリシーが不明確(`DamageBedEffectAsset` との非対称)| ✓ docstring に graceful degradation 意図を明示(engine 側で「引ける分だけ」吸収、ADR-0007 §「山札枯渇」) |
| P-2 | 提案 | `TimeOfDayBranchEffectAsset` の Abnormal が夜側のみ、朝側未テスト | ✓ `MorningEffects` 内 null 経路の対称テスト追加 |
| P-3 | 提案 | `SimpleEffectAssetsTests` / `PlayerInfluenceAssetTests` に CS1614 alias コメント不在 | ✓ M4-PR1 慣例の `using Property = ...` + `using UnityEngine;` 根拠コメントを全 3 ファイルで統一 |
| P-4 | 提案 | `DamageBedEffectAsset` 異常系(Percent 0 / 負値)のユニットテストなし | ✓ 設計方針「Abnormal は INF-019 統合経路で代表」をコメント明示(11 派生型 × 異常系の爆発を抑制) |

#### M4-PR3 進行中の学び

##### 学び 1: Unity Auto-refresh が Editor Focus 時のみ走る仕様

新規 13 ファイル追加後に `dotnet build` pre-commit が CS0246 で失敗。Unity Editor が起動中であっても **VS Code / Terminal を foreground にしている間は `.meta` / `.csproj` が更新されない** ことが判明(Unity 6 default `Auto Refresh = Enabled (When App Has Focus)`、performance trade-off)。Unity Editor を一度 Focus する(クリック / Cmd+Tab)と AssetDatabase scan が走り csproj が即時更新された。

**M4-PR3 で確立した運用**:
- 新規 .cs 追加後に **Unity Editor を一度 Focus する習慣**(Editor 常駐ワークフローでも明示的な refresh tap が必要)
- または `bash scripts/refresh-unity-assets.sh`(Editor 閉鎖時、PR #63 で整備済)

user-level memory `unity-auto-refresh-focus-required.md` を新規作成し永続化。CLAUDE.md §7「Editor 常駐 + Auto-refresh が標準」は **「Editor が時々 Focus される前提」** と読むのが正確、と運用ルール化。M4-PR4 以降の Asset 追加でも再利用される雛形。

##### 学び 2: `[SerializeReference] EffectAsset[]` で wrapper 再帰の確立

`TimeOfDayBranchEffect` / `KeywordedEffect` / `ChoiceEffect` の wrapper 効果の Inner 表現を `[SerializeReference] EffectAsset[]`(Single Asset 再帰参照)で実現。Unity 6 の `[SerializeReference]` は polymorphic serialization を提供し、`EffectAsset` abstract base から派生 sealed class へのインスタンス参照を Inspector で polymorphic 編集可能化できる。

`KeywordedEffect.Inner` のような単一参照は `[SerializeReference] EffectAsset _inner`、配列参照は `[SerializeReference] EffectAsset[]` の 2 パターンを使い分け。再帰の深さ制限は Unity SerializeReference 側でなく Application 層の record 構造(`KeywordedEffect(IEffect Inner)` で nested 可能)に従う。

##### 学び 3: 中間型(`EffectBranchAsset`)で 2 次元配列を回避する Unity Serialize 制約への対応

`ChoiceEffect.Branches`(`IReadOnlyList<IReadOnlyList<IEffect>>` 2 次元 list)を Unity Inspector で表現する場合、**Unity は配列の配列(2 次元配列)を直接 serialize できない** 制約に遭遇。`EffectBranchAsset` という中間型(`sealed class` + `[SerializeReference] EffectAsset[]`)を導入することで、外側 `EffectBranchAsset[]`(`[SerializeField]`)+ 内側 `EffectAsset[]`(`[SerializeReference]`)の 2 層構造で 2 次元を実現。

設計上の論点:
- 中間型 `EffectBranchAsset` を **`sealed` に保つ** ことで外側 `[SerializeField]` を維持(polymorphic 不要)
- 内側の `EffectAsset[]` は abstract base 配列のため `[SerializeReference]` 必須
- 属性不統一の見た目を解消するため docstring で「sealed 維持の根拠」を明示(code-reviewer W-2 反映)

##### 学び 4: 1 PR に 11 派生型をまとめる規模管理(M3-PR6 と同等の規模)

本 PR は 34 ファイル / 987 行(.meta 16 件含む)で M3-PR6(1295 行)に次ぐ規模。code-reviewer 指摘件数(警告 2 / 提案 4 = 計 6 件)は M3-PR6(警告 4 / 提案 6 = 計 10 件)を **下回る**:理由は

1. M4-PR2 で `EffectAsset` 基底 + `AdjustSdpEffectAsset` の設計を検証済、本 PR はパターン追従
2. wrapper 3 件・非 wrapper 8 件で同じ「`internal` ctor + `ToDomain()` + xmldoc」パターン
3. PR #63 の `dotnet build` pre-commit で CS エラーを事前検出、レビュー前に解消可能化

「初期推奨案を ADR に書く」+「M4-PR2 で 1 派生型先行検証」+「M4-PR3 で残り展開」の 3 段階構造が規模管理として有効と実証。M4-PR4(既存 3 カードの SO Asset 化)でも同パターン継承予定。

### Phase 2 の進捗バナー更新(本完成記録 PR 同梱)

CLAUDE.md §11「Phase 進捗」M4 行を以下に更新:

- 旧: `**M4**(永続化 / SO 化 + ユーザー設定): **進行中** — ADR-0012 起票済、M4-PR1 / M4-PR2 完成、CLI 機械検知レイヤ整備(PR #63、dotnet build pre-commit + Unity CLI script)、M4-PR3(残り 11 派生型 SO 対応 + wrapper effect の Inner 表現方式確定)着手予定`
- 新: `**M4**(永続化 / SO 化 + ユーザー設定): **進行中** — ADR-0012 起票済、M4-PR1 / M4-PR2 / M4-PR3 完成(SO 表現基盤完成:`EffectAsset` 基底 + 11 派生型 + 中間型 2 件 + wrapper 再帰 + INF-019 graceful skip 経路)、CLI 機械検知レイヤ整備(PR #63)、M4-PR4(既存 3 カード No.00 / No.01 / No.02 の SO Asset 化)着手予定`

### M4-PR4 完成記録(2026-05-13、既存 3 カード SO ↔ InMemory 同値性検証)

**完成 PR**: PR #67 `feat(infra): 既存 3 カード (No.01 / No.02 / No.00) の SO ↔ InMemory 同値性検証 (M4-PR4)`(merged `20bd853`、2 commit 同梱:実装 `d092484` + .meta 4 件 fix `e75daac`)。本 ADR §6 / §「M4-PR4 着手時の JIT 確認項目」で確定した「既存 3 カードの SO 移行」をテスト内動的構築で実装、M4-PR3 で完成した SO 表現基盤を 3 カード(No.01 / No.02 / No.00、M3 全機構統合の最深カード含む)で **回帰防御テスト化**。

#### Definition of Done 達成項目(本 ADR §6 / §「M4-PR4 着手時の JIT 確認項目」で確定した仕様の実装)

| スコープ項目 | 達成状況 | 備考 |
| ---- | ---- | ---- |
| `CupOfThreatCardCatalogTests` 新設 | ✓ | No.01 「コップ一杯の脅威」(M2-PR3):`TimeOfDayBranchEffectAsset` wrapper 1 段 + 内側 `AdjustSdpEffectAsset` / `DrawCardEffectAsset` の再帰 ToDomain 経路を検証、INF-045 |
| `GreenInvasionCardCatalogTests` 新設 | ✓ | No.02「緑の侵攻」(M2-PR5):`ChoiceEffectAsset` 2 次元再帰 + 中間型 `EffectBranchAsset` + `PlayerInfluenceAsset` 中間型経由 + `ApplyInfluenceEffect` / `RemoveInfluenceEffect` 群、INF-046 |
| `DreamCardCatalogTests` 新設 | ✓ | No.00「夢」(M3-PR6):M3 全機構統合(4 最上位 marker / wrapper + nested `KeywordedEffectAsset([Frenzy, Instinct], EarlyWinTriggerEffectAsset)` + 朝効果 `AdjustSdpEffectAsset(Self, -80)`)、INF-047 |
| 仕様 md 拡張 | ✓ | `card-catalog.md` に M4-PR4 セクション + INF-045〜047 採番 + トレーサビリティ拡張、bullet 分離スタイル(P-3 反映で可読性向上) |
| .meta 同梱 | ✓ | Unity Editor Focus 後の Auto-refresh で生成された 4 件(`Cards.meta` フォルダ用 + 3 テスト .cs 用)を fix commit `e75daac` で同梱、GUID 衝突 0 確認済 |
| **Application.Tests への影響なし** | ✓ | 既存 `CupOfThreatCardTests` / `GreenInvasionCardTests` / `DreamCardTests` は変更ゼロ、Pure C# 哲学維持(ADR-0006 §4 / ADR-0012 §5) |

#### 仕様 ID / NUnit 増加

- 仕様 ID 新規採番(INF-045 〜 INF-047、3 件):
  - **INF-045**:No.01「コップ一杯の脅威」の SO ↔ InMemory 同値性
  - **INF-046**:No.02「緑の侵攻」の SO ↔ InMemory 同値性
  - **INF-047**:No.00「夢」の SO ↔ InMemory 同値性(M3 全機構統合)
- NUnit Property unique: **+3 件 → 累計 379 件**(テスト件数 +6、各カード × 2 ケース:`Get(Name)` 同値 + `GetEffects(IEffect[])` record 値同値)
- M4 累計テスト件数:**43 件**(M4-PR1 12 + M4-PR2 4 + M4-PR3 21 + M4-PR4 6)

#### 本 PR で確定した ADR-0012 §「M4-PR4 着手時の JIT 確認項目」(2026-05-13)

| 項目 | 確定内容 |
| ---- | ---- |
| SO Asset 作成方法 | **テスト内動的構築のみ**(`ScriptableObject.CreateInstance + SetEntriesForTest`)、実 `.asset` 配置は M4-PR7 / M5 で Designer ワークフロー実証時に追加 |
| 3 カード実装順序 | **No.01 → No.02 → No.00** の難易度低 → 高、各カード 1 fixture(`*CardCatalogTests` の 3 ファイル分割) |
| 同値性評価軸 | **ToDomain 経由の `IEffect[]` が record 値同値**(`Is.EqualTo`、wrapper override Equals + record auto-equals)、`DrowZzzRule` 経由 end-to-end は Application.Tests がカバー済 |
| Application.Tests 維持 | **変更ゼロ**(Pure C# 哲学維持、Infrastructure.Tests に新規 SO 経由テストを並行追加) |

不採用案(再確認):
- 実 `.asset` ファイル配置 + `AssetDatabase.LoadAssetAtPath` 経由(Unity Editor 起動必須、YAML 形式 .asset の GUID 管理コスト、M4-PR7 で実証時に切り替え)
- 1 fixture 統合(3 カードを `[TestCase]` で parameterize、可読性低下)
- No.00 から逆順実装(JIT 推奨の難易度低 → 高に反する)
- `DrowZzzRule` 経由 end-to-end 同値性(Application.Tests と二重実装、責務逸脱)

#### code-reviewer subagent 反映(警告 2 / 提案 3 → 4 件反映、1 件 M4-PR7 確認ポイント Skip)

| ID | 種別 | 内容 | 反映 |
| ---- | ---- | ---- | ---- |
| W-1 | 警告 | GetName テスト 3 fixture の AAA 分離スタイル不統一(CupOfThreat は分離、他 2 は統合)| ✓ CupOfThreat 基準に統一、GreenInvasion / Dream を `// When` / `// Then` 分離 |
| W-2 | 警告 | GreenInvasion ヘルパー名にカード名が含まれない(`DomainInfluence` / `SoInfluence`)| ✓ `GreenInvasionDomainInfluence` / `GreenInvasionSoInfluence` に rename(将来別カードのヘルパー追加時の衝突予防)|
| P-1 | 提案 | 「Application.Tests を移植」コメント表記が不正確 | ✓ 3 fixture すべてで「同仕様で再実装」「Application.Tests と仕様共有」に書き換え、同期責任を呼び出し元に明示 |
| P-2 | 提案 | `DreamCardCatalogTests` の using 数が多い件 | Skip — M4-PR7 で実 `.asset` 配置時に未使用 using 確認(本 PR ではアクション不要)|
| P-3 | 提案 | 仕様 md INF-045〜047 の要件文密度が高い | ✓ bullet 分離スタイル(M4-PR7 の Designer 参照雛形として可読性向上)|

#### M4-PR4 進行中の学び

##### 学び 1: SO ↔ InMemory 同値性検証パターンの確立(M4-PR7 雛形)

3 fixture 共通の構造:
1. `NewSoCatalogWith*`:`ScriptableObject.CreateInstance + SetEntriesForTest + CardEntryAsset(internal ctor) + EffectAsset[]` で SO catalog を動的構築
2. `NewInMemoryCatalogWith*`:Application.Tests と同仕様で `InMemoryCardCatalog` を構築(Pure C# 維持)
3. `Get(Name)` 同値テスト 1 件 + `GetEffects(IEffect[])` 同値テスト 1 件

この構造は **M4-PR7 で実 `.asset` 配置時に「SO 経路を `AssetDatabase.LoadAssetAtPath` 経由に切り替え」しても InMemory 側 + 同値性検証は不変** で機能する回帰防御として設計。M4-PR7 の Designer ワークフロー実証で同パターンを継承予定。

##### 学び 2: ヘルパー命名に文脈情報を含める重要性(code-reviewer W-2 反映)

`GreenInvasionDomainInfluence()` / `GreenInvasionSoInfluence()` のようにカード名を含めるヘルパー命名は、複数カードの fixture を持つ将来の Asset テストで衝突予防 / 誤読予防として有効。

簡潔さを優先した `DomainInfluence()` は本 fixture では問題ないが、M4-PR5 以降で複数 fixture が同 namespace に存在する状況下で、ファイル間 grep / IDE jump-to-definition での読み手の負荷を増やすリスク。**カード固有のヘルパーは Card 名を含める** を本 PR で確立、M4-PR7 / M5 に継承。

##### 学び 3: Application.Tests 不変 + Infrastructure.Tests 新設の Ports & Adapters 整合

ADR-0006 §4「Pure C# 哲学」+ ADR-0012 §5「`InMemoryCardCatalog` ↔ `ScriptableObjectCardCatalog` 併存戦略」を本 PR で実証:

- Application.Tests(`CupOfThreatCardTests` / `GreenInvasionCardTests` / `DreamCardTests` 3 ファイル、合計 25+ テスト)は **本 PR で一切変更しない**
- Infrastructure.Tests(本 PR で 3 新規 fixture)は **SO 経路の構造同値検証** に専念
- 同じカード仕様を 2 レイヤーで保持する「同期コスト」は本 PR 時点で 3 カード × 2 レイヤー = 6 ファイルだが、Pure C# 哲学(テスト独立性 + Unity Editor 非依存)を保つ Ports & Adapters の代償

将来カード追加(Phase 3+)では、両レイヤー同期を `code-reviewer` が PR 単位で確認する運用で担保。

##### 学び 4: `dotnet build` pre-commit + Unity Auto-refresh の連携が成熟

PR #63 で整備した `dotnet build` pre-commit と M4-PR3 で確認した「Unity Editor Focus 時の Auto-refresh」が本 PR で **連携の成熟** を実証:

1. Claude が新規 .cs を追加 → `dotnet build` pre-commit は **既存 csproj に新規 .cs が未追加でも 0 エラー**(M4-PR4 では既存 csproj に新規 fixture 用 .cs を含めずビルド成功)
2. オーナーが Unity Editor を Focus → AssetDatabase Refresh → `.meta` + csproj 更新
3. fix commit で `.meta` 同梱(GUID 衝突確認後 push)

M4-PR3 / PR4 で同パターンが安定運用化、M4-PR5 以降の追加 PR でも継続予定。

### Phase 2 の進捗バナー更新(本完成記録 PR 同梱)

CLAUDE.md §11「Phase 進捗」M4 行を以下に更新:

- 旧: `**M4**(永続化 / SO 化 + ユーザー設定): **進行中** — ADR-0012 起票済、M4-PR1 / M4-PR2 / M4-PR3 完成(SO 表現基盤完成…)、M4-PR4(既存 3 カード No.00 / No.01 / No.02 の SO Asset 化)着手予定`
- 新: `**M4**(永続化 / SO 化 + ユーザー設定): **進行中** — ADR-0012 起票済、M4-PR1 / M4-PR2 / M4-PR3 / M4-PR4 完成(SO 表現基盤完成 + 既存 3 カード SO ↔ InMemory 同値性検証完了、43 テスト)、CLI 機械検知レイヤ整備(PR #63)、M4-PR5(`DrowZzzGameSession` JSON 永続化、初期推奨 Newtonsoft.Json)着手予定`

### M4 完成記録の追記タイミング

本 ADR の M4-PR1〜PR7 完成記録は各 PR 単位で本 ADR §M4-PR-N 完成記録(2026-MM-DD)として追記する(ADR-0007 / ADR-0010 / ADR-0011 §「完成記録の追記タイミング」と同パターン)。M4 全体の完成時に §M4 完成記録(全体)を別途追加、Definition of Done 達成方法を集約する。

### 要件 ID prefix(M4 範囲)

| Prefix | 範囲 | 配置 |
| ---- | ---- | ---- |
| `APP-` | 本 ADR では追加なし(M4 は Infrastructure 中心で Application interface 変更なし)| 該当なし |
| `DZ-` | DrowZzz 固有(SO 移行で必要なら追加、現状想定なし)| 該当なし |
| `INF-`(新規) | Infrastructure 層全般の仕様(`ScriptableObjectCardCatalog` / `DrowZzzGameConfigAsset` / `DrowZzzGameSessionSerializer`)| `docs/specs/infrastructure/{card-catalog,game-config,persistence}.md` 等(本 M4 で新ディレクトリ作成) |
| `USR-` | `IUserSettings` / `PlayerPrefsUserSettings` 仕様(`testing-strategy.md §4.5` の `(将来) UserSettings` 予約を本 M4 で実利用)| `docs/specs/infrastructure/user-settings.md`(M4-PR6 で新設、M3-PR6 起票 PR code-reviewer W-5 反映 2026-05-14) |
| `CFG-` | `IGameConfig` 関連の追加(SO 実装の structural 仕様)| 既存 `docs/specs/...` または新規 |

`testing-strategy.md §4.5` の ID 体系図の **`INF-` 行追加 + `USR-` 行の「(将来)」マーク除去** は本 ADR 起票 PR で同時に実施する(prefix 競合を本 PR で物理解決、M3-PR6 起票 PR code-reviewer W-5 反映 2026-05-14)。

M4-PR1〜PR7 着手時に最新採番状況を `grep -hroE "\b(APP|DZ|CFG|INF|USR|GS)-[0-9]+\b" docs/specs/ | sort -u | tail` で再確認、連番継続。

### Phase 2 の進捗バナー更新(本 ADR 起票 PR 同梱)

CLAUDE.md §11「Phase 進捗」M4 行を以下に更新:

- 旧: `**M4**(永続化 / SO 化): **未着手(次の対象)**`
- 新: `**M4**(永続化 / SO 化 + ユーザー設定): **進行中** — ADR-0012 起票済、M4-PR1(ScriptableObjectCardCatalog 骨格)着手予定`

## Related

- [ADR-0005](0005-phase2-roadmap-drowzzz.md) §マイルストーン分割 §5(M4 = Infrastructure 永続化、Phase 2 完了の最小定義)
- [ADR-0006](0006-m1-detail-application-interfaces.md) §1.3(ICardCatalog M2 SO 化記述 → 本 ADR で M4 に正式変更を引き継ぎ)/ §1.4(IGameConfig Phase 2 拡張)/ §4(DI 方針 M1〜M4 Pure C#)
- [ADR-0007](0007-m2-detail-card-effects.md) §1.1(IEffect マーカー interface)/ §5(ICardCatalog SO 化を M4 に変更)
- [ADR-0009](0009-m2-m3-dp-and-victory-conditions.md) §「DDP プールの構造」(DdpPool の 39 要素)
- [ADR-0010](0010-m3-game-termination-and-victory-determination.md) §8 / §9(MaxRoundNumber / EarlyWinScoreThreshold は IGameConfig 非対象)
- [ADR-0011](0011-m3-dream-card-and-game-mechanics-expansion.md) §「ScriptableObject 化」(Keyword / ベッド破損計算式の SO 移行は本 ADR で再評価 → 不採用確定)/ §「連想機構」末尾(1 SO 統合 vs 分離 SO の留保 → 本 ADR で 1 SO 統合確定)/ §「後続」(ADR-0012 候補 → 本 ADR で起票)
- `Assets/_Project/Scripts/Domain/Configuration/IGameConfig.cs`(本 M4 で SO 実装を追加)
- `Assets/_Project/Scripts/Application/Catalog/InMemoryCardCatalog.cs`(本 M4 で「Application.Tests 専用」と xmldoc 更新)
- `Assets/_Project/Scripts/Infrastructure/Drowsy.Infrastructure.asmdef`(本 M4 で実装ファイルを順次追加)
