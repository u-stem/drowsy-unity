using Drowsy.Infrastructure.Persistence.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Drowsy.Infrastructure.Persistence
{
    /// <summary>
    /// DrowZzz の永続化で使う <see cref="JsonSerializerSettings"/> を生成するファクトリ。
    /// `DrowZzzGameSessionSerializer` および test fixture が共通利用する。
    /// </summary>
    /// <remarks>
    /// Newtonsoft.Json をシリアライザとして採用し、<c>"type"</c> discriminator にはカスタム JsonConverter を使用する構成。
    /// <para>
    /// 各 converter は本 class が組み立てる際に固定順で登録され、外部からの差し替えは想定しない
    /// (永続化フォーマットが「Drowsy 専用 JSON」であることを担保するため、JsonConvert.DefaultSettings
    /// による global mutation は使わない)。
    /// </para>
    /// <para>
    /// <b>採用方針</b>:
    /// <list type="bullet">
    /// <item>polymorphic 型(<c>IEffect</c> / <c>GameOutcome</c>)はカスタム JsonConverter で <c>"type"</c> discriminator</item>
    /// <item>単純 value object(<c>CardId</c> / <c>PlayerId</c> / <c>Pile</c> / <c>Hand</c> / <c>DdpPool</c>)は
    /// カスタム JsonConverter で plain string / array 化(JSON 容量と可読性を優先)</item>
    /// <item>enum(<c>DrowZzzPhaseState</c> / <c>InfluenceTrigger</c> / <c>SdpTarget</c> / <c>Keyword</c>)は
    /// <see cref="StringEnumConverter"/> で名前 serialize(後方互換性 + 可読性)</item>
    /// <item>record (<c>TurnState</c> / <c>PlayerState</c> / <c>GameState</c> / <c>PendingCounteredEffect</c> /
    /// 12 IEffect 派生型 / <c>PersistedSessionV1</c>)は positional ctor の自動解決 + PascalCase property 名で round-trip</item>
    /// <item><c>PlayerInfluence</c> は専用 <c>PlayerInfluenceJsonConverter</c> 経由(OriginEffects 追加で複数 ctor になり
    /// Newtonsoft 自動 ctor 選択が失敗するため、専用 converter で PascalCase schema + 旧 v1 JSON 後方互換を制御)</item>
    /// </list>
    /// </para>
    /// </remarks>
    internal static class DrowZzzJsonSettings
    {
        /// <summary>新規 <see cref="JsonSerializerSettings"/> を生成する(各呼び出しで independent インスタンス)。</summary>
        public static JsonSerializerSettings Create()
        {
            var settings = new JsonSerializerSettings
            {
                // PascalCase property 名(Domain / Application 型の C# property 名と JSON key を一致させる)
                ContractResolver = new DefaultContractResolver(),
                // ゲーム状態の serialize なので循環参照は理論上発生しないが、誤った Domain 拡張時の即時検出のため Error
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                // null フィールドは含める(Outcome=null = 未終了の意味を維持)
                NullValueHandling = NullValueHandling.Include,
                // default 値も含める(0 / false / 空 list を default で扱わず明示する)
                DefaultValueHandling = DefaultValueHandling.Include,
                // TypeNameHandling.None: $type を出さない(本実装は全 polymorphic 型を専用 converter で discriminator 化)
                TypeNameHandling = TypeNameHandling.None,
                // Round-trip で日付型は使わないが、念のため ISO8601 で固定(将来 metadata 追加時の保険)
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                // JSON は LF 改行 + indent 2 で人間可読(セーブファイルは小〜中サイズなので overhead は許容)
                Formatting = Formatting.Indented,
            };

            // enum は StringEnumConverter で名前 serialize(後方互換性のため)
            settings.Converters.Add(new StringEnumConverter());

            // 単純 value object converter(plain string / array 化)
            settings.Converters.Add(new CardIdJsonConverter());
            settings.Converters.Add(new PlayerIdJsonConverter());
            settings.Converters.Add(new PileJsonConverter());
            settings.Converters.Add(new HandJsonConverter());
            settings.Converters.Add(new DdpPoolJsonConverter());

            // PlayerInfluence の専用 converter(複数 ctor で Newtonsoft 標準経路が ctor 自動選択に失敗する問題の解消)
            settings.Converters.Add(new PlayerInfluenceJsonConverter());

            // polymorphic 型 converter(discriminator 方式)
            settings.Converters.Add(new EffectJsonConverter());
            settings.Converters.Add(new GameOutcomeJsonConverter());

            return settings;
        }
    }
}
