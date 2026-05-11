namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// SDP 操作系効果(<see cref="AdjustSdpEffect"/> / <see cref="DrawCardEffect"/> 等)が
    /// 「自分(使用者)」「相手(被使用者)」のどちらを対象とするかを表す。
    /// </summary>
    /// <remarks>
    /// ADR-0007 §1.4「他者影響系 actor 拡張」の JIT 確定として M2-PR3 で導入(ADR-0009 §「DP 種別」のカード仕様
    /// 「コップ一杯の脅威」が使用者 / 被使用者の両方を変動させる必要があるため)。設計選択:
    /// <list type="bullet">
    /// <item><b>採用</b>: enum を効果 record の positional 引数に取る(record 数最小、N=2 を超える拡張時にも
    ///       <c>enum DpTarget { Self, AllOpponents, Specific }</c> 等で広げやすい)</item>
    /// <item>不採用: actor ごとに record 分離(`AddSelfSdpEffect` / `AddOpponentSdpEffect`...)→ record 数が
    ///       指数関数的に膨らむため、SDP 以外の DP(FDP / DDP)操作が増えると破綻</item>
    /// <item>不採用: <c>EffectContext</c> + Target 解決を <see cref="EffectInterpreter"/> 内で行う → M2 範囲では過剰、
    ///       Target を効果 record の自己情報として保持する方が record の自己完結性が高い</item>
    /// </list>
    /// 「使用者」「被使用者」(プロジェクトオーナー用語の「甲」「乙」)を、コード identifier として英語の
    /// <c>Self</c> / <c>Opponent</c> に統一する(CLAUDE.md §1 識別子英語規約、ADR-0006 §1.1 と整合)。
    /// <para>
    /// **本 enum の流用に関する注意(code-reviewer W-1 反映)**: 名前は <c>SdpTarget</c> だが、M2-PR3 段階では
    /// SDP 操作以外の効果(<see cref="DrawCardEffect"/> 等)からも「対象プレイヤー」を表す引数として流用している。
    /// 将来 DDP / FDP 操作系効果が増えた時点で、本 enum を「効果汎用の `EffectTarget`」へ改名するか、別 enum
    /// (`DdpTarget` 等)を新設するかを別 PR / ADR で再評価する(`docs/todo.md` で追跡)。それまでは
    /// 「DrowZzz の効果対象プレイヤー(N=2 で Self / Opponent の二択)」の意味で読む。
    /// </para>
    /// </remarks>
    public enum SdpTarget
    {
        /// <summary>カード使用者(現プレイヤー、「甲」)。</summary>
        Self,

        /// <summary>カード被使用者(N=2 で相手プレイヤー、「乙」)。N>2 拡張は Phase 3 候補(ADR-0006 §Negative)。</summary>
        Opponent,
    }
}
