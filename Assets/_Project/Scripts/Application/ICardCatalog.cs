using System.Collections.Generic;
using Drowsy.Domain.Cards;

namespace Drowsy.Application
{
    /// <summary>
    /// <see cref="CardTypeId"/> から <see cref="CardData"/> と効果列 (<typeparamref name="TEffect"/>) を引く責務を持つ汎用 Application 層 interface。
    /// <typeparamref name="TEffect"/> はゲーム固有の効果型を表す型引数。
    /// </summary>
    /// <typeparam name="TEffect">ゲーム固有の効果型(reference 型)。DrowZzz では <c>Drowsy.Application.Games.DrowZzz.Effects.IEffect</c>。</typeparam>
    /// <remarks>
    /// <para>
    /// 実装は in-memory スタブ(<c>InMemoryCardCatalog</c>)と
    /// <c>Drowsy.Infrastructure</c> 配下の <c>ScriptableObjectCardCatalog</c> 系がある。
    /// </para>
    /// <para>
    /// <b>API 引数型</b>:本 interface の引数型は <see cref="CardTypeId"/>(種別 ID)。
    /// <see cref="CardId"/> は Hand 内 instance unique な識別子に専念し、catalog 引き呼び出し側は <c>cardId.TypeId</c> を渡す。
    /// </para>
    /// <para>
    /// ジェネリック化の目的: 汎用 interface に DrowZzz 固有型 (<c>IEffect</c>) を露出させず、
    /// 将来別ゲームが <c>ICardCatalog&lt;IOtherEffect&gt;</c> で自分の効果型を選べる形を維持する。
    /// </para>
    /// </remarks>
    public interface ICardCatalog<TEffect>
        where TEffect : class
    {
        /// <summary>
        /// 登録済 <paramref name="typeId"/> に対応する <see cref="CardData"/> を返す。
        /// </summary>
        /// <exception cref="KeyNotFoundException"><paramref name="typeId"/> が未登録の場合</exception>
        CardData Get(CardTypeId typeId);

        /// <summary>
        /// 登録済 <paramref name="typeId"/> なら <c>true</c> を返し <paramref name="data"/> に <see cref="CardData"/> を設定する。
        /// 未登録なら <c>false</c> を返し <paramref name="data"/> には <c>null</c>(= <c>default(CardData)</c>)を設定する。
        /// </summary>
        /// <remarks>
        /// 宣言型は <c>out CardData</c>(non-nullable)だが、未登録時の <paramref name="data"/> は契約上 <c>null</c> になる。
        /// NRT (Nullable Reference Types) 未有効化のため <c>?</c> を付けない宣言にしている。
        /// </remarks>
        bool TryGet(CardTypeId typeId, out CardData data);

        /// <summary>
        /// 登録済 <paramref name="typeId"/> に対応する効果列を返す。未登録 / 効果なしの場合は空列。
        /// </summary>
        /// <param name="typeId">対象 <see cref="CardTypeId"/>(non-null)</param>
        /// <returns>効果の <see cref="IReadOnlyList{T}"/>(順序は左から順に評価される)</returns>
        /// <remarks>
        /// 戻り値は防御コピーされた immutable 列であり、呼び出し側は変更を試みない。
        /// 効果のない CardTypeId に対しては空列を返し、呼び出し側の
        /// <c>Enumerable.Aggregate(seed, func)</c>(seed 付き overload)等で 0 個ループとして自然に扱える。
        /// </remarks>
        IReadOnlyList<TEffect> GetEffects(CardTypeId typeId);

        /// <summary>
        /// 登録済の全 <see cref="CardTypeId"/> の列挙。
        /// </summary>
        /// <remarks>
        /// 既存 `ScriptableObjectCardCatalog.RegisteredCardTypeIds` の責務を interface 化したもの。
        /// `StartGameUseCase` のゲーム開始時 catalog 全 entry scan(`AssociateToFirstPlayerOnGameStartEffect` 検出)
        /// および `Bootstrap.BuildInitialDeck` の共通山札構築で利用される。
        /// 順序は実装依存(`InMemoryCardCatalog` は登録順、`ScriptableObjectCardCatalog` は entries の宣言順)。
        /// </remarks>
        IReadOnlyCollection<CardTypeId> RegisteredCardTypeIds { get; }
    }
}
