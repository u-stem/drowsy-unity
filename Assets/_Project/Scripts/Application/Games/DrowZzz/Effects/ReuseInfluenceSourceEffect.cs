namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 「現プレイヤーが保有する <c>PlayerInfluence</c> から 1 つを選択し、その影響を生成したカード(発生源)の
    /// 効果列を本プレイヤー <see cref="SdpTarget.Self"/> 起点で再 <see cref="EffectInterpreter"/> + 選択影響を除去」する
    /// 効果マーカー record(No.18「対抗手段」で初導入)。
    /// </summary>
    /// <remarks>
    /// 実評価は <see cref="EffectInterpreter"/> ではなく <c>DrowZzzRule.ApplyPlayCard</c> の専用ヘルパー
    /// (<see cref="ChoiceEffect"/> と同じ「rule 評価層で unwrap される」パターン)で行う。
    /// <see cref="EffectInterpreter.Apply"/> に直接渡された場合は <c>no-op</c>(session 不変返却)で
    /// 再帰防止を兼ねる(Reuse 中の OriginEffects 再評価で本 effect を踏んでも何も起きない)。
    /// <para>
    /// 影響の選択 index は <c>PlayCardAction.Choice</c> を流用する(本カードは <see cref="ChoiceEffect"/> を持たないため衝突なし)。
    /// </para>
    /// <para>
    /// 合法性判定の追加条件(<c>DrowZzzRule.IsLegalPlayCard</c>):
    /// <list type="bullet">
    /// <item>現プレイヤーの <c>Influences</c> が空 → illegal(無対象なら使えない)</item>
    /// <item><c>action.Choice</c> が範囲外(<c>Choice &lt; 0</c> or <c>Choice &gt;= Influences.Count</c>)→ illegal</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed record ReuseInfluenceSourceEffect : IEffect;
}
