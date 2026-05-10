using NUnit.Framework;
using Drowsy.Application;

namespace Drowsy.Application.Tests
{
    [TestFixture]
    public class IGameRuleTests
    {
        // ダミー Action / Session / Rule。
        // IGameRule<TAction, TSession> の最小契約 (APP-005) を検証するための骨格実装。
        private sealed record DummyAction : IGameAction;

        private sealed record DummySession(int Counter);

        private sealed class DummyRule : IGameRule<DummyAction, DummySession>
        {
            public bool IsLegalMove(DummySession session, DummyAction action) => true;

            public DummySession Apply(DummySession session, DummyAction action) =>
                new DummySession(session.Counter + 1);
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
    }
}
