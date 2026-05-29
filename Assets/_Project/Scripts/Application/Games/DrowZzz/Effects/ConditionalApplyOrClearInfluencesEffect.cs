using System;
using Drowsy.Application.Games.DrowZzz.Influences;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 対象プレイヤーの保有 Influences 件数で挙動が分岐する条件付き effect
    /// (No.16「自分勝手な審判」、2026-05-17 で導入)。
    /// </summary>
    /// <param name="Target">対象プレイヤー(Self / Opponent)。Influence 件数のカウント対象 + Apply/Clear 対象</param>
    /// <param name="Threshold">境界値。`Influences.Count <= Threshold` なら Apply、`> Threshold` なら Clear</param>
    /// <param name="InfluenceToApply">Apply 経路で付与する `PlayerInfluence`(null 不可)</param>
    /// <remarks>
    /// <para>
    /// 評価ロジック(`EffectInterpreter` 内):
    /// <list type="number">
    /// <item>`Target` プレイヤーの `Influences.Count` を取得</item>
    /// <item>`Count <= Threshold` なら `InfluenceToApply` を末尾追加(既存 `ApplyInfluenceEffect` と同パターン)</item>
    /// <item>`Count > Threshold` なら Target プレイヤーの Influences を空 list で置換(全消滅)</item>
    /// </list>
    /// 両経路は排他(「Clear と Apply は排他、3 以上で消滅したら本カードの影響も付与されない」)。
    /// </para>
    /// <para>
    /// <b>カウントタイミング</b>:本 effect 評価時点の `session.Influences[Target].Count` を見る(snapshot 不要、
    /// 本 effect を含むカードの他 effect が Influences を変動させない前提)。
    /// 将来「同一カード内で先に Influences を変動させる effect」が追加される場合は本前提を再評価。
    /// </para>
    /// <para>
    /// <b>Clear の範囲</b>:対象プレイヤーの全 Influence(全 trigger:`OwnPhaseStart` / `OnOwnPlayCardAfter` /
    /// `OnOwnAbandonAfter`)を一括除去。Marker 系 Influence(`RestrictAllUsageAndAbandon` 等)も含めて全消滅
    /// (「受けている影響をすべて消滅」)。
    /// </para>
    /// </remarks>
    public sealed record ConditionalApplyOrClearInfluencesEffect(
        SdpTarget Target,
        int Threshold,
        PlayerInfluence InfluenceToApply) : IEffect
    {
        // null 防御の二重ガード(`RestrictSpecificCardInfluenceEffect` 同パターン)
        private readonly PlayerInfluence _influenceToApply = InfluenceToApply
            ?? throw new ArgumentNullException(nameof(InfluenceToApply));

        // Threshold 範囲ガード(positional ctor 経由):
        // 負値が許容されると `Count(>=0) <= Threshold(<0)` が常に false で「常に Clear」という直感に反した挙動になるため、
        // ArgumentOutOfRangeException で fail-fast。`RequiresMinimumTotalPointsMarkerEffect` の Threshold ガード同パターン。
        private readonly int _threshold = Threshold >= 0
            ? Threshold
            : throw new ArgumentOutOfRangeException(
                nameof(Threshold),
                $"Threshold は 0 以上である必要があります(Count<=Threshold で Apply、>Threshold で Clear のセマンティクス上、負値は意味不明): {Threshold}");

        /// <summary>Apply 経路で付与する PlayerInfluence。null 不可。</summary>
        public PlayerInfluence InfluenceToApply
        {
            get => _influenceToApply;
            init => _influenceToApply = value ?? throw new ArgumentNullException(nameof(InfluenceToApply));
        }

        /// <summary>境界値。Count <= Threshold で Apply、> Threshold で Clear。0 以上必須。</summary>
        public int Threshold
        {
            get => _threshold;
            init => _threshold = value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    nameof(Threshold),
                    $"Threshold は 0 以上である必要があります: {value}");
        }
    }
}
