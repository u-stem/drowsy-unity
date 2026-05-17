namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// プレイ時に <see cref="PlayCardAction.TargetCardId"/> を読んで、<see cref="Source"/> プレイヤーの
    /// 手札から該当カードを除去し共通山札(<c>GameState.Deck</c>)の top に置く効果
    /// (No.05「喧騒を纏う」、2026-05-17 で導入)。
    /// </summary>
    /// <param name="Source">対象カードを取り出すプレイヤー(No.05 では <see cref="SdpTarget.Self"/>)</param>
    /// <remarks>
    /// <para>
    /// 戦略意図:現プレイヤー(actor)が「次のターン相手が引きやすい / 自分が再び引きやすい」位置に
    /// 自分の手札の特定カードを押し込む。No.05「喧騒を纏う」では <see cref="SdpTarget.Self"/> 固定で
    /// 「自分の手札から相手に押し付ける」セマンティクス(次のターン開始時に相手が `DrawCardAction` で引く)。
    /// </para>
    /// <para>
    /// 動的部分(対象 CardId)はプレイ時の <see cref="PlayCardAction.TargetCardId"/> に応じて決まるため、
    /// <see cref="ApplyTargetedRestrictionEffect"/> と同パターンで <see cref="EffectContext.TargetCardId"/> を
    /// 経由して <see cref="EffectInterpreter"/> 内で構築する(record 自体には対象 CardId を保持しない)。
    /// </para>
    /// <para>
    /// <b>合法性検証</b>(<see cref="DrowZzzRule.IsLegalPlayCard"/> 拡張):本 effect を含むカードプレイ時:
    /// <list type="bullet">
    /// <item>(1) <see cref="PlayCardAction.TargetCardId"/> が指定されていること</item>
    /// <item>(2) <see cref="Source"/> プレイヤーの手札に <see cref="PlayCardAction.TargetCardId"/> が含まれていること</item>
    /// <item>(3) <see cref="PlayCardAction.TargetCardId"/> が <c>session.AssociatedCardIds</c> に含まれていないこと
    /// (連想由来は選択不可、ADR-0019 PR ② で確立した除外パターンと共通)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed record StackHandCardOnDeckTopEffect(SdpTarget Source) : IEffect;
}
