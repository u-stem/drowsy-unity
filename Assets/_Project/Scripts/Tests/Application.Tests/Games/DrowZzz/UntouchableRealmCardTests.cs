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
    /// カード No.06「牙の届かぬ領域」の統合テスト(DZ-279 〜 DZ-285、2026-05-17 で導入)。
    /// Frenzy(狂乱)キーワード + `DoubleBedDamageSdpInfluenceMarkerEffect` を相手に付与する
    /// 戦術カード。時間帯非依存の即時効果 + ベッド破損 SDP 変動 2 倍化 Influence の長期影響を統合的に検証する。
    /// </summary>
    [TestFixture]
    public sealed class UntouchableRealmCardTests
    {
        // ===== ヘルパー =====

        private static readonly CardTypeId UntouchableTypeId = CardTypeId.Of("06");

        // 「牙の届かぬ領域」が付与する継続影響:OwnPhaseStart で 2 倍化 marker、カウント 4
        private static PlayerInfluence BedDamage2xInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new DoubleBedDamageSdpInfluenceMarkerEffect(),
                4);

        // 「牙の届かぬ領域」の効果列(最上位 3 件、3 件目を Frenzy 包みでカード全体に Frenzy 性質付与)
        private static IEffect[] UntouchableRealmEffects() => new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -12),
            new AdjustSdpEffect(SdpTarget.Opponent, -4),
            new KeywordedEffect(new[] { Keyword.Frenzy },
                new ApplyInfluenceEffect(SdpTarget.Opponent, BedDamage2xInfluence())),
        };

        private static InMemoryCardCatalog NewCatalogWithCardSix()
        {
            var card06 = new CardData("牙の届かぬ領域", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(UntouchableTypeId, card06),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    UntouchableTypeId,
                    (IReadOnlyList<IEffect>)UntouchableRealmEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // p1 の手札に Card "06" を持たせる session を構築(N=2)。
        // p2Influences / bedDamageP2 で Tick テストの初期状態を設定。
        private static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForPlay,
            int turnNumber = 1,
            IReadOnlyList<PlayerInfluence> p2Influences = null,
            int bedDamageP2 = 0)
        {
            var influences = p2Influences == null
                ? null
                : new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = p2Influences,
                };
            var bedDamages = bedDamageP2 != 0
                ? new Dictionary<PlayerId, int>
                {
                    [PlayerId.Of("p1")] = 0,
                    [PlayerId.Of("p2")] = bedDamageP2,
                }
                : null;
            return SessionFactory.NewSession(
                phase: phase,
                p0Hand: new Hand(new[] { CardId.Of(UntouchableTypeId, 0) }),
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences,
                bedDamages: bedDamages);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-279 / 280 / 281: Card 06 をプレイ(時間帯非依存)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-279")]
        public void Given_任意フェーズ_When_Card06をプレイ_Then_自分のSDPがマイナス12()
        {
            // Given(時間帯非依存のため turnNumber=1 (夜) でも 33 (朝) でも同じ結果。代表値 1 で検証)
            var rule = NewRule(NewCatalogWithCardSix());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(UntouchableTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-12));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-280")]
        public void Given_任意フェーズ_When_Card06をプレイ_Then_相手のSDPがマイナス4()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardSix());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(UntouchableTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-4));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-281")]
        public void Given_任意フェーズ_When_Card06をプレイ_Then_相手のInfluencesにBedDamage2xが付与される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardSix());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(UntouchableTypeId, 0)));
            // Then(p2 の Influences に BedDamage2xInfluence が追加)
            Assert.That(next.Influences[PlayerId.Of("p2")], Contains.Item(BedDamage2xInfluence()));
        }

        // ===== DZ-283: 2 倍化 Influence 保有時のベッド破損 Tick(SDP -16)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-283")]
        public void Given_p2が2xInfluence保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス16()
        {
            // Given(p2 が 2 倍化 Influence 保有 + BedDamages[p2]=40%、p1 が WaitingForEndTurn)
            var rule = NewRule(NewCatalogWithCardSix());
            var sessionInPlay = NewSession(
                turnNumber: 1,
                p2Influences: new[] { BedDamage2xInfluence() },
                bedDamageP2: 40);
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When(EndTurn で current=p2 に rotate → ApplyBedDamageToCurrentPlayer 内で 2 倍化適用)
            var next = rule.Apply(session, new EndTurnAction());
            // Then(40/5=8 → 2 倍化で 16、SDP -16)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-16));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-283")]
        public void Given_p2が2xInfluence保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_Influenceの残カウントは不変4()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardSix());
            var sessionInPlay = NewSession(
                turnNumber: 1,
                p2Influences: new[] { BedDamage2xInfluence() },
                bedDamageP2: 40);
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(ADR-0020:p2 Tick で TickEffect 適用のみ、count は p2 自身の EndTurn で -1 されるまで 4 のまま)
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(4));
        }

        // ===== DZ-284: 2 倍化 Influence 非保有時の通常ベッド破損 Tick(SDP -8、非リグレッション)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-284")]
        public void Given_p2が2xInfluence非保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス8()
        {
            // Given(p2 が Influence 持たない、BedDamages[p2]=40%、p1 WaitingForEndTurn)
            var rule = NewRule(NewCatalogWithCardSix());
            var sessionInPlay = NewSession(turnNumber: 1, bedDamageP2: 40);
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(通常計算経路 40/5=8、2 倍化なし、非リグレッション)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-8));
        }

        // ===== DZ-282: Frenzy で Card "06" は CounterAction の target にできない(DreamCardTests DZ-237 同パターン)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-282")]
        public void Given_Card06がField_WaitingForCounter_When_p2がCounterActionでCard06をtarget_Then_IsLegalMoveがfalse()
        {
            // Given(WaitingForCounterResponse、p1 の Card "06" を Field に出した直後、p2 が Counter キーワード持ちカードを保有)
            // 簡素化のため、Counter キーワード持ちダミーカード c_counter を catalog に登録し、p2 の手札に保持する
            // (DreamCardTests DZ-237 と同パターン、ADR-0011 §4.3 / M3-PR5b)。
            var untouchable = new CardData("牙の届かぬ領域", new Dictionary<string, int>());
            var counterCard = new CardData("c_counter", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(UntouchableTypeId, untouchable),
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("c_counter"), counterCard),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    UntouchableTypeId,
                    (IReadOnlyList<IEffect>)UntouchableRealmEffects()),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("c_counter"),
                    new IEffect[]
                    {
                        // Counter キーワードを持つカードであることを表す最低限の効果列(delta=0 ダミー、DreamCardTests DZ-237 と同方針)
                        new KeywordedEffect(
                            new[] { Keyword.Counter },
                            new AdjustSdpEffect(SdpTarget.Self, 0)),
                    }),
            };
            var catalog = new InMemoryCardCatalog(entries, effects);
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());

            // Field に Card "06"、p2 (current) が手札に c_counter、WaitingForCounterResponse。
            // SessionFactory 命名規約注記(code-reviewer P-3 反映 2026-05-17):
            //  - p0Hand = PlayerId.Of("p1") の手札、p1Hand = PlayerId.Of("p2") の手札(0-indexed プレイヤースロット)
            //  - 本テストは p2 = current(currentPlayerIndex=1)で c_counter を保有させたいため p1Hand 引数に渡す
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForCounterResponse,
                currentPlayerIndex: 1,
                p1Hand: new Hand(new[] { CardId.Of(CardTypeId.Of("c_counter"), 0) }),
                field: new Pile(new[] { CardId.Of(UntouchableTypeId, 0) }));

            // When
            var legal = rule.IsLegalMove(
                session,
                new CounterAction(CardId.Of(CardTypeId.Of("c_counter"), 0), CardId.Of(UntouchableTypeId, 0)));
            // Then(Card "06" は Frenzy 持ち → 反撃を受けない、illegal、ADR-0011 §4.5)
            Assert.That(legal, Is.False);
        }

        // ===== DZ-285: カウント 1 Marker は p2 フェーズ全体で機能、p2 EndTurn で除去(ADR-0020) =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-285")]
        public void Given_p2が2xInfluenceカウント1保有_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のInfluencesはカウント1で残存()
        {
            // Given(p2 が 2 倍化 Influence カウント 1、p1 WaitingForEndTurn → EndTurn で p2 自フェーズへ)
            // ADR-0020 後:p1 EndTurn 冒頭で p1 Decrement(no-op、p1 影響なし)→ Turn.Next で p2 へ →
            // ApplyBedDamageToCurrentPlayer で marker 検出して 2 倍化(BedDamage 0% なので結果も 0)→
            // p2 Tick で TickEffect (marker no-op) 適用、count 1 のまま残存。除去は p2 自身の EndTurn まで遅延。
            var rule = NewRule(NewCatalogWithCardSix());
            var inf = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new DoubleBedDamageSdpInfluenceMarkerEffect(),
                1);
            var sessionInPlay = NewSession(turnNumber: 1, p2Influences: new[] { inf });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(ADR-0020:count=1 marker は p2 フェーズで機能、count=1 のまま残存)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(1));
        }
    }
}
