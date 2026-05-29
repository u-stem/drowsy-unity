using System;
using System.Collections.Generic;
using System.Linq;
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
    /// カード No.20「至上の喜び」の統合テスト(DZ-398〜404)。
    /// `PlayOrAbandonBranchEffect` 機構の初導入カード:プレイ時 +20/-20 + 自爆 Marker、放棄時 +4/+6(AbandonChoice 累積)。
    /// </summary>
    [TestFixture]
    public sealed class SupremeJoyCardTests
    {
        private static readonly CardTypeId SupremeJoyTypeId = CardTypeId.Of("20");

        private static IEffect[] SupremeJoyEffects() => new IEffect[]
        {
            new PlayOrAbandonBranchEffect(
                playEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, +20),
                    new AdjustSdpEffect(SdpTarget.Opponent, -20),
                    new ApplyInfluenceEffect(SdpTarget.Self, new PlayerInfluence(
                        Trigger: InfluenceTrigger.OwnPhaseStart,
                        TickEffect: new RestrictAllUsageAndAbandonInfluenceMarkerEffect(),
                        RemainingCount: 1)),
                },
                abandonEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, +4),
                    new AdjustSdpEffect(SdpTarget.Opponent, +6),
                }),
        };

        private static InMemoryCardCatalog NewCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(SupremeJoyTypeId, new CardData("至上の喜び", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(SupremeJoyTypeId, (IReadOnlyList<IEffect>)SupremeJoyEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        private static DrowZzzRule NewRule() => new DrowZzzRule(NewCatalog(), new EffectInterpreter());

        // プレイ用 session:p1 current、WaitingForPlay、p1 手札に Card "20" のみ
        private static DrowZzzGameSession NewPlaySession() =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(SupremeJoyTypeId, 0) }));

        // 放棄用 session(BedDamages 指定可):p1 current、WaitingForPlay、p1 手札に Card "20" のみ
        private static DrowZzzGameSession NewAbandonSession(int bedDamageP1 = 0) =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(SupremeJoyTypeId, 0) }),
                bedDamages: new Dictionary<PlayerId, int>
                {
                    [PlayerId.Of("p1")] = bedDamageP1,
                    [PlayerId.Of("p2")] = 0,
                });

        // ===== DZ-398: プレイ時 SDP +20/-20 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-398")]
        public void Given_Card20_When_PlayCardAction_Then_自分SDPプラス20()
        {
            var rule = NewRule();
            var next = rule.Apply(NewPlaySession(), new PlayCardAction(CardId.Of(SupremeJoyTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(+20));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-398")]
        public void Given_Card20_When_PlayCardAction_Then_相手SDPマイナス20()
        {
            var rule = NewRule();
            var next = rule.Apply(NewPlaySession(), new PlayCardAction(CardId.Of(SupremeJoyTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-20));
        }

        // ===== DZ-399: プレイ時 Self に Restrict Marker Influence 付与 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-399")]
        public void Given_Card20_When_PlayCardAction_Then_Self_Influences_Restrict_Marker1件追加()
        {
            var rule = NewRule();
            var next = rule.Apply(NewPlaySession(), new PlayCardAction(CardId.Of(SupremeJoyTypeId, 0)));
            var p1Influences = next.Influences[PlayerId.Of("p1")];
            Assert.That(p1Influences.Count, Is.EqualTo(1));
            var inf = p1Influences[0];
            Assert.That(inf.Trigger, Is.EqualTo(InfluenceTrigger.OwnPhaseStart));
            Assert.That(inf.TickEffect, Is.TypeOf<RestrictAllUsageAndAbandonInfluenceMarkerEffect>());
            Assert.That(inf.RemainingCount, Is.EqualTo(1));
        }

        // ===== DZ-400: Hand から Field へ移動 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-400")]
        public void Given_Card20手札_When_PlayCardAction_Then_HandからRemove_FieldにAdd()
        {
            var rule = NewRule();
            var card = CardId.Of(SupremeJoyTypeId, 0);
            var next = rule.Apply(NewPlaySession(), new PlayCardAction(card));
            Assert.That(next.GameState.Players[0].Hand.Contains(card), Is.False);
            Assert.That(next.GameState.Field.Cards[0], Is.EqualTo(card));
        }

        // ===== DZ-401: 放棄 GainSdp — Self+5+4 / Opp+6 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-401")]
        public void Given_Card20_When_AbandonGainSdp_Then_Self_SDP_9()
        {
            // AbandonChoice.GainSdp で SDP+5 + カード固有 Self+4 = +9
            var rule = NewRule();
            var next = rule.Apply(NewAbandonSession(), new AbandonAction(CardIndex: 0, Choice: AbandonChoice.GainSdp));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(+9));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-401")]
        public void Given_Card20_When_AbandonGainSdp_Then_Opp_SDP_6()
        {
            // AbandonChoice.GainSdp は Self のみに +5、Opp はカード固有 +6 のみ
            var rule = NewRule();
            var next = rule.Apply(NewAbandonSession(), new AbandonAction(CardIndex: 0, Choice: AbandonChoice.GainSdp));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(+6));
        }

        // ===== DZ-402: 放棄 RepairBed — Bed-20 / Self+4 / Opp+6 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-402")]
        public void Given_Card20_BedDamages50_When_AbandonRepairBed_Then_Bed_30()
        {
            // AbandonChoice.RepairBed で BedDamages 50 → 30
            var rule = NewRule();
            var next = rule.Apply(NewAbandonSession(bedDamageP1: 50), new AbandonAction(CardIndex: 0, Choice: AbandonChoice.RepairBed));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(30));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-402")]
        public void Given_Card20_BedDamages50_When_AbandonRepairBed_Then_Self_SDP_4()
        {
            // AbandonChoice.RepairBed は SDP に影響なし(Bed のみ)、カード固有 Self+4
            var rule = NewRule();
            var next = rule.Apply(NewAbandonSession(bedDamageP1: 50), new AbandonAction(CardIndex: 0, Choice: AbandonChoice.RepairBed));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(+4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-402")]
        public void Given_Card20_BedDamages50_When_AbandonRepairBed_Then_Opp_SDP_6()
        {
            var rule = NewRule();
            var next = rule.Apply(NewAbandonSession(bedDamageP1: 50), new AbandonAction(CardIndex: 0, Choice: AbandonChoice.RepairBed));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(+6));
        }

        // ===== DZ-403: 放棄 — Hand から Discard へ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-403")]
        public void Given_Card20手札_When_Abandon_Then_Hand_Remove_Discard_Add()
        {
            var rule = NewRule();
            var card = CardId.Of(SupremeJoyTypeId, 0);
            var next = rule.Apply(NewAbandonSession(), new AbandonAction(CardIndex: 0, Choice: AbandonChoice.GainSdp));
            Assert.That(next.GameState.Players[0].Hand.Contains(card), Is.False);
            Assert.That(next.GameState.Discard.Cards.Contains(card), Is.True);
        }

        // ===== DZ-404: 放棄では甲影響(Restrict Marker)は付与されない =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-404")]
        public void Given_Card20_When_Abandon_Then_Influences_変動なし()
        {
            // AbandonEffects に ApplyInfluenceEffect なし、放棄経路では甲影響は付与されない
            var rule = NewRule();
            var next = rule.Apply(NewAbandonSession(), new AbandonAction(CardIndex: 0, Choice: AbandonChoice.GainSdp));
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(0));
        }
    }
}
