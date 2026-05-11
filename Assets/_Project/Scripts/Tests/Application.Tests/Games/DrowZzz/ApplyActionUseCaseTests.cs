using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    [TestFixture]
    public class ApplyActionUseCaseTests
    {
        // ===== ヘルパー(DrowZzzRuleTests と独立、本 fixture 完結)=====

        // M2-PR1: DrowZzzRule constructor が ICardCatalog<IEffect> / EffectInterpreter を要求するため、
        // M1 互換挙動を維持する最小依存 (空 catalog + 標準 Interpreter) を内部で組み立てる。
        // 共通テストヘルパー抽出は docs/todo.md TODO で追跡。
        private static DrowZzzRule NewRule() =>
            new DrowZzzRule(
                new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]),
                new EffectInterpreter());

        private static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForDraw,
            int currentPlayerIndex = 0,
            Pile deck = null,
            Hand p0Hand = null,
            Hand p1Hand = null)
        {
            var p0 = new PlayerState(PlayerId.Of("p1"), p0Hand ?? Hand.Empty);
            var p1 = new PlayerState(PlayerId.Of("p2"), p1Hand ?? Hand.Empty);
            var gs = new GameState(
                new[] { p0, p1 },
                deck ?? Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                new TurnState(1, currentPlayerIndex));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 10,
            };
            // SDP は M2-PR3 で追加(ADR-0009 §「DP 種別」)。本ヘルパー利用テストは SDP に関心がないため
            // 全プレイヤー 0 で固定初期化する。
            var sdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            return new DrowZzzGameSession(gs, fdp, sdp, phase);
        }

        private static Pile NewDeck(params string[] cardIds)
        {
            var cards = new CardId[cardIds.Length];
            for (int i = 0; i < cardIds.Length; i++)
            {
                cards[i] = CardId.Of(cardIds[i]);
            }
            return new Pile(cards);
        }

        // ===== APP-023〜025: 各 Action 種別の正常委譲 (rule.Apply と等価) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-023")]
        public void Given_WaitingForDraw_When_DrawCardActionをExecute_Then_RuleApplyの結果と一致する()
        {
            // Given
            var rule = NewRule();
            var useCase = new ApplyActionUseCase(rule);
            var session = NewSession(deck: NewDeck("c1"));
            // When(直接 Apply と useCase.Execute を比較)
            var direct = rule.Apply(session, new DrawCardAction());
            var viaUseCase = useCase.Execute(session, new DrawCardAction());
            // Then
            Assert.That(viaUseCase, Is.EqualTo(direct));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-024")]
        public void Given_WaitingForPlay_When_PlayCardActionをExecute_Then_RuleApplyの結果と一致する()
        {
            // Given
            var rule = NewRule();
            var useCase = new ApplyActionUseCase(rule);
            var p0Hand = new Hand(new[] { CardId.Of("c1") });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            var action = new PlayCardAction(CardId.Of("c1"));
            // When
            var direct = rule.Apply(session, action);
            var viaUseCase = useCase.Execute(session, action);
            // Then
            Assert.That(viaUseCase, Is.EqualTo(direct));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-025")]
        public void Given_WaitingForEndTurn_When_EndTurnActionをExecute_Then_RuleApplyの結果と一致する()
        {
            // Given
            var rule = NewRule();
            var useCase = new ApplyActionUseCase(rule);
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForEndTurn);
            // When
            var direct = rule.Apply(session, new EndTurnAction());
            var viaUseCase = useCase.Execute(session, new EndTurnAction());
            // Then
            Assert.That(viaUseCase, Is.EqualTo(direct));
        }

        // ===== APP-026: IsLegalMove false で InvalidOperationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-026")]
        public void Given_WaitingForPlayでDrawCardAction_When_Execute_Then_InvalidOperationExceptionを投げる()
        {
            // Given(WaitingForPlay は DrawCardAction 非合法)
            var rule = NewRule();
            var useCase = new ApplyActionUseCase(rule);
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, deck: NewDeck("c1"));
            // When / Then
            Assert.Throws<InvalidOperationException>(() => useCase.Execute(session, new DrawCardAction()));
        }

        // ===== APP-027: StartGameAction は常に InvalidOperationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-027")]
        public void Given_StartGameAction_When_Execute_Then_InvalidOperationExceptionを投げる()
        {
            // Given(StartGameAction は IsLegalMove で常に false、StartGameUseCase 経由で扱う)
            var rule = NewRule();
            var useCase = new ApplyActionUseCase(rule);
            var session = NewSession();
            // When / Then
            Assert.Throws<InvalidOperationException>(() => useCase.Execute(session, new StartGameAction()));
        }

        // ===== APP-028 / APP-029: null 検証 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-028")]
        public void Given_sessionにnull_When_Execute_Then_ArgumentNullExceptionを投げる()
        {
            var useCase = new ApplyActionUseCase(NewRule());
            Assert.Throws<ArgumentNullException>(() => useCase.Execute(null, new DrawCardAction()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-029")]
        public void Given_actionにnull_When_Execute_Then_ArgumentNullExceptionを投げる()
        {
            var useCase = new ApplyActionUseCase(NewRule());
            Assert.Throws<ArgumentNullException>(() => useCase.Execute(NewSession(), null));
        }

        // ===== APP-030: constructor null 検証 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-030")]
        public void Given_ruleにnull_When_ApplyActionUseCaseを生成_Then_ArgumentNullExceptionを投げる()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new ApplyActionUseCase(null));
        }
    }
}
