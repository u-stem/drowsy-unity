using Drowsy.Domain.Cards;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 効果適用時のランタイム文脈(action から interpreter に渡す per-play 情報)。
    /// </summary>
    /// <param name="InfluenceRemovalIndex">
    /// <see cref="RemoveInfluenceEffect"/> が削除対象とする影響の index(0-based)。
    /// <see cref="PlayCardAction.InfluenceRemovalIndex"/> から transfer される。
    /// 範囲外の index は <c>RemoveInfluenceEffect</c> 適用時に no-op(graceful、影響がそもそも空かもしれないため)。
    /// </param>
    /// <param name="TargetCardId">
    /// <see cref="ApplyTargetedRestrictionEffect"/> が付与する影響の <c>TargetCardTypeId</c> を決定するために参照される。
    /// <see cref="PlayCardAction.TargetCardId"/> から transfer される。null は「未指定」を意味し、
    /// <see cref="ApplyTargetedRestrictionEffect"/> 評価時に null だと <see cref="System.InvalidOperationException"/>
    /// を投げる(防御的、IsLegalMove で事前防御済の前提)。ADR-0019 PR ② で追加(2026-05-17)。
    /// </param>
    /// <remarks>
    /// M2-PR5 で導入。カード No.02「緑の侵攻」の「プレイヤーが選択して影響 1 つを消滅させる」セマンティクスを実現するため、
    /// 「プレイ時の選択(action 由来)を effect record 評価に透過させる」仕組みとして context 引数を導入する
    /// (ADR-0007 §1.5「継続影響」/ §1.3 EffectInterpreter シグネチャ拡張)。
    /// <para>
    /// <c>EffectInterpreter.Apply(session, effect)</c> 2 引数 overload は <see cref="Default"/> 文脈で動作する
    /// 後方互換ラッパー。M2-PR4 までの既存 effect record(<see cref="AdjustSdpEffect"/> / <see cref="DrawCardEffect"/> /
    /// <see cref="TimeOfDayBranchEffect"/>)は context を読まないため、本拡張による挙動変更はない。
    /// </para>
    /// </remarks>
    public sealed record EffectContext(int InfluenceRemovalIndex, CardId TargetCardId = null)
    {
        /// <summary>
        /// 既定文脈(<see cref="InfluenceRemovalIndex"/> = 0、<see cref="TargetCardId"/> = null)。
        /// Tick 評価など action 由来の選択が無い場面で利用する。
        /// </summary>
        public static EffectContext Default { get; } = new EffectContext(0);
    }
}
