namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.TickEffect"/> として保有時、
    /// 影響保有者の自フェーズ開始時に発生する「ベッド破損由来の SDP 変動」を 2 倍化する marker
    /// (No.06「牙の届かぬ領域」、2026-05-17 で導入)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 既存 <see cref="UsageRestrictionMarkerEffect"/> / <see cref="RestrictSpecificCardInfluenceEffect"/> と同様の
    /// 「Tick 時 no-op、判別用 marker」パターン。実際の 2 倍化計算は <see cref="DrowZzzRule"/>.<c>ApplyBedDamageToCurrentPlayer</c>
    /// 内で本 marker を Influence walk で検出し、`sdpDamage *= 2`(現状の `bedDamage / DrowZzzBedConstants.BedDamageRatePerSdp` 結果を 2 倍化)を適用する。
    /// </para>
    /// <para>
    /// <b>将来拡張</b>:オーナー JIT 共有 2026-05-17 で「ベッド破損 SDP をプラスに変えるカードも今後紹介する」と確定。
    /// 本 marker は計算経路 1 箇所(`ApplyBedDamageToCurrentPlayer`)に集約されるため、将来「プラス変換 effect」が追加された際にも
    /// 計算結果に対する 2 倍化を共通経路で honor 可能(プラス値 +2 → +4 のような自然な拡張)。
    /// </para>
    /// <para>
    /// <see cref="EffectInterpreter"/> 経由評価は <b>session 不変返却(no-op)</b>。
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.RemainingCount"/> の Tick 減算と除去は
    /// <c>DrowZzzRule.TickInfluences</c> の責務で、本 marker 自体は影響しない(`UsageRestrictionMarkerEffect` と完全対称)。
    /// </para>
    /// </remarks>
    public sealed record DoubleBedDamageSdpInfluenceMarkerEffect : IEffect;
}
