using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// カード No.03「身体にいいもの」の統合テスト(DZ-247 〜 DZ-254)。
    /// 時間帯分岐(<see cref="TimeOfDayBranchEffect"/>、No.01 パターン)+ 継続影響付与
    /// (<see cref="ApplyInfluenceEffect"/>、No.02 パターン)に加え、<see cref="InfluenceConstants.Perpetual"/>
    /// による「永続影響」概念の初導入を統合的に検証する。
    /// </summary>
    /// <remarks>
    /// 単体 effect record の挙動は <c>AdjustSdpEffectTests</c> / <c>ApplyInfluenceEffectTests</c> /
    /// <c>TimeOfDayBranchEffectTests</c> でカバー済。本テストは「カード 1 種類の効果列が end-to-end で動くこと」+
    /// 「永続影響が Tick で正しく減算されつつ除去されないこと」を <c>Category("Medium")</c> で検証する
    /// (<c>CupOfThreatCardTests</c> / <c>GreenInvasionCardTests</c> と同パターン)。
    /// </remarks>
    [TestFixture]
    public sealed class GoodForBodyCardTests
    {
        // ===== ヘルパー =====

        // 「身体にいいもの」夜分岐由来の影響 x: 自分のフェーズ開始時に SDP +4、永続。
        private static PlayerInfluence NightInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, 4),
                InfluenceConstants.Perpetual);

        // 「身体にいいもの」朝分岐由来の影響 y: 自分のフェーズ開始時に SDP -6、永続。
        private static PlayerInfluence MorningInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, -6),
                InfluenceConstants.Perpetual);

        // 「身体にいいもの」の効果定義(TimeOfDayBranchEffect 1 件で夜/朝を分岐)。
        // 夜: 自分 SDP -20 + 相手 SDP +5 + 自分に永続 +4 影響
        // 朝: 自分 SDP -10 + 相手 SDP +5 + 自分に永続 -6 影響
        private static IEffect GoodForBodyEffect() =>
            new TimeOfDayBranchEffect(
                nightEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -20),
                    new AdjustSdpEffect(SdpTarget.Opponent, 5),
                    new ApplyInfluenceEffect(SdpTarget.Self, NightInfluence()),
                },
                morningEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -10),
                    new AdjustSdpEffect(SdpTarget.Opponent, 5),
                    new ApplyInfluenceEffect(SdpTarget.Self, MorningInfluence()),
                });

        // InMemoryCardCatalog に「身体にいいもの」(CardTypeId "03") を登録して返す
        private static InMemoryCardCatalog NewCatalogWithCardThree()
        {
            var card03 = new CardData("身体にいいもの", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("03"), card03),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("03"),
                    new IEffect[] { GoodForBodyEffect() }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // 現プレイヤー p1 の手札に Card "03" を持たせる Session を構築。
        // turnNumber で夜(=1)/朝(=33)を切り替える(CupOfThreatCardTests と同方式)。
        private static DrowZzzGameSession NewSessionWithCardInHand(
            int turnNumber,
            IReadOnlyList<PlayerInfluence> p1Influences = null,
            IReadOnlyList<PlayerInfluence> p2Influences = null)
        {
            var influences = p1Influences == null && p2Influences == null
                ? null
                : new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = p1Influences ?? System.Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = p2Influences ?? System.Array.Empty<PlayerInfluence>(),
                };
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                p0Hand: new Hand(new[] { CardId.Of(CardTypeId.Of("03"), 0) }),
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-247 / 248 / 249: 夜のプレイ → 自分 SDP -20 / 相手 SDP +5 / 自分に永続 +4 影響 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-247")]
        public void Given_夜のフェーズ_When_Card03をプレイ_Then_自分のSDPがマイナス20()
        {
            // Given(turnNumber=1 → 夜、p1 が Card "03" を手札に持つ)
            var rule = NewRule(NewCatalogWithCardThree());
            var session = NewSessionWithCardInHand(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("03"), 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-20));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-248")]
        public void Given_夜のフェーズ_When_Card03をプレイ_Then_相手のSDPがプラス5()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardThree());
            var session = NewSessionWithCardInHand(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("03"), 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(5));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-249")]
        public void Given_夜のフェーズ_When_Card03をプレイ_Then_自分のInfluencesに永続Plus4が追加される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardThree());
            var session = NewSessionWithCardInHand(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("03"), 0)));
            // Then(p1 の Influences に NightInfluence が追加)
            Assert.That(next.Influences[PlayerId.Of("p1")], Contains.Item(NightInfluence()));
        }

        // ===== DZ-250 / 251 / 252: 朝のプレイ → 自分 SDP -10 / 相手 SDP +5 / 自分に永続 -6 影響 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-250")]
        public void Given_朝のフェーズ_When_Card03をプレイ_Then_自分のSDPがマイナス10()
        {
            // Given(turnNumber=33 → Round=17、朝、p1 が Card "03" を手札に持つ)
            var rule = NewRule(NewCatalogWithCardThree());
            var session = NewSessionWithCardInHand(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("03"), 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-10));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-251")]
        public void Given_朝のフェーズ_When_Card03をプレイ_Then_相手のSDPがプラス5()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardThree());
            var session = NewSessionWithCardInHand(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("03"), 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(5));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-252")]
        public void Given_朝のフェーズ_When_Card03をプレイ_Then_自分のInfluencesに永続Minus6が追加される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardThree());
            var session = NewSessionWithCardInHand(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("03"), 0)));
            // Then(p1 の Influences に MorningInfluence が追加)
            Assert.That(next.Influences[PlayerId.Of("p1")], Contains.Item(MorningInfluence()));
        }

        // ===== DZ-253: 永続影響 +4(夜由来)が Tick で発動 + 永続継続 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-253")]
        public void Given_p2が永続影響Plus4を保有_p1current_When_p1がEndTurnでp2フェーズへ_Then_p2のSDPがプラス4()
        {
            // Given(p1 が current で WaitingForEndTurn、p2 が NightInfluence 保有 → EndTurn で current=p2 に rotate)
            var rule = NewRule(NewCatalogWithCardThree());
            var sessionInPlay = NewSessionWithCardInHand(turnNumber: 1, p2Influences: new[] { NightInfluence() });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When(EndTurn で current が p2 に rotate、p2 の影響が Tick されて SDP +4)
            var next = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-253")]
        public void Given_p2が永続影響Plus4を保有_p1current_When_p1がEndTurnでp2フェーズへ_Then_p2の影響RemainingCountがPerpetualマイナス1()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardThree());
            var sessionInPlay = NewSessionWithCardInHand(turnNumber: 1, p2Influences: new[] { NightInfluence() });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(ADR-0020:p2 Tick で TickEffect 適用のみ、count は p2 自身の EndTurn まで Perpetual のまま)
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(InfluenceConstants.Perpetual));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-253")]
        public void Given_永続影響Plus4をp1のみが保有_p1current_When_p1がEndTurnでp2フェーズへ_Then_p1の影響はDecrementでPerpetualマイナス1()
        {
            // Given(p1 のみ NightInfluence 保有、ADR-0020:p1 EndTurn 冒頭で p1 自身の Influences が Decrement される)
            var rule = NewRule(NewCatalogWithCardThree());
            var sessionInPlay = NewSessionWithCardInHand(turnNumber: 1, p1Influences: new[] { NightInfluence() });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(ADR-0020:p1 自身の EndTurn 冒頭の Decrement で count -1、Perpetual - 1)
            Assert.That(next.Influences[PlayerId.Of("p1")][0].RemainingCount, Is.EqualTo(InfluenceConstants.Perpetual - 1));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-253")]
        public void Given_永続影響Plus4をp1のみが保有_p1current_When_p1がEndTurnでp2フェーズへ_Then_p1のSDPは変動しない()
        {
            // Given(p1 のみ NightInfluence 保有、EndTurn で current=p2 に rotate → p1 の影響は Tick されず SDP も無影響)
            var rule = NewRule(NewCatalogWithCardThree());
            var sessionInPlay = NewSessionWithCardInHand(turnNumber: 1, p1Influences: new[] { NightInfluence() });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(p1 SDP は初期値 0 のまま、Tick の副作用が漏れていない、code-reviewer W-1 反映 2026-05-17)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0));
        }

        // ===== DZ-254: 永続影響 -6(朝由来)が Tick で発動 + 永続継続 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-254")]
        public void Given_p2が永続影響Minus6を保有_p1current_When_p1がEndTurnでp2フェーズへ_Then_p2のSDPがマイナス6()
        {
            // Given(p1 が current で WaitingForEndTurn、p2 が MorningInfluence 保有 → EndTurn で current=p2 に rotate)
            var rule = NewRule(NewCatalogWithCardThree());
            var sessionInPlay = NewSessionWithCardInHand(turnNumber: 1, p2Influences: new[] { MorningInfluence() });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When(EndTurn で current が p2 に rotate、p2 の影響が Tick されて SDP -6)
            var next = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-6));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-254")]
        public void Given_p2が永続影響Minus6を保有_p1current_When_p1がEndTurnでp2フェーズへ_Then_p2の影響RemainingCountがPerpetualマイナス1()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardThree());
            var sessionInPlay = NewSessionWithCardInHand(turnNumber: 1, p2Influences: new[] { MorningInfluence() });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(InfluenceConstants.Perpetual - 1));
        }
    }
}
