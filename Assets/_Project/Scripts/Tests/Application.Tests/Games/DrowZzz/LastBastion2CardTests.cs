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
    /// カード No.14「最後の砦Ⅱ」の統合テスト(DZ-352 〜 DZ-354、2026-05-17 で導入)。
    /// 「最後の砦」連鎖の中間カード、Choice2 で No.15 を自動連想。
    /// </summary>
    [TestFixture]
    public sealed class LastBastion2CardTests
    {
        private static readonly CardTypeId LastBastion2TypeId = CardTypeId.Of("14");
        private static readonly CardTypeId LastBastion3TypeId = CardTypeId.Of("15");

        private static IEffect[] LastBastion2Effects() => new IEffect[]
        {
            new ChoiceEffect(new IReadOnlyList<IEffect>[]
            {
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, 8),
                    new AdjustSdpEffect(SdpTarget.Opponent, -10),
                },
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -4),
                    new AdjustSdpEffect(SdpTarget.Opponent, -20),
                    new AssociateSpecificCardEffect(LastBastion3TypeId),
                },
            }),
        };

        private static InMemoryCardCatalog NewCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(LastBastion2TypeId, new CardData("最後の砦Ⅱ", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(LastBastion2TypeId, (IReadOnlyList<IEffect>)LastBastion2Effects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        private static DrowZzzGameSession NewSessionWithCardInHand() =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(LastBastion2TypeId, 0) }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-352: Choice0 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-352")]
        public void Given_任意フェーズ_When_Card14をChoice0でプレイ_Then_自分SDPプラス8()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion2TypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(8));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-352")]
        public void Given_任意フェーズ_When_Card14をChoice0でプレイ_Then_相手SDPマイナス10()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion2TypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-10));
        }

        // ===== DZ-353: Choice1 SDP =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-353")]
        public void Given_任意フェーズ_When_Card14をChoice1でプレイ_Then_自分SDPマイナス4()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion2TypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-353")]
        public void Given_任意フェーズ_When_Card14をChoice1でプレイ_Then_相手SDPマイナス20()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion2TypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-20));
        }

        // ===== DZ-354: Choice1 で No.15 自動連想 + AssociatedCardIds 記録 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-354")]
        public void Given_任意フェーズ_When_Card14をChoice1でプレイ_Then_HandにNo15が追加される()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion2TypeId, 0), Choice: 1));
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(LastBastion3TypeId, 0)), Is.True);
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-354")]
        public void Given_任意フェーズ_When_Card14をChoice1でプレイ_Then_AssociatedCardIdsにNo15が記録される()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion2TypeId, 0), Choice: 1));
            Assert.That(next.IsAssociated(CardId.Of(LastBastion3TypeId, 0)), Is.True);
        }
    }
}
