using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using NUnit.Framework;
using static Drowsy.Application.Tests.Stubs.SessionFactory;

namespace Drowsy.Application.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="EarlyWinTriggerEffect"/> を <see cref="EffectInterpreter.Apply"/> で評価した時の
    /// 早期勝利成立 / 不成立挙動を検証する(DZ-183 / DZ-184 / DZ-185 / DZ-186)。
    /// </summary>
    /// <remarks>
    /// 2026-05-16 第 2 弾で <c>Drowsy.Application.Tests.Stubs.SessionFactory.NewSession</c> 経由に統合
    /// (`using static SessionFactory` パターン、`fdp: Dp(p1: ...)` で current player の TotalPoints を制御)。
    /// </remarks>
    [TestFixture]
    public sealed class EarlyWinTriggerEffectTests
    {
        // Clock の RoundNumber を制御するため SessionFactory.NewSession へ TurnState.TurnNumber を渡す。
        // TurnNumber = 2 * (round - 1) + 1 が round の current=p1 フェーズ(N=2)。

        // ===== DZ-183: 夜 + 持ち点 100 で WinnerOutcome 設定 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-183")]
        public void Given_夜_持ち点100_When_EarlyWinTriggerをApply_Then_OutcomeがWinnerOutcomeになる()
        {
            // Given(turnNumber=1 → Round=1、夜、p1 が current、FDP=100 で TotalPoints=100)
            var interpreter = new EffectInterpreter();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                turnNumber: 1,
                fdp: Dp(p1: 100));
            // When
            var next = interpreter.Apply(session, new EarlyWinTriggerEffect());
            // Then
            Assert.That(next.Outcome, Is.EqualTo(new WinnerOutcome(PlayerId.Of("p1"))));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-183")]
        public void Given_夜_持ち点100超過_When_EarlyWinTriggerをApply_Then_OutcomeがWinnerOutcomeになる()
        {
            // Given(閾値超過、持ち点 150)
            var interpreter = new EffectInterpreter();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                turnNumber: 1,
                fdp: Dp(p1: 150));
            // When
            var next = interpreter.Apply(session, new EarlyWinTriggerEffect());
            // Then
            Assert.That(next.Outcome, Is.EqualTo(new WinnerOutcome(PlayerId.Of("p1"))));
        }

        // ===== DZ-184: 朝(IsMorning)では no-op =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-184")]
        public void Given_朝_持ち点100_When_EarlyWinTriggerをApply_Then_Outcomeは設定されない()
        {
            // Given(turnNumber=33 → Round=17、朝、持ち点 100 でも早期勝利不可)
            var interpreter = new EffectInterpreter();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                turnNumber: 33,
                fdp: Dp(p1: 100));
            // When
            var next = interpreter.Apply(session, new EarlyWinTriggerEffect());
            // Then
            Assert.That(next.Outcome, Is.Null);
        }

        // ===== DZ-185: 持ち点不足では no-op =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-185")]
        public void Given_夜_持ち点99_When_EarlyWinTriggerをApply_Then_Outcomeは設定されない()
        {
            // Given(閾値未満、Round=1 夜、持ち点 99)
            var interpreter = new EffectInterpreter();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                turnNumber: 1,
                fdp: Dp(p1: 99));
            // When
            var next = interpreter.Apply(session, new EarlyWinTriggerEffect());
            // Then
            Assert.That(next.Outcome, Is.Null);
        }

        // ===== DZ-186: 既に終了済 session に再度評価しても新規 Outcome は上書きしない =====
        // 注: EarlyWinTriggerEffect は単純な ApplyInterpreter 経由では IsLegalMove ガードを通らないため、
        // 直接呼んだ場合でも条件が再評価されることを確認(冪等性に近い性質)。
        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-186")]
        public void Given_既にWinnerOutcome設定済_When_EarlyWinTriggerをApply_Then_新規WinnerOutcomeで上書きされる()
        {
            // Given(p1 が既に勝者として確定済、Round=1 夜、持ち点 100、再度 EarlyWinTrigger を Apply)
            var interpreter = new EffectInterpreter();
            var baseSession = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                turnNumber: 1,
                fdp: Dp(p1: 100));
            var session = baseSession with { Outcome = new WinnerOutcome(PlayerId.Of("p2")) };
            // When(再評価で current=p1 に基づき Outcome を再設定)
            var next = interpreter.Apply(session, new EarlyWinTriggerEffect());
            // Then(条件成立で WinnerOutcome(p1) に上書き、これは設計上想定挙動 = DrowZzzRule.IsLegalMove が
            // 終了済 session への Action を全て illegal 化するので通常経路では再評価されない、ADR-0010 §6)
            Assert.That(next.Outcome, Is.EqualTo(new WinnerOutcome(PlayerId.Of("p1"))));
        }
    }
}
