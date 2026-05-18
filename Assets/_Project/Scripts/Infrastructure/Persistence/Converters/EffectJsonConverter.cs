using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Drowsy.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// 各 <see cref="IEffect"/> 派生型を polymorphic に serialize / deserialize する converter。
    /// 対応派生型は本クラス doc の `<list>` ブロック(code-reviewer S-5 反映 2026-05-17、件数明示の陳腐化を避けるため列挙のみ)を参照。
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
    /// <item><c>RestrictSpecificCardInfluence</c>(<see cref="RestrictSpecificCardInfluenceEffect"/>、ADR-0019 PR ②)</item>
    /// <item><c>ApplyTargetedRestriction</c>(<see cref="ApplyTargetedRestrictionEffect"/>、ADR-0019 PR ②)</item>
    /// <item><c>StackHandCardOnDeckTop</c>(<see cref="StackHandCardOnDeckTopEffect"/>、No.05「喧騒を纏う」2026-05-17)</item>
    /// <item><c>DoubleBedDamageSdpInfluenceMarker</c>(<see cref="DoubleBedDamageSdpInfluenceMarkerEffect"/>、No.06「牙の届かぬ領域」2026-05-17)</item>
    /// <item><c>InvertBedDamageSdpInfluenceMarker</c>(<see cref="InvertBedDamageSdpInfluenceMarkerEffect"/>、No.08「廻るための知恵」2026-05-17)</item>
    /// <item><c>RemoveInvertBedDamageInfluence</c>(<see cref="RemoveInvertBedDamageInfluenceEffect"/>、No.07「知恵の及ばぬ領域」2026-05-17)</item>
    /// <item><c>RestrictAllUsageAndAbandonInfluenceMarker</c>(<see cref="RestrictAllUsageAndAbandonInfluenceMarkerEffect"/>、No.09「強引過ぎる一手」2026-05-17、ADR-0020 と同 PR)</item>
    /// <item><c>RestrictDrawCardInfluenceMarker</c>(<see cref="RestrictDrawCardInfluenceMarkerEffect"/>、No.10「安直過ぎる一手」2026-05-17、ADR-0021 と同 PR)</item>
    /// <item><c>AdjustSdpByHandCount</c>(<see cref="AdjustSdpByHandCountEffect"/>、No.11「機械仕掛けの冬将軍」2026-05-17)</item>
    /// <item><c>AdjustSdpAfterPlayCard</c>(<see cref="AdjustSdpAfterPlayCardEffect"/>、No.12「偽りの太陽」2026-05-17、ADR-0022 と同 PR)</item>
    /// <item><c>AdjustSdpAfterAbandon</c>(<see cref="AdjustSdpAfterAbandonEffect"/>、No.12「偽りの太陽」2026-05-17、ADR-0022 と同 PR)</item>
    /// <item><c>AssociateSpecificCard</c>(<see cref="AssociateSpecificCardEffect"/>、No.13/14/15「最後の砦Ⅰ/Ⅱ/Ⅲ」2026-05-17)</item>
    /// <item><c>ConditionalApplyOrClearInfluences</c>(<see cref="ConditionalApplyOrClearInfluencesEffect"/>、No.16「自分勝手な審判」2026-05-17)</item>
    /// <item><c>ReuseInfluenceSource</c>(<see cref="ReuseInfluenceSourceEffect"/>、No.18「対抗手段」2026-05-18、ADR-0023 Echo キーワード初導入)</item>
    /// <item><c>AssociateToFirstPlayerOnGameStart</c>(<see cref="AssociateToFirstPlayerOnGameStartEffect"/>、No.19「絶対障壁」2026-05-18、ADR-0024 ゲーム開始時自動連想 marker)</item>
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
                    // PlayerInfluence は専用 PlayerInfluenceJsonConverter で serialize される(ADR-0023、複数 ctor 問題回避)
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

                case RestrictSpecificCardInfluenceEffect e:
                    writer.WriteValue("RestrictSpecificCardInfluence");
                    writer.WritePropertyName("targetCardTypeId");
                    // CardTypeId は sealed record + static Of() factory、default ctor 不在のため string 値で serialize する
                    // (2026-05-17 No.05 開発中に発覚した INF-139 テスト失敗 fixup、Newtonsoft default reflection 回避)
                    writer.WriteValue(e.TargetCardTypeId.Value);
                    break;

                case ApplyTargetedRestrictionEffect e:
                    writer.WriteValue("ApplyTargetedRestriction");
                    writer.WritePropertyName("target");
                    serializer.Serialize(writer, e.Target);
                    writer.WritePropertyName("remainingCount");
                    writer.WriteValue(e.RemainingCount);
                    break;

                case StackHandCardOnDeckTopEffect e:
                    writer.WriteValue("StackHandCardOnDeckTop");
                    writer.WritePropertyName("source");
                    serializer.Serialize(writer, e.Source);
                    break;

                case DoubleBedDamageSdpInfluenceMarkerEffect _:
                    writer.WriteValue("DoubleBedDamageSdpInfluenceMarker");
                    // フィールドなし marker
                    break;

                case InvertBedDamageSdpInfluenceMarkerEffect _:
                    writer.WriteValue("InvertBedDamageSdpInfluenceMarker");
                    // フィールドなし marker
                    break;

                case RemoveInvertBedDamageInfluenceEffect e:
                    writer.WriteValue("RemoveInvertBedDamageInfluence");
                    writer.WritePropertyName("target");
                    serializer.Serialize(writer, e.Target);
                    break;

                case RestrictAllUsageAndAbandonInfluenceMarkerEffect _:
                    writer.WriteValue("RestrictAllUsageAndAbandonInfluenceMarker");
                    // フィールドなし marker
                    break;

                case RestrictDrawCardInfluenceMarkerEffect _:
                    writer.WriteValue("RestrictDrawCardInfluenceMarker");
                    // フィールドなし marker
                    break;

                case AdjustSdpByHandCountEffect _:
                    writer.WriteValue("AdjustSdpByHandCount");
                    // フィールドなし(動的計算は EffectInterpreter で session から取得)
                    break;

                case AdjustSdpAfterPlayCardEffect e:
                    writer.WriteValue("AdjustSdpAfterPlayCard");
                    writer.WritePropertyName("delta");
                    writer.WriteValue(e.Delta);
                    break;

                case AdjustSdpAfterAbandonEffect e:
                    writer.WriteValue("AdjustSdpAfterAbandon");
                    writer.WritePropertyName("delta");
                    writer.WriteValue(e.Delta);
                    break;

                case AssociateSpecificCardEffect e:
                    writer.WriteValue("AssociateSpecificCard");
                    // JSON キー名は既存 `RestrictSpecificCardInfluence` の `"targetCardTypeId"` と統一
                    // (code-reviewer W-1 反映 2026-05-17、SO 側 SerializeField 名 `_targetCardTypeIdValue` と JSON キー名は別)
                    writer.WritePropertyName("targetCardTypeId");
                    writer.WriteValue(e.TargetCardTypeId.Value);
                    break;

                case ConditionalApplyOrClearInfluencesEffect e:
                    writer.WriteValue("ConditionalApplyOrClearInfluences");
                    writer.WritePropertyName("target");
                    serializer.Serialize(writer, e.Target);
                    writer.WritePropertyName("threshold");
                    writer.WriteValue(e.Threshold);
                    writer.WritePropertyName("influenceToApply");
                    serializer.Serialize(writer, e.InfluenceToApply);
                    break;

                case ReuseInfluenceSourceEffect _:
                    // ADR-0023 / No.18「対抗手段」:Echo 効果マーカー。フィールドなし。
                    writer.WriteValue("ReuseInfluenceSource");
                    break;

                case AssociateToFirstPlayerOnGameStartEffect _:
                    // ADR-0024 / No.19「絶対障壁」:ゲーム開始時自動連想マーカー。フィールドなし。
                    writer.WriteValue("AssociateToFirstPlayerOnGameStart");
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
                    RequireToken(jo, "target", typeName).ToObject<SdpTarget>(serializer),
                    RequireToken(jo, "delta", typeName).Value<int>()),

                "ApplyInfluence" => new ApplyInfluenceEffect(
                    RequireToken(jo, "target", typeName).ToObject<SdpTarget>(serializer),
                    // PlayerInfluence は専用 PlayerInfluenceJsonConverter で deserialize される(ADR-0023)
                    RequireToken(jo, "influence", typeName).ToObject<PlayerInfluence>(serializer)),

                "RemoveInfluence" => new RemoveInfluenceEffect(
                    RequireToken(jo, "target", typeName).ToObject<SdpTarget>(serializer)),

                "DrawCard" => new DrawCardEffect(
                    RequireToken(jo, "target", typeName).ToObject<SdpTarget>(serializer),
                    RequireToken(jo, "count", typeName).Value<int>()),

                "DamageBed" => new DamageBedEffect(
                    RequireToken(jo, "target", typeName).ToObject<SdpTarget>(serializer),
                    RequireToken(jo, "percent", typeName).Value<int>()),

                "EarlyWinTrigger" => new EarlyWinTriggerEffect(),

                "Choice" => new ChoiceEffect(ReadBranches(jo["branches"], serializer, fieldName: "branches")),

                "TimeOfDayBranch" => new TimeOfDayBranchEffect(
                    ReadEffectList(jo["nightEffects"], serializer, fieldName: "nightEffects"),
                    ReadEffectList(jo["morningEffects"], serializer, fieldName: "morningEffects")),

                "Keyworded" => new KeywordedEffect(
                    RequireToken(jo, "keywords", typeName).ToObject<List<Keyword>>(serializer),
                    RequireToken(jo, "inner", typeName).ToObject<IEffect>(serializer)),

                "RequiresMinimumTotalPointsMarker" => new RequiresMinimumTotalPointsMarkerEffect(
                    RequireToken(jo, "threshold", typeName).Value<int>()),

                "UsageRestrictionMarker" => new UsageRestrictionMarkerEffect(),

                "AssociatableMarker" => new AssociatableMarkerEffect(),

                "RestrictSpecificCardInfluence" => new RestrictSpecificCardInfluenceEffect(
                    // WriteJson 側と対称:string 値 → CardTypeId.Of(string) で復元(default ctor 不在のため Newtonsoft default 経路は使えない)
                    CardTypeId.Of(RequireToken(jo, "targetCardTypeId", typeName).Value<string>())),

                "ApplyTargetedRestriction" => new ApplyTargetedRestrictionEffect(
                    RequireToken(jo, "target", typeName).ToObject<SdpTarget>(serializer),
                    RequireToken(jo, "remainingCount", typeName).Value<int>()),

                "StackHandCardOnDeckTop" => new StackHandCardOnDeckTopEffect(
                    RequireToken(jo, "source", typeName).ToObject<SdpTarget>(serializer)),

                "DoubleBedDamageSdpInfluenceMarker" => new DoubleBedDamageSdpInfluenceMarkerEffect(),

                "InvertBedDamageSdpInfluenceMarker" => new InvertBedDamageSdpInfluenceMarkerEffect(),

                "RemoveInvertBedDamageInfluence" => new RemoveInvertBedDamageInfluenceEffect(
                    RequireToken(jo, "target", typeName).ToObject<SdpTarget>(serializer)),

                "RestrictAllUsageAndAbandonInfluenceMarker" => new RestrictAllUsageAndAbandonInfluenceMarkerEffect(),

                "RestrictDrawCardInfluenceMarker" => new RestrictDrawCardInfluenceMarkerEffect(),

                "AdjustSdpByHandCount" => new AdjustSdpByHandCountEffect(),

                "AdjustSdpAfterPlayCard" => new AdjustSdpAfterPlayCardEffect(
                    RequireToken(jo, "delta", typeName).Value<int>()),

                "AdjustSdpAfterAbandon" => new AdjustSdpAfterAbandonEffect(
                    RequireToken(jo, "delta", typeName).Value<int>()),

                "AssociateSpecificCard" => new AssociateSpecificCardEffect(
                    CardTypeId.Of(RequireToken(jo, "targetCardTypeId", typeName).Value<string>())),

                "ConditionalApplyOrClearInfluences" => new ConditionalApplyOrClearInfluencesEffect(
                    RequireToken(jo, "target", typeName).ToObject<SdpTarget>(serializer),
                    RequireToken(jo, "threshold", typeName).Value<int>(),
                    RequireToken(jo, "influenceToApply", typeName).ToObject<PlayerInfluence>(serializer)),

                "ReuseInfluenceSource" => new ReuseInfluenceSourceEffect(),

                "AssociateToFirstPlayerOnGameStart" => new AssociateToFirstPlayerOnGameStartEffect(),

                _ => throw new JsonSerializationException(
                    $"未知の IEffect 'type' discriminator: '{typeName}'。EffectJsonConverter に case 追加が必要"),
            };
        }

        // Infra W-1 post-Phase2 レビュー反映:
        // `jo["key"]` は存在しないキーで C# null を返し、後段 `.ToObject<T>()` / `.Value<int>()` で
        // NullReferenceException が発生する。これは JsonException として上位で catch できず、
        // どの field が欠落したかが分からなくなるため、欠落時に即時 JsonSerializationException 化する。
        private static JToken RequireToken(JObject jo, string key, string discriminator) =>
            jo[key] ?? throw new JsonSerializationException(
                $"IEffect '{discriminator}' の deserialize に必須キー '{key}' が見つかりません");

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

        private static IReadOnlyList<IReadOnlyList<IEffect>> ReadBranches(JToken token, JsonSerializer serializer, string fieldName = "branches")
        {
            // Infra W-2: token が null(キー欠落)の場合、「配列でない」エラーより
            // 「キーが見つかりません」の方が診断価値が高いため明示分岐する。
            if (token is null)
            {
                throw new JsonSerializationException(
                    $"ChoiceEffect の deserialize に必須キー '{fieldName}' が見つかりません");
            }
            if (token is not JArray outerArray)
            {
                throw new JsonSerializationException(
                    $"ChoiceEffect.{fieldName} は配列の配列として deserialize する必要があります");
            }
            var result = new List<IReadOnlyList<IEffect>>(outerArray.Count);
            foreach (var innerToken in outerArray)
            {
                result.Add(ReadEffectList(innerToken, serializer, fieldName: $"{fieldName}[]"));
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

        private static IReadOnlyList<IEffect> ReadEffectList(JToken token, JsonSerializer serializer, string fieldName = "effects")
        {
            // Infra W-2: token が null(キー欠落)の場合、「配列でない」エラーより
            // 「キーが見つかりません」の方が診断価値が高いため明示分岐する。
            if (token is null)
            {
                throw new JsonSerializationException(
                    $"効果列の deserialize に必須キー '{fieldName}' が見つかりません");
            }
            if (token is not JArray array)
            {
                throw new JsonSerializationException(
                    $"効果列 '{fieldName}' は配列として deserialize する必要があります");
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
