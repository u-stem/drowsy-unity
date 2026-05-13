using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// <see cref="IEffect"/> 12 派生型を polymorphic に serialize / deserialize する converter。
    /// </summary>
    /// <remarks>
    /// ADR-0012 §7「JIT 確定: Discriminator = カスタム JsonConverter (Recommended)」(2026-05-13 ユーザー JIT 確定)
    /// に基づく実装。<c>"type"</c> property を discriminator として明示し、Newtonsoft 標準の
    /// <c>TypeNameHandling.$type</c>(完全修飾型名)を採用しないことで JSON の互換性 / 可読性 / リネーム耐性を確保する。
    /// <para>
    /// discriminator value 命名は <c>EffectAsset</c>(M4-PR2/PR3 で確立した SO 側 discriminator)と整合させ、
    /// Asset class 名から <c>Effect</c> / <c>EffectAsset</c> suffix を除いた形:
    /// <list type="bullet">
    /// <item><c>AdjustSdp</c>(<see cref="AdjustSdpEffect"/>)</item>
    /// <item><c>ApplyInfluence</c>(<see cref="ApplyInfluenceEffect"/>)</item>
    /// <item><c>RemoveInfluence</c>(<see cref="RemoveInfluenceEffect"/>)</item>
    /// <item><c>DrawCard</c>(<see cref="DrawCardEffect"/>)</item>
    /// <item><c>DamageBed</c>(<see cref="DamageBedEffect"/>)</item>
    /// <item><c>EarlyWinTrigger</c>(<see cref="EarlyWinTriggerEffect"/>)</item>
    /// <item><c>Choice</c>(<see cref="ChoiceEffect"/>)</item>
    /// <item><c>TimeOfDayBranch</c>(<see cref="TimeOfDayBranchEffect"/>)</item>
    /// <item><c>Keyworded</c>(<see cref="KeywordedEffect"/>)</item>
    /// <item><c>RequiresMinimumTotalPointsMarker</c>(<see cref="RequiresMinimumTotalPointsMarkerEffect"/>)</item>
    /// <item><c>UsageRestrictionMarker</c>(<see cref="UsageRestrictionMarkerEffect"/>)</item>
    /// <item><c>AssociatableMarker</c>(<see cref="AssociatableMarkerEffect"/>)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Wrapper 系(<see cref="ChoiceEffect"/> / <see cref="TimeOfDayBranchEffect"/> / <see cref="KeywordedEffect"/>)は
    /// 内部に <see cref="IEffect"/> を保持するため、<see cref="JObject.ToObject{T}(JsonSerializer)"/> 経由で
    /// 本 converter が <see cref="JsonSerializer.Converters"/> に登録されている前提で再帰的に解決される。
    /// </para>
    /// </remarks>
    internal sealed class EffectJsonConverter : JsonConverter<IEffect>
    {
        public override void WriteJson(JsonWriter writer, IEffect value, JsonSerializer serializer)
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
                case AdjustSdpEffect e:
                    writer.WriteValue("AdjustSdp");
                    writer.WritePropertyName("target");
                    serializer.Serialize(writer, e.Target);
                    writer.WritePropertyName("delta");
                    writer.WriteValue(e.Delta);
                    break;

                case ApplyInfluenceEffect e:
                    writer.WriteValue("ApplyInfluence");
                    writer.WritePropertyName("target");
                    serializer.Serialize(writer, e.Target);
                    writer.WritePropertyName("influence");
                    serializer.Serialize(writer, e.Influence);
                    break;

                case RemoveInfluenceEffect e:
                    writer.WriteValue("RemoveInfluence");
                    writer.WritePropertyName("target");
                    serializer.Serialize(writer, e.Target);
                    break;

                case DrawCardEffect e:
                    writer.WriteValue("DrawCard");
                    writer.WritePropertyName("target");
                    serializer.Serialize(writer, e.Target);
                    writer.WritePropertyName("count");
                    writer.WriteValue(e.Count);
                    break;

                case DamageBedEffect e:
                    writer.WriteValue("DamageBed");
                    writer.WritePropertyName("target");
                    serializer.Serialize(writer, e.Target);
                    writer.WritePropertyName("percent");
                    writer.WriteValue(e.Percent);
                    break;

                case EarlyWinTriggerEffect _:
                    writer.WriteValue("EarlyWinTrigger");
                    // フィールドなし marker
                    break;

                case ChoiceEffect e:
                    writer.WriteValue("Choice");
                    writer.WritePropertyName("branches");
                    WriteBranches(writer, e.Branches, serializer);
                    break;

                case TimeOfDayBranchEffect e:
                    writer.WriteValue("TimeOfDayBranch");
                    writer.WritePropertyName("nightEffects");
                    WriteEffectList(writer, e.NightEffects, serializer);
                    writer.WritePropertyName("morningEffects");
                    WriteEffectList(writer, e.MorningEffects, serializer);
                    break;

                case KeywordedEffect e:
                    writer.WriteValue("Keyworded");
                    writer.WritePropertyName("keywords");
                    writer.WriteStartArray();
                    for (int i = 0; i < e.Keywords.Count; i++)
                    {
                        serializer.Serialize(writer, e.Keywords[i]);
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("inner");
                    serializer.Serialize(writer, e.Inner);
                    break;

                case RequiresMinimumTotalPointsMarkerEffect e:
                    writer.WriteValue("RequiresMinimumTotalPointsMarker");
                    writer.WritePropertyName("threshold");
                    writer.WriteValue(e.Threshold);
                    break;

                case UsageRestrictionMarkerEffect _:
                    writer.WriteValue("UsageRestrictionMarker");
                    // フィールドなし marker
                    break;

                case AssociatableMarkerEffect _:
                    writer.WriteValue("AssociatableMarker");
                    // フィールドなし marker
                    break;

                default:
                    throw new JsonSerializationException(
                        $"未対応の IEffect 派生型: {value.GetType().FullName}。新しい派生型は EffectJsonConverter に case 追加が必要");
            }

            writer.WriteEndObject();
        }

        public override IEffect ReadJson(
            JsonReader reader, Type objectType, IEffect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException(
                    $"IEffect は object として deserialize する必要があります(現在の TokenType: {reader.TokenType})");
            }

            var jo = JObject.Load(reader);
            var typeToken = jo["type"]
                ?? throw new JsonSerializationException("IEffect の deserialize に 'type' discriminator が必要です");
            var typeName = typeToken.ToString();

            return typeName switch
            {
                "AdjustSdp" => new AdjustSdpEffect(
                    jo["target"].ToObject<SdpTarget>(serializer),
                    jo["delta"].Value<int>()),

                "ApplyInfluence" => new ApplyInfluenceEffect(
                    jo["target"].ToObject<SdpTarget>(serializer),
                    jo["influence"].ToObject<PlayerInfluence>(serializer)),

                "RemoveInfluence" => new RemoveInfluenceEffect(
                    jo["target"].ToObject<SdpTarget>(serializer)),

                "DrawCard" => new DrawCardEffect(
                    jo["target"].ToObject<SdpTarget>(serializer),
                    jo["count"].Value<int>()),

                "DamageBed" => new DamageBedEffect(
                    jo["target"].ToObject<SdpTarget>(serializer),
                    jo["percent"].Value<int>()),

                "EarlyWinTrigger" => new EarlyWinTriggerEffect(),

                "Choice" => new ChoiceEffect(ReadBranches(jo["branches"], serializer)),

                "TimeOfDayBranch" => new TimeOfDayBranchEffect(
                    ReadEffectList(jo["nightEffects"], serializer),
                    ReadEffectList(jo["morningEffects"], serializer)),

                "Keyworded" => new KeywordedEffect(
                    jo["keywords"].ToObject<List<Keyword>>(serializer),
                    jo["inner"].ToObject<IEffect>(serializer)),

                "RequiresMinimumTotalPointsMarker" => new RequiresMinimumTotalPointsMarkerEffect(
                    jo["threshold"].Value<int>()),

                "UsageRestrictionMarker" => new UsageRestrictionMarkerEffect(),

                "AssociatableMarker" => new AssociatableMarkerEffect(),

                _ => throw new JsonSerializationException(
                    $"未知の IEffect 'type' discriminator: '{typeName}'。EffectJsonConverter に case 追加が必要"),
            };
        }

        // wrapper 系(ChoiceEffect)の二重 list 表現
        private static void WriteBranches(
            JsonWriter writer, IReadOnlyList<IReadOnlyList<IEffect>> branches, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            for (int i = 0; i < branches.Count; i++)
            {
                WriteEffectList(writer, branches[i], serializer);
            }
            writer.WriteEndArray();
        }

        private static IReadOnlyList<IReadOnlyList<IEffect>> ReadBranches(JToken token, JsonSerializer serializer)
        {
            if (token is not JArray outerArray)
            {
                throw new JsonSerializationException(
                    "ChoiceEffect.branches は配列の配列として deserialize する必要があります");
            }
            var result = new List<IReadOnlyList<IEffect>>(outerArray.Count);
            foreach (var innerToken in outerArray)
            {
                result.Add(ReadEffectList(innerToken, serializer));
            }
            return result;
        }

        // wrapper 系(TimeOfDayBranchEffect / 内側 ChoiceEffect)で利用する効果列の serialize / deserialize
        private static void WriteEffectList(
            JsonWriter writer, IReadOnlyList<IEffect> effects, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            for (int i = 0; i < effects.Count; i++)
            {
                serializer.Serialize(writer, effects[i]);
            }
            writer.WriteEndArray();
        }

        private static IReadOnlyList<IEffect> ReadEffectList(JToken token, JsonSerializer serializer)
        {
            if (token is not JArray array)
            {
                throw new JsonSerializationException(
                    "効果列は配列として deserialize する必要があります");
            }
            var result = new List<IEffect>(array.Count);
            foreach (var item in array)
            {
                result.Add(item.ToObject<IEffect>(serializer));
            }
            return result;
        }
    }
}
