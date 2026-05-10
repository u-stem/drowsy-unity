using System;
using System.Collections.Generic;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Players;

namespace Drowsy.Domain.Game
{
    /// <summary>
    /// ゲーム全体の状態を表す不変ルート集約。
    /// Phase 1 では <see cref="Players"/> / <see cref="Deck"/> / <see cref="Discard"/> / <see cref="Field"/> の 4 フィールドを保持する
    /// (<c>Turn</c> は PR-5 で <c>TurnState</c> と一緒に追加予定)。
    /// </summary>
    /// <remarks>
    /// <c>record class</c> として実装し、<c>init</c> setter + バッキングフィールド + <c>value ?? throw</c> で
    /// コンストラクタ + <c>with</c> 式の両経路で null 防御が走る(PR-3 PlayerState と同パターン、ADR-0004 polyfill 前提)。
    /// <see cref="Equals(GameState)"/> / <see cref="GetHashCode"/> は record の auto-generated を上書きし、
    /// <see cref="Players"/> を順序付きシーケンス同値で比較する(record auto-equals は <see cref="IReadOnlyList{T}"/> を
    /// 参照同値で比較するため値同値が壊れる)。<c>==</c> / <c>!=</c> / <c>Equals(object)</c> は record の標準実装が
    /// <c>Equals(GameState)</c> を呼ぶため自動的に正しく動く。
    /// </remarks>
    public sealed record GameState
    {
        private readonly PlayerState[] _players;
        private readonly Pile _deck;
        private readonly Pile _discard;
        private readonly Pile _field;

        /// <summary>プレイヤー一覧(順序付き、PlayerId 重複拒否、防御コピー済)。</summary>
        public IReadOnlyList<PlayerState> Players
        {
            get => _players;
            init => _players = ValidateAndCopyPlayers(value);
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

        /// <summary>
        /// GameState を生成する。各フィールド null は <see cref="ArgumentNullException"/>、
        /// players の null 要素 / 重複 PlayerId は <see cref="ArgumentException"/>。
        /// </summary>
        /// <exception cref="ArgumentNullException">いずれかの引数が null の場合</exception>
        /// <exception cref="ArgumentException">players に null 要素または重複 PlayerId が含まれる場合</exception>
        public GameState(IReadOnlyList<PlayerState> players, Pile deck, Pile discard, Pile field)
        {
            // init setter 経由で全フィールドの null 検証 + Players の重複検証 + 防御コピーが走る
            Players = players;
            Deck = deck;
            Discard = discard;
            Field = field;
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

        /// <summary>順序付きシーケンス同値 + 4 フィールド値同値で比較する。</summary>
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
                && _field.Equals(other._field);
        }

        /// <summary>順序依存ハッシュ。各 Players + Deck + Discard + Field を <see cref="HashCode"/> struct に Add で合成する。</summary>
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
            return hash.ToHashCode();
        }
    }
}
