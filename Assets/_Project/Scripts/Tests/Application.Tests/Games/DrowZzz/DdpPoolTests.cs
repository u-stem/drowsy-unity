using System;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Random;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="DdpPool"/> 値オブジェクトの単体テスト(DZ-148 / DZ-149 / DZ-150 / DZ-151 / DZ-152)。
    /// 構造的性質(DZ-146 / DZ-147 / DZ-153 = sealed class / Values プロパティ / 順序付きシーケンス同値)は
    /// <c>[Ubiquitous]</c> マーカーでテスト免除し、本 fixture は防御要件と API 挙動を検証する。
    /// </summary>
    [TestFixture]
    public sealed class DdpPoolTests
    {
        // ===== DZ-148: ctor null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-148")]
        public void Given_nullを渡す_When_DdpPoolを生成_Then_ArgumentNullExceptionを投げる()
        {
            // Given / When / Then
            Assert.Throws<ArgumentNullException>(() => _ = new DdpPool(null));
        }

        // ===== DZ-149: 空 DdpPool から Draw → InvalidOperationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-149")]
        public void Given_空のDdpPool_When_Drawを呼ぶ_Then_InvalidOperationExceptionを投げる()
        {
            // Given
            var pool = DdpPool.Empty;
            // When / Then
            Assert.Throws<InvalidOperationException>(() => pool.Draw());
        }

        // ===== DZ-150: Draw は先頭要素を返し、残列を Remaining とする =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-150")]
        public void Given_3要素のDdpPool_When_Drawを呼ぶ_Then_先頭要素10を返す()
        {
            // Given
            var pool = new DdpPool(new[] { 10, 20, 30 });
            // When
            var (drawn, _) = pool.Draw();
            // Then
            Assert.That(drawn, Is.EqualTo(10));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-150")]
        public void Given_3要素のDdpPool_When_Drawを呼ぶ_Then_残2要素のDdpPoolを返す()
        {
            // Given
            var pool = new DdpPool(new[] { 10, 20, 30 });
            // When
            var (_, remaining) = pool.Draw();
            // Then
            Assert.That(remaining, Is.EqualTo(new DdpPool(new[] { 20, 30 })));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-150")]
        public void Given_元のDdpPool_When_Drawを呼ぶ_Then_元のDdpPoolは不変()
        {
            // Given(immutability の表明、新インスタンスを返す純関数)
            var pool = new DdpPool(new[] { 10, 20, 30 });
            // When
            _ = pool.Draw();
            // Then
            Assert.That(pool.Count, Is.EqualTo(3));
        }

        // ===== DZ-151: Shuffle null rng 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-151")]
        public void Given_nullのrng_When_Shuffleを呼ぶ_Then_ArgumentNullExceptionを投げる()
        {
            // Given
            var pool = new DdpPool(new[] { 10, 20, 30 });
            // When / Then
            Assert.Throws<ArgumentNullException>(() => pool.Shuffle(null));
        }

        // ===== DZ-152: Shuffle は決定的 + Fisher-Yates のマルチセット保存性 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-152")]
        public void Given_同一シードのrngで2回Shuffle_When_結果のValuesを比較_Then_完全一致()
        {
            // Given(決定性: 同一 seed → 同一結果)
            var pool = new DdpPool(new[] { 10, 20, 30, 40 });
            var a = pool.Shuffle(new XorShiftRandom(42));
            var b = pool.Shuffle(new XorShiftRandom(42));
            // When / Then
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-152")]
        public void Given_4要素のDdpPool_When_Shuffleを呼ぶ_Then_マルチセットが保存される()
        {
            // Given(Fisher-Yates のマルチセット保存性: 順序のみ変化し各値の出現回数は不変)
            var pool = new DdpPool(new[] { 10, 20, 30, 40 });
            // When
            var shuffled = pool.Shuffle(new XorShiftRandom(123));
            // Then(順序を sort で揃えて比較)
            var sortedExpected = new[] { 10, 20, 30, 40 };
            var sortedActual = new int[shuffled.Count];
            for (int i = 0; i < sortedActual.Length; i++)
            {
                sortedActual[i] = shuffled.Values[i];
            }
            Array.Sort(sortedActual);
            Assert.That(sortedActual, Is.EqualTo(sortedExpected));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-152")]
        public void Given_IdentityRandom_When_Shuffleを呼ぶ_Then_順序が変化しない()
        {
            // Given(IdentityRandom.NextInt(min, maxExclusive) は maxExclusive-1 を返すスタブ。
            // Fisher-Yates の `j = rng.NextInt(0, i+1)` で j = i となり swap(arr[i], arr[i]) = no-op、
            // 結果として元の順序が維持される。StartGameUseCaseTests と同パターン)
            var pool = new DdpPool(new[] { 10, 20, 30, 40 });
            // When
            var shuffled = pool.Shuffle(new IdentityRandom());
            // Then
            Assert.That(shuffled, Is.EqualTo(pool));
        }
    }
}
