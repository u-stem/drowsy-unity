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
    /// カード No.10「安直過ぎる一手」の統合テスト(DZ-314 〜 DZ-322)。
    /// 御業(自己コスト SDP -10)+ 相手のベッド 30% 破損 + カウント 1 の `RestrictDrawCardInfluenceMarkerEffect` を相手に付与し、
    /// 相手の次の自フェーズで `DrawCardAction` を illegal 化する戦術カード。
    /// stuck 化 Marker 保有時は `EndTurnAction` が全フェーズで合法化され進行不能化を回避する。
    /// </summary>
    [TestFixture]
    public sealed class EasyPlayCardTests
    {
        // ===== ヘルパー =====

        private static readonly CardTypeId EasyPlayTypeId = CardTypeId.Of("10");

        // 「安直過ぎる一手」が付与する継続影響:OwnPhaseStart で「ドロー禁止」marker、カウント 1
        private static PlayerInfluence EasyPlayInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictDrawCardInfluenceMarkerEffect(),
                1);

        // 「安直過ぎる一手」の効果列(時間帯非依存、最上位 3 件、±0 は省略)
        private static IEffect[] EasyPlayEffects() => new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -10),
            new DamageBedEffect(SdpTarget.Opponent, 30),
            new ApplyInfluenceEffect(SdpTarget.Opponent, EasyPlayInfluence()),
        };

        private static InMemoryCardCatalog NewCatalogWithCardTen()
        {
            var card10 = new CardData("安直過ぎる一手", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(EasyPlayTypeId, card10),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    EasyPlayTypeId,
                    (IReadOnlyList<IEffect>)EasyPlayEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // p1 の手札に Card "10" を持たせる session(プレイ前検証用、p1 current)
        private static DrowZzzGameSession NewSessionWithCardInHand(int turnNumber = 1)
        {
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(EasyPlayTypeId, 0) }),
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));
        }

        // p2 に EasyPlayMarker を保有させた session(illegal-move 検証用)
        private static DrowZzzGameSession NewSessionWithP2Marker(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForDraw,
            int currentPlayerIndex = 1,
            Hand p1Hand = null)
        {
            // SessionFactory 命名規約:p0Hand = PlayerId.Of("p1") の手札、p1Hand = PlayerId.Of("p2") の手札
            // (SessionFactory.cs:118-119、0-indexed プレイヤースロットと PlayerId 文字列のズレ)
            var p2Marker = EasyPlayInfluence();
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = new[] { p2Marker },
            };
            return SessionFactory.NewSession(
                phase: phase,
                currentPlayerIndex: currentPlayerIndex,
                p1Hand: p1Hand,
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-315 / 316 / 317: Card 10 をプレイ(時間帯非依存)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-315")]
        public void Given_任意フェーズ_When_Card10をプレイ_Then_自分のSDPがマイナス10()
        {
            // Given(時間帯非依存のため turnNumber=1 (夜) でも 33 (朝) でも同じ結果。代表値 1 で検証)
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(EasyPlayTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-10));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-316")]
        public void Given_任意フェーズ_When_Card10をプレイ_Then_相手のBedDamageがプラス30()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(EasyPlayTypeId, 0)));
            // Then(BedDamages[p2] = 0 → 30、DamageBedEffect(Opponent, 30) の効果)
            Assert.That(next.BedDamages[PlayerId.Of("p2")], Is.EqualTo(30));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-317")]
        public void Given_任意フェーズ_When_Card10をプレイ_Then_相手のInfluencesにEasyPlayMarkerが付与される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(EasyPlayTypeId, 0)));
            // Then(p2 の Influences に EasyPlayInfluence が追加)
            Assert.That(next.Influences[PlayerId.Of("p2")], Contains.Item(EasyPlayInfluence()));
        }

        // ===== DZ-318: 本 Marker 保有時、DrawCardAction が illegal =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-318")]
        public void Given_p2が本Markerカウント1保有_When_p2がDrawCardActionでIsLegalMove_Then_false()
        {
            // Given(p2 が current、WaitingForDraw、本 Marker を保有)
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForDraw,
                currentPlayerIndex: 1);
            // When / Then(本 Marker walk で illegal、山札からの手段ドロー禁止)
            Assert.That(rule.IsLegalMove(session, new DrawCardAction()), Is.False);
        }

        // ===== DZ-319: 本 Marker 保有時、全フェーズで EndTurnAction が legal(3 件分割)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-319")]
        public void Given_p2が本Markerカウント1保有_WaitingForDraw_When_p2がEndTurnActionでIsLegalMove_Then_true()
        {
            // Given(p2 が current、WaitingForDraw、本 Marker を保有 = stuck 状態)
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForDraw,
                currentPlayerIndex: 1);
            // When
            var legal = rule.IsLegalMove(session, new EndTurnAction());
            // Then(stuck 化 Marker 保有時の全フェーズ合法化、脱出弁)
            Assert.That(legal, Is.True);
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-319")]
        public void Given_p2が本Markerカウント1保有_WaitingForPlay_When_p2がEndTurnActionでIsLegalMove_Then_true()
        {
            // Given(p2 が current、WaitingForPlay、本 Marker 保有)
            // No.10 単独では WaitingForPlay は stuck ではないが、ホワイトリスト方式は「Marker 保有時は
            // PhaseState 問わず合法化」する設計のため、本フェーズでも EndTurnAction は合法でなければならない。
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 1);
            // When
            var legal = rule.IsLegalMove(session, new EndTurnAction());
            // Then(Marker 保有時は WaitingForEndTurn / WaitingForPlay / WaitingForDraw すべてで合法)
            Assert.That(legal, Is.True);
        }

        // ===== DZ-320: 本 Marker 保有時でも他アクション(PlayCardAction)は通常判定 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-320")]
        public void Given_p2が本Markerカウント1保有_When_p2がPlayCardActionでIsLegalMove_Then_本Markerは影響せず通常判定()
        {
            // Given(p2 が current、WaitingForPlay、本 Marker 保有 + 手札に任意の Card "X")
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
            // When / Then(本 Marker は PlayCardAction を制限しない、No.09 とは別概念)
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(anyCard)), Is.True);
        }

        // ===== DZ-321: カウント 1 Marker は p2 フェーズ全体で機能、count 不変 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-321")]
        public void Given_p2が本Markerカウント1保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluencesはカウント1で残存()
        {
            // Given(p1 current、WaitingForEndTurn、p2 が本 Marker 保有)
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 0);
            // When(p1 EndTurn → p1 Decrement(no-op、p1 影響なし)→ Turn.Next で p2 → p2 Tick(marker no-op、count 不変))
            var next = rule.Apply(session, new EndTurnAction());
            // Then(p2 Influence は count=1 のまま残存)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(1));
        }

        // ===== DZ-322: p2 自身の EndTurn 冒頭で Marker が除去される(Decrement、2 経路で対称検証)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-322")]
        public void Given_p2が本Markerカウント1保有_p2current_WaitingForDraw_When_p2EndTurn_Then_p2のInfluences件数が0()
        {
            // Given(p2 current、WaitingForDraw、本 Marker 保有 = stuck 脱出弁での EndTurn 経路)
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForDraw,
                currentPlayerIndex: 1);
            // When(WaitingForDraw でも stuck Marker 保有時は EndTurn 合法、p2 EndTurn 冒頭で Decrement で 1→0 除去)
            var next = rule.Apply(session, new EndTurnAction());
            // Then(p2 Influences 件数 = 0)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(0));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-322")]
        public void Given_p2が本Markerカウント1保有_p2current_WaitingForEndTurn_When_p2EndTurn_Then_p2のInfluences件数が0()
        {
            // Given(p2 current、WaitingForEndTurn = 通常合法経路の EndTurn、Decrement が PhaseState 非依存で機能することを検証)
            // No.09 DZ-313(ForcePlayCardTests)との対称性確保(code-reviewer W-4 反映 2026-05-17)。
            var rule = NewRule(NewCatalogWithCardTen());
            var session = NewSessionWithP2Marker(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 1);
            // When(WaitingForEndTurn でも Decrement で 1→0 除去)
            var next = rule.Apply(session, new EndTurnAction());
            // Then(p2 Influences 件数 = 0)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(0));
        }
    }
}
