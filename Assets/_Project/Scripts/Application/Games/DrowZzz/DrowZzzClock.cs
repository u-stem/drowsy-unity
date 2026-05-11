namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz のゲーム内時計を表す値オブジェクト。
    /// 「1 ターン = 30 分」「開始 21:00」「全 21 ターン」「夜 = Turn 1〜16(21:00〜04:30)」「朝 = Turn 17〜21(05:00〜07:00)」
    /// (ADR-0008 / ADR-0009 で確定)を `RoundNumber` から派生する computed プロパティとして提供する。
    /// </summary>
    /// <param name="RoundNumber">
    /// ゲーム内ターン番号(1 起点)。<see cref="DrowZzzGameSession.Clock"/> から
    /// <see cref="DrowZzzGameSession.CurrentRound"/> と同義の値が渡される(ADR-0008 §2、案 X 採用)。
    /// </param>
    /// <remarks>
    /// 設計指針(ADR-0008 §1):
    /// <list type="bullet">
    /// <item>真の状態は <c>RoundNumber</c> のみ。<c>Hour</c> / <c>Minute</c> / <c>IsNight</c> / <c>IsMorning</c> は
    ///       computed で導出し、複数情報源による不整合を構造的に排除する(Parse-don't-validate)。</item>
    /// <item>24 時間制 mod 24 で表記する(例: Round 7 = 00:00、24:00 ではない)。表示時の可読性優先。</item>
    /// <item><c>RoundNumber &gt; 21</c> は時計仕様上存在しないが、M3 で <c>IGameRule.IsTerminated</c> が
    ///       明示的にガードするまでの過渡期間は computed プロパティとして数学的計算結果を返す。
    ///       <c>IsNight</c> / <c>IsMorning</c> は両方 <c>false</c> を返すことで「夜でも朝でもない」防御値となる
    ///       (ADR-0008 §5 / ADR-0009 §6)。</item>
    /// <item>「1 ターン = 30 分 = N=2 フェーズ」の前提に基づくため、N&gt;2 拡張時は計算式の再評価が必要
    ///       (Phase 3 候補、ADR-0006 §Negative 継承)。</item>
    /// </list>
    /// 用語規約(ADR-0009 §「用語規約」):「ターン (Turn)」= 30 分の大単位、本クラスの <c>RoundNumber</c> に対応。
    /// 実装名 <c>RoundNumber</c> から <c>TurnNumber</c> への改名は別 PR で機械的に追跡(<c>docs/todo.md</c>)。
    /// <para>
    /// ADR-0008 §1 のサンプル表記は <c>sealed record class</c> だったが、これは C# 10 構文。
    /// <c>Drowsy.Application.csproj</c> の <c>&lt;LangVersion&gt;9.0&lt;/LangVersion&gt;</c> および既存
    /// <see cref="DrowZzzGameSession"/> / <c>StartGameAction</c> 等の C# 9 <c>sealed record</c> パターンに合わせ
    /// 本実装では <c>sealed record</c> を採用する(意味は同一、ADR-0008 §1 を本 PR で訂正反映)。
    /// </para>
    /// </remarks>
    public sealed record DrowZzzClock(int RoundNumber)
    {
        // 注: 各 literal は <see cref="DrowZzzClockConstants"/> の const に切り出し済(CLAUDE.md §9
        // 「マジックナンバー禁止」/「L1/L2 は `<Module>Constants` クラスの `const`」)。
        // `RoundNumber - 1` の `1` のみ「1-indexed → 0-indexed の自明変換」として §9 例外を適用し literal で残す。

        /// <summary>時(0〜23、24 時間制 mod で正規化)。</summary>
        public int Hour =>
            (DrowZzzClockConstants.StartHour
             + (RoundNumber - 1) / DrowZzzClockConstants.PhasesPerRound)
            % DrowZzzClockConstants.HoursPerDay;

        /// <summary>分(0 または <see cref="DrowZzzClockConstants.MinutesPerPhase"/>)。</summary>
        public int Minute =>
            ((RoundNumber - 1) % DrowZzzClockConstants.PhasesPerRound)
            * DrowZzzClockConstants.MinutesPerPhase;

        /// <summary>夜判定(21:00 〜 04:30、Turn 1〜16)。</summary>
        public bool IsNight =>
            RoundNumber
                is >= DrowZzzClockConstants.NightStartRound
                and <= DrowZzzClockConstants.NightEndRound;

        /// <summary>朝判定(05:00 〜 07:00、Turn 17〜21。Turn 21 = 07:00 は最終プレイ可能ターン)。</summary>
        public bool IsMorning =>
            RoundNumber
                is >= DrowZzzClockConstants.MorningStartRound
                and <= DrowZzzClockConstants.MorningEndRound;
    }
}
