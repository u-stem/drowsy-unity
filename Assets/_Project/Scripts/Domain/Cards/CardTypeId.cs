using System;

namespace Drowsy.Domain.Cards
{
    /// <summary>
    /// カードの種別(catalog の lookup key)を表す不変値オブジェクト。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本型は <see cref="ICardCatalog{TEffect}"/> 系の lookup key として使用する種別 ID であり、
    /// 「同じ種別のカードが複数枚 deck / hand 内に存在する」ことを想定する。各カードのインスタンス側 ID は
    /// <see cref="CardId"/> が表現する(複合 <c>(CardTypeId TypeId, int Instance)</c> の TypeId 部)。
    /// </para>
    /// <para>
    /// 旧 Phase 1 設計では <see cref="CardId"/> が「catalog の lookup key」と「Hand 内 unique 識別子」を
    /// 兼ねており、業界デファクト(Card Type / Card Instance 分離)と乖離した二重意味を持っていた。
    /// 本型を新設して種別 ID を instance 側から分離する。
    /// </para>
    /// </remarks>
    public sealed record CardTypeId
    {
        /// <summary>種別 ID の文字列値。</summary>
        public string Value { get; }

        private CardTypeId(string value)
        {
            Value = value;
        }

        /// <summary>
        /// CardTypeId を生成する。null・空文字列・空白のみ、または <c>'#'</c> を含む文字列は <see cref="ArgumentException"/>。
        /// </summary>
        /// <param name="value">識別子文字列(非空白、<c>'#'</c> を含まない)</param>
        /// <exception cref="ArgumentException">
        /// value が null・空・空白のみの場合、または <c>'#'</c> を含む場合(<c>'#'</c> は <see cref="CardId.Value"/>
        /// の <c>"<typeId>#<instance>"</c> 区切り文字として予約済)
        /// </exception>
        public static CardTypeId Of(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "CardTypeId は null・空・空白のみにできません",
                    nameof(value));
            }
            if (value.IndexOf('#') >= 0)
            {
                // CardId.Value("<typeId>#<instance>")の区切り文字 '#' は予約済。
                // CardTypeId に含めると CardIdJsonConverter の split で曖昧になるため runtime 強制で弾く。
                throw new ArgumentException(
                    "CardTypeId は '#' を含めることができません(ADR-0018 §8 で CardId.Value の区切り文字として予約)",
                    nameof(value));
            }
            return new CardTypeId(value);
        }

        public override string ToString() => Value;
    }
}
