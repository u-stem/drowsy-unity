namespace Drowsy.Application.Games.DrowZzz.Effects
{
    /// <summary>
    /// 早期勝利トリガー効果。フィールドなしのマーカー的 record。本効果を効果列に持つカードを
    /// **「就寝カード」** と定義する(ADR-0007 §1「カード効果は <c>IEffect</c> で表現」と整合)。
    /// </summary>
    /// <remarks>
    /// ADR-0010 §5 で M3-PR1 として新設。<see cref="EffectInterpreter.Apply"/> 評価時に以下の条件すべてを満たすと
    /// セッションの <see cref="DrowZzzGameSession.Outcome"/> に
    /// <see cref="Drowsy.Domain.Game.WinnerOutcome"/>(現プレイヤー)を設定する:
    /// <list type="bullet">
    /// <item><see cref="DrowZzzGameSession.Clock"/>.<see cref="DrowZzzClock.IsNight"/> が <c>true</c>(Round 1〜16)</item>
    /// <item>現プレイヤーの <see cref="DrowZzzGameSession.TotalPoints"/> が <see cref="DrowZzzVictoryConstants.EarlyWinScoreThreshold"/> 以上</item>
    /// </list>
    /// いずれかの条件が満たされない場合は no-op(session 不変返却、カードプレイ自体は完了する)。
    /// <para>
    /// 閾値(100)は本 record 自身ではなく <see cref="DrowZzzVictoryConstants.EarlyWinScoreThreshold"/> で集約する
    /// (ADR-0010 §5 / §9、全就寝カード共通のゲームルール定数として constants 化)。
    /// </para>
    /// <para>
    /// 最初の利用カード(就寝カード No.X)は本 PR では確定しない。カード仕様は M2-PR6+ で JIT 共有される運用
    /// (ADR-0010 §「Implementation Notes」/ Neutral)。
    /// </para>
    /// </remarks>
    public sealed record EarlyWinTriggerEffect : IEffect;
}
