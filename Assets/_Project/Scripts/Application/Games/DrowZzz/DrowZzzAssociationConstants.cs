namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz の連想機構関連の L2 数学的・ゲームルール不変量を集約する static 定数クラス。
    /// </summary>
    /// <remarks>
    /// ADR-0011 §1 で確定した連想機構の発動条件(<see cref="AssociationThreshold"/>)を集約。
    /// `DrowZzzClockConstants` / `DdpPoolConstants` / `DrowZzzVictoryConstants` / `DrowZzzBedConstants` と
    /// 同パターンで、意味グループ単位でファイル分離する方針を継続(ADR-0008 / ADR-0009 / ADR-0010 §9 / ADR-0011 §3 と整合)。
    /// <para>
    /// 本クラスの定数は **ゲーム設計の核心値** で `IGameConfig` のバランス調整値とは性質が違う
    /// (ADR-0011 §1 / CLAUDE.md §9)。L2 不変量として constants 単一情報源(SSOT)で扱う。
    /// </para>
    /// </remarks>
    public static class DrowZzzAssociationConstants
    {
        /// <summary>
        /// 連想(<see cref="AssociateAction"/>)発動の <c>TotalPoints</c> 下限閾値。
        /// 現プレイヤーの <c>TotalPoints</c> がこの値以上の場合のみ連想を宣言可能(JIT 確定 2026-05-13:80 以上)。
        /// </summary>
        /// <remarks>
        /// ADR-0011 §1 起票時点では「FDS 80 超」(81+) か「80 以上」(80+) かが JIT 確認待ち項目だったが、
        /// 2026-05-13 のプロジェクトオーナー JIT 確認で **80 以上(80+ で発動可)** が確定。
        /// 「FDS」は ADR-0011 §1 / §6 の文脈で <see cref="DrowZzzGameSession.TotalPoints"/>
        /// (= FDP + DDP + SDP)を指す用語規約。
        /// <para>
        /// L2 不変量である根拠:連想機構の「存在条件」(FDS が一定値以上であること)は **ゲームルールとして固定された前提**
        /// であり、デザイナーが任意に調整するバランス値ではない(連想は「特殊な手段」という ADR-0011 §1 / 引き継ぎ JIT 共有原文の
        /// セマンティクス自体を支える境界値)。具体的な「80」という数値は将来 ADR で更新される可能性はあるが、その更新は
        /// 「バランス調整」ではなく「ルール改定」として ADR 起票を伴う(L3 = `IGameConfig` のように Designer-friendly な
        /// バランス調整経路には乗らない)。
        /// </para>
        /// </remarks>
        public const int AssociationThreshold = 80;
    }
}
