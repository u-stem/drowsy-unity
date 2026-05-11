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
    /// (<see cref="FirstDrowsyPoints"/> / <see cref="SecondDrowsyPoints"/> / <see cref="PhaseState"/>)
    /// を保持する。
    /// </summary>
    /// <remarks>
    /// <c>record class</c> + <c>init</c> setter + バッキングフィールド + <c>value ?? throw</c> パターン
    /// (ADR-0004 polyfill 前提、Phase 1 <see cref="GameState"/> と同パターン)。
    /// コンストラクタおよび <c>with</c> 式の両経路で null 検証 + cross-field 検証
    /// (FDP / SDP のキー集合が <see cref="GameState"/>.Players の <see cref="PlayerId"/> 集合と一致するか)
    /// が走る。詳細は ADR-0006 §2.2 / ADR-0009 §「DP 種別」を参照。
    /// 各プレイヤー視点での隠し情報フィルタは Presentation 層 (M5) で実装し、本クラスは完全可視オラクルとする。
    /// <para>
    /// <see cref="Equals(DrowZzzGameSession)"/> / <see cref="GetHashCode"/> は record の auto-generated を上書きする。
    /// 内部 <see cref="_firstDrowsyPoints"/> / <see cref="_secondDrowsyPoints"/> が <see cref="Dictionary{TKey, TValue}"/>
    /// のため auto-equals は参照同値にフォールバックして値同値が壊れる(<see cref="GameState"/> と同じ判断軸、ADR-0002)。
    /// 順序非依存マルチセット同値で比較する。
    /// </para>
    /// <para>
    /// M2-PR3 で <see cref="SecondDrowsyPoints"/>(SDP、公開情報・初期値 0・行動で変動、ADR-0009 §「DP 種別」)
    /// を追加。コンストラクタは 4 引数 (gameState, fdp, sdp, phaseState) に拡張(breaking change、既存テスト全件修正)。
    /// 真の単一情報源で TotalPoints は <see cref="TotalPoints"/> 計算メソッド(FDP + SDP、DDP は M2-PR4 で加算)。
    /// </para>
    /// </remarks>
    public sealed record DrowZzzGameSession
    {
        private readonly GameState _gameState;
        private readonly Dictionary<PlayerId, int> _firstDrowsyPoints;
        private readonly Dictionary<PlayerId, int> _secondDrowsyPoints;
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
                // cross-field: 既に FDP / SDP が確定していれば、それぞれの PlayerId キー集合と新 GameState.Players が一致するか検証
                if (_firstDrowsyPoints is not null)
                {
                    EnsureKeysMatchPlayers(_firstDrowsyPoints, value, nameof(value), dpName: "FirstDrowsyPoints");
                }
                if (_secondDrowsyPoints is not null)
                {
                    EnsureKeysMatchPlayers(_secondDrowsyPoints, value, nameof(value), dpName: "SecondDrowsyPoints");
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
                var copied = ValidateAndCopyDp(value, dpName: "FirstDrowsyPoints");
                if (_gameState is not null)
                {
                    EnsureKeysMatchPlayers(copied, _gameState, nameof(value), dpName: "FirstDrowsyPoints");
                }
                _firstDrowsyPoints = copied;
            }
        }

        /// <summary>
        /// 各プレイヤーの SDP (Second Drowsy Point)。プレイヤーごとに公開情報、初期値 0、各プレイヤーの行動で変動する
        /// (ADR-0009 §「DP 種別」、M2-PR3 で導入)。
        /// キー集合は <see cref="GameState"/>.Players の <see cref="PlayerId"/> 集合と完全一致する必要がある。
        /// 値は負値も許容する(0 floor なし、ADR-0009 「持ち点低い方が勝ち」と整合、DZ-109)。
        /// </summary>
        public IReadOnlyDictionary<PlayerId, int> SecondDrowsyPoints
        {
            get => _secondDrowsyPoints;
            init
            {
                var copied = ValidateAndCopyDp(value, dpName: "SecondDrowsyPoints");
                if (_gameState is not null)
                {
                    EnsureKeysMatchPlayers(copied, _gameState, nameof(value), dpName: "SecondDrowsyPoints");
                }
                _secondDrowsyPoints = copied;
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
        /// <exception cref="ArgumentNullException">いずれかの引数が null</exception>
        /// <exception cref="ArgumentException">
        /// FDP / SDP のキー集合が gameState.Players の PlayerId 集合と一致しない場合
        /// </exception>
        public DrowZzzGameSession(
            GameState gameState,
            IReadOnlyDictionary<PlayerId, int> firstDrowsyPoints,
            IReadOnlyDictionary<PlayerId, int> secondDrowsyPoints,
            DrowZzzPhaseState phaseState)
        {
            // 順序が重要: GameState を先に確定 → FDP / SDP の init setter で _gameState を参照して cross-field 検証。
            // (GameState init setter 時点では _firstDrowsyPoints / _secondDrowsyPoints が null なので cross-field はスキップされる)
            GameState = gameState;
            FirstDrowsyPoints = firstDrowsyPoints;
            SecondDrowsyPoints = secondDrowsyPoints;
            PhaseState = phaseState;
        }

        /// <summary>
        /// 指定プレイヤーの持ち点(= FDP + SDP)を計算する(ADR-0009 §「持ち点」、M2-PR3 段階)。
        /// DDP は M2-PR4 で加算予定(現状は未実装で 0 扱い、ADR-0009 §「DDP 抽選タイミング」)。
        /// </summary>
        /// <param name="playerId">対象プレイヤー</param>
        /// <returns>FDP + SDP の合計</returns>
        /// <exception cref="ArgumentNullException">playerId が null</exception>
        /// <exception cref="ArgumentException">playerId が Players に含まれない場合</exception>
        public int TotalPoints(PlayerId playerId)
        {
            if (playerId is null)
            {
                throw new ArgumentNullException(nameof(playerId));
            }
            if (!_firstDrowsyPoints.TryGetValue(playerId, out var fdp))
            {
                throw new ArgumentException(
                    $"PlayerId {playerId.Value} は Players に含まれていません",
                    nameof(playerId));
            }
            // SDP は cross-field 検証で FDP とキー集合一致が保証されているため、TryGetValue 失敗は構造的に不可能だが
            // 防御的に再確認(将来 cross-field 検証が緩んだ場合の保険)
            if (!_secondDrowsyPoints.TryGetValue(playerId, out var sdp))
            {
                throw new InvalidOperationException(
                    $"内部不変条件違反: SecondDrowsyPoints に PlayerId {playerId.Value} のキーがありません(cross-field 検証の漏れ)");
            }
            return fdp + sdp;
        }

        // FDP / SDP の防御コピー + null 検証(共通化、dpName でエラーメッセージを区別)
        private static Dictionary<PlayerId, int> ValidateAndCopyDp(
            IReadOnlyDictionary<PlayerId, int> source,
            string dpName)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source), $"{dpName} に null は渡せません");
            }
            var buffer = new Dictionary<PlayerId, int>(source.Count);
            foreach (var kv in source)
            {
                buffer[kv.Key] = kv.Value;
            }
            return buffer;
        }

        // DP のキー集合と GameState.Players の PlayerId 集合の一致検証
        // paramName は呼び出し元 init setter の `nameof(value)` を受け取る (GameState init setter のパターン整合)
        // dpName はエラーメッセージで FDP / SDP を区別するため
        private static void EnsureKeysMatchPlayers(
            IReadOnlyDictionary<PlayerId, int> dp,
            GameState gameState,
            string paramName,
            string dpName)
        {
            var playerIds = gameState.Players.Select(p => p.Id).ToHashSet();
            var dpKeys = dp.Keys.ToHashSet();
            if (!playerIds.SetEquals(dpKeys))
            {
                throw new ArgumentException(
                    $"{dpName} のキー集合が GameState.Players の PlayerId 集合と一致しません " +
                    $"(Players: [{string.Join(", ", playerIds.Select(id => id.Value))}], " +
                    $"{dpName} keys: [{string.Join(", ", dpKeys.Select(id => id.Value))}])",
                    paramName);
            }
        }

        /// <summary>
        /// 順序非依存マルチセット同値で比較する。<see cref="_firstDrowsyPoints"/> / <see cref="_secondDrowsyPoints"/>
        /// はキー順を問わず各 <see cref="PlayerId"/>-<see cref="int"/> ペアの一致で判定する。
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
            if (!DpEquals(_firstDrowsyPoints, other._firstDrowsyPoints))
            {
                return false;
            }
            if (!DpEquals(_secondDrowsyPoints, other._secondDrowsyPoints))
            {
                return false;
            }
            return true;
        }

        // FDP / SDP の順序非依存マルチセット同値判定(共通化)
        private static bool DpEquals(Dictionary<PlayerId, int> a, Dictionary<PlayerId, int> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 順序非依存ハッシュ。<see cref="_gameState"/> / <see cref="_phaseState"/> および
        /// <see cref="_firstDrowsyPoints"/> / <see cref="_secondDrowsyPoints"/> の各 (key, value) ペアハッシュを
        /// XOR 合成する(CardData と同じパターン、ADR-0002)。
        /// </summary>
        public override int GetHashCode()
        {
            int hash = HashCode.Combine(_gameState, _phaseState);
            foreach (var kv in _firstDrowsyPoints)
            {
                hash ^= HashCode.Combine(kv.Key, kv.Value);
            }
            foreach (var kv in _secondDrowsyPoints)
            {
                // SDP は FDP と XOR 衝突を避けるため hash を 1 bit 回転 (key, value, 1) で異なる組み合わせに
                hash ^= HashCode.Combine(kv.Key, kv.Value, 1);
            }
            return hash;
        }
    }
}
