# ADR-0006: M1 詳細 — 汎用 Application 層 interface と DrowZzz 最小実装の設計

> **Note(ADR-0018 関連、2026-05-16)**:本 ADR で確定した `ICardCatalog<TEffect>.Get(CardId)` / `TryGet(CardId, out CardData)` / `GetEffects(CardId)` の引数型は [ADR-0018](0018-cardtypeid-cardid-instance-separation.md) で `CardTypeId` に変更された(catalog の責務「種別 → CardData / 効果列」を型で明示するため)。本 ADR の Status は `Accepted` のまま、API 引数型変更を Related で接続。

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-10 |
| Decider | プロジェクトオーナー |

## Context

ADR-0005 で Phase 2 のロードマップを M1〜M5 に分割した。本 ADR は **M1(ターン進行 + カードプレイの最小骨格)の詳細設計** を確定する。

ADR-0005 で決まっているスコープ:

- 縦串で本命ゲーム DrowZzz を直接実装、練習用ゲームを挟まない
- リポジトリ戦略は同 `drowsy-unity` 同居
- 名前空間: 汎用 interface(`Drowsy.Application` 直下) + DrowZzz 固有(`Drowsy.Application.Games.DrowZzz`)
- DI 方針: M1〜M4 は Pure C#、M5 で VContainer 統合
- ロジック先行(Presentation は M5 まで保留)

本 ADR で確定する詳細:

1. 汎用 Application 層 interface の最小 API(`IGameAction` / `IGameRule` / `ICardCatalog`)
2. DrowZzz 固有実装の構造(`DrowZzzAction` / `DrowZzzGameSession` / `DrowZzzTurnPhase` / `DrowZzzRule`)
3. UseCase 構成(`StartGameUseCase` / `ApplyActionUseCase` のハイブリッド構成)
4. M1 で用いる DrowZzz の最小ルール(プロジェクトオーナーから JIT 共有された範囲)
5. `IGameConfig` の Phase 2 拡張予定(`FdpPool` / `MaxRoundNumber` 等)
6. Phase 1 用語(`TurnState.TurnNumber` = サブターン)と DrowZzz 用語(「ターン」 = ラウンド)の整理

### M1 で動かす DrowZzz の最小ルール(プロジェクトオーナーから共有された範囲)

| 項目 | 値 |
| ---- | ---- |
| プレイヤー数 | N=2(現時点ルール、将来 N>2 拡張は Phase 3) |
| セットアップ | 4 ステップ: (1) ゲーム開始 → (2) 先行・後攻を完全ランダムで決定 → (3) FDP プールから被りなく抽選 → (4) 山札から各 5 枚配布 |
| FDP (First Drowsy Point) | プレイヤーごとに隠し情報、ゲーム開始時に確定し以降不変 |
| FDP プール | `[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]`(10 個、不均等間隔) |
| ターン進行 | 「ドロー → 行動(カードを場に出すだけ) → ターン終了」の固定 3 ステップ |
| 「行動」の M1 範囲 | 手札から場にカードを 1 枚出す(効果なし、M2 で実装) |
| 「省略」部分 | M1 範囲外、将来 PR / ADR で追加(プロジェクトオーナーが JIT 共有) |
| 1 ターン目と通常時の差 | M1 では考慮せず、「通常時の Draw → Play → EndTurn」固定 |
| ターン上限 | 全体 20 ラウンド(M3 で実装、本 ADR では Implementation Notes に記録のみ) |

ルールメモ自体は Public リポジトリに置かず、必要な部分のみ EARS / Gherkin として `docs/specs/games/drowzzz/` に体系化する(ADR-0005 の運用ルール)。

## Decision

### 1. 汎用 Application 層 interface(`Drowsy.Application` 直下)

#### 1.1 `IGameAction`

ゲーム上のアクションを表すマーカー interface。具体形は各ゲームの `*Action` 階層が record で実装する(C# の Discriminated Union 風表現を record + マーカー interface で代替)。

```csharp
namespace Drowsy.Application;

public interface IGameAction
{
}
```

#### 1.2 `IGameRule<TAction, TSession>`

状態遷移ルールを表す純関数 interface。型パラメータでアクションとセッションをゲームごとに具象化する。

```csharp
namespace Drowsy.Application;

public interface IGameRule<TAction, TSession>
    where TAction : IGameAction
{
    bool IsLegalMove(TSession session, TAction action);
    TSession Apply(TSession session, TAction action);
}
```

最小 API は 2 メソッド(`IsLegalMove` / `Apply`)。`EnumerateLegalActions` のような AI / UI ヒント用 API は M5 以降で必要になった時点で拡張する(YAGNI)。

**M3 で追加予定のメソッド**: ADR-0005 の M3 完了基準に従い、`bool IsTerminated(TSession)` および `PlayerId? GetWinner(TSession)`(または同等の戻り値型)を M3 着手 PR で追加する。本 ADR では interface 形を確定せず、M3 着手 ADR / PR で確定する。

#### 1.3 `ICardCatalog`

`CardId` から `CardData` を引く責務を持つ。実装は M2 で SO ベース(`Drowsy.Infrastructure.Games.DrowZzz.ScriptableObjectCardCatalog`)、M1 では in-memory のスタブ実装(`InMemoryCardCatalog`)で代替する。

```csharp
namespace Drowsy.Application;

public interface ICardCatalog
{
    CardData Get(CardId id);
    bool TryGet(CardId id, out CardData? data);
}
```

最小 API は `Get` + `TryGet`。`AllIds()` 列挙メソッドは M2 で SO 化する際に必要なら追加。

#### 1.4 `IGameConfig` の Phase 2 拡張予定

Phase 0 で空ひな形として導入された `IGameConfig`(`Drowsy.Domain.Configuration` namespace、`IGameConfig.cs` 参照)に、Phase 2 進行中に DrowZzz 用プロパティを追加する。本 ADR では追加予定のプロパティを **記録のみ**(M1 では `FdpPool` のみ追加、`MaxRoundNumber` 等は M3 着手 PR で個別に追加)。

`IGameConfig` は **`Drowsy.Domain.Configuration` namespace に残す**(`ICardCatalog` を Application に置く判断と非対称だが意図的)。理由は、`IGameConfig` は「ゲームバランス調整可能値」(L3 定数、`docs/architecture/constants-management.md` 参照)を表す純データ interface であり、永続化や外部 I/O を行わないため Domain でも安全に保持できる。これに対して `ICardCatalog` は外部データ源(SO / JSON 等)からの復元責務を持つため Application 配置が筋(ADR-0002 の Repository 配置原則と同様)。

| プロパティ | 型 | 値(DrowZzz) | 追加 PR | 備考 |
| ---- | ---- | ---- | ---- | ---- |
| `FdpPool` | `IReadOnlyList<int>` | `[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]` | **M1-PR3**(`StartGameAction` Apply 実装時) | `StartGameAction` Apply で参照する必要が出るタイミングで追加 |
| `MaxRoundNumber` | `int` | `20` | M3 着手 PR | M3 でゲーム終了判定に参照、M1 では未実装 |
| (将来) | — | — | M2 以降 | カード効果実装中に必要に応じて追加 |

### 2. DrowZzz 固有実装(`Drowsy.Application.Games.DrowZzz`)

#### 2.1 `DrowZzzAction` 階層

M1 範囲で確定するのは 4 種:

```csharp
namespace Drowsy.Application.Games.DrowZzz;

public abstract record DrowZzzAction : IGameAction;

public sealed record StartGameAction : DrowZzzAction;
public sealed record DrawCardAction : DrowZzzAction;
public sealed record PlayCardAction(CardId Card) : DrowZzzAction;
public sealed record EndTurnAction : DrowZzzAction;
```

- プレイヤー Id は payload に持たせず、`session.GameState.Turn.CurrentPlayerIndex` から暗黙取得する(現ターンプレイヤーが行うアクション、という前提)
- 「省略」相当の Action 種別は本 ADR では追加しない(将来 PR / ADR で追加)

#### 2.2 `DrowZzzGameSession`

DrowZzz の完全状態(オラクルビュー、隠し情報含む)を表す record class。Domain `GameState` をラップし、DrowZzz 固有の追加状態(FDP / TurnPhase)を保持する。

```csharp
// using Drowsy.Domain.Game;       // GameState
// using Drowsy.Domain.Players;    // PlayerId
namespace Drowsy.Application.Games.DrowZzz;

public sealed record DrowZzzGameSession
{
    public GameState GameState { get; init; }
    public IReadOnlyDictionary<PlayerId, int> FirstDrowsyPoints { get; init; }
    public DrowZzzTurnPhase TurnPhase { get; init; }

    // DrowZzz の「ターン (=ラウンド)」を Phase 1 TurnNumber から計算
    public int CurrentRound => (GameState.Turn.TurnNumber + 1) / 2;  // N=2 想定、N>2 は Phase 3
}
```

- record class + `init` setter で `with { ... }` 式(ADR-0004 polyfill 前提)
- **null 検証は `GameState` の init setter パターンを踏襲**:バッキングフィールド + `value ?? throw new ArgumentNullException(nameof(value))` を 3 プロパティ全部に適用、Players との整合検証(`FirstDrowsyPoints` のキーが `GameState.Players.Select(p => p.Id)` に一致するか等)はコンストラクタおよび cross-field の init setter で行う(M1-PR2 で実装)
- `FirstDrowsyPoints` は **完全可視オラクル**。各プレイヤー視点フィルタは Presentation 層(M5)で実装
- `CurrentRound` は計算プロパティ(N=2 専用、N>2 拡張時は再設計)

#### 2.3 `DrowZzzTurnPhase`

ターン内のフェーズを表す enum。Domain `TurnState` には影響を与えず、DrowZzz 固有のターン内ステートマシンを Application 層で管理する。

```csharp
public enum DrowZzzTurnPhase
{
    WaitingForDraw,
    WaitingForPlay,
    WaitingForEndTurn,
}
```

#### 2.4 `DrowZzzRule`

`IGameRule<DrowZzzAction, DrowZzzGameSession>` の具体実装。M1 範囲では以下の状態遷移を扱う。

##### `IsLegalMove` の判定

| TurnPhase | 合法な Action |
| ---- | ---- |
| `WaitingForDraw` | `DrawCardAction` のみ |
| `WaitingForPlay` | `PlayCardAction(CardId)`(`session.GameState.Players[CurrentPlayerIndex].Hand` に該当 `CardId` がある場合のみ合法) |
| `WaitingForEndTurn` | `EndTurnAction` のみ |

`StartGameAction` は「セッション未生成状態」で呼ぶため、別系統の API(`StartGameUseCase`)で扱い、`IsLegalMove` 判定は通らない。

##### `Apply` の状態遷移

| Action | 遷移 |
| ---- | ---- |
| `StartGameAction` | セッションを新規生成。先後ランダム決定 → FDP プールから被りなく抽選 → 山札から各 5 枚配布 → `TurnPhase = WaitingForDraw`(専用 API、`IGameRule.Apply` ではなく `StartGameUseCase` で扱う) |
| `DrawCardAction` | 山札から現プレイヤーの手札に 1 枚移動 → `TurnPhase = WaitingForPlay` |
| `PlayCardAction(card)` | 現プレイヤーの手札から指定 `card` を `Field` に移動 → `TurnPhase = WaitingForEndTurn` |
| `EndTurnAction` | `GameState.Turn` を `Next(playerCount)` で次サブターンへ → `TurnPhase = WaitingForDraw`(M1 範囲では「ターン上限」判定なし、M3 で追加) |

### 3. UseCase 構成(ハイブリッド)

```csharp
namespace Drowsy.Application.Games.DrowZzz;

// セッションを新規生成する特殊 UseCase
public sealed class StartGameUseCase
{
    public StartGameUseCase(IRandomSource rng, ICardCatalog catalog, IGameConfig config) { ... }
    public DrowZzzGameSession Execute();  // payload なし、内部で全部やる
}

// 既存セッションに Action を適用する統一 UseCase
public sealed class ApplyActionUseCase
{
    public ApplyActionUseCase(DrowZzzRule rule) { ... }
    public DrowZzzGameSession Execute(DrowZzzGameSession session, DrowZzzAction action);
}
```

- `StartGameUseCase` だけ別(セッション未生成からの開始は特殊)
- それ以外の Action は `ApplyActionUseCase` 統一(`Draw` / `Play` / `EndTurn` を 1 メソッドで)
- 「Action 種別ごとの個別 UseCase」(`PlayCardUseCase` 等)は採らない(統一の方が UI / リプレイ実装で扱いやすい)

**namespace 配置の判断**: 両 UseCase は `Drowsy.Application.Games.DrowZzz` に置く(`Drowsy.Application` 直下ではない)。理由は `DrowZzzGameSession` 型と `DrowZzzAction` 型に強く依存するため、汎用 UseCase ではなく DrowZzz 固有 UseCase に分類される。汎用 `IGameRule<TAction, TSession>` を直接呼ぶ薄い抽象 UseCase(`ApplyActionUseCase<TAction, TSession>`)を `Drowsy.Application` 直下に置く案は M2 以降で必要性が判明してから検討(YAGNI)。

**`IsLegalMove` 違反時の方針**: `ApplyActionUseCase.Execute(session, action)` は **内部で `IsLegalMove` を検証し、`false` の場合は `InvalidOperationException` を投げる**(`Pile.Draw` の空 Pile 例外と同じ防御的パターン)。Result 型(`Option<DrowZzzGameSession>`)を返す案も検討したが、Phase 1 までの Domain は例外で異常状態を表現する設計に統一されており、UseCase 層もこれを踏襲する。Presentation 層(M5)で「呼ぶ前に `IsLegalMove` で検証する」運用を取ることで通常パスは例外を経由しない。

### 4. DI 方針(M1〜M4 は Pure C#)

ADR-0005 の決定どおり、M1〜M4 は VContainer **の機能** を使わず Pure C# のコンストラクタインジェクションで構成する。

- `StartGameUseCase` は `IRandomSource` / `ICardCatalog` / `IGameConfig` を constructor で受け取る
- `ApplyActionUseCase` は `DrowZzzRule`(または直接 `IGameRule<DrowZzzAction, DrowZzzGameSession>`)を受け取る
- テスト時はインターフェースを直接スタブ化(`InMemoryCardCatalog` / `XorShiftRandom` 等)して構築

**asmdef references について**: 既存 `Drowsy.Application.asmdef` は Phase 0 時点で `VContainer` / `R3.Unity` / `UniTask` を `references` に含めている。本 ADR はこれらの **参照を削除しない**(M5 で実利用する前提、M1〜M4 では import するが API を呼ばない)。テストで VContainer を使わないことで「Pure C#」の哲学を担保し、参照の有無自体は変更しない。

M5 で `VContainer.LifetimeScope` を導入し、各 UseCase / `DrowZzzRule` / インフラ実装をコンテナ登録する。

### 5. テスト用 asmdef 新設

`Drowsy.Application.Tests` を新設する。

| asmdef | 用途 |
| ---- | ---- |
| `Drowsy.Application` | 既存(Phase 0)、本 ADR で interface 群を追加 |
| `Drowsy.Application.Tests` | **本 ADR で新設**、`Drowsy.Application` および `Drowsy.Domain` を参照、NUnit テスト |

DrowZzz 固有実装と汎用 interface のテストを **同じ asmdef** に置く(別 asmdef 分離は規模が小さい間は過剰)。Tests 配下のディレクトリ構造で論理分離する:

```
Assets/_Project/Scripts/Tests/Application.Tests/
  Drowsy.Application.Tests.asmdef
  Catalog/        InMemoryCardCatalogTests
  Games/DrowZzz/  StartGameUseCaseTests / ApplyActionUseCaseTests / DrowZzzRuleTests
```

### 6. M1 範囲外の事項(本 ADR で明記)

| 項目 | 扱う場所 |
| ---- | ---- |
| カード効果 | M2 |
| 勝敗判定 | M3 |
| ターン上限(20 ラウンド)の実装 | M3(本 ADR では Implementation Notes に記録のみ) |
| 永続化 | M4 |
| UI / Bootstrap | M5 |
| 「省略」相当の Action 種別 | 将来 PR / ADR(プロジェクトオーナーが JIT 共有) |
| 1 ターン目と通常時の差 | 同上 |
| スキップ動作 | 同上 |
| N>2 プレイヤー | Phase 3 |
| `DrowZzzGameSession.CurrentRound` の N>2 対応 | Phase 3(現在は N=2 想定) |
| 山札枯渇 / 手札 0 枚の挙動 | M2 以降 |

### 7. 用語整理(ADR-0006 で確定)

Phase 1 で実装した `Drowsy.Domain.Game.TurnState.TurnNumber` と DrowZzz 用語の関係を明確化する。

| 用語 | 定義 |
| ---- | ---- |
| `TurnState.TurnNumber`(Phase 1 仕様) | **サブターン番号**(1 プレイヤー動作 = +1)、Phase 1 仕様維持(再定義しない) |
| DrowZzz の「ターン」 | **ラウンド**(全プレイヤーが 1 動作した 1 サイクル)、N=2 で Phase 1 サブターン 2 つ分 |
| `DrowZzzGameSession.CurrentRound` | 計算プロパティ。N=2 で `(TurnNumber + 1) / 2` |
| ゲーム終了判定(M3) | `TurnState.TurnNumber > MaxRoundNumber × Players.Count`(M3 で実装、N=2 / `MaxRoundNumber=20` で `TurnNumber > 40` のとき終了) |

**ゲーム終了判定式の妥当性メモ(M3 実装者向け)**: `TurnState.TurnNumber` は 1 始まりで `Next()` により単調増加するため、`Initial(playerIndex)` の `playerIndex` が 0 でも 1 でも上記式は同じく成立する(先行プレイヤーの index に依存しない)。N=2 の場合、ラウンド 20 の最後のサブターンは `TurnNumber = 40`、`EndTurn` 後に `TurnNumber = 41` となり終了判定が真。

Phase 1 の `turn-state.md` および `TurnState.cs` には手を入れない(後方互換維持)。本 ADR で「Phase 1 TurnNumber は『サブターン番号』として解釈する」を明記し、DrowZzz 用語との対応関係を残す。

## Consequences

### Positive

- M1 着手前に汎用 interface・DrowZzz 固有型・UseCase の最小 API が確定し、後続 PR (M1-PR1〜PR7) のスコープが明確
- ADR-0005 の哲学(ロジック先行 / Pure C# / Domain ゲーム非依存)を実装レベルで担保
- Phase 1 TurnState を変えずに DrowZzz の「ラウンド」概念を Application 層で表現でき、Phase 1 / Phase 2 の責務分離が明確
- IGameConfig の拡張予定(`FdpPool` / `MaxRoundNumber`)が記録され、M2 / M3 着手時の手戻りが少ない
- `IGameRule<TAction, TSession>` のジェネリック設計により、将来別ゲームを追加した際にも汎用 interface が再利用可能(YAGNI を超えない範囲で型安全性を確保)

### Negative

- `DrowZzzGameSession.CurrentRound` が N=2 専用の計算式(`(TurnNumber + 1) / 2`)になっており、N>2 拡張時にロジック修正が必要(Phase 3 候補)
- `IGameRule<TAction, TSession>` のジェネリック型パラメータ 2 つは設計シンプル性とトレードオフ。型推論が効きにくい呼び出し箇所では明示が必要
- `DrowZzzTurnPhase` を Application 層に持つことで、Domain 層の `TurnState` だけでは「次の合法アクション」を判定できない(Application 層 `DrowZzzGameSession` を経由する必要あり)
- 「省略」相当の Action がまだ未確定のため、将来 Action 種別を追加した際に既存 `DrowZzzAction` 階層 / `DrowZzzTurnPhase` enum / `DrowZzzRule.IsLegalMove` に変更が走る可能性
- `StartGameUseCase` だけ別系統(セッション未生成スタート)、UI 層から見ると「1 つの統一 API」ではない非対称性が残る

### Neutral

- M1 着手 PR (M1-PR1〜PR7) で本 ADR の決定を実装に落としていく流れになる
- M2 以降の「省略」相当の Action 種別追加 PR は、本 ADR を更新するのではなく **後続 PR の commit 履歴 + spec ファイルで追跡**(本 ADR は M1 範囲のスナップショット)
- `IRandomSource` を Phase 1 で導入しているため、`StartGameUseCase` の先後ランダム決定 / FDP 抽選はそのまま利用可能(Pile.Shuffle と同じ依存)

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| `IGameAction` を本物の Discriminated Union(`OneOf<...>` ライブラリ等) | 外部依存追加、Unity / asmdef 設定の複雑化、record + マーカー interface で十分 |
| `IGameRule` を非ジェネリック化(`IsLegalMove(object session, IGameAction action)` 等) | 型安全性が失われ、各実装でキャストが必要、Pure C# の利点が減る |
| Action 種別ごとの個別 UseCase(`PlayCardUseCase` / `DrawCardUseCase` 等) | UseCase 数がアクション増加に比例して増える、リプレイ / 履歴処理で統一 API が必要、`ApplyActionUseCase` 統一が筋 |
| `StartGameUseCase` も統一の `ApplyActionUseCase` に統合 | セッション未生成からの開始は「現セッションを引数に取る」前提と矛盾、別系統が筋 |
| `DrowZzzGameSession` を `Drowsy.Domain.Games.DrowZzz` 配下に置く | Domain ゲーム非依存原則(ADR-0002)に反する、Application 配置が筋 |
| `TurnPhase` を Phase 1 `TurnState` に追加 | Phase 1 仕様を Phase 2 で改修、後方互換が崩れ ADR-0002 のクロージャに後戻り影響 |
| `DrowZzzGameSession` を `record struct` に | ヒープ配置を避けたい場面はないため通常の `record class` で十分、`with` 式の null 検証パターンも record class の方が書きやすい |
| FDP を `PlayerState` のフィールド拡張 | Domain がゲーム特化に染まる、ADR-0002 の決定に反する |
| FDP を `CardData.Attributes` 風の汎用辞書として `PlayerState` に追加 | 過剰な汎用化、型安全性減、現状ニーズ(DrowZzz の FDP 1 個)に対して overkill |
| 統一 `ApplyActionUseCase` を `IGameRule` 直接呼び出しに置き換える(UseCase 廃止) | UseCase の責務(検証 / ロギング / トランザクション境界)を将来追加できる余地を残すべき、現状は薄い委譲だが意図的 |
| `Drowsy.Application.Tests` と `Drowsy.Application.Games.DrowZzz.Tests` を別 asmdef 化 | 規模が小さい間は過剰、Tests 配下のディレクトリ構造で論理分離するのが筋(必要時に分離可能) |

## Implementation Notes

### M1 着手 PR 群(ADR-0005 の予定を本 ADR で具体化、**全 7 PR 完了済み 2026-05-11**)

1. **M1-PR1** ✓ (PR #22): 汎用 Application 層 interface + Tests asmdef
   - **既存 `Drowsy.Application.asmdef` を流用**、interface ファイルのみ追加
   - `IGameAction` / `IGameRule<TAction, TSession>` / `ICardCatalog` interface 追加
   - `Drowsy.Application.Tests` asmdef を新設
   - 汎用 interface の軽量契約テスト
2. **M1-PR2** ✓ (PR #23): DrowZzz 固有型の skeleton
   - `DrowZzzAction` 階層(4 種)、`DrowZzzGameSession`、`DrowZzzTurnPhase`、`DrowZzzRule`(NotImplementedException で骨格)
   - `InMemoryCardCatalog`(Application/Catalog/)
   - `Drowsy.Application` 本体に `Compat/IsExternalInit.cs` polyfill 追加
3. **M1-PR3** ✓ (PR #24): `StartGameUseCase` + `IGameConfig.FdpPool` 追加
   - `IGameConfig.FdpPool` プロパティ追加(CFG-101)
   - 先後ランダム決定 / FDP 抽選 / 初期 5 枚配布 / セッション構築
   - JIT 確定: 配布順「1 枚ずつ交互」、UseCase 引数「`(players, initialDeck)`」、FDP 抽選順「先行プレイヤー順」、本物山札サイズ N=2 で 56 枚(`setup.md` Notes)
   - **欠陥修正**: `DrowZzzGameSession.Equals/GetHashCode` override 追加(M1-PR2 由来、record auto-equals が `Dictionary` で参照同値にフォールバックする問題を順序非依存マルチセット同値で解消)
4. **M1-PR4** ✓ (PR #25): `DrawCardAction` の Apply 実装
   - 山札 → 現プレイヤー手札に 1 枚移動、TurnPhase = WaitingForPlay
   - `DrowZzzRule.IsLegalMove` / `Apply` の null 検証を追加(M1-PR3 reviewer 申し送り反映)
5. **M1-PR5** ✓ (PR #26): `PlayCardAction` の Apply 実装 + null 防御
   - 手札 → Field に AddTop で 1 枚移動、TurnPhase = WaitingForEndTurn
   - JIT 確定: `Field` 追加方向「AddTop」(直近プレイカードが Field.Cards[0])、`Card == null` は生成時に弾く
   - **設計確立**: record positional + null 防御の **二重ガードパターン**(バッキングフィールド初期化式 + init setter 本体の両方で `value ?? throw`、CS8907 回避 + with 経路カバー)
6. **M1-PR6** ✓ (PR #27): `EndTurnAction` の Apply 実装 + `ApplyActionUseCase`
   - `GameState.Turn = Next(playerCount)` で次サブターン進行 + TurnPhase = WaitingForDraw
   - `ApplyActionUseCase` 統一 UseCase 新規(IsLegalMove false → InvalidOperationException、true → Rule.Apply 委譲)
   - `_` ケースを「将来 DrowZzzAction 派生型追加用の防御」として再定義、`UnknownDrowZzzAction` ダミー型でカバレッジ確保
7. **M1-PR7** ✓ (PR #28): 統合テスト(N=2 / 数ラウンド分の動作確認)
   - `StartGameUseCase` → `ApplyActionUseCase` の連鎖を Draw → Play → EndTurn で N=2 数ラウンド回す
   - 14 件の統合テスト(StartGame 直後 / 1 サブターン完走 / 1 ラウンド完走 / 3 ラウンド完走 / Deterministic Replay)
   - 新規実装ゼロ、組み合わせ検証のみ
   - `Category("Medium")` 採用(統合テストとして単体 `Small` より一段重い扱い)

各 PR は 1 PR = 1 論理変更の規約に従う。ルールメモから必要な詳細が出てきた時点で当該 PR で EARS / Gherkin に体系化する(ADR-0005 §6 の運用)。

### M1 進行中の学び(将来の record + null 防御 / Equals override パターンの参考)

- **record + 内部 `Dictionary<K,V>` を持つ型の Equals**: M1-PR2 で `DrowZzzGameSession` を `record class` + `Dictionary` バッキングフィールドで実装したが、record auto-generated `Equals(R)` は `Dictionary<K,V>` を `EqualityComparer<T>.Default` (= 参照同値) で比較するため値同値が壊れた。M1-PR3 で `Equals(DrowZzzGameSession)` / `GetHashCode()` を「順序非依存マルチセット同値」で override して解消(Phase 1 `GameState` と同パターン)。**今後の record + 内部 Dictionary / List 型は Equals override が必須**。
- **record positional + null 防御の二重ガード**: M1-PR5 で `PlayCardAction(CardId Card)` の null 防御を試みた際、初期化式 `= Card ?? throw` は constructor 1 回のみ評価で `with { Card = null }` をカバーせず、getter/setter 全置換 init setter のみだと positional `Card` パラメータが未参照となり CS8907 警告が出た。最終形は **両者併用の二重ガード**:
  - バッキングフィールド `_card` の初期化式 `= Card ?? throw`(positional ctor 経由 + CS8907 回避)
  - init setter 本体 `init => _card = value ?? throw`(with 経路)
- **テスト用ダミー派生型による `_` ケースカバレッジ確保**: M1-PR6 で全 Action 種別実装済になり `switch` の `_` ケースに到達する経路が消滅したが、コードカバレッジ 100% 維持と将来拡張防御のため `UnknownDrowZzzAction` をテスト assembly 内に定義して `_` ケースを到達させる手法を採用。
- **JIT 共有方式の有効性**: M1 中の細部(配布順 / Field 追加方向 / FDP 抽選順 / `Card == null` 扱い)はプロジェクトオーナーから着手時に都度受け取る運用で、設計文書に書かれていない隙間が PR ごとに自然に埋まった。M2 以降も同方式で継続。

### 要件 ID prefix(M1 範囲)

| Prefix | 範囲 | 配置 |
| ---- | ---- | ---- |
| `APP-` | 汎用 Application 層 interface の振る舞い | `docs/specs/application/<feature>.md` |
| `DZ-` | DrowZzz 固有ルール(setup / draw / play / endturn 等) | `docs/specs/games/drowzzz/<feature>.md` |

各 PR で対応する要件 ID を採番(M1 中に APP-001 〜 APP-NNN、DZ-001 〜 DZ-NNN)。lefthook traceability スクリプトの対象に含まれる。

### `IRandomSource` の利用

`StartGameUseCase` は `IRandomSource` を受け取り、以下のランダム判定に使用:

1. **先行・後攻決定**: `rng.NextInt(0, 2)` で 0 / 1 を取り、`Players[0]` と `Players[1]` のどちらが先行かを決める(あるいは Players 配列を Shuffle)
2. **FDP 抽選**: `FdpPool` を Shuffle 風に並べ替えて先頭 N 個を取る、または `rng.NextInt(0, pool.Count)` で被りなく抽選

Phase 1 で `Pile.Shuffle(IRandomSource)` を実装済のため、Application 層でも同じ `IRandomSource` を利用すれば Deterministic Replay(将来の M4 永続化 / M5 演出)に有利。

### `StartGameUseCase` の `IsLegalMove` 経由での扱い

`StartGameAction` はセッション未生成状態で呼ぶため、本 ADR では `IGameRule<DrowZzzAction, DrowZzzGameSession>.IsLegalMove(session, StartGameAction)` の判定を **常に false** とする(セッション既存状態でゲーム開始は不可)。

代わりに `StartGameUseCase.Execute()` を直接呼ぶ。これは `IGameRule.Apply` を経由しない別系統。

### Phase 1 TurnState の用語解釈を `turn-state.md` に追記するか

判断: **追記しない**。Phase 1 仕様の本体には手を入れず、本 ADR-0006 で「Phase 1 TurnNumber は『サブターン番号』として解釈する」を確定したことを根拠とする。`turn-state.md` を将来読む人が混乱しないよう、本 ADR §7「用語整理」を Related で参照する。

### TODO 候補(本 PR ではなく後続で扱う)

- `IGameConfig` に `FdpPool` を追加(M1-PR3 で実装)
- `IGameConfig` に `MaxRoundNumber` を追加(M3 着手 PR で実装)
- `DrowZzzGameSession.CurrentRound` の N>2 対応(Phase 3 候補)
- `turn-state.md` の「関連」に「DrowZzz での用語解釈は ADR-0006 §7 参照」を相互参照追加(`docs/todo.md` に登録予定)

これらが M2 着手前に消化されない場合は `docs/todo.md` への移行を検討する(ADR-0003 の運用)。

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — Domain ゲーム非依存原則 / TurnState の Phase 1 仕様
- 前提: [ADR-0003 TODO 運用](0003-todo-operations.md) — 後追い chore の追跡
- 前提: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — `record + init + with` の前提
- 前提: [ADR-0005 Phase 2 Roadmap](0005-phase2-roadmap-drowzzz.md) — M1〜M5 のスコープ確定、本 ADR は M1 詳細
- 関連: [`docs/specs/domain/game/turn-state.md`](../specs/domain/game/turn-state.md) — Phase 1 TurnState 仕様(本 ADR §7 で用語整理)
- 関連: [`docs/specs/domain/configuration/game-config.md`](../specs/domain/configuration/game-config.md) — Phase 2 で `FdpPool` / `MaxRoundNumber` 追加予定(本 ADR §1.4)
- 関連規約: [`CLAUDE.md`](../../CLAUDE.md) §5 アーキテクチャ依存ルール / §6 テスト方針 / §11 ADR 運用
- 後続: M1-PR1 〜 M1-PR7(本 ADR Implementation Notes §M1 着手 PR 群)
- 後続: ADR-0007(M2 詳細、起票予定)
