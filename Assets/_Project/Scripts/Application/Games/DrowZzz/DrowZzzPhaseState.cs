namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz のフェーズ内の待ち状態(Application 層が管理するフェーズ内ステートマシン)。
    /// Phase 1 <see cref="Drowsy.Domain.Game.TurnState"/> には影響を与えない。
    /// </summary>
    /// <remarks>
    /// 用語規約(ADR-0009 §「用語規約」):
    /// <list type="bullet">
    /// <item><b>ターン</b>(30 分 = 全プレイヤー 1 巡 = <see cref="DrowZzzClock.RoundNumber"/> の単位)</item>
    /// <item><b>フェーズ</b>(1 プレイヤー 1 行動 = <see cref="Drowsy.Domain.Game.TurnState.TurnNumber"/> の単位)</item>
    /// <item><b>PhaseState</b>(フェーズ内の待ち状態 = 本 enum、WaitingForDraw / WaitingForPlay / WaitingForEndTurn / WaitingForCounterResponse)</item>
    /// </list>
    /// 詳細は ADR-0006 §2.3(旧名 <c>DrowZzzTurnPhase</c> の導入経緯)/ ADR-0009 §「用語規約」(本 enum への改名根拠)を参照。
    /// <para>
    /// M3-PR5b で <see cref="WaitingForCounterResponse"/> を追加(ADR-0011 §4.3.3 / JIT 確定 2026-05-13:候補 (i) 採用)。
    /// 既存 ADR-0006「自分のターン中のみカードプレイ可能」原則を拡張し、相手プレイ直後の **反撃機会フェーズ** を表現する。
    /// declaration order は既存値の順序を保持して serialize 互換性を確保(末尾に追加)。
    /// </para>
    /// </remarks>
    public enum DrowZzzPhaseState
    {
        /// <summary>ドロー待ち。<c>DrawCardAction</c> のみが合法。</summary>
        WaitingForDraw,

        /// <summary>カードプレイ待ち。<c>PlayCardAction(card)</c> のみが合法(card は手札に存在する必要あり)。</summary>
        WaitingForPlay,

        /// <summary>フェーズ終了待ち。<c>EndTurnAction</c> のみが合法。</summary>
        WaitingForEndTurn,

        /// <summary>
        /// 反撃応答待ち。M3-PR5b で追加(ADR-0011 §4.3.3、JIT 確定 2026-05-13:候補 (i) 採用)。
        /// 「相手プレイヤーが直前にプレイしたカードに対して、本プレイヤーが反撃(<c>CounterAction</c>)を打つか
        /// 反撃なしで進行(<c>PassCounterAction</c>)するかを応答する」フェーズ。
        /// </summary>
        /// <remarks>
        /// 遷移条件(<c>DrowZzzRule.ApplyPlayCard</c>):相手プレイヤーの手札に Counter キーワード持ち効果列を含む
        /// カードが 1 枚以上ある場合、PlayCardAction 後の <see cref="WaitingForEndTurn"/> 遷移を本フェーズに置き換える
        /// (相手の Counter 持ち手札 0 枚なら従来通り <see cref="WaitingForEndTurn"/> に直接遷移、既存テスト破壊回避)。
        /// 合法 action:<c>CounterAction(CardId Counter, CardId Target)</c> / <c>PassCounterAction</c>。
        /// </remarks>
        WaitingForCounterResponse,
    }
}
