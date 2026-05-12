namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の勝利条件関連の L2 数学的・ゲームルール不変量を集約する static 定数クラス。
    /// </summary>
    /// <remarks>
    /// ADR-0010 §9 で「`EarlyWinScoreThreshold` の所在」として新設。`DrowZzzClockConstants`(時刻・ラウンド変換の物理的不変量)/
    /// `DdpPoolConstants`(DDP プール構造の不変量)と同パターンで、意味グループ単位でファイル分離する方針を継続。
    /// <para>
    /// 本クラスの定数は **ゲーム設計の核心値** で `IGameConfig` のバランス調整値とは性質が違う(ADR-0010 §9 / CLAUDE.md §9)。
    /// L2 不変量として constants 単一情報源(SSOT)で扱う。
    /// </para>
    /// </remarks>
    public static class DrowZzzVictoryConstants
    {
        /// <summary>
        /// 早期勝利成立に必要な持ち点(<c>TotalPoints</c>)の閾値。
        /// 夜の間(<see cref="DrowZzzClock.IsNight"/>)にこの値以上で就寝カード(<see cref="Effects.EarlyWinTriggerEffect"/>
        /// を効果列に持つカード)をプレイすると早期勝利が成立する(ADR-0009 §「コンセプト」/ ADR-0010 §4 / §5)。
        /// </summary>
        public const int EarlyWinScoreThreshold = 100;
    }
}
