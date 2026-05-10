using System;
using System.Collections.Generic;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Players;

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
