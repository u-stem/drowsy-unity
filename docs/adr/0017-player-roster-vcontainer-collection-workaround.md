# ADR-0017: PlayerRoster wrapper 型導入 — VContainer collection resolution と IReadOnlyList<T> 予約型問題

- Status: Accepted
- Date: 2026-05-16
- Decider: -

---

## Context

M5-PR4(PR #89)で新規対戦の `players` を Bootstrap が構築し VContainer 経由で `DrowZzzGamePresenter` に注入する設計を採用した。具体的には:

```csharp
// ProjectLifetimeScope.Configure(M5-PR4 時点)
builder.RegisterInstance<IReadOnlyList<PlayerId>>(BuildPlayers());  // = new[] { p1, p2 }

// DrowZzzGamePresenter ctor(M5-PR4 時点)
public DrowZzzGamePresenter(
    StartGameUseCase startGameUseCase,
    ...,
    IReadOnlyList<PlayerId> players,
    Pile initialDeck)
```

M5-PR1〜PR7 の EditMode テストでは Presenter を直接 `new DrowZzzGamePresenter(..., ValidPlayers(), ...)` で構築しており VContainer を通さなかった。M5 UI 実機検証(2026-05-16)で初めて Unity Play モードに乗せた際、以下のランタイムエラーが発生した:

```
[DrowZzzGamePresenter] BootAsync failed: System.ArgumentException: players は 1 人以上必要です
Parameter name: players
  at Drowsy.Application.Games.DrowZzz.StartGameUseCase.ValidateArguments (...)
  at Drowsy.Application.Games.DrowZzz.StartGameUseCase.Execute (...)
  at Drowsy.Presentation.Games.DrowZzz.DrowZzzGamePresenter.BootAsync (CancellationToken ct)
```

調査の結果、VContainer 1.17.0 のソースから、`IReadOnlyList<T>` と `IEnumerable<T>` は **collection 専用予約型**として扱われていることが分かった。関連箇所:

- `Runtime/Internal/InstanceProviders/CollectionInstanceProvider.cs:22`(`Match` メソッド):
  ```csharp
  public static bool Match(Type openGenericType) =>
      openGenericType == typeof(IEnumerable<>) ||
      openGenericType == typeof(IReadOnlyList<>);
  ```
- `Runtime/Registry.cs:118-122`(`TryGet` の閉ジェネリック解決経路):`hashTable` 直接 lookup が失敗した場合、`TryGetClosedGenericRegistration`(開ジェネリック登録)→ **`TryFallbackToSingleElementCollection`**(`CollectionInstanceProvider.Match` で発動)→ `TryFallbackToContainerLocal` の順でフォールバック。`TryFallbackToSingleElementCollection` は `elementType`(本件では `PlayerId`)の registration を集めて collection を構築し、要素 0 件なら空配列を返す。
- `Runtime/Registry.cs:51-87`(`AddToBuildBuffer`):同じ `service` 型に対して 2 件目の registration が発行されると、既存 registration を **`CollectionInstanceProvider` に昇格**させる(`collectionKey` の 2 度目検出経路)。

本件で実機エラーが起きた根本メカニズムには複数の可能性が残っており、本 ADR では完全特定には踏み込まない:

- 仮説 A:`AddToBuildBuffer` の昇格経路で何らかの理由(`Configure` の多重実行 / 親子スコープ間の登録重複等)により `IReadOnlyList<PlayerId>` 型が 2 件目扱いされてコレクション化された
- 仮説 B:`hashTable` 直接 lookup が成功せず `TryFallbackToSingleElementCollection` に到達し、`PlayerId` 単独 registration が無いため空 collection が返った

観測された事実は「`RegisterInstance<IReadOnlyList<PlayerId>>(BuildPlayers())` で `BuildPlayers()` が 2 要素を返しているにもかかわらず、Presenter ctor が受け取った `players` は `Count == 0` だった」のみ。本件の対処方針(wrapper 型導入)はいずれの仮説でも有効なため、根本原因の完全特定は別途調査(再現テスト・VContainer issue 報告等)に委ね、本 ADR では「**`IReadOnlyList<T>` 系の VContainer 予約型に対する `RegisterInstance` は安全でない**」という結論で対処を確定する。

これは M5-PR4 の設計時に VContainer 仕様の未確認と、Unity Play モードでの統合検証を行わず EditMode テストのみで完成扱いとしたことが重なって発覚した設計バグである。再発防止のため:

1. 即時の対処(本 ADR で確定する実装変更)
2. 同種の落とし穴の知見記録(本 ADR 自体が SSOT)
3. 機械検知 / テスト戦略の補強(Phase 3 で別途検討、本 ADR では Out of Scope)

を分けて整理する。

### 検討した修正案

| 案 | 内容 | 評価 |
| ---- | ---- | ---- |
| A. **`PlayerRoster` wrapper record 導入**(本 ADR の採択案) | Application 層に `sealed record PlayerRoster(IReadOnlyList<PlayerId> Players)` を新設し、Bootstrap は `RegisterInstance(new PlayerRoster(BuildPlayers()))` で登録、Presenter ctor は `PlayerRoster roster` を受け取る | VContainer collection rule から完全に外れる(`PlayerRoster` は予約型ではない)。設計上 semantic 価値あり(対戦参加者の集合を型として明示)。Phase 3 で N>2 / プレイヤー名入力 UI 拡張時の自然な拡張点。Presenter ctor 引数型変更 + Presenter test 修正の影響範囲が中規模だが許容できる |
| B. `List<PlayerId>` 型で登録 | `RegisterInstance<List<PlayerId>>(new List<PlayerId>(BuildPlayers()))` で登録、Presenter ctor も `List<PlayerId>` で受ける | `List<T>` は CollectionInstanceProvider.Match に含まれないため動作する最小変更だが、mutable コレクション型を ctor 引数に持つのは設計の美しさを損なう(Presenter は変更しないが型契約上は変更を許容している)。Phase 3 拡張時に再リファクタリング必要 |
| C. `Register<PlayerId>` 個別登録 + collection rule に意図的に乗せる | `builder.Register<PlayerId>(_ => PlayerId.Of("p1"), Singleton); builder.Register<PlayerId>(_ => PlayerId.Of("p2"), Singleton);` を ProjectLifetimeScope で 2 回呼ぶ | Presenter ctor 変更不要だが、プレイヤー数がハードコードされ、Phase 3 でプレイヤー名入力 UI を導入するときに ProjectLifetimeScope に動的登録ロジックを書く必要があり複雑化。各 `PlayerId` が個別の Singleton になり VContainer 内のオブジェクト数が増える |

案 A を採択する。理由は表の「評価」列の通り、設計上の美しさと拡張性を両立しつつ、影響範囲が許容できる規模に収まるため。

---

## Decision

### 1. Application 層に `PlayerRoster` wrapper record を新設する

配置: `Assets/_Project/Scripts/Application/Games/DrowZzz/PlayerRoster.cs`
namespace: `Drowsy.Application.Games.DrowZzz`

```csharp
public sealed record PlayerRoster
{
    public IReadOnlyList<PlayerId> Players { get; init; }

    public PlayerRoster(IReadOnlyList<PlayerId> players)
    {
        Players = players ?? throw new ArgumentNullException(nameof(players));
        if (Players.Count == 0)
        {
            throw new ArgumentException("players は 1 人以上必要です", nameof(players));
        }
    }
}
```

- `sealed record`:`Pile` / `DdpPool` は値同値性のため `sealed class : IEquatable` だが、`PlayerRoster` は DI wrapper で「Bootstrap で 1 つ作って共有」する利用のため `record` の参照同値で十分(`IReadOnlyList<T>` プロパティの内容比較は不要)
- ctor で **null + empty** を検証(Parse-don't-validate、`StartGameUseCase.ValidateArguments` の二重検証は許容)
- `init` setter:`record` の `with` semantics 整合性のため(M5 範囲では `with` 利用なし、Phase 3 拡張時の自然な拡張点)

### 2. ProjectLifetimeScope の登録を `PlayerRoster` 経由に変更する

```csharp
// 変更前
builder.RegisterInstance<IReadOnlyList<PlayerId>>(BuildPlayers());

// 変更後
builder.RegisterInstance(new PlayerRoster(BuildPlayers()));
```

`BuildPlayers()` の戻り値型 `IReadOnlyList<PlayerId>` はそのまま維持(`PlayerRoster` ctor がこの型を受け取るため)。

### 3. DrowZzzGamePresenter ctor の引数型を `PlayerRoster` に変更する

```csharp
// 変更前
public DrowZzzGamePresenter(
    ..., IReadOnlyList<PlayerId> players, Pile initialDeck)
{
    _players = players ?? throw new ArgumentNullException(nameof(players));
    _initialDeck = initialDeck ?? throw new ArgumentNullException(nameof(initialDeck));
}

// 変更後
public DrowZzzGamePresenter(
    ..., PlayerRoster roster, Pile initialDeck)
{
    if (roster is null) throw new ArgumentNullException(nameof(roster));
    _players = roster.Players;  // PlayerRoster ctor で null + empty 検証済
    _initialDeck = initialDeck ?? throw new ArgumentNullException(nameof(initialDeck));
}
```

`_players` フィールド型は変更しない(内部表現は引き続き `IReadOnlyList<PlayerId>`)。`BootAsync` 内の `_startGameUseCase.Execute(_players, _initialDeck)` 呼び出しも変更不要。

### 4. EARS 仕様の追加と Presenter spec の更新

新規モジュール `ROSTER`(PlayerRoster 専用)で 4 件の要件を追加:

| 要件 ID | 種別 | 内容 |
| ---- | ---- | ---- |
| ROSTER-001 | Ubiquitous | The `PlayerRoster` shall be a sealed value type (record) holding `IReadOnlyList<PlayerId>` |
| ROSTER-002 | Unwanted | If the ctor is called with `players = null`, then `ArgumentNullException` shall be thrown |
| ROSTER-003 | Unwanted | If the ctor is called with `players.Count == 0`, then `ArgumentException` shall be thrown |
| ROSTER-004 | Event-driven | When ctor is called with non-empty `players`, the `PlayerRoster` shall expose them via `Players` in original order |

配置: `docs/specs/application/games/drowzzz/player-roster.md` + `.feature`

既存 `presenter-skeleton.md` の PRES-014(`players = null` → `ArgumentNullException`)は **PRES-014 を維持しつつ意味を「`roster = null` → `ArgumentNullException`」に置き換える**(EARS 要件 ID は安定性を最優先するため番号は変えない、Presenter ctor のシグネチャ変更を反映するだけの redefine)。

### 5. ADR-0016 §2 登録対象表 + §3.2 Presenter 説明の追記

ADR-0016 §2 の players 行に `PlayerRoster` 経由である旨と本 ADR 参照を追記。§3.2 の Presenter ctor 引数列も同様に更新。M5-PR4 完成記録には「実装後発覚: VContainer collection rule、ADR-0017 で修正」の注記を追加(M5-PR4 自体の Status は変えない、後続 fix を Related で接続するのみ)。

---

## Consequences

### Positive

- VContainer collection rule から確実に外れる(`PlayerRoster` は予約型ではない)
- 設計上 semantic 価値:対戦参加者の集合を型として明示し、IReadOnlyList との取り違いを構造的に防ぐ
- Phase 3 拡張時の自然な拡張点:N>2 / プレイヤー名入力 UI / リトライ機構などで `PlayerRoster` の責務を拡張可能(`record` の `with` で immutable 更新)
- 再発防止の知見蓄積:VContainer 1.x の `IEnumerable<T>` / `IReadOnlyList<T>` 予約型問題を ADR として記録し、今後の DI 統合時の参照点を作る
- 既存 `StartGameUseCase.Execute(IReadOnlyList<PlayerId>, Pile)` API は変更不要(Application 層 UseCase は VContainer に依存しない設計、Bootstrap/Presenter 境界のみで `PlayerRoster` を扱う)

### Negative

- ProjectLifetimeScope / DrowZzzGamePresenter ctor / DrowZzzGamePresenterTests の修正が必要(影響: Bootstrap 1 行 / Presenter ctor 1 引数 / Presenter test の `ValidPlayers()` → `ValidRoster()` 名称変更 + 全 9 箇所の引数差し替え)
- ADR-0016 §2 / §3.2 + presenter-skeleton.md(spec ファイル)/ presenter-skeleton.feature(Gherkin)の更新が必要
- EARS 新規モジュール `ROSTER` の追加(`docs/specs/application/games/drowzzz/player-roster.md` + `.feature` 新規)、traceability 担保のための NUnit `[Property("Requirement", "ROSTER-00X")]` を 3 件追加(ROSTER-001 は Ubiquitous で免除)
- M5-PR4(PR #89)の設計が部分的に不適切だったという事実が記録される(これは透明性として Positive 側面)
- Phase 3 で同種の落とし穴(VContainer collection rule や別の DI 仕様)が再発しないよう、機械検知 / テスト戦略の補強を検討するが、本 ADR では Out of Scope(別 ADR / TODO)

### 機械検知 / 統合テストの今後

本件は「ライブラリ仕様の暗黙の予約型」が原因で EditMode 単体テストでは検出できなかった。将来の Phase 3 以降で検討すべき再発防止策(本 ADR では決定せず TODO に切り出す):

- Bootstrap LifetimeScope の構築 → Resolve を Unity Test Runner の PlayMode テストで検証する仕組み(VContainer の `IContainerBuilder.Build` を呼んで `Container.Resolve<DrowZzzGamePresenter>()` する smoke test)
- `RegisterInstance<IReadOnlyList<>>` / `RegisterInstance<IEnumerable<>>` のパターンを禁止する Roslyn Analyzer(カスタム、Phase 3 のテスト戦略強化と合わせて検討)

`docs/todo.md` にエントリを追加して追跡する。

---

## Related

- ADR-0016 §2「登録対象と寿命(詳細)」 — players 登録行に本 ADR を Related として追記(該当行を `PlayerRoster` 経由に更新)
- ADR-0016 §3.2「Presenter」 — Presenter ctor 引数列を `PlayerRoster` に更新
- ADR-0016 §11「PR 分割計画」 — M5-PR4 完成記録に「実装後発覚: VContainer collection rule、ADR-0017 で修正」を注記
- ADR-0006「M1 詳細 — Application interfaces」 — `IGameAction` / `IGameRule` / `ICardCatalog` の汎用 interface 設計、本 ADR の PlayerRoster は Application 層の wrapper として整合
- VContainer 1.17.0 ソース: `Runtime/Internal/InstanceProviders/CollectionInstanceProvider.cs:22`(`Match` メソッド)、`Runtime/Registry.cs:185`(closed generic 解決時の優先制御)
- 発覚 PR: M5-PR4 = PR #89(`feat(presentation): Presenter Handler 3 種 + BootAsync 新規対戦経路 + Render 本実装`)、修正 PR: 本 ADR と同 PR(`fix/m5-player-roster-vcontainer-collection`)
- 関連 spec(本 ADR と同 PR で追加): `docs/specs/application/games/drowzzz/player-roster.md` + `.feature`(EARS / Gherkin)
