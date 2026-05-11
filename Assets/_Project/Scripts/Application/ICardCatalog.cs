using System.Collections.Generic;
using Drowsy.Domain.Cards;

namespace Drowsy.Application
{
    /// <summary>
    /// <see cref="CardId"/> から <see cref="CardData"/> と効果列 (<typeparamref name="TEffect"/>) を引く責務を持つ汎用 Application 層 interface。
    /// <typeparamref name="TEffect"/> はゲーム固有の効果型を表す型引数。
    /// </summary>
    /// <typeparam name="TEffect">ゲーム固有の効果型(reference 型)。DrowZzz では <c>Drowsy.Application.Games.DrowZzz.Effects.IEffect</c>。</typeparam>
    /// <remarks>
    /// <para>
    /// 実装は M1 では in-memory スタブ(<c>InMemoryCardCatalog</c>、M1-PR2)を採用し、
    /// 永続化と一緒に SO 化を判断する M4 で <c>Drowsy.Infrastructure</c> 配下に
    /// <c>ScriptableObjectCardCatalog</c> 系を追加予定(ADR-0007 §5 / §M2-PR1)。
    /// ADR-0006 §1.3 の「M2 で SO 化」記載は ADR-0007 §5 で M4 に変更済。
    /// </para>
    /// <para>
    /// ジェネリック化の根拠(ADR-0007 §2): 汎用 interface に DrowZzz 固有型 (<c>IEffect</c>) を露出させず、
    /// 将来別ゲームが <c>ICardCatalog&lt;IOtherEffect&gt;</c> で自分の効果型を選べる形を維持する。
    /// 案 A(本ジェネリック)を採用、案 B(別 interface 分離) / 案 C(<c>IReadOnlyList&lt;object&gt;</c>) / 案 D(DrowZzz namespace 移動)は却下。
    /// </para>
    /// </remarks>
    public interface ICardCatalog<TEffect>
        where TEffect : class
    {
        /// <summary>
        /// 登録済 <paramref name="id"/> に対応する <see cref="CardData"/> を返す。
        /// </summary>
        /// <exception cref="KeyNotFoundException"><paramref name="id"/> が未登録の場合</exception>
        CardData Get(CardId id);

        /// <summary>
        /// 登録済 <paramref name="id"/> なら <c>true</c> を返し <paramref name="data"/> に <see cref="CardData"/> を設定する。
        /// 未登録なら <c>false</c> を返し <paramref name="data"/> には <c>null</c>(= <c>default(CardData)</c>)を設定する。
        /// </summary>
        /// <remarks>
        /// 宣言型は <c>out CardData</c>(non-nullable)だが、未登録時の <paramref name="data"/> は契約上 <c>null</c> になる。
        /// Phase 2 時点では NRT (Nullable Reference Types) 未有効化のため <c>?</c> を付けない宣言にしている。
        /// NRT 有効化時(<c>docs/todo.md</c> 「NRT 検討」)に <c>out CardData?</c> へ昇格させる。
        /// </remarks>
        bool TryGet(CardId id, out CardData data);

        /// <summary>
        /// 登録済 <paramref name="id"/> に対応する効果列を返す。未登録 / 効果なしの場合は空列。
        /// </summary>
        /// <param name="id">対象 <see cref="CardId"/>(non-null)</param>
        /// <returns>効果の <see cref="IReadOnlyList{T}"/>(順序は左から順に評価される、ADR-0007 §3)</returns>
        /// <remarks>
        /// 戻り値は防御コピーされた immutable 列であり、呼び出し側は変更を試みない。
        /// 効果のない CardId(M2-PR1 段階では全カード)に対しては空列を返し、呼び出し側の
        /// <c>Enumerable.Aggregate(seed, func)</c>(seed 付き overload)等で 0 個ループとして自然に扱える
        /// (M1 互換、ADR-0007 §3 末尾)。
        /// </remarks>
        IReadOnlyList<TEffect> GetEffects(CardId id);
    }
}
