using System;
using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Stubs
{
    /// <summary>
    /// `ApplyActionUseCaseTests` / `DrowZzzRuleTests` の共通ヘルパー
    /// (`NewSession` / `NewRule` / `NewDeck`)。他 fixture への段階的拡張は
    /// `docs/todo.md` 参照。
    /// </summary>
    public static class SessionFactory
    {
        /// <summary>
        /// `DrowZzzRule` の最小依存(空 `InMemoryCardCatalog` + 標準 `EffectInterpreter`)を
        /// 組み立てる。M1 互換挙動の維持目的(ADR-0007 §3、constructor 引数の `ICardCatalog<IEffect>` /
        /// `EffectInterpreter` を満たす最小値)。
        /// </summary>
        public static DrowZzzRule NewRule() =>
            new DrowZzzRule(
                new InMemoryCardCatalog(new KeyValuePair<CardId, CardData>[0]),
                new EffectInterpreter());

        /// <summary>
        /// `params string[]` から `Pile` を組み立てるショートカット(`NewDeck("c1", "c2")` で
        /// 2 枚の Pile を生成、先頭順序保持)。`CardId.Of` を内部で呼ぶ。
        /// </summary>
        public static Pile NewDeck(params string[] cardIds)
        {
            var cards = new CardId[cardIds.Length];
            for (int i = 0; i < cardIds.Length; i++)
            {
                cards[i] = CardId.Of(cardIds[i]);
            }
            return new Pile(cards);
        }

        /// <summary>
        /// `DrowZzzGameSession` を全引数 optional で生成する。デフォルトは N=2 / `WaitingForDraw` /
        /// 空 Deck / 空 Hand / 空 `DdpPool` / 全 DP=0 / 空 Influences / 空 BedDamages /
        /// 空 PendingCounteredEffects。
        /// </summary>
        /// <remarks>
        /// 旧 `DrowZzzRuleTests.NewSession` のスーパーセット引数(`turnNumber` / `ddpPool` /
        /// `ddp` / `influences` / `bedDamages` を引数化)を維持しつつ、`ApplyActionUseCaseTests`
        /// (旧 NewSession は固定値だった項目)も同じシグネチャで呼び出せる設計。引数を渡さない
        /// 経路は既存両 fixture の挙動と完全一致する。
        /// </remarks>
        public static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForDraw,
            int currentPlayerIndex = 0,
            Pile deck = null,
            Hand p0Hand = null,
            Hand p1Hand = null,
            int turnNumber = 1,
            DdpPool ddpPool = null,
            IReadOnlyDictionary<PlayerId, int> ddp = null,
            IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerInfluence>> influences = null,
            IReadOnlyDictionary<PlayerId, int> bedDamages = null)
        {
            var p0 = new PlayerState(PlayerId.Of("p1"), p0Hand ?? Hand.Empty);
            var p1 = new PlayerState(PlayerId.Of("p2"), p1Hand ?? Hand.Empty);
            var gs = new GameState(
                new[] { p0, p1 },
                deck ?? Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                new TurnState(turnNumber, currentPlayerIndex));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 10,
            };
            // SDP は M2-PR3 で追加(ADR-0009 §「DP 種別」)。本ヘルパー利用テストは SDP に関心がないため
            // 全プレイヤー 0 で固定初期化する。
            var sdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // DDP / DdpPool は M2-PR4 で追加(ADR-0009 §「DP 種別」/ §「DDP プールの構造」)。
            // DDP 自動抽選機構を検証しないテストはデフォルト DDP=0 / 空 DdpPool で十分。
            var ddpResolved = ddp ?? new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // M2-PR5: Influences は引数指定なら採用、未指定なら空 list 固定
            var influencesResolved = influences ?? new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            // M3-PR2: BedDamages は引数指定なら採用、未指定なら 0/0 固定(ADR-0011 §3、ddp / influences と同パターン)
            var bedDamagesResolved = bedDamages ?? new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            return new DrowZzzGameSession(
                gs,
                fdp,
                ddpResolved,
                sdp,
                ddpPool ?? DdpPool.Empty,
                influencesResolved,
                phase,
                outcome: null,
                bedDamages: bedDamagesResolved,
                Array.Empty<PendingCounteredEffect>());
        }
    }
}
