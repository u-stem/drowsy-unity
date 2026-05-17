using System;
using System.Collections.Generic;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Players;

// `[assembly: InternalsVisibleTo]` は Properties/AssemblyInfo.cs に集約済(2026-05-17 #5 post-Phase2 残対応)。

namespace Drowsy.Domain.Game
{
    /// <summary>
    /// ゲーム全体の状態を表す不変ルート集約。
    /// Phase 1 完結時点で <see cref="Players"/> / <see cref="Deck"/> / <see cref="Discard"/> /
    /// <see cref="Field"/> / <see cref="Turn"/> の 5 フィールドを保持する。
    /// </summary>
    /// <remarks>
    /// <c>record class</c> として実装し、<c>init</c> setter + バッキングフィールド + <c>value ?? throw</c> で
    /// コンストラクタ + <c>with</c> 式の両経路で null 防御が走る(PR-3 PlayerState と同パターン、ADR-0004 polyfill 前提)。
    /// <see cref="Equals(GameState)"/> / <see cref="GetHashCode"/> は record の auto-generated を上書きし、
    /// <see cref="Players"/> を順序付きシーケンス同値で比較する(record auto-equals は <see cref="IReadOnlyList{T}"/> を
    /// 参照同値で比較するため値同値が壊れる)。<c>==</c> / <c>!=</c> / <c>Equals(object)</c> は record の標準実装が
    /// <c>Equals(GameState)</c> を呼ぶため自動的に正しく動く。
    /// <see cref="Turn"/> は PR-5 で追加され、<see cref="TurnState.CurrentPlayerIndex"/> が
    /// <see cref="Players"/> の範囲内(<c>0 &lt;= index &lt; Count</c>)であることをコンストラクタで検証する(GS-022)。
    /// <para>
    /// <b>呼び出し規約 — `with` 同時更新の禁止</b>:
    /// <c>Players</c> と <c>Turn</c> の init setter は、それぞれ「現在のもう片方の値」を読んで GS-022(範囲整合)
    /// を検証する。C# の <c>with</c> 式が複数 init setter を呼ぶ順序はコンパイラ依存で保証されないため、
    /// <c>with { Players = newPlayers, Turn = newTurn }</c> のような **同時更新は使わない**。
    /// 順序依存で「旧 Turn × 新 Players」または「旧 Players × 新 Turn」で検証が走り、本来弾くべき不整合を
    /// 通過させる可能性がある。代わりに 2 段 <c>with</c>(<c>state with { Players = ... } with { Turn = ... }</c>)
    /// またはコンストラクタ経由(<c>new GameState(...)</c>)で、Players → Turn の順に確定する。
    /// </para>
    /// </remarks>
    public sealed record GameState
    {
        private readonly PlayerState[] _players;
        private readonly Pile _deck;
        private readonly Pile _discard;
        private readonly Pile _field;
        private readonly TurnState _turn;

        /// <summary>プレイヤー一覧(順序付き、PlayerId 重複拒否、防御コピー済)。</summary>
        public IReadOnlyList<PlayerState> Players
        {
            get => _players;
            init
            {
                var newPlayers = ValidateAndCopyPlayers(value);
                // GS-022: Players を縮小して既存 Turn が範囲外になる経路を防ぐ
                // (with { Players = ... } 経由でも検証が走るよう init setter 内で完結させる)
                if (_turn is not null && _turn.CurrentPlayerIndex >= newPlayers.Length)
                {
                    throw new ArgumentException(
                        $"Players ({newPlayers.Length} 人) が現在の Turn.CurrentPlayerIndex ({_turn.CurrentPlayerIndex}) の範囲外になります",
                        nameof(value));
                }
                _players = newPlayers;
            }
        }

        /// <summary>山札。</summary>
        public Pile Deck
        {
            get => _deck;
            init => _deck = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>捨て札。</summary>
        public Pile Discard
        {
            get => _discard;
            init => _discard = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>場(場を持たないゲームでは <see cref="Pile.Empty"/> を渡す)。</summary>
        public Pile Field
        {
            get => _field;
            init => _field = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>ターン進行状態。</summary>
        public TurnState Turn
        {
            get => _turn;
            init
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                // GS-022: with { Turn = ... } 経由でも Players 範囲外を防ぐ。
                // _players は MemberwiseClone (with 経由) または直前の Players init setter (コンストラクタ経由) で確定済み。
                if (_players is not null && value.CurrentPlayerIndex >= _players.Length)
                {
                    throw new ArgumentException(
                        $"Turn.CurrentPlayerIndex ({value.CurrentPlayerIndex}) が Players の範囲外です(Players.Count = {_players.Length})",
                        nameof(value));
                }
                _turn = value;
            }
        }

        /// <summary>
        /// GameState を生成する。各フィールド null は <see cref="ArgumentNullException"/>、
        /// players の null 要素 / 重複 PlayerId / Turn と Players の不整合は <see cref="ArgumentException"/>。
        /// </summary>
        /// <exception cref="ArgumentNullException">いずれかの引数が null の場合</exception>
        /// <exception cref="ArgumentException">players に null 要素または重複 PlayerId が含まれる場合、または turn の CurrentPlayerIndex が Players の範囲外の場合</exception>
        public GameState(IReadOnlyList<PlayerState> players, Pile deck, Pile discard, Pile field, TurnState turn)
        {
            // init setter 経由で全フィールドの null 検証 + Players の重複検証 + 防御コピー +
            // Turn と Players の整合性検証(GS-022)が走る。
            // 順序が重要: Players を先に確定 → Turn の init setter で _players を参照して範囲検証。
            Players = players;
            Deck = deck;
            Discard = discard;
            Field = field;
            Turn = turn;
        }

        /// <summary>
        /// 既に検証済みの <paramref name="alreadyValidatedPlayers"/> 配列を防御コピー / 重複検証なしで
        /// 受け取り、現 <see cref="GameState"/> から Players のみ差し替えた新インスタンスを返す。
        /// </summary>
        /// <remarks>
        /// <b>post-Phase2 アルゴリズム最適化レビュー Top-2 反映</b>:
        /// 旧経路 <c>gameState with { Players = newPlayers }</c> は init setter が
        /// <see cref="ValidateAndCopyPlayers"/> を再走させ、呼び出し元が既に確保した <c>PlayerState[N]</c>
        /// と同サイズの配列を **もう一度** alloc + <see cref="HashSet{T}"/> alloc + 重複検証を行っていた。
        /// <see cref="DrowZzzRule"/> の Apply 系で「現プレイヤー 1 人だけ差し替える」パターンが頻出
        /// (1 Apply あたり 1〜2 回)で、各呼び出しが <c>PlayerState[N]</c> + <c>HashSet&lt;PlayerId&gt;</c>
        /// の 2 alloc を発生させていた。
        /// <para>
        /// 本メソッドは呼び出し元の責務として「<paramref name="alreadyValidatedPlayers"/> は null でない、
        /// null 要素を含まない、重複 PlayerId を含まない」ことを保証する前提で防御コピーをスキップする。
        /// Turn と Players の整合性(GS-022、<c>CurrentPlayerIndex &lt; Players.Length</c>)のみ維持する
        /// (Players 配列の長さが変わるユースケースは現状なく、現プレイヤー差し替えのみ)。
        /// </para>
        /// <para>
        /// internal 公開で <see cref="DrowZzzRule"/> 等 Drowsy.Application からのみ利用可能
        /// (<c>AssemblyInfo.cs</c> の <c>InternalsVisibleTo</c>)。外部からは引き続き
        /// <c>with { Players = ... }</c> の安全経路を使う。
        /// </para>
        /// </remarks>
        /// <param name="alreadyValidatedPlayers">既に検証済みの <see cref="PlayerState"/> 配列(non-null、null 要素なし、重複 PlayerId なし)</param>
        /// <exception cref="ArgumentNullException"><paramref name="alreadyValidatedPlayers"/> が null</exception>
        /// <exception cref="ArgumentException">現 <see cref="Turn"/> の <see cref="TurnState.CurrentPlayerIndex"/> が <paramref name="alreadyValidatedPlayers"/> の範囲外になる場合(GS-022)</exception>
        internal GameState WithPlayersUnchecked(PlayerState[] alreadyValidatedPlayers)
            => WithPlayersAndPilesUnchecked(alreadyValidatedPlayers);

        /// <summary>
        /// 既に検証済みの <paramref name="alreadyValidatedPlayers"/> 配列と、任意の <see cref="Pile"/> 差し替えを
        /// 1 段で適用する。<paramref name="deck"/> / <paramref name="discard"/> / <paramref name="field"/> が
        /// <c>null</c> の場合は現 <see cref="GameState"/> の対応フィールドを継承する。
        /// </summary>
        /// <remarks>
        /// <b>post-Phase2 アルゴリズム最適化レビュー Top-2 残対応(複合 with)反映</b>:
        /// <see cref="DrowZzzRule"/> の Apply 系には「Players + Deck」「Players + Field」「Players + Discard」
        /// 「Players + Field + Discard」などの複合更新が 5-6 箇所あり、それぞれ <c>gameState with { Players = ..., Discard = ... }</c>
        /// の形で <see cref="ValidateAndCopyPlayers"/> の二重 alloc を引き起こしていた。本 API は全パターンを
        /// 1 つの internal ctor 呼び出しに集約し、<see cref="WithPlayersUnchecked"/> 経由の Players 単独差し替えと
        /// 同じく <see cref="PlayerState"/>[] / <see cref="HashSet{T}"/> alloc を排除する。
        /// <para>
        /// 呼び出し側は名前付き引数で意図を明示する(例: <c>WithPlayersAndPilesUnchecked(newPlayers, discard: newDiscard)</c>)。
        /// <see cref="Pile"/> 引数の <c>null</c> sentinel で「変更なし(既存値継承)」を表現する設計上、ADR-0015 で
        /// NRT 非採用のため CS8625 等の警告は出ない。
        /// </para>
        /// </remarks>
        /// <param name="alreadyValidatedPlayers">既に検証済みの <see cref="PlayerState"/> 配列</param>
        /// <param name="deck">新 <see cref="Deck"/>(<c>null</c> なら現値継承)</param>
        /// <param name="discard">新 <see cref="Discard"/>(<c>null</c> なら現値継承)</param>
        /// <param name="field">新 <see cref="Field"/>(<c>null</c> なら現値継承)</param>
        /// <exception cref="ArgumentNullException"><paramref name="alreadyValidatedPlayers"/> が null</exception>
        /// <exception cref="ArgumentException">現 <see cref="Turn"/> の <see cref="TurnState.CurrentPlayerIndex"/> が範囲外になる場合(GS-022)</exception>
        internal GameState WithPlayersAndPilesUnchecked(
            PlayerState[] alreadyValidatedPlayers,
            Pile deck = null,
            Pile discard = null,
            Pile field = null)
        {
            if (alreadyValidatedPlayers is null)
            {
                throw new ArgumentNullException(nameof(alreadyValidatedPlayers));
            }
            if (_turn.CurrentPlayerIndex >= alreadyValidatedPlayers.Length)
            {
                throw new ArgumentException(
                    $"alreadyValidatedPlayers ({alreadyValidatedPlayers.Length} 人) が現在の Turn.CurrentPlayerIndex ({_turn.CurrentPlayerIndex}) の範囲外になります",
                    nameof(alreadyValidatedPlayers));
            }
            // null sentinel で「変更なし」を表現:non-null なら新値、null なら既存バッキングフィールドを継承。
            // internal unchecked ctor 経由で全フィールド直接代入し ValidateAndCopyPlayers / 各 Pile init setter を回避。
            return new GameState(
                alreadyValidatedPlayers,
                deck ?? _deck,
                discard ?? _discard,
                field ?? _field,
                _turn,
                uncheckedMarker: default);
        }

        // internal unchecked constructor for WithPlayersUnchecked.
        // uncheckedMarker は public 5 引数 ctor とのオーバーロード曖昧性を排除するための型タグ(値は使用しない)。
        // 呼び出し元(WithPlayersUnchecked のみ)が事前検証を保証する責務を持つ。
        // アクセシビリティは ctor と同じ internal に揃える(CS0051 回避)。
        internal readonly struct UncheckedCtorMarker { }
        internal GameState(PlayerState[] alreadyValidatedPlayers, Pile deck, Pile discard, Pile field, TurnState turn, UncheckedCtorMarker uncheckedMarker)
        {
            _ = uncheckedMarker; // suppress unused warning
            _players = alreadyValidatedPlayers;
            _deck = deck;
            _discard = discard;
            _field = field;
            _turn = turn;
        }

        // 防御コピー + null 要素 / 重複 PlayerId の検証
        private static PlayerState[] ValidateAndCopyPlayers(IReadOnlyList<PlayerState> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var seen = new HashSet<PlayerId>();
            var buffer = new PlayerState[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                var ps = source[i];
                if (ps is null)
                {
                    throw new ArgumentException(
                        "GameState の players に null 要素を含めることはできません",
                        nameof(source));
                }
                if (!seen.Add(ps.Id))
                {
                    throw new ArgumentException(
                        $"GameState の players に重複 PlayerId を含めることはできません: {ps.Id.Value}",
                        nameof(source));
                }
                buffer[i] = ps;
            }
            return buffer;
        }

        /// <summary>順序付きシーケンス同値 + 5 フィールド値同値で比較する。</summary>
        public bool Equals(GameState other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (_players.Length != other._players.Length)
            {
                return false;
            }
            for (int i = 0; i < _players.Length; i++)
            {
                if (!_players[i].Equals(other._players[i]))
                {
                    return false;
                }
            }
            return _deck.Equals(other._deck)
                && _discard.Equals(other._discard)
                && _field.Equals(other._field)
                && _turn.Equals(other._turn);
        }

        /// <summary>順序依存ハッシュ。各 Players + Deck + Discard + Field + Turn を <see cref="HashCode"/> struct に Add で合成する。</summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            for (int i = 0; i < _players.Length; i++)
            {
                hash.Add(_players[i]);
            }
            hash.Add(_deck);
            hash.Add(_discard);
            hash.Add(_field);
            hash.Add(_turn);
            return hash.ToHashCode();
        }
    }
}
