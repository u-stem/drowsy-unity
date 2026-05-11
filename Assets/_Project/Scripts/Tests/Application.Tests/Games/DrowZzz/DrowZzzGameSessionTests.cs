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

        // SDP は M2-PR3 で追加(ADR-0009 §「DP 種別」)。引数なし呼び出しで N=2 (p1, p2) の SDP=0 を返す
        // ことで、SDP に関心がない既存テスト(DZ-006〜017 / DZ-036 / DZ-097)の修正を最小化する。
        private static IReadOnlyDictionary<PlayerId, int> Sdp(params (string id, int value)[] pairs)
        {
            if (pairs.Length == 0)
            {
                return new Dictionary<PlayerId, int>
                {
                    [PlayerId.Of("p1")] = 0,
                    [PlayerId.Of("p2")] = 0,
                };
            }
            var d = new Dictionary<PlayerId, int>();
            foreach (var (id, v) in pairs)
            {
                d[PlayerId.Of(id)] = v;
            }
            return d;
        }

        // DDP は M2-PR4 で追加(ADR-0009 §「DP 種別」)。Sdp と同じパターンで引数なしで N=2 の DDP=0 を返す。
        private static IReadOnlyDictionary<PlayerId, int> Ddp(params (string id, int value)[] pairs)
        {
            if (pairs.Length == 0)
            {
                return new Dictionary<PlayerId, int>
                {
                    [PlayerId.Of("p1")] = 0,
                    [PlayerId.Of("p2")] = 0,
                };
            }
            var d = new Dictionary<PlayerId, int>();
            foreach (var (id, v) in pairs)
            {
                d[PlayerId.Of(id)] = v;
            }
            return d;
        }

        // 空 DdpPool。多くの既存テストは DDP プール状態に関心がないため、空 Pool を使うことで構築簡略化。
        private static readonly DdpPool EmptyDdpPool = DdpPool.Empty;

        // ===== DZ-006: コンストラクタの値保持(1 テスト 1 アサーションで 3 プロパティ分離) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-006")]
        public void Given_有効な引数_When_DrowZzzGameSessionを生成_Then_GameStateが入力と一致する()
        {
            // Given
            var gs = NewGameState();
            // When
            var session = new DrowZzzGameSession(gs, Fdp(("p1", 0), ("p2", 10)), Ddp(), Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw);
            // Then
            Assert.That(session.GameState, Is.SameAs(gs));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-006")]
        public void Given_有効な引数_When_DrowZzzGameSessionを生成_Then_FirstDrowsyPointsが入力と一致する()
        {
            // Given
            var fdp = Fdp(("p1", 0), ("p2", 10));
            // When
            var session = new DrowZzzGameSession(NewGameState(), fdp, Ddp(), Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw);
            // Then
            Assert.That(session.FirstDrowsyPoints, Is.EquivalentTo(fdp));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-006")]
        public void Given_有効な引数_When_DrowZzzGameSessionを生成_Then_PhaseStateが入力と一致する()
        {
            // When
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForPlay);
            // Then
            Assert.That(session.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForPlay));
        }

        // ===== DZ-007 / DZ-008: null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-007")]
        public void Given_GameStateにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DrowZzzGameSession(null, Fdp(("p1", 0), ("p2", 10)), Ddp(), Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-008")]
        public void Given_FirstDrowsyPointsにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DrowZzzGameSession(NewGameState(), null, Ddp(), Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw));
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
                new DrowZzzGameSession(gs, fdp, Ddp(), Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-009")]
        public void Given_FirstDrowsyPointsのキーがPlayersと部分的に異なる_When_DrowZzzGameSessionを生成_Then_ArgumentExceptionを投げる()
        {
            // Given(Players は p1 / p2、FDP は p1 / p3)
            var gs = NewGameState();
            var fdp = Fdp(("p1", 0), ("p3", 10));
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                new DrowZzzGameSession(gs, fdp, Ddp(), Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw));
        }

        // ===== DZ-010: CurrentRound 計算(N=2)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-010")]
        public void Given_TurnNumber1のGameState_When_CurrentRoundを取得_Then_1を返す()
        {
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 1),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            Assert.That(session.CurrentRound, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-010")]
        public void Given_TurnNumber2のGameState_When_CurrentRoundを取得_Then_1を返す()
        {
            // ラウンド 1 のフェーズ 2(後攻プレイヤー)
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 2, currentPlayerIndex: 1),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            Assert.That(session.CurrentRound, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-010")]
        public void Given_TurnNumber3のGameState_When_CurrentRoundを取得_Then_2を返す()
        {
            // ラウンド 2 のフェーズ 1(先行プレイヤー)
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 3, currentPlayerIndex: 0),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            Assert.That(session.CurrentRound, Is.EqualTo(2));
        }

        // ===== DZ-014〜017: with 式経路の null / cross-field 検証 =====

        private static DrowZzzGameSession NewSession() =>
            new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);

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

        // ===== DZ-036: 値同値性(順序非依存マルチセット同値)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-036")]
        public void Given_同フィールド値の2つのDrowZzzGameSession_When_等価比較_Then_等価()
        {
            // Given(同じ GameState / FirstDrowsyPoints / DDP / SDP / DdpPool / PhaseState で別 instance を 2 つ生成)
            var gs = NewGameState();
            var fdp = Fdp(("p1", 0), ("p2", 10));
            var ddp = Ddp(("p1", 5), ("p2", -5));
            var sdp = Sdp(("p1", 5), ("p2", -3));
            var s1 = new DrowZzzGameSession(gs, fdp, ddp, sdp, EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw);
            var s2 = new DrowZzzGameSession(gs, fdp, ddp, sdp, EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(s1, Is.EqualTo(s2));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-036")]
        public void Given_FirstDrowsyPoints挿入順が異なる2つのDrowZzzGameSession_When_等価比較_Then_等価()
        {
            // Given(GameState / DDP / SDP / DdpPool / PhaseState は同一、FirstDrowsyPoints は同じ key-value だが挿入順が逆)
            var gs = NewGameState();
            var fdpA = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 10,
            };
            var fdpB = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p2")] = 10,
                [PlayerId.Of("p1")] = 0,
            };
            var s1 = new DrowZzzGameSession(gs, fdpA, Ddp(), Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw);
            var s2 = new DrowZzzGameSession(gs, fdpB, Ddp(), Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(s1, Is.EqualTo(s2));
        }

        // ===== DZ-097: Session.Clock.RoundNumber と CurrentRound の同義性(N=2)=====
        // ADR-0008 §2 で確定した computed プロパティ `Clock => new DrowZzzClock(CurrentRound)` の挙動を、
        // N=2 で Round 1 / 16 / 21 に相当する TurnNumber=1 / 31 / 41 で表明する。

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-097")]
        public void Given_TurnNumber1のSession_When_Clock_RoundNumberを取得_Then_CurrentRoundと一致()
        {
            // Given(Round 1 フェーズ 1)
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 1),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.Clock.RoundNumber, Is.EqualTo(session.CurrentRound));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-097")]
        public void Given_TurnNumber31のSession_When_Clock_RoundNumberを取得_Then_CurrentRoundと一致()
        {
            // Given(Round 16 フェーズ 1、夜の終端 04:30。N=2 で TurnNumber = 2 * (16 - 1) + 1 = 31)
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 31),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.Clock.RoundNumber, Is.EqualTo(session.CurrentRound));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-097")]
        public void Given_TurnNumber41のSession_When_Clock_RoundNumberを取得_Then_CurrentRoundと一致()
        {
            // Given(Round 21 フェーズ 1、朝の最終 07:00。N=2 で TurnNumber = 2 * (21 - 1) + 1 = 41)
            var session = new DrowZzzGameSession(
                NewGameState(turnNumber: 41),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.Clock.RoundNumber, Is.EqualTo(session.CurrentRound));
        }

        // ===== DZ-100: SDP プロパティの値保持(M2-PR3 で追加)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-100")]
        public void Given_有効なSDP_When_DrowZzzGameSessionを生成_Then_SecondDrowsyPointsが入力と一致する()
        {
            // Given
            var sdp = Sdp(("p1", 5), ("p2", -3));
            // When
            var session = new DrowZzzGameSession(NewGameState(), Fdp(("p1", 0), ("p2", 10)), Ddp(), sdp, EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw);
            // Then
            Assert.That(session.SecondDrowsyPoints, Is.EquivalentTo(sdp));
        }

        // ===== DZ-101: SDP null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-101")]
        public void Given_SDPにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DrowZzzGameSession(NewGameState(), Fdp(("p1", 0), ("p2", 10)), Ddp(), null, EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw));
        }

        // ===== DZ-102: SDP cross-field 検証(キー集合不一致)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-102")]
        public void Given_SDPのキーがPlayersと不一致_When_DrowZzzGameSessionを生成_Then_ArgumentExceptionを投げる()
        {
            // Given(Players は p1 / p2、SDP は p1 / p3)
            var gs = NewGameState();
            var sdp = Sdp(("p1", 0), ("p3", 5));
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                new DrowZzzGameSession(gs, Fdp(("p1", 0), ("p2", 10)), Ddp(), sdp, EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw));
        }

        // ===== DZ-103: TotalPoints = FDP + SDP(M2-PR3 段階)→ M2-PR4 で DDP=0 を含めた 3 項合計の検証として継続 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-103")]
        public void Given_FDP100SDP10のSession_When_TotalPointsを取得_Then_110を返す()
        {
            // Given(SDP 正値、DDP=0 で M2-PR3 と等価)
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 100), ("p2", 0)),
                Ddp(),
                Sdp(("p1", 10), ("p2", 0)),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.TotalPoints(PlayerId.Of("p1")), Is.EqualTo(110));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-103")]
        public void Given_FDP100SDPマイナス10のSession_When_TotalPointsを取得_Then_90を返す()
        {
            // Given(SDP 負値、DZ-109 と整合、DDP=0)
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 100), ("p2", 0)),
                Ddp(),
                Sdp(("p1", -10), ("p2", 0)),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.TotalPoints(PlayerId.Of("p1")), Is.EqualTo(90));
        }

        // ===== DZ-104: TotalPoints に存在しない PlayerId を渡す =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-104")]
        public void Given_PlayersにいないPlayerId_When_TotalPointsを取得_Then_ArgumentExceptionを投げる()
        {
            // Given(Players は p1 / p2 のみ)
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 0), ("p2", 0)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.Throws<ArgumentException>(() => session.TotalPoints(PlayerId.Of("p3")));
        }

        // ===== DZ-107 / DZ-108: with 式経由 SDP の null / cross-field 検証 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-107")]
        public void Given_既存Session_When_with_SDPにnull_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var session = NewSession();
            // When / Then
            Assert.Throws<ArgumentNullException>(() => _ = session with { SecondDrowsyPoints = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-108")]
        public void Given_既存Session_When_with_SDPをキー不一致に変更_Then_ArgumentExceptionを投げる()
        {
            // Given(既存 Session の Players = p1, p2、新 SDP = p1, p3)
            var session = NewSession();
            var mismatched = Sdp(("p1", 0), ("p3", 5));
            // When / Then
            Assert.Throws<ArgumentException>(() => _ = session with { SecondDrowsyPoints = mismatched });
        }

        // ===== DZ-109: 負値 SDP も保持される(0 floor なし)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-109")]
        public void Given_負のSDP値_When_DrowZzzGameSessionを生成_Then_保持される()
        {
            // Given(SDP に負値、0 floor 適用なし、ADR-0009 戦略「持ち点低い方が勝ち」と整合)
            var sdp = Sdp(("p1", -20), ("p2", -5));
            // When
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 0), ("p2", 10)),
                Ddp(),
                sdp,
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // Then
            Assert.That(session.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-20));
        }

        // ===== DZ-130: DDP プロパティの値保持(M2-PR4 で追加)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-130")]
        public void Given_有効なDDP_When_DrowZzzGameSessionを生成_Then_DrawDrowsyPointsが入力と一致する()
        {
            // Given
            var ddp = Ddp(("p1", 5), ("p2", -10));
            // When
            var session = new DrowZzzGameSession(NewGameState(), Fdp(("p1", 0), ("p2", 10)), ddp, Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw);
            // Then
            Assert.That(session.DrawDrowsyPoints, Is.EquivalentTo(ddp));
        }

        // ===== DZ-131: DDP null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-131")]
        public void Given_DDPにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DrowZzzGameSession(NewGameState(), Fdp(("p1", 0), ("p2", 10)), null, Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw));
        }

        // ===== DZ-132: DDP cross-field 検証(キー集合不一致)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-132")]
        public void Given_DDPのキーがPlayersと不一致_When_DrowZzzGameSessionを生成_Then_ArgumentExceptionを投げる()
        {
            // Given(Players は p1 / p2、DDP は p1 / p3)
            var gs = NewGameState();
            var ddp = Ddp(("p1", 0), ("p3", 5));
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                new DrowZzzGameSession(gs, Fdp(("p1", 0), ("p2", 10)), ddp, Sdp(), EmptyDdpPool, DrowZzzPhaseState.WaitingForDraw));
        }

        // ===== DZ-133: DdpPool null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-133")]
        public void Given_DdpPoolにnull_When_DrowZzzGameSessionを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DrowZzzGameSession(NewGameState(), Fdp(("p1", 0), ("p2", 10)), Ddp(), Sdp(), null, DrowZzzPhaseState.WaitingForDraw));
        }

        // ===== DZ-134 / DZ-135: with 式経由 DDP の null / cross-field 検証 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-134")]
        public void Given_既存Session_When_with_DDPにnull_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var session = NewSession();
            // When / Then
            Assert.Throws<ArgumentNullException>(() => _ = session with { DrawDrowsyPoints = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-135")]
        public void Given_既存Session_When_with_DDPをキー不一致に変更_Then_ArgumentExceptionを投げる()
        {
            // Given(既存 Session の Players = p1, p2、新 DDP = p1, p3)
            var session = NewSession();
            var mismatched = Ddp(("p1", 0), ("p3", 5));
            // When / Then
            Assert.Throws<ArgumentException>(() => _ = session with { DrawDrowsyPoints = mismatched });
        }

        // ===== DZ-136: with 式経由 DdpPool の null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-136")]
        public void Given_既存Session_When_with_DdpPoolにnull_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var session = NewSession();
            // When / Then
            Assert.Throws<ArgumentNullException>(() => _ = session with { DdpPool = null });
        }

        // ===== DZ-137: 負値 DDP も保持される(0 floor なし)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-137")]
        public void Given_負のDDP値_When_DrowZzzGameSessionを生成_Then_保持される()
        {
            // Given(DDP に負値、0 floor 適用なし、ADR-0009 戦略「持ち点低い方が勝ち」と整合)
            var ddp = Ddp(("p1", -20), ("p2", -5));
            // When
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 0), ("p2", 10)),
                ddp,
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // Then
            Assert.That(session.DrawDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-20));
        }

        // ===== DZ-138: TotalPoints = FDP + DDP + SDP の 3 項合計(M2-PR4 で 2 項 → 3 項に拡張)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-138")]
        public void Given_FDP100DDP5SDP10_When_TotalPointsを取得_Then_115を返す()
        {
            // Given(全項正値)
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 100), ("p2", 0)),
                Ddp(("p1", 5), ("p2", 0)),
                Sdp(("p1", 10), ("p2", 0)),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.TotalPoints(PlayerId.Of("p1")), Is.EqualTo(115));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-138")]
        public void Given_FDP100DDPマイナス5SDP10_When_TotalPointsを取得_Then_105を返す()
        {
            // Given(DDP 負値)
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 100), ("p2", 0)),
                Ddp(("p1", -5), ("p2", 0)),
                Sdp(("p1", 10), ("p2", 0)),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.TotalPoints(PlayerId.Of("p1")), Is.EqualTo(105));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-138")]
        public void Given_FDP100DDPマイナス30SDPマイナス20_When_TotalPointsを取得_Then_50を返す()
        {
            // Given(全項減少、DDP / SDP ともに大きく減らした場合の境界)
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 100), ("p2", 0)),
                Ddp(("p1", -30), ("p2", 0)),
                Sdp(("p1", -20), ("p2", 0)),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.TotalPoints(PlayerId.Of("p1")), Is.EqualTo(50));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-138")]
        public void Given_全項0_When_TotalPointsを取得_Then_0を返す()
        {
            // Given(全項 0、StartGame 直後の最低値)
            var session = new DrowZzzGameSession(
                NewGameState(),
                Fdp(("p1", 0), ("p2", 0)),
                Ddp(),
                Sdp(),
                EmptyDdpPool,
                DrowZzzPhaseState.WaitingForDraw);
            // When / Then
            Assert.That(session.TotalPoints(PlayerId.Of("p1")), Is.EqualTo(0));
        }
    }
}
