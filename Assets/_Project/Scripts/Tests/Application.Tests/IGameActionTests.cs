using NUnit.Framework;
using Drowsy.Application;

namespace Drowsy.Application.Tests
{
    [TestFixture]
    public sealed class IGameActionTests
    {
        // ダミー record。「record が IGameAction を実装できる」契約 (APP-002) の検証用。
        private sealed record DummyAction : IGameAction;

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-002")]
        public void Given_record型がIGameActionを実装_When_IGameActionとして代入_Then_代入できる()
        {
            // Given
            var action = new DummyAction();
            // When
            IGameAction marker = action;
            // Then
            Assert.That(marker, Is.SameAs(action));
        }
    }
}
