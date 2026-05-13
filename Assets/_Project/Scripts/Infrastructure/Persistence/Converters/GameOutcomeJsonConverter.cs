using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="GameOutcome"/> 階層(<see cref="WinnerOutcome"/> / <see cref="DrawOutcome"/>)を polymorphic に
    /// serialize / deserialize する converter。
    /// </summary>
    /// <remarks>
    /// <see cref="EffectJsonConverter"/> と同パターンで <c>"type"</c> discriminator を採用:
    /// <list type="bullet">
    /// <item><c>Winner</c>(<see cref="WinnerOutcome"/>)+ <c>"winner"</c> プロパティに <see cref="PlayerId"/></item>
    /// <item><c>Draw</c>(<see cref="DrawOutcome"/>)+ フィールドなし marker</item>
    /// </list>
    /// <c>null</c>(未終了)は <see cref="JsonToken.Null"/> として serialize する(`"outcome": null`)。
    /// </remarks>
    internal sealed class GameOutcomeJsonConverter : JsonConverter<GameOutcome>
    {
        public override void WriteJson(JsonWriter writer, GameOutcome value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("type");

            switch (value)
            {
                case WinnerOutcome w:
                    writer.WriteValue("Winner");
                    writer.WritePropertyName("winner");
                    serializer.Serialize(writer, w.Winner);
                    break;

                case DrawOutcome _:
                    writer.WriteValue("Draw");
                    break;

                default:
                    throw new JsonSerializationException(
                        $"未対応の GameOutcome 派生型: {value.GetType().FullName}");
            }

            writer.WriteEndObject();
        }

        public override GameOutcome ReadJson(
            JsonReader reader, Type objectType, GameOutcome existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException(
                    $"GameOutcome は object として deserialize する必要があります(現在の TokenType: {reader.TokenType})");
            }

            var jo = JObject.Load(reader);
            var typeToken = jo["type"]
                ?? throw new JsonSerializationException("GameOutcome の deserialize に 'type' discriminator が必要です");
            var typeName = typeToken.ToString();

            return typeName switch
            {
                "Winner" => new WinnerOutcome(jo["winner"].ToObject<PlayerId>(serializer)),
                "Draw" => new DrawOutcome(),
                _ => throw new JsonSerializationException(
                    $"未知の GameOutcome 'type' discriminator: '{typeName}'"),
            };
        }
    }
}
