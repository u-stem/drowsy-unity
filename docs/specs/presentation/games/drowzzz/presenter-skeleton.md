# DrowZzzGamePresenter (Presentation 層 Presenter) (M5-PR2 骨格 / M5-PR4 Handler 拡張)

このファイルは `DrowZzzGamePresenter` の契約を EARS で記述する。
ADR-0016 §3.2「Presenter」+ §11「PR 分割計画」M5-PR2〜PR4 で確定したスコープに対応する。

M5-PR2 で骨格(ctor 防御 / Start / Dispose / BootAsync 復元経路)を、M5-PR4 で Handler 3 種 +
BootAsync 新規対戦経路 + ctor 引数 `players` / `initialDeck` の防御を追加した。

配置先: `docs/specs/presentation/games/drowzzz/presenter-skeleton.md`

---

## 概要

`Drowsy.Presentation.Games.DrowZzz.DrowZzzGamePresenter` は MVP の P(Pure C#)。VContainer の
`IStartable` / `IDisposable` を実装し、`Start` で View event 配線 + `BootAsync.Forget()` を行い、
`Dispose` で event 解除 + CTS Cancel + R3 Subject 解放 + CompositeDisposable Dispose を行う。
状態公開は `Observable<DrowZzzGameSession> SessionStream`(`Subject<T>` ベース、Boot 完了後のみ `OnNext`)。
ctor は 8 引数(`StartGameUseCase` / `ApplyActionUseCase` / `IDrowZzzGameView` / `IDrowZzzGameSessionSerializer` /
`IUserSettings` / `string savePath` / `IReadOnlyList<PlayerId> players` / `Pile initialDeck`)。

## 普遍要件 (Ubiquitous)

- [PRES-001] [Ubiquitous] The `DrowZzzGamePresenter` shall implement `VContainer.Unity.IStartable` and `System.IDisposable`.

## 異常要件 (Unwanted) — ctor 引数検査

- [PRES-002] If the ctor is called with `startGameUseCase = null`, then the Presenter shall throw `ArgumentNullException`.
- [PRES-003] If the ctor is called with `applyActionUseCase = null`, then the Presenter shall throw `ArgumentNullException`.
- [PRES-004] If the ctor is called with `view = null`, then the Presenter shall throw `ArgumentNullException`.
- [PRES-005] If the ctor is called with `serializer = null`, then the Presenter shall throw `ArgumentNullException`.
- [PRES-006] If the ctor is called with `userSettings = null`, then the Presenter shall throw `ArgumentNullException`.
- [PRES-007] If the ctor is called with `savePath = null`, then the Presenter shall throw `ArgumentNullException`.
- [PRES-008] If the ctor is called with `savePath` that is empty or whitespace-only, then the Presenter shall throw `ArgumentException`.
- [PRES-014] If the ctor is called with `players = null`, then the Presenter shall throw `ArgumentNullException`.
- [PRES-015] If the ctor is called with `initialDeck = null`, then the Presenter shall throw `ArgumentNullException`.

## 異常要件 (Unwanted) — Handler の不正状態

- [PRES-017] If a Handler receives a click that is an illegal move (`ApplyActionUseCase.Execute` throws `InvalidOperationException`), then the Presenter shall swallow the exception, log a warning, and leave the session unchanged (no Render).
- [PRES-018] If a Handler is invoked before boot completes (`Current` is null), then the Presenter shall log a warning and take no action (no exception, no Render).

## 事象駆動要件 (Event-driven)

- [PRES-009] When `Start()` is invoked, the Presenter shall subscribe to the View's `OnDrawClicked`, `OnPlayClicked`, and `OnEndTurnClicked` events (1 subscriber each).
- [PRES-010] When `Dispose()` is invoked, the Presenter shall unsubscribe its handlers from the View's three click events (subscriber count returns to 0).
- [PRES-011] When `Start()` triggers `BootAsync` and `LoadAsync` returns a session, the Presenter shall propagate the session to the View via `Render(session)` (verified by Subject → View pipeline).
- [PRES-012] When `BootAsync` encounters `LoadAsync` failing with `FileNotFoundException`, the Presenter shall start a new game via `StartGameUseCase.Execute(players, initialDeck)` and propagate the resulting session to the View via `Render`.
- [PRES-013] When `Dispose()` is invoked twice, the Presenter shall be idempotent (the second call shall be a silent no-op).
- [PRES-016] When a Handler receives a click after boot completes and the move is legal, the Presenter shall apply the corresponding `DrowZzzAction` via `ApplyActionUseCase` and propagate the updated session to the View via `Render`.

## 関連

- 確定 ADR: [ADR-0016 §3.2 Presenter](../../../adr/0016-m5-bootstrap-presentation.md) / §11 PR 分割計画 M5-PR2〜PR4 / §「各 PR 着手時の JIT 確認項目」
- 関連 ADR: [ADR-0006 §4 Pure C# 哲学](../../../adr/0006-m1-detail-application-interfaces.md)、[ADR-0014 StartGameUseCase の CardCatalog 依存削除](../../../adr/0014-start-game-usecase-cardcatalog-removal.md)
- 実装: `Assets/_Project/Scripts/Presentation/Games/DrowZzz/DrowZzzGamePresenter.cs`
- 実装 (View interface): `Assets/_Project/Scripts/Presentation/Games/DrowZzz/IDrowZzzGameView.cs`
- テスト: `Assets/_Project/Scripts/Tests/Presentation.Tests/Games/DrowZzz/DrowZzzGamePresenterTests.cs`
- モック群: `Assets/_Project/Scripts/Tests/Presentation.Tests/Games/DrowZzz/Mock*.cs`
- シナリオ: `presenter-skeleton.feature`(同ディレクトリ)

## トレーサビリティ

| 要件 ID | カバーするテスト | 備考 |
| ---- | ---- | ---- |
| PRES-001 | (テスト免除: Ubiquitous) | interface 実装はコンパイル時保証 |
| PRES-002 | Given_startGameUseCaseNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-003 | Given_applyActionUseCaseNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-004 | Given_viewNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-005 | Given_serializerNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-006 | Given_userSettingsNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-007 | Given_savePathNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-008 | Given_savePathInvalid_When_Ctor_Then_ArgumentException | Abnormal(empty / whitespace) |
| PRES-009 | Given_constructed_When_Start_Then_ViewEventsHaveSubscribers | Normal |
| PRES-010 | Given_started_When_Dispose_Then_ViewEventsHaveNoSubscribers | Normal |
| PRES-011 | Given_started_When_BootAsyncCompletes_Then_ViewRenderInvokedWithLoadedSession | Normal(UniTask 完了同期) |
| PRES-012 | Given_loadAsyncFileNotFound_When_Boot_Then_NewGameStartedAndRendered | Normal(M5-PR4 で本実装、Optional 解除) |
| PRES-013 | Given_disposed_When_DisposeAgain_Then_NoException | Normal(冪等性) |
| PRES-014 | Given_playersNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-015 | Given_initialDeckNull_When_Ctor_Then_ArgumentNullException | Abnormal |
| PRES-016 | Given_bootCompleted_When_DrawClicked_Then_SessionUpdatedAndRendered | Normal(Draw 代表、TryApplyAction 共通経路) |
| PRES-017 | Given_bootCompleted_When_IllegalEndTurnClicked_Then_NoReaction | Abnormal(不合法手) |
| PRES-018 | Given_bootIncomplete_When_DrawClicked_Then_NoExceptionAndNoRender | Abnormal(Boot 未完了) |
