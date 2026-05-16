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
    /// Application.Tests 配下 fixture の共通ヘルパー(`NewSession` / `NewRule` / `NewDeck`)。
    /// 統合済 fixture: `ApplyActionUseCaseTests` / `DrowZzzRuleTests`(2026-05-13)、
    /// `DrowZzzGameSessionTests` / `EffectInterpreterTests`(2026-05-13 第 1 弾)、
    /// `EarlyWinTriggerEffectTests` / `AdjustSdpEffectTests`(2026-05-16 第 2 弾、`fdp` / `sdp` 引数 +
    /// `Dp(p1, p2)` builder 追加に伴う)。
    /// 残 fixture(`CounterActionTests` / `AssociateActionTests` / `AbandonActionTests` /
    /// `CupOfThreatCardTests` / `GreenInvasionCardTests` / `DreamCardTests` /
    /// `CounterCounterTests` / `Effects/*Tests` 大部分)への段階的拡張は `docs/todo.md` で追跡。
    /// </summary>
    public static class SessionFactory
    {
        /// <summary>
        /// N=2 用 DP Dictionary を 2 値から組み立てるショートカット。
        /// 引数省略で全プレイヤー 0(`{p1: 0, p2: 0}`)。<see cref="NewSession"/> の
        /// `fdp` / `sdp` / `ddp` / `bedDamages` 引数に渡せる。
        /// </summary>
        public static IReadOnlyDictionary<PlayerId, int> Dp(int p1 = 0, int p2 = 0) =>
            new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = p1,
                [PlayerId.Of("p2")] = p2,
            };

        /// <summary>
        /// `DrowZzzRule` の最小依存(空 `InMemoryCardCatalog` + 標準 `EffectInterpreter`)を
        /// 組み立てる。M1 互換挙動の維持目的(ADR-0007 §3、constructor 引数の `ICardCatalog<IEffect>` /
        /// `EffectInterpreter` を満たす最小値)。
        /// </summary>
        public static DrowZzzRule NewRule() =>
            new DrowZzzRule(
                new InMemoryCardCatalog(new KeyValuePair<CardTypeId, CardData>[0]),
                new EffectInterpreter());

        /// <summary>
        /// `params string[]` から `Pile` を組み立てるショートカット(`NewDeck("c1", "c2")` で
        /// 2 枚の Pile を生成、先頭順序保持)。`CardId.Of(CardTypeId.Of(typeId), 0)` を内部で呼ぶ。
        /// </summary>
        /// <remarks>
        /// <b>注意(ADR-0018 / code-reviewer 提案 5)</b>:本 helper は <c>instance=0 固定</c> で `CardId` を生成するため、
        /// 同じ string を複数渡すと <c>(typeId, 0)</c> が重複し、<c>StartGameUseCase</c> が Hand 配布時に
        /// `Hand.Add` の unique 制約で <see cref="ArgumentException"/> を投げる。
        /// 「同種カードを複数枚配布」をテストしたい場合は本 helper を使わず、
        /// <c>CardId.Of(CardTypeId.Of(id), i)</c> を直接呼んで instance を変えて並べること。
        /// </remarks>
        public static Pile NewDeck(params string[] cardIds)
        {
            var cards = new CardId[cardIds.Length];
            for (int i = 0; i < cardIds.Length; i++)
            {
                cards[i] = CardId.Of(CardTypeId.Of(cardIds[i]), 0);
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
            IReadOnlyDictionary<PlayerId, int> fdp = null,
            IReadOnlyDictionary<PlayerId, int> sdp = null,
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
            // 既存 fixture(`DrowZzzRuleTests` / `ApplyActionUseCaseTests` 等)互換のデフォルト fdp は `{p1: 0, p2: 10}`。
            // 新規 fixture で固有値が必要な場合は `fdp: Dp(p1: 100, p2: 0)` のように明示する。
            var fdpResolved = fdp ?? new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 10,
            };
            // SDP は M2-PR3 で追加(ADR-0009 §「DP 種別」)。デフォルトは全プレイヤー 0 で固定、
            // 新規 fixture で固有値が必要な場合は `sdp: Dp(p1: 5)` のように明示する。
            var sdpResolved = sdp ?? new Dictionary<PlayerId, int>
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
                fdpResolved,
                ddpResolved,
                sdpResolved,
                ddpPool ?? DdpPool.Empty,
                influencesResolved,
                phase,
                outcome: null,
                bedDamages: bedDamagesResolved,
                Array.Empty<PendingCounteredEffect>());
        }
    }
}
