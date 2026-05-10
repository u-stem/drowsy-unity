using System.Collections.Generic;
using Drowsy.Domain.Cards;

namespace Drowsy.Application
{
    /// <summary>
    /// <see cref="CardId"/> から <see cref="CardData"/> を引く責務を持つ Application 層 interface。
    /// 実装は M1 では in-memory スタブ(<c>InMemoryCardCatalog</c>、M1-PR2)、
    /// M2 以降で <c>ScriptableObject</c> ベース(<c>Drowsy.Infrastructure</c> 配下)を予定。
    /// </summary>
    /// <remarks>
    /// 詳細は ADR-0006 §1.3 を参照。
    /// </remarks>
    public interface ICardCatalog
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
    }
}
