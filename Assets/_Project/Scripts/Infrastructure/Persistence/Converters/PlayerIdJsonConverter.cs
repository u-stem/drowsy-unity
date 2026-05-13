using System;
using Newtonsoft.Json;
using Drowsy.Domain.Players;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="PlayerId"/> を plain string として serialize / deserialize する converter。
    /// </summary>
    /// <remarks>
    /// <see cref="CardIdJsonConverter"/> と対称設計(<see cref="PlayerId"/> も
    /// <c>private ctor + static <see cref="PlayerId.Of(string)"/></c> パターン)。
    /// </remarks>
    internal sealed class PlayerIdJsonConverter : JsonConverter<PlayerId>
    {
        public override void WriteJson(JsonWriter writer, PlayerId value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteValue(value.Value);
        }

        public override PlayerId ReadJson(
            JsonReader reader, Type objectType, PlayerId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException(
                    $"PlayerId は string として deserialize する必要があります(現在の TokenType: {reader.TokenType})");
            }
            return PlayerId.Of((string)reader.Value);
        }
    }
}
