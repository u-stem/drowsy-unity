namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz のベッド破損機構関連の L2 数学的・ゲームルール不変量を集約する static 定数クラス。
    /// </summary>
    /// <remarks>
    /// ADR-0011 §3 で確定したベッド破損計算式(<see cref="BedDamageRatePerSdp"/>)/ 破損率の境界
    /// (<see cref="MinBedDamagePercent"/> / <see cref="MaxBedDamagePercent"/>)を集約。
    /// `DrowZzzClockConstants` / `DdpPoolConstants` / `DrowZzzVictoryConstants` と同パターンで、意味グループ単位で
    /// ファイル分離する方針を継続(ADR-0008 / ADR-0009 / ADR-0010 §9 と整合)。
    /// <para>
    /// 本クラスの定数は **ゲーム設計の核心値** で `IGameConfig` のバランス調整値とは性質が違う
    /// (ADR-0011 §3 / CLAUDE.md §9)。L2 不変量として constants 単一情報源(SSOT)で扱う。
    /// </para>
    /// </remarks>
    public static class DrowZzzBedConstants
    {
        /// <summary>
        /// ベッド破損率 1 単位あたりの SDP マイナス換算係数。「5% につき SDP -1」(ADR-0011 §3 JIT 確定 2026-05-12)。
        /// 自ターン開始時の SDP マイナス計算は <c>bedDamage / BedDamageRatePerSdp</c>(整数除算)。
        /// </summary>
        public const int BedDamageRatePerSdp = 5;

        /// <summary>ベッド破損率の下限(0%、L2)。修繕効果(M3-PR3)はこの値でクランプ。</summary>
        public const int MinBedDamagePercent = 0;

        /// <summary>ベッド破損率の上限(100%、L2)。<see cref="Effects.DamageBedEffect"/> はこの値でクランプ。</summary>
        public const int MaxBedDamagePercent = 100;

        /// <summary>
        /// 1 回の修繕(<see cref="AbandonChoice.RepairBed"/>)で減少するベッド破損率(20%、L2、ADR-0011 §2 / M3-PR3)。
        /// 修繕後の値は <see cref="MinBedDamagePercent"/>(0%)で下限クランプ。
        /// </summary>
        public const int BedRepairPercent = 20;

        /// <summary>
        /// <see cref="AbandonChoice.GainSdp"/> で得られる SDP の増分(+5、L2、ADR-0011 §2 / M3-PR3)。
        /// </summary>
        public const int AbandonSdpGain = 5;
    }
}
