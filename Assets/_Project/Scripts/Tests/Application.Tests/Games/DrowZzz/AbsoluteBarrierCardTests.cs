using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// カード No.19「絶対障壁」の統合テスト(DZ-394 〜 DZ-396、ADR-0024、2026-05-18 で導入)。
    /// Counter + Frenzy 両キーワード持ち(反撃可能 + 被反撃不可)+ ゲーム開始時自動連想 marker。
    /// 開始時自動連想テスト(DZ-390〜393)は <see cref="StartGameUseCaseTests"/> に配置(StartGameUseCase 経路)。
    /// </summary>
    [TestFixture]
    public sealed class AbsoluteBarrierCardTests
    {
        private static readonly CardTypeId AbsoluteBarrierTypeId = CardTypeId.Of("19");
        private static readonly CardTypeId DummyTargetTypeId = CardTypeId.Of("X");
        private static readonly CardTypeId DummyCounterTypeId = CardTypeId.Of("CTR");

        private static IEffect[] AbsoluteBarrierEffects() => new IEffect[]
        {
            new KeywordedEffect(new[] { Keyword.Counter, Keyword.Frenzy }, new AdjustSdpEffect(SdpTarget.Self, 0)),
            new AssociateToFirstPlayerOnGameStartEffect(),
        };

        // 非 Frenzy Target (Counter 用ターゲット)
        private static IEffect[] DummyTargetEffects() => System.Array.Empty<IEffect>();

        // ダミー Counter 持ちカード(DZ-396 で「Card "19" を反撃しようとする他 Counter カード」用)
        private static IEffect[] DummyCounterEffects() => new IEffect[]
        {
            new KeywordedEffect(new[] { Keyword.Counter }, new AdjustSdpEffect(SdpTarget.Self, 0)),
        };

        // No.19 + Target(非 Frenzy)の catalog(DZ-394 / DZ-395 用)
        private static InMemoryCardCatalog NewCatalogWithCard19AndTarget()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(AbsoluteBarrierTypeId, new CardData("絶対障壁", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(DummyTargetTypeId, new CardData("X", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(AbsoluteBarrierTypeId, (IReadOnlyList<IEffect>)AbsoluteBarrierEffects()),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(DummyTargetTypeId, (IReadOnlyList<IEffect>)DummyTargetEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // No.19 + ダミー Counter 持ちカードの catalog(DZ-396 用)
        private static InMemoryCardCatalog NewCatalogWithCard19AndDummyCounter()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(AbsoluteBarrierTypeId, new CardData("絶対障壁", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(DummyCounterTypeId, new CardData("CTR", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(AbsoluteBarrierTypeId, (IReadOnlyList<IEffect>)AbsoluteBarrierEffects()),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(DummyCounterTypeId, (IReadOnlyList<IEffect>)DummyCounterEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // Counter 経路(WaitingForCounterResponse)の session 構築:
        // p1 currentPlayerIndex=0、PhaseState=WaitingForCounterResponse、p2 が Card "19" Instance 0 保有、Field に Card "X"
        private static DrowZzzGameSession NewCounterSessionWithCard19InP2Hand() =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForCounterResponse,
                currentPlayerIndex: 0,
                p1Hand: new Hand(new[] { CardId.Of(AbsoluteBarrierTypeId, 0) }),
                field: new Pile(new[] { CardId.Of(DummyTargetTypeId, 0) }));

        // Frenzy 反撃不可検証用 session:Field に Card "19"(p1 がプレイした直後想定)、p2 が DummyCounter "CTR" を保有
        private static DrowZzzGameSession NewCounterSessionWithCard19InField() =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForCounterResponse,
                currentPlayerIndex: 0,
                p1Hand: new Hand(new[] { CardId.Of(DummyCounterTypeId, 0) }),
                field: new Pile(new[] { CardId.Of(AbsoluteBarrierTypeId, 0) }));

        // ===== DZ-394: Counter 経路で非 Frenzy Target に反撃で IsLegalMove が true =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-394")]
        public void Given_WaitingForCounter_NonFrenzyTarget_When_Card19でCounterAction_Then_IsLegalMoveがtrue()
        {
            var rule = NewRule(NewCatalogWithCard19AndTarget());
            var legal = rule.IsLegalMove(NewCounterSessionWithCard19InP2Hand(),
                new CounterAction(CardId.Of(AbsoluteBarrierTypeId, 0), CardId.Of(DummyTargetTypeId, 0)));
            Assert.That(legal, Is.True);
        }

        // ===== DZ-395: Apply — Card "19" と Target が Discard へ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-395")]
        public void Given_WaitingForCounter_When_Card19でCounterAction_Then_Card19とTargetがDiscardへ()
        {
            var rule = NewRule(NewCatalogWithCard19AndTarget());
            var next = rule.Apply(NewCounterSessionWithCard19InP2Hand(),
                new CounterAction(CardId.Of(AbsoluteBarrierTypeId, 0), CardId.Of(DummyTargetTypeId, 0)));
            // p2 Hand から Card "19" が Remove + Field から Target が Remove + Discard に両方含まれる
            Assert.That(next.GameState.Players[1].Hand.Contains(CardId.Of(AbsoluteBarrierTypeId, 0)), Is.False);
            Assert.That(next.GameState.Field.Cards.Contains(CardId.Of(DummyTargetTypeId, 0)), Is.False);
            Assert.That(next.GameState.Discard.Cards.Contains(CardId.Of(AbsoluteBarrierTypeId, 0)), Is.True);
            Assert.That(next.GameState.Discard.Cards.Contains(CardId.Of(DummyTargetTypeId, 0)), Is.True);
        }

        // ===== DZ-396: Frenzy 経路 — Card "19" には反撃不可 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-396")]
        public void Given_Card19がField_When_OtherCounterCardで反撃_Then_IsLegalMoveがfalse()
        {
            // Field に Card "19"(Frenzy 持ち)、p2 が他の Counter カード "CTR" を手札に持ち反撃しようとする
            var rule = NewRule(NewCatalogWithCard19AndDummyCounter());
            var legal = rule.IsLegalMove(NewCounterSessionWithCard19InField(),
                new CounterAction(CardId.Of(DummyCounterTypeId, 0), CardId.Of(AbsoluteBarrierTypeId, 0)));
            // Frenzy 持ち(Card "19")は反撃を受けない、ADR-0011 §4.5
            Assert.That(legal, Is.False);
        }
    }
}
