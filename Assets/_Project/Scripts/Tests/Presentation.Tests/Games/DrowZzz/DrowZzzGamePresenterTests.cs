using System;
using System.Collections.Generic;
using System.Linq;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using Drowsy.Presentation.Games.DrowZzz;
using NUnit.Framework;
using static Drowsy.Application.Tests.Stubs.SessionFactory;

namespace Drowsy.Presentation.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="DrowZzzGamePresenter"/> の単体テスト(M5-PR2 で骨格、M5-PR4 で Handler / 新規対戦経路、M5-PR5 で Auto-save)。
    /// </summary>
    /// <remarks>
    /// テストスコープ:
    /// <list type="bullet">
    /// <item>ctor null 防御 8 件(PRES-002〜007 + PRES-014 / PRES-015)+ savePath 空白防御 1 件(PRES-008)</item>
    /// <item>Start で View event 配線(PRES-009)/ Dispose で解除(PRES-010)/ Dispose 冪等(PRES-013)</item>
    /// <item>BootAsync 復元経路で Subject → View.Render(PRES-011)/ 新規対戦経路(PRES-012)</item>
    /// <item>Handler の正常 / 異常 / Boot 未完了パス(PRES-016 / PRES-017 / PRES-018)</item>
    /// <item>Auto-save(EndTurn 成功時のみ、PRES-019 / PRES-020 / PRES-021)</item>
    /// </list>
    /// <para>
    /// <b>UseCase 構築</b>:Presenter ctor は具象 <see cref="StartGameUseCase"/> / <see cref="ApplyActionUseCase"/>
    /// を取るため、本テストでは <c>Drowsy.Application.Tests.Stubs</c>(`IdentityRandom` / `StubGameConfig` /
    /// `SessionFactory.NewRule`)を再利用して両 UseCase を構築する。
    /// </para>
    /// <para>
    /// <b>BootAsync / AutoSaveAsync の同期完了(本テストが void で書ける理由)</b>:
    /// <see cref="MockDrowZzzGameSessionSerializer"/> の <c>LoadAsync</c> / <c>SaveAsync</c> は
    /// <c>UniTask.FromResult</c> / <c>UniTask.CompletedTask</c> / 同期 throw で <b>同期完了</b> する。
    /// <c>async UniTaskVoid</c> メソッド(<c>BootAsync</c> / <c>AutoSaveAsync</c>)は内部の全 <c>await</c> が
    /// 同期完了する場合、呼び出し元のスタック内で完全に実行されるため、<c>Start()</c> / <c>Fire*Clicked()</c> から
    /// 戻った時点で Boot / Auto-save は完了済み。よって本テストは <c>void</c> メソッドで書き、
    /// <c>UniTask.Yield()</c> 等の完了待ちは不要かつ <b>使ってはならない</b>:<c>UniTask.Yield()</c> は次フレームの
    /// 実行を Unity の PlayerLoop に依存するが、EditMode テストでは PlayerLoop が回らず次フレームが永久に来ないため
    /// テストがハングする(M5-PR5 で `async Task` + `UniTask.Yield()` を全廃して本構成に修正)。
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class DrowZzzGamePresenterTests
    {
        // ===== 共通ヘルパー =====

        /// <summary>
        /// M5 範囲の N=2 ホットシート players(<see cref="StartGameUseCase.Execute"/> 用、
        /// <see cref="IReadOnlyList{PlayerId}"/> を要求する API へ直接渡す)。
        /// </summary>
        private static PlayerId[] ValidPlayers() => new[] { PlayerId.Of("p1"), PlayerId.Of("p2") };

        /// <summary>
        /// M5 範囲の N=2 ホットシート roster(<see cref="PlayerRoster"/> wrapper)。
        /// <see cref="DrowZzzGamePresenter"/> ctor 用。<see cref="StartGameUseCase.Execute"/> へは
        /// <see cref="ValidPlayers"/> を使う(API が <see cref="IReadOnlyList{PlayerId}"/> を要求するため)。
        /// </summary>
        private static PlayerRoster ValidRoster() => new PlayerRoster(ValidPlayers());

        /// <summary>
        /// 配布(5 × 2 = 10 枚)+ 数ターン分の Draw を賄える有効な initialDeck(30 枚)。
        /// </summary>
        /// <remarks>
        /// 30 枚は <c>ProjectLifetimeScope.CopiesPerCardForM5Deck</c>(本番 Bootstrap の M5 簡易デッキ枚数)とは
        /// 独立したテスト固有の値。本テストは Handler の Draw を数回行えれば十分なため、本番デッキ構成に追従させず
        /// 固定 30 枚とする(code-reviewer S-3 反映)。
        /// </remarks>
        private static Pile ValidInitialDeck()
            => NewDeck(Enumerable.Range(0, 30).Select(i => $"c{i}").ToArray());

        /// <summary>標準的な依存セットで <see cref="DrowZzzGamePresenter"/> を構築する。</summary>
        /// <remarks>
        /// MockUserSettings の寿命は呼び出し元テストメソッドが <c>ctx.UserSettings.Dispose()</c> で管理する
        /// (本 helper 内で <c>using var</c> を使うと return 直後にスコープ外 Dispose される、code-reviewer T-1 反映)。
        /// </remarks>
        private static PresenterContext NewContext(string savePath = "path/a")
        {
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));
            var applyActionUseCase = new ApplyActionUseCase(NewRule());
            var view = new MockDrowZzzGameView();
            var serializer = new MockDrowZzzGameSessionSerializer();
            var userSettings = new MockUserSettings();
            var presenter = new DrowZzzGamePresenter(
                startGameUseCase, applyActionUseCase, view, serializer, userSettings, savePath,
                ValidRoster(), ValidInitialDeck());
            return new PresenterContext
            {
                Presenter = presenter,
                View = view,
                Serializer = serializer,
                UserSettings = userSettings,
                StartGameUseCase = startGameUseCase,
                ApplyActionUseCase = applyActionUseCase,
            };
        }

        private sealed class PresenterContext
        {
            public DrowZzzGamePresenter Presenter { get; set; }
            public MockDrowZzzGameView View { get; set; }
            public MockDrowZzzGameSessionSerializer Serializer { get; set; }
            public MockUserSettings UserSettings { get; set; }
            public StartGameUseCase StartGameUseCase { get; set; }
            public ApplyActionUseCase ApplyActionUseCase { get; set; }
        }

        /// <summary>
        /// 新規対戦の bootSession を Serializer に注入し、`Presenter.Start()` で Boot を完了させる
        /// (`MockDrowZzzGameSessionSerializer.LoadAsync` が同期完了するため Start() 戻り時点で Boot 済)。
        /// </summary>
        /// <remarks>
        /// Application 層の状態遷移知識(初期 PhaseState = WaitingForDraw)をテスト本体から除去するための共通 setup
        /// (`docs/todo.md`「Presenter テストのターン進行セットアップを共通ヘルパーへ切り出す」反映、2026-05-16)。
        /// </remarks>
        private static void Boot(PresenterContext ctx)
        {
            var bootSession = ctx.StartGameUseCase.Execute(ValidPlayers(), ValidInitialDeck());
            ctx.Serializer.LoadAsyncReturnSession = bootSession;
            ctx.Presenter.Start();
        }

        /// <summary>
        /// <see cref="Boot"/> 完了済の <paramref name="ctx"/> を Draw → Play まで進めて
        /// <see cref="DrowZzzPhaseState.WaitingForEndTurn"/> に到達させる。
        /// </summary>
        /// <remarks>
        /// 進行ロジックは Application 層の WaitingForDraw → Draw → WaitingForPlay → Play → WaitingForEndTurn の
        /// 状態遷移に依拠するため、Application 仕様が変わると本ヘルパー自身を更新する必要がある(テスト本体には
        /// 漏らさない、code-reviewer T-1 / docs/todo.md 「Presenter テストのターン進行セットアップを共通
        /// ヘルパーへ切り出す」反映 2026-05-16)。
        /// </remarks>
        private static void AdvanceToWaitingForEndTurn(PresenterContext ctx)
        {
            ctx.View.FireDrawClicked();
            var afterDraw = ctx.View.RenderedSessions[ctx.View.RenderedSessions.Count - 1];
            var currentPlayer = afterDraw.GameState.Players[afterDraw.GameState.Turn.CurrentPlayerIndex];
            ctx.View.FirePlayClicked(currentPlayer.Hand.Cards[0]);
        }

        // ===== PRES-002〜007 / PRES-014 / PRES-015: ctor null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-002")]
        public void Given_startGameUseCaseNull_When_Ctor_Then_ArgumentNullException()
        {
            using var userSettings = new MockUserSettings();
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            Assert.That(
                () => new DrowZzzGamePresenter(
                    null, applyActionUseCase, new MockDrowZzzGameView(), new MockDrowZzzGameSessionSerializer(),
                    userSettings, "path/a", ValidRoster(), ValidInitialDeck()),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-003")]
        public void Given_applyActionUseCaseNull_When_Ctor_Then_ArgumentNullException()
        {
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));

            Assert.That(
                () => new DrowZzzGamePresenter(
                    startGameUseCase, null, new MockDrowZzzGameView(), new MockDrowZzzGameSessionSerializer(),
                    userSettings, "path/a", ValidRoster(), ValidInitialDeck()),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-004")]
        public void Given_viewNull_When_Ctor_Then_ArgumentNullException()
        {
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            Assert.That(
                () => new DrowZzzGamePresenter(
                    startGameUseCase, applyActionUseCase, null, new MockDrowZzzGameSessionSerializer(),
                    userSettings, "path/a", ValidRoster(), ValidInitialDeck()),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-005")]
        public void Given_serializerNull_When_Ctor_Then_ArgumentNullException()
        {
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            Assert.That(
                () => new DrowZzzGamePresenter(
                    startGameUseCase, applyActionUseCase, new MockDrowZzzGameView(), null,
                    userSettings, "path/a", ValidRoster(), ValidInitialDeck()),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-006")]
        public void Given_userSettingsNull_When_Ctor_Then_ArgumentNullException()
        {
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            Assert.That(
                () => new DrowZzzGamePresenter(
                    startGameUseCase, applyActionUseCase, new MockDrowZzzGameView(),
                    new MockDrowZzzGameSessionSerializer(), null, "path/a", ValidRoster(), ValidInitialDeck()),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-007")]
        public void Given_savePathNull_When_Ctor_Then_ArgumentNullException()
        {
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            Assert.That(
                () => new DrowZzzGamePresenter(
                    startGameUseCase, applyActionUseCase, new MockDrowZzzGameView(),
                    new MockDrowZzzGameSessionSerializer(), userSettings, null, ValidRoster(), ValidInitialDeck()),
                Throws.TypeOf<ArgumentNullException>());
        }

        [TestCase("")]
        [TestCase("   ")]
        [Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-008")]
        public void Given_savePathInvalid_When_Ctor_Then_ArgumentException(string savePath)
        {
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            // ArgumentException 厳密一致(ArgumentNullException は savePath = null 経路のみ)
            Assert.That(
                () => new DrowZzzGamePresenter(
                    startGameUseCase, applyActionUseCase, new MockDrowZzzGameView(),
                    new MockDrowZzzGameSessionSerializer(), userSettings, savePath, ValidRoster(), ValidInitialDeck()),
                Throws.TypeOf<ArgumentException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-014")]
        public void Given_rosterNull_When_Ctor_Then_ArgumentNullException()
        {
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            Assert.That(
                () => new DrowZzzGamePresenter(
                    startGameUseCase, applyActionUseCase, new MockDrowZzzGameView(),
                    new MockDrowZzzGameSessionSerializer(), userSettings, "path/a", null, ValidInitialDeck()),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-015")]
        public void Given_initialDeckNull_When_Ctor_Then_ArgumentNullException()
        {
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig(), new InMemoryCardCatalog(Array.Empty<KeyValuePair<CardTypeId, CardData>>()));
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            Assert.That(
                () => new DrowZzzGamePresenter(
                    startGameUseCase, applyActionUseCase, new MockDrowZzzGameView(),
                    new MockDrowZzzGameSessionSerializer(), userSettings, "path/a", ValidRoster(), null),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ===== PRES-009 / PRES-010: Start / Dispose の event 配線・解除 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-009")]
        public void Given_constructed_When_Start_Then_ViewEventsHaveSubscribers()
        {
            // Given
            var ctx = NewContext();
            ctx.Serializer.LoadAsyncReturnSession = NewSession();

            // When
            ctx.Presenter.Start();

            // Then(3 event いずれも購読者 1)
            Assert.That(ctx.View.OnDrawClickedSubscriberCount, Is.EqualTo(1));
            Assert.That(ctx.View.OnPlayClickedSubscriberCount, Is.EqualTo(1));
            Assert.That(ctx.View.OnEndTurnClickedSubscriberCount, Is.EqualTo(1));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-010")]
        public void Given_started_When_Dispose_Then_ViewEventsHaveNoSubscribers()
        {
            // Given
            var ctx = NewContext();
            ctx.Serializer.LoadAsyncReturnSession = NewSession();
            ctx.Presenter.Start();

            // When
            ctx.Presenter.Dispose();

            // Then(3 event いずれも購読者 0)
            Assert.That(ctx.View.OnDrawClickedSubscriberCount, Is.EqualTo(0));
            Assert.That(ctx.View.OnPlayClickedSubscriberCount, Is.EqualTo(0));
            Assert.That(ctx.View.OnEndTurnClickedSubscriberCount, Is.EqualTo(0));

            // Cleanup
            ctx.UserSettings.Dispose();
        }

        // ===== PRES-011 / PRES-012: BootAsync 復元経路 / 新規対戦経路 =====
        // MockSerializer は同期完了するため Start() 直後に BootAsync は完了済み(クラス xmldoc §「同期完了」参照)。

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-011")]
        public void Given_started_When_BootAsyncCompletes_Then_ViewRenderInvokedWithLoadedSession()
        {
            // Given
            var ctx = NewContext();
            var expectedSession = NewSession();
            ctx.Serializer.LoadAsyncReturnSession = expectedSession;
            ctx.Serializer.LoadAsyncBehavior = MockDrowZzzGameSessionSerializer.LoadBehavior.ReturnSession;

            // When
            ctx.Presenter.Start();

            // Then(MockView.Render が 1 回呼ばれ、引数は LoadAsync が返した session)
            Assert.That(ctx.View.RenderedSessions, Has.Count.EqualTo(1));
            Assert.That(ctx.View.RenderedSessions[0], Is.SameAs(expectedSession));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-012")]
        public void Given_loadAsyncFileNotFound_When_Boot_Then_NewGameStartedAndRendered()
        {
            // Given(セーブファイル不在 → 新規対戦経路)
            var ctx = NewContext();
            ctx.Serializer.LoadAsyncBehavior = MockDrowZzzGameSessionSerializer.LoadBehavior.ThrowFileNotFound;

            // When
            ctx.Presenter.Start();

            // Then(StartGameUseCase.Execute(players, initialDeck) で生成した session が 1 回 Render される)
            Assert.That(ctx.View.RenderedSessions, Has.Count.EqualTo(1));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        // ===== PRES-013: Dispose 冪等性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-013")]
        public void Given_disposed_When_DisposeAgain_Then_NoException()
        {
            // Given
            var ctx = NewContext();
            ctx.Serializer.LoadAsyncReturnSession = NewSession();
            ctx.Presenter.Start();
            ctx.Presenter.Dispose();

            // When / Then(2 回目の Dispose は silent no-op、例外を投げない)
            Assert.That(() => ctx.Presenter.Dispose(), Throws.Nothing);

            // Cleanup
            ctx.UserSettings.Dispose();
        }

        // ===== PRES-016 / PRES-017 / PRES-018: Handler の正常 / 異常 / Boot 未完了 =====

        // 本テストは「Handler → ApplyActionUseCase → Subject → View.Render のパイプライン疎通」のみを確認する。
        // Draw 後の session 内容の正しさ(PhaseState が WaitingForPlay に遷移する等)は ApplyActionUseCase /
        // DrowZzzRule の責務であり Application.Tests が担保する(code-reviewer S-4 反映、関心の分離)。
        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-016")]
        public void Given_bootCompleted_When_DrawClicked_Then_SessionUpdatedAndRendered()
        {
            // Given(StartGameUseCase で本物の初期 session = WaitingForDraw を Boot で復元)
            var ctx = NewContext();
            var bootSession = ctx.StartGameUseCase.Execute(ValidPlayers(), ValidInitialDeck());
            ctx.Serializer.LoadAsyncReturnSession = bootSession;
            ctx.Presenter.Start();
            var renderCountAfterBoot = ctx.View.RenderedSessions.Count;

            // When(合法な Draw)
            ctx.View.FireDrawClicked();

            // Then(Draw が適用され OnNext が追加発火、Render が 1 回増える)
            Assert.That(ctx.View.RenderedSessions.Count, Is.EqualTo(renderCountAfterBoot + 1));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-017")]
        public void Given_bootCompleted_When_IllegalEndTurnClicked_Then_NoReaction()
        {
            // Given(初期 session は WaitingForDraw、EndTurn は WaitingForEndTurn でのみ合法 → 不合法手)
            var ctx = NewContext();
            var bootSession = ctx.StartGameUseCase.Execute(ValidPlayers(), ValidInitialDeck());
            ctx.Serializer.LoadAsyncReturnSession = bootSession;
            ctx.Presenter.Start();
            var renderCountAfterBoot = ctx.View.RenderedSessions.Count;

            // When(不合法手:WaitingForDraw で EndTurn)
            ctx.View.FireEndTurnClicked();

            // Then(無反応、Render は増えない)
            Assert.That(ctx.View.RenderedSessions.Count, Is.EqualTo(renderCountAfterBoot));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-018")]
        public void Given_bootIncomplete_When_DrawClicked_Then_NoExceptionAndNoRender()
        {
            // Given(LoadAsync が OperationCanceledException → BootAsync が _current をセットできず Boot 未完了)
            var ctx = NewContext();
            ctx.Serializer.LoadAsyncBehavior =
                MockDrowZzzGameSessionSerializer.LoadBehavior.ThrowOperationCanceled;
            ctx.Presenter.Start();

            // When(Boot 未完了で Handler 発火)
            ctx.View.FireDrawClicked();

            // Then(例外を投げず、Render も発火しない)
            Assert.That(ctx.View.RenderedSessions, Is.Empty);

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        // ===== PRES-019 / PRES-020 / PRES-021: Auto-save(EndTurn 後のみ)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-019")]
        public void Given_bootCompleted_When_LegalEndTurnClicked_Then_AutoSaveInvoked()
        {
            // Given(Draw → Play を経て WaitingForEndTurn にしてから EndTurn する)
            var ctx = NewContext();
            Boot(ctx);
            AdvanceToWaitingForEndTurn(ctx);
            var saveCountBeforeEndTurn = ctx.Serializer.SaveAsyncCallCount;

            // When(合法な EndTurn)
            ctx.View.FireEndTurnClicked();

            // Then(EndTurn 成功時のみ Auto-save、SaveAsync が 1 回呼ばれる)
            Assert.That(ctx.Serializer.SaveAsyncCallCount, Is.EqualTo(saveCountBeforeEndTurn + 1));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-020")]
        public void Given_bootCompleted_When_IllegalEndTurnClicked_Then_AutoSaveNotInvoked()
        {
            // Given(初期 session は WaitingForDraw、EndTurn は不合法手)
            var ctx = NewContext();
            Boot(ctx);

            // When(不合法な EndTurn)
            ctx.View.FireEndTurnClicked();

            // Then(TryApplyAction が false を返し Auto-save はトリガーされない)
            Assert.That(ctx.Serializer.SaveAsyncCallCount, Is.EqualTo(0));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-021")]
        public void Given_bootCompleted_When_DrawClicked_Then_AutoSaveNotInvoked()
        {
            // Given(Auto-save は EndTurn 後のみ、Draw / Play では行わない)
            var ctx = NewContext();
            Boot(ctx);

            // When(合法な Draw — Auto-save 対象外)
            ctx.View.FireDrawClicked();

            // Then(Draw では Auto-save しない)
            Assert.That(ctx.Serializer.SaveAsyncCallCount, Is.EqualTo(0));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        // ===== PRES-031 / PRES-032: Outcome 確定の UI 反映と終了後の入力 disable =====

        // DrowZzzGameSession は sealed record で Outcome は init setter のため、with 式で IsTerminated な
        // session を組み立てられる(本物 Rule で 21 ターン回す / 早期勝利カードをプレイする統合経路は不要)。
        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-031")]
        public void Given_terminatedSession_When_Boot_Then_RenderOutcomeInvoked()
        {
            // Given(Outcome 確定済み = IsTerminated の session を Boot で復元)
            var ctx = NewContext();
            var terminatedSession = NewSession() with { Outcome = new WinnerOutcome(PlayerId.Of("p1")) };
            ctx.Serializer.LoadAsyncReturnSession = terminatedSession;

            // When
            ctx.Presenter.Start();

            // Then(SessionStream 購読が IsTerminated を検出して RenderOutcome を 1 回呼ぶ)
            Assert.That(ctx.View.RenderedOutcomes, Has.Count.EqualTo(1));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-032")]
        public void Given_terminatedSession_When_DrawClicked_Then_NoReaction()
        {
            // Given(Outcome 確定済みの session を Boot で復元)
            var ctx = NewContext();
            var terminatedSession = NewSession() with { Outcome = new WinnerOutcome(PlayerId.Of("p1")) };
            ctx.Serializer.LoadAsyncReturnSession = terminatedSession;
            ctx.Presenter.Start();
            var renderCountAfterBoot = ctx.View.RenderedSessions.Count;

            var outcomeCountAfterBoot = ctx.View.RenderedOutcomes.Count;

            // When(終了後の Handler 発火)
            ctx.View.FireDrawClicked();

            // Then(TryApplyAction の IsTerminated ガードで無反応、Render も RenderOutcome も増えない)
            Assert.That(ctx.View.RenderedSessions.Count, Is.EqualTo(renderCountAfterBoot));
            Assert.That(ctx.View.RenderedOutcomes.Count, Is.EqualTo(outcomeCountAfterBoot));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }
    }
}
