using System;
using System.Collections.Generic;
using System.Linq;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の完全状態(オラクルビュー、隠し情報含む)を表す record class。
    /// Domain <see cref="GameState"/> をラップし、DrowZzz 固有の追加状態
    /// (<see cref="FirstDrowsyPoints"/> / <see cref="DrawDrowsyPoints"/> / <see cref="SecondDrowsyPoints"/> /
    /// <see cref="DdpPool"/> / <see cref="Influences"/> / <see cref="PhaseState"/>)を保持する。
    /// </summary>
    /// <remarks>
    /// <c>record class</c> + <c>init</c> setter + バッキングフィールド + <c>value ?? throw</c> パターン
    /// (ADR-0004 polyfill 前提、Phase 1 <see cref="GameState"/> と同パターン)。
    /// コンストラクタおよび <c>with</c> 式の両経路で null 検証 + cross-field 検証
    /// (FDP / DDP / SDP / Influences のキー集合が <see cref="GameState"/>.Players の <see cref="PlayerId"/> 集合と一致するか)
    /// が走る。詳細は ADR-0006 §2.2 / ADR-0009 §「DP 種別」§「DDP プールの構造」/ ADR-0007 §1.5「継続影響」を参照。
    /// 各プレイヤー視点での隠し情報フィルタは Presentation 層 (M5) で実装し、本クラスは完全可視オラクルとする。
    /// <para>
    /// <see cref="Equals(DrowZzzGameSession)"/> / <see cref="GetHashCode"/> は record の auto-generated を上書きする。
    /// 内部 DP 群(<see cref="_firstDrowsyPoints"/> / <see cref="_drawDrowsyPoints"/> / <see cref="_secondDrowsyPoints"/>)
    /// が <see cref="Dictionary{TKey, TValue}"/> のため auto-equals は参照同値にフォールバックして値同値が壊れる
    /// (<see cref="GameState"/> と同じ判断軸、ADR-0002)。順序非依存マルチセット同値で比較する。
    /// <see cref="DdpPool"/> は順序付きシーケンス同値(<see cref="DdpPool"/>.Equals)。
    /// <see cref="Influences"/> はキー順は問わず、各プレイヤーの影響 list は順序保持シーケンス同値で比較する
    /// (list の順序は FIFO Tick 規約上意味を持つため)。
    /// </para>
    /// <para>
    /// M2-PR3 で <see cref="SecondDrowsyPoints"/>(SDP、公開情報・初期値 0・行動で変動)を追加。
    /// M2-PR4 で <see cref="DrawDrowsyPoints"/>(DDP、隠し情報・累積式・Turn 5/9/13/17/21 開始時に共有プールから抽選)
    /// + <see cref="DdpPool"/>(残 DDP プール、プレイヤー間共有)を追加(ADR-0009 §「DP 種別」§「DDP プールの構造」)。
    /// M2-PR5 で <see cref="Influences"/>(継続影響、プレイヤーごとに 0+ 件保有、Tick 評価は <c>DrowZzzRule.ApplyEndTurn</c>)
    /// を追加(ADR-0007 §1.5)。
    /// M3-PR1 で <see cref="Outcome"/>(ゲーム終了状態、<c>GameOutcome?</c>、null = 未終了)を追加
    /// (ADR-0010 §3)。
    /// M3-PR2 で <see cref="BedDamages"/>(プレイヤーごとのベッド破損率、0〜100%)を追加
    /// (ADR-0011 §3)。コンストラクタは 9 引数 (gameState, fdp, ddp, sdp, ddpPool, influences, phaseState, outcome, bedDamages)
    /// に拡張(breaking change、既存テスト全件修正)。<see cref="TotalPoints"/> は FDP + DDP + SDP の 3 項合計を維持
    /// (ベッド破損は SDP に間接寄与:自ターン開始時に `bedDamage / 5` の SDP マイナスが入る、ADR-0011 §3 / §5)。
    /// <see cref="IsTerminated"/> は <see cref="Outcome"/> != null の薄い computed プロパティ。
    /// </para>
    /// </remarks>
    public sealed record DrowZzzGameSession
    {
        private readonly GameState _gameState;
        private readonly Dictionary<PlayerId, int> _firstDrowsyPoints;
        private readonly Dictionary<PlayerId, int> _drawDrowsyPoints;
        private readonly Dictionary<PlayerId, int> _secondDrowsyPoints;
        private readonly DdpPool _ddpPool;
        // _influences の value 型は IReadOnlyList<PlayerInfluence>(内部実装は防御コピー後の PlayerInfluence[])。
        // FDP / DDP / SDP と同じく Dictionary をそのまま IReadOnlyDictionary として公開する設計に合わせ、value 型を
        // IReadOnlyList で揃えることで getter の dict 再構築が不要になる(構築時にコピー、以降は immutable な扱い)。
        private readonly Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>> _influences;
        private readonly DrowZzzPhaseState _phaseState;
        // _outcome は null = 未終了、非 null = ゲーム終了状態(WinnerOutcome / DrawOutcome)。M3-PR1 で追加(ADR-0010 §3)。
        private readonly GameOutcome _outcome;
        // _bedDamages は各プレイヤーのベッド破損率(0〜100%)。M3-PR2 で追加(ADR-0011 §3)。
        // FDP / DDP / SDP と同パターン: Dictionary<PlayerId, int> を防御コピーで保持、cross-field 検証で Players キーと一致。
        private readonly Dictionary<PlayerId, int> _bedDamages;

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
                // cross-field: 既に DP 群 / Influences が確定していれば、それぞれの PlayerId キー集合と新 GameState.Players が一致するか検証
                if (_firstDrowsyPoints is not null)
                {
                    EnsureKeysMatchPlayers(_firstDrowsyPoints.Keys, value, nameof(value), keysetName: "FirstDrowsyPoints");
                }
                if (_drawDrowsyPoints is not null)
                {
                    EnsureKeysMatchPlayers(_drawDrowsyPoints.Keys, value, nameof(value), keysetName: "DrawDrowsyPoints");
                }
                if (_secondDrowsyPoints is not null)
                {
                    EnsureKeysMatchPlayers(_secondDrowsyPoints.Keys, value, nameof(value), keysetName: "SecondDrowsyPoints");
                }
                if (_influences is not null)
                {
                    EnsureKeysMatchPlayers(_influences.Keys, value, nameof(value), keysetName: "Influences");
                }
                if (_bedDamages is not null)
                {
                    EnsureKeysMatchPlayers(_bedDamages.Keys, value, nameof(value), keysetName: "BedDamages");
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
                    EnsureKeysMatchPlayers(copied.Keys, _gameState, nameof(value), keysetName: "FirstDrowsyPoints");
                }
                _firstDrowsyPoints = copied;
            }
        }

        /// <summary>
        /// 各プレイヤーの DDP (Draw Drowsy Point)。プレイヤーごとに隠し情報、初期値 0、
        /// Turn 5/9/13/17/21 開始時に <see cref="DdpPool"/> から抽選した値が累積される
        /// (ADR-0009 §「DP 種別」§「DDP 抽選タイミング」、M2-PR4 で導入)。
        /// キー集合は <see cref="GameState"/>.Players の <see cref="PlayerId"/> 集合と完全一致する必要がある。
        /// 値は負値も許容する(0 floor なし、ADR-0009 「持ち点低い方が勝ち」と整合、DZ-137)。
        /// </summary>
        public IReadOnlyDictionary<PlayerId, int> DrawDrowsyPoints
        {
            get => _drawDrowsyPoints;
            init
            {
                var copied = ValidateAndCopyDp(value, dpName: "DrawDrowsyPoints");
                if (_gameState is not null)
                {
                    EnsureKeysMatchPlayers(copied.Keys, _gameState, nameof(value), keysetName: "DrawDrowsyPoints");
                }
                _drawDrowsyPoints = copied;
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
                    EnsureKeysMatchPlayers(copied.Keys, _gameState, nameof(value), keysetName: "SecondDrowsyPoints");
                }
                _secondDrowsyPoints = copied;
            }
        }

        /// <summary>
        /// 残 DDP プール。プレイヤー間で共有され、Turn 5/9/13/17/21 開始時に先頭から N (= player count) 枚が
        /// 抽選されてプールから除外される(ADR-0009 §「DDP プールの構造」、M2-PR4 で導入)。
        /// </summary>
        public DdpPool DdpPool
        {
            get => _ddpPool;
            init => _ddpPool = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 各プレイヤーが保有する継続影響(<see cref="PlayerInfluence"/>)の list。
        /// キー集合は <see cref="GameState"/>.Players の <see cref="PlayerId"/> 集合と完全一致する必要があり、
        /// 各 list は 0 件(空 list)以上を許容する。M2-PR5 で導入(ADR-0007 §1.5)。
        /// </summary>
        /// <remarks>
        /// list の順序は FIFO Tick 規約(先頭から評価)で意味を持つため、順序保持シーケンス同値で扱う。
        /// 内部表現は <c>PlayerInfluence[]</c>(防御コピー、Pile / DdpPool と同じ array ベース)。
        /// </remarks>
        public IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerInfluence>> Influences
        {
            get => _influences;
            init
            {
                var copied = ValidateAndCopyInfluences(value);
                if (_gameState is not null)
                {
                    EnsureKeysMatchPlayers(copied.Keys, _gameState, nameof(value), keysetName: "Influences");
                }
                _influences = copied;
            }
        }

        /// <summary>ターン内フェーズ(Application 層管理のステートマシン)。</summary>
        public DrowZzzPhaseState PhaseState
        {
            get => _phaseState;
            init => _phaseState = value;
        }

        /// <summary>
        /// ゲーム終了状態(<c>null</c> = 未終了、<see cref="WinnerOutcome"/> = 勝者あり、<see cref="DrawOutcome"/> = 引き分け)。
        /// M3-PR1 で追加(ADR-0010 §3)。
        /// </summary>
        /// <remarks>
        /// 設定経路は 2 つ(ADR-0010 §4):
        /// <list type="bullet">
        /// <item>早期勝利: <see cref="Drowsy.Application.Games.DrowZzz.Effects.EarlyWinTriggerEffect"/> 評価時、
        /// 夜 + 持ち点 ≥ <see cref="DrowZzzVictoryConstants.EarlyWinScoreThreshold"/> で
        /// <see cref="WinnerOutcome"/>(現プレイヤー)を設定</item>
        /// <item>終了時勝利: <c>DrowZzzRule.ApplyEndTurn</c> 内、Round 21 完了検出時に <see cref="TotalPoints"/> 比較で
        /// 低い方を <see cref="WinnerOutcome"/>、等値なら <see cref="DrawOutcome"/></item>
        /// </list>
        /// <c>Outcome != null</c> の session に対する <c>Action</c> はすべて illegal(<c>DrowZzzRule.IsLegalMove</c> で防御、ADR-0010 §6)。
        /// </remarks>
        public GameOutcome Outcome
        {
            get => _outcome;
            init => _outcome = value;
        }

        /// <summary>
        /// ゲームが終了済かどうか(<see cref="Outcome"/> != null の computed プロパティ)。
        /// M3-PR1 で追加(ADR-0010 §3)。
        /// </summary>
        public bool IsTerminated => _outcome is not null;

        /// <summary>
        /// 各プレイヤーのベッド破損率(0〜100%)。M3-PR2 で追加(ADR-0011 §3)。
        /// </summary>
        /// <remarks>
        /// プレイヤーごとに「自分のベッド」を持ち、自ターン開始時に <c>bedDamage / 5</c>(整数除算)の SDP マイナスが入る
        /// (ADR-0011 §3、<see cref="DrowZzzBedConstants.BedDamageRatePerSdp"/>)。
        /// 破損率の増加は <see cref="Effects.DamageBedEffect"/>(M3-PR2 で導入、5 の倍数のみ)で行う。
        /// 修繕は M3-PR3 の <c>AbandonAction(AbandonChoice.RepairBed)</c> で -20%(下限 0%)。
        /// <para>
        /// FDP / DDP / SDP と同パターン: 値は <c>[<see cref="DrowZzzBedConstants.MinBedDamagePercent"/>(0),
        /// <see cref="DrowZzzBedConstants.MaxBedDamagePercent"/>(100)]</c> の範囲、cross-field 検証で
        /// キー集合が <see cref="GameState"/>.Players と一致。
        /// </para>
        /// </remarks>
        public IReadOnlyDictionary<PlayerId, int> BedDamages
        {
            get => _bedDamages;
            init
            {
                var copied = ValidateAndCopyBedDamages(value);
                if (_gameState is not null)
                {
                    EnsureKeysMatchPlayers(copied.Keys, _gameState, nameof(value), keysetName: "BedDamages");
                }
                _bedDamages = copied;
            }
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
        /// FDP / DDP / SDP / Influences のキー集合が gameState.Players の PlayerId 集合と一致しない場合、
        /// または Influences の各 list に null 要素が含まれる場合
        /// </exception>
        public DrowZzzGameSession(
            GameState gameState,
            IReadOnlyDictionary<PlayerId, int> firstDrowsyPoints,
            IReadOnlyDictionary<PlayerId, int> drawDrowsyPoints,
            IReadOnlyDictionary<PlayerId, int> secondDrowsyPoints,
            DdpPool ddpPool,
            IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerInfluence>> influences,
            DrowZzzPhaseState phaseState,
            GameOutcome outcome,
            IReadOnlyDictionary<PlayerId, int> bedDamages)
        {
            // 順序が重要: GameState を先に確定 → 各 DP / Influences / BedDamages の init setter で _gameState を参照して cross-field 検証。
            // (GameState init setter 時点では DP 群 / Influences / BedDamages が null なので cross-field はスキップされる)
            GameState = gameState;
            FirstDrowsyPoints = firstDrowsyPoints;
            DrawDrowsyPoints = drawDrowsyPoints;
            SecondDrowsyPoints = secondDrowsyPoints;
            DdpPool = ddpPool;
            Influences = influences;
            PhaseState = phaseState;
            Outcome = outcome;
            BedDamages = bedDamages;
        }

        /// <summary>
        /// 指定プレイヤーの持ち点(= FDP + DDP + SDP)を計算する(ADR-0009 §「持ち点」、M2-PR4 で 3 項合計に拡張)。
        /// </summary>
        /// <param name="playerId">対象プレイヤー</param>
        /// <returns>FDP + DDP + SDP の合計</returns>
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
            // DDP / SDP は cross-field 検証で FDP とキー集合一致が保証されているため、TryGetValue 失敗は構造的に不可能だが
            // 防御的に再確認(将来 cross-field 検証が緩んだ場合の保険)
            if (!_drawDrowsyPoints.TryGetValue(playerId, out var ddp))
            {
                throw new InvalidOperationException(
                    $"内部不変条件違反: DrawDrowsyPoints に PlayerId {playerId.Value} のキーがありません(cross-field 検証の漏れ)");
            }
            if (!_secondDrowsyPoints.TryGetValue(playerId, out var sdp))
            {
                throw new InvalidOperationException(
                    $"内部不変条件違反: SecondDrowsyPoints に PlayerId {playerId.Value} のキーがありません(cross-field 検証の漏れ)");
            }
            return fdp + ddp + sdp;
        }

        // FDP / DDP / SDP の防御コピー + null 検証(共通化、dpName でエラーメッセージを区別)
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

        // BedDamages の防御コピー + null 検証 + 範囲検証(0〜100%、ADR-0011 §3)
        private static Dictionary<PlayerId, int> ValidateAndCopyBedDamages(
            IReadOnlyDictionary<PlayerId, int> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source), "BedDamages に null は渡せません");
            }
            var buffer = new Dictionary<PlayerId, int>(source.Count);
            foreach (var kv in source)
            {
                if (kv.Value < DrowZzzBedConstants.MinBedDamagePercent
                    || kv.Value > DrowZzzBedConstants.MaxBedDamagePercent)
                {
                    throw new ArgumentException(
                        $"BedDamages[{kv.Key?.Value ?? "<null>"}] は " +
                        $"{DrowZzzBedConstants.MinBedDamagePercent}〜{DrowZzzBedConstants.MaxBedDamagePercent}% の範囲である必要があります " +
                        $"(現在: {kv.Value}、ADR-0011 §3)",
                        nameof(source));
                }
                buffer[kv.Key] = kv.Value;
            }
            return buffer;
        }

        // Influences の防御コピー + null 検証(各 list 内の null 要素も検出)。
        // 内部 value 型は IReadOnlyList<PlayerInfluence> として保持し、PlayerInfluence[] を中身として
        // 防御コピーする(配列だが IReadOnlyList で公開され immutable な扱い、Pile / DdpPool 同パターン)。
        private static Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>> ValidateAndCopyInfluences(
            IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerInfluence>> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source), "Influences に null は渡せません");
            }
            var buffer = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>(source.Count);
            foreach (var kv in source)
            {
                if (kv.Value is null)
                {
                    throw new ArgumentException(
                        $"Influences[{kv.Key?.Value ?? "<null>"}] に null list は渡せません(空 list は許容)",
                        nameof(source));
                }
                var arr = new PlayerInfluence[kv.Value.Count];
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (kv.Value[i] is null)
                    {
                        throw new ArgumentException(
                            $"Influences[{kv.Key?.Value ?? "<null>"}][{i}] に null 要素を含めることはできません",
                            nameof(source));
                    }
                    arr[i] = kv.Value[i];
                }
                buffer[kv.Key] = arr;
            }
            return buffer;
        }

        // 任意のキー集合と GameState.Players の PlayerId 集合の一致検証
        // paramName は呼び出し元 init setter の `nameof(value)` を受け取る
        // keysetName はエラーメッセージで FDP / DDP / SDP / Influences を区別するため
        private static void EnsureKeysMatchPlayers(
            IEnumerable<PlayerId> keys,
            GameState gameState,
            string paramName,
            string keysetName)
        {
            var playerIds = gameState.Players.Select(p => p.Id).ToHashSet();
            var keySet = keys.ToHashSet();
            if (!playerIds.SetEquals(keySet))
            {
                throw new ArgumentException(
                    $"{keysetName} のキー集合が GameState.Players の PlayerId 集合と一致しません " +
                    $"(Players: [{string.Join(", ", playerIds.Select(id => id.Value))}], " +
                    $"{keysetName} keys: [{string.Join(", ", keySet.Select(id => id.Value))}])",
                    paramName);
            }
        }

        /// <summary>
        /// 順序非依存マルチセット同値で比較する。<see cref="_firstDrowsyPoints"/> / <see cref="_drawDrowsyPoints"/> /
        /// <see cref="_secondDrowsyPoints"/> はキー順を問わず各 <see cref="PlayerId"/>-<see cref="int"/> ペアの一致で判定する。
        /// <see cref="_ddpPool"/> は順序付きシーケンス同値(<see cref="DdpPool.Equals(DdpPool)"/>)。
        /// <see cref="_influences"/> はキー順を問わず、各プレイヤーの list は順序保持シーケンス同値で比較する。
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
            if (!_ddpPool.Equals(other._ddpPool))
            {
                return false;
            }
            if (!DpEquals(_firstDrowsyPoints, other._firstDrowsyPoints))
            {
                return false;
            }
            if (!DpEquals(_drawDrowsyPoints, other._drawDrowsyPoints))
            {
                return false;
            }
            if (!DpEquals(_secondDrowsyPoints, other._secondDrowsyPoints))
            {
                return false;
            }
            if (!InfluencesEquals(_influences, other._influences))
            {
                return false;
            }
            // M3-PR1: Outcome の値同値性。null / WinnerOutcome / DrawOutcome の三値比較(record auto-equals 利用)。
            if (!Equals(_outcome, other._outcome))
            {
                return false;
            }
            // M3-PR2: BedDamages の値同値性。FDP / DDP / SDP と同じ順序非依存マルチセット同値で比較。
            if (!DpEquals(_bedDamages, other._bedDamages))
            {
                return false;
            }
            return true;
        }

        // FDP / DDP / SDP の順序非依存マルチセット同値判定(共通化)
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

        // Influences の順序非依存キー比較 + 各 list の順序保持シーケンス同値
        private static bool InfluencesEquals(
            Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>> a,
            Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var bList))
                {
                    return false;
                }
                if (kv.Value.Count != bList.Count)
                {
                    return false;
                }
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (!Equals(kv.Value[i], bList[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 順序非依存ハッシュ。<see cref="_gameState"/> / <see cref="_phaseState"/> / <see cref="_ddpPool"/> および
        /// FDP / DDP / SDP / Influences の各ペアハッシュを XOR 合成する(CardData と同じパターン、ADR-0002)。
        /// 4 つのキー dict は seed 整数(0 / 1 / 2 / 3)を組み合わせて XOR 衝突を回避する。
        /// </summary>
        public override int GetHashCode()
        {
            // M3-PR1: Outcome を合成(null / WinnerOutcome(p) / DrawOutcome の三値、record の GetHashCode を利用)。
            int hash = HashCode.Combine(_gameState, _phaseState, _ddpPool, _outcome);
            foreach (var kv in _firstDrowsyPoints)
            {
                hash ^= HashCode.Combine(kv.Key, kv.Value);
            }
            foreach (var kv in _drawDrowsyPoints)
            {
                // DDP は seed 1 で FDP との XOR 衝突を回避
                hash ^= HashCode.Combine(kv.Key, kv.Value, 1);
            }
            foreach (var kv in _secondDrowsyPoints)
            {
                // SDP は seed 2 で FDP / DDP との XOR 衝突を回避
                hash ^= HashCode.Combine(kv.Key, kv.Value, 2);
            }
            foreach (var kv in _influences)
            {
                // Influences は seed 3 で他 DP との XOR 衝突を回避、list の順序を保持して合成
                int listHash = 0;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    listHash = HashCode.Combine(listHash, kv.Value[i], i);
                }
                hash ^= HashCode.Combine(kv.Key, listHash, 3);
            }
            foreach (var kv in _bedDamages)
            {
                // BedDamages は seed 4 で他 DP / Influences との XOR 衝突を回避
                hash ^= HashCode.Combine(kv.Key, kv.Value, 4);
            }
            return hash;
        }
    }
}
