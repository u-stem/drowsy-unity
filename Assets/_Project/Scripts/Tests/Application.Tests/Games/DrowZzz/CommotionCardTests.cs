using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// カード No.05「喧騒を纏う」の統合テスト(DZ-270 〜 DZ-276、2026-05-17 で導入)。
    /// 時間帯分岐 + <see cref="StackHandCardOnDeckTopEffect"/>(動的山札 top 配置)+ `IsLegalPlayCard` 拡張
    /// (TargetCardId 必須 / Source プレイヤー手札所持 / AssociatedCardIds 除外)を統合的に検証する。
    /// No.04「静寂を纏う」と対をなす対人介入カード(押し付け方向)。
    /// </summary>
    [TestFixture]
    public sealed class CommotionCardTests
    {
        // ===== ヘルパー =====

        private static readonly CardTypeId CommotionTypeId = CardTypeId.Of("05");
        private static readonly CardTypeId TargetTypeId = CardTypeId.Of("X");

        // 「喧騒を纏う」の効果列(最上位 2 件:時間帯依存 SDP + 時間帯非依存 Stack)
        private static IEffect[] CommotionEffects() => new IEffect[]
        {
            new TimeOfDayBranchEffect(
                nightEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -8),
                    new AdjustSdpEffect(SdpTarget.Opponent, -18),
                },
                morningEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -4),
                    new AdjustSdpEffect(SdpTarget.Opponent, 12),
                }),
            new StackHandCardOnDeckTopEffect(SdpTarget.Self),
        };

        private static InMemoryCardCatalog NewCatalogWithCardFive()
        {
            var card05 = new CardData("喧騒を纏う", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CommotionTypeId, card05),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CommotionTypeId,
                    (IReadOnlyList<IEffect>)CommotionEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // p1 の手札に Card "05" + Card "X" を持たせる session を構築(N=2)。
        // turnNumber で夜(=1)/朝(=33)を切り替える。
        // includeTargetInOwnHand=false の場合は Card "X" を手札に含めない(DZ-275 用)。
        private static DrowZzzGameSession NewSession(
            int turnNumber,
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForPlay,
            IReadOnlyCollection<CardId> associatedCardIds = null,
            bool includeTargetInOwnHand = true)
        {
            var p1HandCards = includeTargetInOwnHand
                ? new[] { CardId.Of(CommotionTypeId, 0), CardId.Of(TargetTypeId, 0) }
                : new[] { CardId.Of(CommotionTypeId, 0) };
            return SessionFactory.NewSession(
                phase: phase,
                p0Hand: new Hand(p1HandCards),
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                associatedCardIds: associatedCardIds);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-270 / 271 / 272: 夜のプレイ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-270")]
        public void Given_夜のフェーズ_When_Card05をプレイ_Then_自分のSDPがマイナス8()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-8));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-271")]
        public void Given_夜のフェーズ_When_Card05をプレイ_Then_相手のSDPがマイナス18()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-18));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-272")]
        public void Given_夜のフェーズ_When_Card05をプレイ_Then_自分の手札から対象が除去される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then(p1 の手札に Card "X" は含まれない)
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(TargetTypeId, 0)), Is.False);
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-272")]
        public void Given_夜のフェーズ_When_Card05をプレイ_Then_共通山札のtopが対象になる()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then(Pile.Draw() = top の取り出し、AddTop は先頭追加)
            var (top, _) = next.GameState.Deck.Draw();
            Assert.That(top, Is.EqualTo(CardId.Of(TargetTypeId, 0)));
        }

        // ===== DZ-273: 朝のプレイ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-273")]
        public void Given_朝のフェーズ_When_Card05をプレイ_Then_自分のSDPがマイナス4()
        {
            // Given(turnNumber=33 → 朝)
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-273")]
        public void Given_朝のフェーズ_When_Card05をプレイ_Then_相手のSDPがプラス12()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(12));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-273")]
        public void Given_朝のフェーズ_When_Card05をプレイ_Then_共通山札のtopが対象になる()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            var (top, _) = next.GameState.Deck.Draw();
            Assert.That(top, Is.EqualTo(CardId.Of(TargetTypeId, 0)));
        }

        // ===== DZ-274: TargetCardId なし → illegal =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-274")]
        public void Given_TargetCardIdなし_When_Card05をIsLegalMove_Then_false()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 1);
            // When / Then
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(CardId.Of(CommotionTypeId, 0))), Is.False);
        }

        // ===== DZ-275: 自分手札にない TargetCardId → illegal =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-275")]
        public void Given_自分手札にないTargetCardId_When_Card05をIsLegalMove_Then_false()
        {
            // Given(p1 の手札に Card "X" を含めない)
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 1, includeTargetInOwnHand: false);
            // When / Then
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0))), Is.False);
        }

        // ===== DZ-276: AssociatedCardIds 含有 → illegal(ADR-0019、PR ① consumer 2 件目)=====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-276")]
        public void Given_AssociatedCardIds含有のTargetCardId_When_Card05をIsLegalMove_Then_false()
        {
            // Given(p1 の手札に Card "X" を持つが、Card "X" が連想由来 = AssociatedCardIds に含まれる)
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(
                turnNumber: 1,
                associatedCardIds: new[] { CardId.Of(TargetTypeId, 0) });
            // When / Then(連想由来は選択不可、ADR-0019)
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(
                CardId.Of(CommotionTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0))), Is.False);
        }

        // ===== DZ-277: TargetCardId == action.Card → illegal(code-reviewer W-2 反映)=====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-277")]
        public void Given_TargetCardIdがプレイ中のCardと同一_When_Card05をIsLegalMove_Then_false()
        {
            // Given(プレイ中の Card "05" と同一の TargetCardId を指定 = 仕様矛盾、code-reviewer W-2 反映)
            var rule = NewRule(NewCatalogWithCardFive());
            var session = NewSession(turnNumber: 1);
            // When / Then(プレイ中のカードは Field に移動するため、Deck top に重ねる対象として矛盾)
            var cardId = CardId.Of(CommotionTypeId, 0);
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(
                cardId,
                TargetCardId: cardId)), Is.False);
        }
    }
}
