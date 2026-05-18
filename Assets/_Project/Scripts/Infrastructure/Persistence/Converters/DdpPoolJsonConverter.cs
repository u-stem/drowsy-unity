using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz;
using Newtonsoft.Json;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="DdpPool"/> を int 配列として serialize / deserialize する converter。
    /// </summary>
    /// <remarks>
    /// <see cref="DdpPool"/> は <c>sealed class : IEquatable&lt;DdpPool&gt;</c> で <c>DdpPool(IEnumerable&lt;int&gt;)</c>
    /// public ctor を持つ。順序付きシーケンス同値で等価判定するため、配列順をそのまま保持する。
    /// <c>"ddpPool": [-3, 4, 7, -1]</c> 形式で round-trip 可能(<see cref="PileJsonConverter"/> と同パターン)。
    /// </remarks>
    internal sealed class DdpPoolJsonConverter : JsonConverter<DdpPool>
    {
        public override void WriteJson(JsonWriter writer, DdpPool value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteStartArray();
            for (int i = 0; i < value.Values.Count; i++)
            {
                writer.WriteValue(value.Values[i]);
            }
            writer.WriteEndArray();
        }

        public override DdpPool ReadJson(
            JsonReader reader, Type objectType, DdpPool existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonSerializationException(
                    $"DdpPool は int 配列として deserialize する必要があります(現在の TokenType: {reader.TokenType})");
            }

            var values = serializer.Deserialize<List<int>>(reader);
            return new DdpPool(values);
        }
    }
}
