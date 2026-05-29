namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz のベッド破損機構関連の L2 数学的・ゲームルール不変量を集約する static 定数クラス。
    /// </summary>
    /// <remarks>
    /// ベッド破損計算式(<see cref="BedDamageRatePerSdp"/>)/ 破損率の境界
    /// (<see cref="MinBedDamagePercent"/> / <see cref="MaxBedDamagePercent"/>)を集約。
    /// `DrowZzzClockConstants` / `DdpPoolConstants` / `DrowZzzVictoryConstants` と同パターンで、意味グループ単位でファイル分離する。
    /// <para>
    /// 本クラスの定数はゲーム設計の核心値で `IGameConfig` のバランス調整値とは性質が違う(L2 不変量)。
    /// </para>
    /// </remarks>
    public static class DrowZzzBedConstants
    {
        /// <summary>
        /// ベッド破損率 1 単位あたりの SDP マイナス換算係数。「5% につき SDP -1」。
        /// 自ターン開始時の SDP マイナス計算は <c>bedDamage / BedDamageRatePerSdp</c>(整数除算)。
        /// </summary>
        public const int BedDamageRatePerSdp = 5;

        /// <summary>ベッド破損率の下限(0%、L2)。修繕効果(M3-PR3)はこの値でクランプ。</summary>
        public const int MinBedDamagePercent = 0;

        /// <summary>ベッド破損率の上限(100%、L2)。<see cref="Effects.DamageBedEffect"/> はこの値でクランプ。</summary>
        public const int MaxBedDamagePercent = 100;

        /// <summary>
        /// 1 回の修繕(<see cref="AbandonChoice.RepairBed"/>)で減少するベッド破損率(20%、L2)。
        /// 修繕後の値は <see cref="MinBedDamagePercent"/>(0%)で下限クランプ。
        /// </summary>
        public const int BedRepairPercent = 20;

        /// <summary>
        /// <see cref="AbandonChoice.GainSdp"/> で得られる SDP の増分(+5、L2)。
        /// </summary>
        public const int AbandonSdpGain = 5;
    }
}
