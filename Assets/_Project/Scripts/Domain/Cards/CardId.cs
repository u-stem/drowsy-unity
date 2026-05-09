using System;

namespace Drowsy.Domain.Cards
{
    /// <summary>
    /// カードの一意識別子。string の値同等性を持つ不変値オブジェクト。
    /// </summary>
    public sealed record CardId
    {
        /// <summary>識別子の文字列値。</summary>
        public string Value { get; }

        private CardId(string value)
        {
            Value = value;
        }

        /// <summary>
        /// CardId を生成する。null・空文字列・空白のみは <see cref="ArgumentException"/>。
        /// </summary>
        /// <param name="value">識別子文字列(非空白)</param>
        /// <exception cref="ArgumentException">value が null・空・空白のみの場合</exception>
        public static CardId Of(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "CardId は null・空・空白のみにできません",
                    nameof(value));
            }
            return new CardId(value);
        }

        public override string ToString() => Value;
    }
}
