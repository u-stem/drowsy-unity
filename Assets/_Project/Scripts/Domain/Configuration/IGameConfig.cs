using System.Collections.Generic;

namespace Drowsy.Domain.Configuration
{
    /// <summary>
    /// ゲームバランス調整可能値の抽象。Phase 2 で具体プロパティを順次追加していく。
    /// </summary>
    /// <remarks>
    /// 実装ガイドラインは <c>docs/architecture/constants-management.md</c> を参照。
    /// <list type="bullet">
    /// <item>本 interface は Drowsy.Domain で純粋 C# として宣言される(UnityEngine 非依存)</item>
    /// <item>具象実装は Drowsy.Infrastructure で <c>ScriptableObject</c> として提供される(M2 以降)</item>
    /// <item>テスト時は Mock / Stub 実装を注入することで異なる値で検証可能</item>
    /// </list>
    /// 追加予定プロパティの一覧と追加 PR は ADR-0006 §1.4 に記録される。
    /// </remarks>
    public interface IGameConfig
    {
        /// <summary>
        /// DrowZzz の First Drowsy Point (FDP) 抽選プール。
        /// ゲーム開始時に各プレイヤーに重複なく割り当てる候補値の一覧。
        /// DrowZzz の現行値は <c>[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]</c>(ADR-0006 §M1 / §1.4)。
        /// プレイヤー数 N に対して <c>FdpPool.Count &gt;= N</c> である必要がある(検証は <c>StartGameUseCase</c>)。
        /// </summary>
        IReadOnlyList<int> FdpPool { get; }

        // 後続追加予定:
        //   int MaxRoundNumber { get; }   // M3 着手 PR (ゲーム終了判定)
        //   その他 M2 以降のカード効果実装で必要になり次第追加
    }
}
