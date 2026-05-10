using System;

namespace Drowsy.Domain.Players
{
    /// <summary>
    /// プレイヤーの一意識別子。string の値同等性を持つ不変値オブジェクト(<see cref="Drowsy.Domain.Cards.CardId"/> と対称)。
    /// </summary>
    public sealed record PlayerId
    {
        /// <summary>識別子の文字列値。</summary>
        public string Value { get; }

        private PlayerId(string value)
        {
            Value = value;
        }

        /// <summary>
        /// PlayerId を生成する。null・空文字列・空白のみは <see cref="ArgumentException"/>。
        /// </summary>
        /// <param name="value">識別子文字列(非空白)</param>
        /// <exception cref="ArgumentException">value が null・空・空白のみの場合</exception>
        public static PlayerId Of(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "PlayerId は null・空・空白のみにできません",
                    nameof(value));
            }
            return new PlayerId(value);
        }

        public override string ToString() => Value;
    }
}
