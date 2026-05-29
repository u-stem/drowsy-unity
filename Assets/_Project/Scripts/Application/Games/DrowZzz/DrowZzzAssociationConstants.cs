namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の連想機構関連の L2 数学的・ゲームルール不変量を集約する static 定数クラス。
    /// </summary>
    /// <remarks>
    /// 連想機構の発動条件(<see cref="AssociationThreshold"/>)を集約。
    /// `DrowZzzClockConstants` / `DdpPoolConstants` / `DrowZzzVictoryConstants` / `DrowZzzBedConstants` と
    /// 同パターンで、意味グループ単位でファイル分離する。
    /// <para>
    /// 本クラスの定数はゲーム設計の核心値で `IGameConfig` のバランス調整値とは性質が違う(L2 不変量)。
    /// 変更時はゲームルール改定として扱う。
    /// </para>
    /// </remarks>
    public static class DrowZzzAssociationConstants
    {
        /// <summary>
        /// 連想(<see cref="AssociateAction"/>)発動の <c>TotalPoints</c> 下限閾値。
        /// 現プレイヤーの <c>TotalPoints</c> がこの値以上の場合のみ連想を宣言可能(80 以上)。
        /// </summary>
        /// <remarks>
        /// 「FDS」は <see cref="DrowZzzGameSession.TotalPoints"/>(= FDP + DDP + SDP)を指す用語規約。
        /// <para>
        /// L2 不変量である根拠:連想機構の「存在条件」(FDS が一定値以上であること)はゲームルールとして固定された前提であり、
        /// デザイナーが任意に調整するバランス値ではない。変更はルール改定として扱い、L3 = `IGameConfig` のように
        /// Designer-friendly なバランス調整経路には乗らない。
        /// </para>
        /// </remarks>
        public const int AssociationThreshold = 80;
    }
}
