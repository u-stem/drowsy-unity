# ADR-0016: M5 詳細 — Bootstrap / Presentation 統合(VContainer + UniTask + R3)

| 項目 | 値 |
| ---- | ---- |
| Status | Accepted |
| Date | 2026-05-13 |
| Decider | プロジェクトオーナー |

## Context

ADR-0005 で Phase 2 のロードマップを M1〜M5 に分割し、M1(ADR-0006)/ M2(ADR-0007 / 0008 / 0009)/ M3(ADR-0010 / 0011)/ M4(ADR-0012、PR1〜PR6 完成、PR7 未着手)を順次完成させてきた。本 ADR は **M5(Bootstrap + Presentation 最小)の詳細設計** を確定し、Phase 2 完了に必要な縦串を閉じる役割を持つ。

本 ADR は **M4-PR7(M4 完成 PR)着手前(M4-PR6 完成後)に起票** する。M4-PR7 と M5 着手は依存関係上は直列順序(本 ADR §11 M5-PR1 着手前提条件)だが、M5 の枠組みを先に固めておくことで M4-PR7 の Designer ワークフロー実証が M5 側の DI 要件と整合するかを早期に検証できる。

採用ライブラリのバージョン:**VContainer 1.17.0 / UniTask 2.5.10 / R3 1.3.0**(CLAUDE.md §4 に Single Source of Truth、本 ADR は §4 を参照、バージョン更新時は §4 を更新する)。

ADR-0005 で確定している M5 の完了基準:

> Unity Play モードで DrowZzz が遊べる(最小 UI / コンソール出力可)、VContainer LifetimeScope + UniTask + R3 を実利用

そして ADR-0005 §「Phase 2 完了の最小定義」:

> - N=2 で起動できる
> - ルールメモの主要部分が動作する(M1 + M2 + M3 完了相当)
> - 勝敗判定が出る
> - 永続化が動く(M4 完了相当)
> - Unity Play モードで人間が操作できる(M5 完了相当、最小 UI で OK)

### 既存基盤の現況(M5 着手前)

| 既存資産 | 出典 | M5 での扱い |
| ---- | ---- | ---- |
| `IRandomSource` (Domain) | Phase 1 | DI 登録対象、Project スコープ Singleton |
| `IGameConfig` (Domain.Configuration) | ADR-0006 §1.4 + 拡張 | SO 実装 `DrowZzzGameConfigAsset` を Inspector 注入 |
| `ICardCatalog<IEffect>` (Application) | ADR-0007 §3 | `ScriptableObjectCardCatalog` を Inspector 注入(ADR-0012 §3) |
| `IUserSettings` (Domain.Configuration) + R3 Observable | ADR-0012 §8 / M4-PR6 | DI 登録対象、Project スコープ Singleton、UI バインディング |
| `DrowZzzGameSessionSerializer` (Infrastructure.Persistence) | M4-PR5 | DI 登録、Game スコープ Singleton |
| `DrowZzzRule` / `StartGameUseCase` / `ApplyActionUseCase` (Application) | M1-PR1〜PR7 / ADR-0014 | DI 登録、Game スコープ |
| `Drowsy.Bootstrap.asmdef` | Phase 0 | 既存:`Drowsy.Domain` / `Drowsy.Application` / `Drowsy.Infrastructure` / `Drowsy.Presentation` / `UniTask` / `VContainer` / `R3.Unity` を references 済、`.cs` 0 ファイル |
| `Drowsy.Presentation.asmdef` | Phase 0 | 既存:`Drowsy.Domain` / `Drowsy.Application` / `UniTask` / `VContainer` / `R3.Unity` を references 済、`.cs` 0 ファイル |

ADR-0014 で `StartGameUseCase` の constructor は 2 引数 `(IRandomSource, IGameConfig)` に縮小済み、ADR-0015 で NRT 不採用を確定済み(再評価条件第 1 項に「M5 Bootstrap で外部 API クライアント / シリアライザ等の null 多発コード」を明示)。M4-PR6 完成記録で `Drowsy.Domain.asmdef` に `R3.dll` を `precompiledReferences` 追加済み、`PlayerPrefsUserSettings` が `IDisposable` を実装。

### M5 着手前に必要な意思決定

ADR-0005 / 0006 が「M5 で VContainer LifetimeScope を導入」「ロジック先行で Presentation は M5 まで保留」と要点のみ確定しているため、Bootstrap / Presentation 実装に踏み込むには下記の確定が必要:

1. **VContainer LifetimeScope の階層構造**(Project / Game / Scene)と各層に登録する型・寿命
2. **Presentation アーキテクチャ**(MVP / MVVM / direct binding のいずれを Pure C# Presenter で採用するか)
3. **最小 UI フレームワーク**(UI Toolkit / uGUI / IMGUI)
4. **UniTask 1.17.0 + R3 1.3.0 の利用範囲**(I/O 非同期 / 状態購読 / 入力イベント)
5. **シーン構成**(Boot.unity / Main.unity 分離 vs 単一)
6. **プレイモデル**(N=2 ホットシート vs CPU 対戦 vs オラクル可視デバッグ)
7. **Session 自動セーブ / 自動復元のタイミング**
8. **SO Asset 配置先と Bootstrap への注入経路**(Inspector `[SerializeField]` / Resources / AssetReference)
9. **PR 分割計画**(M5-PR1〜PRn)と PR 間の依存関係
10. **M4-PR7(M4 完成 PR)との順序関係**(M4-PR7 で Designer ワークフローを実証してから M5-PR1 か、M5 内で並行か)
11. **NRT 再評価ポイント**(ADR-0015 §「再評価条件」第 1 項の充足を M5 のどこで判定するか)
12. **テストスコープ**(Presenter Pure C# 単体テストの asmdef 構成、Bootstrap LifetimeScope 単体テストの可否)

これらが未決のまま個別 PR を開始すると ADR-0006 / 0014 / 0012 と同様に「JIT で都度確認 + breaking change の連鎖」が発生するため、本 ADR で枠組みを固める。

### JIT 共有された方針(プロジェクトオーナーから 2026-05-13 受領)

| 項目 | 確定内容 |
| ---- | ---- |
| 最小 UI フレームワーク | **UI Toolkit**(Unity 6 / WebGL の現行デファクト、UIDocument を Bootstrap が `[SerializeField]` で参照し、`VisualElement query` で UI 要素を取得、View → C# event で Presenter に通知、Presenter は Pure C# で `Observable<DrowZzzGameSession>`(内部 `Subject<T>`)を公開、本 ADR §3.2 で NRT 不採用方針との整合を確定) |
| プレイモデル | **N=2 ホットシート(同一端末 2 人交代制)のみ**(CPU 対戦 / Network は Phase 3 で別 ADR、隠し情報は View 側で現プレイヤー視点フィルタを行う) |

その他項目は本 ADR Decision §以降で「初期推奨」として明示し、M5-PR1 以降の各 PR 着手時に都度 JIT 確認する運用(ADR-0007 / 0010 / 0012 で確立済の「初期推奨案を ADR に書く」パターン継承)。

## Decision

### 1. VContainer LifetimeScope 階層構造

**2 階層構成**(Project + Game の 2 段)を採用する。

```
ProjectLifetimeScope (DontDestroyOnLoad / Application 寿命)
  ├─ Singleton:
  │   - IRandomSource = XorShiftRandom
  │   - IGameConfig   = DrowZzzGameConfigAsset (Inspector 注入)
  │   - ICardCatalog<IEffect> = ScriptableObjectCardCatalog (Inspector 注入)
  │   - IUserSettings = PlayerPrefsUserSettings (Application スコープ Disposable)
  │   - IDrowZzzGameSessionSerializer = DrowZzzGameSessionSerializer
  │   - DrowZzzRule (stateless)
  │
  └─ GameLifetimeScope (1 対戦ごとに Create / Dispose)
      ├─ Singleton (Game 寿命):
      │   - StartGameUseCase
      │   - ApplyActionUseCase
      │   - DrowZzzGamePresenter (Pure C#)
      │   - Observable<DrowZzzGameSession> SessionStream (Subject<T> ベース、Boot 完了後 OnNext)
      │
      └─ View (MonoBehaviour) は GameLifetimeScope の AutoInjectGameObject から参照
```

採用理由:
- ADR-0005 §「Phase 2 完了の最小定義」が「N=2 で起動できる」までを範囲とし、Scene 切替 / リプレイモード切替は Phase 3 範疇のため、Scene スコープを独立に切る必要が薄い
- 3 階層(Project / Game / Scene)は VContainer の理想形だが、M5 規模(1 シーン構成)では過剰、Phase 3 の Scene 拡張時に GameLifetimeScope を分割する余地は残す
- Project スコープを `DontDestroyOnLoad` にしておくことで、Phase 3 の Scene 切替時にも Settings / Catalog / Serializer を再初期化せずに保てる

**Game スコープの生成タイミング**:アプリ起動直後 1 回(M5 範囲では対戦 1 回限定、リスタート / リトライ UI なし)。Phase 3 で「新規対戦ボタン」を追加する際に `GameLifetimeScope.Create()` / `Dispose()` の繰り返し利用に拡張。

### 2. 登録対象と寿命(詳細)

| 型 | スコープ | 寿命 | 登録方法 | 備考 |
| ---- | ---- | ---- | ---- | ---- |
| `IRandomSource` | Project | Singleton | `RegisterInstance(new XorShiftRandom(seed))` | seed は M5 では時刻ベース、Phase 3 でリプレイ復元用シード保存検討 |
| `IGameConfig` | Project | Singleton | `RegisterInstance<IGameConfig>(_gameConfig)` | M4-PR1 の `DrowZzzGameConfigAsset`(ScriptableObject)を `[SerializeField]` で受けて `RegisterInstance` 注入(M4-PR7 で配置経路確立)。`RegisterComponentInHierarchy` は MonoBehaviour 検索 API のため SO には適用不可 |
| `ICardCatalog<IEffect>` | Project | Singleton | `RegisterInstance<ICardCatalog<IEffect>>(_cardCatalog)` | `ScriptableObjectCardCatalog`(SO)を `[SerializeField]` で受ける(M4-PR7) |
| `IUserSettings` | Project | Singleton (Disposable) | `Register<IUserSettings, PlayerPrefsUserSettings>(Lifetime.Singleton)` | `PlayerPrefsUserSettings.Dispose` は VContainer の `IObjectResolver.Dispose` で自動呼び出し(`IDisposable` 検出) |
| `IDrowZzzGameSessionSerializer` | Project | Singleton | `Register<IDrowZzzGameSessionSerializer, DrowZzzGameSessionSerializer>(Lifetime.Singleton)` | M4-PR5 の `DrowZzzGameSessionSerializer` は stateless、interface 抽出が必要(本 ADR §5.2 で確定) |
| `string`(セーブパス、Presenter ctor `string savePath` 引数として注入)| Project | Singleton | `RegisterInstance(DrowZzzGameSessionSerializer.DefaultSavePath())` | `DefaultSavePath` は `static` のため interface に含められず(W-1 / W-5 反映)、Project Singleton として string を登録。`Application.persistentDataPath` 呼び出しは Configure 内のメインスレッドで実行され、ワーカースレッドからの参照を回避。M5 範囲で string 登録は本 1 個のみ、型 `string` で衝突なく Resolve 可能。Phase 3 で「multi-slot save / log path 等」で複数 string 登録が必要になった時点で型別の wrapper(`SavePath` record 等)導入を再評価 |
| `EffectInterpreter` | Project | Singleton | `Register<EffectInterpreter>(Lifetime.Singleton)` | `DrowZzzRule` ctor の第 2 引数。VContainer は未登録の具象型を自動解決しないため明示登録が必要(M5-PR3 で実装、code-reviewer W-1 反映で本表に追記)|
| `DrowZzzRule` | Project | Singleton | `Register<DrowZzzRule>(Lifetime.Singleton)` | stateless、ctor で `ICardCatalog<IEffect>` + `EffectInterpreter` を要求 |
| `StartGameUseCase` | Game | Singleton | `Register<StartGameUseCase>(Lifetime.Singleton)` | ADR-0014 で 2 引数 ctor 化済、Game スコープに置くことで「対戦 1 回 1 instance」を担保 |
| `ApplyActionUseCase` | Game | Singleton | `Register<ApplyActionUseCase>(Lifetime.Singleton)` | stateless、`DrowZzzRule` を constructor 注入 |
| `DrowZzzGamePresenter` | Game | Singleton | `Register<DrowZzzGamePresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()` | `IStartable` 実装で Boot 時に自動起動、`IDisposable` 実装で Game スコープ Dispose 時に CompositeDisposable / CTS / Subject 解放(本 ADR §3.2)。`AsSelf()` は単体テスト / View 参照で具象型 Resolve 経路を両立 |
| `IDrowZzzGameView` 実装(View MonoBehaviour) | Game | Singleton | `RegisterComponentInHierarchy<DrowZzzGameView>().AsImplementedInterfaces()` | UIDocument を持つ MonoBehaviour。Presenter ctor の `IDrowZzzGameView` 引数として Container から解決される(依存方向は Presenter → View、View 側に `[Inject]` は不要、code-reviewer W-3 反映で訂正)|
| `PlayerRoster` (新規対戦のプレイヤー roster) | Project | Singleton | `RegisterInstance(new PlayerRoster(BuildPlayers()))` | **ADR-0017**:M5-PR4 当初は `RegisterInstance<IReadOnlyList<PlayerId>>(BuildPlayers())` だったが、VContainer 1.x の `CollectionInstanceProvider.Match` が `IReadOnlyList<T>` を予約型として扱い `RegisterInstance` を上書きするため、wrapper record (`PlayerRoster`) を Application 層に新設して回避。Presenter ctor の 7 番目の引数も `PlayerRoster roster` に変更 |
| `Pile` (新規対戦の initialDeck) | Project | Singleton | `RegisterInstance(BuildInitialDeck(_cardCatalog))` | M5-PR4 で追加。Presenter ctor の 8 番目の引数。Domain 型 `Pile` は VContainer の予約型ではないため `RegisterInstance` で直接登録可(`PlayerRoster` のような wrapper 不要)|

**`StartGameUseCase` を Singleton(Transient ではなく)で登録する理由**:M5 範囲では対戦 1 回限定で、ctor で受ける `IRandomSource` は Project Singleton(=各対戦で同一 instance を共有)。1 対戦内で `Execute()` は 1 回しか呼ばれないため Singleton と Transient で動作差なし、Singleton のほうがゲーム状態の単一情報源管理が明示的になる。Phase 3 で「新規対戦」を導入する場合は GameLifetimeScope ごと再生成するため、Singleton 寿命は GameLifetimeScope に従う形で自然にリセットされる。

**`IDrowZzzGameSessionSerializer` interface 抽出が必要な理由**:M4-PR5 の `DrowZzzGameSessionSerializer` は具象クラスで実装されている。Bootstrap で DI 経由テストを行うため、本 ADR §5.2 で interface を追加(`Drowsy.Application.Persistence` namespace)、`Drowsy.Infrastructure.Persistence.DrowZzzGameSessionSerializer` が実装、という Ports & Adapters 構造に整える。

### 3. Presentation アーキテクチャ:MVP(Model-View-Presenter)

**MVP パターン採用**。理由:

| 案 | 結論 | 根拠 |
| ---- | ---- | ---- |
| **MVP**(View interface + Pure C# Presenter) | **採用** | ADR-0006 §4「Pure C# 哲学」を Presentation 側でも貫徹。Presenter を `Drowsy.Presentation.Tests` で NUnit 単体テスト可能。View 実装差し替え容易(将来 uGUI / IMGUI に切替可能) |
| MVVM(R3 ReactiveProperty を View が直接 Bind) | 不採用 | UI Toolkit `DataBinding` は Unity 2023+ に存在するが Unity 6 で安定性検証コストあり、Pure C# 単体テスト性が下がる |
| Direct binding(MonoBehaviour が UseCase を直接呼ぶ) | 不採用 | ADR-0006 §4 の Pure C# 原則違反、テスト不能、依存方向違反(Presentation Internal が UseCase を密結合) |

#### 3.1 View interface

```csharp
// 配置: Assets/_Project/Scripts/Presentation/Games/DrowZzz/IDrowZzzGameView.cs
namespace Drowsy.Presentation.Games.DrowZzz;

public interface IDrowZzzGameView
{
    // 出力:Presenter → View(状態反映)
    void Render(DrowZzzGameSession session);
    void RenderOutcome(GameOutcome outcome);

    // 入力:View → Presenter(C# event で通知)
    event Action OnDrawClicked;
    event Action<CardId> OnPlayClicked;
    event Action OnEndTurnClicked;

    // 拡張(M5-PR4 以降で必要に応じて追加):
    // event Action<DrowZzzAction> OnActionRequested; など
}
```

`event` 採用理由:R3 `Subject<T>` でも可だが、View → Presenter の片方向通知に過剰、View MonoBehaviour 側のメモリリーク防止責務を `event += / -=` の対称運用で明示する方が Unity 文化に整合(`MonoBehaviour.OnDestroy` で `event -=` を行う)。

**Domain 型(`CardId` / `DrowZzzGameSession` / `GameOutcome`)を View interface が直接受け取る件**:ADR-0006 §4「View に Domain エンティティを直接バインドせず Presenter / DTO 経由を推奨」との関係で、本 M5 範囲では **DTO 化を行わず Domain 型を View interface で受ける** ことを許容範囲とする。理由:

- M5 完了基準(ADR-0005 §7)は「最小 UI / コンソール出力可」で、DTO レイヤを通す価値より単純さが優先
- 現状の View は MonoBehaviour 1 クラス(`DrowZzzGameView`)で、内部で `CardId` を UI 表示用の文字列等に変換する手間が少ない
- Phase 3 で View が複雑化(複数 Panel / Animation / Localization)した時点で DTO 化を別 ADR で評価し、その時点で本 ADR を Superseded by NNNN で覆す余地を残す

#### 3.2 Presenter

```csharp
// 配置: Assets/_Project/Scripts/Presentation/Games/DrowZzz/DrowZzzGamePresenter.cs
namespace Drowsy.Presentation.Games.DrowZzz;

public sealed class DrowZzzGamePresenter : IStartable, IDisposable
{
    private readonly StartGameUseCase _startGameUseCase;
    private readonly ApplyActionUseCase _applyActionUseCase;
    private readonly IDrowZzzGameView _view;
    private readonly IDrowZzzGameSessionSerializer _serializer;
    private readonly IUserSettings _userSettings;
    private readonly string _savePath;
    // _players の型は IReadOnlyList<PlayerId> のまま(内部表現)。ctor 引数だけ PlayerRoster wrapper に
    // 差し替え(ADR-0017、VContainer 1.x の CollectionInstanceProvider が IReadOnlyList<T> を予約型として
    // 扱う問題への対処)。
    private readonly IReadOnlyList<PlayerId> _players;   // M5-PR4 で追加(新規対戦の players、ADR-0017 で ctor 経路変更)
    private readonly Pile _initialDeck;                  // M5-PR4 で追加(新規対戦の initialDeck)
    private readonly Subject<DrowZzzGameSession> _sessionSubject = new();
    private readonly CompositeDisposable _disposables = new();
    private readonly CancellationTokenSource _cts = new();

    // Boot 完了後にのみ OnNext が発火する Observable(View はここを Subscribe)
    public Observable<DrowZzzGameSession> SessionStream => _sessionSubject;

    // 現セッション(Boot 完了前 / リプレイ復元失敗時のみ null、外部からは IsReady プロパティで識別)
    private DrowZzzGameSession _current;
    public DrowZzzGameSession Current => _current;
    public bool IsReady => _current is not null;

    // M5-PR4 で 6 引数 → 8 引数化(players / initialDeck 追加、ADR-0016 §3.2 line 238 の TBD 解消、
    // M5-PR4 着手時 JIT 確定 2026-05-14)。ADR-0017 で 7 番目の引数を IReadOnlyList<PlayerId> →
    // PlayerRoster に差し替え(VContainer 1.x の予約型回避)。
    public DrowZzzGamePresenter(
        StartGameUseCase startGameUseCase,
        ApplyActionUseCase applyActionUseCase,
        IDrowZzzGameView view,
        IDrowZzzGameSessionSerializer serializer,
        IUserSettings userSettings,
        string savePath,
        PlayerRoster roster,
        Pile initialDeck)
    {
        _startGameUseCase = startGameUseCase ?? throw new ArgumentNullException(nameof(startGameUseCase));
        _applyActionUseCase = applyActionUseCase ?? throw new ArgumentNullException(nameof(applyActionUseCase));
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
        // savePath は null と空白を分けて防御する(null → ArgumentNullException、空白のみ → ArgumentException)。
        if (savePath is null) throw new ArgumentNullException(nameof(savePath));
        if (string.IsNullOrWhiteSpace(savePath))
            throw new ArgumentException("savePath は空・空白のみにできません", nameof(savePath));
        _savePath = savePath;
        // PlayerRoster ctor で null + 空配列検証済(ROSTER-002 / ROSTER-003、ADR-0017)。
        if (roster is null) throw new ArgumentNullException(nameof(roster));
        _players = roster.Players;
        _initialDeck = initialDeck ?? throw new ArgumentNullException(nameof(initialDeck));
    }

    // VContainer の IStartable.Start() は Container 構築後に 1 回のみ呼ばれる(VContainer 仕様)
    // ため、本メソッド内での event += / Subscribe は多重登録の懸念がない。
    public void Start()
    {
        // 入力配線
        _view.OnDrawClicked += HandleDrawClicked;
        _view.OnPlayClicked += HandlePlayClicked;
        _view.OnEndTurnClicked += HandleEndTurnClicked;

        // 状態 → View(Boot 完了後の OnNext のみ View に伝搬)
        _sessionSubject.Subscribe(s => _view.Render(s)).AddTo(_disposables);

        // 起動シーケンス(UniTask、Forget の内側で try/catch + Cancellation Token 連動)
        BootAsync(_cts.Token).Forget();
    }

    private async UniTaskVoid BootAsync(CancellationToken ct)
    {
        try
        {
            // セーブファイル存在判定 → 復元 / 新規対戦
            DrowZzzGameSession session;
            try
            {
                session = await _serializer.LoadAsync(_savePath, ct);
            }
            catch (FileNotFoundException)
            {
                // 初回起動 or 永続化ファイル削除済 → 新規対戦(M5-PR4 で本実装、ctor 注入の players / initialDeck を使用)
                session = _startGameUseCase.Execute(_players, _initialDeck);
            }
            _current = session;
            _sessionSubject.OnNext(session);
        }
        catch (OperationCanceledException)
        {
            // Presenter Dispose 中、何もしない
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DrowZzzGamePresenter] BootAsync failed: {ex}");
        }
    }

    // 以下、HandleDrawClicked / HandlePlayClicked / HandleEndTurnClicked
    // (M5-PR2〜PR4 で実装、各 Handler は ApplyActionUseCase.Execute を呼んで _current 更新 + _sessionSubject.OnNext)

    public void Dispose()
    {
        _view.OnDrawClicked -= HandleDrawClicked;
        _view.OnPlayClicked -= HandlePlayClicked;
        _view.OnEndTurnClicked -= HandleEndTurnClicked;
        _cts.Cancel();
        _cts.Dispose();
        _disposables.Dispose();
        _sessionSubject.Dispose();
    }
}
```

- **`IStartable`(VContainer)** 実装で `Start()` を Boot 時に **VContainer が 1 回のみ呼ぶ保証**(VContainer 1.17.0 仕様、`AutoInjectGameObject` 経由でも同様)。MonoBehaviour 側で呼ぶ必要なし
- **`IDisposable`** 実装で `CompositeDisposable` / `CancellationTokenSource` / `Subject<T>` を解放(`AutoInjectGameObject` 経由の Game スコープ Dispose で発火)
- **`Subject<DrowZzzGameSession>` ベースで状態公開**(`Observable<DrowZzzGameSession> SessionStream`):ADR-0015 NRT 不採用方針下で `ReactiveProperty<DrowZzzGameSession?>` のような nullable アノテーション(`?`)を避ける。`Subject<T>` は Boot 完了の `OnNext` 前は何も発火しないため、View は「最初の有効な状態が来てから描画開始」が自然に成立する
- **null 防御**:ADR-0015 で NRT 不採用済、`ArgumentNullException` を constructor 各引数で投げる(既存パターン継承)。`savePath` は string のため `string.IsNullOrWhiteSpace` で空白も弾く
- **`SessionStream` の購読挙動**:`Subject<T>` 採用のため Subscribe 後の最初の `OnNext` のみ発火、Subscribe より前の発火は伝搬しない。M5 範囲では Boot 完了 → View Subscribe 順が `IStartable.Start()` シーケンス内で確定するため、初期描画は確実に届く。Subscribe より後に View が遅延 attach する場合(Phase 3)は `BehaviorSubject<T>` / `ReplaySubject<T>(1)` への切替を検討
- **`Forget` の Cancellation Token 連動**:`BootAsync` は CTS で打ち切り可能、Dispose 時に `_cts.Cancel()` で進行中の I/O を中断、`OperationCanceledException` は握りつぶす(意図された取り消し)、それ以外の例外は `Debug.LogError`

### 4. R3 1.3.0 の利用範囲

| 用途 | 採用 | 備考 |
| ---- | ---- | ---- |
| `IUserSettings` の `Observable<int>` / `Observable<string>` 購読(M4-PR6 で interface 確定済) | ✓ | View 側で BGM / SE / Language を Subscribe、即時値反映(BehaviorSubject 相当、M4-PR6 完成記録の xmldoc) |
| Presenter の `Observable<DrowZzzGameSession>` 公開(内部 `Subject<T>`)| ✓ | View が Subscribe してレンダリング、Pure C# 単体テストで `Subject.OnNext` 経由の発火を Mock View が記録(NRT 不採用方針との整合は本 ADR §3.2 / W-3 反映)|
| View → Presenter の入力通知 | ✗(C# event) | 本 ADR §3.1 で確定、R3 `Subject<T>` は過剰 |
| `CompositeDisposable` での Subscribe ライフサイクル管理 | ✓ | M4-PR6 で `PlayerPrefsUserSettings.Dispose` と同等のパターン |
| `R3.Unity` の MainThreadScheduler | ✓(必要時) | UniTask との並走で M5-PR5 / PR6 で必要に応じて利用、Domain は触らない(`Drowsy.Domain.asmdef` は `R3.dll` 直接参照、`R3.Unity` は Application / Presentation 経由) |

**MainThreadScheduler の暗黙利用**:UI Toolkit / MonoBehaviour は Unity Main Thread でのみ操作可能。`UniTask` の `PlayerLoopTiming.Update` (デフォルト) なら Main Thread 保証されるため、M5 範囲では `ObserveOn(MainThreadScheduler)` の明示は不要。Phase 3 で worker thread からの通知が入る場合に検討。

### 5. UniTask 2.5.10 の利用範囲

| 用途 | 採用 | 備考 |
| ---- | ---- | ---- |
| Session JSON 永続化の async I/O | ✓ | 本 ADR §5.2 で `IDrowZzzGameSessionSerializer.SaveAsync` / `LoadAsync` を追加 |
| Presenter `BootAsync` の起動シーケンス | ✓ | §3.2 の `BootAsync().Forget()` パターン |
| EndTurn 後の Auto-save | ✓ | §6 で `await _serializer.SaveAsync(...)` を `HandleEndTurnClicked` 内で呼ぶ |
| UI 演出の遅延 / フレーム待機 | ✗(M5 範囲外) | Phase 3、演出が入る時点で `UniTask.Delay` / `UniTask.NextFrame` 等を利用 |
| Cancellation Token | ✓ | Presenter Dispose 時に Token cancel、未完了 `SaveAsync` を打ち切る |

#### 5.1 `UniTask.Forget` の運用

`BootAsync().Forget()` のように戻り値を破棄するパターンは「投げっぱなし」で例外をログだけに留めてしまう罠がある。本 ADR では以下を遵守:

- `.Forget()` の前に `try { ... } catch { Debug.LogError } finally { ... }` を内包する(Forget で握りつぶさない)
- Cancellation Token を Presenter Dispose 時に発火させ、進行中の I/O を打ち切る
- M5-PR5 で UniTaskTracker(Unity Window > UniTask Tracker)で leak 確認

#### 5.2 `IDrowZzzGameSessionSerializer` interface 抽出

M4-PR5 の `DrowZzzGameSessionSerializer` は具象クラスのみ。本 ADR では:

```csharp
// 配置: Assets/_Project/Scripts/Application/Persistence/IDrowZzzGameSessionSerializer.cs
namespace Drowsy.Application.Persistence;

public interface IDrowZzzGameSessionSerializer
{
    // 同期 API は維持(M4-PR5 のテスト 43 件が直接参照、後方互換)
    // 引数順は既存 DrowZzzGameSessionSerializer.Save / Load と完全一致(session-first / path-only)
    void Save(DrowZzzGameSession session, string path);
    DrowZzzGameSession Load(string path);  // ファイル不在は FileNotFoundException(既存仕様継承)

    // 非同期 API は本 ADR で追加(M5-PR1 で同期ラップとして実装、M5-PR5 で WebGL 最適化検討)
    UniTask SaveAsync(DrowZzzGameSession session, string path, CancellationToken ct = default);
    UniTask<DrowZzzGameSession> LoadAsync(string path, CancellationToken ct = default);  // ファイル不在は FileNotFoundException(同期版と同じ仕様)
}
```

設計確定事項:

- **`DefaultSavePath` は interface に含めない**(W-1 反映):`DrowZzzGameSessionSerializer.DefaultSavePath(string fileName = "session.json")` は `static` メソッドで、C# 8 / .NET Standard 2.1 範囲では interface に `static` メンバを定義不可。代わりに Bootstrap で `RegisterInstance(DrowZzzGameSessionSerializer.DefaultSavePath())` し、Project Singleton として string を Presenter / 他 consumer に注入する(§7.2 Bootstrap スニペット参照)。`Application.persistentDataPath` を内部で呼ぶ件は Configure 内のメインスレッドで実行され、ワーカースレッドからの参照を回避(W-5 反映)
- **引数順は既存実装と完全一致**:`Save(session, path)` / `Load(path)`、`SaveAsync(session, path, ct)` / `LoadAsync(path, ct)`(W-4 反映)
- **`LoadAsync` の戻り値型は `UniTask<DrowZzzGameSession>`(non-null)**:M4-PR5 既存 `Load` の仕様(ファイル不在で `FileNotFoundException`)を継承、ADR-0015 NRT 不採用方針下で nullable 戻り値を避ける。Presenter は `BootAsync` で `try { LoadAsync } catch (FileNotFoundException) { _startGameUseCase.Execute() }` 分岐(§3.2 Presenter スニペット参照)
- M4-PR5 の同期 `Save` / `Load` を維持(43 テストの破壊回避)
- `SaveAsync` / `LoadAsync` の内部実装は M5-PR5 で確定、初期推奨は **同期版を `UniTask.RunOnThreadPool` ラップ**。ただし WebGL では Thread Pool が制限(WebGL は実質シングルスレッド、`RunOnThreadPool` は Main Thread fallback)されるため、I/O ブロックが目立つ場合は `UniTask.Yield(PlayerLoopTiming.LastUpdate)` を挿入して 1 フレーム分譲るパターンも候補(P-4 反映、M5-PR5 着手時の JIT 確認項目)
- 配置を `Drowsy.Application.Persistence` namespace に移動(Application が interface を定義、Infrastructure が実装する Ports & Adapters)

**M4-PR5 完成記録との後方互換**:既存 `DrowZzzGameSessionSerializer.Save(session, path)` を呼ぶテスト 43 件はそのまま動く(具象クラスの直接呼び出しで interface を経由しない)。`DrowZzzGameSessionSerializer.Save` / `Load` のシグネチャは無変更、interface は新規メソッド `SaveAsync` / `LoadAsync` を追加する形のみ。

### 6. シーン構成と Bootstrap 配置

**1 シーン構成(`Main.unity`)を採用**。理由:

| 案 | 結論 | 根拠 |
| ---- | ---- | ---- |
| `Boot.unity` + `Main.unity` の 2 シーン分離 | 不採用 | M5 範囲では Scene 切替が発生しない、Boot 専用シーンを作る価値が薄い、build settings 管理コスト |
| `Main.unity` 1 シーン構成 | **採用** | ProjectLifetimeScope + GameLifetimeScope を 1 GameObject ツリーに乗せ、Editor で完結する最小構成 |

シーン構造:

```
Main.unity
  ├─ [ProjectLifetimeScope] (GameObject, AutoInjectGameObject)
  │   - DrowZzzGameConfigAsset (SerializedField, ScriptableObject reference)
  │   - ScriptableObjectCardCatalog (SerializedField)
  │   - PlayerPrefsUserSettings (生成は Register、Inspector 非表示)
  │
  ├─ [GameLifetimeScope] (GameObject, Project の子)
  │   - DrowZzzGameView (MonoBehaviour, UIDocument を持つ)
  │       - UIDocument (UI Toolkit、UXML / USS asset を SerializedField で参照)
  │
  └─ EventSystem (UI Toolkit の入力サポート)
```

`ProjectLifetimeScope` を `DontDestroyOnLoad` にする実装は VContainer の `ProjectLifetimeScope.Find` 経由のスクリプタブル設定 or `[DefaultExecutionOrder(-10000)]` で初期化順制御(Phase 3 で Scene 切替時に対応)。

**Phase 3 で Boot 分離を再評価する判断点**:タイトル画面 / メニュー / 設定画面のような Scene 切替が必要になった時点で、別 ADR で 2 シーン構成に移行する。

### 7. SO Asset 配置先と Bootstrap への注入経路

#### 7.1 配置先

**訂正記録(M4-PR7 反映)**:本 ADR 起票時に「各 EffectAsset 派生を `<CardName>/<EffectName>.asset` として、CardEntryAsset を `<CardName>/<CardName>.asset` として独立 `.asset` 配置」と記述していたが、実態は **EffectAsset 派生型(12 種)と CardEntryAsset がいずれも `[Serializable] POCO`(ScriptableObject 非継承)**であり、`DrowZzzCardCatalog.asset` 1 個の中に `[SerializeReference]` で polymorphic にインライン編集される(ADR-0012 §3 案 (a) で確定済の事実、M4-PR1〜PR4 で実装済)。M4-PR7 で本 §7.1 を実態整合に訂正、§7.2 Bootstrap スニペットの `[SerializeField]` 注入対象も 2 SO のみで整合済。

| Asset | 配置先 | 備考 |
| ---- | ---- | ---- |
| `DrowZzzGameConfigAsset`(ScriptableObject、M4-PR7 で追加実装)| `Assets/_Project/Data/Configuration/DrowZzzGameConfig.asset` | 1 個固定、`[CreateAssetMenu(menuName = "Drowsy/DrowZzz/Game Config")]` |
| `ScriptableObjectCardCatalog`(ScriptableObject、M4-PR1〜PR3 完成)| `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset` | 1 個固定、`[CreateAssetMenu(menuName = "Drowsy/DrowZzz/Card Catalog")]`、Entries / Effects は本 Asset 内にインライン編集 |
| 各 `EffectAsset` 派生型(`[Serializable] POCO`、M4-PR2 / PR3 完成)| `DrowZzzCardCatalog.asset` の **`Entries[i].Effects[j]` インライン** | 独立 `.asset` ファイルにはならない、`[SerializeReference]` で polymorphic 編集 |
| `CardEntryAsset`(`[Serializable] POCO`、M4-PR1 完成)| `DrowZzzCardCatalog.asset` の **`Entries[i]` インライン** | 独立 `.asset` ファイルにはならない、`SerializeField` で配列要素として保持 |

Designer ワークフローの完全な手順は [`docs/architecture/designer-workflow.md`](../architecture/designer-workflow.md)(M4-PR7 で確立)を参照。

**`[SerializeReference]` の polymorphic 編集 UX**(M4-PR7 第 4 弾で確立):Unity 6 標準 UI では `EffectAsset` 派生型の型選択ドロップダウンが安定して表示されないため、`Assets/_Project/Scripts/Infrastructure/Editor/EffectAssetReferenceDrawer.cs`(Editor only、`Drowsy.Infrastructure.Editor` asmdef)を導入し、`[CustomPropertyDrawer(typeof(EffectAsset), useForChildren: true)]` で全派生型のドロップダウン UI を提供する。M5-PR3 で UI Toolkit を採用する際の Inspector 操作にも影響なし(Drawer は Unity Inspector 内部のみで動作、Build には乗らない)。

#### 7.2 Bootstrap への注入経路

**Inspector `[SerializeField]` 注入** を採用。Resources / AssetReference は M5 範囲では不採用(Phase 3 で Addressables 移行検討の余地)。

```csharp
// 配置: Assets/_Project/Scripts/Bootstrap/ProjectLifetimeScope.cs
namespace Drowsy.Bootstrap;

public sealed class ProjectLifetimeScope : LifetimeScope
{
    [SerializeField] private DrowZzzGameConfigAsset _gameConfig;
    [SerializeField] private ScriptableObjectCardCatalog _cardCatalog;

    protected override void Configure(IContainerBuilder builder)
    {
        // Inspector 注入忘れの Boot 時点検出(Build 後の沈黙 fail 回避、本 ADR §7.2 末尾)
        if (_gameConfig is null)
        {
            throw new InvalidOperationException(
                "DrowZzzGameConfigAsset が Inspector で未設定です。" +
                "ProjectLifetimeScope の _gameConfig フィールドに DrowZzzGameConfig.asset を割り当ててください。");
        }
        if (_cardCatalog is null)
        {
            throw new InvalidOperationException(
                "ScriptableObjectCardCatalog が Inspector で未設定です。" +
                "ProjectLifetimeScope の _cardCatalog フィールドに DrowZzzCardCatalog.asset を割り当ててください。");
        }

        // Settings / Catalog(ScriptableObject は RegisterInstance、RegisterComponentInHierarchy は MonoBehaviour 専用)
        builder.RegisterInstance<IGameConfig>(_gameConfig);
        builder.RegisterInstance<ICardCatalog<IEffect>>(_cardCatalog);

        // セーブパス(Application.persistentDataPath は Configure 内のメインスレッドで取得、
        // ワーカースレッドからの参照を回避、W-5 反映)
        builder.RegisterInstance(DrowZzzGameSessionSerializer.DefaultSavePath());

        // Infrastructure(XorShiftRandom は uint seed ctor のため Register<,> では解決不可、
        // RegisterInstance で時刻ベース seed を渡す、code-reviewer W-2 反映)
        builder.RegisterInstance<IRandomSource>(new XorShiftRandom(unchecked((uint)DateTime.UtcNow.Ticks)));
        builder.Register<IDrowZzzGameSessionSerializer, DrowZzzGameSessionSerializer>(Lifetime.Singleton);
        builder.Register<IUserSettings, PlayerPrefsUserSettings>(Lifetime.Singleton);

        // Application(DrowZzzRule は ctor で ICardCatalog<IEffect> + EffectInterpreter を要求、
        // EffectInterpreter は VContainer の自動解決対象外のため明示 Register、code-reviewer W-1 反映)
        builder.Register<EffectInterpreter>(Lifetime.Singleton);
        builder.Register<DrowZzzRule>(Lifetime.Singleton);
    }
}
```

```csharp
// 配置: Assets/_Project/Scripts/Bootstrap/GameLifetimeScope.cs
namespace Drowsy.Bootstrap;

public sealed class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<StartGameUseCase>(Lifetime.Singleton);
        builder.Register<ApplyActionUseCase>(Lifetime.Singleton);

        // View MonoBehaviour は同シーン Hierarchy 内に配置済みを前提に検索
        builder.RegisterComponentInHierarchy<DrowZzzGameView>().AsImplementedInterfaces();

        // Presenter は IStartable / IDisposable を Container 経由で発火させるため AsImplementedInterfaces() で公開、
        // および Presenter 自身の Singleton 登録(Container が一意 instance を保持)を両立(P-1 反映)
        builder.Register<DrowZzzGamePresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
    }
}
```

**`AsImplementedInterfaces().AsSelf()` 併用の意図**:`IStartable` / `IDisposable` を Container 経由で呼び出してもらうための公開と、Presenter 単体テストや View 参照のために具象型 `DrowZzzGamePresenter` で Resolve できる経路を両立。VContainer の標準パターン。

**SerializeField で null が混入したときの挙動**:`Configure` 内で `RegisterInstance` が `null` を受けると VContainer は `ArgumentNullException` を投げる。M5-PR1 で `Configure` の前段に `if (_gameConfig is null) throw new InvalidOperationException("...")` を入れる(Inspector 注入忘れを Boot 時点で検出、Build 後の沈黙 fail を回避)。

### 8. Session 自動セーブ / 自動復元

| タイミング | 動作 | 備考 |
| ---- | ---- | ---- |
| アプリ起動時(Presenter `BootAsync`) | `DefaultSavePath()` のファイル存在なら `LoadAsync` で復元、なければ `StartGameUseCase.Execute(...)` で新規対戦 | M5-PR4 で実装済(`BootAsync` 復元経路 + 新規対戦経路、code-reviewer M5-PR5 W-1 反映で訂正)|
| `EndTurnAction` Apply 後 | `await _serializer.SaveAsync(session, _savePath, ct)` | M5-PR5、毎ターン後(引数順は §5.2 の interface 定義通り session-first) |
| Game 終了時(`GameOutcome` 確定) | `await _serializer.SaveAsync` + 同期 backup(別 path で並行保存)| M5-PR7 で確定、初期推奨はメイン path 上書きのみ |
| アプリ終了時(`OnApplicationQuit`) | `OnApplicationQuit` で Cancellation Token 発火 + 同期 `Save`(WebGL は不可、後述) | M5-PR5 で WebGL 制約を確認 |

**WebGL 制約**:WebGL では `OnApplicationQuit` が発火しない / IndexedDB への同期 I/O 制約あり。Auto-save をターン後に行う方針なら最終ターン後の保存で実質的に閉じる。M4-PR7 / M5-PR5 で WebGL 実機検証時に確認。

**Auto-save の頻度**:`EndTurn` 後のみ(`DrawCardAction` / `PlayCardAction` の各 Apply 後ではない)。理由:

- ターン内の Draw / Play は 1 アクションずつ独立性が低い(Draw → Play → EndTurn が 1 セット)
- Save 頻度を抑えることで I/O 負荷低減(WebGL の IndexedDB 書込みは特にコスト高)
- ADR-0011 で確定した「キーワード能力 / 反撃」が 1 ターン内で複数 Action を発生させる構造との整合性

### 9. namespace 構造

| namespace | 内容 | 配置 |
| ---- | ---- | ---- |
| `Drowsy.Bootstrap` | `ProjectLifetimeScope` / `GameLifetimeScope` | `Assets/_Project/Scripts/Bootstrap/` |
| `Drowsy.Presentation.Games.DrowZzz` | `IDrowZzzGameView` / `DrowZzzGameView` / `DrowZzzGamePresenter` | `Assets/_Project/Scripts/Presentation/Games/DrowZzz/` |
| `Drowsy.Application.Persistence` | `IDrowZzzGameSessionSerializer`(interface) | `Assets/_Project/Scripts/Application/Persistence/` |
| `Drowsy.Infrastructure.Persistence` | `DrowZzzGameSessionSerializer`(具象、interface 実装に変更) | 既存(M4-PR5)、interface 実装に変更 |

ADR-0005 §3 の namespace 階層化原則を踏襲(`*.Games.DrowZzz.*` で DrowZzz 固有実装を分離)。

### 10. テスト戦略

| asmdef | 範囲 | 着手 PR |
| ---- | ---- | ---- |
| `Drowsy.Application.Tests`(既存) | `IDrowZzzGameSessionSerializer` interface 契約テスト追加(後述) | M5-PR1 |
| `Drowsy.Infrastructure.Tests`(既存) | `DrowZzzGameSessionSerializer.SaveAsync` / `LoadAsync` round-trip(同期 API 43 件は維持) | M5-PR5 |
| **`Drowsy.Presentation.Tests`(新設)** | `DrowZzzGamePresenter` Pure C# 単体テスト、`IDrowZzzGameView` モック | **M5-PR2** |
| **`Drowsy.Bootstrap.Tests`(新設、任意)** | LifetimeScope の Configure テスト | **判断保留**(VContainer の標準テスト helper は限定的、Pure C# Presenter 側で代替可能、本 ADR では新設しない) |

#### 10.1 `Drowsy.Presentation.Tests` 構成

```
Assets/_Project/Scripts/Tests/Presentation.Tests/
  Drowsy.Presentation.Tests.asmdef
    references: Drowsy.Domain / Drowsy.Application / Drowsy.Application.Tests /
                Drowsy.Presentation / UniTask / VContainer / R3.Unity /
                UnityEngine.TestRunner / UnityEditor.TestRunner
    precompiledReferences: nunit.framework.dll
  Games/DrowZzz/
    DrowZzzGamePresenterTests.cs
    MockDrowZzzGameView.cs (IDrowZzzGameView の最小実装、event 発火制御用)
    MockDrowZzzGameSessionSerializer.cs (LoadAsync の挙動制御で BootAsync の各分岐を駆動)
    MockUserSettings.cs (IUserSettings の最小モック、M5-PR6 で粒度向上)
```

**`Drowsy.Application.Tests` 参照の判断**(M5-PR2 着手時の JIT 確定 2026-05-14):
Presenter ctor が具象 `StartGameUseCase` / `ApplyActionUseCase` を取るため、テストで両 UseCase を構築するには
`IRandomSource` / `IGameConfig` / `DrowZzzRule` の stub instance が必要。`Drowsy.Application.Tests.Stubs`
配下の `IdentityRandom` / `StubGameConfig` / `SessionFactory.NewRule` を再利用する目的で、
`Drowsy.Presentation.Tests.asmdef` の references に `Drowsy.Application.Tests` を加える(test asmdef
間の依存、本番 Build には混入しない)。代替案(stubs 多重実装)より DRY 性と保守性を優先。

`includePlatforms: ["Editor"]` + `UNITY_INCLUDE_TESTS` constraint で本番 Build に混入しない(M4-PR1 / PR6 で確立済パターン継承)。

**View モック方針**:`IDrowZzzGameView` を Pure C# クラスで実装(`MockDrowZzzGameView`)、`OnDrawClicked` / `OnPlayClicked` / `OnEndTurnClicked` を public method で発火、`Render` 呼び出しの記録を List に蓄積、で検証。

#### 10.2 Bootstrap LifetimeScope テストの判断

VContainer の `ScopedContainerBuilder` を直接呼んで `Resolve<T>()` するテストは可能だが、`RegisterComponentInHierarchy` / `RegisterInstance` の Inspector 注入経路は Editor / Play モード起動を要し、軽量 NUnit 単体テストには馴染まない。M5 範囲では:

- LifetimeScope 単体テストは作らない
- 代わりに Presenter 単体テスト(`Drowsy.Presentation.Tests`)で「依存型が constructor 注入された後の動作」を検証
- 統合テストは「実際に Play モードで動かす」手動 QA で M5-PR8 / M4-PR7 と並走

### 11. PR 分割計画(M5-PR1 〜 M5-PR8)

ADR-0006 / 0011 / 0012 で確立した「1 PR = 1 論理変更」+ 「JIT 確認を各 PR 着手時に行う」運用に従い、M5 を **8 PR に分割** する。

| PR | スコープ | 主要成果物 | 依存 | 備考 |
| ---- | ---- | ---- | ---- | ---- |
| **M5-PR1** | `IDrowZzzGameSessionSerializer` interface 抽出 + `ProjectLifetimeScope` 骨格 + `GameLifetimeScope` 骨格 | `Drowsy.Application.Persistence.IDrowZzzGameSessionSerializer` / `DrowZzzGameSessionSerializer` を interface 実装に変更 / `ProjectLifetimeScope.cs` / `GameLifetimeScope.cs`(空 Configure)/ `Drowsy.Application.Tests` で interface 契約テスト追加 | M4-PR7(SO Asset 配置確立) | M4-PR5 の同期 API + 43 テストは維持、Async API は本 PR で追加(空実装 or 同期ラップ)、Bootstrap は登録ゼロで build 通過確認 |
| **M5-PR2** | `IDrowZzzGameView` interface + `DrowZzzGamePresenter` Pure C# 実装(NotImplementedException 骨格) + `Drowsy.Presentation.Tests` asmdef 新設 + `MockDrowZzzGameView` + Presenter 単体テスト最小セット | `IDrowZzzGameView.cs` / `DrowZzzGamePresenter.cs`(Start / Dispose / BootAsync 実装、Handler 3 種は NotImplementedException) / `MockDrowZzzGameView.cs` / `DrowZzzGamePresenterTests.cs`(ctor null 防御 6 引数 + savePath 空白防御 + Start で event 配線確認 + Dispose で event 解除確認 + Subject 発火順序確認) | M5-PR1 | View は MonoBehaviour 未実装、UI Toolkit の `UIDocument` も未配置、純粋に Presenter ロジックの単体テスト、Presenter ctor は 6 引数(`StartGameUseCase` / `ApplyActionUseCase` / `IDrowZzzGameView` / `IDrowZzzGameSessionSerializer` / `IUserSettings` / `string savePath`)|
| **M5-PR3** | UI Toolkit `UIDocument` + UXML / USS skelton + `DrowZzzGameView` MonoBehaviour 実装(`IDrowZzzGameView` 実装) + Bootstrap でのワイヤリング | `Assets/_Project/UI/DrowZzzGame.uxml` / `.uss` / `DrowZzzGameView.cs`(VisualElement query + event 発火) / `ProjectLifetimeScope` / `GameLifetimeScope` の Configure 実装 | M5-PR2 | UXML はラベル数個 + ボタン 3 個の最小骨格、Render は `Debug.Log` 出力でひとまず動作確認、Play モードで「ボタン押すと event 発火」を確認 |
| **M5-PR4** | Presenter Handler 実装(`HandleDrawClicked` / `HandlePlayClicked` / `HandleEndTurnClicked`)+ `IsLegalMove` ガード + Render 実装 | `DrowZzzGamePresenter` Handler 3 種実装 / View の `Render(DrowZzzGameSession)` 実装(山札残数 / 手札 / 場札 / 現プレイヤー / TurnPhase / SDP / FDP / DDP / Round 表示) / Presenter 単体テスト追加(各 Handler の正常 / 異常パス) | M5-PR3 | この PR で「Play モードで Draw → Play → EndTurn の 1 ラウンドが動く」が達成 |
| **M5-PR5** | `DrowZzzGameSessionSerializer.SaveAsync` / `LoadAsync` 実装 + Presenter `BootAsync` で自動復元 + `HandleEndTurnClicked` で Auto-save | `DrowZzzGameSessionSerializer.SaveAsync / LoadAsync`(同期 API を `UniTask.RunOnThreadPool` ラップ、初期推奨)/ `Drowsy.Infrastructure.Tests` で round-trip 追加 / Presenter の `BootAsync` + Auto-save Handler 統合 / Cancellation Token 管理 | M5-PR4 | Auto-save タイミングは `EndTurn` 後のみ、JIT 確認待ち項目 1(WebGL の IndexedDB 制約) |
| **M5-PR6** | `IUserSettings` を View に R3 Observable バインディング + 設定 UI(BGM Slider / SE Slider / Language Dropdown の最小骨格) | UXML 拡張(Slider × 2 + Dropdown × 1)/ View での Subscribe + Setter 呼び出し / Presenter で `IUserSettings` を View へ橋渡し or View 直接注入 / Presentation テストで Subscribe 経路検証 | M5-PR5 | `IUserSettings` を View に直接 Inject する設計か Presenter 経由かで JIT 確認、**初期推奨は View に直接 Inject**(P-5 反映、判断軸:① View 直接注入は View constructor の引数が 1 つ増えるが MockUserSettings で単体テスト可能 / ② Presenter 経由は Presenter が UI 設定とゲーム状態両方を管理して Single Responsibility が崩れる、UI 設定はゲーム状態と独立性が高いため ① 採用) |
| **M5-PR7** | ゲーム終了判定の UI 反映(Winner / Draw 表示) + Round 21 完了時の Auto-save Final 経路 | `DrowZzzGamePresenter` で `Session.Value.Outcome` を購読 / View の `RenderOutcome(GameOutcome)` 実装 / Outcome 確定後の入力 disable | M5-PR6 | ADR-0010 §3 / §4 の `WinnerOutcome` / `DrawOutcome` を UI に反映、リトライボタンは Phase 3 |
| **M5-PR8** | WebGL ビルド検証 + Phase 2 完結処理 + 統合確認 + README ステータスバナー更新 + CLAUDE.md §11 Phase 進捗更新 + ADR-0005 §7 完了基準の達成記録 | WebGL Build 実機テスト記録(操作スクショ等)/ `link.xml` の M4-PR5 設定が WebGL Build で動くこと確認 / README / CLAUDE.md / 本 ADR / ADR-0005 / ADR-0012 完成記録同梱 | M5-PR7 | 新規実装ゼロ、検証 + ドキュメント、ADR-0015 §「再評価条件」充足確認、本 PR を以て Phase 2 完結 |

**PR 数の目安**:8 PR(M3 / M4 と同規模、M3-PR5 のような 1 PR 内の sub-PR 分割は想定しないが、M5-PR3 の UI Toolkit 立ち上げが大きい場合 PR3 を 3a(UXML / USS のみ)/ 3b(View MonoBehaviour 実装) に分割する可能性、その判断は M5-PR3 着手時)。

**M5-PR1 着手前提条件**:M4-PR7(M4 完成 PR、ADR-0012 §「M4-PR7 着手時の項目」)が完了していること。M4-PR7 で:

1. 実 `.asset` ファイル配置(`Assets/_Project/Data/Cards/...`)
2. Designer ワークフロー実証(Inspector で SO 編集 → Catalog 参照 → 起動)
3. WebGL/IL2CPP 実機検証(link.xml 動作確認)
4. M4 完成記録 + CLAUDE.md §11 Phase 進捗バナー更新

が完了することで、M5-PR1 で `[SerializeField]` 注入する SO Asset が確実に存在する。

### 12. NRT 再評価ポイント

ADR-0015 §「再評価条件」第 1 項:

> M5 Bootstrap で外部 API クライアント / シリアライザ等の null 多発コードが入る時点

本 ADR-0016 で M5 範囲に登場する null 多発コード候補:

- `IDrowZzzGameSessionSerializer.LoadAsync(...)` の戻り値 `DrowZzzGameSession?`(永続化ファイル不在で null)
- `Presenter._session.Value` の `DrowZzzGameSession?`(`BootAsync` 完了前 null)
- `View.Render(DrowZzzGameSession)` の引数(Subscribe で null フィルタ済の前提だが nullable 注釈なし)
- `[SerializeField]` 注入のフィールド(Inspector 未設定時 null)

これらは ADR-0015 で確立した「`?? throw new ArgumentNullException` + Abnormal テスト網羅」で M5-PR1〜PR8 範囲を担保可能と判断、M5 範囲では **NRT を有効化しない**。M5-PR8 完成時点で「外部 API クライアント / シリアライザの null 経路が PR 内テストで適切にカバーできているか」を再評価し、不足が見つかった場合のみ ADR-0015 を Superseded by ADR-NNNN で覆す。

### 13. 機械検知レイヤとの整合性

| 対象 | 機械検知 |
| ---- | ---- |
| `IDrowZzzGameView` interface に `[Test]` Category 必須など | 対象外(interface 自体はテスト対象でない、実装側でカバー) |
| Presenter / View の dependency direction | asmdef references で物理保証(Presentation → Application は既存設定、Bootstrap → 全 4 層は既存設定) |
| `[SerializeField]` の null 注入 | Configure 前の手動 throw(本 ADR §7.2)、Roslyn Analyzer での自動検出は Unity Editor のみで限定 |
| `R3` の `Subscribe` メモリリーク | `CompositeDisposable.AddTo` パターン(本 ADR §3.2)、Roslynator RCS-1090(Add value 'CallerMemberName')等の関連 RCS は本 ADR 範囲では発火せず |
| `UniTask` の Forget 罠 | UniTaskTracker(Editor のみ、CI 未整備、Phase 3 で検討) |

新規の Roslyn Analyzer / lefthook フック追加は本 ADR では行わない(M5-PR1〜PR8 で必要が出た時点で別 chore PR で追加)。

## Consequences

### Positive

- Phase 2 完了の最小定義(ADR-0005 §7)が達成可能な実装計画が確定
- VContainer / UniTask / R3 の実利用が始まり、asmdef references の dead 状態(M1〜M4 で「import するが API を呼ばない」状態だった)が解消
- Presenter Pure C# 化により ADR-0006 §4「Pure C# 哲学」が Presentation 側まで貫徹
- M4-PR6 の `IUserSettings` Observable 公開設計が UI バインディングで初めて実利用される(M5-PR6)
- ADR-0014 で削った `StartGameUseCase` の 2 引数 ctor が Bootstrap での `Register` で素直に書ける
- ADR-0015 の NRT 不採用判断が M5 範囲のテストで再検証され、再評価条件第 1 項の自己回収機会になる
- UI Toolkit + UIDocument + DI 注入のパターンが確立し、Phase 3 の本格 UI 拡張時のひな形となる

### Negative

- 8 PR 構成は M5 スコープに対してやや多め(各 PR が小さくなる)
  - **緩和**:1 PR = 1 論理変更の規約を維持することで code-reviewer 単位を保ち、レビュー粒度を確保
- UI Toolkit の学習コスト(uGUI 経験者から見ると VisualElement / USS 等の差分)
  - **緩和**:M5-PR3 で UXML / USS の skelton を 1 ファイルで完結させる方針、複雑 layout は Phase 3
- `[SerializeField]` 注入の null 検出が Configure 前の手動 throw に依存(機械検知が薄い)
  - **緩和**:M5-PR1 で `if (_gameConfig is null) throw` のテンプレートを `ProjectLifetimeScope.cs` に書き、後続 PR が踏襲
- WebGL の `OnApplicationQuit` 非発火 / IndexedDB 制約を M4-PR7 / M5-PR5 で確認するまで不確定
  - **緩和**:Auto-save をターン後にしておけば最終ターン後の保存で実質的に閉じる、M5-PR5 で実機検証
- **WebGL で対戦中にタブを閉じた場合、最後の `EndTurn` 後の Save 状態までしか復元できない**(P-3 反映):`OnApplicationQuit` が WebGL では発火せず、ターン進行中(Draw / Play 後の `EndTurn` 前)に閉じられると進行中状態は失われる
  - **緩和**:M5 範囲では受容、Phase 3 で「Draw 後 / Play 後の局所セーブ」または「IndexedDB の `beforeunload` event hook」を別 ADR で評価
- WebGL の Thread Pool 制限により `UniTask.RunOnThreadPool` が実質 Main Thread fallback になり、I/O ブロックが UI フリーズを引き起こす可能性
  - **緩和**:M5-PR5 で実測、必要なら `UniTask.Yield(PlayerLoopTiming.LastUpdate)` 挿入 or `StreamReader.ReadToEndAsync` への切替(本 ADR §5.2)
- Phase 3 の Scene 切替時に 1 シーン構成 → 2 シーン構成のリファクタが必要
  - **緩和**:本 ADR §6 で「Phase 3 で別 ADR」を明示、現時点で構造を縛らない
- `IDrowZzzGameSessionSerializer` の同期 + 非同期 API 併存は型表面が広がる
  - **緩和**:本 ADR §5.2 で「M4-PR5 の 43 テスト維持」を理由として明記、Phase 3 で同期 API 撤去判断

### Neutral

- M4-PR7 と M5-PR1 の境界が「M4-PR7 完了 → M5-PR1 開始」の直列順序になる(並行不可)
- 本 ADR で確定した PR 分割は JIT 共有での微調整余地を残す(PR3 の 3a / 3b 分割など)
- Presenter Pure C# 単体テストが `Drowsy.Presentation.Tests` に集中するため、View MonoBehaviour 側のテストは Play モードでの手動 QA に依存(統合テスト不在)

## Alternatives Considered

| 案 | 不採用理由 |
| ---- | ---- |
| **VContainer 3 階層**(Project / Game / Scene) | M5 範囲は 1 シーン構成で Scene スコープを独立に切る必要が薄い、Phase 3 で必要に応じて分割 |
| **`StartGameUseCase` を Transient で登録** | 1 対戦内で `Execute()` は 1 回しか呼ばれず、Singleton と動作差なし、Singleton のほうが寿命管理が明示的 |
| **MVVM(R3 ReactiveProperty を View が直接 Bind)** | UI Toolkit `DataBinding` は Unity 2023+ に存在するが Unity 6 で安定性検証コストあり、Pure C# 単体テスト性が下がる |
| **Direct binding(MonoBehaviour が UseCase を直接呼ぶ)** | ADR-0006 §4 の Pure C# 原則違反、テスト不能 |
| **uGUI / IMGUI 採用** | プロジェクトオーナー JIT で UI Toolkit 確定(2026-05-13)、本 ADR §JIT 共有された方針 参照 |
| **CPU 対戦 / Networking を M5 内に含める** | プロジェクトオーナー JIT で「N=2 ホットシートのみ、CPU / Network は Phase 3」確定 |
| **View → Presenter 通知に R3 `Subject<T>` を使う** | 過剰、event で十分、View のメモリリーク防止責務が `event += / -=` の対称運用で明示される |
| **Auto-save を全 Action(Draw / Play / EndTurn)後に行う** | I/O 頻度過剰、特に WebGL の IndexedDB 書込みコスト、`EndTurn` 後のみで実用十分 |
| **手動セーブ / リセットボタンを M5 内に追加** | M5 範囲が膨らむ、Auto-save + 自動復元で「Play モードで遊べる」最小定義は満たされる、Phase 3 で UI 拡張 |
| **`Boot.unity` + `Main.unity` 2 シーン分離** | M5 範囲では Scene 切替が発生しない、build settings 管理コスト、Phase 3 で Scene 切替が必要になった時点で別 ADR |
| **`IDrowZzzGameSessionSerializer` 同期 API を撤去して非同期のみ** | M4-PR5 の 43 テストが破壊される、後方互換維持を優先、Phase 3 で再評価 |
| **`Drowsy.Bootstrap.Tests` asmdef 新設** | VContainer の `ScopedContainerBuilder` テストは Editor / Play 起動を要し軽量化が難しい、Presenter 単体テストで代替可能 |
| **NRT を M5 で有効化** | ADR-0015 の判断を覆すコスト(プロダクション 87 ファイル touch)が M5 範囲の null 多発コードの価値を上回る、M5-PR8 完成時点で再評価 |
| **`StartGameUseCase` を Transient + 各対戦で IRandomSource 新規生成** | Phase 3 の「新規対戦」UI 導入時に GameLifetimeScope ごと再生成する方が自然、本 ADR では Singleton で足りる |
| **`PlayerPrefsUserSettings` の代わりに JSON ファイル保存** | M4-PR6 で確定済(`Drowsy.Infrastructure.Settings`)、本 ADR で覆さない |
| **`[SerializeField]` 注入を Resources / AssetReference に変更** | Phase 3 の Addressables 導入時に再評価、M5 範囲では Inspector 注入が最も単純 |
| **`DontDestroyOnLoad` を使わず Game スコープごとに Project スコープを作り直す** | VContainer の `RootLifetimeScope` パターン(ProjectLifetimeScope を Awake で `DontDestroyOnLoad`)が VContainer デファクト、Phase 3 の Scene 切替時に対応 |

## Implementation Notes

### M5-PR1 着手前確認項目

1. **M4-PR7 完了確認**:
   - 実 `.asset` ファイル配置完了(`Assets/_Project/Data/Cards/...`)
   - Designer ワークフロー実証完了
   - WebGL/IL2CPP 実機検証完了
   - M4 完成記録 + CLAUDE.md §11 Phase 進捗バナー更新完了
2. **VContainer 1.17.0 の `IStartable` / `IObjectResolver.Dispose` の挙動再確認**:
   - `IStartable.Start()` が `IPostStartable` よりも早く呼ばれるか(Presenter `BootAsync` の起動タイミング)
   - `Lifetime.Singleton` で登録した `IDisposable` 実装が Scope.Dispose 時に自動 Dispose されるか
3. **`Drowsy.Application.asmdef` への `Cysharp.Threading.Tasks` reference 追加要否**:
   - `IDrowZzzGameSessionSerializer.SaveAsync` の戻り値 `UniTask` は Application 層 interface が UniTask に依存する形になる
   - 既存 `Drowsy.Application.asmdef` の references に `UniTask` が含まれている(Phase 0、本 ADR Context §既存基盤の現況 参照)
   - 既存設定で対応可能、追加 references 不要

### 各 PR 着手時の JIT 確認項目

| PR | JIT 確認項目 |
| ---- | ---- |
| M5-PR1 | (1) `IDrowZzzGameSessionSerializer` の同期 API 維持で良いか / (2) `LoadAsync` の `null` 戻り値が「ファイル不在」と「JSON parse 失敗」を区別すべきか |
| M5-PR2 | (1) `IDrowZzzGameView.Render(DrowZzzGameSession)` の引数を `DrowZzzGameSession?` にすべきか(null 状態の View 表示)/ (2) Presenter の `IStartable` 実装で良いか or 別 `IBootable` interface を切るか |
| M5-PR3 | (1) UXML / USS の配置先(`Assets/_Project/UI/` vs `Assets/_Project/Scripts/Presentation/Games/DrowZzz/UI/`)/ (2) `RegisterComponentInHierarchy<DrowZzzGameView>` vs `RegisterComponentInNewPrefab` の選択 |
| M5-PR4 | (1) `IsLegalMove` false 時の View 表示(ボタン disable / トースト表示 / 無反応)/ (2) `PlayCardAction` の手札選択 UX(クリック / ドラッグ / ボタン)|
| M5-PR5 | (1) `SaveAsync` を同期 API のラップで実装するか、`StreamReader.ReadToEndAsync` 再実装するか / (2) WebGL の `OnApplicationQuit` 非発火 / IndexedDB 動作確認 |
| M5-PR6 | (1) `IUserSettings` を View に直接 Inject か Presenter 経由か / (2) Language Dropdown の選択肢(M4-PR6 の `LanguageCodes` 直接利用 vs UI 表示文字列マッピング)|
| M5-PR7 | (1) `GameOutcome` 確定後の入力 disable 方法(View 側でボタン disable vs Presenter 側で event 無視)/ (2) Auto-save Final 経路の path 別名保存 vs メイン path 上書き |
| M5-PR8 | (1) WebGL ビルド検証の最終手順 / (2) Phase 3 着手判断記録(別 ADR で扱うか本 PR で附帯記録か)|

### 学びと運用継承の予定

ADR-0007 / 0010 / 0011 / 0012 で確立した運用を本 ADR で継承:

- **初期推奨案を ADR に書く運用**(ADR-0007 §1.5 / ADR-0010 §「Implementation Notes」/ ADR-0012 §3):本 ADR §3 / §6 / §7 で各候補に「採用 / 不採用」の根拠を明記、JIT 確認は「初期推奨で進めて良いか」のシンプルな質問に収まる
- **記録の Single Source of Truth**(ADR-0003 / CLAUDE.md §12):PR 別の完成記録は本 ADR §「M5-PR1〜PR8 完成記録」に逐次追記、CLAUDE.md にはミラーしない
- **`docs/todo.md` への小規模 chore 切り出し**(ADR-0003):M5-PR1〜PR8 中に発生する後追い chore は本 ADR ではなく `docs/todo.md` で追跡(例:Presenter 単体テストの境界網羅、WebGL Build CI 整備)
- **`dotnet build` pre-commit + Unity Editor Focus Auto-refresh**(M4-PR3 で確立):本 PR 群でも継続、新規 `.cs` 追加時に Editor Focus を取って `.csproj` 自動更新

### M5-PR1〜PR8 完成記録

M5 各 PR の完成記録を逐次追記する(本 ADR が Single Source of Truth、CLAUDE.md §11 にはミラーしない方針 — 「学びと運用継承の予定」§2 参照)。
PR 番号は GitHub マージ後に確定するため、本セクションは原則として **後続 PR の最初の commit で前 PR の完成記録を追記する** 運用とする(M3 / M4 過去 PR と同パターン)。

#### M5-PR1 完成記録

- **PR**:[#86](https://github.com/u-stem/drowsy-unity/pull/86)、squash merged → commit `bbb8728`(2026-05-14)
- **スコープ達成**:
  - `IDrowZzzGameSessionSerializer` interface 抽出(`Drowsy.Application.Persistence` namespace)、同期 `Save` / `Load`(M4-PR5 既存 43 テスト互換維持)+ 非同期 `SaveAsync` / `LoadAsync`(UniTask + CancellationToken)
  - Infrastructure `DrowZzzGameSessionSerializer` を interface 実装に変更、`SaveAsync` / `LoadAsync` は `UniTask.RunOnThreadPool` ラップ(本 ADR §5.2 初期推奨通り)
  - Bootstrap `ProjectLifetimeScope` / `GameLifetimeScope` 骨格(`sealed`、Configure 空、M5-PR3 で実 Register 追加予定)
  - `Drowsy.Application.Tests` に `UniTask` reference 追加 + `InMemoryDrowZzzGameSessionSerializer` fake + 契約テスト 13 件(APP-044〜APP-056)
  - EARS `docs/specs/application/persistence/session-serializer-interface.md` + `.feature` 新設
- **検証結果**:
  - `dotnet build drowsy-unity.slnx`:0 警告 / 0 エラー / 5.70 秒
  - `bash scripts/check-traceability.sh`:仕様 ID 534 件 / Property ID 445 件 / 整合性 OK
  - lefthook pre-commit 全フック緑(test-categories / gitleaks / traceability / dotnet-build / dotnet-format / conventional)
  - Unity Test Runner EditMode 緑確認はオーナー側で実機実施(`Given_savedSessionAsync_When_LoadAsync_Then_ReturnsSavedSession` の NullReferenceException は `.AsTask()` 変換で解消済)
- **JIT 確定事項**:
  - 同期 API は M4-PR5 既存 43 テスト互換のため維持(本 ADR §5.2 確定通り)
  - `LoadAsync` 戻り値は non-null、ファイル不在は `FileNotFoundException`(本 ADR §5.2 + ADR-0015 NRT 不採用方針整合)
  - 非同期 API 初期実装は `UniTask.RunOnThreadPool` ラップ(本 ADR §5.2 初期推奨通り、WebGL 最適化は M5-PR5 で再検討)
  - 契約テスト粒度は fake 実装 + round-trip(本 PR で `InMemoryDrowZzzGameSessionSerializer` を `Drowsy.Application.Tests.Stubs` に追加、M5-PR2 以降の Presenter 単体テストでも再利用予定)
- **本 ADR への訂正**:
  - §8 表「`EndTurnAction` Apply 後」の `SaveAsync` 引数順を `path-first` 誤記から `session-first` に訂正(W-1 反映)
  - §5.2 内コメント「M5-PR5 実装」を「M5-PR1 で同期ラップとして実装、M5-PR5 で WebGL 最適化検討」に訂正(T-3 反映)
- **code-reviewer 指摘の反映**:W-1〜W-4 + T-2 / T-3 / T-5 / T-7 を本 PR 内で反映、T-1 / T-4 / T-6 は本 PR 範囲外と判断(再評価条件発生時に再起票)
- **次 PR への引き継ぎ**:
  - `IDrowZzzGameSessionSerializer` は M5-PR2 の Presenter ctor で 4 番目引数として注入される
  - `InMemoryDrowZzzGameSessionSerializer` fake は `Drowsy.Application.Tests.Stubs` 配置のため、`Drowsy.Presentation.Tests`(M5-PR2 新設)からは参照できない(asmdef 境界)。Presenter 単体テストでは別の Mock 実装(`Drowsy.Presentation.Tests.Stubs` 配下)を新設するか、Fake を `Drowsy.Application` 本体に昇格させる判断を M5-PR2 で行う

#### M5-PR2 完成記録

- **PR**:[#87](https://github.com/u-stem/drowsy-unity/pull/87)、squash merged → commit `8de677f`(2026-05-14)
- **スコープ達成**:
  - `IDrowZzzGameView` interface 抽出(`Drowsy.Presentation.Games.DrowZzz` namespace、`Render` / `RenderOutcome` + 3 event)
  - `DrowZzzGamePresenter` Pure C# 実装(`IStartable, IDisposable`、ctor 6 引数 + null 防御、`Start` で event 配線 + Subject 購読 + `BootAsync.Forget`、`Dispose` で event 解除 + CTS / Disposables / Subject 解放 + 冪等性、Handler 3 種 + BootAsync 新規対戦経路は `NotImplementedException` stub)
  - `Drowsy.Presentation.Tests` asmdef 新設(`Drowsy.Application.Tests` 参照で Stub 再利用、`R3.dll` precompiledReferences 追加)
  - `MockDrowZzzGameView` / `MockUserSettings` / `MockDrowZzzGameSessionSerializer` 新設 + 契約テスト 12 件(PRES-002〜011 + PRES-013、PRES-008 は TestCase 2 件)
  - EARS `docs/specs/presentation/games/drowzzz/presenter-skeleton.{md,feature}` 新設(PRES-001〜013、新規 prefix `PRES-`)
  - `IdentityRandom` / `StubGameConfig` を internal → public 化(`Drowsy.Presentation.Tests` から再利用、`SessionFactory` が既に public な事実と整合)
- **検証結果**:
  - `dotnet build drowsy-unity.slnx`:0 警告 / 0 エラー / 1.66 秒
  - `bash scripts/check-traceability.sh`:仕様 ID 547 件 / Property ID 456 件 / 整合性 OK
  - lefthook pre-commit 全フック緑(2 commits 構成:`3e4fe64` ADR 完成記録 + `2ca6bb8` 実装本体)
  - Unity Test Runner EditMode 緑確認はオーナー側で実機実施
- **JIT 確定事項**:
  - `IDrowZzzGameView.Render` の引数は non-null(`DrowZzzGameSession?` にしない、本 ADR §3.1 + ADR-0015 NRT 不採用方針整合)
  - Presenter は `IStartable` 採用(別 `IBootable` interface は切らない、本 ADR §3.2 確定通り)
  - BootAsync 新規対戦経路は `NotImplementedException` stub(M5-PR4 で `StartGameUseCase.Execute(players, initialDeck)` の引数生成戦略を JIT 確定後に本実装、本 ADR §3.2 line 238 の TBD と整合)
  - `Drowsy.Presentation.Tests` から `Drowsy.Application.Tests` を参照(test asmdef 間依存、Stub 再利用、本番 Build 非混入、代替案の stubs 多重実装より DRY 性優先)
- **本 ADR への訂正**:
  - §10.1 の `Drowsy.Presentation.Tests` references リストを実態整合化(`Drowsy.Application.Tests` 追加 + 参照根拠の明記 + Mock リストを 3 種に整合)
- **code-reviewer 指摘の反映**:T-1(重大、`NewContext` 内 `using var` のスコープ外 Dispose)+ W-1 / W-2 / W-3 / S-3 / S-4 を本 PR 内で反映、S-1 / S-2 / W-4 / W-5 は本 PR 範囲外と判断
- **次 PR への引き継ぎ**:
  - M5-PR3 で `ProjectLifetimeScope` / `GameLifetimeScope` の `Configure` を実装し、Presenter / View / UseCase を Register
  - M5-PR3 の `DrowZzzGameView` MonoBehaviour が `IDrowZzzGameView` を実装、`RegisterComponentInHierarchy<DrowZzzGameView>()` で Game スコープに登録(M5-PR3 着手時 JIT 確定 2026-05-14)
  - `MockDrowZzzGameSessionSerializer` に `catch (Exception ex)` 経路(LogError)を駆動する `LoadBehavior` バリアントが未実装(code-reviewer S-2)、M5-PR4 以降で必要なら追加

#### M5-PR3 完成記録

- **PR**:[#88](https://github.com/u-stem/drowsy-unity/pull/88)、squash merged → commit `0a298cc`(2026-05-14)
- **スコープ達成**:
  - UI Toolkit UXML / USS skeleton(`Assets/_Project/UI/Games/DrowZzz/DrowZzzGame.uxml` / `.uss`、ラベル 3 + ボタン 3 の最小骨格)
  - `DrowZzzGameView` MonoBehaviour 実装(`IDrowZzzGameView` 実装、`[RequireComponent(typeof(UIDocument))]`、OnEnable で VisualElement query + button.clicked 配線 / OnDisable で対称解除、`Q<Button>()` の null ガード、Render / RenderOutcome は Debug.Log のみ、Play ボタンはプレースホルダ CardId 発火)
  - `ProjectLifetimeScope` の Configure 実装(`[SerializeField]` SO 2 種 + null チェック + RegisterInstance/Register 群:IGameConfig / ICardCatalog&lt;IEffect&gt; / セーブパス string / IRandomSource(時刻 seed XorShiftRandom)/ IDrowZzzGameSessionSerializer / IUserSettings / EffectInterpreter / DrowZzzRule)
  - `GameLifetimeScope` の Configure 実装(StartGameUseCase / ApplyActionUseCase / DrowZzzGamePresenter(`AsImplementedInterfaces().AsSelf()`)/ DrowZzzGameView(`RegisterComponentInHierarchy().AsImplementedInterfaces()`))
- **検証結果**:
  - `dotnet build drowsy-unity.slnx`:0 警告 / 0 エラー / 3.05 秒
  - `bash scripts/check-traceability.sh`:仕様 ID 547 件 / Property ID 456 件 / 整合性 OK
  - lefthook pre-commit 全フック緑(2 commits 構成:`a777f84` ADR 完成記録 + `6331b17` 実装本体)
  - Play モード動作確認(Main.unity Scene 作成 + GameObject 配置 + ボタン押下 → event 発火)はオーナー側で実機実施
- **JIT 確定事項**:
  - UXML / USS 配置先は `Assets/_Project/UI/Games/DrowZzz/`(`Data/` と並ぶトップレベル UI/ に集約、Phase 3 のローカライズ / テーマ拡張を見据える)
  - `DrowZzzGameView` の Register は `RegisterComponentInHierarchy<DrowZzzGameView>()`(`RegisterComponentInNewPrefab` は Phase 3 のリトライ UI で再評価)
  - EARS 追加なし:View は MonoBehaviour / Bootstrap 中心で単体テスト対象外(CLAUDE.md §6 / 本 ADR §10 / §13)
- **本 ADR への訂正**(code-reviewer 指摘):
  - §2 表に `EffectInterpreter` 行を追加(W-1、`DrowZzzRule` ctor の第 2 引数として明示登録が必要)
  - §7.2 スニペットの `IRandomSource` を `Register<,>` から `RegisterInstance` 形式に訂正 + `EffectInterpreter` 登録追加(W-1 / W-2)
  - §2 表の `IDrowZzzGameView` 備考「`[Inject]` で Presenter を受け取る」を「Presenter ctor 引数として解決、View 側に `[Inject]` 不要」に訂正(W-3、依存方向は Presenter → View)
- **code-reviewer 指摘の反映**:W-1 / W-2 / W-3(ADR 訂正)+ W-4(`Q<Button>()` null ガード)+ T-2 / T-3 を本 PR 内で反映、T-1(`GetComponent` 統一)/ T-4(`.meta` 末尾改行)は本 PR 範囲外と判断
- **次 PR への引き継ぎ**:
  - M5-PR4 で Presenter Handler 3 種 + `BootAsync` 新規対戦経路を本実装、`DrowZzzGameView.Render` を本実装(山札残数 / 手札 / 場札 / SDP / FDP / DDP / Round 表示)
  - `BootAsync` 新規対戦経路の `initialDeck` / `players` 生成は M5-PR4 着手時 JIT で確定(M5-PR4 で本 ADR §3.2 line 238 の TBD を解消)
  - オーナー側 Unity Editor 作業:`Main.unity` Scene 新規作成 + GameObject ツリー配置 + Inspector で SerializeField 割り当て(SO 2 種 + UXML Source Asset)

#### M5-PR4 完成記録

- **PR**:[#89](https://github.com/u-stem/drowsy-unity/pull/89)、squash merged → commit `0822e5d`(2026-05-14)
- **スコープ達成**(「Play モードで Draw → Play → EndTurn の 1 ラウンドが動く」を達成):
  - `ScriptableObjectCardCatalog` に `RegisteredCardIds` 列挙 API 追加(SO 具象専用拡張、`ICardCatalog<TEffect>` interface は不変、`_cache.Keys` の snapshot を返却)
  - `ProjectLifetimeScope` に新規対戦の `players` / `initialDeck` 構築 + `RegisterInstance`(`BuildPlayers`:N=2 ホットシート固定 + player ID const 化 / `BuildInitialDeck`:catalog 登録カードを `CopiesPerCardForM5Deck`(20)枚ずつ、catalog 空チェック付き、本物 56 枚デッキは Phase 3)
  - `DrowZzzGamePresenter` ctor 6 → 8 引数化(`IReadOnlyList<PlayerId> players` + `Pile initialDeck` 追加)+ Handler 3 種本実装(`TryApplyAction` 共通ヘルパー)+ `BootAsync` 新規対戦経路本実装(`StartGameUseCase.Execute(_players, _initialDeck)`、§3.2 line 238 の TBD 解消)
  - `DrowZzzGameView.Render` 本実装(Round / 現プレイヤー / TurnPhase / 山札・場札・捨て札枚数 / FDP・DDP・SDP・Total / 現プレイヤー手札を Label に反映)+ UXML 拡張(`points-label` / `hand-label`)+ `RaisePlayClicked` を手札先頭カード発火に変更
  - PresenterTests:8 引数化追従 + PRES-012 を Optional → 通常要件昇格 + PRES-014/015(ctor null 防御)/ PRES-016/017/018(Handler 正常・異常・Boot 未完了)追加(計 17 メソッド)
  - EARS `presenter-skeleton.{md,feature}` 更新(PRES-012 昇格 + PRES-014〜018 採番)
- **本 ADR §11 M5-PR4 行の主要成果物列の補足**(M5-PR4 code-reviewer S-6 反映):§11 表の M5-PR4「主要成果物」列には Presenter / View のみ記載されていたが、実態は Bootstrap 側にも `ProjectLifetimeScope.BuildPlayers` / `BuildInitialDeck` + `ScriptableObjectCardCatalog.RegisteredCardIds` の追加を含む(本完成記録を Single Source of Truth とする)。
- **検証結果**:
  - `dotnet build drowsy-unity.slnx`:0 警告 / 0 エラー / 2.27 秒
  - `bash scripts/check-traceability.sh`:仕様 ID 552 件 / Property ID 462 件 / 整合性 OK
  - lefthook pre-commit 全フック緑(2 commits 構成:`ba1679d` ADR 完成記録 + `90c0a69` 実装本体)
  - Play モードでの 1 ラウンド動作確認(Main.unity Scene 配置)はオーナー側で実機実施
- **JIT 確定事項**(M5-PR4 着手時 2026-05-14):
  - `initialDeck` / `players` は Bootstrap で構築し Presenter ctor 8 引数化(`ScriptableObjectCardCatalog.RegisteredCardIds` 追加)
  - `IsLegalMove` false 時は無反応 + `Debug.LogWarning`(`TryApplyAction` が `InvalidOperationException` を握りつぶす、ボタン disable / トーストは Phase 3)
  - `PlayCardAction` の手札選択は「手札[0] 自動選択」(View 側 `RaisePlayClicked` が直近 Render の手札先頭を発火)
- **本 ADR への訂正**:§3.2 の Presenter ctor スニペットを 8 引数化、`BootAsync` の `/* TBD: players / initialDeck */` を解消、`_savePath` 検査を統一パターンに(code-reviewer S-1)
- **実装後発覚 → ADR-0017 で修正**(2026-05-16):本 M5-PR4 で導入した `RegisterInstance<IReadOnlyList<PlayerId>>(BuildPlayers())` は VContainer 1.x の `CollectionInstanceProvider.Match` が `IReadOnlyList<T>` を予約型として扱う仕様により実質的に空配列に上書きされていたことが Unity Play モードでの M5 UI 実機検証で発覚。`PlayerRoster` wrapper record を Application 層に新設して回避(ADR-0017)。Presenter ctor の 7 番目引数も `IReadOnlyList<PlayerId> players` → `PlayerRoster roster` に変更。M5-PR4 の Status は Accepted のまま、後続 fix を Related で接続する(設計判断は維持、wrapper 経由で実装のみ差し替え)
- **code-reviewer 指摘の反映**:W-1〜W-5 + S-1 / S-2 / S-3 / S-4 / S-5 を本 PR 内で反映、S-6(§11 備考欄補足)は本完成記録で反映
- **次 PR への引き継ぎ**:
  - `SaveAsync` / `LoadAsync` は M5-PR1 で `UniTask.RunOnThreadPool` ラップ実装済 → M5-PR5 では実装変更なし、Infrastructure.Tests の round-trip テスト追加 + Auto-save 統合に集中
  - M5-PR5 で `HandleEndTurnClicked` に Auto-save(`SaveAsync`)を統合(`TryApplyAction` を bool 返却化、M5-PR5 着手時 JIT 確定 2026-05-14)

#### M5-PR5 完成記録

- **PR**:[#90](https://github.com/u-stem/drowsy-unity/pull/90)、squash merged → commit `ea6fdf2`(2026-05-14)
- **スコープ達成**:
  - `DrowZzzGamePresenter`:Auto-save 統合(`TryApplyAction` を void → bool 返却化、`HandleEndTurnClicked` が EndTurn 成功時のみ `AutoSaveAsync(_cts.Token).Forget()`、`HandleDrawClicked` / `HandlePlayClicked` は bool を discard)
  - `AutoSaveAsync`:`UniTaskVoid` + `Forget`、`_current` をローカルキャプチャ、`OperationCanceledException` 握りつぶし / それ以外は `Debug.LogError` でゲーム継続
  - `Drowsy.Infrastructure.Tests.asmdef` に `UniTask` reference 追加 + `DrowZzzGameSessionSerializerAsyncTests` 新設(INF-083〜087:SaveAsync / LoadAsync round-trip + 引数防御 + ファイル不在)
  - `DrowZzzGamePresenterTests` に Auto-save テスト 3 件追加(PRES-019/020/021)
  - EARS `drowzzz-game-session-serializer.{md,feature}`(INF-083〜087)+ `presenter-skeleton.{md,feature}`(PRES-019〜021)
- **追加修正(テストハング)**:M5-PR5 のテストが Unity Test Runner (EditMode) でハングする問題を同 PR 内で修正(commit `f9716bd`):`DrowZzzGamePresenterTests` の全 `async Task` テストを `void` 化 + `UniTask.Yield()` 全除去(MockSerializer は同期完了)、`DrowZzzGameSessionSerializerAsyncTests` の round-trip + INF-087 を `[UnityTest]` + `UniTask.ToCoroutine()` 化(実 Serializer の `RunOnThreadPool` が `async Task` + `.AsTask()` で EditMode デッドロックするため)。chore で Unity 自動生成の `Assets/_Recovery/` を `.gitignore` に追加(commit `b8ae408`)
- **検証結果**:
  - `dotnet build drowsy-unity.slnx`:0 警告 / 0 エラー
  - `bash scripts/check-traceability.sh`:仕様 ID 560 件 / Property ID 470 件 / 整合性 OK
  - lefthook pre-commit 全フック緑(4 commits 構成:`546b70b` ADR 完成記録 + `94df291` 実装本体 + `f9716bd` テストハング修正 + `b8ae408` chore)
  - Unity Test Runner EditMode:全テスト緑(オーナー実機確認済み、テストハング修正後)
- **JIT 確定事項**(M5-PR5 着手時 2026-05-14):
  - `SaveAsync` / `LoadAsync` は M5-PR1 の `UniTask.RunOnThreadPool` ラップを維持(WebGL 最適化は M5-PR8 検証 → 必要なら Phase 3)
  - Auto-save トリガーは `TryApplyAction` を bool 返却化
  - Auto-save 失敗時は `Debug.LogError` のみ、ゲーム継続(リトライ / ユーザー通知は Phase 3)
- **本 ADR への訂正**:§8 表の「アプリ起動時 … M5-PR5 で実装」を「M5-PR4 で実装済」に訂正(code-reviewer W-1)
- **code-reviewer 指摘の反映**:本体レビューで W-1 / W-2 + T-1 / T-2 / T-4 / T-5、テストハング修正レビューで T-3 を反映。テストハング修正レビューの T-1(PRES-019 の Given が長い)は `docs/todo.md` に切り出し
- **学び(memory 化済)**:Unity EditMode テストで UniTask を扱う 3 罠(`UniTask.Yield()` 使用不可 / `RunOnThreadPool` 実装は `[UnityTest]` + `ToCoroutine` / 同期 mock は `void` テスト)を `unity-editmode-unitask-test-patterns` として記録
- **次 PR への引き継ぎ**:
  - M5-PR6 で `IUserSettings` を View に直接 Inject(VContainer `[Inject]`)+ R3 Observable バインディング、`UserSettingsBinder` Pure C# 切り出しでテスタブル化(M5-PR6 着手時 JIT 確定 2026-05-14)

#### M5-PR6 完成記録

- **PR**:[#91](https://github.com/u-stem/drowsy-unity/pull/91)(M5-PR6 本体、squash merged → commit `f6f47ff`)+ [#92](https://github.com/u-stem/drowsy-unity/pull/92)(code-reviewer コメント反映漏れ修正、squash merged → commit `bd4740c`)、いずれも 2026-05-14
- **スコープ達成**:
  - `UserSettingsBinder` Pure C# クラス新設(`Slider` × 2 + `DropdownField` + `IUserSettings` を双方向バインド、UIDocument 非依存でテスタブル化、settings → UI は `Observable.Subscribe` + `SetValueWithoutNotify` で callback 抑止しループ防止、UI → settings は `RegisterValueChangedCallback`、`IDisposable` で対称解放 + 冪等)
  - UXML 拡張(`settings-section`:`bgm-slider` / `se-slider` / `language-dropdown`)+ USS 拡張
  - `DrowZzzGameView`:`[Inject] Construct(IUserSettings)` + `Start()` で `UserSettingsBinder` 生成 + `OnDestroy()` で Dispose(button.clicked は既存の `OnEnable`/`OnDisable` のまま、`[Inject]` 依存注入タイミングの都合で非対称)
  - `UserSettingsBinderTests`(PRES-022〜029:ctor null 防御 4 + choices 設定 + settings→UI Subscribe 経路 + Dispose 冪等、`new Slider()` + `MockUserSettings` で UIDocument 非依存テスト)
  - EARS `user-settings-binder.{md,feature}` 新設(PRES-022〜030、PRES-030 の UI→settings は `Slider.value` setter の `panel != null` 制約で EditMode 単体テスト不可のため `[Optional]` 手動 QA)
  - `LanguageCodes` 設計変更(`refactor(domain)`):カバレッジレポートで唯一の未カバーだった `static readonly Supported`(暗黙の `.cctor`)を、`Supported` 式本体プロパティ化 + `IsSupported` const 比較化で static ctor ごと廃止。`.cctor` は AppDomain 内 1 回のみ実行で計測区間外実行時に永久未カバーになる構造的限界の根本回避
- **検証結果**:
  - `dotnet build drowsy-unity.slnx`:0 警告 / 0 エラー
  - `bash scripts/check-traceability.sh`:仕様 ID 569 件 / Property ID 477 件 / 整合性 OK
  - lefthook pre-commit 全フック緑(PR #91 は 3 commits:`719f46b` 完成記録 + `322f9f7` refactor + `eef6afc` 実装、PR #92 は `52880e3` コメント反映漏れ修正)
  - Unity Test Runner / Play モード設定 UI 操作確認はオーナー側で実機実施
- **JIT 確定事項**(M5-PR6 着手時 2026-05-14):
  - `IUserSettings` を View に直接 `[Inject]`(ADR-0016 §11 初期推奨、Presenter の SRP を保つ)
  - Language Dropdown は `LanguageCodes.Supported`(`"ja"` / `"en"`)をコード直接表示(表示名マッピングは Phase 3)
  - バインディングは `UserSettingsBinder` Pure C# 切り出しで EditMode テスタブル化
  - `LanguageCodes` の `.cctor` 未カバーは設計変更(static ctor 廃止)で根本回避
- **運用上の学び**:`git commit -- <paths>`(ワークツリー内容を commit)と `git commit`(ファイル指定なし、ステージ済み内容を commit)の挙動差により、code-reviewer 反映 Edit 後の `git add -A` 忘れで W-2 / T-2 / T-5 のコメント 3 件が PR #91 から漏れ、PR #92 で追補。今後はレビュー反映 Edit 後に必ず `git add -A` してから commit する
- **code-reviewer 指摘の反映**:W-2 / T-1 / T-2 / T-5 反映(W-1 は現状で慣用パターン通り、T-3 対応不要、T-4 は設定 UI 常時表示の Phase 3 改善候補として PR description に記録)
- **次 PR への引き継ぎ**:
  - M5-PR7 で `GameOutcome` の UI 反映(`outcome-label` 新設、`RenderOutcome` 本実装)+ Outcome 確定後の入力 disable(View ボタン disable + Presenter event 無視の多層防御)+ Auto-save Final(メイン path 上書き、`TryApplyAction` 内に Auto-save 判定を集約し M5-PR5 の bool 返却を void に戻す)、M5-PR7 着手時 JIT 確定 2026-05-14

#### M5-PR7 完成記録

- **PR**:[#93](https://github.com/u-stem/drowsy-unity/pull/93)、squash merged → commit `06472f3`(2026-05-16)
- **スコープ達成**:
  - `DrowZzzGameView.RenderOutcome` 本実装(`outcome switch` で `WinnerOutcome winner => $"Winner: {winner.Winner.Value}"` / `DrawOutcome => "Draw"` の表示分岐、`outcome-label` を UXML に新規追加)+ Outcome 確定後の入力 disable(`DrowZzzGameView` が 3 ボタン disable + `DrowZzzGamePresenter.TryApplyAction` が `_current.IsTerminated` ガード = 多層防御、M5-PR7 着手時 JIT「View でボタン disable + Presenter で event 無視」確定 2026-05-14)
  - **Auto-save Final**:`TryApplyAction` 内に Auto-save 判定を集約(`if (action is EndTurnAction || next.IsTerminated) AutoSaveAsync(_cts.Token).Forget();`)、M5-PR5 で bool 返却にしていた `TryApplyAction` を void に戻して、Outcome 確定で終了した場合も Auto-save が EndTurn と同じ Save 経路に統合(メイン path 上書き、M5-PR7 着手時 JIT 確定 2026-05-14)
  - `Start()` の `SessionStream` 購読を `Render` + `IsTerminated → RenderOutcome` 分岐に拡張(BootAsync 復元で `IsTerminated` な session を受け取った場合も RenderOutcome が呼ばれる、PRES-031)
  - EARS `presenter-skeleton.md/.feature` 更新(PRES-031 / PRES-032 / PRES-033 新設、PRES-033 は `[Optional]` 手動 QA)、`DrowZzzGamePresenterTests` 拡張(計 19 メソッド、PRES-031 / PRES-032 ×2 追加 + 全テスト void 化済維持)
- **検証結果**:
  - `dotnet build drowsy-unity.slnx`:0 警告 / 0 エラー
  - lefthook pre-commit 全フック緑
  - Unity Test Runner / Play モード Outcome 表示確認はオーナー側で実機実施(本 PR 単独では Play 動作未達、後続 PR #95 / #96 で根本対処後に完全動作)
- **JIT 確定事項**(M5-PR7 着手時 2026-05-14):
  - Outcome 確定後の入力 disable:**View でボタン disable + Presenter で event 無視**(多層防御、片方の漏れでもう片方が catch)
  - Auto-save Final:**メイン path 上書きのみ**(別 path / 履歴保存は Phase 3)
  - `outcome-label`:**新規 UXML 要素**(`status-label` 流用ではなく独立要素、可視性とトレーサビリティ向上)
- **次 PR への引き継ぎ(当初計画)**:M5-PR8 で WebGL Build 検証 + Phase 2 完結処理(ADR-0016 §11「M5 完成後の Phase 進捗バナー更新案」適用)
- **実装後発覚 → 後続 PR で根本対処**:M5-PR7 マージ後の Unity Play モード実機検証で 2 件の根本問題が連鎖発覚し、M5-PR8(本 PR、後続)着手前に以下 3 PR が挟まった:
  1. **PR #94**(`docs/architecture/scene-setup.md` 手順書追加、squash `28d81f9`、2026-05-16):オーナー側 Main.unity Scene セットアップ手順の機械化、ADR-0016 §6 のシーン構造を実体化する作業の障害(scene 開いていない / コンポーネント取り違え / フォントエラー / Parent 設定漏れ等)を一つずつ解消
  2. **PR #95**(`fix/m5-player-roster-vcontainer-collection`、squash `49b2d77`、2026-05-16):ADR-0017 PlayerRoster wrapper 導入、VContainer 1.x の `CollectionInstanceProvider.Match` が `IReadOnlyList<T>` を予約型として扱い `RegisterInstance` を上書きする問題を回避(`StartGameUseCase` が空 players で `ArgumentException`「players は 1 人以上必要です」の根本原因)
  3. **PR #96**(`refactor/m4-cardtypeid-instance-id-separation`、squash `20ea1de`、2026-05-16):ADR-0018 `CardTypeId` 新設 + `CardId` を `(CardTypeId, int Instance)` 複合型に refactor、Hand の重複 CardId 検出エラー(`BuildInitialDeck` が同じ CardId を 20 枚並べる方針 vs Hand の unique 制約の不整合)を根本対処、Drowsy.Domain 100% カバレッジ達成

#### M5-PR8 起票(進行中、Phase 2 完結 PR)

- **PR**:TBD(本 ADR の commit 完了後に GitHub PR 作成、ブランチ `docs/m5-pr8-phase2-completion`)
- **着手日**:2026-05-16
- **スコープ(新規実装ゼロ、検証 + ドキュメント中心)**:
  - **WebGL Build 検証**(オーナー実機):Unity Editor + WebGL module で Build 実行 → `Result: Succeeded` 確認 → `Build/WebGL/` 生成物確認 → 結果(Build 時間、Result サマリ、生成ファイルサイズ)を `docs/architecture/webgl-il2cpp-verification.md` に追記。M4-PR7 時点の 59 秒実績との差異を確認(Hand 重複対処 / CardTypeId 分離後の Build 時間に影響があるか)
  - **Phase 2 完結処理(ドキュメント)**:
    - ADR-0016 §11 M5-PR8 完成記録(本セクションを完成記録に転換)
    - CLAUDE.md §11 Phase 進捗:M5 を「進行中」→「**完結**」、Phase 2 を「進行中」→「**完結**」に更新
    - README.md status banner 更新(Phase 2 完結バナー、`docs/adr/README.md` の最新 ADR 表ともに整合)
    - ADR-0005 §7 Phase 2 完了基準 5 軸チェック(本 PR 完成時点で全 5 軸達成見込み)
    - `docs/todo.md`:WebGL CI 整備 / Presenter C0 カバレッジ計測 / UI Toolkit `DataBinding` 切替評価 を「未着手」セクションへ追加(本 ADR §「TODO 候補」から todo.md へ移行)
  - **統合確認**:Unity Play モードで DrowZzz が Draw / Play / EndTurn / 設定 UI を全機能動作させられることを最終確認(Hand 重複検出エラーは PR #96 = ADR-0018 で根本解消済)
- **検証結果**:**WebGL Build 検証はオーナー実機作業、結果待ち(commit 2 段階構成:着手 commit でドキュメント整備 → push → 検証依頼 → 結果反映 commit で M5-PR8 完成記録に転換)**
- **JIT 確定事項**(M5-PR8 着手時 2026-05-16):
  - **commit 2 段階構成**:本 ADR 着手 commit でドキュメント整備 + 進行中ステータス → push → オーナーへ WebGL 検証依頼 → 結果反映 commit で M5-PR8 完成記録に転換 + Phase 2 完結マーク(WebGL 検証なしで Phase 2 完結を宣言しない、ADR-0005 §7 完了基準厳密遵守)
  - **本 PR で取り込んだ Scene Asset**(PR #96 末尾で実体化済):`Main.unity` / `DrowZzzPanelSettings.asset` / `UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss` / `EditorBuildSettings.asset` 修正(Build Settings に Main.unity 追加)+ SampleScene.unity 削除 → これらが揃った状態が WebGL Build 検証の前提
  - **Phase 3 着手判断**:本 PR 完結後の別 ADR(候補 ADR-0019)で Phase 3 ロードマップ(N>2 拡張 / 本格 UI / 世界観統合 / Networking 等)を起票、本 ADR では Out of Scope

### M5 完成後の Phase 進捗バナー更新案

M5-PR8 完成時点で CLAUDE.md §11「Phase 進捗」を以下に書き換え:

```
- **Phase 2**(DrowZzz 本命実装): **完結**(ADR-0005、M1〜M5 完成)
  - M1 〜 M4 は既存記述維持
  - **M5**(Bootstrap / Presentation): **完結**(ADR-0016、M5-PR1〜PR8 完成、VContainer LifetimeScope 2 階層構成 + UI Toolkit + R3 `Subject<T>` + UniTask 永続化、Phase 2 完了の最小定義(ADR-0005 §7)達成)
- **Phase 3**(N>2 拡張 / 本格 UI / 世界観統合 / Networking 等): 未着手(着手判断は Phase 2 完結後、別 ADR で再評価)
```

### TODO 候補(本 PR ではなく後続で扱う)

- WebGL Build CI 整備(GameCI 経由、`docs/todo.md` に登録予定、M5-PR8 完成後)
- Presenter 単体テストの C0 カバレッジ計測(`docs/testing-strategy.md` のカバレッジ目標 Presentation 行を「計測対象外」から「Presenter のみ計測対象」に格上げ検討)
- UI Toolkit `DataBinding`(Unity 2023+ API)への切替評価(Phase 3 候補)
- ADR-0014 §「再評価機会」項目:M5 Bootstrap で `StartGameUseCase` が `ICardCatalog<IEffect>` を必要としない事実が再確認できれば、ADR-0014 を Status `Accepted` のまま完結扱い(本 ADR が最終証跡)

これらが M5 着手中に消化されない場合は `docs/todo.md` への移行を検討する(ADR-0003 の運用)。

## Related

- 前提: [ADR-0001 ADR Operations](0001-adr-operations.md)
- 前提: [ADR-0002 Phase 1 Domain 拡張の集約境界と概念モデル](0002-phase1-domain-boundaries.md) — Domain ゲーム非依存原則、本 ADR §3 / §9 で踏襲
- 前提: [ADR-0003 TODO 運用](0003-todo-operations.md) — 後追い chore の追跡先
- 前提: [ADR-0004 IsExternalInit polyfill](0004-init-setter-polyfill.md) — `record + init + with` の前提、`PersistedSessionV1` 等が利用
- 前提: [ADR-0005 Phase 2 Roadmap](0005-phase2-roadmap-drowzzz.md) — M5 スコープ、Phase 2 完了の最小定義(§7)、本 ADR が回収
- 前提: [ADR-0006 M1 詳細](0006-m1-detail-application-interfaces.md) — Pure C# 哲学(§4)、UseCase 構成(§3)、namespace 規約(§1)を本 ADR が継承
- 前提: [ADR-0007 M2 カード効果](0007-m2-detail-card-effects.md) — `ICardCatalog<IEffect>` ジェネリック化、本 ADR §2 で DI 登録
- 前提: [ADR-0010 M3 終了判定 + 勝者決定](0010-m3-game-termination-and-victory-determination.md) — `GameOutcome` を本 ADR §3.1 で View Render 対象に追加
- 前提: [ADR-0011 M3 拡張 + 夢カード](0011-m3-dream-card-and-game-mechanics-expansion.md) — Session 10 引数化、本 ADR §5.2 で persistence 対象
- 前提: [ADR-0012 M4 SO 化 + 永続化 + ユーザー設定](0012-m4-scriptableobject-and-persistence.md) — M4-PR1〜PR6 で確立した SO Catalog / Serializer / IUserSettings を本 ADR が DI 統合、M4-PR7 完了後に本 ADR §「M5-PR1 着手前確認項目」を経て M5 着手
- 前提: [ADR-0013 Roslynator.Analyzers 導入](0013-roslynator-adoption.md) — 機械検知レイヤ、本 ADR §13 で整合性確認
- 前提: [ADR-0014 StartGameUseCase の CardCatalog 依存削除](0014-start-game-usecase-cardcatalog-removal.md) — 2 引数 ctor 化、本 ADR §2 で DI 登録の constructor injection を簡素化
- 前提: [ADR-0015 NRT 不採用](0015-nullable-reference-types-not-adopting.md) — 再評価条件第 1 項を本 ADR §12 で M5-PR8 完成時点に位置付け
- 後続: M5-PR1 〜 M5-PR8(本 ADR Implementation Notes §11 PR 分割計画)
- 後続: Phase 2 完結処理(M5-PR8 で README ステータスバナー / CLAUDE.md §11 / 本 ADR / ADR-0005 / ADR-0012 完成記録同梱)
- 関連: [`CLAUDE.md`](../../CLAUDE.md) §4 採用 lib(VContainer / UniTask / R3)/ §5 アーキテクチャ依存ルール / §11 ADR 運用 / §12 TODO 追跡
- 関連: [`docs/architecture/dependency-rules.md`](../architecture/dependency-rules.md) — Bootstrap → 全 4 層、Presentation → Application(本 ADR §9 で踏襲)
- 関連: [`docs/testing-strategy.md`](../testing-strategy.md) — Presenter 単体テスト追加(本 ADR §10)、カバレッジ目標 Presentation 行の再評価候補(本 ADR §「TODO 候補」)
