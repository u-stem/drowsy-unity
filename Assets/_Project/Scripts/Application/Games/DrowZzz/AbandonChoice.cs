namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// <see cref="AbandonAction"/> の放棄選択肢(M3-PR3 で導入、ADR-0011 §2)。
    /// </summary>
    /// <remarks>
    /// ADR-0011 §2 で確定した「放棄(代替ターン行動)」の 2 つの選択肢:
    /// <list type="bullet">
    /// <item><see cref="GainSdp"/>: 現プレイヤーの SDP +5</item>
    /// <item><see cref="RepairBed"/>: 現プレイヤーのベッド破損率 -20%(下限 0%、JIT 確定 2026-05-13「0% では修繕不可」)</item>
    /// </list>
    /// いずれの選択肢でも、手札から <see cref="AbandonAction.CardIndex"/> で指定された 1 枚を捨て札に移動する。
    /// </remarks>
    public enum AbandonChoice
    {
        /// <summary>手札を 1 枚捨てて SDP +5。</summary>
        GainSdp,

        /// <summary>手札を 1 枚捨ててベッド破損率 -20%(下限 0%、現プレイヤーの BedDamages が 0% 時は illegal)。</summary>
        RepairBed,
    }
}
