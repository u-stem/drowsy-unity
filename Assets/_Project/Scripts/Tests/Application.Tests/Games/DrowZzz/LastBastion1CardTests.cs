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
    /// カード No.13「最後の砦Ⅰ」の統合テスト(DZ-345 〜 DZ-350、2026-05-17 で導入)。
    /// 自動連想 effect `AssociateSpecificCardEffect` の初導入カード。Choice2 で No.14 を自動連想 + Hand 追加 + AssociatedCardIds 記録。
    /// 2 回プレイで Instance 自動採番(0, 1)を検証。
    /// </summary>
    [TestFixture]
    public sealed class LastBastion1CardTests
    {
        private static readonly CardTypeId LastBastion1TypeId = CardTypeId.Of("13");
        private static readonly CardTypeId LastBastion2TypeId = CardTypeId.Of("14");

        private static IEffect[] LastBastion1Effects() => new IEffect[]
        {
            new ChoiceEffect(new IReadOnlyList<IEffect>[]
            {
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, 6),
                    new AdjustSdpEffect(SdpTarget.Opponent, -10),
                },
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -4),
                    new AdjustSdpEffect(SdpTarget.Opponent, -10),
                    new AssociateSpecificCardEffect(LastBastion2TypeId),
                },
            }),
        };

        private static InMemoryCardCatalog NewCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(LastBastion1TypeId, new CardData("最後の砦Ⅰ", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(LastBastion1TypeId, (IReadOnlyList<IEffect>)LastBastion1Effects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        private static DrowZzzGameSession NewSessionWithCardInHand(Hand p0Hand = null)
        {
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: p0Hand ?? new Hand(new[] { CardId.Of(LastBastion1TypeId, 0) }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-346: Choice0 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-346")]
        public void Given_任意フェーズ_When_Card13をChoice0でプレイ_Then_自分のSDPがプラス6()
        {
            var rule = NewRule(NewCatalog());
            var session = NewSessionWithCardInHand();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(LastBastion1TypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(6));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-346")]
        public void Given_任意フェーズ_When_Card13をChoice0でプレイ_Then_相手のSDPがマイナス10()
        {
            var rule = NewRule(NewCatalog());
            var session = NewSessionWithCardInHand();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(LastBastion1TypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-10));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-346")]
        public void Given_任意フェーズ_When_Card13をChoice0でプレイ_Then_HandにNo14は追加されない()
        {
            var rule = NewRule(NewCatalog());
            var session = NewSessionWithCardInHand();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(LastBastion1TypeId, 0), Choice: 0));
            // Choice0 では連想 effect が含まれないため Hand に No.14 は追加されない(Hand は元の Card "13" を Remove して空)
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(LastBastion2TypeId, 0)), Is.False);
        }

        // ===== DZ-347: Choice1 SDP =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-347")]
        public void Given_任意フェーズ_When_Card13をChoice1でプレイ_Then_自分のSDPがマイナス4()
        {
            var rule = NewRule(NewCatalog());
            var session = NewSessionWithCardInHand();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(LastBastion1TypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-347")]
        public void Given_任意フェーズ_When_Card13をChoice1でプレイ_Then_相手のSDPがマイナス10()
        {
            var rule = NewRule(NewCatalog());
            var session = NewSessionWithCardInHand();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(LastBastion1TypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-10));
        }

        // ===== DZ-348: Choice1 で No.14 自動連想 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-348")]
        public void Given_任意フェーズ_When_Card13をChoice1でプレイ_Then_HandにNo14が追加される()
        {
            var rule = NewRule(NewCatalog());
            var session = NewSessionWithCardInHand();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(LastBastion1TypeId, 0), Choice: 1));
            // Hand: 元の Card "13" Instance 0 は Remove 済、新規 Card "14" Instance 0 が追加されている
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(LastBastion2TypeId, 0)), Is.True);
        }

        // ===== DZ-349: Choice1 で AssociatedCardIds に記録 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-349")]
        public void Given_任意フェーズ_When_Card13をChoice1でプレイ_Then_AssociatedCardIdsにNo14が記録される()
        {
            var rule = NewRule(NewCatalog());
            var session = NewSessionWithCardInHand();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(LastBastion1TypeId, 0), Choice: 1));
            Assert.That(next.IsAssociated(CardId.Of(LastBastion2TypeId, 0)), Is.True);
        }

        // ===== DZ-350: 2 回プレイで Instance 自動採番 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-350")]
        public void Given_Card13を2回Choice1でプレイ_Then_HandにNo14がInstance0と1の2件追加される()
        {
            // Given(p1 が Card "13" を 2 枚(Instance 0, 1)保有)
            var rule = NewRule(NewCatalog());
            var p0Hand = new Hand(new[]
            {
                CardId.Of(LastBastion1TypeId, 0),
                CardId.Of(LastBastion1TypeId, 1),
            });
            var session = NewSessionWithCardInHand(p0Hand: p0Hand);
            // When(1 回目:Card "13" Instance 0 を Choice=1 でプレイ)
            var afterFirst = rule.Apply(session, new PlayCardAction(CardId.Of(LastBastion1TypeId, 0), Choice: 1));
            // 1 回目で Hand に CardId.Of("14", 0) が追加されているはず
            Assert.That(afterFirst.GameState.Players[0].Hand.Contains(CardId.Of(LastBastion2TypeId, 0)), Is.True);
            // 実ゲームでは同一ターン内に 2 回 PlayCardAction は不可(1 ターン 1 プレイ制約、PlayCardAction 後の
            // PhaseState 遷移で WaitingForPlay → WaitingForEndTurn にロックされる)。本テストは Instance 採番ロジックを
            // 隔離検証するため、PhaseState を強制的に WaitingForPlay に戻して 2 回目プレイを可能にする
            // (テスト都合の擬似シナリオ、code-reviewer P-2 反映 2026-05-17)。
            var beforeSecond = afterFirst with { PhaseState = DrowZzzPhaseState.WaitingForPlay };
            // When(2 回目:Card "13" Instance 1 を Choice=1 でプレイ)
            var afterSecond = rule.Apply(beforeSecond, new PlayCardAction(CardId.Of(LastBastion1TypeId, 1), Choice: 1));
            // Then(Hand に CardId.Of("14", 0) と CardId.Of("14", 1) の 2 件が存在)
            Assert.That(afterSecond.GameState.Players[0].Hand.Contains(CardId.Of(LastBastion2TypeId, 0)), Is.True);
            Assert.That(afterSecond.GameState.Players[0].Hand.Contains(CardId.Of(LastBastion2TypeId, 1)), Is.True);
        }
    }
}
