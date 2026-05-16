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
    /// カード No.02「緑の侵攻」の統合テスト(DZ-170 〜 DZ-178)。
    /// 選択式カード(<see cref="ChoiceEffect"/>)+ 影響付与(<see cref="ApplyInfluenceEffect"/>)+
    /// 影響削除(<see cref="RemoveInfluenceEffect"/>)+ 自フェーズ開始時 Tick の 4 つの新機構を統合的に検証する。
    /// </summary>
    /// <remarks>
    /// 単体 effect record の挙動は <c>ApplyInfluenceEffectTests</c> / <c>RemoveInfluenceEffectTests</c> /
    /// <c>ChoiceEffectTests</c> でカバー済。本テストは「カード 1 種類の効果列が end-to-end で動くこと」を
    /// <c>Category("Medium")</c> で検証する(<c>CupOfThreatCardTests</c> と同パターン)。
    /// </remarks>
    [TestFixture]
    public sealed class GreenInvasionCardTests
    {
        // ===== ヘルパー =====

        // 「緑の侵攻」が持つ継続影響: 自分のフェーズ開始時に SDP -5、カウント 3。
        private static PlayerInfluence GreenInvasionInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, -5),
                3);

        // 「緑の侵攻」の効果定義(ChoiceEffect 1 件で 2 分岐を保有)。
        // 選択 1: 自分 SDP -6 + 相手の影響 1 件消滅 + 相手に影響付与
        // 選択 2: 相手 SDP +6 + 自分の影響 1 件消滅 + 自分に影響付与
        private static IEffect GreenInvasionEffect() =>
            new ChoiceEffect(new IEffect[][]
            {
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -6),
                    new RemoveInfluenceEffect(SdpTarget.Opponent),
                    new ApplyInfluenceEffect(SdpTarget.Opponent, GreenInvasionInfluence()),
                },
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Opponent, 6),
                    new RemoveInfluenceEffect(SdpTarget.Self),
                    new ApplyInfluenceEffect(SdpTarget.Self, GreenInvasionInfluence()),
                },
            });

        // InMemoryCardCatalog に「緑の侵攻」(CardId "02") を登録して返す
        private static InMemoryCardCatalog NewCatalogWithCardTwo()
        {
            var card02 = new CardData("緑の侵攻", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("02"), card02),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("02"),
                    new IEffect[] { GreenInvasionEffect() }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // 現プレイヤー p1 の手札に Card "02" を持たせる Session を構築
        private static DrowZzzGameSession NewSessionWithCardInHand(
            IReadOnlyList<PlayerInfluence> p1Influences = null,
            IReadOnlyList<PlayerInfluence> p2Influences = null)
        {
            var p1Hand = new Hand(new[] { CardId.Of(CardTypeId.Of("02"), 0) });
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), p1Hand),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, Pile.Empty,
                new TurnState(1, 0));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = p1Influences ?? Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = p2Influences ?? Array.Empty<PlayerInfluence>(),
            };
            return new DrowZzzGameSession(gs, fdp, ddp, sdp, DdpPool.Empty, influences, DrowZzzPhaseState.WaitingForPlay, outcome: null, bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 }, System.Array.Empty<PendingCounteredEffect>());
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-170: 選択 1 をプレイ → 自分 SDP -6 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-170")]
        public void Given_p1current_When_Card02をChoice0でプレイ_Then_p1のSDPがマイナス6()
        {
            // Given
            var catalog = NewCatalogWithCardTwo();
            var rule = NewRule(catalog);
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("02"), 0), Choice: 0));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-6));
        }

        // ===== DZ-171: 選択 1 をプレイ → 相手に影響付与 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-171")]
        public void Given_p1current_When_Card02をChoice0でプレイ_Then_p2のInfluencesに1件追加()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardTwo());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("02"), 0), Choice: 0));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
        }

        // ===== DZ-172: 選択 1 で相手が既に影響保有 → index 指定で除去 + 新規付与 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-172")]
        public void Given_p2に既存影響1件_When_Choice0_index0で除去後新規付与_Then_p2のInfluences件数が1()
        {
            // Given(p2 が既存影響 1 件保有、除去 → 新規付与で件数 1 のまま)
            var rule = NewRule(NewCatalogWithCardTwo());
            var existing = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -3), 2);
            var session = NewSessionWithCardInHand(p2Influences: new[] { existing });
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("02"), 0), Choice: 0, InfluenceRemovalIndex: 0));
            // Then(既存削除 → 新規追加で 1 件)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-172")]
        public void Given_p2に既存影響1件_When_Choice0_index0で除去後新規付与_Then_残った影響は新規の方()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardTwo());
            var existing = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -3), 2);
            var session = NewSessionWithCardInHand(p2Influences: new[] { existing });
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("02"), 0), Choice: 0, InfluenceRemovalIndex: 0));
            // Then(残った 1 件は新規付与の GreenInvasionInfluence)
            Assert.That(next.Influences[PlayerId.Of("p2")][0], Is.EqualTo(GreenInvasionInfluence()));
        }

        // ===== DZ-173: 選択 2 をプレイ → 相手 SDP +6 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-173")]
        public void Given_p1current_When_Card02をChoice1でプレイ_Then_p2のSDPが6()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardTwo());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("02"), 0), Choice: 1));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(6));
        }

        // ===== DZ-174: 選択 2 をプレイ → 自分に影響付与 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-174")]
        public void Given_p1current_When_Card02をChoice1でプレイ_Then_p1のInfluencesに1件追加()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardTwo());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(CardTypeId.Of("02"), 0), Choice: 1));
            // Then
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(1));
        }

        // ===== DZ-175: 選択範囲外 → IsLegalMove で false =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-175")]
        public void Given_Card02をChoice2_範囲外_When_IsLegalMove_Then_false()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardTwo());
            var session = NewSessionWithCardInHand();
            // When / Then(Choice=2 は範囲外 [0, 1] のため illegal)
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(CardId.Of(CardTypeId.Of("02"), 0), Choice: 2)), Is.False);
        }

        // ===== DZ-176: フェーズ進行で自分の影響が Tick =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-176")]
        public void Given_p2に影響カウント3_p1ターン終了_When_EndTurn_Then_p2のSDPがマイナス5()
        {
            // Given(p1 が current で WaitingForEndTurn、p2 が GreenInvasionInfluence 保有)
            var rule = NewRule(NewCatalogWithCardTwo());
            var p2Inf = GreenInvasionInfluence();
            var sessionInPlay = NewSessionWithCardInHand(p2Influences: new[] { p2Inf });
            // PhaseState を WaitingForEndTurn に切り替え(EndTurn を適用可能にする、IsLegalMove 条件)
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When(EndTurn で current が p2 に rotate、p2 の影響が Tick されて SDP -5)
            var next = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-5));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-176")]
        public void Given_p2に影響カウント3_p1ターン終了_When_EndTurn_Then_p2のInfluenceカウントが2()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardTwo());
            var p2Inf = GreenInvasionInfluence();
            var sessionInPlay = NewSessionWithCardInHand(p2Influences: new[] { p2Inf });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(Tick 後にカウントが -1 されて 2 になる、まだ list に残る)
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(2));
        }

        // ===== DZ-177: カウント 1 から Tick で 0 → 除去 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-177")]
        public void Given_p2に影響カウント1_p1ターン終了_When_EndTurn_Then_p2のInfluences件数が0()
        {
            // Given(カウント 1 = 次 Tick で 0 到達 → 除去)
            var rule = NewRule(NewCatalogWithCardTwo());
            var p2Inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -5), 1);
            var sessionInPlay = NewSessionWithCardInHand(p2Influences: new[] { p2Inf });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(0));
        }

        // ===== DZ-178: 他プレイヤーの影響は Tick されない =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-178")]
        public void Given_p1とp2に各影響カウント3_p1ターン終了_When_EndTurn_Then_p1のInfluenceカウントは不変3()
        {
            // Given(EndTurn で current = p2 に rotate、p2 のみ Tick されるべき)
            var rule = NewRule(NewCatalogWithCardTwo());
            var p1Inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -1), 3);
            var p2Inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -5), 3);
            var sessionInPlay = NewSessionWithCardInHand(p1Influences: new[] { p1Inf }, p2Influences: new[] { p2Inf });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(p1 の影響は Tick されず、カウント 3 のまま)
            Assert.That(next.Influences[PlayerId.Of("p1")][0].RemainingCount, Is.EqualTo(3));
        }
    }
}
