using System;
using System.Collections.Generic;
using Drowsy.Domain.Cards;

namespace Drowsy.Application.Catalog
{
    /// <summary>
    /// <see cref="ICardCatalog"/> の in-memory 実装。<see cref="Dictionary{TKey, TValue}"/> をベースとした汎用 stub。
    /// </summary>
    /// <remarks>
    /// M1〜M2 のテスト・skeleton 用途で利用し、本格的な永続化や ScriptableObject ベースの実装
    /// (M2 以降の <c>Drowsy.Infrastructure.Games.DrowZzz.ScriptableObjectCardCatalog</c> 予定) と並行存続する。
    /// 詳細は ADR-0006 §1.3 / §M1-PR2 を参照。
    /// </remarks>
    public sealed class InMemoryCardCatalog : ICardCatalog
    {
        private readonly Dictionary<CardId, CardData> _store;

        /// <summary>
        /// <paramref name="entries"/> の防御コピーを内部に保持する。
        /// </summary>
        /// <param name="entries">登録する (CardId, CardData) の列(同一キー後勝ち)</param>
        /// <exception cref="ArgumentNullException">entries が null の場合</exception>
        /// <exception cref="ArgumentException">entries に null CardData 値が含まれる場合</exception>
        public InMemoryCardCatalog(IEnumerable<KeyValuePair<CardId, CardData>> entries)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }
            _store = new Dictionary<CardId, CardData>();
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
        }

        /// <inheritdoc />
        public CardData Get(CardId id)
        {
            if (!_store.TryGetValue(id, out var data))
            {
                throw new KeyNotFoundException(
                    $"InMemoryCardCatalog に登録されていない CardId: {id?.Value ?? "<null>"}");
            }
            return data;
        }

        /// <inheritdoc />
        public bool TryGet(CardId id, out CardData data) => _store.TryGetValue(id, out data);
    }
}
