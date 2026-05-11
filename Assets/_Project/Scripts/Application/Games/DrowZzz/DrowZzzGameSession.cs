using System;
using System.Collections.Generic;
using System.Linq;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の完全状態(オラクルビュー、隠し情報含む)を表す record class。
    /// Domain <see cref="GameState"/> をラップし、DrowZzz 固有の追加状態
    /// (<see cref="FirstDrowsyPoints"/> / <see cref="PhaseState"/>) を保持する。
    /// </summary>
    /// <remarks>
    /// <c>record class</c> + <c>init</c> setter + バッキングフィールド + <c>value ?? throw</c> パターン
    /// (ADR-0004 polyfill 前提、Phase 1 <see cref="GameState"/> と同パターン)。
    /// コンストラクタおよび <c>with</c> 式の両経路で null 検証 + cross-field 検証
    /// (<see cref="FirstDrowsyPoints"/> のキー集合が <see cref="GameState"/>.Players の <see cref="PlayerId"/> 集合と一致するか)
    /// が走る。詳細は ADR-0006 §2.2 を参照。
    /// 各プレイヤー視点での隠し情報フィルタは Presentation 層 (M5) で実装し、本クラスは完全可視オラクルとする。
    /// <para>
    /// <see cref="Equals(DrowZzzGameSession)"/> / <see cref="GetHashCode"/> は record の auto-generated を上書きする。
    /// 内部 <see cref="_firstDrowsyPoints"/> が <see cref="Dictionary{TKey, TValue}"/> のため auto-equals は
    /// 参照同値にフォールバックして値同値が壊れる(<see cref="GameState"/> と同じ判断軸、ADR-0002)。
    /// 順序非依存マルチセット同値で比較する。
    /// </para>
    /// </remarks>
    public sealed record DrowZzzGameSession
    {
        private readonly GameState _gameState;
        private readonly Dictionary<PlayerId, int> _firstDrowsyPoints;
        private readonly DrowZzzPhaseState _phaseState;

        /// <summary>Domain ルート集約。</summary>
        public GameState GameState
        {
            get => _gameState;
            init
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                // cross-field: 既に FirstDrowsyPoints が確定していれば、その PlayerId キー集合と新 GameState.Players が一致するか検証
                if (_firstDrowsyPoints is not null)
                {
                    EnsureKeysMatchPlayers(_firstDrowsyPoints, value, nameof(value));
                }
                _gameState = value;
            }
        }

        /// <summary>
        /// 各プレイヤーの FDP (First Drowsy Point)。プレイヤーごとに隠し情報、ゲーム開始時に確定し以降不変。
        /// キー集合は <see cref="GameState"/>.Players の <see cref="PlayerId"/> 集合と完全一致する必要がある。
        /// </summary>
        public IReadOnlyDictionary<PlayerId, int> FirstDrowsyPoints
        {
            get => _firstDrowsyPoints;
            init
            {
                var copied = ValidateAndCopyFdp(value);
                if (_gameState is not null)
                {
                    EnsureKeysMatchPlayers(copied, _gameState, nameof(value));
                }
                _firstDrowsyPoints = copied;
            }
        }

        /// <summary>ターン内フェーズ(Application 層管理のステートマシン)。</summary>
        public DrowZzzPhaseState PhaseState
        {
            get => _phaseState;
            init => _phaseState = value;
        }

        /// <summary>
        /// DrowZzz の「ターン (=ラウンド)」を Phase 1 <c>TurnNumber</c> から計算する(N=2 想定)。
        /// N&gt;2 拡張は Phase 3 候補(ADR-0006 §Negative)。
        /// </summary>
        public int CurrentRound => (_gameState.Turn.TurnNumber + 1) / 2;

        /// <summary>
        /// DrowZzz のゲーム内時計(<see cref="CurrentRound"/> 由来の値オブジェクト)。
        /// </summary>
        /// <remarks>
        /// ADR-0008 §2 で確定した案 X(computed プロパティ採用)。真の単一情報源は
        /// <c>TurnState.TurnNumber</c> で、<see cref="DrowZzzClock.RoundNumber"/> ≡ <see cref="CurrentRound"/>
        /// が構造的に保証される(DZ-097)。<c>Equals</c> / <c>GetHashCode</c> での二重カウントは不要。
        /// 詳細は <c>docs/specs/games/drowzzz/clock.md</c> を参照。
        /// </remarks>
        public DrowZzzClock Clock => new DrowZzzClock(CurrentRound);

        /// <summary>
        /// DrowZzzGameSession を生成する。
        /// </summary>
        /// <exception cref="ArgumentNullException">gameState または firstDrowsyPoints が null</exception>
        /// <exception cref="ArgumentException">firstDrowsyPoints のキー集合が gameState.Players の PlayerId 集合と一致しない場合</exception>
        public DrowZzzGameSession(
            GameState gameState,
            IReadOnlyDictionary<PlayerId, int> firstDrowsyPoints,
            DrowZzzPhaseState phaseState)
        {
            // 順序が重要: GameState を先に確定 → FirstDrowsyPoints の init setter で _gameState を参照して cross-field 検証。
            // (GameState init setter 時点では _firstDrowsyPoints が null なので cross-field はスキップされる)
            GameState = gameState;
            FirstDrowsyPoints = firstDrowsyPoints;
            PhaseState = phaseState;
        }

        // FirstDrowsyPoints の防御コピー + null 検証
        private static Dictionary<PlayerId, int> ValidateAndCopyFdp(IReadOnlyDictionary<PlayerId, int> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var buffer = new Dictionary<PlayerId, int>(source.Count);
            foreach (var kv in source)
            {
                buffer[kv.Key] = kv.Value;
            }
            return buffer;
        }

        // FirstDrowsyPoints のキー集合と GameState.Players の PlayerId 集合の一致検証
        // paramName は呼び出し元 init setter の `nameof(value)` を受け取る (GameState init setter のパターン整合)
        private static void EnsureKeysMatchPlayers(
            IReadOnlyDictionary<PlayerId, int> fdp,
            GameState gameState,
            string paramName)
        {
            var playerIds = gameState.Players.Select(p => p.Id).ToHashSet();
            var fdpKeys = fdp.Keys.ToHashSet();
            if (!playerIds.SetEquals(fdpKeys))
            {
                throw new ArgumentException(
                    $"FirstDrowsyPoints のキー集合が GameState.Players の PlayerId 集合と一致しません " +
                    $"(Players: [{string.Join(", ", playerIds.Select(id => id.Value))}], " +
                    $"FDP keys: [{string.Join(", ", fdpKeys.Select(id => id.Value))}])",
                    paramName);
            }
        }

        /// <summary>
        /// 順序非依存マルチセット同値で比較する。<see cref="_firstDrowsyPoints"/> はキー順を問わず
        /// 各 <see cref="PlayerId"/>-<see cref="int"/> ペアの一致で判定する。
        /// </summary>
        public bool Equals(DrowZzzGameSession other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (!_gameState.Equals(other._gameState))
            {
                return false;
            }
            if (_phaseState != other._phaseState)
            {
                return false;
            }
            if (_firstDrowsyPoints.Count != other._firstDrowsyPoints.Count)
            {
                return false;
            }
            foreach (var kv in _firstDrowsyPoints)
            {
                if (!other._firstDrowsyPoints.TryGetValue(kv.Key, out var v) || v != kv.Value)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 順序非依存ハッシュ。<see cref="_gameState"/> / <see cref="_phaseState"/> および
        /// <see cref="_firstDrowsyPoints"/> の各 (key, value) ペアハッシュを XOR 合成する
        /// (CardData と同じパターン、ADR-0002)。
        /// </summary>
        public override int GetHashCode()
        {
            int hash = HashCode.Combine(_gameState, _phaseState);
            foreach (var kv in _firstDrowsyPoints)
            {
                hash ^= HashCode.Combine(kv.Key, kv.Value);
            }
            return hash;
        }
    }
}
