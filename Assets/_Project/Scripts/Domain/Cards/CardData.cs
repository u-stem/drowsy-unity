using System;
using System.Collections.Generic;

namespace Drowsy.Domain.Cards
{
    /// <summary>
    /// カードの中身(名前と数値属性集合)を表す不変値オブジェクト。
    /// <see cref="CardId"/> とは独立し、Application 層の <c>ICardCatalog</c> (Phase 2 スコープ) で結合する想定。
    /// </summary>
    /// <remarks>
    /// <c>record</c> ではなく <c>sealed class</c> として実装する。理由は <see cref="Attributes"/> (内部 <see cref="Dictionary{TKey, TValue}"/>) の
    /// 等値比較を順序非依存マルチセット同値で行うために <see cref="Equals(CardData)"/> /
    /// <see cref="GetHashCode"/> の独自 override が必須であり、record の auto-equals では
    /// 参照同値となり値同値が壊れるため。
    /// </remarks>
    public sealed class CardData : IEquatable<CardData>
    {
        private readonly Dictionary<string, int> _attributes;

        /// <summary>カード名(空白文字列禁止)。</summary>
        public string Name { get; }

        /// <summary>カード属性辞書(読み取り専用、コンストラクタで防御コピー済)。</summary>
        public IReadOnlyDictionary<string, int> Attributes => _attributes;

        /// <summary>
        /// CardData を生成する。null・空文字列・空白のみの <paramref name="name"/> は <see cref="ArgumentException"/>。
        /// null の <paramref name="attributes"/> は <see cref="ArgumentNullException"/>。
        /// 空白キーを含む <paramref name="attributes"/> は <see cref="ArgumentException"/>。
        /// </summary>
        /// <param name="name">カード名(非空白)</param>
        /// <param name="attributes">数値属性辞書(防御コピーされる)</param>
        /// <exception cref="ArgumentException">name が null・空・空白のみ、または attributes に空白キーを含む場合</exception>
        /// <exception cref="ArgumentNullException">attributes が null の場合</exception>
        public CardData(string name, IEnumerable<KeyValuePair<string, int>> attributes)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "CardData の name は null・空・空白のみにできません",
                    nameof(name));
            }
            if (attributes is null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            Name = name;
            _attributes = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var kv in attributes)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                {
                    throw new ArgumentException(
                        "CardData の attributes キーは null・空・空白のみにできません",
                        nameof(attributes));
                }
                _attributes[kv.Key] = kv.Value;
            }
        }

        /// <summary><paramref name="key"/> が <see cref="Attributes"/> に存在するかを返す。</summary>
        /// <exception cref="ArgumentNullException">key が null の場合</exception>
        public bool HasAttribute(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            return _attributes.ContainsKey(key);
        }

        /// <summary>
        /// <paramref name="key"/> に対応する属性値を返す。存在しなければ <paramref name="defaultValue"/> を返す。
        /// </summary>
        /// <exception cref="ArgumentNullException">key が null の場合</exception>
        public int GetAttribute(string key, int defaultValue = 0)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            return _attributes.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>順序非依存マルチセット同値で比較する。</summary>
        public bool Equals(CardData other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (!string.Equals(Name, other.Name, StringComparison.Ordinal))
            {
                return false;
            }
            if (_attributes.Count != other._attributes.Count)
            {
                return false;
            }
            foreach (var kv in _attributes)
            {
                if (!other._attributes.TryGetValue(kv.Key, out var v) || v != kv.Value)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj) => obj is CardData other && Equals(other);

        /// <summary>値同値で == 比較する。両方 null は等価、片方のみ null は非等価。</summary>
        public static bool operator ==(CardData left, CardData right) =>
            left is null ? right is null : left.Equals(right);

        /// <summary>値同値で != 比較する。</summary>
        public static bool operator !=(CardData left, CardData right) => !(left == right);

        /// <summary>順序非依存ハッシュ。Name のハッシュと各 (key, value) ペアのハッシュを XOR 合成する。</summary>
        /// <remarks>
        /// XOR は同じハッシュペアが偶数回現れると 0 で打ち消し合うため理論的衝突リスクがあるが、
        /// CardData の典型的な属性数(1〜10 個)では実用上十分な分布。属性数が大幅に増えるゲームで
        /// Dictionary / HashSet のバケット検索効率が問題化する場合は累積加算等への変更を検討する。
        /// </remarks>
        public override int GetHashCode()
        {
            int hash = StringComparer.Ordinal.GetHashCode(Name);
            foreach (var kv in _attributes)
            {
                hash ^= HashCode.Combine(kv.Key, kv.Value);
            }
            return hash;
        }
    }
}
