using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Drowsy.Domain.Cards;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="Hand"/> を <see cref="CardId"/> 配列として serialize / deserialize する converter。
    /// </summary>
    /// <remarks>
    /// <see cref="PileJsonConverter"/> と対称設計(<see cref="Hand"/> も <c>sealed class</c> +
    /// <c>Hand(IEnumerable&lt;CardId&gt;)</c> ctor)。<see cref="Hand"/> は重複禁止 + 順序保持の配列としての性質を持つが、
    /// 等価性 / serialize 順序は <see cref="Hand.Cards"/>(防御コピー後の配列)と一致するため、
    /// <c>"hand": ["01", "02"]</c> 形式で round-trip 可能(重複検出は ctor 側 `Hand(IEnumerable&lt;CardId&gt;)` が担う)。
    /// </remarks>
    internal sealed class HandJsonConverter : JsonConverter<Hand>
    {
        public override void WriteJson(JsonWriter writer, Hand value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteStartArray();
            for (int i = 0; i < value.Cards.Count; i++)
            {
                serializer.Serialize(writer, value.Cards[i]);
            }
            writer.WriteEndArray();
        }

        public override Hand ReadJson(
            JsonReader reader, Type objectType, Hand existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException(
                    $"Hand は CardId 配列として deserialize する必要があります(現在の TokenType: {reader.TokenType})");
            }

            var cards = serializer.Deserialize<List<CardId>>(reader);
            return new Hand(cards);
        }
    }
}
