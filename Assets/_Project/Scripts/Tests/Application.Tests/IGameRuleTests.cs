using System;
using NUnit.Framework;
using Drowsy.Application;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests
{
    [TestFixture]
    public class IGameRuleTests
    {
        // ダミー Action / Session / Rule。
        // IGameRule<TAction, TSession> の最小契約(APP-005)+ M3 拡張(APP-041 / APP-042)を検証するための骨格実装。
        private sealed record DummyAction : IGameAction;

        private sealed record DummySession(int Counter, bool Terminated = false, PlayerId Winner = null);

        // M3-PR1 で IGameRule に IsTerminated / GetWinner を追加(ADR-0010 §1)。
        // DummyRule は session.Terminated を IsTerminated として、session.Winner を GetWinner として返す薄い実装。
        private sealed class DummyRule : IGameRule<DummyAction, DummySession>
        {
            public bool IsLegalMove(DummySession session, DummyAction action) => true;

            public DummySession Apply(DummySession session, DummyAction action) =>
                new DummySession(session.Counter + 1);

            public bool IsTerminated(DummySession session) => session.Terminated;

            public PlayerId GetWinner(DummySession session)
            {
                if (!session.Terminated)
                {
                    throw new InvalidOperationException("未終了 session で GetWinner は呼べない(ADR-0010 §1 契約)");
                }
                return session.Winner;
            }
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-005")]
        public void Given_合法判定がtrueなRuleとAction_When_Applyを呼ぶ_Then_新しいSessionが返る()
        {
            // Given(DummyRule は IsLegalMove が常に true を返す設計)
            var rule = new DummyRule();
            var session = new DummySession(0);
            var action = new DummyAction();
            // When
            var next = rule.Apply(session, action);
            // Then
            Assert.That(next, Is.Not.SameAs(session));
        }

        // ===== APP-041: IGameRule.IsTerminated が session の終了状態を返す =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-041")]
        public void Given_未終了Session_When_IsTerminated_Then_false()
        {
            var rule = new DummyRule();
            var session = new DummySession(0, Terminated: false);
            Assert.That(rule.IsTerminated(session), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-041")]
        public void Given_終了済Session_When_IsTerminated_Then_true()
        {
            var rule = new DummyRule();
            var session = new DummySession(0, Terminated: true);
            Assert.That(rule.IsTerminated(session), Is.True);
        }

        // ===== APP-042: IGameRule.GetWinner の契約 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-042")]
        public void Given_終了済Session勝者あり_When_GetWinner_Then_勝者PlayerIdを返す()
        {
            var rule = new DummyRule();
            var session = new DummySession(0, Terminated: true, Winner: PlayerId.Of("p1"));
            Assert.That(rule.GetWinner(session), Is.EqualTo(PlayerId.Of("p1")));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-042")]
        public void Given_終了済Session引き分け_When_GetWinner_Then_nullを返す()
        {
            var rule = new DummyRule();
            var session = new DummySession(0, Terminated: true, Winner: null);
            Assert.That(rule.GetWinner(session), Is.Null);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-042")]
        public void Given_未終了Session_When_GetWinner_Then_InvalidOperationExceptionを投げる()
        {
            var rule = new DummyRule();
            var session = new DummySession(0, Terminated: false);
            Assert.Throws<InvalidOperationException>(() => rule.GetWinner(session));
        }
    }
}
