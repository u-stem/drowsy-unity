using System;

namespace Drowsy.Domain.Random
{
    /// <summary>
    /// ゲームロジックで使用する乱数源の抽象。
    /// テスト時に seed を固定して再現性を確保する目的で interface 化されている。
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>
        /// min 以上 maxExclusive 未満の整数を返す。
        /// </summary>
        /// <param name="min">下限(含む)</param>
        /// <param name="maxExclusive">上限(含まない)</param>
        /// <exception cref="ArgumentException">maxExclusive &lt;= min の場合</exception>
        int NextInt(int min, int maxExclusive);
    }
}
