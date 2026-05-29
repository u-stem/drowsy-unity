using System;
using System.Collections.Generic;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz.Effects;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz
{
    /// <summary>
    /// <see cref="ScriptableObjectCardCatalog"/> の 1 エントリ(= 1 カード)を表す
    /// <c>[Serializable]</c> POCO。
    /// Unity Inspector で編集可能なカードデータ + 効果列(<see cref="EffectAsset"/> 配列)の保持を担う。
    /// </summary>
    /// <remarks>
    /// <see cref="ToCardData"/> で <see cref="Drowsy.Domain.Cards.CardData"/> に変換し、
    /// <see cref="ScriptableObjectCardCatalog.Get"/> / <see cref="ScriptableObjectCardCatalog.TryGet"/> 経路で
    /// 呼び出し側へ返却する。
    /// <para>
    /// 効果列(<see cref="Effects"/>)は <c>[SerializeReference] EffectAsset[] _effects</c> で
    /// polymorphic serialize を実現し、Designer が Inspector で複数派生型を選択可能。
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class CardEntryAsset
    {
        [SerializeField] private string _cardIdValue;
        [SerializeField] private string _name;
        [SerializeField] private AttributeEntry[] _attributes;

        // [SerializeReference] で polymorphic serialization を実現し、Designer が Inspector で複数派生型を選択可能。
        // null 要素 / ToDomain 失敗は ScriptableObjectCardCatalog 側で graceful skip(INF-018 / INF-019)。
        [SerializeReference] private EffectAsset[] _effects;

        /// <summary>カード ID 文字列(<see cref="CardId.Of"/> に渡す前の raw 値)。</summary>
        public string CardIdValue => _cardIdValue;

        /// <summary>カード名(<see cref="CardData.Name"/>)。</summary>
        public string Name => _name;

        /// <summary>カード属性配列(本 PR では空配列許容、<see cref="CardData.Attributes"/> の Dictionary 表現)。</summary>
        public IReadOnlyList<AttributeEntry> Attributes =>
            _attributes ?? Array.Empty<AttributeEntry>();

        /// <summary>
        /// 効果列(M4-PR2 で追加、INF-015)。<see cref="ScriptableObjectCardCatalog.GetEffects"/> が
        /// <see cref="EffectAsset.ToDomain"/> 経由で <see cref="IEffect"/> 派生型に変換して返す。
        /// null フィールドは空配列にフォールバックする null-safe プロパティ。
        /// </summary>
        public IReadOnlyList<EffectAsset> Effects =>
            _effects ?? Array.Empty<EffectAsset>();

        /// <summary>
        /// テスト用 ctor。本番経路では Unity Inspector が <see cref="SerializeField"/> /
        /// <see cref="SerializeReference"/> private field を直接初期化するため、
        /// 本 ctor は <c>internal</c> でテスト asmdef からのみアクセス可能。
        /// M4-PR2 で <paramref name="effects"/> 引数を末尾追加(default null で後方互換維持)。
        /// </summary>
        internal CardEntryAsset(
            string cardIdValue,
            string name,
            AttributeEntry[] attributes,
            EffectAsset[] effects = null)
        {
            _cardIdValue = cardIdValue;
            _name = name;
            _attributes = attributes;
            _effects = effects;
        }

        /// <summary>
        /// 本エントリを <see cref="Drowsy.Domain.Cards.CardData"/> に変換する。
        /// <see cref="CardData"/> ctor の防御に従い、<see cref="Name"/> が null / 空 / 空白のみなら <see cref="ArgumentException"/>、
        /// <see cref="Attributes"/> 要素の <see cref="AttributeEntry.Key"/> が null / 空 / 空白のみなら <see cref="ArgumentException"/>。
        /// </summary>
        /// <exception cref="ArgumentException"><see cref="Name"/> または属性 key が無効</exception>
        /// <exception cref="InvalidOperationException"><see cref="Attributes"/> に null 要素が含まれている場合(本来 <see cref="ScriptableObjectCardCatalog.RebuildCache"/> 上位で skip 想定だが、直接呼ばれた場合の明示防御、M4-PR1 code-reviewer W-1 反映 2026-05-14)</exception>
        public CardData ToCardData()
        {
            // M4-PR1 code-reviewer P-1 反映 2026-05-14:プロパティ経由で null ガード済の Attributes を参照
            var attrs = Attributes;
            var kvs = new KeyValuePair<string, int>[attrs.Count];
            for (int i = 0; i < attrs.Count; i++)
            {
                var entry = attrs[i];
                if (entry is null)
                {
                    // 配列要素が null の場合は明示的に例外を投げる(default KVP を返す暗黙の挙動を排除、W-1 反映)。
                    // 本経路は通常上位の RebuildCache が skip して到達しないが、直接 ToCardData() を呼ぶ場合の明示防御。
                    throw new InvalidOperationException(
                        $"CardEntryAsset.Attributes[{i}] が null です(CardIdValue='{_cardIdValue}')。" +
                        "Unity Inspector で null 要素を含めないでください。");
                }
                kvs[i] = new KeyValuePair<string, int>(entry.Key, entry.Value);
            }
            return new CardData(_name, kvs);
        }
    }
}
