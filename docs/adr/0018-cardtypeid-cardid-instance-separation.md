# ADR-0018: CardTypeId と CardId(instance)の分離 — Hand 重複検出問題の根本対処

- Status: Accepted
- Date: 2026-05-16
- Decider: -

---

## Context

Phase 1 の Domain 設計時、`CardId` を「カードの一意識別子(string Value)」として導入した(`docs/adr/0002-phase1-domain-boundaries.md`)。同時に `Hand` を「順序付きユニーク `CardId` 集合」(`docs/specs/domain/cards/hand.md` HAND-003 / HAND-005)として実装し、`ICardCatalog.Get(CardId)`(Phase 2 / ADR-0006)で catalog の lookup key としても利用した。

この設計には**二重の意味**が暗黙に混在していた:

| API | `CardId` の意味 | 重複制約 |
| ---- | ---- | ---- |
| `ICardCatalog.Get(CardId)` | **カードデータの key(種別 ID)** | 種別数だけ存在(catalog 登録カード数) |
| `Hand`(HAND-003 / 005) | **インスタンス ID** | 重複不可(unique) |
| `Pile`(deck / discard) | 種別 ID として複数保持可 | 重複制約なし(`List<CardId>`) |

Phase 1 の `Hand` テストは「異なる種類のカードを並べる」ケースのみ想定しており、「同じ種類のカードを 2 枚以上配布する」シナリオが暗黙に避けられていたため不整合が顕在化していなかった。

M5 UI 実機検証(2026-05-16)で `ProjectLifetimeScope.BuildInitialDeck` が catalog 登録カード(M4 時点 3 種、`CardId="00"` / `"01"` / `"02"`)を `CopiesPerCardForM5Deck = 20` 枚ずつ並べた deck から `StartGameUseCase.Execute` が各プレイヤーに 5 枚配布した際、`Hand.Add` で重複検出エラーが発生した:

```
System.ArgumentException: Hand に既に同じ CardId が含まれているため Add できません: 00
  at Drowsy.Domain.Cards.Hand.Add (Hand.cs:86)
  at Drowsy.Application.Games.DrowZzz.StartGameUseCase.Execute (StartGameUseCase.cs:89)
```

ADR-0017(PlayerRoster wrapper、PR #95)で VContainer collection rule を回避し BootAsync が新規対戦経路まで到達した結果、本問題が次の障害として顕在化した。本 ADR はこの根本対処を確定する。

### 検討した修正案(同じ詳細は M5 UI 検証時の議論記録 / Claude セッション参照)

| 案 | 内容 | 評価 |
| ---- | ---- | ---- |
| **A. CardTypeId 完全分離(本 ADR 採択)** | `CardTypeId` を新設(catalog key)、`CardId` を `record CardId(CardTypeId TypeId, int Instance)` 複合型に refactor(Pile/Hand/Field/Discard 内 unique) | デファクト(MTG / Hearthstone / Yu-Gi-Oh 等の業界標準)準拠、Domain 設計として最も clean、`Hand` の unique 制約が正当化される。影響範囲広大(全体 100+ファイル)だが Phase 3 以降の保守性最大 |
| B. `Hand` を `List<CardId>`(重複可)に変更、CardId は種別 ID として統一 | 影響範囲最小、ただし `Hand` の Phase 1 設計意図(ユニーク集合)を撤回することになり、HAND-003 / HAND-005 EARS の breaking change | 設計の整合性は得られるが、業界デファクトと逆行 |
| C. Phase 2 では `"dream#0"` 文字列規約で最小漸進、Phase 3 で本格化 | 実装最速だが naming convention の脆さ(`#` 含む typeId 禁止 / split の暗黙規約)、設計負債が残る | M5 完結最速だが Domain の根本不整合は持続 |

オーナーの直感「同じ種類でも ID は別であるはず」は業界デファクトと一致しており、Phase 3 以降の長期保守性を優先して **案 A** を採択する。

---

## Decision

### 1. 新規 `CardTypeId` を Domain に導入

配置: `Assets/_Project/Scripts/Domain/Cards/CardTypeId.cs`
namespace: `Drowsy.Domain.Cards`

```csharp
public sealed record CardTypeId
{
    public string Value { get; }

    private CardTypeId(string value) { Value = value; }

    public static CardTypeId Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CardTypeId は null・空・空白のみにできません", nameof(value));
        return new CardTypeId(value);
    }

    public override string ToString() => Value;
}
```

- `sealed record`:値同値性(record 自動生成)、`with` 不要なら `{ get; }` で十分
- ctor は空白文字列を弾く(Parse-don't-validate、CardId の既存ガードと統一)
- 用途:**catalog の lookup key**(`ICardCatalog<TEffect>.Get(CardTypeId)`)/ カードデータの種別識別

### 2. `CardId` を複合型 `record CardId(CardTypeId TypeId, int Instance)` に refactor

```csharp
public sealed record CardId
{
    public CardTypeId TypeId { get; }
    public int Instance { get; }

    /// <summary>後方互換的な文字列表現(永続化 / ログ / 表示用)。"$"{TypeId.Value}#{Instance}"" 形式。</summary>
    public string Value => $"{TypeId.Value}#{Instance}";

    private CardId(CardTypeId typeId, int instance) { TypeId = typeId; Instance = instance; }

    public static CardId Of(CardTypeId typeId, int instance)
    {
        if (typeId is null) throw new ArgumentNullException(nameof(typeId));
        if (instance < 0) throw new ArgumentOutOfRangeException(nameof(instance), "instance は 0 以上である必要があります");
        return new CardId(typeId, instance);
    }

    public override string ToString() => Value;
}
```

- 旧 `CardId.Of(string)` API は**廃止**(breaking change、全呼び出し側を `CardId.Of(CardTypeId, int)` に修正)
- `Value` プロパティは computed string(後方互換 / 永続化 / ログ用)で `"<typeId>#<instance>"` 形式
- 等価性は record 自動生成(`TypeId` と `Instance` の組で等価、unique instance ID として機能)
- 利用箇所:`Pile` / `Hand` / `Field` / `Discard` / `GameState` / 全 Action / 効果評価

### 3. `ICardCatalog<TEffect>` API を `CardTypeId` ベースに変更

```csharp
public interface ICardCatalog<TEffect>
    where TEffect : class
{
    CardData Get(CardTypeId typeId);
    bool TryGet(CardTypeId typeId, out CardData data);
    IReadOnlyList<TEffect> GetEffects(CardTypeId typeId);
}
```

- 引数型を `CardId` → `CardTypeId` に変更(catalog の責務「種別 ID から CardData / Effect 列を引く」を型で明示)
- 呼び出し側(`PlayCardAction.Apply` 等)は `cardId.TypeId` を渡す
- ScriptableObject catalog / InMemory catalog 実装も lookup key を `CardTypeId` に変更

### 4. `Hand` / `Pile` / `Field` / `Discard` の API は変更しない

- `Hand` の `unique CardId 制約`(HAND-003 / 005)は **意味が正当化される**(CardId が instance unique なので、同一インスタンスを 2 枚持つことはあり得ない)
- EARS の文言は「ユニーク `CardId` 集合」のままで実装と整合

### 5. `Bootstrap.ProjectLifetimeScope.BuildInitialDeck` で instance ID 生成

```csharp
foreach (var typeId in catalog.RegisteredCardTypeIds)
{
    for (int i = 0; i < CopiesPerCardForM5Deck; i++)
    {
        cards.Add(CardId.Of(typeId, i));   // unique instance
    }
}
```

- `RegisteredCardIds` → `RegisteredCardTypeIds` に rename(API シグネチャ変更)

### 6. 新規 EARS モジュール `CTYPE`(CardTypeId)

| 要件 ID | 種別 | 内容 |
| ---- | ---- | ---- |
| CTYPE-001 | Ubiquitous | The `CardTypeId` shall be a `sealed record` holding `string Value` as a `get`-only property |
| CTYPE-002 | Unwanted | If `Of(value)` is called with null / empty / whitespace, then `ArgumentException` shall be thrown |
| CTYPE-003 | Event-driven | When `Of(value)` is called with a non-blank string, the `CardTypeId` shall expose it via `Value` |
| CTYPE-004 | Ubiquitous | Two `CardTypeId` instances shall be equal iff their `Value` strings are equal (record default) |
| CTYPE-005 | Unwanted | If `Of(value)` is called with a string containing `'#'`, then `ArgumentException` shall be thrown(§8 で予約済の `CardId.Value` 区切り文字、runtime 強制) |

配置: `docs/specs/domain/cards/card-type-id.md` + `.feature`

### 7. 既存 EARS / ADR の修正

| 文書 | 修正内容 |
| ---- | ---- |
| `docs/specs/domain/cards/card-id.md` + `.feature` | `CardId` を「`(CardTypeId, int Instance)` 複合型 / instance unique」として再定義、CARD-001 以降の要件文を全面改訂 |
| `docs/specs/domain/cards/hand.md` | HAND-003 / HAND-005 の「ユニーク CardId 集合」の意味を明示(CardId = instance unique による正当性)|
| `docs/specs/domain/cards/pile.md` | PILE 系は変更不要(順序付きシーケンス、重複に対する立場は元から開放) |
| ADR-0002 | 「Phase 1 Domain 集約境界」の `CardId` 概念欄に「ADR-0018 で意味を instance unique へ refactor」注記 |
| ADR-0006 | ICardCatalog API `Get(CardId)` → `Get(CardTypeId)` の API breaking change を Related で接続 |
| ADR-0007 | `ICardCatalog<TEffect>` ジェネリック化記述に CardTypeId 引数化を追記 |
| ADR-0012 | M4 ScriptableObjectCardCatalog の lookup key 変更を Related で接続 |

### 8. 永続化 schema の検証

`PersistedSessionV1` DTO の `CardId` シリアライゼーション:

- 旧 schema:`"cardId": "00"` (string)
- 新 schema:`"cardId": { "typeId": "00", "instance": 5 }` (object) または `"cardId": "00#5"` (string、`Value` プロパティ)
- 採用:**string `"00#5"` 形式**(後方互換性検証可能 + JSON サイズ小)、`JsonConverter<CardId>` を実装して `Value` (string) ↔ `CardId` の変換を行う
- 旧セーブデータ migration:M4-PR5 永続化導入後オーナー側で使用開始したばかりのため migration は不要(必要なら破棄推奨)

---

## Consequences

### Positive

- 業界デファクト(Card Type / Card Instance 分離)準拠で Domain 設計が clean
- `Hand` の unique 制約が CardId instance unique で正当化(EARS の文言修正不要)
- Phase 3 以降の拡張(同種カード複数枚デッキ / カード instance 効果 / 永続化された個別カード履歴)が自然に扱える
- `catalog.Get(cardTypeId)` の API シグネチャが「種別から CardData を引く」意図を型で明示
- M5-PR4 / M5-PR5 で見送られていた Hand 重複問題の根本対処、Phase 2 完結への必要条件を満たす

### Negative

- **超大規模 breaking change**:CardId 52ファイル / CardData 27ファイル / catalog 系 36ファイル + 全 EARS(CARD- / HAND- / PILE- / DZ- / INF- 多数)
- `CardId.Of(string)` 旧 API の呼び出し全箇所(テスト含む)を `CardId.Of(CardTypeId, int)` に書き換え必要
- 既存セーブデータ(M4-PR5 以降)は破棄(オーナー側で使用開始したばかりなので影響軽微)
- Phase 1 完結時点の Domain 設計(ADR-0002)を覆す形になる(SSOT は本 ADR-0018 に移行)
- レビューが時間を要する(commit 分割でレビュー容易性を担保)

### 機械検知 / 統合テストの今後

本件は「Domain 設計の暗黙の二重意味」が原因で EditMode 単体テストでは検出できなかった(Hand テストが「異種カードを並べる」ケースに偏っていた)。Phase 3 以降の再発防止策(本 ADR では Out of Scope、`docs/todo.md` で追跡):

- Hand テストに「同種カードを複数 instance 配布する正常系」を追加して回帰防止
- catalog テストに「`CardId.TypeId` を経由した lookup の動作確認」シナリオを追加

---

## Related

- ADR-0002「Phase 1 Domain 集約境界」 — `CardId` 概念を本 ADR で refactor
- ADR-0006「M1 — Application interfaces」 — `ICardCatalog.Get(CardId)` を `Get(CardTypeId)` に変更
- ADR-0007「M2 — カード効果」 — `ICardCatalog<TEffect>` ジェネリック化に CardTypeId 引数化を追記
- ADR-0012「M4 — ScriptableObject + 永続化」 — `ScriptableObjectCardCatalog` lookup key 変更、`PersistedSessionV1` の CardId schema 変更
- ADR-0017「PlayerRoster wrapper」 — VContainer 統合の M5 fix、本 ADR と同じ「Phase 2 完結のための後追い fix」群
- 発覚 PR:なし(M5 UI 実機検証で確定、ADR-0017 PR #95 マージ後の M5 BootAsync 実行で再現)
- 修正 PR:本 ADR と同 PR(`refactor/m4-cardtypeid-instance-id-separation`)
