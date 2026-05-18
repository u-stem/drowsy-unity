using System;
using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using NUnit.Framework;
using static Drowsy.Application.Tests.Stubs.SessionFactory;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    [TestFixture]
    public sealed class ApplyActionUseCaseTests
    {
        // ヘルパー(`NewSession` / `NewRule` / `NewDeck`)は M1-PR6 reviewer 指摘 P-2 起点の
        // `docs/todo.md`「テストヘルパー抽出」TODO で 2026-05-13 に `Drowsy.Application.Tests.Stubs.SessionFactory`
        // に統合(本 fixture と `DrowZzzRuleTests` の重複解消、`ApplyActionUseCaseTests.NewSession`
        // は引数 5 個・固定値多めだったが SessionFactory はスーパーセット引数で同じ挙動を維持)。

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
            var p0Hand = new Hand(new[] { CardId.Of(CardTypeId.Of("c1"), 0) });
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, p0Hand: p0Hand);
            var action = new PlayCardAction(CardId.Of(CardTypeId.Of("c1"), 0));
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
            var ex = Assert.Throws<ArgumentNullException>(() => useCase.Execute(null, new DrawCardAction()));
            Assert.That(ex!.ParamName, Is.EqualTo("session"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-029")]
        public void Given_actionにnull_When_Execute_Then_ArgumentNullExceptionを投げる()
        {
            var useCase = new ApplyActionUseCase(NewRule());
            var ex = Assert.Throws<ArgumentNullException>(() => useCase.Execute(NewSession(), null));
            Assert.That(ex!.ParamName, Is.EqualTo("action"));
        }

        // ===== APP-030: constructor null 検証 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-030")]
        public void Given_ruleにnull_When_ApplyActionUseCaseを生成_Then_ArgumentNullExceptionを投げる()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new ApplyActionUseCase(null));
            Assert.That(ex!.ParamName, Is.EqualTo("rule"));
        }
    }
}
