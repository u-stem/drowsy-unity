using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    [TestFixture]
    public class DrowZzzGameSessionTests
    {
        // ===== ヘルパー =====

        private static PlayerState Player(string id) =>
            new PlayerState(PlayerId.Of(id), Hand.Empty);

        private static GameState NewGameState(
            IReadOnlyList<PlayerState> players = null,
            int turnNumber = 1,
            int currentPlayerIndex = 0)
        {
            return new GameState(
                players ?? new[] { Player("p1"), Player("p2") },
                Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                new TurnState(turnNumber, currentPlayerIndex));
        }

        private static IReadOnlyDictionary<PlayerId, int> Fdp(params (string id, int value)[] pairs)
        {
            var d = new Dictionary<PlayerId, int>();
            foreach (var (id, v) in pairs)
            {
                d[PlayerId.Of(id)] = v;
            }
            return d;
        }

        // ===== DZ-006: コンストラクタの値保持(1 テスト 1 アサーションで 3 プロパティ分離) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-006")]
        public void Given_有効な引数_When_DrowZzzGameSessionを生成_Then_GameStateが入力と一致する()
        {
            // Given
            var gs = NewGameState();
            // When
            var session = new DrowZzzGameSession(gs, Fdp(("p1", 0), ("p2", 10)), DrowZzzTurnPhase.WaitingForDraw);
            // Then
            Assert.That(session.GameState, Is.SameAs(gs));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-006")]
        public void Given_有効な引数_When_DrowZzzGameSessionを生成_Then_FirstDrowsyPointsが入力と一致する()
        {
            // Given
            var fdp = Fdp(("p1", 0), ("p2", 10));
            // When
            var session = new DrowZzzGameSession(NewGameState(), fdp, DrowZzzTurnPhase.WaitingForDraw);
            // Then
            Assert.That(session.FirstDrowsyPoints, Is.EquivalentTo(fdp));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-006")]
        public void Given_有効な引数_When_DrowZzzGameSessionを生成_Then_TurnPhaseが入力と一致する()
        {
            // When
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 0), ("p2", 10)),
                DrowZzzTurnPhase.WaitingForPlay);
            // Then
            Assert.That(session.TurnPhase, Is.EqualTo(DrowZzzTurnPhase.WaitingForPlay));
        }

        // ===== DZ-007 / DZ-008: null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-007")]
        public void Given_GameStateにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DrowZzzGameSession(null, Fdp(("p1", 0), ("p2", 10)), DrowZzzTurnPhase.WaitingForDraw));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-008")]
        public void Given_FirstDrowsyPointsにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DrowZzzGameSession(NewGameState(), null, DrowZzzTurnPhase.WaitingForDraw));
        }

        // ===== DZ-009: cross-field 検証(キー集合不一致) =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-009")]
        public void Given_FirstDrowsyPointsのキー数がPlayersより少ない_When_DrowZzzGameSessionを生成_Then_ArgumentExceptionを投げる()
        {
            // Given(Players は p1 / p2、FDP は p1 のみ)
            var gs = NewGameState();
            var fdp = Fdp(("p1", 0));
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                new DrowZzzGameSession(gs, fdp, DrowZzzTurnPhase.WaitingForDraw));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-009")]
        public void Given_FirstDrowsyPointsのキーがPlayersと部分的に異なる_When_DrowZzzGameSessionを生成_Then_ArgumentExceptionを投げる()
        {
            // Given(Players は p1 / p2、FDP は p1 / p3)
            var gs = NewGameState();
            var fdp = Fdp(("p1", 0), ("p3", 10));
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                new DrowZzzGameSession(gs, fdp, DrowZzzTurnPhase.WaitingForDraw));
        }

        // ===== DZ-010: CurrentRound 計算(N=2)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-010")]
        public void Given_TurnNumber1のGameState_When_CurrentRoundを取得_Then_1を返す()
        {
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 1),
                Fdp(("p1", 0), ("p2", 10)),
                DrowZzzTurnPhase.WaitingForDraw);
            Assert.That(session.CurrentRound, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-010")]
        public void Given_TurnNumber2のGameState_When_CurrentRoundを取得_Then_1を返す()
        {
            // ラウンド 1 のサブターン 2(後攻プレイヤー)
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 2, currentPlayerIndex: 1),
                Fdp(("p1", 0), ("p2", 10)),
                DrowZzzTurnPhase.WaitingForDraw);
            Assert.That(session.CurrentRound, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-010")]
        public void Given_TurnNumber3のGameState_When_CurrentRoundを取得_Then_2を返す()
        {
            // ラウンド 2 のサブターン 1(先行プレイヤー)
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 3, currentPlayerIndex: 0),
                Fdp(("p1", 0), ("p2", 10)),
                DrowZzzTurnPhase.WaitingForDraw);
            Assert.That(session.CurrentRound, Is.EqualTo(2));
        }

        // ===== DZ-014〜017: with 式経路の null / cross-field 検証 =====

        private static DrowZzzGameSession NewSession() =>
            new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 0), ("p2", 10)),
                DrowZzzTurnPhase.WaitingForDraw);

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-014")]
        public void Given_既存Session_When_with_GameStateにnull_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var session = NewSession();
            // When / Then
            Assert.Throws<ArgumentNullException>(() => _ = session with { GameState = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-015")]
        public void Given_既存Session_When_with_FirstDrowsyPointsにnull_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var session = NewSession();
            // When / Then
            Assert.Throws<ArgumentNullException>(() => _ = session with { FirstDrowsyPoints = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-016")]
        public void Given_既存Session_When_with_FirstDrowsyPointsをキー不一致に変更_Then_ArgumentExceptionを投げる()
        {
            // Given(既存 Session の Players = p1, p2、新 FDP = p1, p3)
            var session = NewSession();
            var mismatched = Fdp(("p1", 0), ("p3", 10));
            // When / Then
            Assert.Throws<ArgumentException>(() => _ = session with { FirstDrowsyPoints = mismatched });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-017")]
        public void Given_既存Session_When_with_GameStateをPlayers不一致に変更_Then_ArgumentExceptionを投げる()
        {
            // Given(既存 Session の FDP keys = p1, p2、新 GameState の Players = p1, p3)
            var session = NewSession();
            var mismatchedGameState = NewGameState(
                players: new[] { Player("p1"), Player("p3") });
            // When / Then
            Assert.Throws<ArgumentException>(() => _ = session with { GameState = mismatchedGameState });
        }
    }
}
