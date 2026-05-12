using System;
using System.Collections.Generic;
using Drowsy.Domain.Cards;
using UnityEngine;

namespace Drowsy.Infrastructure.Games.DrowZzz
{
    /// <summary>
    /// <see cref="ScriptableObjectCardCatalog"/> の 1 エントリ(= 1 カード)を表す
    /// <c>[Serializable]</c> POCO(M4-PR1 で導入、ADR-0012 §2)。
    /// Unity Inspector で編集可能なカードデータと、将来追加される効果列(M4-PR2 / PR3)の保持を担う。
    /// </summary>
    /// <remarks>
    /// <see cref="ToCardData"/> で <see cref="Drowsy.Domain.Cards.CardData"/> に変換し、
    /// <see cref="ScriptableObjectCardCatalog.Get"/> / <see cref="ScriptableObjectCardCatalog.TryGet"/> 経路で
    /// 呼び出し側へ返却する。本 PR 範囲では効果列フィールドは追加せず、M4-PR2 / PR3 で
    /// <c>EffectAsset[] _effects</c>(SerializeReference 含む)を追加予定(ADR-0012 §3、INF-008)。
    /// </remarks>
    [Serializable]
    public sealed class CardEntryAsset
    {
        [SerializeField] private string _cardIdValue;
        [SerializeField] private string _name;
        [SerializeField] private AttributeEntry[] _attributes;

        // M4-PR2 / PR3 で効果列フィールドを追加予定(ADR-0012 §3):
        //   [SerializeReference] private IEffect[] _effects;  または [SerializeField] private EffectAsset[] _effectAssets;
        // 採用案(a / b / c)は M4-PR2 着手時に JIT 確定する。

        /// <summary>カード ID 文字列(<see cref="CardId.Of"/> に渡す前の raw 値)。</summary>
        public string CardIdValue => _cardIdValue;

        /// <summary>カード名(<see cref="CardData.Name"/>)。</summary>
        public string Name => _name;

        /// <summary>カード属性配列(本 PR では空配列許容、<see cref="CardData.Attributes"/> の Dictionary 表現)。</summary>
        public IReadOnlyList<AttributeEntry> Attributes =>
            _attributes ?? Array.Empty<AttributeEntry>();

        /// <summary>
        /// テスト用 ctor。本番経路では Unity Inspector が <see cref="SerializeField"/> private field を直接初期化するため、
        /// 本 ctor は <c>internal</c> でテスト asmdef からのみアクセス可能。
        /// </summary>
        internal CardEntryAsset(string cardIdValue, string name, AttributeEntry[] attributes)
        {
            _cardIdValue = cardIdValue;
            _name = name;
            _attributes = attributes;
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
