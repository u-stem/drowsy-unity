using System;
using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Presentation.Games.DrowZzz;

namespace Drowsy.Presentation.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="IDrowZzzGameView"/> の Pure C# モック実装(M5-PR2 単体テスト用)。
    /// </summary>
    /// <remarks>
    /// Pure C# クラスで実装し、<c>On*Clicked</c> を
    /// public method で発火、<see cref="Render"/> / <see cref="RenderOutcome"/> 呼び出しを List に蓄積する。
    /// <para>
    /// <b>event 発火 API</b>:Presenter の event subscription を検証するため、テストから event を
    /// 直接発火できるように <see cref="FireDrawClicked"/> / <see cref="FirePlayClicked"/> /
    /// <see cref="FireEndTurnClicked"/> を提供する。event field は private のため外部から直接呼べないが、
    /// public method を介して raise する設計(C# の event は declaring class 以外から raise できないため)。
    /// </para>
    /// </remarks>
    public sealed class MockDrowZzzGameView : IDrowZzzGameView
    {
        private readonly List<DrowZzzGameSession> _renderedSessions = new();
        private readonly List<GameOutcome> _renderedOutcomes = new();

        /// <summary><see cref="Render"/> が受け取った session の履歴(古い順)。</summary>
        public IReadOnlyList<DrowZzzGameSession> RenderedSessions => _renderedSessions;

        /// <summary><see cref="RenderOutcome"/> が受け取った outcome の履歴(古い順)。</summary>
        public IReadOnlyList<GameOutcome> RenderedOutcomes => _renderedOutcomes;

        /// <inheritdoc />
        public void Render(DrowZzzGameSession session)
        {
            _renderedSessions.Add(session);
        }

        /// <inheritdoc />
        public void RenderOutcome(GameOutcome outcome)
        {
            _renderedOutcomes.Add(outcome);
        }

        /// <inheritdoc />
        public event Action OnDrawClicked;

        /// <inheritdoc />
        public event Action<CardId> OnPlayClicked;

        /// <inheritdoc />
        public event Action OnEndTurnClicked;

        /// <summary>テスト用:<see cref="OnDrawClicked"/> を発火する。</summary>
        public void FireDrawClicked() => OnDrawClicked?.Invoke();

        /// <summary>テスト用:<see cref="OnPlayClicked"/> を発火する。</summary>
        public void FirePlayClicked(CardId cardId) => OnPlayClicked?.Invoke(cardId);

        /// <summary>テスト用:<see cref="OnEndTurnClicked"/> を発火する。</summary>
        public void FireEndTurnClicked() => OnEndTurnClicked?.Invoke();

        /// <summary>テスト用:<see cref="OnDrawClicked"/> の購読者数(<see cref="Delegate.GetInvocationList"/> 経由)。</summary>
        public int OnDrawClickedSubscriberCount => OnDrawClicked?.GetInvocationList().Length ?? 0;

        /// <summary>テスト用:<see cref="OnPlayClicked"/> の購読者数。</summary>
        public int OnPlayClickedSubscriberCount => OnPlayClicked?.GetInvocationList().Length ?? 0;

        /// <summary>テスト用:<see cref="OnEndTurnClicked"/> の購読者数。</summary>
        public int OnEndTurnClickedSubscriberCount => OnEndTurnClicked?.GetInvocationList().Length ?? 0;
    }
}
