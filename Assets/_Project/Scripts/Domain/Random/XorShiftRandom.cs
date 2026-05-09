using System;

namespace Drowsy.Domain.Random
{
    /// <summary>
    /// XorShift32 ベースの決定的擬似乱数源。同じ seed なら同じ系列を生成する。
    /// </summary>
    /// <remarks>
    /// XorShift32 は周期 2^32 - 1 の軽量乱数。ゲーム用途として十分な品質を持つ。
    /// より高品質な乱数(PCG 系)が必要になった場合は Phase 1 以降で差し替え検討。
    /// </remarks>
    public sealed class XorShiftRandom : IRandomSource
    {
        private uint _state;

        /// <summary>
        /// seed を指定して初期化する。
        /// seed == 0 は XorShift32 の退化点(常に 0 を返す)になるため、内部で 1 に補正する。
        /// </summary>
        public XorShiftRandom(uint seed)
        {
            _state = seed == 0u ? 1u : seed;
        }

        public int NextInt(int min, int maxExclusive)
        {
            if (maxExclusive <= min)
            {
                throw new ArgumentException(
                    $"maxExclusive ({maxExclusive}) は min ({min}) より大きい必要があります",
                    nameof(maxExclusive));
            }
            uint range = (uint)(maxExclusive - min);
            return min + (int)(NextUInt() % range);
        }

        private uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
