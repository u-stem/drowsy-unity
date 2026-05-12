using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="AbandonAction"/> の合法性判定(`IsLegalMove`)と状態遷移(`Apply`)を検証する
    /// (DZ-199 / DZ-200 / DZ-201 / DZ-202 / DZ-203)。
    /// </summary>
    [TestFixture]
    public sealed class AbandonActionTests
    {
        // ===== ヘルパー =====

        private static readonly DdpPool EmptyDdpPool = DdpPool.Empty;

        private static DrowZzzRule NewRule() =>
            new DrowZzzRule(
                new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]),
                new EffectInterpreter());

        // 現プレイヤー p1 が手札 2 枚(c1, c2)を持つ WaitingForPlay セッション
        private static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForPlay,
            int handCount = 2,
            int bedP1 = 0,
            int sdpP1 = 0)
        {
            var p1HandCards = new CardId[handCount];
            for (int i = 0; i < handCount; i++)
            {
                p1HandCards[i] = CardId.Of($"c{i + 1}");
            }
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), new Hand(p1HandCards)),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, Pile.Empty,
                new TurnState(1, 0));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = sdpP1, [PlayerId.Of("p2")] = 0 };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            var bed = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = bedP1,
                [PlayerId.Of("p2")] = 0,
            };
            return new DrowZzzGameSession(
                gs, fdp, ddp, sdp, EmptyDdpPool, influences, phase, outcome: null, bedDamages: bed);
        }

        // ===== DZ-199: IsLegalMove の合法条件(WaitingForPlay + 手札 1 枚以上 + CardIndex 範囲内)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-199")]
        public void Given_WaitingForPlay手札2枚_CardIndex0_GainSdp_When_IsLegalMove_Then_true()
        {
            var rule = NewRule();
            var session = NewSession();
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0)), Is.True);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-199")]
        public void Given_WaitingForDrawフェーズ_When_IsLegalMove_Then_false()
        {
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-199")]
        public void Given_手札0枚_When_IsLegalMove_Then_false()
        {
            var rule = NewRule();
            var session = NewSession(handCount: 0);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-199")]
        public void Given_CardIndexが範囲外_When_IsLegalMove_Then_false()
        {
            var rule = NewRule();
            var session = NewSession(handCount: 2);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 2)), Is.False);
        }

        // ===== DZ-200: RepairBed は BedDamages > 0% でのみ合法(JIT 確定 2026-05-13、(b) 不可選択)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-200")]
        public void Given_ベッド破損0_RepairBed_When_IsLegalMove_Then_false()
        {
            var rule = NewRule();
            var session = NewSession(bedP1: 0);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.RepairBed)), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-200")]
        public void Given_ベッド破損20_RepairBed_When_IsLegalMove_Then_true()
        {
            var rule = NewRule();
            var session = NewSession(bedP1: 20);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.RepairBed)), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-200")]
        public void Given_ベッド破損0_GainSdp_When_IsLegalMove_Then_true()
        {
            // GainSdp はベッド 0% でも合法(RepairBed のみ BedDamages > 0% 条件)
            var rule = NewRule();
            var session = NewSession(bedP1: 0);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp)), Is.True);
        }

        // ===== DZ-201: Apply で手札が 1 枚減る + Discard に追加 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-201")]
        public void Given_手札2枚_CardIndex0_When_Apply_Then_手札が1枚減る()
        {
            var rule = NewRule();
            var session = NewSession(handCount: 2);
            var next = rule.Apply(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0));
            Assert.That(next.GameState.Players[0].Hand.Count, Is.EqualTo(1));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-201")]
        public void Given_手札2枚_CardIndex0_When_Apply_Then_Discardにc1が追加される()
        {
            var rule = NewRule();
            var session = NewSession(handCount: 2);
            var next = rule.Apply(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0));
            Assert.That(next.GameState.Discard.Cards[0], Is.EqualTo(CardId.Of("c1")));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-201")]
        public void Given_手札2枚_CardIndex1_When_Apply_Then_Discardにc2が追加される()
        {
            // CardIndex=1 で 2 枚目のカードを捨てる
            var rule = NewRule();
            var session = NewSession(handCount: 2);
            var next = rule.Apply(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 1));
            Assert.That(next.GameState.Discard.Cards[0], Is.EqualTo(CardId.Of("c2")));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-201")]
        public void Given_Apply完了_When_PhaseStateを取得_Then_WaitingForEndTurn()
        {
            var rule = NewRule();
            var session = NewSession();
            var next = rule.Apply(session, new AbandonAction(AbandonChoice.GainSdp));
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForEndTurn));
        }

        // ===== DZ-202: GainSdp で SDP +5 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-202")]
        public void Given_SDP10_GainSdp_When_Apply_Then_SDPが15になる()
        {
            var rule = NewRule();
            var session = NewSession(sdpP1: 10);
            var next = rule.Apply(session, new AbandonAction(AbandonChoice.GainSdp));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(15));
        }

        // ===== DZ-203: RepairBed で BedDamages -20%(下限 0%)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-203")]
        public void Given_ベッド破損40_RepairBed_When_Apply_Then_ベッドが20になる()
        {
            var rule = NewRule();
            var session = NewSession(bedP1: 40);
            var next = rule.Apply(session, new AbandonAction(AbandonChoice.RepairBed));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(20));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-203")]
        public void Given_ベッド破損10_RepairBed_When_Apply_Then_ベッドが0でクランプ()
        {
            // 10 - 20 = -10 だが、下限 0% でクランプ
            var rule = NewRule();
            var session = NewSession(bedP1: 10);
            var next = rule.Apply(session, new AbandonAction(AbandonChoice.RepairBed));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(0));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-203")]
        public void Given_ベッド破損100_RepairBed_When_Apply_Then_ベッドが80になる()
        {
            // 上限値 100% から -20% = 80%
            var rule = NewRule();
            var session = NewSession(bedP1: 100);
            var next = rule.Apply(session, new AbandonAction(AbandonChoice.RepairBed));
            Assert.That(next.BedDamages[PlayerId.Of("p1")], Is.EqualTo(80));
        }

        // ===== Apply 防御例外 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-199")]
        public void Given_WaitingForDraw_When_AbandonActionをApply_Then_InvalidOperationException()
        {
            var rule = NewRule();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForDraw);
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(session, new AbandonAction(AbandonChoice.GainSdp)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-200")]
        public void Given_ベッド0_RepairBedをApply_When_直接呼ぶ_Then_InvalidOperationException()
        {
            var rule = NewRule();
            var session = NewSession(bedP1: 0);
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(session, new AbandonAction(AbandonChoice.RepairBed)));
        }

        // ===== DZ-213: Instinct を含むカードは AbandonAction の CardIndex 対象から除外(M3-PR5a、ADR-0011 §4.2)=====

        // Instinct 効果列を持つカード c1 を catalog に登録した rule(c2 は効果列なし = Instinct なし)
        private static DrowZzzRule NewRuleWithInstinctOnC1()
        {
            var entries = new[]
            {
                new KeyValuePair<CardId, CardData>(CardId.Of("c1"), new CardData("instinct card", new Dictionary<string, int>())),
                new KeyValuePair<CardId, CardData>(CardId.Of("c2"), new CardData("normal card", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardId, IReadOnlyList<IEffect>>(
                    CardId.Of("c1"),
                    new IEffect[]
                    {
                        new KeywordedEffect(
                            new[] { Keyword.Instinct },
                            new AssociatableMarkerEffect()),
                    }),
            };
            return new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-213")]
        public void Given_c1がInstinct_CardIndex0_When_IsLegalMove_Then_false()
        {
            // c1(Instinct)を捨て対象に指定 → false
            var rule = NewRuleWithInstinctOnC1();
            var session = NewSession(handCount: 2);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0)), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-213")]
        public void Given_c1がInstinct_CardIndex1_When_IsLegalMove_Then_true()
        {
            // c2(Instinct なし)を捨て対象に指定 → true(catalog に c2 を登録、効果列なし扱い)
            var rule = NewRuleWithInstinctOnC1();
            var session = NewSession(handCount: 2);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 1)), Is.True);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-213")]
        public void Given_c1がInstinct_CardIndex0_When_Apply_Then_InvalidOperationException()
        {
            var rule = NewRuleWithInstinctOnC1();
            var session = NewSession(handCount: 2);
            Assert.Throws<InvalidOperationException>(() =>
                rule.Apply(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0)));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-213")]
        public void Given_ChoiceEffect内にInstinct_When_IsLegalMove_Then_false()
        {
            // W-4 反映:ChoiceEffect.Branches の片方に KeywordedEffect([Instinct], _) が nest されているケース
            // (HasInstinctKeyword の再帰 walk で ChoiceEffect 経路を網羅)
            var entries = new[]
            {
                new KeyValuePair<CardId, CardData>(CardId.Of("c1"), new CardData("choice with instinct branch", new Dictionary<string, int>())),
                new KeyValuePair<CardId, CardData>(CardId.Of("c2"), new CardData("normal", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardId, IReadOnlyList<IEffect>>(
                    CardId.Of("c1"),
                    new IEffect[]
                    {
                        new ChoiceEffect(new IReadOnlyList<IEffect>[]
                        {
                            // 選択 0: 通常効果(Instinct なし)
                            new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, Delta: 1) },
                            // 選択 1: KeywordedEffect([Instinct], _) を含む
                            new IEffect[]
                            {
                                new KeywordedEffect(new[] { Keyword.Instinct }, new AssociatableMarkerEffect()),
                            },
                        }),
                    }),
            };
            var rule = new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());
            var session = NewSession(handCount: 2);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0)), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-213")]
        public void Given_TimeOfDayBranch内にInstinct_When_IsLegalMove_Then_false()
        {
            // TimeOfDayBranchEffect の NightEffects に KeywordedEffect([Instinct], _) が nest されているケース
            // (ADR-0011 §6「夢」カードの想定パターン、再帰 walk で検出される)
            var entries = new[]
            {
                new KeyValuePair<CardId, CardData>(CardId.Of("c1"), new CardData("nested instinct", new Dictionary<string, int>())),
                new KeyValuePair<CardId, CardData>(CardId.Of("c2"), new CardData("normal", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardId, IReadOnlyList<IEffect>>(
                    CardId.Of("c1"),
                    new IEffect[]
                    {
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new KeywordedEffect(new[] { Keyword.Instinct }, new AssociatableMarkerEffect()),
                            },
                            morningEffects: new IEffect[0]),
                    }),
            };
            var rule = new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());
            var session = NewSession(handCount: 2);
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0)), Is.False);
        }
    }
}
