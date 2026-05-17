using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Configuration;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using Drowsy.Domain.Random;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    [TestFixture]
    public sealed class StartGameUseCaseTests
    {
        // ===== ヘルパー =====

        private const int DefaultSeed = 42;

        private static IReadOnlyList<PlayerId> NewPlayers(params string[] ids) =>
            ids.Select(PlayerId.Of).ToList();

        private static Pile NewDeck(int count)
        {
            var cards = new CardId[count];
            for (int i = 0; i < count; i++)
            {
                cards[i] = CardId.Of(CardTypeId.Of($"c{i + 1}"), 0);
            }
            return new Pile(cards);
        }

        // ADR-0014: StartGameUseCase の ICardCatalog<IEffect> 依存削除に伴い catalog 引数を除去。
        private static StartGameUseCase NewUseCase(
            IRandomSource rng = null,
            IGameConfig config = null)
        {
            return new StartGameUseCase(
                rng ?? new XorShiftRandom(DefaultSeed),
                config ?? new StubGameConfig());
        }

        // ===== DZ-019: Players 数 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-019")]
        public void Given_有効な引数_When_StartGameUseCase_Execute_Then_PlayersCountが入力と一致する()
        {
            // Given
            var useCase = NewUseCase();
            var players = NewPlayers("p1", "p2");
            var deck = NewDeck(20);
            // When
            var session = useCase.Execute(players, deck);
            // Then
            Assert.That(session.GameState.Players.Count, Is.EqualTo(players.Count));
        }

        // ===== DZ-020: FirstDrowsyPoints キー集合 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-020")]
        public void Given_有効な引数_When_Execute_Then_FirstDrowsyPointsキー集合がplayersと一致する()
        {
            // Given
            var useCase = NewUseCase();
            var players = NewPlayers("p1", "p2");
            var deck = NewDeck(20);
            // When
            var session = useCase.Execute(players, deck);
            // Then
            Assert.That(session.FirstDrowsyPoints.Keys, Is.EquivalentTo(players));
        }

        // ===== DZ-021: FDP 値が FdpPool から被りなく抽選(1 ID 2 テスト)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-021")]
        public void Given_有効な引数_When_Execute_Then_FirstDrowsyPointsの値が全てFdpPoolに含まれる()
        {
            // Given
            var config = new StubGameConfig();
            var useCase = NewUseCase(config: config);
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            var fdpValues = session.FirstDrowsyPoints.Values.ToList();
            Assert.That(fdpValues.All(v => config.FdpPool.Contains(v)), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-021")]
        public void Given_有効な引数_When_Execute_Then_FirstDrowsyPointsの値が全て互いに異なる()
        {
            // Given
            var useCase = NewUseCase();
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            var fdpValues = session.FirstDrowsyPoints.Values.ToList();
            Assert.That(fdpValues.Distinct().Count(), Is.EqualTo(fdpValues.Count));
        }

        // ===== DZ-022: 各プレイヤーの手札が 5 枚 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-022")]
        public void Given_有効な引数_When_Execute_Then_各プレイヤーの手札が5枚()
        {
            // Given
            var useCase = NewUseCase();
            var players = NewPlayers("p1", "p2");
            var deck = NewDeck(20);
            // When
            var session = useCase.Execute(players, deck);
            // Then
            Assert.That(session.GameState.Players.All(p => p.Hand.Count == 5), Is.True);
        }

        // ===== DZ-023: 手札が交互順で配布(Shuffle 後 Players 順に基づく、1 ID 2 テスト)=====
        //
        // IdentityRandom を使い Players Shuffle / FdpPool Shuffle を no-op 化することで、
        // テストが XorShiftRandom の seed 実装に依存しないようにしている(reviewer 警告 W-2)。

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-023")]
        public void Given_IdentityRandom_When_Execute_Then_Players0の手札が奇数位カード()
        {
            // Given(Deck Top 順 = c1〜c20、IdentityRandom で Shuffle 無効化)
            var useCase = NewUseCase(rng: new IdentityRandom());
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            // ADR-0018:Hand.Cards 要素は CardId(instance unique)、本テストの意図は「deck 順に応じた
            // 種別を受け取る」検証なので TypeId.Value で比較する(CardId.Value = "<typeId>#<instance>" の
            // instance 部分は配布順序の検証対象ではない)。
            var p0Hand = session.GameState.Players[0].Hand.Cards.Select(c => c.TypeId.Value).ToArray();
            Assert.That(p0Hand, Is.EqualTo(new[] { "c1", "c3", "c5", "c7", "c9" }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-023")]
        public void Given_IdentityRandom_When_Execute_Then_Players1の手札が偶数位カード()
        {
            // Given(Deck Top 順 = c1〜c20、IdentityRandom で Shuffle 無効化)
            var useCase = NewUseCase(rng: new IdentityRandom());
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            // ADR-0018:同上、TypeId.Value で比較。
            var p1Hand = session.GameState.Players[1].Hand.Cards.Select(c => c.TypeId.Value).ToArray();
            Assert.That(p1Hand, Is.EqualTo(new[] { "c2", "c4", "c6", "c8", "c10" }));
        }

        // ===== DZ-024: 山札残り枚数 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-024")]
        public void Given_有効な引数_When_Execute_Then_山札残り枚数が初期マイナス10()
        {
            // Given
            var useCase = NewUseCase();
            var players = NewPlayers("p1", "p2");
            var deck = NewDeck(20);
            // When
            var session = useCase.Execute(players, deck);
            // Then
            Assert.That(session.GameState.Deck.Count, Is.EqualTo(20 - 5 * 2));
        }

        // ===== DZ-025: PhaseState が WaitingForDraw =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-025")]
        public void Given_有効な引数_When_Execute_Then_PhaseStateがWaitingForDraw()
        {
            // Given
            var useCase = NewUseCase();
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            Assert.That(session.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForDraw));
        }

        // ===== DZ-026: Turn が Initial(0) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-026")]
        public void Given_有効な引数_When_Execute_Then_TurnがInitial0と等価()
        {
            // Given
            var useCase = NewUseCase();
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            Assert.That(session.GameState.Turn, Is.EqualTo(TurnState.Initial(0)));
        }

        // ===== DZ-027: Deterministic Replay =====

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "DZ-027")]
        public void Given_同一引数と同一rng列_When_Executeを2回呼ぶ_Then_結果が等価()
        {
            // Given(同一 seed の rng を 2 つ、他の引数も同一)
            var useCase1 = NewUseCase(rng: new XorShiftRandom(123));
            var useCase2 = NewUseCase(rng: new XorShiftRandom(123));
            var players = NewPlayers("p1", "p2");
            var deck = NewDeck(20);
            // When
            var s1 = useCase1.Execute(players, deck);
            var s2 = useCase2.Execute(players, deck);
            // Then
            Assert.That(s1, Is.EqualTo(s2));
        }

        // ===== DZ-028 〜 DZ-033: 異常系 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-028")]
        public void Given_playersにnull_When_Execute_Then_ArgumentNullExceptionを投げる()
        {
            var useCase = NewUseCase();
            var ex = Assert.Throws<ArgumentNullException>(() => useCase.Execute(null, NewDeck(20)));
            Assert.That(ex!.ParamName, Is.EqualTo("players"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-029")]
        public void Given_空のplayers_When_Execute_Then_ArgumentExceptionを投げる()
        {
            var useCase = NewUseCase();
            Assert.Throws<ArgumentException>(() =>
                useCase.Execute(new List<PlayerId>(), NewDeck(20)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-030")]
        public void Given_重複PlayerIdのplayers_When_Execute_Then_ArgumentExceptionを投げる()
        {
            var useCase = NewUseCase();
            var players = new[] { PlayerId.Of("p1"), PlayerId.Of("p1") };
            Assert.Throws<ArgumentException>(() => useCase.Execute(players, NewDeck(20)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-031")]
        public void Given_initialDeckにnull_When_Execute_Then_ArgumentNullExceptionを投げる()
        {
            var useCase = NewUseCase();
            var ex = Assert.Throws<ArgumentNullException>(() =>
                useCase.Execute(NewPlayers("p1", "p2"), null));
            Assert.That(ex!.ParamName, Is.EqualTo("initialDeck"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-032")]
        public void Given_山札枚数が5xPlayers未満_When_Execute_Then_ArgumentExceptionを投げる()
        {
            var useCase = NewUseCase();
            // 5 × 2 = 10 必要だが 5 枚しかない
            Assert.Throws<ArgumentException>(() =>
                useCase.Execute(NewPlayers("p1", "p2"), NewDeck(5)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-033")]
        public void Given_PlayersCountがFdpPoolより多い_When_Execute_Then_InvalidOperationExceptionを投げる()
        {
            // Given(FdpPool は 10 個のデフォルト、players は 11 人)
            var useCase = NewUseCase();
            var players = Enumerable.Range(1, 11).Select(i => PlayerId.Of($"p{i}")).ToList();
            var deck = NewDeck(11 * 5);
            // When / Then
            Assert.Throws<InvalidOperationException>(() => useCase.Execute(players, deck));
        }

        // ===== DZ-037: players に null 要素を含む =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-037")]
        public void Given_playersにnull要素を含む_When_Execute_Then_ArgumentExceptionを投げる()
        {
            // Given
            var useCase = NewUseCase();
            var players = new[] { PlayerId.Of("p1"), null };
            // When / Then
            Assert.Throws<ArgumentException>(() => useCase.Execute(players, NewDeck(20)));
        }

        // ===== DZ-105: SDP 初期化(M2-PR3 で追加)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-105")]
        public void Given_有効な引数_When_Execute_Then_SecondDrowsyPointsキー集合がplayersと一致する()
        {
            // Given
            var useCase = NewUseCase();
            var players = NewPlayers("p1", "p2");
            // When
            var session = useCase.Execute(players, NewDeck(20));
            // Then
            Assert.That(session.SecondDrowsyPoints.Keys, Is.EquivalentTo(players));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-105")]
        public void Given_有効な引数_When_Execute_Then_SecondDrowsyPointsの全値が0で初期化される()
        {
            // Given(ADR-0009 §「DP 種別」§SDP の「初期値 0」)
            var useCase = NewUseCase();
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            Assert.That(session.SecondDrowsyPoints.Values.All(v => v == 0), Is.True);
        }

        // ===== DZ-139: DDP 初期化(M2-PR4 で追加、ADR-0009 §「DP 種別」§DDP)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-139")]
        public void Given_有効な引数_When_Execute_Then_DrawDrowsyPointsキー集合がplayersと一致する()
        {
            // Given
            var useCase = NewUseCase();
            var players = NewPlayers("p1", "p2");
            // When
            var session = useCase.Execute(players, NewDeck(20));
            // Then
            Assert.That(session.DrawDrowsyPoints.Keys, Is.EquivalentTo(players));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-139")]
        public void Given_有効な引数_When_Execute_Then_DrawDrowsyPointsの全値が0で初期化される()
        {
            // Given(ADR-0009 §「DDP 抽選タイミング」: 初期値 0、Turn 5/9/13/17/21 で累積開始)
            var useCase = NewUseCase();
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            Assert.That(session.DrawDrowsyPoints.Values.All(v => v == 0), Is.True);
        }

        // ===== DZ-140: DdpPool が IGameConfig.DdpPool を Shuffle 済みで保持(M2-PR4 で追加)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-140")]
        public void Given_デフォルトStubGameConfig_When_Execute_Then_DdpPoolが39要素を持つ()
        {
            // Given(StubGameConfig.DdpPool = DdpPoolConstants.BuildDefaultPool() = 13 種 × 3 枚 = 39 要素)
            var useCase = NewUseCase();
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then
            Assert.That(session.DdpPool.Count, Is.EqualTo(39));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-140")]
        public void Given_デフォルトStubGameConfig_When_Execute_Then_DdpPoolマルチセットがIGameConfigDdpPoolと一致()
        {
            // Given(Shuffle はマルチセットを保つ、Fisher-Yates 性)
            var config = new StubGameConfig();
            var useCase = NewUseCase(config: config);
            // When
            var session = useCase.Execute(NewPlayers("p1", "p2"), NewDeck(20));
            // Then(マルチセット比較: 順序は問わず、各値の出現回数が一致)
            var expectedSorted = config.DdpPool.OrderBy(v => v).ToArray();
            var actualSorted = session.DdpPool.Values.OrderBy(v => v).ToArray();
            Assert.That(actualSorted, Is.EqualTo(expectedSorted));
        }
    }
}
