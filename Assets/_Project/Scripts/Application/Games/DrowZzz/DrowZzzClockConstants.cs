namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// <see cref="DrowZzzClock"/> が依存する DrowZzz 固有ゲーム定数を集約する。
    /// </summary>
    /// <remarks>
    /// CLAUDE.md §9「マジックナンバー禁止」/「L1/L2 は `&lt;Module&gt;Constants` クラスの `const`」に従い、
    /// <see cref="DrowZzzClock"/> 内に直書きされていた数値リテラル(21 / 24 / 30 / 2 / 1 / 16 / 17 / 21)を
    /// 命名された <c>const</c> として切り出す。Domain ではなく Application 層に配置するのは、
    /// 21:00 開始 / 1 ターン 30 分 / 21 ターン構成が DrowZzz 固有のゲームルールであり Domain ゲーム非依存原則
    /// (ADR-0002)と整合させるため(ADR-0008 §Alternatives「Clock を Domain 配下に配置」却下と同じ判断)。
    /// <para>
    /// 階層分類は <c>docs/architecture/constants-management.md</c> 参照:
    /// <list type="bullet">
    /// <item><b>L1</b>(数学的・物理的不変量): <see cref="HoursPerDay"/></item>
    /// <item><b>L2</b>(ドメイン上の真の不変量): その他 7 件 — 21:00 開始 / 1 フェーズ 30 分 / N=2 フェーズ /
    ///       夜 1〜16 / 朝 17〜21 はすべて DrowZzz の真の不変量で、ゲームバランス調整 (L3) で動かす値ではない</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class DrowZzzClockConstants
    {
        /// <summary>ゲーム開始時刻 (時、24 時間制)。21:00 開始(ADR-0008 §Context、L2)。</summary>
        public const int StartHour = 21;

        /// <summary>1 日の時数(24 時間制 mod 計算用、L1 数学的不変量)。</summary>
        public const int HoursPerDay = 24;

        /// <summary>1 フェーズあたりの分数(L2、1 ターン = N=2 フェーズ × 30 分 = 60 分)。</summary>
        public const int MinutesPerPhase = 30;

        /// <summary>1 ターンあたりのフェーズ数(L2、N=2 想定、ADR-0006 §Negative — N&gt;2 は Phase 3 候補)。</summary>
        public const int PhasesPerRound = 2;

        /// <summary>夜の開始ターン番号(21:00 = Round 1、ADR-0008 §Context、L2)。</summary>
        public const int NightStartRound = 1;

        /// <summary>夜の終了ターン番号(04:30 = Round 16、ADR-0008 §Context / ADR-0009 §「Clock 仕様の境界訂正」、L2)。</summary>
        public const int NightEndRound = 16;

        /// <summary>朝の開始ターン番号(05:00 = Round 17、ADR-0008 §Context、L2)。</summary>
        public const int MorningStartRound = 17;

        /// <summary>朝の終了ターン番号(07:00 = Round 21、最終プレイ可能ターン、ADR-0009 §「Clock 仕様の境界訂正」、L2)。</summary>
        public const int MorningEndRound = 21;

        /// <summary>
        /// ゲーム終了判定で参照する最大ターン番号(= <see cref="MorningEndRound"/> の別名、L2)。
        /// ターン上限到達時に <c>DrowZzzRule.ApplyEndTurn</c> 内で <see cref="Drowsy.Domain.Game.GameOutcome"/> が確定する
        /// (ADR-0010 §4(b) / §8)。意味的には「最終プレイ可能ターン」と同値だが、ゲーム終了判定の文脈で
        /// 「Clock 仕様の境界」(<see cref="MorningEndRound"/>)とは別の概念として参照できるよう独立した定数として公開する。
        /// </summary>
        public const int MaxRoundNumber = MorningEndRound;
    }
}
