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
    /// カード No.08「廻るための知恵」の統合テスト(DZ-294 〜 DZ-302、2026-05-17 で導入)。
    /// Instinct(本能)キーワード + 選択式 ChoiceEffect で自他いずれかに「ベッド破損 SDP 符号反転」永続影響を付与。
    /// 保有数の奇偶判定(1 件=反転 / 2 件=元に戻る / 3 件=反転)+ No.06 2 倍化との組み合わせ(逆転 → 2 倍化順)を統合検証。
    /// </summary>
    [TestFixture]
    public sealed class CirculatingWisdomCardTests
    {
        // ===== ヘルパー =====

        private static readonly CardTypeId WisdomTypeId = CardTypeId.Of("08");

        private static PlayerInfluence InvertBedDamageInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new InvertBedDamageSdpInfluenceMarkerEffect(),
                InfluenceConstants.Perpetual);

        // No.08 の効果列(最上位 ChoiceEffect、各 branch 内 ApplyInfluenceEffect を Keyworded([Instinct]) で包む。
        // ChoiceEffect を最上位に置かないと ApplyPlayCard の unwrap が機能せず NotImplementedException になる
        // (No.04 / No.05 / No.06 の「KeywordedEffect 内 nested 問題」と同根、2026-05-17 開発中に DZ-294/302 失敗で発覚)。
        // Instinct 性質は HasKeywordInEffects 再帰 walk(ChoiceEffect → Branches → KeywordedEffect)で OR 検出される)。
        private static IEffect[] CirculatingWisdomEffects() => new IEffect[]
        {
            new ChoiceEffect(new IReadOnlyList<IEffect>[]
            {
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Opponent, 5),
                    new KeywordedEffect(new[] { Keyword.Instinct },
                        new ApplyInfluenceEffect(SdpTarget.Self, InvertBedDamageInfluence())),
                },
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, 5),
                    new KeywordedEffect(new[] { Keyword.Instinct },
                        new ApplyInfluenceEffect(SdpTarget.Opponent, InvertBedDamageInfluence())),
                },
            }),
        };

        private static InMemoryCardCatalog NewCatalogWithCardEight()
        {
            var card08 = new CardData("廻るための知恵", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(WisdomTypeId, card08),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    WisdomTypeId,
                    (IReadOnlyList<IEffect>)CirculatingWisdomEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        private static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForPlay,
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
                p0Hand: new Hand(new[] { CardId.Of(WisdomTypeId, 0) }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences,
                bedDamages: bedDamages);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-294 / 295: Choice 0(自分強化) =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-294")]
        public void Given_任意フェーズ_When_Card08をChoice0でプレイ_Then_相手のSDPがプラス5()
        {
            var rule = NewRule(NewCatalogWithCardEight());
            var session = NewSession();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(WisdomTypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(5));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-295")]
        public void Given_任意フェーズ_When_Card08をChoice0でプレイ_Then_自分のInfluencesにInvertBedDamageが追加()
        {
            var rule = NewRule(NewCatalogWithCardEight());
            var session = NewSession();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(WisdomTypeId, 0), Choice: 0));
            Assert.That(next.Influences[PlayerId.Of("p1")], Contains.Item(InvertBedDamageInfluence()));
        }

        // ===== DZ-296 / 297: Choice 1(相手押し付け) =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-296")]
        public void Given_任意フェーズ_When_Card08をChoice1でプレイ_Then_自分のSDPがプラス5()
        {
            var rule = NewRule(NewCatalogWithCardEight());
            var session = NewSession();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(WisdomTypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(5));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-297")]
        public void Given_任意フェーズ_When_Card08をChoice1でプレイ_Then_相手のInfluencesにInvertBedDamageが追加()
        {
            var rule = NewRule(NewCatalogWithCardEight());
            var session = NewSession();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(WisdomTypeId, 0), Choice: 1));
            Assert.That(next.Influences[PlayerId.Of("p2")], Contains.Item(InvertBedDamageInfluence()));
        }

        // ===== DZ-298: Instinct で AbandonAction illegal =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-298")]
        public void Given_手札にCard08_When_Card08のCardIndexでAbandonAction_Then_IsLegalMoveがfalse()
        {
            // Given(p1 が Card "08" を手札 index 0 に保持)
            var rule = NewRule(NewCatalogWithCardEight());
            var session = NewSession();
            // When / Then(Instinct = 放棄対象不可、DZ-238 同パターン)
            Assert.That(rule.IsLegalMove(session, new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0)), Is.False);
        }

        // ===== DZ-299: 1 件保有で反転(回復方向) =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-299")]
        public void Given_p2がInvert1件保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがプラス8()
        {
            // Given(p2 が Invert 1 件、BedDamages[p2]=40%、p1 WaitingForEndTurn)
            var rule = NewRule(NewCatalogWithCardEight());
            var sessionInPlay = NewSession(
                p2Influences: new[] { InvertBedDamageInfluence() },
                bedDamageP2: 40);
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When(EndTurn で current=p2 に rotate → ApplyBedDamageToCurrentPlayer 内で奇偶反転)
            var next = rule.Apply(session, new EndTurnAction());
            // Then(40/5=8 の符号反転、SDP -= -8 = +8 回復)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(8));
        }

        // ===== DZ-300: 2 件保有で元に戻る(奇偶 = 偶数) =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-300")]
        public void Given_p2がInvert2件保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがマイナス8()
        {
            // Given(p2 が Invert 2 件、奇偶判定で偶数 = 元に戻る)
            var rule = NewRule(NewCatalogWithCardEight());
            var sessionInPlay = NewSession(
                p2Influences: new[] { InvertBedDamageInfluence(), InvertBedDamageInfluence() },
                bedDamageP2: 40);
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(40/5=8、反転 × 反転 = 元、通常の減算)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-8));
        }

        // ===== DZ-301: Invert 1 件 + Double 1 件で「逆転 → 2 倍化」順 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-301")]
        public void Given_p2がInvert1件Double1件保有_BedDamage40pct_p1current_When_p1EndTurnでp2フェーズへ_Then_p2のSDPがプラス16()
        {
            // Given(p2 が Invert 1 件 + Double 1 件保有)
            var rule = NewRule(NewCatalogWithCardEight());
            var doubleInf = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new DoubleBedDamageSdpInfluenceMarkerEffect(),
                4);
            var sessionInPlay = NewSession(
                p2Influences: new[] { InvertBedDamageInfluence(), doubleInf },
                bedDamageP2: 40);
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(SDP -8 → 反転 +8 → 2 倍化 +16、オーナー JIT 確定順序、SDP -= -16 = +16)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(16));
        }

        // ===== DZ-302: Choice 範囲外 → illegal =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-302")]
        public void Given_Card08をChoice2_範囲外_When_IsLegalMove_Then_false()
        {
            var rule = NewRule(NewCatalogWithCardEight());
            var session = NewSession();
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(CardId.Of(WisdomTypeId, 0), Choice: 2)), Is.False);
        }
    }
}
