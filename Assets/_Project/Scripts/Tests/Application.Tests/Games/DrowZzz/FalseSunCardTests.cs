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
    /// カード No.12「偽りの太陽」の統合テスト(DZ-334 〜 DZ-344、2026-05-17 で導入、ADR-0022 と同 PR)。
    /// Reactive Influence(アクション後発動型)の初導入で、夜に使うと永続的に
    /// 「PlayCard 後 SDP-10 / Abandon 後 SDP+5」の影響を保有者(自分)に背負わせる戦術カード。
    /// 朝に使うと即時 SDP -4 / +18 のみ(影響付与なし)。
    /// </summary>
    [TestFixture]
    public sealed class FalseSunCardTests
    {
        // ===== ヘルパー =====

        private static readonly CardTypeId FalseSunTypeId = CardTypeId.Of("12");

        // 「偽りの太陽」が夜に付与する Reactive Influence 2 件
        private static PlayerInfluence PlayCardReactiveInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OnOwnPlayCardAfter,
                new AdjustSdpAfterPlayCardEffect(-10),
                InfluenceConstants.Perpetual);

        private static PlayerInfluence AbandonReactiveInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OnOwnAbandonAfter,
                new AdjustSdpAfterAbandonEffect(5),
                InfluenceConstants.Perpetual);

        // 「偽りの太陽」の効果列(TimeOfDayBranchEffect 1 件最上位)
        private static IEffect[] FalseSunEffects() => new IEffect[]
        {
            new TimeOfDayBranchEffect(
                nightEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -4),
                    new AdjustSdpEffect(SdpTarget.Opponent, 6),
                    new ApplyInfluenceEffect(SdpTarget.Self, PlayCardReactiveInfluence()),
                    new ApplyInfluenceEffect(SdpTarget.Self, AbandonReactiveInfluence()),
                },
                morningEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -4),
                    new AdjustSdpEffect(SdpTarget.Opponent, 18),
                }
            ),
        };

        private static InMemoryCardCatalog NewCatalogWithCardTwelve()
        {
            var card12 = new CardData("偽りの太陽", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(FalseSunTypeId, card12),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    FalseSunTypeId,
                    (IReadOnlyList<IEffect>)FalseSunEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // p1 の手札に Card "12" を持たせる session(夜=turnNumber 1、朝=turnNumber 33)
        private static DrowZzzGameSession NewSessionWithCardInHand(int turnNumber)
        {
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(FalseSunTypeId, 0) }),
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-335 / 336 / 337: 夜プレイ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-335")]
        public void Given_夜_When_Card12をプレイ_Then_自分のSDPがマイナス4()
        {
            var rule = NewRule(NewCatalogWithCardTwelve());
            var session = NewSessionWithCardInHand(turnNumber: 1);
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(FalseSunTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-336")]
        public void Given_夜_When_Card12をプレイ_Then_相手のSDPがプラス6()
        {
            var rule = NewRule(NewCatalogWithCardTwelve());
            var session = NewSessionWithCardInHand(turnNumber: 1);
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(FalseSunTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(6));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-337")]
        public void Given_夜_When_Card12をプレイ_Then_自分のInfluencesに2件追加される()
        {
            var rule = NewRule(NewCatalogWithCardTwelve());
            var session = NewSessionWithCardInHand(turnNumber: 1);
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(FalseSunTypeId, 0)));
            // Reactive Influence 2 件が p1 に追加される
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(2));
            Assert.That(next.Influences[PlayerId.Of("p1")], Contains.Item(PlayCardReactiveInfluence()));
            Assert.That(next.Influences[PlayerId.Of("p1")], Contains.Item(AbandonReactiveInfluence()));
        }

        // ===== DZ-338 / 339 / 340: 朝プレイ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-338")]
        public void Given_朝_When_Card12をプレイ_Then_自分のSDPがマイナス4()
        {
            var rule = NewRule(NewCatalogWithCardTwelve());
            var session = NewSessionWithCardInHand(turnNumber: 33);  // 朝 (Round 17-21 相当の TurnNumber)
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(FalseSunTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-339")]
        public void Given_朝_When_Card12をプレイ_Then_相手のSDPがプラス18()
        {
            var rule = NewRule(NewCatalogWithCardTwelve());
            var session = NewSessionWithCardInHand(turnNumber: 33);
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(FalseSunTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(18));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-340")]
        public void Given_朝_When_Card12をプレイ_Then_自分のInfluencesは不変空()
        {
            var rule = NewRule(NewCatalogWithCardTwelve());
            var session = NewSessionWithCardInHand(turnNumber: 33);
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(FalseSunTypeId, 0)));
            // 朝は影響付与なし、p1 の Influences は初期値(空)のまま
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(0));
        }

        // ===== DZ-341: Reactive — 他カード使用で SDP-10 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-341")]
        public void Given_p1が本ReactiveInfluence保有_When_他カードをプレイ_Then_p1のSDPがマイナス10()
        {
            // Given(p1 が PlayCard Reactive 保有 + 効果なしダミー Card "X" を手札に)
            var anyCard = CardId.Of(CardTypeId.Of("X"), 0);
            var rule = new DrowZzzRule(
                new InMemoryCardCatalog(
                    new[] { new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("X"), new CardData("X", new Dictionary<string, int>())) },
                    new[] { new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("X"), (IReadOnlyList<IEffect>)System.Array.Empty<IEffect>()) }),
                new EffectInterpreter());
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = new[] { PlayCardReactiveInfluence() },
                [PlayerId.Of("p2")] = System.Array.Empty<PlayerInfluence>(),
            };
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { anyCard }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
            // When
            var next = rule.Apply(session, new PlayCardAction(anyCard));
            // Then(本 Reactive walk で SDP -10)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-10));
        }

        // ===== DZ-342 第 1: Reactive — AbandonAction(GainSdp)で SDP+10(GainSdp +5 + Reactive +5 合算)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-342")]
        public void Given_p1が本ReactiveInfluence保有_When_AbandonActionGainSdp_Then_p1のSDPがプラス10()
        {
            // Given(p1 が Abandon Reactive 保有 + 任意のカードを手札に + AbandonChoice.GainSdp で SDP +5、本 Reactive +5、計 +10)
            var dummyCard = CardId.Of(CardTypeId.Of("X"), 0);
            var rule = new DrowZzzRule(
                new InMemoryCardCatalog(
                    new[] { new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("X"), new CardData("X", new Dictionary<string, int>())) },
                    new[] { new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("X"), (IReadOnlyList<IEffect>)System.Array.Empty<IEffect>()) }),
                new EffectInterpreter());
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = new[] { AbandonReactiveInfluence() },
                [PlayerId.Of("p2")] = System.Array.Empty<PlayerInfluence>(),
            };
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { dummyCard }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
            // When(AbandonAction.GainSdp で +5、本 Reactive +5、合計 +10)
            var next = rule.Apply(session, new AbandonAction(CardIndex: 0, Choice: AbandonChoice.GainSdp));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(10));
        }

        // ===== DZ-342 第 2: Reactive — AbandonAction(RepairBed)で SDP+5(Reactive のみ、code-reviewer P-7 反映)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-342")]
        public void Given_p1が本ReactiveInfluence保有_When_AbandonActionRepairBed_Then_p1のSDPがプラス5()
        {
            // Given(p1 が Abandon Reactive 保有 + BedDamages=30% で RepairBed の合法条件を満たす)
            // RepairBed は SDP に作用しないため、Reactive +5 のみが SDP に反映される(GainSdp 経路の +10 合算とは別)
            var dummyCard = CardId.Of(CardTypeId.Of("X"), 0);
            var rule = new DrowZzzRule(
                new InMemoryCardCatalog(
                    new[] { new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("X"), new CardData("X", new Dictionary<string, int>())) },
                    new[] { new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("X"), (IReadOnlyList<IEffect>)System.Array.Empty<IEffect>()) }),
                new EffectInterpreter());
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = new[] { AbandonReactiveInfluence() },
                [PlayerId.Of("p2")] = System.Array.Empty<PlayerInfluence>(),
            };
            var bedDamages = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 30,  // RepairBed 合法条件(> 0% 必須、ADR-0011 §2)
                [PlayerId.Of("p2")] = 0,
            };
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { dummyCard }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences,
                bedDamages: bedDamages);
            // When(AbandonAction.RepairBed → SDP は不変、本 Reactive +5、合計 +5)
            var next = rule.Apply(session, new AbandonAction(CardIndex: 0, Choice: AbandonChoice.RepairBed));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(5));
        }

        // ===== DZ-343: snapshot ベース walk — 本カードプレイ自体には Reactive が適用されない =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-343")]
        public void Given_夜_p1が本カードプレイ_Then_本カードプレイ自体にReactiveが適用されない()
        {
            // Given(p1 が Card "12" を手札に、Reactive 影響は未保有 = 付与は本 PlayCard 内で行われる)
            var rule = NewRule(NewCatalogWithCardTwelve());
            var session = NewSessionWithCardInHand(turnNumber: 1);
            // When(本カードプレイ → 即時効果 -4 + Reactive 影響 2 件付与、ただし本 PlayCard には Reactive は適用されない)
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(FalseSunTypeId, 0)));
            // Then(SDP は -4 のみ、本 Reactive SDP-10 は適用されない、ADR-0022 §4 snapshot ベース walk)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
            // 付与は完了している(検証は DZ-337 の責務だが本テストでも確認)
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(2));
        }

        // ===== DZ-344: 他プレイヤー保護 — Reactive は保有者のアクションでのみ発動 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-344")]
        public void Given_p2が本ReactiveInfluence保有_When_p1がPlayCardAction_Then_p2のSDPは不変()
        {
            // Given(p2 が PlayCard Reactive 保有、p1 が current でダミーカードをプレイ)
            // Reactive walk は current player(=actor)の influences が対象、p2 の Reactive は発動しない
            var dummyCard = CardId.Of(CardTypeId.Of("X"), 0);
            var rule = new DrowZzzRule(
                new InMemoryCardCatalog(
                    new[] { new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("X"), new CardData("X", new Dictionary<string, int>())) },
                    new[] { new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("X"), (IReadOnlyList<IEffect>)System.Array.Empty<IEffect>()) }),
                new EffectInterpreter());
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = new[] { PlayCardReactiveInfluence() },
            };
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { dummyCard }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
            // When(p1 が PlayCardAction、p2 は actor ではない)
            var next = rule.Apply(session, new PlayCardAction(dummyCard));
            // Then(p2 の SDP は初期値 0 のまま、p2 の Reactive は p1 のアクションでは発動しない)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0));
        }
    }
}
