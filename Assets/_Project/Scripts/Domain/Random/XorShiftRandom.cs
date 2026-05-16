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
            // `% range` は range が 2^32 の約数でない場合に modulo bias を生じる(分布の僅かな偏り)。
            // 本プロジェクトの最大想定範囲(FDP プール 10, DDP プール 39, 山札 60 弱)では
            // 偏り < 約 1e-8 で実用上検出不能なため、bias-free な rejection sampling は採用しない。
            // ゲームバランスに数学的厳密性を要する Phase 3+ で必要になれば差し替え検討。
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
