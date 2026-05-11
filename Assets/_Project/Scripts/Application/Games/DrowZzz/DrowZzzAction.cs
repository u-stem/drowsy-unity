using System;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz のアクション階層の基底。
    /// <see cref="IGameAction"/> を実装するマーカー record で、具体アクションは sealed record で派生する。
    /// 詳細は ADR-0006 §2.1 を参照。
    /// </summary>
    public abstract record DrowZzzAction : IGameAction;

    /// <summary>
    /// ゲーム開始アクション。セッション未生成状態で <c>StartGameUseCase</c> から扱う(M1-PR3 で実装)。
    /// プレイヤー Id は payload に持たず、<c>StartGameUseCase</c> の入力から決定する。
    /// </summary>
    public sealed record StartGameAction : DrowZzzAction;

    /// <summary>
    /// 山札から現プレイヤーの手札に 1 枚移動するアクション。
    /// 現プレイヤー Id は <c>session.GameState.Turn.CurrentPlayerIndex</c> から暗黙取得する。
    /// </summary>
    public sealed record DrawCardAction : DrowZzzAction;

    /// <summary>
    /// 現プレイヤーの手札から指定 <paramref name="Card"/> を場 (<c>Field</c>) に移動するアクション。
    /// <see cref="Card"/> は null 不可(生成時 / <c>with</c> 式 経由の両方で <see cref="ArgumentNullException"/> で防御)。
    /// </summary>
    /// <param name="Card">場に出すカードの識別子(現プレイヤーの手札に存在することが合法条件)</param>
    public sealed record PlayCardAction(CardId Card) : DrowZzzAction
    {
        /// <summary>
        /// 場に出すカードの識別子。
        /// record positional の自動生成 init setter に <c>value ?? throw</c> ガードを被せ、
        /// <c>new PlayCardAction(null)</c> も <c>existing with { Card = null }</c> も両方弾く
        /// (Phase 1 <c>PlayerState</c> / <c>GameState</c> の null 防御パターン整合)。
        /// </summary>
        public CardId Card { get; init; } = Card ?? throw new ArgumentNullException(nameof(Card));
    }

    /// <summary>
    /// ターン終了アクション。<c>GameState.Turn</c> を <c>Next(playerCount)</c> で次サブターンへ進める。
    /// </summary>
    public sealed record EndTurnAction : DrowZzzAction;
}
