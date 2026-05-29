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
    /// </remarks>
    public interface IGameConfig
    {
        /// <summary>
        /// DrowZzz の First Drowsy Point (FDP) 抽選プール。
        /// ゲーム開始時に各プレイヤーに重複なく割り当てる候補値の一覧。
        /// DrowZzz の現行値は <c>[0, 10, 20, 30, 35, 40, 45, 50, 55, 60]</c>。
        /// プレイヤー数 N に対して <c>FdpPool.Count &gt;= N</c> である必要がある(検証は <c>StartGameUseCase</c>)。
        /// </summary>
        IReadOnlyList<int> FdpPool { get; }

        /// <summary>
        /// DrowZzz の Draw Drowsy Point (DDP) 共有プール。
        /// ゲーム開始時に <c>StartGameUseCase</c> が <see cref="Drowsy.Domain.Random.IRandomSource"/> で Shuffle し、
        /// セッションに保持する。Turn 5 / 9 / 13 / 17 / 21 の開始時にプール先頭から N (= player count) 枚を抽選し、
        /// プールから除外して各プレイヤーの DDP に累積する。
        /// DrowZzz の現行値は 13 種(-30, -25, ..., +30)× 3 枚 = 39 要素。
        /// </summary>
        IReadOnlyList<int> DdpPool { get; }

        // 後続追加予定(M2 以降のカード効果実装で必要になり次第追加):
        //   その他バランス調整値(現状 FdpPool / DdpPool のみ)
        // 注: MaxRoundNumber は IGameConfig に追加しない判断。
        //   21 は Clock 構造に紐づく L2 数学的不変量で「調整」する対象ではないため、
        //   DrowZzzClockConstants.MaxRoundNumber を単一情報源とする。
        // 注: EarlyWinScoreThreshold (= 100) も IGameConfig には追加しない。
        //   ゲーム設計の核心値で L2 不変量、DrowZzzVictoryConstants が単一情報源。
    }
}
