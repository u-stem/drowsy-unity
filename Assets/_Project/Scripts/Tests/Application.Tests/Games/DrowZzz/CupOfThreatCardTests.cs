using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// カード No.01「コップ一杯の脅威」の統合テスト(DZ-126 / DZ-127)。
    /// InMemoryCardCatalog の `(entries, effects)` 2 段 constructor を使い、
    /// `TimeOfDayBranchEffect` 1 件を効果列として登録し、`DrowZzzRule.PlayCardAction.Apply` 経由で
    /// SDP / 手札 / 山札の変化を統合的に検証する。
    /// </summary>
    /// <remarks>
    /// 単体 effect record の挙動は <c>AdjustSdpEffectTests</c> / <c>DrawCardEffectTests</c> /
    /// <c>TimeOfDayBranchEffectTests</c> でカバー済。本テストは「カード 1 種類の効果列が end-to-end で動くこと」を
    /// `Category("Medium")` で検証する(M1IntegrationTests と同じ統合粒度、CLAUDE.md §6 カテゴリ規約)。
    /// </remarks>
    [TestFixture]
    public sealed class CupOfThreatCardTests
    {
        // ===== ヘルパー =====

        // ADR-0009 §「コップ一杯の脅威」JIT 共有仕様に従い、CardId "01" を登録する InMemoryCardCatalog を生成
        private static InMemoryCardCatalog NewCatalogWithCardOne()
        {
            var card01 = new CardData("コップ一杯の脅威", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("01"), card01),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("01"),
                    new IEffect[]
                    {
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new DrawCardEffect(SdpTarget.Self, 1),
                                new AdjustSdpEffect(SdpTarget.Opponent, -10),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -4),
                                new AdjustSdpEffect(SdpTarget.Opponent, 10),
                            }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // 現プレイヤー(p1)の手札に Card "01" を 1 枚持たせる Session を構築する。
        // turnNumber を引数で渡し、夜(=1)/ 朝(=33)を切り替える。
        private static DrowZzzGameSession NewSessionWithCardInHand(int turnNumber, Pile deck = null) =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                deck: deck,
                p0Hand: new Hand(new[] { CardId.Of(CardTypeId.Of("01"), 0) }),
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));

        // ===== DZ-126: 夜のプレイで自分 SDP -4 / 1 枚ドロー / 相手 SDP -10 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-126")]
        public void Given_夜のRound_When_Card01をプレイ_Then_自分のSDPがマイナス4()
        {
            // Given(turnNumber=1 → Round=1、夜、現プレイヤー p1 が Card "01" を手札に持つ、山札 top: c1)
            var catalog = NewCatalogWithCardOne();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithCardInHand(turnNumber: 1, deck: new Pile(new[] { CardId.Of(CardTypeId.Of("c1"), 0) }));
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("01"), 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-126")]
        public void Given_夜のRound_When_Card01をプレイ_Then_相手のSDPがマイナス10()
        {
            // Given
            var catalog = NewCatalogWithCardOne();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithCardInHand(turnNumber: 1, deck: new Pile(new[] { CardId.Of(CardTypeId.Of("c1"), 0) }));
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("01"), 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-10));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-126")]
        public void Given_夜のRound_When_Card01をプレイ_Then_自分の手札に山札topがドローされる()
        {
            // Given(山札 top に c1)
            var catalog = NewCatalogWithCardOne();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithCardInHand(turnNumber: 1, deck: new Pile(new[] { CardId.Of(CardTypeId.Of("c1"), 0) }));
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("01"), 0)));
            // Then(p1 の手札に c1 が含まれている、Card "01" は Field 移動済)
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(CardTypeId.Of("c1"), 0)), Is.True);
        }

        // ===== DZ-127: 朝のプレイで自分 SDP -4 / 相手 SDP +10、ドローなし =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-127")]
        public void Given_朝のRound_When_Card01をプレイ_Then_自分のSDPがマイナス4()
        {
            // Given(turnNumber=33 → Round=17、朝、山札なし)
            var catalog = NewCatalogWithCardOne();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithCardInHand(turnNumber: 33, deck: Pile.Empty);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("01"), 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-127")]
        public void Given_朝のRound_When_Card01をプレイ_Then_相手のSDPがプラス10()
        {
            // Given
            var catalog = NewCatalogWithCardOne();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithCardInHand(turnNumber: 33, deck: Pile.Empty);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("01"), 0)));
            // Then(朝は相手を眠くさせる方向、+10)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(10));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-127")]
        public void Given_朝のRound_When_Card01をプレイ_Then_ドロー効果は発動しない()
        {
            // Given(朝は MorningEffects のみ評価、DrawCardEffect は NightEffects 側にしかないため発動しない)
            var catalog = NewCatalogWithCardOne();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithCardInHand(turnNumber: 33, deck: new Pile(new[] { CardId.Of(CardTypeId.Of("c1"), 0) }));
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("01"), 0)));
            // Then(山札は不変、Card "01" は Field 移動のみ)
            Assert.That(next.GameState.Deck.Count, Is.EqualTo(1));
        }
    }
}
