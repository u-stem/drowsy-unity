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
    /// カード No.11「機械仕掛けの冬将軍」の統合テスト(DZ-323 〜 DZ-330、2026-05-17 で導入)。
    /// 狂乱(Frenzy)持ち + 自分 SDP -4 / 相手 SDP -8 + 乙に永続「自フェーズ開始時に SDP-n(n = Hand.Count)」の動的 Influence 付与。
    /// 動的計算 TickEffect の初導入(`AdjustSdpByHandCountEffect`)、Hand.Count=0 で graceful no-op。
    /// </summary>
    [TestFixture]
    public sealed class MechanicalWinterGeneralCardTests
    {
        // ===== ヘルパー =====

        private static readonly CardTypeId WinterGeneralTypeId = CardTypeId.Of("11");

        // 「機械仕掛けの冬将軍」が付与する永続 Influence
        private static PlayerInfluence WinterGeneralInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpByHandCountEffect(),
                InfluenceConstants.Perpetual);

        // 「機械仕掛けの冬将軍」の効果列(最上位 3 件、3 件目を Frenzy 包みでカード全体に Frenzy 性質付与)
        private static IEffect[] WinterGeneralEffects() => new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -4),
            new AdjustSdpEffect(SdpTarget.Opponent, -8),
            new KeywordedEffect(new[] { Keyword.Frenzy },
                new ApplyInfluenceEffect(SdpTarget.Opponent, WinterGeneralInfluence())),
        };

        private static InMemoryCardCatalog NewCatalogWithCardEleven()
        {
            var card11 = new CardData("機械仕掛けの冬将軍", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(WinterGeneralTypeId, card11),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    WinterGeneralTypeId,
                    (IReadOnlyList<IEffect>)WinterGeneralEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // p1 の手札に Card "11" を持たせる session(プレイ前検証用)
        private static DrowZzzGameSession NewSessionWithCardInHand(int turnNumber = 1)
        {
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(WinterGeneralTypeId, 0) }),
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));
        }

        // Tick 検証用:p1 current / WaitingForEndTurn、p2 が本 Influence + p2 Hand を引数で制御。
        // SessionFactory 命名規約注記(code-reviewer P-3 反映 2026-05-17):
        //  - 本ヘルパーの引数 `p2Hand` は意味的に「PlayerId.Of("p2") の手札」だが、
        //    内部で SessionFactory.NewSession の `p1Hand` パラメータに渡している。
        //  - SessionFactory.NewSession の `p1Hand` パラメータ名は「0-indexed プレイヤースロット 1 番 = PlayerId "p2"」を意味する
        //    (SessionFactory.cs:118-119、`p0Hand` = "p1"、`p1Hand` = "p2" のスロット番号 vs PlayerId 文字列のズレ)。
        //  - 将来 SessionFactory のパラメータ命名を `player1Hand` / `player2Hand` に整理する別 chore PR で同時解消予定。
        private static DrowZzzGameSession NewSessionForTick(
            IReadOnlyList<CardId> p2Hand,
            IReadOnlyList<PlayerInfluence> p2Influences)
        {
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = p2Influences,
            };
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForEndTurn,
                currentPlayerIndex: 0,
                p1Hand: p2Hand == null ? Hand.Empty : new Hand(p2Hand),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-324 / 325 / 326: Card 11 をプレイ(時間帯非依存)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-324")]
        public void Given_任意フェーズ_When_Card11をプレイ_Then_自分のSDPがマイナス4()
        {
            // Given(時間帯非依存のため turnNumber=1 (夜) でも 33 (朝) でも同じ結果)
            var rule = NewRule(NewCatalogWithCardEleven());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(WinterGeneralTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-325")]
        public void Given_任意フェーズ_When_Card11をプレイ_Then_相手のSDPがマイナス8()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardEleven());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(WinterGeneralTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-8));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-326")]
        public void Given_任意フェーズ_When_Card11をプレイ_Then_相手のInfluencesにWinterGeneralが付与される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardEleven());
            var session = NewSessionWithCardInHand();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(WinterGeneralTypeId, 0)));
            // Then(p2 の Influences に WinterGeneralInfluence が追加)
            Assert.That(next.Influences[PlayerId.Of("p2")], Contains.Item(WinterGeneralInfluence()));
        }

        // ===== DZ-327: Frenzy で Card "11" は CounterAction の target にできない =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-327")]
        public void Given_Card11がField_WaitingForCounter_When_p2がCounterActionでCard11をtarget_Then_IsLegalMoveがfalse()
        {
            // Given(WaitingForCounterResponse、p1 の Card "11" を Field に出した直後、p2 が Counter キーワード持ちカードを保有)
            // 簡素化のため Counter キーワード持ちダミーカード c_counter を catalog に登録(No.06 UntouchableRealmCardTests と同パターン)
            var winterCard = new CardData("機械仕掛けの冬将軍", new Dictionary<string, int>());
            var counterCard = new CardData("c_counter", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(WinterGeneralTypeId, winterCard),
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("c_counter"), counterCard),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    WinterGeneralTypeId,
                    (IReadOnlyList<IEffect>)WinterGeneralEffects()),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("c_counter"),
                    new IEffect[]
                    {
                        new KeywordedEffect(
                            new[] { Keyword.Counter },
                            new AdjustSdpEffect(SdpTarget.Self, 0)),
                    }),
            };
            var rule = new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());

            // Field に Card "11"、p2 (currentPlayerIndex=1) が手札に c_counter、WaitingForCounterResponse
            // SessionFactory 命名規約:p1Hand = PlayerId.Of("p2") の手札(0-indexed プレイヤースロット)
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForCounterResponse,
                currentPlayerIndex: 1,
                p1Hand: new Hand(new[] { CardId.Of(CardTypeId.Of("c_counter"), 0) }),
                field: new Pile(new[] { CardId.Of(WinterGeneralTypeId, 0) }));

            // When
            var legal = rule.IsLegalMove(
                session,
                new CounterAction(CardId.Of(CardTypeId.Of("c_counter"), 0), CardId.Of(WinterGeneralTypeId, 0)));
            // Then(Card "11" は Frenzy 持ち → 反撃を受けない、illegal、ADR-0011 §4.5)
            Assert.That(legal, Is.False);
        }

        // ===== DZ-328: 動的計算 — Hand.Count=3 で SDP-3 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-328")]
        public void Given_p2が本Influence保有_HandCount3_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス3()
        {
            // Given(p2 が本 Influence 保有 + Hand.Count=3、p1 が WaitingForEndTurn)
            var rule = NewRule(NewCatalogWithCardEleven());
            var p2Hand = new[]
            {
                CardId.Of(CardTypeId.Of("X"), 0),
                CardId.Of(CardTypeId.Of("Y"), 0),
                CardId.Of(CardTypeId.Of("Z"), 0),
            };
            var session = NewSessionForTick(p2Hand: p2Hand, p2Influences: new[] { WinterGeneralInfluence() });
            // When(EndTurn で current=p2 に rotate → Tick で動的計算 SDP -= Hand.Count(=3))
            var next = rule.Apply(session, new EndTurnAction());
            // Then(SDP[p2] -= 3)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-3));
        }

        // ===== DZ-329: graceful no-op — Hand.Count=0 で SDP 不変 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-329")]
        public void Given_p2が本Influence保有_HandCount0_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPは不変()
        {
            // Given(p2 が本 Influence 保有 + Hand.Count=0、p1 が WaitingForEndTurn)
            var rule = NewRule(NewCatalogWithCardEleven());
            var session = NewSessionForTick(p2Hand: null, p2Influences: new[] { WinterGeneralInfluence() });
            // When(EndTurn で current=p2 に rotate → Tick で動的計算、Hand.Count=0 で graceful no-op)
            var next = rule.Apply(session, new EndTurnAction());
            // Then(SDP[p2] は初期値 0 のまま)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0));
        }

        // ===== DZ-331: 動的計算 — Hand.Count=1 最小非ゼロ境界 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-331")]
        public void Given_p2が本Influence保有_HandCount1_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス1()
        {
            // Given(p2 が本 Influence 保有 + Hand.Count=1、最小非ゼロ境界、code-reviewer P-1 反映 2026-05-17)
            var rule = NewRule(NewCatalogWithCardEleven());
            var p2Hand = new[] { CardId.Of(CardTypeId.Of("X"), 0) };
            var session = NewSessionForTick(p2Hand: p2Hand, p2Influences: new[] { WinterGeneralInfluence() });
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(SDP[p2] -= 1)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-1));
        }

        // ===== DZ-332: 他プレイヤー(p1)の SDP は変更されない(非リグレッション、code-reviewer W-2 反映)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-332")]
        public void Given_p2が本Influence保有_HandCount3_p1current_When_p1EndTurnでp2フェーズへ_Then_p1のSDPは不変()
        {
            // Given(DZ-328 と同シナリオ、p1 の SDP が触られないことを独立に検証)
            var rule = NewRule(NewCatalogWithCardEleven());
            var p2Hand = new[]
            {
                CardId.Of(CardTypeId.Of("X"), 0),
                CardId.Of(CardTypeId.Of("Y"), 0),
                CardId.Of(CardTypeId.Of("Z"), 0),
            };
            var session = NewSessionForTick(p2Hand: p2Hand, p2Influences: new[] { WinterGeneralInfluence() });
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(p1 の SDP は初期値 0 のまま、`kv.Key.Equals(currentPlayer.Id)` ガード回帰防御)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0));
        }

        // ===== DZ-333: 動的計算 — Hand.Count=5 大き目境界(累積バグ防御)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-333")]
        public void Given_p2が本Influence保有_HandCount5_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス5()
        {
            // Given(p2 が本 Influence 保有 + Hand.Count=5、大き目境界で foreach 累積バグ防御、code-reviewer P-1 反映)
            var rule = NewRule(NewCatalogWithCardEleven());
            var p2Hand = new[]
            {
                CardId.Of(CardTypeId.Of("A"), 0),
                CardId.Of(CardTypeId.Of("B"), 0),
                CardId.Of(CardTypeId.Of("C"), 0),
                CardId.Of(CardTypeId.Of("D"), 0),
                CardId.Of(CardTypeId.Of("E"), 0),
            };
            var session = NewSessionForTick(p2Hand: p2Hand, p2Influences: new[] { WinterGeneralInfluence() });
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(SDP[p2] -= 5、foreach が 1 回だけ -5 を適用、累積で -10 等にならない)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-5));
        }

        // ===== DZ-330: ADR-0020 — Tick は count 不変、Perpetual は実質除去されない =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-330")]
        public void Given_p2が本Influence保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluenceRemainingCountは不変Perpetual()
        {
            // Given(p2 が本 Influence Perpetual 保有、p1 が WaitingForEndTurn)
            var rule = NewRule(NewCatalogWithCardEleven());
            var p2Hand = new[] { CardId.Of(CardTypeId.Of("X"), 0) };
            var session = NewSessionForTick(p2Hand: p2Hand, p2Influences: new[] { WinterGeneralInfluence() });
            // When(ADR-0020:p2 Tick で TickEffect 適用のみ、count 不変)
            var next = rule.Apply(session, new EndTurnAction());
            // Then(Influence の RemainingCount は Perpetual のまま、p2 自身の EndTurn で -1 されるが Perpetual は実質除去されない)
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(InfluenceConstants.Perpetual));
        }
    }
}
