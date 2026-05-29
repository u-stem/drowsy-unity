namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// SDP 操作系効果(<see cref="AdjustSdpEffect"/> / <see cref="DrawCardEffect"/> 等)が
    /// 「自分」「相手」のどちらを対象とするかを表す。
    /// </summary>
    /// <remarks>
    /// カード仕様「コップ一杯の脅威」が自分 / 相手の両方を変動させる必要があるため導入。設計選択:
    /// <list type="bullet">
    /// <item><b>採用</b>: enum を効果 record の positional 引数に取る(record 数最小、N=2 を超える拡張時にも
    ///       <c>enum DpTarget { Self, AllOpponents, Specific }</c> 等で広げやすい)</item>
    /// <item>不採用: actor ごとに record 分離(`AddSelfSdpEffect` / `AddOpponentSdpEffect`...)→ record 数が
    ///       指数関数的に膨らむため、SDP 以外の DP(FDP / DDP)操作が増えると破綻</item>
    /// <item>不採用: <c>EffectContext</c> + Target 解決を <see cref="EffectInterpreter"/> 内で行う → 過剰、
    ///       Target を効果 record の自己情報として保持する方が record の自己完結性が高い</item>
    /// </list>
    /// 「自分」「相手」をコード identifier として英語の <c>Self</c> / <c>Opponent</c> に統一する。
    /// <para>
    /// 名前は <c>SdpTarget</c> だが、SDP 操作以外の効果(<see cref="DrawCardEffect"/> 等)からも「対象プレイヤー」を
    /// 表す引数として流用している。将来 DDP / FDP 操作系効果が増えた時点で改名を再評価する。
    /// それまでは「DrowZzz の効果対象プレイヤー(N=2 で Self / Opponent の二択)」の意味で読む。
    /// </para>
    /// </remarks>
    public enum SdpTarget
    {
        /// <summary>自分(現プレイヤー、カードをプレイした側)。</summary>
        Self,

        /// <summary>相手(N=2 で現プレイヤー以外、カードをプレイされた側)。N>2 拡張は Phase 3 候補。</summary>
        Opponent,
    }
}
