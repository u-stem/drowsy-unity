using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Players;
using Drowsy.Domain.Random;

namespace Drowsy.Application.Tests.Integration
{
    /// <summary>
    /// M1 完成シナリオの end-to-end 統合テスト。
    /// <c>StartGameUseCase</c> → <c>ApplyActionUseCase</c> の連鎖を Draw → Play → EndTurn → Draw → ...
    /// で N=2 数ラウンド回し、組み合わせの正しさと累積の正しさを検証する(ADR-0006 §M1-PR7、ADR-0005 §M1 Definition of Done)。
    /// </summary>
    [TestFixture]
    public class M1IntegrationTests
    {
        // ===== ヘルパー =====

        private const int DeckSize = 30;
        private const int InitialHandSize = 5;

        private static IReadOnlyList<PlayerId> NewPlayers() =>
            new[] { PlayerId.Of("p1"), PlayerId.Of("p2") };

        private static Pile NewDeck(int count = DeckSize)
        {
            var cards = new CardId[count];
            for (int i = 0; i < count; i++)
            {
                cards[i] = CardId.Of($"c{i + 1}");
            }
            return new Pile(cards);
        }

        // (StartGameUseCase, ApplyActionUseCase) のペアを生成
        // M2-PR1: DrowZzzRule の依存追加 (catalog / interpreter) に追従。
        // ADR-0014: StartGameUseCase は ICardCatalog<IEffect> 依存を削除済(catalog は DrowZzzRule のみが受け取る)。
        private static (StartGameUseCase start, ApplyActionUseCase apply) NewUseCases(IRandomSource rng = null)
        {
            rng ??= new IdentityRandom();
            var catalog = new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]);
            var interpreter = new EffectInterpreter();
            var rule = new DrowZzzRule(catalog, interpreter);
            var config = new StubGameConfig();
            var start = new StartGameUseCase(rng, config);
            var apply = new ApplyActionUseCase(rule);
            return (start, apply);
        }

        // 1 フェーズ完走 (Draw → Play(現プレイヤー手札 Top カード) → EndTurn)
        private static DrowZzzGameSession PlayOnePhase(ApplyActionUseCase useCase, DrowZzzGameSession session)
        {
            session = useCase.Execute(session, new DrawCardAction());
            int currentIndex = session.GameState.Turn.CurrentPlayerIndex;
            var firstCard = session.GameState.Players[currentIndex].Hand.Cards[0];
            session = useCase.Execute(session, new PlayCardAction(firstCard));
            session = useCase.Execute(session, new EndTurnAction());
            return session;
        }

        // 指定回数フェーズを連続実行
        private static DrowZzzGameSession PlayPhases(
            ApplyActionUseCase useCase,
            DrowZzzGameSession session,
            int count)
        {
            for (int i = 0; i < count; i++)
            {
                session = PlayOnePhase(useCase, session);
            }
            return session;
        }

        // ===== DZ-077: StartGameUseCase 直後の状態 (4 不変条件を 1 テスト 1 アサーションで分離) =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-077")]
        public void Given_有効な引数_When_StartGameUseCase_Execute_Then_PhaseStateがWaitingForDraw()
        {
            var (start, _) = NewUseCases();
            var session = start.Execute(NewPlayers(), NewDeck());
            Assert.That(session.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForDraw));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-077")]
        public void Given_有効な引数_When_StartGameUseCase_Execute_Then_TurnNumberが1()
        {
            var (start, _) = NewUseCases();
            var session = start.Execute(NewPlayers(), NewDeck());
            Assert.That(session.GameState.Turn.TurnNumber, Is.EqualTo(1));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-077")]
        public void Given_有効な引数_When_StartGameUseCase_Execute_Then_CurrentPlayerIndexが0()
        {
            var (start, _) = NewUseCases();
            var session = start.Execute(NewPlayers(), NewDeck());
            Assert.That(session.GameState.Turn.CurrentPlayerIndex, Is.EqualTo(0));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-077")]
        public void Given_有効な引数_When_StartGameUseCase_Execute_Then_各プレイヤーHandが5枚()
        {
            var (start, _) = NewUseCases();
            var session = start.Execute(NewPlayers(), NewDeck());
            Assert.That(
                new[] { session.GameState.Players[0].Hand.Count, session.GameState.Players[1].Hand.Count },
                Is.EqualTo(new[] { InitialHandSize, InitialHandSize }));
        }

        // ===== DZ-078: 1 フェーズ完走の PhaseState 遷移 + Turn 進行 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-078")]
        public void Given_StartGame直後_When_Drawを実行_Then_WaitingForPlayに遷移()
        {
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            var s1 = apply.Execute(s0, new DrawCardAction());
            Assert.That(s1.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForPlay));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-078")]
        public void Given_Draw後_When_Playを実行_Then_WaitingForEndTurnに遷移()
        {
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            var s1 = apply.Execute(s0, new DrawCardAction());
            var card = s1.GameState.Players[0].Hand.Cards[0];
            var s2 = apply.Execute(s1, new PlayCardAction(card));
            Assert.That(s2.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForEndTurn));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-078")]
        public void Given_Play後_When_EndTurnを実行_Then_WaitingForDrawに戻る()
        {
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            var s3 = PlayOnePhase(apply, s0);
            Assert.That(s3.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForDraw));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-078")]
        public void Given_StartGame直後_When_1フェーズ完走_Then_TurnNumberが1増える()
        {
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            int before = s0.GameState.Turn.TurnNumber;
            var s3 = PlayOnePhase(apply, s0);
            Assert.That(s3.GameState.Turn.TurnNumber, Is.EqualTo(before + 1));
        }

        // ===== DZ-079: 1 ラウンド完走 (N=2 フェーズ) =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-079")]
        public void Given_StartGame直後_When_1ラウンド完走_Then_TurnNumberが2増える()
        {
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            int before = s0.GameState.Turn.TurnNumber;
            var sN = PlayPhases(apply, s0, 2);
            Assert.That(sN.GameState.Turn.TurnNumber, Is.EqualTo(before + 2));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-079")]
        public void Given_StartGame直後_When_1ラウンド完走_Then_CurrentPlayerIndexがプレイヤー0に戻る()
        {
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            var sN = PlayPhases(apply, s0, 2);
            Assert.That(sN.GameState.Turn.CurrentPlayerIndex, Is.EqualTo(0));
        }

        // ===== DZ-080: 3 ラウンド完走 (6 フェーズ) =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-080")]
        public void Given_StartGame直後_When_3ラウンド完走_Then_TurnNumberが6増える()
        {
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            int before = s0.GameState.Turn.TurnNumber;
            var sN = PlayPhases(apply, s0, 6);
            Assert.That(sN.GameState.Turn.TurnNumber, Is.EqualTo(before + 6));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-080")]
        public void Given_StartGame直後_When_3ラウンド完走_Then_FieldCountが6()
        {
            // 各フェーズで 1 枚 Play されるため、6 フェーズ後の Field は 6 枚累積
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            var sN = PlayPhases(apply, s0, 6);
            Assert.That(sN.GameState.Field.Count, Is.EqualTo(6));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-080")]
        public void Given_StartGame直後_When_3ラウンド完走_Then_各プレイヤーHandCountが5維持()
        {
            // 各フェーズで Draw 1 + Play 1 = ±0 のため、Hand.Count は初期 5 維持
            var (start, apply) = NewUseCases();
            var s0 = start.Execute(NewPlayers(), NewDeck());
            var sN = PlayPhases(apply, s0, 6);
            Assert.That(
                new[] { sN.GameState.Players[0].Hand.Count, sN.GameState.Players[1].Hand.Count },
                Is.EqualTo(new[] { InitialHandSize, InitialHandSize }));
        }

        // ===== DZ-081: Deterministic Replay (XorShiftRandom seed 一致で完全同一) =====

        [Test, Category("Medium"), Category("SemiNormal"), Property("Requirement", "DZ-081")]
        public void Given_同一引数と同一seed_When_M1完走を2回実行_Then_最終セッションが等価()
        {
            // Given(同一 players / deck と、同一 seed の rng を 2 つ)
            const int Seed = 12345;
            var (start1, apply1) = NewUseCases(rng: new XorShiftRandom(Seed));
            var (start2, apply2) = NewUseCases(rng: new XorShiftRandom(Seed));

            // When(各々で StartGame → 1 ラウンド完走)
            var sessionA = start1.Execute(NewPlayers(), NewDeck());
            sessionA = PlayPhases(apply1, sessionA, 2);

            var sessionB = start2.Execute(NewPlayers(), NewDeck());
            sessionB = PlayPhases(apply2, sessionB, 2);

            // Then
            Assert.That(sessionA, Is.EqualTo(sessionB));
        }
    }
}
