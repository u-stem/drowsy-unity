namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// DrowZzz のカード効果を表すマーカー interface。具体形は <c>record</c> で実装する
    /// (Discriminated Union 風表現を「マーカー interface + sealed record 階層」で代替する設計、ADR-0006 §1.1 と対称)。
    /// </summary>
    /// <remarks>
    /// <para>
    /// M2-PR1 段階では空の marker(派生型 0 個)として導入する。M2-PR2 以降で
    /// 1 PR = 1 効果 record(例: <c>DrawCardsEffect</c>)を順次追加する(ADR-0007 §1.2 / §6)。
    /// </para>
    /// <para>
    /// 設計指針(ADR-0007 §1.2 / ADR-0006 §M1 進行中の学び):
    /// <list type="bullet">
    /// <item><c>record class</c> + <c>init</c> setter で immutability を確保(ADR-0004 polyfill 前提)</item>
    /// <item>positional 引数を持つ record は null 防御の <b>二重ガードパターン</b>(バッキングフィールド初期化式
    /// + init setter 本体の両方で <c>value ?? throw</c>)が必須</item>
    /// <item>内部に <c>Dictionary&lt;K, V&gt;</c> / <c>List&lt;T&gt;</c> を持つ場合は <c>Equals</c> / <c>GetHashCode</c> の override が必須</item>
    /// </list>
    /// </para>
    /// <para>
    /// 評価は <see cref="EffectInterpreter"/> が担う(<c>EffectInterpreter.Apply(session, effect)</c>)。
    /// 効果の発動タイミングは <c>DrowZzzRule.PlayCardAction.Apply</c> 中に同期発動(ADR-0007 §3)。
    /// </para>
    /// </remarks>
    public interface IEffect
    {
    }
}
