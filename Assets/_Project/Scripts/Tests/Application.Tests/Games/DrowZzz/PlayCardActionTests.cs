using System;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="PlayCardAction"/> の null 防御挙動 (DZ-053) を検証する fixture。
    /// 構造的性質 (DZ-052) は init setter 宣言で保証されるためテスト免除。
    /// </summary>
    [TestFixture]
    public sealed class PlayCardActionTests
    {
        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-053")]
        public void Given_PlayCardActionにnullCard_When_生成_Then_ArgumentNullExceptionを投げる()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new PlayCardAction(null));
            Assert.That(ex!.ParamName, Is.EqualTo("Card"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-053")]
        public void Given_既存PlayCardAction_When_with_Cardにnull_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var action = new PlayCardAction(CardId.Of(CardTypeId.Of("c1"), 0));
            // When / Then
            var ex = Assert.Throws<ArgumentNullException>(() => _ = action with { Card = null });
            Assert.That(ex!.ParamName, Is.EqualTo("Card"));
        }
    }
}
