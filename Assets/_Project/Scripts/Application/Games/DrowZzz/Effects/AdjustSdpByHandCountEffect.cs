namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="Drowsy.Application.Games.DrowZzz.Influences.PlayerInfluence.TickEffect"/> として使われ、
    /// 影響保有者の自フェーズ開始時 Tick で「保有者の SDP を保有者の Hand.Count だけ減らす」動的計算を行う effect
    /// (No.11「機械仕掛けの冬将軍」、2026-05-17 で導入)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 既存 marker 系 effect(`UsageRestrictionMarkerEffect` / `DoubleBedDamageSdpInfluenceMarkerEffect` 等)が
    /// 「Tick 時 no-op で session を変えない判別用フラグ」だったのに対し、本 effect は **Tick 時に動的に SDP を更新** する
    /// 能動 effect。`AdjustSdpEffect(Self, -5)` の固定 delta(No.02「緑の侵攻」)に対して、本 effect は
    /// **保有者の Hand.Count に依存する動的 delta** を表現する。
    /// </para>
    /// <para>
    /// 計算式:Tick 時の `session.GameState.Turn.CurrentPlayerIndex` が指すプレイヤー(= 影響保有者、
    /// `TickInfluencesForCurrentPlayer` は新 current player に対して Tick するため)の Hand.Count を取得し、
    /// `SDP[保有者] -= Hand.Count` を適用する。Hand.Count == 0 なら no-op(SDP -0 = 不変)。
    /// </para>
    /// <para>
    /// セマンティクス:「思考に存在する手段の数」(= 保有者の手札枚数)に応じて自分が眠くなる
    /// (SDP マイナス = Drowzzz では「眠くなる」= 有利方向)。手札を多く抱えている = 退却型デバフを受ける戦術カード。
    /// 保有者にとっては「手札を減らしてから自フェーズを迎えれば被害が少ない」というプレイ判断を促す。
    /// </para>
    /// <para>
    /// フィールドなし record。将来「相手 Hand 依存」「Hand 以外の集合依存」が必要になれば、
    /// `AdjustSdpByHandCountEffect(SdpTarget Target, int Multiplier, int Sign)` のような汎用化を別 effect で行う方針
    /// (No.06 DoubleBedDamage / No.08 InvertBedDamage と同じく、最初はカード固有特化型で導入)。
    /// </para>
    /// <para>
    /// <b>本 effect は `PlayerInfluence.TickEffect` 専用</b>。`PlayCardAction` の効果列に直接配置すると、
    /// `CurrentPlayerIndex` が actor を指すため「actor の手札残数 = actor が眠くなる量」という意図せぬ動作になる
    /// (カードプレイ直後は Hand から該当カード分が減算済の手札残数を見るため、設計者の意図と乖離しやすい)。
    /// `ApplyInfluenceEffect(SdpTarget.Opponent, PlayerInfluence(OwnPhaseStart, AdjustSdpByHandCountEffect, ...))` 経由でのみ使用する
    /// (code-reviewer W-1 反映 2026-05-17)。
    /// </para>
    /// </remarks>
    public sealed record AdjustSdpByHandCountEffect : IEffect;
}
