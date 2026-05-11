using System;
using System.Collections.Generic;
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
    /// M1〜M2 のテスト・skeleton 用途で利用する。永続化と一緒に判断する M4 で
    /// <c>Drowsy.Infrastructure.Games.DrowZzz.ScriptableObjectCardCatalog</c> 系の追加を検討する
    /// (ADR-0007 §5 / §M2-PR1。ADR-0006 §1.3 の「M2 で SO 化」記載は ADR-0007 §5 で M4 に変更済)。
    /// </para>
    /// <para>
    /// M2-PR1 段階では <see cref="GetEffects(CardId)"/> は常に空列を返す(全カードが効果なし、M1 互換)。
    /// M2-PR2 以降で 1 PR = 1 effect record を追加する都度、本クラスを拡張するか、effect 別 store の導入を判断する。
    /// </para>
    /// </remarks>
    public sealed class InMemoryCardCatalog : ICardCatalog<IEffect>
    {
        // 効果なしカードに対して共通で返す空配列(allocation を 1 回に抑える)
        private static readonly IReadOnlyList<IEffect> EmptyEffects = Array.Empty<IEffect>();

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

        /// <inheritdoc />
        /// <remarks>
        /// M2-PR1 段階では未登録 / 登録済を問わず常に空列(<see cref="Array.Empty{T}"/>)を返す。
        /// 未登録 CardId に対する例外は投げない(効果列の問い合わせは <c>0 個</c> として扱える方が
        /// <c>PlayCardAction.Apply</c> 内の <c>Aggregate</c> に自然に乗る、ADR-0007 §3)。
        /// </remarks>
        public IReadOnlyList<IEffect> GetEffects(CardId id) => EmptyEffects;
    }
}
