namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 現プレイヤー(<see cref="SdpTarget.Self"/>)または相手プレイヤー(<see cref="SdpTarget.Opponent"/>)の
    /// <c>SecondDrowsyPoints</c> を <see cref="Delta"/> だけ加減する効果。
    /// </summary>
    /// <param name="Target">対象プレイヤー(Self / Opponent)</param>
    /// <param name="Delta">SDP の増減量(正値=増、負値=減、0=no-op)。負値での結果が負 SDP になっても 0 floor しない(DZ-109)</param>
    /// <remarks>
    /// M2-PR3 で導入する最初の actor 拡張(Self / Opponent)効果(ADR-0007 §1.4 JIT 確定)。
    /// 「コップ一杯の脅威」(No.01)カードで使用者 / 被使用者の両方の SDP を変動させる必要があるため、
    /// `SdpTarget` enum を positional 引数として効果 record 自身が保持する設計を採用(`SdpTarget.cs` remarks 参照)。
    /// <para>
    /// 評価は <see cref="EffectInterpreter.Apply"/> で行う。0 floor は適用しない(ADR-0009「持ち点低い方が勝ち」と整合、
    /// DZ-109 の負値許容方針)。
    /// </para>
    /// </remarks>
    public sealed record AdjustSdpEffect(SdpTarget Target, int Delta) : IEffect;
}
