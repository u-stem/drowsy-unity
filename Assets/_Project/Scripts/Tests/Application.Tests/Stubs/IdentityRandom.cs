using Drowsy.Domain.Random;

namespace Drowsy.Application.Tests.Stubs
{
    /// <summary>
    /// テスト用 <see cref="IRandomSource"/> stub。<see cref="NextInt"/> が常に <c>maxExclusive - 1</c> を返すため、
    /// Fisher-Yates シャッフル(<c>j = rng.NextInt(0, i + 1)</c>)では <c>j = i</c> となり <c>swap(arr[i], arr[i])</c>
    /// (no-op)になる。結果として「Shuffle 後の配列順 = 入力順」が保証され、
    /// テストが <see cref="XorShiftRandom"/> の seed 実装に依存しなくなる。
    /// </summary>
    /// <remarks>
    /// 用途: <c>StartGameUseCase</c> のテストで「Players 順 / FDP 順 / カード配布順が予測通り」を検証する際、
    /// rng の影響を排除して構造的な不変量(交互配布、被りなし抽選 等)に焦点を絞る。
    /// </remarks>
    internal sealed class IdentityRandom : IRandomSource
    {
        public int NextInt(int min, int maxExclusive) => maxExclusive - 1;
    }
}
