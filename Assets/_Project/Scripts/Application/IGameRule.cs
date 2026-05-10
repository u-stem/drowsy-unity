namespace Drowsy.Application
{
    /// <summary>
    /// ゲームの状態遷移ルールを表す純関数 interface。
    /// <see cref="IsLegalMove"/> で合法性を判定し、<see cref="Apply"/> で次セッションを返す。
    /// 各ゲームの実装(例: <c>DrowZzzRule</c>)が具象型でこの interface を実装する。
    /// </summary>
    /// <remarks>
    /// 最小 API は 2 メソッドのみ(<c>EnumerateLegalActions</c> 等の AI / UI ヒント用 API は YAGNI)。
    /// 終了判定 API(<c>IsTerminated</c> / <c>GetWinner</c>)は M3 着手 PR で追加予定。
    /// 詳細は ADR-0006 §1.2 を参照。
    /// </remarks>
    /// <typeparam name="TAction">ゲーム固有のアクション型(<see cref="IGameAction"/> を実装)</typeparam>
    /// <typeparam name="TSession">ゲーム固有のセッション型(完全状態 / オラクルビュー)</typeparam>
    public interface IGameRule<TAction, TSession>
        where TAction : IGameAction
    {
        /// <summary>
        /// 与えられた <paramref name="session"/> 状態で <paramref name="action"/> が合法かどうかを返す。
        /// 副作用なしの純関数。
        /// </summary>
        bool IsLegalMove(TSession session, TAction action);

        /// <summary>
        /// 与えられた <paramref name="session"/> 状態に <paramref name="action"/> を適用した次セッションを返す。
        /// 副作用なしの純関数。呼び出し側は事前に <see cref="IsLegalMove"/> で合法性を確認する想定。
        /// 不正な <paramref name="action"/> を渡した場合の挙動は実装(例: <c>InvalidOperationException</c>)に委ねる。
        /// </summary>
        TSession Apply(TSession session, TAction action);
    }
}
