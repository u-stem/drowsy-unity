using System;
using Drowsy.Domain.Players;

namespace Drowsy.Application
{
    /// <summary>
    /// ゲームの状態遷移ルールを表す純関数 interface。
    /// <see cref="IsLegalMove"/> で合法性を判定し、<see cref="Apply"/> で次セッションを返す。
    /// <see cref="IsTerminated"/> / <see cref="GetWinner"/> でゲーム終了状態を問う。
    /// 各ゲームの実装(例: <c>DrowZzzRule</c>)が具象型でこの interface を実装する。
    /// </summary>
    /// <remarks>
    /// M1 では <see cref="IsLegalMove"/> / <see cref="Apply"/> の 2 メソッドのみで起動。
    /// M3-PR1 で <see cref="IsTerminated"/> / <see cref="GetWinner"/> を追加(ADR-0010 §1)、
    /// generic interface への第一回拡張。<c>EnumerateLegalActions</c> 等の AI / UI ヒント用 API は YAGNI で見送る。
    /// 詳細は ADR-0006 §1.2 / ADR-0010 §1 を参照。
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

        /// <summary>
        /// 与えられた <paramref name="session"/> がゲーム終了状態かどうかを返す。副作用なしの純関数
        /// (M3-PR1 で追加、ADR-0010 §1)。
        /// </summary>
        /// <param name="session">問い合わせ対象のセッション</param>
        /// <returns>終了済みなら <c>true</c>、未終了なら <c>false</c></returns>
        bool IsTerminated(TSession session);

        /// <summary>
        /// 与えられた <paramref name="session"/> から勝者を返す。副作用なしの純関数(M3-PR1 で追加、ADR-0010 §1)。
        /// </summary>
        /// <param name="session">問い合わせ対象のセッション</param>
        /// <returns>
        /// <list type="bullet">
        /// <item>勝者がいる場合:勝者の <see cref="PlayerId"/></item>
        /// <item>引き分けの場合:<c>null</c></item>
        /// </list>
        /// 呼び出し側は事前に <see cref="IsTerminated"/> が <c>true</c> を返すことを確認する契約
        /// (未終了の session に対する呼び出しは <see cref="InvalidOperationException"/> を投げることが推奨される)。
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <see cref="IsTerminated"/> が <c>false</c> を返す未終了 session に対して呼ばれた場合
        /// </exception>
        PlayerId GetWinner(TSession session);
    }
}
