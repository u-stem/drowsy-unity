using System;
using System.Collections.Generic;

namespace Drowsy.Domain.Cards
{
    /// <summary>
    /// プレイヤーの手札を表す不変値オブジェクト。順序付きユニーク <see cref="CardId"/> 集合で、
    /// 追加順を保つ。同じ <see cref="CardId"/> の重複を許容しない。
    /// </summary>
    /// <remarks>
    /// 内部実装は <see cref="System.Array"/>(<c>CardId[]</c>)。<see cref="Pile"/> と類似のパターンで、
    /// <see cref="System.Collections.Immutable"/> が Unity 6 で利用不可のため独自に防御コピーで不変性を担保する。
    /// 全変更操作(<see cref="Add"/> / <see cref="Remove"/>)は新しい <see cref="Hand"/> を返す純関数。
    /// 等値性は順序付きシーケンス同値で <see cref="Equals(Hand)"/> / <see cref="GetHashCode"/> /
    /// <see cref="op_Equality"/> / <see cref="op_Inequality"/> を override する(設計判断は ADR-0002)。
    /// </remarks>
    public sealed class Hand : IEquatable<Hand>
    {
        private readonly CardId[] _cards;

        /// <summary>カード一覧(読み取り専用、追加順)。</summary>
        public IReadOnlyList<CardId> Cards => _cards;

        /// <summary>枚数。</summary>
        public int Count => _cards.Length;

        /// <summary>空かどうか。</summary>
        public bool IsEmpty => _cards.Length == 0;

        /// <summary>空 Hand のシングルトン。</summary>
        public static Hand Empty { get; } = new Hand(Array.Empty<CardId>());

        /// <summary>
        /// CardId 列から Hand を生成する。null・null 要素・重複は禁止。
        /// </summary>
        /// <param name="cards">カード列(防御コピーされる)</param>
        /// <exception cref="ArgumentNullException">cards が null</exception>
        /// <exception cref="ArgumentException">cards に null 要素または重複 CardId を含む</exception>
        public Hand(IEnumerable<CardId> cards)
        {
            if (cards is null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var seen = new HashSet<CardId>();
            var buffer = new List<CardId>();
            foreach (var card in cards)
            {
                if (card is null)
                {
                    throw new ArgumentException(
                        "Hand の cards に null CardId を含めることはできません",
                        nameof(cards));
                }
                if (!seen.Add(card))
                {
                    throw new ArgumentException(
                        $"Hand の cards に重複 CardId を含めることはできません: {card.Value}",
                        nameof(cards));
                }
                buffer.Add(card);
            }
            _cards = buffer.ToArray();
        }

        // 内部用: 既に所有権を持つ配列を直接ラップする(防御コピーを省略)
        private Hand(CardId[] cards)
        {
            _cards = cards;
        }

        /// <summary>
        /// <paramref name="card"/> を末尾に追加した新 Hand を返す。既存の <see cref="CardId"/> は禁止。
        /// </summary>
        /// <exception cref="ArgumentNullException">card が null</exception>
        /// <exception cref="ArgumentException">card が既に Hand に含まれる</exception>
        public Hand Add(CardId card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }
            if (Contains(card))
            {
                throw new ArgumentException(
                    $"Hand に既に同じ CardId が含まれているため Add できません: {card.Value}",
                    nameof(card));
            }
            var next = new CardId[_cards.Length + 1];
            Array.Copy(_cards, 0, next, 0, _cards.Length);
            next[_cards.Length] = card;
            return new Hand(next);
        }

        /// <summary>
        /// <paramref name="card"/> を取り除いた新 Hand を返す。残りのカードの相対順序は保たれる。
        /// </summary>
        /// <exception cref="ArgumentNullException">card が null</exception>
        /// <exception cref="ArgumentException">card が Hand に含まれない</exception>
        public Hand Remove(CardId card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }
            int index = -1;
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i].Equals(card))
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                throw new ArgumentException(
                    $"Hand に存在しない CardId は Remove できません: {card.Value}",
                    nameof(card));
            }
            var next = new CardId[_cards.Length - 1];
            Array.Copy(_cards, 0, next, 0, index);
            Array.Copy(_cards, index + 1, next, index, _cards.Length - index - 1);
            return new Hand(next);
        }

        /// <summary><paramref name="card"/> が Hand に含まれるかを返す。</summary>
        /// <exception cref="ArgumentNullException">card が null</exception>
        public bool Contains(CardId card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i].Equals(card))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>順序付きシーケンス同値で比較する。</summary>
        public bool Equals(Hand other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (_cards.Length != other._cards.Length)
            {
                return false;
            }
            for (int i = 0; i < _cards.Length; i++)
            {
                if (!_cards[i].Equals(other._cards[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj) => obj is Hand other && Equals(other);

        /// <summary>値同値で == 比較する。両方 null は等価、片方のみ null は非等価。</summary>
        public static bool operator ==(Hand left, Hand right) =>
            left is null ? right is null : left.Equals(right);

        /// <summary>値同値で != 比較する。</summary>
        public static bool operator !=(Hand left, Hand right) => !(left == right);

        /// <summary>順序依存ハッシュ。<see cref="HashCode"/> struct に各 CardId を <c>Add</c> で順次合成する。</summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            for (int i = 0; i < _cards.Length; i++)
            {
                hash.Add(_cards[i]);
            }
            return hash.ToHashCode();
        }
    }
}
