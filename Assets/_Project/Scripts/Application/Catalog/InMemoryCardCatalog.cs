using System;
using System.Collections.Generic;
using System.Linq;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Catalog
{
    /// <summary>
    /// DrowZzz 向け <see cref="ICardCatalog{TEffect}"/>(<c>TEffect = IEffect</c>)の in-memory 実装。
    /// <see cref="Dictionary{TKey, TValue}"/> をベースとした汎用 stub。
    /// </summary>
    /// <remarks>
    /// <para>
    /// テスト・skeleton 用途で利用する。本番環境では
    /// <c>Drowsy.Infrastructure.Games.DrowZzz.ScriptableObjectCardCatalog</c> 系を使う。
    /// </para>
    /// <para>
    /// <c>(entries)</c> 単独呼び出しは効果なし(後方互換)、<c>(entries, effects)</c> で
    /// 効果列を明示登録できる。
    /// </para>
    /// <para>
    /// <b>引数型</b>:本 catalog の lookup key は <see cref="CardTypeId"/>(種別 ID)。<see cref="CardId"/> は
    /// instance unique な ID として Pile / Hand / Field / Discard が保持する別概念で、本 catalog の API には現れない。
    /// </para>
    /// </remarks>
    public sealed class InMemoryCardCatalog : ICardCatalog<IEffect>
    {
        // 効果なしカードに対して共通で返す空配列(allocation を 1 回に抑える)
        private static readonly IReadOnlyList<IEffect> EmptyEffects = Array.Empty<IEffect>();

        private readonly Dictionary<CardTypeId, CardData> _store;
        private readonly Dictionary<CardTypeId, IReadOnlyList<IEffect>> _effects;

        /// <summary>
        /// 効果なしカタログを生成する後方互換コンストラクタ(M1〜M2-PR2 互換)。
        /// </summary>
        /// <param name="entries">登録する (CardTypeId, CardData) の列(同一キー後勝ち)</param>
        /// <exception cref="ArgumentNullException">entries が null の場合</exception>
        /// <exception cref="ArgumentException">entries に null CardData 値が含まれる場合</exception>
        public InMemoryCardCatalog(IEnumerable<KeyValuePair<CardTypeId, CardData>> entries)
            : this(entries, null)
        {
        }

        /// <summary>
        /// 効果列込みカタログを生成する。
        /// </summary>
        /// <param name="entries">登録する (CardTypeId, CardData) の列</param>
        /// <param name="effects">登録する (CardTypeId, 効果列) の列。null 可(全カード効果なし扱い)。同一キー後勝ち</param>
        /// <exception cref="ArgumentNullException">entries が null の場合</exception>
        /// <exception cref="ArgumentException">
        /// entries に null CardData 値が含まれる場合、または effects に null 効果列が含まれる場合
        /// </exception>
        public InMemoryCardCatalog(
            IEnumerable<KeyValuePair<CardTypeId, CardData>> entries,
            IEnumerable<KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>> effects)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }
            _store = new Dictionary<CardTypeId, CardData>();
            foreach (var kv in entries)
            {
                if (kv.Value is null)
                {
                    throw new ArgumentException(
                        $"InMemoryCardCatalog の entries に null CardData 値を含めることはできません (key: {kv.Key?.Value ?? "<null>"})",
                        nameof(entries));
                }
                _store[kv.Key] = kv.Value;
            }
            _effects = new Dictionary<CardTypeId, IReadOnlyList<IEffect>>();
            if (effects is not null)
            {
                foreach (var kv in effects)
                {
                    if (kv.Value is null)
                    {
                        throw new ArgumentException(
                            $"InMemoryCardCatalog の effects に null 効果列を含めることはできません (key: {kv.Key?.Value ?? "<null>"})",
                            nameof(effects));
                    }
                    _effects[kv.Key] = kv.Value;
                }
            }
        }

        /// <inheritdoc />
        public CardData Get(CardTypeId typeId)
        {
            if (!_store.TryGetValue(typeId, out var data))
            {
                throw new KeyNotFoundException(
                    $"InMemoryCardCatalog に登録されていない CardTypeId: {typeId?.Value ?? "<null>"}");
            }
            return data;
        }

        /// <inheritdoc />
        public bool TryGet(CardTypeId typeId, out CardData data) => _store.TryGetValue(typeId, out data);

        /// <inheritdoc />
        /// <remarks>
        /// 未登録 CardTypeId / 効果列を登録していない CardTypeId に対しては空列(<see cref="Array.Empty{T}"/>)を返す
        /// (例外を投げない、<c>PlayCardAction.Apply</c> 内の <c>Aggregate</c> に自然に乗る)。
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="typeId"/> が null(App W-3 post-Phase2 レビュー反映)</exception>
        public IReadOnlyList<IEffect> GetEffects(CardTypeId typeId)
        {
            // null をサイレントに空列で受けると呼び出し側のバグが隠れるため、Get/TryGet と同様 ArgumentNullException 化。
            // (ICardCatalog の xmldoc が `non-null` を契約として明記しているため呼び出し側のバグ検出を優先する)
            if (typeId is null)
            {
                throw new ArgumentNullException(nameof(typeId));
            }
            return _effects.TryGetValue(typeId, out var list) ? list : EmptyEffects;
        }

        /// <inheritdoc />
        /// <remarks>
        /// `_store` の Keys を防御コピーした immutable collection を返す
        /// (`_store` 変更後も外部の enumerator が壊れないように `ToList` でスナップショット化)。
        /// 順序は entries の登録順(Dictionary 挿入順、.NET の Dictionary は挿入順保持を契約していないが
        /// 実用上安定、テストでは順序非依存に書くこと)。
        /// </remarks>
        public IReadOnlyCollection<CardTypeId> RegisteredCardTypeIds => _store.Keys.ToList();
    }
}
