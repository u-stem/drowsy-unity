using System;

namespace Drowsy.Domain.Game
{
    /// <summary>
    /// ゲームのターン進行状態(ターン番号と現在ターンプレイヤーの index)を表す不変値オブジェクト。
    /// </summary>
    /// <remarks>
    /// <c>record</c> として実装する(内部に辞書 / 配列を持たないため auto-equals が値同値で正しく動く、
    /// <see cref="Players.PlayerState"/> と同じ判断軸)。
    /// <see cref="TurnNumber"/> は 1 始まり(ゲーム開始時 = ターン 1)。
    /// <see cref="CurrentPlayerIndex"/> は <c>GameState.Players</c> 内の 0 始まり index で、
    /// 上限(<c>Players.Count</c> 未満)の検証は <see cref="GameState"/> 側の責務(GS-022)。
    /// </remarks>
    public sealed record TurnState
    {
        /// <summary>ゲーム開始から数えたターン番号(1 始まり)。</summary>
        public int TurnNumber { get; }

        /// <summary><c>GameState.Players</c> における現在ターンプレイヤーの 0 始まり index。</summary>
        public int CurrentPlayerIndex { get; }

        /// <summary>
        /// TurnState を生成する。<paramref name="turnNumber"/> &lt; 1 または
        /// <paramref name="currentPlayerIndex"/> &lt; 0 で <see cref="ArgumentOutOfRangeException"/>。
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">turnNumber が 1 未満、または currentPlayerIndex が負の場合</exception>
        public TurnState(int turnNumber, int currentPlayerIndex)
        {
            if (turnNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(turnNumber), turnNumber,
                    "TurnNumber は 1 以上である必要があります(1 始まり)");
            }
            if (currentPlayerIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentPlayerIndex), currentPlayerIndex,
                    "CurrentPlayerIndex は 0 以上である必要があります");
            }
            TurnNumber = turnNumber;
            CurrentPlayerIndex = currentPlayerIndex;
        }

        /// <summary>
        /// 初期 TurnState を生成する。<see cref="TurnNumber"/> = 1、<see cref="CurrentPlayerIndex"/> = <paramref name="playerIndex"/>。
        /// </summary>
        /// <param name="playerIndex">最初にターンを取るプレイヤーの 0 始まり index</param>
        /// <exception cref="ArgumentOutOfRangeException">playerIndex が負の場合</exception>
        public static TurnState Initial(int playerIndex = 0) => new TurnState(1, playerIndex);

        /// <summary>
        /// 次のターンに進めた新 TurnState を返す。
        /// <see cref="TurnNumber"/> + 1、<see cref="CurrentPlayerIndex"/> = (current + 1) % playerCount。
        /// </summary>
        /// <param name="playerCount">プレイヤー総数(GameState.Players.Count を渡す想定)</param>
        /// <exception cref="ArgumentOutOfRangeException">playerCount が 0 以下の場合</exception>
        public TurnState Next(int playerCount)
        {
            if (playerCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerCount), playerCount,
                    "playerCount は 1 以上である必要があります");
            }
            return new TurnState(TurnNumber + 1, (CurrentPlayerIndex + 1) % playerCount);
        }
    }
}
