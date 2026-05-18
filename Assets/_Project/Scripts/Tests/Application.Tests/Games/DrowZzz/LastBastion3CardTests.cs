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
    /// カード No.15「最後の砦Ⅲ」の統合テスト(DZ-356 〜 DZ-358、2026-05-17 で導入)。
    /// 「最後の砦」連鎖の終端カード、Choice2 でも連想なし(エスカレーション打ち止め)。
    /// </summary>
    [TestFixture]
    public sealed class LastBastion3CardTests
    {
        private static readonly CardTypeId LastBastion3TypeId = CardTypeId.Of("15");

        private static IEffect[] LastBastion3Effects() => new IEffect[]
        {
            new ChoiceEffect(new IReadOnlyList<IEffect>[]
            {
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, 10),
                    new AdjustSdpEffect(SdpTarget.Opponent, -10),
                },
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -4),
                    new AdjustSdpEffect(SdpTarget.Opponent, -30),
                    // 連想なし(終端カード)
                },
            }),
        };

        private static InMemoryCardCatalog NewCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(LastBastion3TypeId, new CardData("最後の砦Ⅲ", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(LastBastion3TypeId, (IReadOnlyList<IEffect>)LastBastion3Effects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        private static DrowZzzGameSession NewSessionWithCardInHand() =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(LastBastion3TypeId, 0) }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-356: Choice0 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-356")]
        public void Given_任意フェーズ_When_Card15をChoice0でプレイ_Then_自分SDPプラス10()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion3TypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(10));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-356")]
        public void Given_任意フェーズ_When_Card15をChoice0でプレイ_Then_相手SDPマイナス10()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion3TypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-10));
        }

        // ===== DZ-357: Choice1 連鎖最強攻撃 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-357")]
        public void Given_任意フェーズ_When_Card15をChoice1でプレイ_Then_自分SDPマイナス4()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion3TypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-357")]
        public void Given_任意フェーズ_When_Card15をChoice1でプレイ_Then_相手SDPマイナス30()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion3TypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-30));
        }

        // ===== DZ-358: Choice1 で Hand に No.13/14/15 は追加されない(終端、連想なし)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-358")]
        public void Given_任意フェーズ_When_Card15をChoice1でプレイ_Then_HandにNo13_14_15は追加されない()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithCardInHand(), new PlayCardAction(CardId.Of(LastBastion3TypeId, 0), Choice: 1));
            // Hand: 元の Card "15" Instance 0 は Remove 済、新規に No.13/14/15 のいずれも追加されない
            // (code-reviewer P-4 反映 2026-05-17:Count==0 だけでなく Contains で「個別カード追加なし」を明示検証、
            // 別カードが誤って追加されていても検出可能化、spec との対応強化)
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(CardTypeId.Of("13"), 0)), Is.False);
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(CardTypeId.Of("14"), 0)), Is.False);
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(LastBastion3TypeId, 0)), Is.False);
            Assert.That(next.GameState.Players[0].Hand.Count, Is.EqualTo(0));
        }
    }
}
