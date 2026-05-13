using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Drowsy.Domain.Cards;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="Pile"/> を <see cref="CardId"/> 配列として serialize / deserialize する converter。
    /// </summary>
    /// <remarks>
    /// <see cref="Pile"/> は <c>sealed class : IEquatable&lt;Pile&gt;</c> で <c>Pile(IEnumerable&lt;CardId&gt;)</c> ctor を持つ。
    /// Newtonsoft.Json の自動 deserialize は <see cref="Pile.Cards"/> プロパティを介して試みるが、<see cref="Pile"/> は
    /// readonly プロパティのみで setter を持たないため失敗する。本 converter で配列直接表現に変換し、
    /// <c>"deck": ["01", "02", "03"]</c> のような可読性の高い JSON を実現する。
    /// </remarks>
    internal sealed class PileJsonConverter : JsonConverter<Pile>
    {
        public override void WriteJson(JsonWriter writer, Pile value, JsonSerializer serializer)
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

        public override Pile ReadJson(
            JsonReader reader, Type objectType, Pile existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException(
                    $"Pile は CardId 配列として deserialize する必要があります(現在の TokenType: {reader.TokenType})");
            }

            var cards = serializer.Deserialize<List<CardId>>(reader);
            return new Pile(cards);
        }
    }
}
