using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Presentation.Games.DrowZzz;
using static Drowsy.Application.Tests.Stubs.SessionFactory;

namespace Drowsy.Presentation.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="DrowZzzGamePresenter"/> の単体テスト(M5-PR2)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §3.2 / §11 M5-PR2 + §10.1 で確定したスコープ:
    /// <list type="bullet">
    /// <item>ctor null 防御 6 件(PRES-002〜007)+ savePath 空白防御 1 件(PRES-008)</item>
    /// <item>Start で View event 配線確認(PRES-009)</item>
    /// <item>Dispose で View event 解除確認(PRES-010)</item>
    /// <item>BootAsync 完了で Subject → View.Render パイプライン検証(PRES-011)</item>
    /// <item>Dispose 冪等性(PRES-013)</item>
    /// </list>
    /// <para>
    /// <b>UseCase 構築</b>:Presenter ctor は具象 <see cref="StartGameUseCase"/> / <see cref="ApplyActionUseCase"/>
    /// を取るため、本テストでは <c>Drowsy.Application.Tests.Stubs</c>(`IdentityRandom` / `StubGameConfig` /
    /// `SessionFactory.NewRule`)を再利用して両 UseCase を構築する(asmdef references で
    /// <c>Drowsy.Application.Tests</c> を参照、ADR-0016 §10.1 訂正反映)。
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class DrowZzzGamePresenterTests
    {
        /// <summary>標準的な依存セットで <see cref="DrowZzzGamePresenter"/> を構築する。</summary>
        private static PresenterContext NewContext(string savePath = "path/a")
        {
            var rng = new IdentityRandom();
            var config = new StubGameConfig();
            var startGameUseCase = new StartGameUseCase(rng, config);
            var applyActionUseCase = new ApplyActionUseCase(NewRule());
            var view = new MockDrowZzzGameView();
            var serializer = new MockDrowZzzGameSessionSerializer();
            // MockUserSettings の寿命は呼び出し元テストメソッドの ctx.UserSettings.Dispose() で管理する。
            // ここで using var を使うと return 直後にスコープ外 Dispose されて呼び出し側が Dispose 済み instance を
            // 参照する(code-reviewer T-1 反映、M5-PR2、M5-PR6 で Observable Subscribe テスト追加時の時限バグ予防)。
            var userSettings = new MockUserSettings();
            var presenter = new DrowZzzGamePresenter(
                startGameUseCase, applyActionUseCase, view, serializer, userSettings, savePath);
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

        // ---- PRES-002: startGameUseCase = null ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-002")]
        public void Given_startGameUseCaseNull_When_Ctor_Then_ArgumentNullException()
        {
            // Given
            var view = new MockDrowZzzGameView();
            var serializer = new MockDrowZzzGameSessionSerializer();
            using var userSettings = new MockUserSettings();
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            // When / Then
            Assert.That(
                () => new DrowZzzGamePresenter(null, applyActionUseCase, view, serializer, userSettings, "path/a"),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---- PRES-003: applyActionUseCase = null ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-003")]
        public void Given_applyActionUseCaseNull_When_Ctor_Then_ArgumentNullException()
        {
            // Given
            var view = new MockDrowZzzGameView();
            var serializer = new MockDrowZzzGameSessionSerializer();
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig());

            // When / Then
            Assert.That(
                () => new DrowZzzGamePresenter(startGameUseCase, null, view, serializer, userSettings, "path/a"),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---- PRES-004: view = null ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-004")]
        public void Given_viewNull_When_Ctor_Then_ArgumentNullException()
        {
            // Given
            var serializer = new MockDrowZzzGameSessionSerializer();
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig());
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            // When / Then
            Assert.That(
                () => new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, null, serializer, userSettings, "path/a"),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---- PRES-005: serializer = null ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-005")]
        public void Given_serializerNull_When_Ctor_Then_ArgumentNullException()
        {
            // Given
            var view = new MockDrowZzzGameView();
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig());
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            // When / Then
            Assert.That(
                () => new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, null, userSettings, "path/a"),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---- PRES-006: userSettings = null ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-006")]
        public void Given_userSettingsNull_When_Ctor_Then_ArgumentNullException()
        {
            // Given
            var view = new MockDrowZzzGameView();
            var serializer = new MockDrowZzzGameSessionSerializer();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig());
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            // When / Then
            Assert.That(
                () => new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, null, "path/a"),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---- PRES-007: savePath = null ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-007")]
        public void Given_savePathNull_When_Ctor_Then_ArgumentNullException()
        {
            // Given
            var view = new MockDrowZzzGameView();
            var serializer = new MockDrowZzzGameSessionSerializer();
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig());
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            // When / Then
            Assert.That(
                () => new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, userSettings, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---- PRES-008: savePath = empty / whitespace ----

        [TestCase("")]
        [TestCase("   ")]
        [Category("Small"), Category("Abnormal"), Property("Requirement", "PRES-008")]
        public void Given_savePathInvalid_When_Ctor_Then_ArgumentException(string savePath)
        {
            // Given
            var view = new MockDrowZzzGameView();
            var serializer = new MockDrowZzzGameSessionSerializer();
            using var userSettings = new MockUserSettings();
            var startGameUseCase = new StartGameUseCase(new IdentityRandom(), new StubGameConfig());
            var applyActionUseCase = new ApplyActionUseCase(NewRule());

            // When / Then(ArgumentException 厳密一致、ArgumentNullException は savePath = null 経路のみ)
            Assert.That(
                () => new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, userSettings, savePath),
                Throws.TypeOf<ArgumentException>());
        }

        // ---- PRES-009: Start で View event 配線確認 ----

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

        // ---- PRES-010: Dispose で View event 解除確認 ----

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

        // ---- PRES-011: BootAsync 完了 → View.Render パイプライン ----

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "PRES-011")]
        public async Task Given_started_When_BootAsyncCompletes_Then_ViewRenderInvokedWithLoadedSession()
        {
            // Given
            var ctx = NewContext();
            var expectedSession = NewSession();
            ctx.Serializer.LoadAsyncReturnSession = expectedSession;
            ctx.Serializer.LoadAsyncBehavior = MockDrowZzzGameSessionSerializer.LoadBehavior.ReturnSession;

            // When
            ctx.Presenter.Start();
            // MockSerializer.LoadAsync は UniTask.FromResult で同期完了するが、
            // UniTaskVoid の継続を確実に進めるため 1 frame だけ待つ
            await UniTask.Yield().ToUniTask();

            // Then(MockView.Render が 1 回呼ばれ、引数は LoadAsync が返した session)
            Assert.That(ctx.View.RenderedSessions, Has.Count.EqualTo(1));
            Assert.That(ctx.View.RenderedSessions[0], Is.SameAs(expectedSession));

            // Cleanup
            ctx.Presenter.Dispose();
            ctx.UserSettings.Dispose();
        }

        // ---- PRES-013: Dispose 冪等性 ----

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
    }
}
