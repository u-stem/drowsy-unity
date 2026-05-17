namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Target"/> プレイヤーの Influences から <see cref="InvertBedDamageSdpInfluenceMarkerEffect"/> を
    /// TickEffect として持つ Influence を **1 件だけ** 除去する効果(No.07「知恵の及ばぬ領域」、2026-05-17 で導入)。
    /// </summary>
    /// <param name="Target">影響を除去する対象プレイヤー(No.07 では <see cref="SdpTarget.Opponent"/>)</param>
    /// <remarks>
    /// <para>
    /// 既存 <see cref="RemoveInfluenceEffect"/>(index 指定で削除)とは異なる **型マッチ削除** セマンティクス。
    /// 「廻るための知恵」(No.08)が付与する <see cref="InvertBedDamageSdpInfluenceMarkerEffect"/> を識別キーとして、
    /// 該当 marker 型の Influence を **先頭から 1 件削除** する(該当なしなら graceful no-op、`RemoveInfluenceEffect` の
    /// 範囲外 no-op と同パターン)。
    /// </para>
    /// <para>
    /// <b>1 件削除セマンティクス</b>(オーナー JIT 確定 2026-05-17 「1 でいい」):複数件保有していても 1 件だけ削除。
    /// 例:保有 3 件 → 削除後 2 件(奇偶判定で「反転 × 反転 = 元」となる中間状態)。No.07 を複数回プレイすれば
    /// 順次削除されるが、本 PR 範囲では No.07 = 1 枚 / No.08 = 3 枚のため最悪 3 回プレイで全削除可能。
    /// </para>
    /// <para>
    /// <b>将来汎用化候補</b>(本 PR では実装せず):本効果を <c>RemoveInfluenceByTickEffectTypeEffect(SdpTarget Target, IEffect markerPrototype)</c>
    /// のような汎用型にリファクタする選択肢があるが、現状は専用 effect で単純さを優先(他カードで類似ニーズが
    /// 出てきた時点で再評価)。
    /// </para>
    /// </remarks>
    public sealed record RemoveInvertBedDamageInfluenceEffect(SdpTarget Target) : IEffect;
}
