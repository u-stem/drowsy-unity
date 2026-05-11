using System.Collections.Generic;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// <see cref="DdpPool"/> および <see cref="DrowZzzRule"/> の DDP 抽選機構が依存する DrowZzz 固有定数を集約する。
    /// </summary>
    /// <remarks>
    /// CLAUDE.md §9「マジックナンバー禁止」/「L1/L2 は <c>&lt;Module&gt;Constants</c> クラスの <c>const</c>」に従い、
    /// ADR-0009 §「DDP プールの構造」/ §「DDP 抽選タイミング」で確定した数値を命名された <c>const</c> として切り出す。
    /// Domain ではなく Application 層に配置するのは、DDP プール構造 / 抽選タイミングが DrowZzz 固有のゲームルールで
    /// あり Domain ゲーム非依存原則(ADR-0002)と整合させるため(<see cref="DrowZzzClockConstants"/> と同判断軸)。
    /// <para>
    /// 階層分類は <c>docs/architecture/constants-management.md</c> 参照:
    /// <list type="bullet">
    /// <item><b>L2</b>(ドメイン上の真の不変量): DDP プール値域(<see cref="MinValue"/> / <see cref="MaxValue"/> /
    ///       <see cref="Step"/>)/ 1 値あたり枚数(<see cref="CopiesPerValue"/>)/ プール総枚数
    ///       (<see cref="TotalPoolSize"/>)/ 抽選対象ターン(<see cref="DrawRounds"/>)はすべて DrowZzz の
    ///       真の不変量で、ゲームバランス調整 (L3) ではなく仕様の境界として固定。</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>L3 への移管可能性</b>: 将来 DDP プールの値域 / 枚数 / 抽選タイミングが調整パラメータ化する場合は
    /// <see cref="Drowsy.Domain.Configuration.IGameConfig"/> へ移管する(<c>docs/todo.md</c> で追跡)。現状は
    /// 仕様確定済の真の不変量として L2 const に集約する。
    /// </para>
    /// </remarks>
    public static class DdpPoolConstants
    {
        /// <summary>DDP プール初期値の最小値(L2、ADR-0009 §「DDP プールの構造」)。</summary>
        public const int MinValue = -30;

        /// <summary>DDP プール初期値の最大値(L2、ADR-0009 §「DDP プールの構造」)。</summary>
        public const int MaxValue = 30;

        /// <summary>DDP プール初期値の刻み(L2、5 刻みで <see cref="MinValue"/> から <see cref="MaxValue"/> まで)。</summary>
        public const int Step = 5;

        /// <summary>DDP プール初期値の各値あたり枚数(L2、ADR-0009 §「DDP プールの構造」)。</summary>
        public const int CopiesPerValue = 3;

        /// <summary>
        /// DDP プール初期値の値の種類数(L2、(<see cref="MaxValue"/> - <see cref="MinValue"/>) / <see cref="Step"/> + 1 = 13)。
        /// </summary>
        public const int DistinctValueCount = (MaxValue - MinValue) / Step + 1;

        /// <summary>
        /// DDP プール初期総枚数(L2、<see cref="DistinctValueCount"/> × <see cref="CopiesPerValue"/> = 36)。
        /// </summary>
        public const int TotalPoolSize = DistinctValueCount * CopiesPerValue;

        /// <summary>
        /// DDP 抽選対象ターン番号(L2、ADR-0009 §「DDP 抽選タイミング」: 23:00 / 01:00 / 03:00 / 05:00 / 07:00 =
        /// Turn 5 / 9 / 13 / 17 / 21)。
        /// </summary>
        /// <remarks>
        /// <c>const</c> 不可な配列のため <c>static readonly</c> + <see cref="IReadOnlyList{T}"/> で公開する
        /// (CLAUDE.md §9 「L2 は <c>const</c>」原則の例外として spec 内に明記、定数依存セクション参照)。
        /// 値の集合は ADR-0009 §「DDP 抽選タイミング」と同期し、変更時は ADR 改訂が必要。
        /// </remarks>
        public static readonly IReadOnlyList<int> DrawRounds = new[] { 5, 9, 13, 17, 21 };

        /// <summary>
        /// 既定の DDP プール(<see cref="DistinctValueCount"/> 種 × <see cref="CopiesPerValue"/> 枚 =
        /// <see cref="TotalPoolSize"/> 枚)を新規配列として生成する。
        /// 同じ値が連続して並ぶ整序状態で返るため、<see cref="DdpPool.Shuffle"/> で混ぜてから使う想定。
        /// </summary>
        /// <returns>長さ <see cref="TotalPoolSize"/> の <see cref="IReadOnlyList{T}"/></returns>
        public static IReadOnlyList<int> BuildDefaultPool()
        {
            var pool = new int[TotalPoolSize];
            int index = 0;
            for (int v = MinValue; v <= MaxValue; v += Step)
            {
                for (int c = 0; c < CopiesPerValue; c++)
                {
                    pool[index++] = v;
                }
            }
            return pool;
        }
    }
}
