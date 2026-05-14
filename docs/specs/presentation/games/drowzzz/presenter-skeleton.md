# DrowZzzGamePresenter (Presentation 層 Presenter 骨格) (M5-PR2)

このファイルは `DrowZzzGamePresenter` 骨格の契約を EARS で記述する(M5-PR2 範囲)。
ADR-0016 §3.2「Presenter」+ §11「PR 分割計画」M5-PR2 行で確定したスコープに対応する。

Handler 3 種(`HandleDrawClicked` / `HandlePlayClicked` / `HandleEndTurnClicked`)は M5-PR2 では
`NotImplementedException` stub のため本仕様の対象外、M5-PR4 で別途 EARS 要件として追加する。

配置先: `docs/specs/presentation/games/drowzzz/presenter-skeleton.md`

---

## 概要

`Drowsy.Presentation.Games.DrowZzz.DrowZzzGamePresenter` は MVP の P(Pure C#)。VContainer の
`IStartable` / `IDisposable` を実装し、`Start` で View event 配線 + `BootAsync.Forget()` を行い、
`Dispose` で event 解除 + CTS Cancel + R3 Subject 解放 + CompositeDisposable Dispose を行う。
状態公開は `Observable<DrowZzzGameSession> SessionStream`(`Subject<T>` ベース、Boot 完了後のみ `OnNext`)。

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

## 事象駆動要件 (Event-driven)

- [PRES-009] When `Start()` is invoked, the Presenter shall subscribe to the View's `OnDrawClicked`, `OnPlayClicked`, and `OnEndTurnClicked` events (1 subscriber each).
- [PRES-010] When `Dispose()` is invoked, the Presenter shall unsubscribe its handlers from the View's three click events (subscriber count returns to 0).
- [PRES-011] When `Start()` triggers `BootAsync` and `LoadAsync` returns a session, the Presenter shall propagate the session to the View via `Render(session)` (verified by Subject → View pipeline).
- [PRES-013] When `Dispose()` is invoked twice, the Presenter shall be idempotent (the second call shall be a silent no-op).

## 任意要件 (Optional)

- [PRES-012] [Optional] Where `BootAsync` encounters `LoadAsync` failing with `FileNotFoundException` (new-game path), the Presenter shall log a stub warning and skip session OnNext (M5-PR2 stub; M5-PR4 replaces this with `StartGameUseCase.Execute(players, initialDeck)` once arguments are JIT-confirmed).

## 関連

- 確定 ADR: [ADR-0016 §3.2 Presenter](../../../adr/0016-m5-bootstrap-presentation.md) / §11 PR 分割計画 M5-PR2 / §「各 PR 着手時の JIT 確認項目」
- 関連 ADR: [ADR-0006 §4 Pure C# 哲学](../../../adr/0006-m1-detail-application-interfaces.md)、[ADR-0014 StartGameUseCase の CardCatalog 依存削除](../../../adr/0014-start-game-usecase-cardcatalog-removal.md)(BootAsync 新規対戦経路の引数決定が M5-PR4 で発生)
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
| PRES-012 | (テスト免除: Optional) | stub 経路、本実装は M5-PR4 で追加 |
| PRES-013 | Given_disposed_When_DisposeAgain_Then_NoException | Normal(冪等性) |
