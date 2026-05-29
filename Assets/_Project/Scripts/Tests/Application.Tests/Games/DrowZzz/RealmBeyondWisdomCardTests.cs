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
    /// カード No.07「知恵の及ばぬ領域」の統合テスト(DZ-287 〜 DZ-292、2026-05-17 で導入)。
    /// Frenzy(狂乱)キーワード + `RemoveInvertBedDamageInfluenceEffect`(No.08 由来 1 件削除)+
    /// `RestrictSpecificCardInfluenceEffect(CardTypeId.Of("08"))`(No.08 使用禁止 影響、No.04 由来既存型流用)を統合検証。
    /// </summary>
    [TestFixture]
    public sealed class RealmBeyondWisdomCardTests
    {
        // ===== ヘルパー =====

        private static readonly CardTypeId RealmBeyondTypeId = CardTypeId.Of("07");
        private static readonly CardTypeId WisdomTypeId = CardTypeId.Of("08");

        // No.07 が付与する影響: OwnPhaseStart で No.08 使用禁止 marker、カウント 4
        private static PlayerInfluence RestrictWisdomInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictSpecificCardInfluenceEffect(WisdomTypeId),
                4);

        // No.08 が付与する Invert influence(本テストでは「相手保有」の入力として使う)
        private static PlayerInfluence InvertBedDamageInfluence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new InvertBedDamageSdpInfluenceMarkerEffect(),
                InfluenceConstants.Perpetual);

        // No.07 の効果列(最上位 4 件、4 件目を Frenzy 包みで ApplyInfluence)
        private static IEffect[] RealmBeyondEffects() => new IEffect[]
        {
            new AdjustSdpEffect(SdpTarget.Self, -6),
            new AdjustSdpEffect(SdpTarget.Opponent, 5),
            new RemoveInvertBedDamageInfluenceEffect(SdpTarget.Opponent),
            new KeywordedEffect(new[] { Keyword.Frenzy },
                new ApplyInfluenceEffect(SdpTarget.Opponent, RestrictWisdomInfluence())),
        };

        private static InMemoryCardCatalog NewCatalogWithCardSeven()
        {
            var card07 = new CardData("知恵の及ばぬ領域", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(RealmBeyondTypeId, card07),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    RealmBeyondTypeId,
                    (IReadOnlyList<IEffect>)RealmBeyondEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        private static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForPlay,
            IReadOnlyList<PlayerInfluence> p2Influences = null)
        {
            var influences = p2Influences == null
                ? null
                : new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = p2Influences,
                };
            return SessionFactory.NewSession(
                phase: phase,
                p0Hand: new Hand(new[] { CardId.Of(RealmBeyondTypeId, 0) }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-287 / 288: SDP 変動 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-287")]
        public void Given_任意フェーズ_When_Card07をプレイ_Then_自分のSDPがマイナス6()
        {
            var rule = NewRule(NewCatalogWithCardSeven());
            var session = NewSession();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(RealmBeyondTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-6));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-288")]
        public void Given_任意フェーズ_When_Card07をプレイ_Then_相手のSDPがプラス5()
        {
            var rule = NewRule(NewCatalogWithCardSeven());
            var session = NewSession();
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(RealmBeyondTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(5));
        }

        // ===== DZ-289: 相手 InvertBedDamage 影響保有時 → 1 件削除 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-289")]
        public void Given_相手がInvertBedDamageInfluence2件保有_When_Card07をプレイ_Then_該当Influence件数が2マイナス1で1になる()
        {
            // Given(p2 が InvertBedDamage 2 件保有)
            var rule = NewRule(NewCatalogWithCardSeven());
            var session = NewSession(p2Influences: new[] { InvertBedDamageInfluence(), InvertBedDamageInfluence() });
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(RealmBeyondTypeId, 0)));
            // Then(p2 の InvertBedDamage 件数が 2 → 1 に減算)
            int invertCount = 0;
            foreach (var inf in next.Influences[PlayerId.Of("p2")])
            {
                if (inf.TickEffect is InvertBedDamageSdpInfluenceMarkerEffect)
                {
                    invertCount++;
                }
            }
            Assert.That(invertCount, Is.EqualTo(1));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-289")]
        public void Given_相手がInvertBedDamageInfluence2件保有_When_Card07をプレイ_Then_Influences総件数が2になる()
        {
            // Given(p2 が InvertBedDamage 2 件保有)
            // 「削除と新規付与が同時に起きる」DZ-289 固有の境界条件を回帰防御するため、総件数も検証する
            // (Remove 1 件 + Add 1 件 = 純増 0、初期 2 件 → 結果 2 件、code-reviewer P-2 反映 2026-05-17)。
            var rule = NewRule(NewCatalogWithCardSeven());
            var session = NewSession(p2Influences: new[] { InvertBedDamageInfluence(), InvertBedDamageInfluence() });
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(RealmBeyondTypeId, 0)));
            // Then(InvertBedDamage 1 件 + RestrictCard08 1 件 = 総件数 2)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(2));
        }

        // ===== DZ-290: 相手 InvertBedDamage 影響非保有時 → graceful no-op =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-290")]
        public void Given_相手がInvertBedDamageInfluence非保有_When_Card07をプレイ_Then_RestrictCard08のみ1件追加()
        {
            // Given(p2 が Influence 持たない)
            var rule = NewRule(NewCatalogWithCardSeven());
            var session = NewSession();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(RealmBeyondTypeId, 0)));
            // Then(p2 の Influences:0 → 1(RestrictCard08 のみ、InvertBedDamage 削除は no-op))
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
        }

        // ===== DZ-291: RestrictCard08 影響付与 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-291")]
        public void Given_任意フェーズ_When_Card07をプレイ_Then_相手のInfluencesにRestrictCard08が追加される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardSeven());
            var session = NewSession();
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of(RealmBeyondTypeId, 0)));
            // Then(p2 の Influences に RestrictWisdomInfluence が追加)
            Assert.That(next.Influences[PlayerId.Of("p2")], Contains.Item(RestrictWisdomInfluence()));
        }

        // ===== DZ-292: Frenzy で Counter 不可(DZ-282 同パターン)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-292")]
        public void Given_Card07がField_WaitingForCounter_When_p2がCounterActionでCard07をtarget_Then_IsLegalMoveがfalse()
        {
            // Given(WaitingForCounterResponse、p1 の Card "07" を Field に出した直後、p2 が Counter キーワード持ちカードを保有)
            var card07 = new CardData("知恵の及ばぬ領域", new Dictionary<string, int>());
            var counterCard = new CardData("c_counter", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(RealmBeyondTypeId, card07),
                new KeyValuePair<CardTypeId, CardData>(CardTypeId.Of("c_counter"), counterCard),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    RealmBeyondTypeId,
                    (IReadOnlyList<IEffect>)RealmBeyondEffects()),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardTypeId.Of("c_counter"),
                    new IEffect[]
                    {
                        new KeywordedEffect(new[] { Keyword.Counter }, new AdjustSdpEffect(SdpTarget.Self, 0)),
                    }),
            };
            var catalog = new InMemoryCardCatalog(entries, effects);
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());

            // SessionFactory 命名注記:p0Hand = p1 の手札、p1Hand = p2 の手札(currentPlayerIndex=1 で p2 current)
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForCounterResponse,
                currentPlayerIndex: 1,
                p1Hand: new Hand(new[] { CardId.Of(CardTypeId.Of("c_counter"), 0) }),
                field: new Pile(new[] { CardId.Of(RealmBeyondTypeId, 0) }));

            // When
            var legal = rule.IsLegalMove(
                session,
                new CounterAction(CardId.Of(CardTypeId.Of("c_counter"), 0), CardId.Of(RealmBeyondTypeId, 0)));
            // Then(Card "07" は Frenzy 持ち → 反撃を受けない、illegal)
            Assert.That(legal, Is.False);
        }
    }
}
