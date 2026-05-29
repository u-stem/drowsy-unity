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
    /// カード No.09「強引過ぎる一手」の統合テスト(DZ-303 〜 DZ-313)。
    /// 御業(高火力 SDP 変動 -10/+10)+ カウント 1 の `RestrictAllUsageAndAbandonInfluenceMarkerEffect` を相手に付与し、
    /// 相手の次の自フェーズで `PlayCardAction` / `CounterAction` / `AbandonAction` の 3 アクションを illegal 化する戦術カード。
    /// count -1 タイミングは EndTurn 冒頭のため、count=1 Marker が自フェーズ中は正しく機能する。
    /// </summary>
    [TestFixture]
    public sealed class ForcePlayCardTests
    {
        // ===== ヘルパー =====

        private static readonly CardTypeId ForcePlayTypeId = CardTypeId.Of("09");

        // 「強引過ぎる一手」が付与する継続影響:OwnPhaseStart で「使用・放棄禁止」marker、カウント 1
        private static PlayerInfluence ForcePlayInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictAllUsageAndAbandonInfluenceMarkerEffect(),
                1);

        // 「強引過ぎる一手」の効果列(時間帯非依存、最上位 3 件)
        private static IEffect[] ForcePlayEffects() => new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -10),
            new AdjustSdpEffect(SdpTarget.Opponent, 10),
            new ApplyInfluenceEffect(SdpTarget.Opponent, ForcePlayInfluence()),
        };

        private static InMemoryCardCatalog NewCatalogWithCardNine()
        {
            var card09 = new CardData("強引過ぎる一手", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(ForcePlayTypeId, card09),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    ForcePlayTypeId,
                    (IReadOnlyList<IEffect>)ForcePlayEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // p1 の手札に Card "09" を持たせる session(プレイ前検証用、p1 current)
        private static DrowZzzGameSession NewSessionWithCardInHand(int turnNumber = 1)
        {
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(ForcePlayTypeId, 0) }),
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));
        }

        // 引数指定で p2 の Influences と current player index / phase を切り替える session(illegal-move 検証用)
        private static DrowZzzGameSession NewSessionWithP2Marker(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForPlay,
            int currentPlayerIndex = 1,
            Hand p1Hand = null,
            Pile field = null,
            IReadOnlyList<PendingCounteredEffect> pending = null)
        {
            var p2Marker = ForcePlayInfluence();
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = new[] { p2Marker },
            };
            return SessionFactory.NewSession(
                phase: phase,
                currentPlayerIndex: currentPlayerIndex,
                p1Hand: p1Hand,
                field: field,
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences,
                pendingCounteredEffects: pending);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-304 / 305 / 306: Card 09 をプレイ(時間帯非依存)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-304")]
        public void Given_任意フェーズ_When_Card09をプレイ_Then_自分のSDPがマイナス10()
        {
            // Given(時間帯非依存のため turnNumber=1 (夜) でも 33 (朝) でも同じ結果。代表値 1 で検証)
            var rule = NewRule(NewCatalogWithCardNine());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(ForcePlayTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-10));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-305")]
        public void Given_任意フェーズ_When_Card09をプレイ_Then_相手のSDPがプラス10()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardNine());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(ForcePlayTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(10));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-306")]
        public void Given_任意フェーズ_When_Card09をプレイ_Then_相手のInfluencesにForcePlayMarkerが付与される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardNine());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(ForcePlayTypeId, 0)));
            // Then(p2 の Influences に ForcePlayInfluence が追加)
            Assert.That(next.Influences[PlayerId.Of("p2")], Contains.Item(ForcePlayInfluence()));
        }

        // ===== DZ-307: 本 Marker 保有時、PlayCardAction が illegal =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-307")]
        public void Given_p2が本Markerカウント1保有_When_p2がPlayCardActionでIsLegalMove_Then_false()
        {
            // Given(p2 が current、WaitingForPlay、本 Marker を保有。手札に任意の Card("X")を 1 枚)
            var anyCard = CardId.Of(CardTypeId.Of("X"), 0);
            var rule = new DrowZzzRule(
                new InMemoryCardCatalog(
                    new[] { new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("X"), new CardData("X", new Dictionary<string, int>())) },
                    new[] { new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("X"), (IReadOnlyList<IEffect>)System.Array.Empty<IEffect>()) }),
                new EffectInterpreter());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 1,
                p1Hand: new Hand(new[] { anyCard }));
            // When / Then(本 Marker walk で illegal、CardTypeId 非依存)
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(anyCard)), Is.False);
        }

        // ===== DZ-308: 本 Marker 保有時、AbandonAction が illegal =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-308")]
        public void Given_p2が本Markerカウント1保有_When_p2がAbandonActionでIsLegalMove_Then_false()
        {
            // Given(p2 が current、WaitingForPlay、本 Marker を保有。手札に任意の 1 枚)
            var anyCard = CardId.Of(CardTypeId.Of("X"), 0);
            var rule = new DrowZzzRule(
                new InMemoryCardCatalog(
                    new[] { new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("X"), new CardData("X", new Dictionary<string, int>())) },
                    new[] { new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CardTypeId.Of("X"), (IReadOnlyList<IEffect>)System.Array.Empty<IEffect>()) }),
                new EffectInterpreter());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 1,
                p1Hand: new Hand(new[] { anyCard }));
            // When / Then(本 Marker walk で illegal、AbandonChoice 非依存)
            Assert.That(rule.IsLegalMove(session, new AbandonAction(CardIndex: 0, Choice: AbandonChoice.GainSdp)), Is.False);
        }

        // ===== DZ-309: 本 Marker 保有時、CounterAction(経路 1)が illegal =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-309")]
        public void Given_p2が本Markerカウント1保有_When_p1フェーズ中の反撃CounterActionでIsLegalMove_Then_false()
        {
            // Given:p1 が currentPlayerIndex=0、WaitingForCounterResponse。p2 が反撃を打つ立場 + 本 Marker 保有。
            // p2 の手札に Counter キーワード持ちダミーカード "c_counter" を保有させる。
            // SessionFactory 命名規約注記:p0Hand = PlayerId.Of("p1") の手札、p1Hand = PlayerId.Of("p2") の手札
            //(SessionFactory.cs:118-119、0-indexed プレイヤースロットと PlayerId 文字列のズレ、code-reviewer S-2 反映 2026-05-17)
            var counterCard = new CardData("c_counter", new Dictionary<string, int>());
            var targetCard = new CardData("target", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("c_counter"), counterCard),
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("target"), targetCard),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("c_counter"),
                    new IEffect[]
                    {
                        new KeywordedEffect(
                            new[] { Keyword.Counter },
                            new AdjustSdpEffect(SdpTarget.Self, 0)),
                    }),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("target"),
                    (IReadOnlyList<IEffect>)System.Array.Empty<IEffect>()),
            };
            var rule = new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());

            // p2 の Influences に本 Marker、p2 (counterPlayerIndex=1) の手札に c_counter、Field に target、p1 current
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForCounterResponse,
                currentPlayerIndex: 0,
                p1Hand: new Hand(new[] { CardId.Of(CardTypeId.Of("c_counter"), 0) }),
                field: new Pile(new[] { CardId.Of(CardTypeId.Of("target"), 0) }));

            // When
            var legal = rule.IsLegalMove(
                session,
                new CounterAction(CardId.Of(CardTypeId.Of("c_counter"), 0), CardId.Of(CardTypeId.Of("target"), 0)));
            // Then(本 Marker walk で illegal、Counter キーワードを持っていても本 Marker が優先)
            Assert.That(legal, Is.False);
        }

        // ===== DZ-309: 本 Marker 保有時、CounterAction(経路 2:自フェーズの反撃の反撃)も illegal =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-309")]
        public void Given_p2が本Markerカウント1保有_When_p2自フェーズの反撃の反撃CounterActionでIsLegalMove_Then_false()
        {
            // Given:p2 が currentPlayerIndex=1、WaitingForEndTurn(自フェーズ中)。p2 が本 Marker 保有 + 反撃の反撃を打つ立場。
            // PendingCounteredEffects に 1 件のエントリ(CounterCard = "c_counter_b"、OriginalCard = "target_a")を入れて
            // p2 が「c_counter_b」を打ち消す反撃の反撃を試みるシナリオ(`IsLegalCounterAsCounterCounter` 経路)。
            var counterB = new CardData("c_counter_b", new Dictionary<string, int>());
            var counterA = new CardData("c_counter_a", new Dictionary<string, int>());
            var targetA = new CardData("target_a", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("c_counter_b"), counterB),
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("c_counter_a"), counterA),
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("target_a"), targetA),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("c_counter_b"),
                    new IEffect[]
                    {
                        new KeywordedEffect(
                            new[] { Keyword.Counter },
                            new AdjustSdpEffect(SdpTarget.Self, 0)),
                    }),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("c_counter_a"),
                    new IEffect[]
                    {
                        new KeywordedEffect(
                            new[] { Keyword.Counter },
                            new AdjustSdpEffect(SdpTarget.Self, 0)),
                    }),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("target_a"),
                    (IReadOnlyList<IEffect>)System.Array.Empty<IEffect>()),
            };
            var rule = new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());

            // p2 の Influences に本 Marker、p2 (currentPlayerIndex=1) の手札に c_counter_a
            // PendingCounteredEffects に 1 件(B が A を打ち消した記録)
            var p2Marker = ForcePlayInfluence();
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = new[] { p2Marker },
            };
            var pending = new[]
            {
                new PendingCounteredEffect(
                    CardId.Of(CardTypeId.Of("c_counter_b"), 0),
                    CardId.Of(CardTypeId.Of("target_a"), 0),
                    System.Array.Empty<IEffect>()),
            };
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 1,
                p1Hand: new Hand(new[] { CardId.Of(CardTypeId.Of("c_counter_a"), 0) }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences,
                pendingCounteredEffects: pending);

            // When(経路 2:Pending 最後エントリの CounterCard=c_counter_b を target に、c_counter_a で反撃の反撃)
            var legal = rule.IsLegalMove(
                session,
                new CounterAction(
                    CardId.Of(CardTypeId.Of("c_counter_a"), 0),
                    CardId.Of(CardTypeId.Of("c_counter_b"), 0)));
            // Then(本 Marker walk で illegal、IsLegalCounterAsCounterCounter 経路でも禁止)
            Assert.That(legal, Is.False);
        }

        // ===== DZ-310: 本 Marker 保有時でも AssociateAction は許可(連想可)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-310")]
        public void Given_p2が本Markerカウント1保有_When_p2がAssociateActionでIsLegalMove_Then_true()
        {
            // Given:p2 current、自フェーズ中(WaitingForDraw)、本 Marker 保有。
            // 連想対象カード "assoc" は AssociatableMarkerEffect を持つ + p2 TotalPoints は AssociationThreshold 以上(FDP 80)。
            var assocCard = new CardData("assoc", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("assoc"), assocCard),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("assoc"),
                    new IEffect[] { new AssociatableMarkerEffect() }),
            };
            var rule = new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());

            var p2Marker = ForcePlayInfluence();
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = new[] { p2Marker },
            };
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForDraw,
                currentPlayerIndex: 1,
                p1Hand: Hand.Empty,
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: DrowZzzAssociationConstants.AssociationThreshold),
                influences: influences);

            // When(p2 が assoc を連想)
            var legal = rule.IsLegalMove(session, new AssociateAction(CardId.Of(CardTypeId.Of("assoc"), 0)));
            // Then(本 Marker は AssociateAction を illegal 化しない、連想は明示禁止対象外)
            Assert.That(legal, Is.True);
        }

        // ===== DZ-311: 本 Marker 保有時、EndTurnAction は WaitingForEndTurn / WaitingForPlay 両方で legal(stuck 脱出弁)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-311")]
        public void Given_p2が本Markerカウント1保有_When_p2がEndTurnActionでIsLegalMove_Then_true()
        {
            // Given(p2 current、WaitingForEndTurn、本 Marker 保有、通常合法経路)
            var rule = NewRule(NewCatalogWithCardNine());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 1);
            // When
            var legal = rule.IsLegalMove(session, new EndTurnAction());
            // Then
            Assert.That(legal, Is.True);
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-311")]
        public void Given_p2が本Markerカウント1保有_WaitingForPlay_When_EndTurnAction_Then_stuck脱出弁で合法()
        {
            // Given(p2 current、WaitingForPlay、本 Marker 保有 = PlayCard/Abandon 両方禁止で stuck 化するフェーズ)
            // 本 Marker は stuck 化 Marker のため、EndTurnAction は WaitingForPlay でも legal 化される
            var rule = NewRule(NewCatalogWithCardNine());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 1);
            // When
            var legal = rule.IsLegalMove(session, new EndTurnAction());
            // Then(stuck 化 Marker 保有時の全フェーズ合法化、脱出弁が機能)
            Assert.That(legal, Is.True);
        }

        // ===== DZ-312: カウント 1 Marker は p2 フェーズ全体で機能、count 不変 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-312")]
        public void Given_p2が本Markerカウント1保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluencesはカウント1で残存()
        {
            // Given(p1 current、WaitingForEndTurn、p2 が本 Marker 保有)
            var rule = NewRule(NewCatalogWithCardNine());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 0);
            // When(p1 EndTurn → p1 Decrement(no-op、p1 影響なし)→ Turn.Next で p2 → p2 Tick(marker no-op、count 不変))
            var next = rule.Apply(session, new EndTurnAction());
            // Then(p2 Influence は count=1 のまま残存)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(1));
        }

        // ===== DZ-313: p2 自身の EndTurn 冒頭で Marker が除去される =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-313")]
        public void Given_p2が本Markerカウント1保有_p2current_When_p2EndTurn_Then_p2のInfluences件数が0()
        {
            // Given(p2 current、WaitingForEndTurn、p2 が本 Marker 保有)
            var rule = NewRule(NewCatalogWithCardNine());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 1);
            // When(p2 EndTurn 冒頭で p2 Decrement → count 1→0 で除去)
            var next = rule.Apply(session, new EndTurnAction());
            // Then(p2 Influences 件数 = 0)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(0));
        }
    }
}
