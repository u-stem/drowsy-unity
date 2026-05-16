using System;
using NUnit.Framework;
using Drowsy.Domain.Random;

namespace Drowsy.Domain.Tests.Random
{
    [TestFixture]
    public sealed class XorShiftRandomTests
    {
        [Test, Category("Small"), Category("Normal"), Property("Requirement", "RND-001")]
        public void Given_seed42_When_NextIntを範囲内で100回呼ぶ_Then_全て範囲内の整数を返す()
        {
            var rng = new XorShiftRandom(42);
            for (int i = 0; i < 100; i++)
            {
                var value = rng.NextInt(0, 10);
                Assert.That(value, Is.GreaterThanOrEqualTo(0));
                Assert.That(value, Is.LessThan(10));
            }
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "RND-002"), Property("Requirement", "RND-003")]
        public void Given_同じseedの2つのインスタンス_When_NextIntを繰り返し呼ぶ_Then_同じ系列を生成()
        {
            var rng1 = new XorShiftRandom(42);
            var rng2 = new XorShiftRandom(42);
            for (int i = 0; i < 20; i++)
            {
                Assert.That(rng1.NextInt(0, 1000), Is.EqualTo(rng2.NextInt(0, 1000)));
            }
        }

        [Test, Category("Small"), Category("SemiNormal"), Property("Requirement", "RND-005")]
        public void Given_seed0とseed1_When_NextIntを呼ぶ_Then_両者の系列は完全一致()
        {
            // Given (seed 0 は内部で 1 に補正される)
            var rng0 = new XorShiftRandom(0);
            var rng1 = new XorShiftRandom(1);
            // When / Then
            for (int i = 0; i < 10; i++)
            {
                Assert.That(rng0.NextInt(0, 1000), Is.EqualTo(rng1.NextInt(0, 1000)));
            }
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "RND-004")]
        public void Given_maxExclusiveがminより小さい_When_NextInt_Then_ArgumentException()
        {
            var rng = new XorShiftRandom(1);
            Assert.Throws<ArgumentException>(() => rng.NextInt(10, 5));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "RND-004")]
        public void Given_maxExclusiveがminと同じ_When_NextInt_Then_ArgumentException()
        {
            var rng = new XorShiftRandom(1);
            Assert.Throws<ArgumentException>(() => rng.NextInt(5, 5));
        }
    }
}
