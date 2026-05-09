using System;
using System.Collections.Generic;
using System.Linq;
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
    /// </remarks>
    public sealed class Pile
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
        public Pile(IEnumerable<CardId> cards)
        {
            if (cards is null)
            {
                throw new ArgumentNullException(nameof(cards));
            }
            _cards = cards.ToArray();
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
    }
}
