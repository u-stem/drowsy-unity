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
    /// <param name="Choice">
    /// 選択式カード(<see cref="Drowsy.Application.Games.DrowZzz.Effects.ChoiceEffect"/> を含むカード)の選択肢 index
    /// (0-based)。非選択式カードでは無視される。M2-PR5 で導入(ADR-0007 §1.5、カード No.02「緑の侵攻」)。
    /// </param>
    /// <param name="InfluenceRemovalIndex">
    /// <see cref="Drowsy.Application.Games.DrowZzz.Effects.RemoveInfluenceEffect"/> を含む効果列の評価時に、
    /// 削除対象の影響 index(0-based)として参照される。範囲外なら no-op(graceful)。M2-PR5 で導入。
    /// </param>
    /// <remarks>
    /// M2-PR5 で <paramref name="Choice"/> / <paramref name="InfluenceRemovalIndex"/> を追加(両者 default 0 で後方互換維持、
    /// M1-PR5〜M2-PR4 の単一引数 ctor 呼び出しは全て継続動作)。
    /// </remarks>
    public sealed record PlayCardAction(CardId Card, int Choice = 0, int InfluenceRemovalIndex = 0) : DrowZzzAction
    {
        // null 防御の二重ガード:
        //  - バッキングフィールドの初期化式で positional ctor 経由の null を弾く (Card パラメータを使用、
        //    CS8907 回避: 「Parameter 'Card' is unread」警告を出さないため Card を初期化式で参照)
        //  - init setter 本体で `with { Card = null }` 経由の null を弾く (Phase 1 GameState パターン)
        // 両者とも `_card` を ArgumentNullException で防御し、生成時 / with 式の両経路をカバーする。
        private readonly CardId _card = Card ?? throw new ArgumentNullException(nameof(Card));

        /// <summary>場に出すカードの識別子。</summary>
        public CardId Card
        {
            get => _card;
            init => _card = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// ターン終了アクション。<c>GameState.Turn</c> を <c>Next(playerCount)</c> で次フェーズへ進める。
    /// </summary>
    public sealed record EndTurnAction : DrowZzzAction;
}
