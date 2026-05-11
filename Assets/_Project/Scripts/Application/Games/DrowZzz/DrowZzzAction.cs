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
        // バッキングフィールド + init setter 本体に検証を入れることで、record positional ctor 経由
        // (init setter を介して代入される) と `with { Card = ... }` 経由の両方で null を弾く。
        // `public CardId Card { get; init; } = Card ?? throw ...` の「初期化式」は constructor 1 回のみ
        // 評価され with 経路をカバーしないため、Phase 1 GameState と同じ getter/setter 全置換パターンを採用。
        private readonly CardId _card;

        /// <summary>場に出すカードの識別子。</summary>
        public CardId Card
        {
            get => _card;
            init => _card = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// ターン終了アクション。<c>GameState.Turn</c> を <c>Next(playerCount)</c> で次サブターンへ進める。
    /// </summary>
    public sealed record EndTurnAction : DrowZzzAction;
}
