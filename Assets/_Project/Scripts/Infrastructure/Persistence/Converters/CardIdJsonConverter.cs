using System;
using Newtonsoft.Json;
using Drowsy.Domain.Cards;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="CardId"/> を plain string として serialize / deserialize する converter。
    /// </summary>
    /// <remarks>
    /// <see cref="CardId"/> は <c>private ctor + static <see cref="CardId.Of(string)"/></c> パターンの value object で、
    /// Newtonsoft.Json の自動 deserialize は private ctor を呼べない。本 converter で <c>"value"</c> プロパティを介さず
    /// 直接 string として表現する(JSON 容量と可読性が向上、`{"value": "01"}` ではなく `"01"`)。
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
            return CardId.Of((string)reader.Value);
        }
    }
}
