namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 指定プレイヤー(<see cref="SdpTarget.Self"/> / <see cref="SdpTarget.Opponent"/>)の影響リストから
    /// 影響を 1 件除去する効果。除去対象の index は <see cref="EffectContext.InfluenceRemovalIndex"/>
    /// から取得する(カードをプレイするプレイヤーの選択を action 経由で transfer)。
    /// </summary>
    /// <param name="Target">影響を除去する対象プレイヤー</param>
    /// <remarks>
    /// 継続影響(Influence)の除去系効果。カード No.02「緑の侵攻」の「プレイヤーが選択して影響 1 つを消滅させる」に使う
    /// (カードをプレイするプレイヤーが index を action に指定して選択する仕様)。
    /// <para>
    /// 除去の挙動(graceful):
    /// <list type="bullet">
    /// <item>対象プレイヤーの影響リストが空 → no-op(session 不変返却、カードは play 完了)</item>
    /// <item>index が範囲外(負値 or 件数以上)→ no-op(同上、illegal move 扱いせずプレイ者の選択ミスを許容)</item>
    /// <item>index が範囲内 → 該当影響を list から除去、後続要素は前にシフト</item>
    /// </list>
    /// <c>IsLegalMove</c> 段階で index 範囲を強制チェックしない理由: 影響リストの内容(件数)は action 構築側が
    /// 知らない場合があるため(UI から index=0 を default 送付するケース等)。illegal-move 化は Presentation 層
    /// が UI 上で取り得る index を制約する方式と再評価する。
    /// </para>
    /// </remarks>
    public sealed record RemoveInfluenceEffect(SdpTarget Target) : IEffect;
}
