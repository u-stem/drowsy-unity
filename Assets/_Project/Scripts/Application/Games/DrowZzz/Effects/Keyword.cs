namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// DrowZzz のキーワード能力(<see cref="KeywordedEffect"/> で効果単位に付与する属性)。
    /// </summary>
    /// <remarks>
    /// DrowZzz のキーワード能力を表現する enum。
    /// <para>
    /// 本 enum は **拡張順序**(declaration order = 0:Frenzy / 1:Instinct / 2:Counter)を持つが、
    /// 数値による比較に意味付けはせず、命名された値で switch / equality 判定するのが正規利用。
    /// </para>
    /// <para>
    /// 将来未開示キーワードを追加する場合は enum 末尾に新規値を追加し
    /// (既存値の順序は維持 = serialize 互換性確保)、対応する処理を <see cref="EffectInterpreter"/> /
    /// <c>DrowZzzRule</c> に追加する(`KeywordedEffect.Keywords` を `IReadOnlyList&lt;Keyword&gt;` で持つ
    /// 設計は新規キーワード追加に対して既存 effect record の switch case が破壊されない)。
    /// </para>
    /// </remarks>
    public enum Keyword
    {
        /// <summary>
        /// 狂乱:「反撃を受けない」効果属性。<see cref="Counter"/> 機構との連動で意味を持つ。
        /// </summary>
        Frenzy,

        /// <summary>
        /// 本能:「手段の放棄を受け付けない」効果属性。
        /// <see cref="KeywordedEffect"/> で本値を付与した効果を含むカードは、<c>AbandonAction.CardIndex</c> の
        /// 捨て対象から除外される(<c>DrowZzzRule.IsLegalAbandon</c> で判定)。
        /// </summary>
        Instinct,

        /// <summary>
        /// 反撃:「相手のカードを無効化する」効果属性。
        /// `CounterAction` / `WaitingForCounterResponse` PhaseState と連動する。
        /// </summary>
        Counter,

        /// <summary>
        /// Echo(反響):「受けている影響を 1 つ選び、その発生源カードを再使用する」効果属性(No.18 で初導入)。
        /// 判別用属性で評価時は副作用なし(<see cref="KeywordedEffect"/> 経由で <c>Inner</c> を逐次評価するのみ)。
        /// 本 enum 値は将来同種カード追加時の汎用判別用に <c>HasKeywordInEffects(effects, Echo)</c> で利用可能。
        /// 実際の Reuse 動作は <see cref="ReuseInfluenceSourceEffect"/> + <c>DrowZzzRule.ApplyPlayCard</c> の専用ヘルパーで実装。
        /// </summary>
        Echo,
    }
}
