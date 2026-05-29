using System;
using System.Collections.Generic;
using Drowsy.Domain.Random;

namespace Drowsy.Domain.Cards
{
    /// <summary>
    /// 順序ありカード列(山札・捨て札・場の基底)。Immutable で全操作が新インスタンスを返す。
    /// </summary>
    /// <remarks>
    /// 内部実装は <see cref="System.Array"/>(<c>CardId[]</c>)。
    /// 当初 <c>ImmutableArray&lt;T&gt;</c> を採用予定だったが、Unity 6 では
    /// <c>System.Collections.Immutable</c> が internal アクセシビリティのため利用不可。
    /// 代替として配列を private で保持し、<see cref="Cards"/> プロパティ経由で
    /// <see cref="IReadOnlyList{T}"/> として読み取り専用公開する。
    /// 全変更操作は新しい配列を割り当てて新 Pile を返す純関数として実装する。
    ///
    /// 等値性は順序付きシーケンス同値で <see cref="Equals(Pile)"/> / <see cref="GetHashCode"/> /
    /// <see cref="op_Equality"/> / <see cref="op_Inequality"/> を override する(<see cref="Hand"/> と同じパターン)。
    /// </remarks>
    public sealed class Pile : IEquatable<Pile>
    {
        private readonly CardId[] _cards;

        /// <summary>カード一覧(読み取り専用)。先頭が Top、末尾が Bottom。</summary>
        public IReadOnlyList<CardId> Cards => _cards;

        /// <summary>カード枚数。</summary>
        public int Count => _cards.Length;

        /// <summary>空かどうか。</summary>
        public bool IsEmpty => _cards.Length == 0;

        /// <summary>空 Pile のシングルトン。</summary>
        public static Pile Empty { get; } = new Pile(Array.Empty<CardId>());

        /// <summary>カード列から Pile を生成する。</summary>
        /// <exception cref="ArgumentNullException">cards が null</exception>
        /// <exception cref="ArgumentException">cards に null 要素が含まれる</exception>
        public Pile(IEnumerable<CardId> cards)
        {
            if (cards is null)
            {
                throw new ArgumentNullException(nameof(cards));
            }
            // null 要素は混入箇所と例外箇所が乖離してデバッグ困難になるため、構築時に検出する
            // (Hand と同じパターン、Domain 集合型の null 防御を Hand / Pile 間で対称化する)。
            var buffer = new List<CardId>();
            foreach (var card in cards)
            {
                if (card is null)
                {
                    throw new ArgumentException(
                        "Pile の cards に null CardId を含めることはできません",
                        nameof(cards));
                }
                buffer.Add(card);
            }
            _cards = buffer.ToArray();
        }

        // 内部用: 既に所有権を持つ配列を直接ラップする(防御コピーを省略)
        private Pile(CardId[] cards)
        {
            _cards = cards;
        }

        /// <summary>先頭(Top、index 0)にカードを追加した新 Pile を返す。</summary>
        /// <exception cref="ArgumentNullException">card が null</exception>
        public Pile AddTop(CardId card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }
            var next = new CardId[_cards.Length + 1];
            next[0] = card;
            Array.Copy(_cards, 0, next, 1, _cards.Length);
            return new Pile(next);
        }

        /// <summary>末尾(Bottom)にカードを追加した新 Pile を返す。</summary>
        /// <exception cref="ArgumentNullException">card が null</exception>
        public Pile AddBottom(CardId card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }
            var next = new CardId[_cards.Length + 1];
            Array.Copy(_cards, 0, next, 0, _cards.Length);
            next[_cards.Length] = card;
            return new Pile(next);
        }

        /// <summary>先頭から 1 枚引く。引いたカードと残り Pile のタプルを返す。</summary>
        /// <exception cref="InvalidOperationException">空の Pile</exception>
        public (CardId Drawn, Pile Remaining) Draw()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("空の Pile からは Draw できません");
            }
            var drawn = _cards[0];
            var remaining = new CardId[_cards.Length - 1];
            Array.Copy(_cards, 1, remaining, 0, remaining.Length);
            return (drawn, new Pile(remaining));
        }

        /// <summary>Fisher-Yates シャッフルした新 Pile を返す。rng が決定的なら結果も決定的。</summary>
        /// <exception cref="ArgumentNullException">rng が null</exception>
        public Pile Shuffle(IRandomSource rng)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }
            var array = (CardId[])_cards.Clone();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
            return new Pile(array);
        }

        /// <summary>順序付きシーケンス同値で比較する。</summary>
        public bool Equals(Pile other)
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

        public override bool Equals(object obj) => obj is Pile other && Equals(other);

        /// <summary>値同値で == 比較する。両方 null は等価、片方のみ null は非等価。</summary>
        public static bool operator ==(Pile left, Pile right) =>
            left is null ? right is null : left.Equals(right);

        /// <summary>値同値で != 比較する。</summary>
        public static bool operator !=(Pile left, Pile right) => !(left == right);

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
