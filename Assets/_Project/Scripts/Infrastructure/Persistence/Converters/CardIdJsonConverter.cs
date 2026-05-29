using System;
using Drowsy.Domain.Cards;
using Newtonsoft.Json;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="CardId"/> を plain string(<see cref="CardId.Value"/> 形式 <c>"$"{TypeId.Value}#{Instance}""</c>)として
    /// serialize / deserialize する converter。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CardId"/> は <c>private ctor + static <see cref="CardId.Of(CardTypeId, int)"/></c> パターンの value object で、
    /// Newtonsoft.Json の自動 deserialize は private ctor を呼べない。本 converter で <c>"typeId"</c> / <c>"instance"</c> プロパティを
    /// 介さず直接 string として表現する(JSON 容量と可読性が向上、`{"typeId":"01","instance":0}` ではなく `"01#0"`)。
    /// </para>
    /// <para>
    /// <b>schema</b>:string は <c>"<typeId>#<instance>"</c> 形式。例:<c>"dream#0"</c> / <c>"sheep#3"</c>。
    /// <c>#</c> を最後に出現する位置で split し、左を <see cref="CardTypeId.Of(string)"/>、右を <see cref="int.Parse(string)"/>
    /// で復元する(<see cref="CardTypeId"/> 内に <c>#</c> を含まない前提)。schema 違反は
    /// <see cref="JsonSerializationException"/>。
    /// </para>
    /// </remarks>
    internal sealed class CardIdJsonConverter : JsonConverter<CardId>
    {
        public override void WriteJson(JsonWriter writer, CardId value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteValue(value.Value);
        }

        public override CardId ReadJson(
            JsonReader reader, Type objectType, CardId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException(
                    $"CardId は string として deserialize する必要があります(現在の TokenType: {reader.TokenType})");
            }
            var raw = (string)reader.Value;
            return ParseCardIdString(raw);
        }

        // "<typeId>#<instance>" の split。schema 違反は JsonSerializationException(deserialize 文脈)。
        // CardTypeId は '#' を含まないため、LastIndexOf('#') で安全に split できる。
        private static CardId ParseCardIdString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new JsonSerializationException(
                    "CardId 文字列は null・空・空白のみにできません");
            }
            var sepIndex = raw.LastIndexOf('#');
            if (sepIndex < 0)
            {
                throw new JsonSerializationException(
                    $"CardId 文字列に instance separator '#' が含まれていません(ADR-0018 schema 違反): '{raw}'");
            }
            var typeIdPart = raw.Substring(0, sepIndex);
            var instancePart = raw.Substring(sepIndex + 1);
            if (!int.TryParse(instancePart, out var instance))
            {
                throw new JsonSerializationException(
                    $"CardId の instance 部分が int として parse できません: '{instancePart}'(raw: '{raw}')");
            }
            try
            {
                return CardId.Of(CardTypeId.Of(typeIdPart), instance);
            }
            catch (ArgumentException ex)
            {
                throw new JsonSerializationException(
                    $"CardId の復元に失敗しました(raw: '{raw}'): {ex.Message}",
                    ex);
            }
        }
    }
}
