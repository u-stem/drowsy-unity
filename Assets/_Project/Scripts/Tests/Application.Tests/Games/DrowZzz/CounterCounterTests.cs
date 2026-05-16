using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// 「反撃の反撃」+ 元カード A の効果遡及発動の検証(M3-PR5c、ADR-0011 §4.4)。
    /// <see cref="PendingCounteredEffect"/> record + <see cref="DrowZzzGameSession.PendingCounteredEffects"/> +
    /// <see cref="CounterAction"/> 経路 2(自ターン <see cref="DrowZzzPhaseState.WaitingForEndTurn"/>) +
    /// <c>EndTurnAction.Apply</c> での Pending 一括クリア
    /// (DZ-222 / DZ-223 / DZ-224 / DZ-225 / DZ-226 / DZ-227)。
    /// </summary>
    [TestFixture]
    public sealed class CounterCounterTests
    {
        // ===== ヘルパー =====

        private static readonly CardId CardAId = CardId.Of(CardTypeId.Of("a"), 0); // 元カード A、効果列に AdjustSdpEffect(Self, +10) を持つ
        private static readonly CardId CardBId = CardId.Of(CardTypeId.Of("b"), 0); // 反撃カード B、Counter キーワード持ち
        private static readonly CardId CardCId = CardId.Of(CardTypeId.Of("c"), 0); // 反撃の反撃カード C、Counter キーワード持ち
        private static readonly CardId FrenzyBId = CardId.Of(CardTypeId.Of("frenzyB"), 0); // Frenzy + Counter 持ち(target で Frenzy 検出)
        private static readonly CardId PlainId = CardId.Of(CardTypeId.Of("plain"), 0); // Counter なし

        // 主要 effects:
        //   A: [AdjustSdpEffect(Self, +10)] — 遡及発動で SDP[currentPlayer] が +10 される
        //   B: [Counter 持ち]
        //   C: [Counter 持ち]
        //   FrenzyB: [Frenzy + Counter 持ち](target にすると Frenzy 検出で経路 2 illegal)
        private static DrowZzzRule NewRule()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CardAId.TypeId, new CardData("a", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(CardBId.TypeId, new CardData("b", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(CardCId.TypeId, new CardData("c", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(FrenzyBId.TypeId, new CardData("frenzyB", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(PlainId.TypeId, new CardData("plain", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardAId.TypeId,
                    new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) }),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardBId.TypeId,
                    new IEffect[] { new KeywordedEffect(new[] { Keyword.Counter }, new AssociatableMarkerEffect()) }),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CardCId.TypeId,
                    new IEffect[] { new KeywordedEffect(new[] { Keyword.Counter }, new AssociatableMarkerEffect()) }),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    FrenzyBId.TypeId,
                    new IEffect[]
                    {
                        new KeywordedEffect(new[] { Keyword.Frenzy }, new AssociatableMarkerEffect()),
                        new KeywordedEffect(new[] { Keyword.Counter }, new AssociatableMarkerEffect()),
                    }),
            };
            return new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());
        }

        // p1(currentPlayer)の手札 = [C, plain]、p2 の手札 = [](空)。
        // Discard には [C 候補がない場合の] B / A が積まれた状態を再現(経路 1 Apply 完了後の状態をシミュレート)。
        // Field は空(B / A はすでに Discard へ移動済)。PhaseState = WaitingForEndTurn(経路 1 終了後)。
        // PendingCounteredEffects: 引数で指定(default は経路 1 直後の 1 件登録状態)。
        private static DrowZzzGameSession NewSessionAfterCounter(
            IReadOnlyList<PendingCounteredEffect> pending = null,
            IReadOnlyList<CardId> p1Hand = null,
            int currentPlayerIndex = 0)
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), new Hand(p1Hand ?? new[] { CardCId, PlainId })),
                new PlayerState(PlayerId.Of("p2"), new Hand(Array.Empty<CardId>())),
            };
            var discard = new Pile(new[] { CardBId, CardAId }); // top = B, [1] = A
            var gs = new GameState(
                players, Pile.Empty, discard, Pile.Empty,
                new TurnState(1, currentPlayerIndex));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            var bed = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            var defaultPending = new PendingCounteredEffect[]
            {
                new PendingCounteredEffect(
                    CounterCard: CardBId,
                    OriginalCard: CardAId,
                    OriginalEffects: new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) }),
            };
            return new DrowZzzGameSession(
                gs, fdp, ddp, sdp, DdpPool.Empty, influences,
                DrowZzzPhaseState.WaitingForEndTurn,
                outcome: null,
                bedDamages: bed,
                pendingCounteredEffects: pending ?? defaultPending);
        }

        // ===== DZ-222: PendingCounteredEffect の null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-222")]
        public void Given_CounterCardがnull_When_PendingCounteredEffect生成_Then_ArgumentNullException()
        {
            // When / Then
            Assert.Throws<ArgumentNullException>(() =>
                new PendingCounteredEffect(null, CardAId, new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) }));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-222")]
        public void Given_OriginalCardがnull_When_PendingCounteredEffect生成_Then_ArgumentNullException()
        {
            // When / Then
            Assert.Throws<ArgumentNullException>(() =>
                new PendingCounteredEffect(CardBId, null, new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) }));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-222")]
        public void Given_OriginalEffectsがnull_When_PendingCounteredEffect生成_Then_ArgumentNullException()
        {
            // When / Then
            Assert.Throws<ArgumentNullException>(() =>
                new PendingCounteredEffect(CardBId, CardAId, null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-222")]
        public void Given_OriginalEffectsにnull要素_When_PendingCounteredEffect生成_Then_ArgumentException()
        {
            // Given
            var withNullElement = new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10), null };
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                new PendingCounteredEffect(CardBId, CardAId, withNullElement));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-222")]
        public void Given_空のOriginalEffects_When_PendingCounteredEffect生成_Then_例外なし()
        {
            // Given / When
            var pending = new PendingCounteredEffect(CardBId, CardAId, Array.Empty<IEffect>());
            // Then
            Assert.That(pending.OriginalEffects.Count, Is.EqualTo(0));
        }

        // ===== DZ-223: DrowZzzGameSession.PendingCounteredEffects null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-223")]
        public void Given_pendingCounteredEffectsがnull_When_Session生成_Then_ArgumentNullException()
        {
            // Given: ヘルパー NewSessionAfterCounter は `pending ?? defaultPending` で null 合体するため、
            //        本テストでは直接 ctor を呼んで pendingCounteredEffects: null を渡す(code-reviewer W-1 反映)
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), new Hand(new[] { CardCId })),
                new PlayerState(PlayerId.Of("p2"), new Hand(Array.Empty<CardId>())),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, Pile.Empty,
                new TurnState(1, 0));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            var bed = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            // When / Then
            Assert.Throws<ArgumentNullException>(() => new DrowZzzGameSession(
                gs, fdp, ddp, sdp, DdpPool.Empty, influences,
                DrowZzzPhaseState.WaitingForEndTurn,
                outcome: null,
                bedDamages: bed,
                pendingCounteredEffects: null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-223")]
        public void Given_pendingCounteredEffectsにnull要素_When_Session生成_Then_ArgumentException()
        {
            // When / Then
            Assert.Throws<ArgumentException>(() =>
                NewSessionAfterCounter(pending: new PendingCounteredEffect[] { null }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-223")]
        public void Given_空のpendingCounteredEffects_When_Session生成_Then_例外なし()
        {
            // Given / When
            var session = NewSessionAfterCounter(pending: Array.Empty<PendingCounteredEffect>());
            // Then
            Assert.That(session.PendingCounteredEffects.Count, Is.EqualTo(0));
        }

        // ===== DZ-224: 経路 1 Apply 後の PendingCounteredEffects に 1 件追加 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-224")]
        public void Given_経路1のCounterAction_When_Apply_Then_PendingCounteredEffectsに1件追加()
        {
            // Given: p1 が A をプレイ後、p2 が B でカウンタするセッション(WaitingForCounterResponse、Field 先頭 A)
            var rule = NewRule();
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), new Hand(Array.Empty<CardId>())),
                new PlayerState(PlayerId.Of("p2"), new Hand(new[] { CardBId })),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, new Pile(new[] { CardAId }),
                new TurnState(1, 0));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            var bed = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var session = new DrowZzzGameSession(
                gs, fdp, ddp, sdp, DdpPool.Empty, influences,
                DrowZzzPhaseState.WaitingForCounterResponse,
                outcome: null,
                bedDamages: bed,
                pendingCounteredEffects: Array.Empty<PendingCounteredEffect>());
            // When
            var next = rule.Apply(session, new CounterAction(CardBId, CardAId));
            // Then: PendingCounteredEffects に 1 件追加(CounterCard = B, OriginalCard = A)
            Assert.That(next.PendingCounteredEffects.Count, Is.EqualTo(1));
            Assert.That(next.PendingCounteredEffects[0].CounterCard, Is.EqualTo(CardBId));
            Assert.That(next.PendingCounteredEffects[0].OriginalCard, Is.EqualTo(CardAId));
        }

        // ===== DZ-225: 経路 2 IsLegalMove =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-225")]
        public void Given_WaitingForEndTurn_Pending非空_最後Bと一致_When_IsLegalMove_Then_true()
        {
            // Given: 経路 2 の合法条件すべて満たす
            var rule = NewRule();
            var session = NewSessionAfterCounter();
            // When / Then
            Assert.That(rule.IsLegalMove(session, new CounterAction(CardCId, CardBId)), Is.True);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-225")]
        public void Given_Pending空_When_経路2IsLegalMove_Then_false()
        {
            // Given: Pending が空 → 経路 2 illegal
            var rule = NewRule();
            var session = NewSessionAfterCounter(pending: Array.Empty<PendingCounteredEffect>());
            // When / Then
            Assert.That(rule.IsLegalMove(session, new CounterAction(CardCId, CardBId)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-225")]
        public void Given_Pending最後エントリとTarget不一致_When_経路2IsLegalMove_Then_false()
        {
            // Given: Pending 最後エントリの CounterCard != action.Target(CardBId)
            var rule = NewRule();
            var other = new PendingCounteredEffect(
                CounterCard: PlainId, OriginalCard: CardAId,
                OriginalEffects: new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) });
            var session = NewSessionAfterCounter(pending: new[] { other });
            // When / Then: target = CardBId だが最後エントリの CounterCard = PlainId
            Assert.That(rule.IsLegalMove(session, new CounterAction(CardCId, CardBId)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-225")]
        public void Given_Counterが現プレイヤー手札になし_When_経路2IsLegalMove_Then_false()
        {
            // Given: p1 の手札に CardCId なし
            var rule = NewRule();
            var session = NewSessionAfterCounter(p1Hand: new[] { PlainId });
            // When / Then
            Assert.That(rule.IsLegalMove(session, new CounterAction(CardCId, CardBId)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-225")]
        public void Given_TargetにFrenzy_When_経路2IsLegalMove_Then_false()
        {
            // Given: Pending 最後エントリの CounterCard = FrenzyBId(Frenzy 持ち)
            var rule = NewRule();
            var frenzyEntry = new PendingCounteredEffect(
                CounterCard: FrenzyBId, OriginalCard: CardAId,
                OriginalEffects: new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, 10) });
            var session = NewSessionAfterCounter(pending: new[] { frenzyEntry });
            // When / Then: target = FrenzyBId は Frenzy 持ち → 経路 2 illegal
            Assert.That(rule.IsLegalMove(session, new CounterAction(CardCId, FrenzyBId)), Is.False);
        }

        // ===== DZ-226: 経路 2 Apply =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-226")]
        public void Given_経路2のCounterAction_When_Apply_Then_Counter手札から除去()
        {
            // Given / When
            var rule = NewRule();
            var session = NewSessionAfterCounter();
            var next = rule.Apply(session, new CounterAction(CardCId, CardBId));
            // Then
            Assert.That(next.GameState.Players[0].Hand.Contains(CardCId), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-226")]
        public void Given_経路2のCounterAction_When_Apply_Then_Discard先頭がC()
        {
            // Given / When
            var rule = NewRule();
            var session = NewSessionAfterCounter();
            var next = rule.Apply(session, new CounterAction(CardCId, CardBId));
            // Then: AddTop 後の Discard 先頭(Cards[0])は C
            Assert.That(next.GameState.Discard.Cards[0], Is.EqualTo(CardCId));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-226")]
        public void Given_経路2のCounterAction_When_Apply_Then_Pending最後エントリ削除()
        {
            // Given / When
            var rule = NewRule();
            var session = NewSessionAfterCounter();
            var next = rule.Apply(session, new CounterAction(CardCId, CardBId));
            // Then: Pending が空(1 件 → 0 件)
            Assert.That(next.PendingCounteredEffects.Count, Is.EqualTo(0));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-226")]
        public void Given_経路2のCounterAction_When_Apply_Then_AのOriginalEffectsが遡及発動()
        {
            // Given: Pending = [(B, A, [AdjustSdpEffect(Self, +10)])]、p1 SDP = 0
            var rule = NewRule();
            var session = NewSessionAfterCounter();
            // When: 経路 2 Apply で A の効果が遡及発動 → p1 SDP +10
            var next = rule.Apply(session, new CounterAction(CardCId, CardBId));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(10));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-226")]
        public void Given_経路2のCounterAction_When_Apply_Then_PhaseStateがWaitingForEndTurn維持()
        {
            // Given / When
            var rule = NewRule();
            var session = NewSessionAfterCounter();
            var next = rule.Apply(session, new CounterAction(CardCId, CardBId));
            // Then: PhaseState は WaitingForEndTurn 維持(続いて EndTurnAction 待ち)
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForEndTurn));
        }

        // ===== DZ-227: EndTurnAction.Apply で Pending クリア =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-227")]
        public void Given_Pending非空_When_EndTurnAction_Then_Pendingが空()
        {
            // Given: Pending 1 件、WaitingForEndTurn
            var rule = NewRule();
            var session = NewSessionAfterCounter();
            // When: EndTurnAction でターン進行
            var next = rule.Apply(session, new EndTurnAction());
            // Then: Pending クリア
            Assert.That(next.PendingCounteredEffects.Count, Is.EqualTo(0));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-227")]
        public void Given_Pending空_When_EndTurnAction_Then_PendingClear後も空でNoOp()
        {
            // Given: Pending 空、WaitingForEndTurn
            var rule = NewRule();
            var session = NewSessionAfterCounter(pending: Array.Empty<PendingCounteredEffect>());
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then: Pending 空のまま(no-op、graceful)
            Assert.That(next.PendingCounteredEffects.Count, Is.EqualTo(0));
        }
    }
}
