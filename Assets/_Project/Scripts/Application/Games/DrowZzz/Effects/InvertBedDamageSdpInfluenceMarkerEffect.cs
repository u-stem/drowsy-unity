namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.TickEffect"/> として保有時、
    /// 影響保有者の自フェーズ開始時に発生する「ベッド破損由来の SDP 変動」の符号を反転する marker
    /// (No.08「廻るための知恵」、2026-05-17 で導入)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// `UsageRestrictionMarkerEffect` / `DoubleBedDamageSdpInfluenceMarkerEffect`(No.06)と同パターンの「Tick 時 no-op、
    /// 判別用 marker」。実際の符号反転計算は <see cref="DrowZzzRule"/>.<c>ApplyBedDamageToCurrentPlayer</c> 内で
    /// 本 marker を Influence walk で **保有数カウント** し、奇偶判定で反転する。
    /// </para>
    /// <para>
    /// <b>奇偶判定セマンティクス</b>(オーナー JIT 確定 2026-05-17):「廻るための知恵」は 3 枚あり、同一プレイヤーに
    /// 同種 marker が 1〜3 件付与される可能性がある。保有数 1 件 = 反転 / 2 件 = 元に戻る / 3 件 = 反転、と
    /// 「そのたびに ± が変わる」セマンティクス。`No.06 DoubleBedDamage` の「bool 検出(複数保有時も 2 倍止まり)」とは
    /// 対照的な設計判断(本 marker 専用、`CountInvertBedDamageInfluence` 経由)。
    /// </para>
    /// <para>
    /// <b>No.06 2 倍化 marker との重複保有時の挙動</b>:`ApplyBedDamageToCurrentPlayer` 内で
    /// 「逆転(符号反転) → 2 倍化」の順で適用する(オーナー JIT 確定 2026-05-17、SDP -8 → +8 → +16)。
    /// </para>
    /// <para>
    /// <see cref="EffectInterpreter"/> 経由評価は <b>session 不変返却(no-op)</b>。RemainingCount 減算と除去は
    /// <c>DrowZzzRule.TickInfluences</c> の責務で、本 marker 自体は影響しない。
    /// </para>
    /// </remarks>
    public sealed record InvertBedDamageSdpInfluenceMarkerEffect : IEffect;
}
