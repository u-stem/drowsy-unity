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
    /// カード No.16「自分勝手な審判」の統合テスト(DZ-359 〜 DZ-368、2026-05-17 で導入)。
    /// 条件分岐 effect `ConditionalApplyOrClearInfluencesEffect` の初導入カード。
    /// 対象プレイヤーの Influences 件数で「<=2 で Apply、>2 で Clear」と分岐。
    /// </summary>
    [TestFixture]
    public sealed class SelfishJudgementCardTests
    {
        private static readonly CardTypeId SelfishJudgementTypeId = CardTypeId.Of("16");

        // 本カードが付与する Influence(両 Choice 共通):自フェーズ開始時 SDP-4 永続
        private static PlayerInfluence OwnPhaseSdpMinus4Influence() =>
            new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, -4),
                InfluenceConstants.Perpetual);

        private static IEffect[] SelfishJudgementEffects() => new IEffect[]
        {
            new ChoiceEffect(new IReadOnlyList<IEffect>[]
            {
                // 選択1(Choice == 0):自分 -8 / 相手 +5、甲(Self)対象の条件分岐
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -8),
                    new AdjustSdpEffect(SdpTarget.Opponent, 5),
                    new ConditionalApplyOrClearInfluencesEffect(SdpTarget.Self, 2, OwnPhaseSdpMinus4Influence()),
                },
                // 選択2(Choice == 1):自分 +5 / 相手 -8、乙(Opponent)対象の条件分岐
                new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, 5),
                    new AdjustSdpEffect(SdpTarget.Opponent, -8),
                    new ConditionalApplyOrClearInfluencesEffect(SdpTarget.Opponent, 2, OwnPhaseSdpMinus4Influence()),
                },
            }),
        };

        private static InMemoryCardCatalog NewCatalog()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(SelfishJudgementTypeId, new CardData("自分勝手な審判", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(SelfishJudgementTypeId, (IReadOnlyList<IEffect>)SelfishJudgementEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // ダミー Influence N 件を持つ session を構築(p1 / p2 それぞれ独立指定)
        private static DrowZzzGameSession NewSessionWithInfluenceCounts(int p1InfluenceCount, int p2InfluenceCount)
        {
            var dummyInfluence = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, -1), 5);
            var p1Infs = new List<PlayerInfluence>();
            for (int i = 0; i < p1InfluenceCount; i++) p1Infs.Add(dummyInfluence);
            var p2Infs = new List<PlayerInfluence>();
            for (int i = 0; i < p2InfluenceCount; i++) p2Infs.Add(dummyInfluence);
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = p1Infs,
                [PlayerId.Of("p2")] = p2Infs,
            };
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(SelfishJudgementTypeId, 0) }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-360: 選択1 SDP =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-360")]
        public void Given_任意_When_Choice0プレイ_Then_自分SDPマイナス8()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-8));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-360")]
        public void Given_任意_When_Choice0プレイ_Then_相手SDPプラス5()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 0));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(5));
        }

        // ===== DZ-361: 選択1 + 甲影響 0 件 → Apply 経路(境界 0)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-361")]
        public void Given_甲影響0件_When_Choice0プレイ_Then_甲にSDPMinus4Influenceが追加()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 0));
            // Apply 経路:p1 Influences 0 → 1(本カード影響のみ)
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(1));
            Assert.That(next.Influences[PlayerId.Of("p1")][0], Is.EqualTo(OwnPhaseSdpMinus4Influence()));
        }

        // ===== DZ-362: 選択1 + 甲影響 2 件 → Apply 経路(境界 2)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-362")]
        public void Given_甲影響2件_When_Choice0プレイ_Then_甲の影響が3件に増える()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(2, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 0));
            // Apply 経路(境界 Count<=2):既存 2 件 + 本カード影響 1 件 = 3 件
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(3));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-362")]
        public void Given_甲影響2件_When_Choice0プレイ_Then_末尾は本カードのSDPMinus4Influenceと一致()
        {
            // code-reviewer P-2 反映 2026-05-17:DZ-361 と同じく、追加された Influence の値同一性まで検証
            // (1 テスト 1 アサーション原則で別テストメソッドとして追加、Count 検証だけだと dummy が追加されていても誤 pass する)
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(2, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 0));
            // 末尾(index 2)に本カードの Influence が追加されているはず(Apply 経路の末尾追加パターン)
            Assert.That(next.Influences[PlayerId.Of("p1")][2], Is.EqualTo(OwnPhaseSdpMinus4Influence()));
        }

        // ===== DZ-363: 選択1 + 甲影響 3 件 → Clear 経路(境界 3、Apply されない)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-363")]
        public void Given_甲影響3件_When_Choice0プレイ_Then_甲の影響が空になる()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(3, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 0));
            // Clear 経路(境界 Count>2):p1 Influences は空、本カード影響も Apply されない(排他)
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(0));
        }

        // ===== DZ-364: 選択2 SDP =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-364")]
        public void Given_任意_When_Choice1プレイ_Then_自分SDPプラス5()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(5));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-364")]
        public void Given_任意_When_Choice1プレイ_Then_相手SDPマイナス8()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 1));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-8));
        }

        // ===== DZ-365: 選択2 + 乙影響 0 件 → 乙に Apply =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-365")]
        public void Given_乙影響0件_When_Choice1プレイ_Then_乙にSDPMinus4Influenceが追加()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 1));
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
            Assert.That(next.Influences[PlayerId.Of("p2")][0], Is.EqualTo(OwnPhaseSdpMinus4Influence()));
        }

        // ===== DZ-365 第 2(乙 Apply 経路の境界 2、code-reviewer P-3 反映 2026-05-17):Choice0 の DZ-362 と対称 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-365")]
        public void Given_乙影響2件_When_Choice1プレイ_Then_乙の影響が3件に増える()
        {
            // Choice0 の DZ-362 と対称、乙(Opponent)対象の境界 Count==2 = Apply 経路で本カード影響が末尾追加される
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 2), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 1));
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(3));
        }

        // ===== DZ-366: 選択2 + 乙影響 3 件 → 乙の影響を Clear =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-366")]
        public void Given_乙影響3件_When_Choice1プレイ_Then_乙の影響が空になる()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 3), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 1));
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(0));
        }

        // ===== DZ-367: 他プレイヤー保護 — 選択1 では乙の影響は触らない =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-367")]
        public void Given_乙影響5件_When_Choice0プレイ_Then_乙の影響は不変()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(0, 5), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 0));
            // 選択1 は Target=Self なので p2 Influences は不変(5 件のまま)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(5));
        }

        // ===== DZ-368: 他プレイヤー保護 — 選択2 では甲の影響は触らない =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-368")]
        public void Given_甲影響5件_When_Choice1プレイ_Then_甲の影響は不変()
        {
            var rule = NewRule(NewCatalog());
            var next = rule.Apply(NewSessionWithInfluenceCounts(5, 0), new PlayCardAction(CardId.Of(SelfishJudgementTypeId, 0), Choice: 1));
            // 選択2 は Target=Opponent なので p1 Influences は不変(5 件のまま)
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(5));
        }
    }
}
