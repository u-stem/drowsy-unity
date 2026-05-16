# ADR-0002: Phase 1 Domain 拡張の集約境界と概念モデル

> **Note(ADR-0018 関連、2026-05-16)**:本 ADR で導入した `CardId`(単純文字列型、catalog key と Hand instance unique を兼ねる)は [ADR-0018](0018-cardtypeid-cardid-instance-separation.md) で `(CardTypeId TypeId, int Instance)` 複合型に refactor された。catalog key 用途は新設 `CardTypeId` が担い、`CardId` は instance unique 識別子に専念する(Hand の unique 制約は instance unique で正当化)。

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-09 |
| Decider | プロジェクトオーナー |

## Context

Phase 0 で Domain 骨格(`CardId` / `Pile` / `IRandomSource` / `XorShiftRandom`)を導入し、25 NUnit テスト緑・Domain C0 100% を達成した。Phase 1 ではここに `CardData` / `Hand` / `PlayerState` / `GameState` / `TurnState` を順次追加し、汎用カードゲームエンジン基盤として 1 ゲームのライフサイクル(初期配布 → ターン進行 → 終了判定)を Domain だけで表現できる状態に持ち込む。

このタイミングで以下の設計判断をまとめて確定し、後続 PR の指針とする必要がある。これらは複数の妥当な選択肢があり、後から覆すと既存テストおよび後続 PR を全て巻き戻すコストが高い。

1. **集約境界** — GameState を単一ルート集約にするか、Player / Match を独立集約にするか、Pile フラット構造にするか
2. **Card 抽象** — Suit/Rank を Domain で持つか、CardData 値オブジェクトを置くか、CardId のみで中身は外部化するか
3. **CardData 等値性** — Card 抽象で CardData を採用する場合、辞書型属性をどう値同値で比較するか
4. **Immutability** — Domain 全体を immutable に揃えるか、集合系のみ mutable を許容するか、すべて mutable とするか
5. **Player 想定** — 最初から N 人想定にするか、Phase 1 はシングル固定にするか、Pile の名前で表現するか

なお既存 `Pile.cs` は既に **完全 immutable**(`sealed class` + `private readonly CardId[]`、全操作が新 Pile を返す純関数)として実装済みである。`Immutability` の選択はこの既存方針との一貫性も判断材料に含む。

`System.Collections.Immutable` は Unity 6 / 当該 Mono 版で `internal` アクセシビリティのため利用不可、という制約も Phase 0 で判明している(`Pile.cs` の XML doc コメントに記録済)。

## Decision

### 4 つの設計判断

| 軸 | 決定 |
| ---- | ---- |
| 集約境界 | **GameState 単一ルート集約**。Deck / Discard / Field / Players / Turn を保持し、すべての状態変更は GameState を通る。手札 (Hand) は GameState の直接プロパティではなく `PlayerState[i].Hand` として `Players` の内側に保持する |
| Card 抽象 | **`CardId` + `CardData`**。`CardData` は Domain の値オブジェクトとして `Name` と `Attributes`(数値属性)を持つ。Suit/Rank はトランプ系ゲームでは `Attributes` で表現。`ScriptableObject` 等での復元は Infrastructure の責務 |
| `CardData` 等値性 | **`record` の auto-equals は `Attributes`(辞書)に対して参照同値となるため、`Equals` / `GetHashCode` を override して値同値を保証する**。具体的な実装方法と要件 ID は PR-1 の EARS 仕様で確定 |
| Immutability | **Domain 全体を immutable に統一**(既存 `Pile` の方針を踏襲)。`record` または `sealed class` + `private readonly` フィールド、全変更操作が新インスタンスを返す純関数 |
| Player 想定 | **最初から N 人想定**。`PlayerId` 値オブジェクトと `Players: IReadOnlyList<PlayerState>` を `GameState` が保持。Phase 1 のテストは N=1 と N=2 を両方カバーして「汎用性」を担保する |

### 集合体の構造

```
GameState (root, immutable record)
├── Players      : IReadOnlyList<PlayerState>
│                  └── PlayerState (immutable record)
│                       ├── Id    : PlayerId
│                       └── Hand  : Hand
│                                   └── Hand (immutable, Pile と類似だが意味語彙で独立クラス)
├── Deck         : Pile          (山札)
├── Discard      : Pile          (捨て札)
├── Field        : Pile          (場。場を持たないゲームでは Pile.Empty で固定し null は使わない)
└── Turn         : TurnState     (現在ターン番号 / 現在プレイヤー Index)
```

- `Hand` は内部実装が `Pile` と類似するが、API は手札意味語彙(`Add` / `Remove(CardId)` / `Contains` 等)で公開する独立クラスとする。`Pile` は山札・捨て札・場の語彙、`Hand` は手札の語彙。
- `CardData` は `CardId` と独立して存在し、Domain は `CardId` のみで完結する操作を優先する。Application 層に `ICardCatalog` インターフェースを置いて `CardData` を復元する設計を想定する(Domain は外部具体データ源を知らない、という依存方向ルールに従い、Repository 相当の interface は Application に置く / 実装は Infrastructure に置く)。**`ICardCatalog` の起票は Phase 2 スコープ**(本 ADR の実装順序表には含まない)。Phase 1 では `CardData` の値オブジェクト定義のみを Domain に追加する。

### 不変コレクションの表現

`System.Collections.Immutable` が Unity 6 / 当該 Mono 版で利用不可(`internal` アクセシビリティ)のため、以下のパターンを Domain 全体で踏襲する。これは `Pile.cs` で既に確立済み。

| 用途 | 内部実装 | 公開型 |
| ---- | ---- | ---- |
| 順序ありカード列 | `private readonly CardId[]` | `IReadOnlyList<CardId>` |
| 順序ありプレイヤー列 | `private readonly PlayerState[]` | `IReadOnlyList<PlayerState>` |
| 数値属性辞書 | `private readonly Dictionary<string, int>` | `IReadOnlyDictionary<string, int>` |

コンストラクタで防御的コピーを取る。public constructor は `IEnumerable` を受け取り `.ToArray()` する。internal 用に「既に所有権を持つ配列を直接ラップする」private constructor を併設して防御コピーを省略するのは `Pile` と同じパターン。

### 実装順序(後続 PR の単位)

1 PR = 1 論理変更の規約に従い、以下の順で進める。各 PR は EARS 仕様 + Gherkin + NUnit テスト + 要件 ID + 100% C0 カバレッジを伴う。

| # | PR スコープ | 依存 | 新規モジュール ID |
| ---- | ---- | ---- | ---- |
| 1 | `CardData` 値オブジェクト | なし | `CDATA-NNN` |
| 2 | `Hand` 集合 | `CardId` | `HAND-NNN` |
| 3 | `PlayerId` + `PlayerState` | `Hand` | `PLAYER-NNN` |
| 4 | `GameState` ルート集約 | `Pile` / `Hand` / `PlayerState` | `GS-NNN` |
| 5 | `TurnState` + ターン進行 | `GameState` | `TURN-NNN` |

各 PR の完了条件:
- `docs/specs/domain/<module>/<feature>.md`(EARS)
- `docs/specs/domain/<module>/<feature>.feature`(Gherkin)
- `Assets/_Project/Scripts/Domain/<Module>/*.cs`
- `Assets/_Project/Scripts/Tests/Domain.Tests/<Module>/*.cs`
- 要件トレーサビリティ ID(EARS と NUnit `[Property("Requirement", "<ID>")]` の双方向検証通過)
- Domain C0 95%+ を維持(Phase 0 の 100% を退行させないこと。新規 PR で追加されるロジックも 100% を目標とし、95%+ は CI 失敗ライン)

## Consequences

### Positive

- GameState 単一ルートにより状態管理がシンプル(Save / Load / Undo / リプレイがすべて GameState の差し替えで実現)
- 全 immutable で並列処理・テスト・デバッグが安全
- Card 抽象を CardData で持つことで、トランプ系・モンスター系・効果カード系どの種類のゲームにも対応可能
- N 人プレイヤー想定により、Phase 2 以降でマルチプレイヤー対応に踏み出すときに API 変更が不要

### Negative

- GameState 単一ルートのため、変更パスが GameState の `With...` 系メソッドに集中し、メソッド数が増える
- 全 immutable のため、シャッフルや大量配布時に毎回新配列を確保する GC プレッシャー(WebGL 主対象では特に注意)
- N 人想定により単一プレイヤーゲームでも Players: `IReadOnlyList<PlayerState>` + TurnIndex の冗長な構造が常時付きまとう
- `System.Collections.Immutable` 不在のため独自 immutable パターンが必要(既に `Pile` で確立済みなので追加負担は小)

### Neutral

- `Hand` を `Pile` の派生 / 別名にせず独立クラスにすることで、Domain の語彙が増える(意味の明示と引き換えに概念数が増える)
- `CardData.Attributes` を `IReadOnlyDictionary<string, int>` で持つため、属性キー・値の妥当性検証は Application / Infrastructure 側のスキーマ検証で行う必要がある

## Alternatives Considered

| 軸 | 不採用案 | 不採用理由 |
| ---- | ---- | ---- |
| 集約境界 | Player / Match を独立集約 | 小〜中規模カードゲームでは GameState ルートで十分。集約を分けるとトランザクション境界の管理コストが増える |
| 集約境界 | Pile フラット構造のみ(Player 抽象なし) | 「汎用」を突き詰めると魅力的だが、テスト容易性 / 仕様読み手の認知負荷が上がる。「汎用性」と「読みやすさ」のバランスで現案を採用 |
| Card 抽象 | Suit/Rank を Domain に直接 enum 定義 | トランプ系特化となり「汎用カードゲームエンジン」の主目的に反する |
| Card 抽象 | CardId のみ(中身は完全に Infrastructure) | Domain でカード名の重複検査などが書けない、ルール判定が CardId のテーブル参照だけで完結するゲームに限定される |
| Immutability | GameState immutable + Pile mutable のハイブリッド | 既存 Pile が完全 immutable で実装済み、リファクタの逆行は無価値 |
| Immutability | GameState も mutable | テスト・デバッグ・スナップショットが困難、`Pile` と方針が割れる |
| Player 想定 | Phase 1 は単一プレイヤー固定 | Phase 2 への移行時に GameState の API が破壊的変更になる |
| Player 想定 | Pile の名前で Player を表現 | Domain の語彙から `Player` が消えて読みにくく、ターン進行の概念が表現しにくい |

## Implementation Notes

### CardData の素朴定義例(参考、確定 API は PR-1 の EARS 仕様で再検討)

```csharp
public sealed record CardData(string Name, IReadOnlyDictionary<string, int> Attributes)
{
    public bool HasAttribute(string key) => Attributes.ContainsKey(key);
    public int GetAttribute(string key, int defaultValue = 0) =>
        Attributes.TryGetValue(key, out var v) ? v : defaultValue;
}
```

防御的コピーは値オブジェクトのコンストラクタ側で行う(`Attributes` を内部で `new Dictionary<string, int>(source)` してから `IReadOnlyDictionary` として保持)。`record` の auto-equals は `Dictionary` を参照同値で比較するため値同値性が壊れる。Decision テーブルに従い `Equals` / `GetHashCode` の override は **必須**(同値ペアキーを順序非依存で比較し、ハッシュも順序非依存で合成)。具体的な要件文と要件 ID は PR-1 の EARS 仕様で `[CDATA-NNN]` として明記する。

### Domain 集合型の値同値性方針(PR-2 で確定、Pile は TODO-1 完了 PR で揃え済み)

PR-1 で `CardData` に値同値性(`Equals` / `GetHashCode` / `operator==` / `operator!=` を override)を導入し、PR-2 で `Hand` にも順序付きシーケンス同値の値同値性を導入した。一方、Phase 0 で実装済みの `Pile` は値同値性を持たず参照同値のままで残っていたが、ADR-0003 で確立した TODO 運用の初回エントリ「TODO-1 Pile に値同値性を追加」として本 ADR の予約通り後追いで揃えた(PR-2 と PR-3 の間に挿入)。これにより Domain 集合型(`Pile` / `Hand` / `CardData`)は全て値同値で揃った状態となり、PR-4 (GameState) で内部の Pile / Hand を含む等値比較が破綻なく実装できる。

**完了**: TODO-1 完了 PR で Pile に PILE-014〜017 を追加(Hand と完全対称な順序付きシーケンス同値)。本セクションのトレーサビリティはここで閉じる。

### N=1 と N=2 のテスト網羅

PR-3 (`PlayerState`) と PR-4 (`GameState`) では、N=1(単一プレイヤー想定の最小ケース)と N=2(複数プレイヤー想定の最小ケース)の双方に対し、初期配布・手札追加・ターン開始の各シナリオをテストする。N=3 以降は MC/DC 観点で「N=1 と N=2 のテストで網羅できない分岐があるか」を検討してから追加する。

### Phase 1 完了後の状態

Phase 1 完了時、Domain は「ルールロジックを含まない、純粋な状態モデル + 状態遷移関数」として完成する。`IGameRule` / `IGameAction` interface(具体ゲームのルール差込)は Phase 2 以降の射程として本 ADR の範囲外とする。Phase 0 で導入済みの `IGameConfig` (ひな形) も Phase 2 以降で具体プロパティを追加する。

**完了**: PR-1〜PR-5 + TODO-1 のすべてがマージされ、Phase 1 は完結した(完了確認日: 2026-05-10、Date フィールドは起票日のため変更しない)。Domain 全 9 クラス(`CardId` / `CardData` / `Hand` / `Pile` / `PlayerId` / `PlayerState` / `GameState` / `TurnState` / `XorShiftRandom`)で C0 100%(427/427 行、87/87 メソッド)、NUnit 189 件全緑。本 ADR の実装順序表のトレーサビリティはここで閉じる。

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §5 アーキテクチャ依存ルール、§6 テスト方針、§9 定数管理方針
- 関連設計: [`docs/architecture/dependency-rules.md`](../architecture/dependency-rules.md) §2.1 Domain — 既に `Hand` / `Player` / `GameState` / `TurnState` の用語を含む(本 ADR で具体化)
- 既存実装の踏襲元: `Assets/_Project/Scripts/Domain/Cards/Pile.cs`(immutable パターンと `System.Collections.Immutable` 不在制約の記録)
- 既存仕様の踏襲元: `docs/specs/domain/cards/pile.md` / `docs/specs/domain/cards/card-id.md`
- 後続 PR: 本 ADR の「実装順序」表の 5 PR(PR-1 〜 PR-5)
- 完了 PR(マージ済): PR #12 (PR-1 CardData) / PR #13 (PR-2 Hand) / PR #15 (TODO-1 Pile 値同値性、ADR-0003 運用初回適用) / PR #16 (PR-3 PlayerState + ADR-0004) / PR #17 (PR-4 GameState) / PR #18 (PR-5 TurnState + GameState 拡張、Phase 1 最終確認)
