namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// DrowZzz のキーワード能力(<see cref="KeywordedEffect"/> で効果単位に付与する属性)。
    /// </summary>
    /// <remarks>
    /// ADR-0011 §4 で確定したキーワード能力 3 種を表現する enum。M3-PR5a で全 3 値を導入するが、
    /// M3-PR5a 範囲で実際に機能を有効化するのは <see cref="Instinct"/> のみで、
    /// <see cref="Frenzy"/> / <see cref="Counter"/> は enum 値の宣言のみ(機能は M3-PR5b / PR5c で順次追加)。
    /// <para>
    /// 「効果単位の付与」「将来未開示キーワードで拡張前提」(ADR-0011 §4 JIT 確定 2026-05-12)を背景に、
    /// 本 enum は **拡張順序**(declaration order = 0:Frenzy / 1:Instinct / 2:Counter)を持つが、
    /// 数値による比較に意味付けはせず、命名された値で switch / equality 判定するのが正規利用。
    /// </para>
    /// <para>
    /// 将来 JIT 共有された未開示キーワードを追加する場合は enum 末尾に新規値を追加し
    /// (既存値の順序は維持 = serialize 互換性確保)、対応する処理を <see cref="EffectInterpreter"/> /
    /// <c>DrowZzzRule</c> に追加する(ADR-0011 §4「`KeywordedEffect.Keywords` を `IReadOnlyList&lt;Keyword&gt;` で持つ
    /// 設計は新規キーワード追加に対して既存 effect record の switch case が破壊されない」と整合)。
    /// </para>
    /// </remarks>
    public enum Keyword
    {
        /// <summary>
        /// 狂乱:「反撃を受けない」効果属性(ADR-0011 §4.1)。M3-PR5a では enum 値のみ宣言、機能化は M3-PR5b 以降
        /// (<see cref="Counter"/> 機構との連動で意味を持つため、Counter 実装と同時期に有効化される)。
        /// </summary>
        Frenzy,

        /// <summary>
        /// 本能:「手段の放棄を受け付けない」効果属性(ADR-0011 §4.2)。M3-PR5a で機能化:
        /// <see cref="KeywordedEffect"/> で本値を付与した効果を含むカードは、<c>AbandonAction.CardIndex</c> の
        /// 捨て対象から除外される(<c>DrowZzzRule.IsLegalAbandon</c> で判定)。
        /// </summary>
        Instinct,

        /// <summary>
        /// 反撃:「相手のカードを無効化する」効果属性(ADR-0011 §4.3)。M3-PR5a では enum 値のみ宣言、機能化は M3-PR5b
        /// (`CounterAction` / `WaitingForCounterResponse` PhaseState 追加)。
        /// </summary>
        Counter,
    }
}
