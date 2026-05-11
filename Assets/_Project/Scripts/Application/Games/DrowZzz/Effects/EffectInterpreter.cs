using System;

namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// DrowZzz のカード効果(<see cref="IEffect"/> 派生型)を <see cref="DrowZzzGameSession"/> に適用する純関数を提供する。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 設計詳細は ADR-0007 §1.3 を参照。<c>switch</c> で <see cref="IEffect"/> 派生型をマッチングし、
    /// 各 case で session 遷移を返す。M2-PR1 段階では具体的な派生型は未追加(<see cref="IEffect"/> は空 marker)であり、
    /// 全ての <see cref="Apply"/> 呼び出しは <c>_</c> ケースを経由して <see cref="NotImplementedException"/> を投げる
    /// (具体 effect record の case 追加は M2-PR2 以降で 1 PR = 1 effect 種別、ADR-0007 §6)。
    /// </para>
    /// <para>
    /// 例外型の選択根拠(ADR-0007 §1.3):
    /// <list type="bullet">
    /// <item><b>実装漏れ(library 作者の switch case 追加忘れ)</b> = <see cref="NotImplementedException"/></item>
    /// <item><b>利用側の不正(state を確認せずに違法操作を要求)</b> = <see cref="InvalidOperationException"/></item>
    /// </list>
    /// 本クラスの <c>_</c> ケースは前者(library 内の case 追加漏れ)に該当するため <see cref="NotImplementedException"/>。
    /// <see cref="DrowZzzRule"/> の <c>_</c> ケースと整合させている。
    /// </para>
    /// </remarks>
    public sealed class EffectInterpreter
    {
        /// <summary>
        /// 与えられた <paramref name="session"/> 状態に <paramref name="effect"/> を適用した次セッションを返す。
        /// 副作用なしの純関数。
        /// </summary>
        /// <param name="session">適用前のセッション(完全状態 / オラクルビュー)</param>
        /// <param name="effect">適用する効果</param>
        /// <returns>適用後のセッション</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="session"/> または <paramref name="effect"/> が null(APP-033 / APP-034)
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// <paramref name="effect"/> の派生型に対応する <c>switch</c> case が未実装(APP-035、将来効果追加用の防御)。
        /// 例外メッセージには runtime 型名が含まれる。
        /// </exception>
        public DrowZzzGameSession Apply(DrowZzzGameSession session, IEffect effect)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (effect is null)
            {
                throw new ArgumentNullException(nameof(effect));
            }
            return effect switch
            {
                // M2-PR2 以降で具体 effect record の case を追加していく(ADR-0007 §6)。
                _ => throw new NotImplementedException(
                    $"EffectInterpreter.Apply ({effect.GetType().Name}) は M2-PR1 範囲では到達不可。将来 IEffect 派生型を追加する PR で対応する"),
            };
        }
    }
}
