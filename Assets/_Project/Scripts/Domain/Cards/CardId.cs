using System;

namespace Drowsy.Domain.Cards
{
    /// <summary>
    /// カードの**インスタンス**識別子(deck / hand / field / discard 内で unique)を表す不変値オブジェクト(ADR-0018)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 旧 Phase 1 設計では <c>CardId.Of(string)</c> 単純文字列型で「catalog の lookup key」と
    /// 「Hand 内 unique 識別子」を兼ねていたが、業界デファクト(Card Type / Card Instance 分離)と
    /// 乖離した二重意味を持っていた。M5 UI 実機検証で Hand 重複検出エラーが発覚し、ADR-0018 で
    /// <see cref="CardTypeId"/> を新設して種別 ID を分離、本型は instance unique な ID として再定義した。
    /// </para>
    /// <para>
    /// <b>構造</b>:<c>(CardTypeId TypeId, int Instance)</c> の複合 record。等価性は record 自動生成で
    /// <see cref="TypeId"/> と <see cref="Instance"/> の組で比較する。同じ種別(TypeId)のカードが複数枚
    /// deck / hand に存在する場合、それぞれが異なる <see cref="Instance"/> を持つことで unique 性を担保する。
    /// </para>
    /// <para>
    /// <b>catalog lookup</b>:<see cref="ICardCatalog{TEffect}"/> 系 API は引数として <see cref="CardTypeId"/>
    /// を取る(ADR-0018 §3)。<see cref="CardId"/> から catalog を引く側は <c>cardId.TypeId</c> を渡す。
    /// </para>
    /// <para>
    /// <b>後方互換</b>:旧 <c>CardId.Of(string)</c> API は廃止(breaking change)。永続化 / ログ / 表示用には
    /// <see cref="Value"/> プロパティが <c>"$"{TypeId.Value}#{Instance}""</c> 形式の computed string を返す。
    /// </para>
    /// </remarks>
    public sealed record CardId
    {
        /// <summary>カードの種別 ID(catalog の lookup key)。</summary>
        public CardTypeId TypeId { get; }

        /// <summary>同一種別内でのインスタンス番号(0 以上)。</summary>
        public int Instance { get; }

        // post-Phase2 アルゴリズム最適化レビュー Top-3 反映:旧実装は computed property
        // `Value => $"{TypeId.Value}#{Instance}"` で参照毎に string を alloc していた。
        // ctor で 1 回計算して private readonly field にキャッシュする方式に変更し、
        // 以降の Value 参照を alloc ゼロにする。
        //
        // 重要:record 自動生成の Equals / GetHashCode はパラメータ + auto-property のみを対象とする。
        // `_value` を明示 backing field + expression-bodied property にすることで等価判定に含めず、
        // CardId の等価性は引き続き (TypeId, Instance) の組のみで決定する(意味的にも _value は
        // 派生値であり、等価判定への寄与は冗長)。
        private readonly string _value;

        /// <summary>
        /// 永続化 / ログ / 表示用の文字列表現(<c>"$"{TypeId.Value}#{Instance}""</c> 形式、ctor で 1 回計算済)。
        /// </summary>
        public string Value => _value;

        private CardId(CardTypeId typeId, int instance)
        {
            TypeId = typeId;
            Instance = instance;
            _value = $"{typeId.Value}#{instance}";
        }

        /// <summary>
        /// CardId(インスタンス)を生成する。
        /// </summary>
        /// <param name="typeId">カードの種別 ID(non-null)</param>
        /// <param name="instance">同一種別内でのインスタンス番号(0 以上)</param>
        /// <exception cref="ArgumentNullException">typeId が null</exception>
        /// <exception cref="ArgumentOutOfRangeException">instance が 0 未満</exception>
        public static CardId Of(CardTypeId typeId, int instance)
        {
            if (typeId is null)
            {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (instance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(instance),
                    "CardId.Instance は 0 以上である必要があります");
            }
            return new CardId(typeId, instance);
        }

        public override string ToString() => Value;
    }
}
