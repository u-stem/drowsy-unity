using System;

namespace Drowsy.Application.Games.DrowZzz
{
    /// <summary>
    /// セッション既存状態に対して <see cref="DrowZzzAction"/> を適用する統一 UseCase。
    /// <see cref="StartGameUseCase"/>(セッション未生成スタート)とは独立した別系統。
    /// </summary>
    /// <remarks>
    /// ADR-0006 §3 / §M1-PR6 / §Implementation Notes に基づく。
    /// <see cref="Execute"/> は内部で <see cref="DrowZzzRule.IsLegalMove"/> を検証し、
    /// <c>false</c> の場合は <see cref="InvalidOperationException"/> を投げる(<c>Pile.Draw</c> の空 Pile 例外と同じ防御的パターン)。
    /// <c>true</c> なら <see cref="DrowZzzRule.Apply"/> に委譲する薄い抽象化層。
    /// <para>
    /// <see cref="StartGameAction"/> は <c>DrowZzzRule.IsLegalMove</c> で常に <c>false</c> を返す設計のため、
    /// 本 UseCase 経由で呼ぶと <see cref="InvalidOperationException"/> が投げられる(<see cref="StartGameUseCase"/> 経由で扱う)。
    /// </para>
    /// </remarks>
    public sealed class ApplyActionUseCase
    {
        private readonly DrowZzzRule _rule;

        /// <exception cref="ArgumentNullException">rule が null</exception>
        public ApplyActionUseCase(DrowZzzRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        /// <summary>
        /// 与えられた <paramref name="session"/> に <paramref name="action"/> を適用した次セッションを返す。
        /// </summary>
        /// <exception cref="ArgumentNullException">session または action が null</exception>
        /// <exception cref="InvalidOperationException">
        /// <c>rule.IsLegalMove(session, action)</c> が <c>false</c> を返す場合
        /// (<see cref="StartGameAction"/> 含む、ADR-0006 §3 §IsLegalMove 違反時の方針)
        /// </exception>
        public DrowZzzGameSession Execute(DrowZzzGameSession session, DrowZzzAction action)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            if (!_rule.IsLegalMove(session, action))
            {
                throw new InvalidOperationException(
                    $"ApplyActionUseCase: {action.GetType().Name} は現セッション (TurnPhase: {session.TurnPhase}) では合法ではありません");
            }
            return _rule.Apply(session, action);
        }
    }
}
