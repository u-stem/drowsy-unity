using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Drowsy.Domain.Random;

namespace Drowsy.Domain.Cards
{
    /// <summary>
    /// 順序ありカード列(山札・捨て札・場の基底)。Immutable で全操作が新インスタンスを返す。
    /// 内部実装は <see cref="ImmutableArray{T}"/>(小規模 + ランダムアクセス重視)。
    /// </summary>
    public sealed class Pile
    {
        private readonly ImmutableArray<CardId> _cards;

        /// <summary>カード一覧(読み取り専用)。先頭が Top、末尾が Bottom。</summary>
        public IReadOnlyList<CardId> Cards => _cards;

        /// <summary>カード枚数。</summary>
        public int Count => _cards.Length;

        /// <summary>空かどうか。</summary>
        public bool IsEmpty => _cards.Length == 0;

        /// <summary>空 Pile のシングルトン。</summary>
        public static Pile Empty { get; } = new Pile(Enumerable.Empty<CardId>());

        /// <summary>カード列から Pile を生成する。</summary>
        /// <exception cref="ArgumentNullException">cards が null</exception>
        public Pile(IEnumerable<CardId> cards)
        {
            if (cards is null)
            {
                throw new ArgumentNullException(nameof(cards));
            }
            _cards = cards.ToImmutableArray();
        }

        private Pile(ImmutableArray<CardId> cards)
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
            return new Pile(_cards.Insert(0, card));
        }

        /// <summary>末尾(Bottom)にカードを追加した新 Pile を返す。</summary>
        /// <exception cref="ArgumentNullException">card が null</exception>
        public Pile AddBottom(CardId card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }
            return new Pile(_cards.Add(card));
        }

        /// <summary>先頭から 1 枚引く。引いたカードと残り Pile のタプルを返す。</summary>
        /// <exception cref="InvalidOperationException">空の Pile</exception>
        public (CardId Drawn, Pile Remaining) Draw()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("空の Pile からは Draw できません");
            }
            return (_cards[0], new Pile(_cards.RemoveAt(0)));
        }

        /// <summary>Fisher-Yates シャッフルした新 Pile を返す。rng が決定的なら結果も決定的。</summary>
        /// <exception cref="ArgumentNullException">rng が null</exception>
        public Pile Shuffle(IRandomSource rng)
        {
            if (rng is null)
            {
                throw new ArgumentNullException(nameof(rng));
            }
            // ImmutableArray は変更不可のため、可変配列にコピーしてから Fisher-Yates を実施
            var array = _cards.ToArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
            return new Pile(ImmutableArray.Create(array));
        }
    }
}
