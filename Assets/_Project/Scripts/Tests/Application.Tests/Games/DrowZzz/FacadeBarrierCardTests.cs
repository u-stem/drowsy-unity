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
    /// カード No.17「見掛け倒しの障壁」の統合テスト(DZ-370 〜 DZ-376、2026-05-18 で導入)。
    /// 既存 Counter キーワード機構を使う初の本物の反撃カード。SDP 変動なし、Counter キーワード付与のみ。
    /// 通常 PlayCardAction(SDP 0)+ CounterAction 経路 1 / Frenzy 反撃不可 / Apply 経路の Hand/Field/Discard 操作 + Pending 記録 + フェーズ遷移を検証。
    /// </summary>
    [TestFixture]
    public sealed class FacadeBarrierCardTests
    {
        private static readonly CardTypeId FacadeBarrierTypeId = CardTypeId.Of("17");
        private static readonly CardTypeId DummyTargetTypeId = CardTypeId.Of("X");

        private static IEffect[] FacadeBarrierEffects() => new IEffect[]
        {
            new KeywordedEffect(new[] { Keyword.Counter },
                new AdjustSdpEffect(SdpTarget.Self, 0)),
        };

        // Counter 経路テスト用のダミー Target カード(Frenzy なし)
        private static IEffect[] DummyTargetEffects() => System.Array.Empty<IEffect>();

        // Frenzy 持ちダミー Target カード(DZ-373 用)
        private static IEffect[] FrenzyTargetEffects() => new IEffect[]
        {
            new KeywordedEffect(new[] { Keyword.Frenzy },
                new AdjustSdpEffect(SdpTarget.Self, 0)),
        };

        private static InMemoryCardCatalog NewCatalogWithCard17Only()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(FacadeBarrierTypeId, new CardData("見掛け倒しの障壁", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(FacadeBarrierTypeId, (IReadOnlyList<IEffect>)FacadeBarrierEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // No.17 + 非 Frenzy Target / Frenzy Target を含む catalog(Counter 経路テスト用)
        private static InMemoryCardCatalog NewCatalogWithCard17AndTarget(bool targetIsFrenzy = false)
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(FacadeBarrierTypeId, new CardData("見掛け倒しの障壁", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(DummyTargetTypeId, new CardData("X", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(FacadeBarrierTypeId, (IReadOnlyList<IEffect>)FacadeBarrierEffects()),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(DummyTargetTypeId, targetIsFrenzy
                    ? (IReadOnlyList<IEffect>)FrenzyTargetEffects()
                    : (IReadOnlyList<IEffect>)DummyTargetEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // 通常 PlayCard 経路の共通 session 構築(code-reviewer P-2 反映 2026-05-18):
        // p1 current、WaitingForPlay、p1 手札に Card "17" Instance 0 のみ
        private static DrowZzzGameSession NewPlayCardSession() =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0,
                p0Hand: new Hand(new[] { CardId.Of(FacadeBarrierTypeId, 0) }),
                turnNumber: 1,
                fdp: SessionFactory.Dp(p1: 0, p2: 0));

        // Counter 経路(WaitingForCounterResponse)の共通 session 構築:
        // p1 currentPlayerIndex=0、PhaseState=WaitingForCounterResponse、p2(counterPlayerIndex=1)が Card "17" Instance 0 保有、
        // Field 先頭に Card "X" Instance 0(Target)
        // SessionFactory: p1Hand = PlayerId.Of("p2") の手札(0-indexed プレイヤースロット)
        private static DrowZzzGameSession NewCounterSessionWithCard17InP2Hand() =>
            SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForCounterResponse,
                currentPlayerIndex: 0,
                p1Hand: new Hand(new[] { CardId.Of(FacadeBarrierTypeId, 0) }),
                field: new Pile(new[] { CardId.Of(DummyTargetTypeId, 0) }));

        // ===== DZ-370 / 371: 通常 PlayCardAction =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-370")]
        public void Given_任意フェーズ_When_Card17をPlayCardAction_Then_自分のSDP変動なし()
        {
            // 1 テスト 1 アサーション原則(code-reviewer W-1 反映 2026-05-18)で p1 / p2 を別テストに分割
            var rule = NewRule(NewCatalogWithCard17Only());
            var next = rule.Apply(NewPlayCardSession(), new PlayCardAction(CardId.Of(FacadeBarrierTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-370")]
        public void Given_任意フェーズ_When_Card17をPlayCardAction_Then_相手のSDP変動なし()
        {
            var rule = NewRule(NewCatalogWithCard17Only());
            var next = rule.Apply(NewPlayCardSession(), new PlayCardAction(CardId.Of(FacadeBarrierTypeId, 0)));
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-371")]
        public void Given_任意フェーズ_When_Card17をPlayCardAction_Then_HandからRemove_FieldにAdd()
        {
            var rule = NewRule(NewCatalogWithCard17Only());
            var next = rule.Apply(NewPlayCardSession(), new PlayCardAction(CardId.Of(FacadeBarrierTypeId, 0)));
            // Hand から Remove、Field 先頭に AddTop(PlayCardAction 直後の中間状態、ADR-0006 §M1-PR5)
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of(FacadeBarrierTypeId, 0)), Is.False);
            Assert.That(next.GameState.Field.Cards[0], Is.EqualTo(CardId.Of(FacadeBarrierTypeId, 0)));
        }

        // ===== DZ-372: 非 Frenzy Target に反撃で IsLegalMove が true =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-372")]
        public void Given_WaitingForCounter_NonFrenzyTarget_When_Card17でCounterAction_Then_IsLegalMoveがtrue()
        {
            var rule = NewRule(NewCatalogWithCard17AndTarget(targetIsFrenzy: false));
            var legal = rule.IsLegalMove(NewCounterSessionWithCard17InP2Hand(),
                new CounterAction(CardId.Of(FacadeBarrierTypeId, 0), CardId.Of(DummyTargetTypeId, 0)));
            Assert.That(legal, Is.True);
        }

        // ===== DZ-373: Frenzy Target には反撃不可 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-373")]
        public void Given_WaitingForCounter_FrenzyTarget_When_Card17でCounterAction_Then_IsLegalMoveがfalse()
        {
            var rule = NewRule(NewCatalogWithCard17AndTarget(targetIsFrenzy: true));
            var legal = rule.IsLegalMove(NewCounterSessionWithCard17InP2Hand(),
                new CounterAction(CardId.Of(FacadeBarrierTypeId, 0), CardId.Of(DummyTargetTypeId, 0)));
            // Frenzy 持ち Target には反撃不可、ADR-0011 §4.5
            Assert.That(legal, Is.False);
        }

        // ===== DZ-374: Apply — Card "17" と Target が Discard へ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-374")]
        public void Given_WaitingForCounter_When_Card17でCounterAction_Then_Card17とTargetがDiscardへ()
        {
            var rule = NewRule(NewCatalogWithCard17AndTarget(targetIsFrenzy: false));
            var next = rule.Apply(NewCounterSessionWithCard17InP2Hand(),
                new CounterAction(CardId.Of(FacadeBarrierTypeId, 0), CardId.Of(DummyTargetTypeId, 0)));
            // p2 Hand から Card "17" が Remove(Hand は Contains 持ち)
            Assert.That(next.GameState.Players[1].Hand.Contains(CardId.Of(FacadeBarrierTypeId, 0)), Is.False);
            // Field から Target が Remove、Discard に両方含まれる(Pile.Cards は IReadOnlyList で LINQ Contains)
            Assert.That(next.GameState.Field.Cards.Contains(CardId.Of(DummyTargetTypeId, 0)), Is.False);
            Assert.That(next.GameState.Discard.Cards.Contains(CardId.Of(FacadeBarrierTypeId, 0)), Is.True);
            Assert.That(next.GameState.Discard.Cards.Contains(CardId.Of(DummyTargetTypeId, 0)), Is.True);
        }

        // ===== DZ-375: Apply — PendingCounteredEffects に 1 件追加 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-375")]
        public void Given_WaitingForCounter_When_Card17でCounterAction_Then_PendingCounteredEffectsに1件追加()
        {
            var rule = NewRule(NewCatalogWithCard17AndTarget(targetIsFrenzy: false));
            var next = rule.Apply(NewCounterSessionWithCard17InP2Hand(),
                new CounterAction(CardId.Of(FacadeBarrierTypeId, 0), CardId.Of(DummyTargetTypeId, 0)));
            // PendingCounteredEffects に (CounterCard=Card "17", OriginalCard=Target, OriginalEffects=Target の効果列) が 1 件
            Assert.That(next.PendingCounteredEffects.Count, Is.EqualTo(1));
            Assert.That(next.PendingCounteredEffects[0].CounterCard, Is.EqualTo(CardId.Of(FacadeBarrierTypeId, 0)));
            Assert.That(next.PendingCounteredEffects[0].OriginalCard, Is.EqualTo(CardId.Of(DummyTargetTypeId, 0)));
        }

        // ===== DZ-376: Apply — PhaseState が WaitingForEndTurn に戻る =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-376")]
        public void Given_WaitingForCounter_When_Card17でCounterAction_Then_PhaseStateがWaitingForEndTurn()
        {
            var rule = NewRule(NewCatalogWithCard17AndTarget(targetIsFrenzy: false));
            var next = rule.Apply(NewCounterSessionWithCard17InP2Hand(),
                new CounterAction(CardId.Of(FacadeBarrierTypeId, 0), CardId.Of(DummyTargetTypeId, 0)));
            // 元プレイヤー(p1 currentPlayerIndex=0)のターン進行に戻る、WaitingForEndTurn
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForEndTurn));
        }
    }
}
