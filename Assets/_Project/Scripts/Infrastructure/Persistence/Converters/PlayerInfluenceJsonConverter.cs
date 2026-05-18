using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="PlayerInfluence"/> の専用 JsonConverter(ADR-0023、2026-05-18 で導入)。
    /// </summary>
    /// <remarks>
    /// ADR-0023 で <see cref="PlayerInfluence.OriginEffects"/> フィールドが追加され、コンストラクタが
    /// 3 引数 / 4 引数の 2 種類存在するようになった。Newtonsoft 標準 reflection 経路では複数 ctor + 引数なし ctor
    /// 不在 + <c>[JsonConstructor]</c> 属性不在の組み合わせで「Unable to find a constructor to use」エラーが
    /// 発生する(`PersistedSessionV1.Influences` deserialize 経路で再現)。
    /// <para>
    /// Application 層は Newtonsoft.Json に依存しないため、<see cref="PlayerInfluence"/> に直接
    /// <c>[JsonConstructor]</c> 属性を付けるのは Clean Architecture 違反。代わりに Infrastructure 層の
    /// 本専用 converter で serialize / deserialize を制御する。
    /// </para>
    /// <para>
    /// JSON schema(PascalCase property 名、`DrowZzzJsonSettings` 既存 record と整合):
    /// <code>
    /// {
    ///   "Trigger": "OwnPhaseStart",
    ///   "TickEffect": { ... IEffect via EffectJsonConverter ... },
    ///   "RemainingCount": 3,
    ///   "OriginEffects": [ ... IEffect[] via EffectJsonConverter ... ]
    /// }
    /// </code>
    /// 旧 v1 JSON 後方互換:<c>OriginEffects</c> キー欠落時は <see cref="Array.Empty{T}"/> フォールバック
    /// (ADR-0023 §8、ADR-0019 `AssociatedCardIds` と同パターン)。
    /// </para>
    /// </remarks>
    internal sealed class PlayerInfluenceJsonConverter : JsonConverter<PlayerInfluence>
    {
        public override void WriteJson(JsonWriter writer, PlayerInfluence value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteStartObject();
            writer.WritePropertyName("Trigger");
            serializer.Serialize(writer, value.Trigger);
            writer.WritePropertyName("TickEffect");
            serializer.Serialize(writer, value.TickEffect);
            writer.WritePropertyName("RemainingCount");
            writer.WriteValue(value.RemainingCount);
            writer.WritePropertyName("OriginEffects");
            writer.WriteStartArray();
            for (int i = 0; i < value.OriginEffects.Count; i++)
            {
                serializer.Serialize(writer, value.OriginEffects[i]);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public override PlayerInfluence ReadJson(
            JsonReader reader, Type objectType, PlayerInfluence existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException(
                    $"PlayerInfluence は object として deserialize する必要があります(現在の TokenType: {reader.TokenType})");
            }

            var jo = JObject.Load(reader);
            var triggerToken = jo["Trigger"]
                ?? throw new JsonSerializationException("PlayerInfluence の deserialize に必須キー 'Trigger' が見つかりません");
            var tickEffectToken = jo["TickEffect"]
                ?? throw new JsonSerializationException("PlayerInfluence の deserialize に必須キー 'TickEffect' が見つかりません");
            var remainingCountToken = jo["RemainingCount"]
                ?? throw new JsonSerializationException("PlayerInfluence の deserialize に必須キー 'RemainingCount' が見つかりません");

            var trigger = triggerToken.ToObject<InfluenceTrigger>(serializer);
            var tickEffect = tickEffectToken.ToObject<IEffect>(serializer);
            int remainingCount = remainingCountToken.Value<int>();

            // OriginEffects は nullable + 空 list フォールバック(旧 v1 JSON 後方互換、ADR-0023 §8)
            var originEffectsToken = jo["OriginEffects"];
            IReadOnlyList<IEffect> originEffects;
            if (originEffectsToken is null || originEffectsToken.Type == JTokenType.Null)
            {
                originEffects = Array.Empty<IEffect>();
            }
            else
            {
                if (originEffectsToken is not JArray array)
                {
                    throw new JsonSerializationException(
                        $"PlayerInfluence.OriginEffects は配列として deserialize する必要があります(TokenType: {originEffectsToken.Type})");
                }
                var list = new List<IEffect>(array.Count);
                foreach (var item in array)
                {
                    list.Add(item.ToObject<IEffect>(serializer));
                }
                originEffects = list;
            }

            return new PlayerInfluence(trigger, tickEffect, remainingCount, originEffects);
        }
    }
}
