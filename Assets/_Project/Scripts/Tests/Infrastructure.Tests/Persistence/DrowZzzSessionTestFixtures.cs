using System.Collections.Generic;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// Persistence テスト群が共有する <see cref="DrowZzzGameSession"/> 構築ヘルパー。
    /// </summary>
    /// <remarks>
    /// 既存の Application.Tests の <c>DrowZzzSessionFixtures</c>(M2 以降確立)と仕様共有するが、
    /// 本テスト範囲(Infrastructure 層)では Pure C# 維持の必要なし(Drowsy.Infrastructure.Tests asmdef は
    /// UnityEngine 依存を持つ)。Application.Tests の InMemory パターンを再実装する形で重複保持
    /// (CardCatalog 系テスト群と同パターン、ADR-0012 §5「Pure C# 哲学維持と Ports &amp; Adapters 整合」)。
    /// </remarks>
    internal static class DrowZzzSessionTestFixtures
    {
        public static readonly PlayerId PlayerA = PlayerId.Of("PlayerA");
        public static readonly PlayerId PlayerB = PlayerId.Of("PlayerB");

        /// <summary>最小構成の session(全 dictionary が default 値、Outcome=null、効果なし)。</summary>
        public static DrowZzzGameSession MinimalSession()
        {
            var hand = Hand.Empty;
            var players = new[]
            {
                new PlayerState(PlayerA, hand),
                new PlayerState(PlayerB, hand),
            };
            var gameState = new GameState(
                players: players,
                deck: Pile.Empty,
                discard: Pile.Empty,
                field: Pile.Empty,
                turn: TurnState.Initial(0));

            return new DrowZzzGameSession(
                gameState: gameState,
                firstDrowsyPoints: new Dictionary<PlayerId, int>
                {
                    [PlayerA] = 0,
                    [PlayerB] = 0,
                },
                drawDrowsyPoints: new Dictionary<PlayerId, int>
                {
                    [PlayerA] = 0,
                    [PlayerB] = 0,
                },
                secondDrowsyPoints: new Dictionary<PlayerId, int>
                {
                    [PlayerA] = 0,
                    [PlayerB] = 0,
                },
                ddpPool: DdpPool.Empty,
                influences: new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerA] = System.Array.Empty<PlayerInfluence>(),
                    [PlayerB] = System.Array.Empty<PlayerInfluence>(),
                },
                phaseState: DrowZzzPhaseState.WaitingForDraw,
                outcome: null,
                bedDamages: new Dictionary<PlayerId, int>
                {
                    [PlayerA] = 0,
                    [PlayerB] = 0,
                },
                pendingCounteredEffects: System.Array.Empty<PendingCounteredEffect>());
        }

        /// <summary>「全部入り」session(全機構を round-trip で検証する fixture):
        /// FDP/DDP/SDP に値、Hand/Deck/Discard/Field にカード、Influences 1 件、BedDamages、
        /// PendingCounteredEffects 1 件、PhaseState=WaitingForCounterResponse、Outcome=null。
        /// </summary>
        public static DrowZzzGameSession FullSessionWithAllFeatures()
        {
            var card01 = CardId.Of(CardTypeId.Of("01"), 0);
            var card02 = CardId.Of(CardTypeId.Of("02"), 0);
            var card03 = CardId.Of(CardTypeId.Of("03"), 0);

            var handA = new Hand(new[] { card01 });
            var handB = new Hand(new[] { card02 });

            var gameState = new GameState(
                players: new[]
                {
                    new PlayerState(PlayerA, handA),
                    new PlayerState(PlayerB, handB),
                },
                deck: new Pile(new[] { card03 }),
                discard: Pile.Empty,
                field: Pile.Empty,
                turn: new TurnState(turnNumber: 5, currentPlayerIndex: 1));

            var influence = new PlayerInfluence(
                Trigger: InfluenceTrigger.OwnPhaseStart,
                TickEffect: new AdjustSdpEffect(SdpTarget.Self, -3),
                RemainingCount: 2);

            var pendingEntry = new PendingCounteredEffect(
                CounterCard: card02,
                OriginalCard: card01,
                OriginalEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Opponent, -5),
                    new DrawCardEffect(SdpTarget.Self, 1),
                });

            return new DrowZzzGameSession(
                gameState: gameState,
                firstDrowsyPoints: new Dictionary<PlayerId, int>
                {
                    [PlayerA] = 12,
                    [PlayerB] = 18,
                },
                drawDrowsyPoints: new Dictionary<PlayerId, int>
                {
                    [PlayerA] = -3,
                    [PlayerB] = 7,
                },
                secondDrowsyPoints: new Dictionary<PlayerId, int>
                {
                    [PlayerA] = 4,
                    [PlayerB] = -2,
                },
                ddpPool: new DdpPool(new[] { -1, 5, -7, 2 }),
                influences: new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerA] = new[] { influence },
                    [PlayerB] = System.Array.Empty<PlayerInfluence>(),
                },
                phaseState: DrowZzzPhaseState.WaitingForCounterResponse,
                outcome: null,
                bedDamages: new Dictionary<PlayerId, int>
                {
                    [PlayerA] = 0,
                    [PlayerB] = 30,
                },
                pendingCounteredEffects: new[] { pendingEntry });
        }

        /// <summary>勝者確定済み session(Outcome=WinnerOutcome)。</summary>
        public static DrowZzzGameSession SessionWithWinnerOutcome()
        {
            return MinimalSession() with { Outcome = new WinnerOutcome(PlayerA) };
        }

        /// <summary>引き分け確定済み session(Outcome=DrawOutcome)。</summary>
        public static DrowZzzGameSession SessionWithDrawOutcome()
        {
            return MinimalSession() with { Outcome = new DrawOutcome() };
        }
    }
}
