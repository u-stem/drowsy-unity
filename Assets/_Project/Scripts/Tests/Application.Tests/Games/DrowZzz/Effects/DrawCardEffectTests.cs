using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="DrawCardEffect"/> を <see cref="EffectInterpreter.Apply"/> で評価した時の挙動を検証する
    /// (DZ-115 / DZ-116 / DZ-117 / DZ-118)。
    /// </summary>
    [TestFixture]
    public sealed class DrawCardEffectTests
    {
        // ===== ヘルパー =====

        private static DrowZzzGameSession NewSession(Pile deck, int currentPlayerIndex = 0)
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players,
                deck,
                Pile.Empty,
                Pile.Empty,
                new TurnState(1, currentPlayerIndex));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            var sdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // DDP / DdpPool は M2-PR4 で追加。本 fixture は Draw 効果テスト目的のため DDP=0 / 空 DdpPool で固定。
            var ddp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // M2-PR5: Influences は本 fixture では空 list 固定
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            return new DrowZzzGameSession(gs, fdp, ddp, sdp, DdpPool.Empty, influences, DrowZzzPhaseState.WaitingForPlay, outcome: null, bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 }, System.Array.Empty<PendingCounteredEffect>());
        }

        private static Pile Deck(params string[] cardIds)
        {
            var cards = new CardId[cardIds.Length];
            for (int i = 0; i < cardIds.Length; i++)
            {
                cards[i] = CardId.Of(cardIds[i]);
            }
            return new Pile(cards);
        }

        // ===== DZ-115: Target=Self Count=1 で現プレイヤーが 1 枚引く =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-115")]
        public void Given_TargetSelf_Count1_When_Apply_Then_手札にトップカードが1枚追加される()
        {
            // Given(山札 [c1, c2, c3]、top: c1、現プレイヤー p1)
            var interpreter = new EffectInterpreter();
            var session = NewSession(Deck("c1", "c2", "c3"));
            // When
            var next = interpreter.Apply(session, new DrawCardEffect(SdpTarget.Self, 1));
            // Then
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of("c1")), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-115")]
        public void Given_TargetSelf_Count1_When_Apply_Then_山札top1枚分減少する()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var session = NewSession(Deck("c1", "c2", "c3"));
            // When
            var next = interpreter.Apply(session, new DrawCardEffect(SdpTarget.Self, 1));
            // Then
            Assert.That(next.GameState.Deck.Count, Is.EqualTo(2));
        }

        // ===== DZ-116: Target=Self Count=N で N 枚引く =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-116")]
        public void Given_TargetSelf_Count3_When_Apply_Then_手札枚数が3になる()
        {
            // Given(山札 [c1..c5]、現プレイヤー p1)
            var interpreter = new EffectInterpreter();
            var session = NewSession(Deck("c1", "c2", "c3", "c4", "c5"));
            // When
            var next = interpreter.Apply(session, new DrawCardEffect(SdpTarget.Self, 3));
            // Then(枚数 3、内容詳細は次テストで検証)
            Assert.That(next.GameState.Players[0].Hand.Count, Is.EqualTo(3));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-116")]
        public void Given_TargetSelf_Count3_When_Apply_Then_山札top3枚が順に手札に追加される()
        {
            // Given(山札 top: c1, c2, c3, ...)
            var interpreter = new EffectInterpreter();
            var session = NewSession(Deck("c1", "c2", "c3", "c4", "c5"));
            // When
            var next = interpreter.Apply(session, new DrawCardEffect(SdpTarget.Self, 3));
            // Then(top 3 枚 = c1, c2, c3 が手札に全て含まれる、code-reviewer P-4 反映)
            var hand = next.GameState.Players[0].Hand;
            Assert.That(
                hand.Contains(CardId.Of("c1")) && hand.Contains(CardId.Of("c2")) && hand.Contains(CardId.Of("c3")),
                Is.True);
        }

        // ===== DZ-117: 枯渇時 graceful degradation =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-117")]
        public void Given_山札が2枚でCount3_When_Apply_Then_手札に2枚追加され例外を投げない()
        {
            // Given(山札 2 枚、Count=3 要求)
            var interpreter = new EffectInterpreter();
            var session = NewSession(Deck("c1", "c2"));
            // When
            var next = interpreter.Apply(session, new DrawCardEffect(SdpTarget.Self, 3));
            // Then(例外なし、手札 2 枚 + 山札空)
            Assert.That(next.GameState.Players[0].Hand.Count, Is.EqualTo(2));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-117")]
        public void Given_山札が2枚でCount3_When_Apply_Then_山札が空になる()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var session = NewSession(Deck("c1", "c2"));
            // When
            var next = interpreter.Apply(session, new DrawCardEffect(SdpTarget.Self, 3));
            // Then
            Assert.That(next.GameState.Deck.IsEmpty, Is.True);
        }

        // ===== DZ-118: Target=Opponent は M2-PR3 範囲では未実装 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-118")]
        public void Given_TargetOpponent_When_Apply_Then_NotImplementedExceptionを投げる()
        {
            // Given(M2-PR3 では Opponent ドローカードは未登場)
            var interpreter = new EffectInterpreter();
            var session = NewSession(Deck("c1"));
            // When / Then
            Assert.Throws<NotImplementedException>(
                () => interpreter.Apply(session, new DrawCardEffect(SdpTarget.Opponent, 1)));
        }
    }
}
