namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の勝利条件関連の L2 数学的・ゲームルール不変量を集約する static 定数クラス。
    /// </summary>
    /// <remarks>
    /// `DrowZzzClockConstants`(時刻・ラウンド変換の物理的不変量)/
    /// `DdpPoolConstants`(DDP プール構造の不変量)と同パターンで、意味グループ単位でファイル分離する。
    /// <para>
    /// 本クラスの定数はゲーム設計の核心値で `IGameConfig` のバランス調整値とは性質が違う(L2 不変量)。
    /// </para>
    /// </remarks>
    public static class DrowZzzVictoryConstants
    {
        /// <summary>
        /// 早期勝利成立に必要な持ち点(<c>TotalPoints</c>)の閾値。
        /// 夜の間(<see cref="DrowZzzClock.IsNight"/>)にこの値以上で就寝カード(<see cref="Effects.EarlyWinTriggerEffect"/>
        /// を効果列に持つカード)をプレイすると早期勝利が成立する。
        /// </summary>
        public const int EarlyWinScoreThreshold = 100;
    }
}
