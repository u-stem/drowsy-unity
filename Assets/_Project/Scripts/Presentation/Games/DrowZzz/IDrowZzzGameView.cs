using System;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;

namespace Drowsy.Presentation.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz ゲームの View 抽象(MVP の V)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §3.1「View interface」で確定。Presenter(Pure C#)から View への状態反映を
    /// <see cref="Render"/> / <see cref="RenderOutcome"/> で行い、View から Presenter への入力通知を
    /// C# event(<see cref="OnDrawClicked"/> / <see cref="OnPlayClicked"/> / <see cref="OnEndTurnClicked"/>)で行う。
    /// <para>
    /// <b>Domain 型を直接受け取る件</b>:本 M5 範囲では Domain 型(<see cref="CardId"/> /
    /// <see cref="DrowZzzGameSession"/> / <see cref="GameOutcome"/>)を View interface で直接受ける運用とする
    /// (<b>ADR-0016 §3.1 で M5 範囲の明示的な例外として承認</b>)。背景にある一般原則は ADR-0006 §4
    /// 「View に Domain エンティティを直接バインドせず Presenter / DTO 経由を推奨」だが、M5 完了基準
    /// (ADR-0005 §7「最小 UI / コンソール出力可」)に対して DTO レイヤ導入は過剰と判断した。
    /// Phase 3 で View が複雑化(複数 Panel / Animation / Localization)した時点で DTO 化を別 ADR で再評価する。
    /// </para>
    /// <para>
    /// <b>event 採用理由</b>:R3 <c>Subject&lt;T&gt;</c> でも可だが、View → Presenter の片方向通知に過剰、
    /// View MonoBehaviour 側のメモリリーク防止責務を <c>event += / -=</c> の対称運用で明示する方が
    /// Unity 文化に整合(<c>MonoBehaviour.OnDestroy</c> で <c>event -=</c> を行う)。
    /// </para>
    /// <para>
    /// <b>引数の null 性</b>:ADR-0015 NRT 不採用方針整合のため、 <see cref="Render"/> / <see cref="RenderOutcome"/>
    /// は non-null の引数を期待する。Presenter は <c>Subject&lt;T&gt;</c> 経由で「Boot 完了後の有効な状態のみ」を
    /// 発火するため、Render が null 引数で呼ばれる経路は構造的に発生しない(ADR-0016 §3.2 line 218〜)。
    /// </para>
    /// </remarks>
    public interface IDrowZzzGameView
    {
        /// <summary>
        /// 現セッション状態を View に反映する(山札残数 / 手札 / 場札 / 現プレイヤー / TurnPhase /
        /// SDP / FDP / DDP / Round 等)。本実装は M5-PR4 で確定。
        /// </summary>
        /// <param name="session">表示対象の session(non-null)</param>
        void Render(DrowZzzGameSession session);

        /// <summary>
        /// ゲーム終了結果(Winner / Draw)を View に反映する。本実装は M5-PR7 で確定。
        /// </summary>
        /// <param name="outcome">表示対象の outcome(non-null)</param>
        void RenderOutcome(GameOutcome outcome);

        /// <summary>Draw ボタン押下通知。Presenter は <c>HandleDrawClicked</c> で購読(M5-PR4 で実装)。</summary>
        event Action OnDrawClicked;

        /// <summary>Play ボタン押下通知。Presenter は <c>HandlePlayClicked</c> で購読(M5-PR4 で実装)。</summary>
        event Action<CardId> OnPlayClicked;

        /// <summary>EndTurn ボタン押下通知。Presenter は <c>HandleEndTurnClicked</c> で購読(M5-PR4 で実装)。</summary>
        event Action OnEndTurnClicked;
    }
}
