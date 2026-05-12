using System;
using Drowsy.Domain.Players;

namespace Drowsy.Domain.Game
{
    /// <summary>
    /// ゲーム終了状態を表す値オブジェクト。<see cref="WinnerOutcome"/>(勝者あり)/ <see cref="DrawOutcome"/>(引き分け)
    /// のいずれかで派生する sealed record 階層。
    /// </summary>
    /// <remarks>
    /// ADR-0010 §2 で新設。Domain 層に配置する理由は「勝者がいる / 引き分け」というゲーム終了状態が
    /// ゲーム非依存の汎用概念であり、<see cref="Drowsy.Application.IGameRule{TAction, TSession}"/> の generic 性質と整合するため
    /// (ADR-0002 Domain ゲーム非依存原則と矛盾しない)。
    /// <para>
    /// 「未終了」は <c>GameOutcome?</c> の <c>null</c> で表現し、本階層には派生型を作らない:
    /// 「ゲームが終わっている事実」と「終わり方」を <see cref="GameOutcome"/> 階層に閉じ込め、
    /// 「終わっていない事実」は <c>null</c> という C# 言語機能で表現する方が API が単純(ADR-0010 §1 / §2)。
    /// </para>
    /// </remarks>
    public abstract record GameOutcome;

    /// <summary>
    /// 特定プレイヤーが勝者として確定したゲーム終了状態。
    /// </summary>
    /// <param name="Winner">勝者の <see cref="PlayerId"/>。null 不可。</param>
    /// <remarks>
    /// null 防御の二重ガード(positional ctor 経由のバッキングフィールド初期化式 + init setter 本体)を採用
    /// (M1 進行中に確立した record + positional + 二重ガードパターン、ADR-0006 §M1)。
    /// Application 層の <c>PlayCardAction</c> / <c>PlayerInfluence</c> 等も同パターン。
    /// </remarks>
    public sealed record WinnerOutcome(PlayerId Winner) : GameOutcome
    {
        private readonly PlayerId _winner = Winner ?? throw new ArgumentNullException(nameof(Winner));

        /// <summary>勝者の <see cref="PlayerId"/>。null 不可。</summary>
        public PlayerId Winner
        {
            get => _winner;
            init => _winner = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// 引き分けで確定したゲーム終了状態。両プレイヤーの持ち点(<c>TotalPoints</c>)が等値の場合に用いる。
    /// </summary>
    /// <remarks>
    /// フィールドなしのマーカー的派生型。record auto-equals により <see cref="DrawOutcome"/> 同士は常に等価。
    /// ADR-0010 §7 で「引き分けは tiebreaker なし」と確定したため、本 record は付帯情報を持たない。
    /// </remarks>
    public sealed record DrawOutcome : GameOutcome;
}
