using System;
using Drowsy.Domain.Cards;

namespace Drowsy.Domain.Players
{
    /// <summary>
    /// 1 プレイヤーの状態(識別子と手札)を表す不変値オブジェクト。
    /// </summary>
    /// <remarks>
    /// <c>record class</c> として実装し、auto-equals が <see cref="Id"/>(<see cref="PlayerId"/>:record)と
    /// <see cref="Hand"/>(<see cref="Cards.Hand"/>:<see cref="IEquatable{T}"/> 実装)それぞれの値同値性を呼ぶことで
    /// 全体として値同値となる。<see cref="CardData"/> / <see cref="Cards.Hand"/> / <see cref="Cards.Pile"/> が内部辞書 / 配列のため
    /// <c>sealed class + IEquatable</c> を要したのとは異なり、<see cref="PlayerState"/> は内部に値同値が壊れる
    /// フィールドを持たないため <c>record</c> で安全。
    /// 状態更新は <c>with</c> 式(例: <c>state with { Hand = newHand }</c>)で行う。
    /// <c>init</c> setter にも null 検証を入れることで <c>with</c> 式での null 渡しも防御する。
    /// </remarks>
    public sealed record PlayerState
    {
        private readonly PlayerId _id;
        private readonly Hand _hand;

        /// <summary>プレイヤー識別子。</summary>
        public PlayerId Id
        {
            get => _id;
            init => _id = value ?? throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>プレイヤーの手札。</summary>
        public Hand Hand
        {
            get => _hand;
            init => _hand = value ?? throw new ArgumentNullException(nameof(Hand));
        }

        /// <summary>
        /// PlayerState を生成する。<paramref name="id"/> または <paramref name="hand"/> が null の場合は <see cref="ArgumentNullException"/>。
        /// </summary>
        /// <exception cref="ArgumentNullException">id または hand が null の場合</exception>
        public PlayerState(PlayerId id, Hand hand)
        {
            // init setter 経由で null チェックが効く
            Id = id;
            Hand = hand;
        }
    }
}
