using System;
using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="CounterAction"/> / <see cref="PassCounterAction"/> + WaitingForCounterResponse PhaseState +
    /// <see cref="DrowZzzRule.ApplyPlayCard"/> 後の PhaseState 分岐を検証する
    /// (DZ-214 / DZ-215 / DZ-216 / DZ-217 / DZ-218 / DZ-219 / DZ-220 / DZ-221)。
    /// </summary>
    [TestFixture]
    public sealed class CounterActionTests
    {
        // ===== ヘルパー =====

        private static readonly CardId TargetId = CardId.Of(CardTypeId.Of("target"), 0);
        private static readonly CardId CounterId = CardId.Of(CardTypeId.Of("counter"), 0);
        private static readonly CardId FrenzyTargetId = CardId.Of(CardTypeId.Of("frenzyTarget"), 0);
        private static readonly CardId PlainId = CardId.Of(CardTypeId.Of("plain"), 0);

        // Counter キーワード持ちカード(counter)と Frenzy target カード(frenzyTarget)を含む catalog
        private static DrowZzzRule NewRuleWithCounterAndFrenzy()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(TargetId.TypeId, new CardData("target", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(CounterId.TypeId, new CardData("counter", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(FrenzyTargetId.TypeId, new CardData("frenzyTarget", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(PlainId.TypeId, new CardData("plain", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CounterId.TypeId,
                    new IEffect[]
                    {
                        new KeywordedEffect(new[] { Keyword.Counter }, new AssociatableMarkerEffect()),
                    }),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    FrenzyTargetId.TypeId,
                    new IEffect[]
                    {
                        new KeywordedEffect(new[] { Keyword.Frenzy }, new AssociatableMarkerEffect()),
                    }),
            };
            return new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());
        }

        // 相手プレイヤー p2 が CounterId を手札に持ち、p1 が target/frenzyTarget/plain を手札に持つセッション
        // p1 が currentPlayer。Field は空(PlayCard 直前の状態)、PhaseState は呼び出し側で指定。
        // 2026-05-17 SessionFactory 統合 第 3 弾:内部実装を SessionFactory.NewSession 呼び出しに置換
        // (SessionFactory.NewSession に `field` / `discard` 引数を追加して受け皿を整備済)。
        private static DrowZzzGameSession NewSession(
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForCounterResponse,
            CardId fieldTop = null) =>
            Stubs.SessionFactory.NewSession(
                phase: phase,
                p0Hand: new Hand(new[] { TargetId, FrenzyTargetId, PlainId }),
                p1Hand: new Hand(new[] { CounterId }),
                field: fieldTop is null ? null : new Pile(new[] { fieldTop }),
                fdp: Stubs.SessionFactory.Dp(p1: 0, p2: 0));

        // ===== DZ-215: PlayCardAction 後の PhaseState 分岐(相手手札に Counter 持ち → WaitingForCounterResponse)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-215")]
        public void Given_相手手札にCounter持ち_When_PlayCardActionをApply_Then_WaitingForCounterResponseに遷移()
        {
            // p1 が target をプレイ、p2 の手札に counter(Counter キーワード持ち)あり
            var rule = NewRuleWithCounterAndFrenzy();
            var session = new DrowZzzGameSession(
                new GameState(
                    new[]
                    {
                        new PlayerState(PlayerId.Of("p1"), new Hand(new[] { TargetId })),
                        new PlayerState(PlayerId.Of("p2"), new Hand(new[] { CounterId })),
                    },
                    Pile.Empty, Pile.Empty, Pile.Empty,
                    new TurnState(1, 0)),
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                DdpPool.Empty,
                new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
                },
                DrowZzzPhaseState.WaitingForPlay,
                outcome: null,
                bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 }, System.Array.Empty<PendingCounteredEffect>());
            var next = rule.Apply(session, new PlayCardAction(TargetId));
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForCounterResponse));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-215")]
        public void Given_相手手札にCounter持ちなし_When_PlayCardActionをApply_Then_WaitingForEndTurnに遷移()
        {
            // p2 の手札に Counter 持ちなし(plain のみ) → WaitingForEndTurn(従来通り)
            var rule = NewRuleWithCounterAndFrenzy();
            var session = new DrowZzzGameSession(
                new GameState(
                    new[]
                    {
                        new PlayerState(PlayerId.Of("p1"), new Hand(new[] { TargetId })),
                        new PlayerState(PlayerId.Of("p2"), new Hand(new[] { PlainId })),
                    },
                    Pile.Empty, Pile.Empty, Pile.Empty,
                    new TurnState(1, 0)),
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                DdpPool.Empty,
                new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
                },
                DrowZzzPhaseState.WaitingForPlay,
                outcome: null,
                bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 }, System.Array.Empty<PendingCounteredEffect>());
            var next = rule.Apply(session, new PlayCardAction(TargetId));
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForEndTurn));
        }

        // ===== DZ-218: CounterAction.IsLegalMove 合法条件 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-218")]
        public void Given_WaitingForCounterResponse_target_counter_Frenzyなし_When_IsLegalMove_Then_true()
        {
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForCounterResponse, fieldTop: TargetId);
            Assert.That(rule.IsLegalMove(session, new CounterAction(CounterId, TargetId)), Is.True);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-218")]
        public void Given_WaitingForPlay_When_CounterActionのIsLegalMove_Then_false()
        {
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, fieldTop: TargetId);
            Assert.That(rule.IsLegalMove(session, new CounterAction(CounterId, TargetId)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-218")]
        public void Given_Counter手札になし_When_IsLegalMove_Then_false()
        {
            // p2 の手札に CounterId が存在しないケース(plain のみ)
            var rule = NewRuleWithCounterAndFrenzy();
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), new Hand(new[] { TargetId })),
                new PlayerState(PlayerId.Of("p2"), new Hand(new[] { PlainId })),
            };
            var session = new DrowZzzGameSession(
                new GameState(players, Pile.Empty, Pile.Empty, new Pile(new[] { TargetId }), new TurnState(1, 0)),
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                DdpPool.Empty,
                new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
                },
                DrowZzzPhaseState.WaitingForCounterResponse,
                outcome: null,
                bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 }, System.Array.Empty<PendingCounteredEffect>());
            Assert.That(rule.IsLegalMove(session, new CounterAction(CounterId, TargetId)), Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-218")]
        public void Given_target_FieldTopではない_When_IsLegalMove_Then_false()
        {
            // Field 先頭が plain(TargetId ではない)→ false
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: PlainId);
            Assert.That(rule.IsLegalMove(session, new CounterAction(CounterId, TargetId)), Is.False);
        }

        // ===== DZ-221: Frenzy vs Counter は illegal-move =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-221")]
        public void Given_targetにFrenzy_When_CounterActionのIsLegalMove_Then_false()
        {
            // Field 先頭が FrenzyTarget(Frenzy キーワード持ち) → 反撃不可
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: FrenzyTargetId);
            Assert.That(rule.IsLegalMove(session, new CounterAction(CounterId, FrenzyTargetId)), Is.False);
        }

        // ===== DZ-219: CounterAction.Apply 状態遷移 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-219")]
        public void Given_CounterAction_When_Apply_Then_Field空()
        {
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: TargetId);
            var next = rule.Apply(session, new CounterAction(CounterId, TargetId));
            Assert.That(next.GameState.Field.Count, Is.EqualTo(0));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-219")]
        public void Given_CounterAction_When_Apply_Then_DiscardにtargetとcounterAddTop()
        {
            // target → counter の順で AddTop(後追加で AddTop は Cards[0] = counter / Cards[1] = target)
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: TargetId);
            var next = rule.Apply(session, new CounterAction(CounterId, TargetId));
            Assert.That(next.GameState.Discard.Cards[0], Is.EqualTo(CounterId));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-219")]
        public void Given_CounterAction_When_Apply_Then_反撃側手札からcounter除去()
        {
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: TargetId);
            var next = rule.Apply(session, new CounterAction(CounterId, TargetId));
            Assert.That(next.GameState.Players[1].Hand.Contains(CounterId), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-219")]
        public void Given_CounterAction_When_Apply_Then_PhaseStateがWaitingForEndTurn()
        {
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: TargetId);
            var next = rule.Apply(session, new CounterAction(CounterId, TargetId));
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForEndTurn));
        }

        // ===== DZ-220: PassCounterAction の合法性 / Apply =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-220")]
        public void Given_WaitingForCounterResponse_When_PassCounterActionのIsLegalMove_Then_true()
        {
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForCounterResponse, fieldTop: TargetId);
            Assert.That(rule.IsLegalMove(session, new PassCounterAction()), Is.True);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-220")]
        public void Given_WaitingForPlay_When_PassCounterActionのIsLegalMove_Then_false()
        {
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(phase: DrowZzzPhaseState.WaitingForPlay, fieldTop: TargetId);
            Assert.That(rule.IsLegalMove(session, new PassCounterAction()), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-220")]
        public void Given_PassCounterAction_When_Apply_Then_PhaseStateがWaitingForEndTurn()
        {
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: TargetId);
            var next = rule.Apply(session, new PassCounterAction());
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForEndTurn));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-220")]
        public void Given_PassCounterAction_When_Apply_Then_Field_Discard_Hand_すべて不変()
        {
            // PhaseState 以外の状態変化なし(Hand / Field / Discard 不変)
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: TargetId);
            var next = rule.Apply(session, new PassCounterAction());
            Assert.That(next.GameState.Field.Cards[0], Is.EqualTo(TargetId));
        }

        // ===== null 防御(DZ-216:CounterAction)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-216")]
        public void Given_Counter_null_When_CounterAction生成_Then_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new CounterAction(Counter: null, Target: TargetId));
            Assert.That(ex!.ParamName, Is.EqualTo("Counter"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-216")]
        public void Given_Target_null_When_CounterAction生成_Then_ArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new CounterAction(Counter: CounterId, Target: null));
            Assert.That(ex!.ParamName, Is.EqualTo("Target"));
        }

        // ===== DZ-219 補強:Discard.Cards[1] が TargetId であること(P-3 反映)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-219")]
        public void Given_CounterAction_When_Apply_Then_DiscardCards1がTargetId()
        {
            // Discard.AddTop(target).AddTop(counter) の順序で、Cards[0] = counter / Cards[1] = target
            var rule = NewRuleWithCounterAndFrenzy();
            var session = NewSession(fieldTop: TargetId);
            var next = rule.Apply(session, new CounterAction(CounterId, TargetId));
            Assert.That(next.GameState.Discard.Cards[1], Is.EqualTo(TargetId));
        }

        // ===== DZ-215 補強:EarlyWin 経路では PhaseState 上書きしない(W-3 反映)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-215")]
        public void Given_PlayCardでEarlyWin成立_相手にCounter持ち_When_PlayCardApply_Then_PhaseStateは上書きされずOutcome設定済()
        {
            // EarlyWinTriggerEffect を効果列に持つカード(就寝カード)を p1 がプレイし、夜 + 持ち点 100 で Outcome 確定
            // → 相手手札に Counter 持ちカードがあっても WaitingForCounterResponse には遷移しない(IsTerminated ガード)
            var bedtimeCard = CardId.Of(CardTypeId.Of("bedtime"), 0);
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(bedtimeCard.TypeId, new CardData("bedtime", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(CounterId.TypeId, new CardData("counter", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    bedtimeCard.TypeId,
                    new IEffect[] { new EarlyWinTriggerEffect() }),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    CounterId.TypeId,
                    new IEffect[]
                    {
                        new KeywordedEffect(new[] { Keyword.Counter }, new AssociatableMarkerEffect()),
                    }),
            };
            var rule = new DrowZzzRule(new InMemoryCardCatalog(entries, effects), new EffectInterpreter());

            // p1 が currentPlayer(turn=1, idx=0)、夜(Round=1)、FDP=100 で TotalPoints=100、Outcome 未設定で開始
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), new Hand(new[] { bedtimeCard })),
                new PlayerState(PlayerId.Of("p2"), new Hand(new[] { CounterId })),
            };
            var session = new DrowZzzGameSession(
                new GameState(players, Pile.Empty, Pile.Empty, Pile.Empty, new TurnState(1, 0)),
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 100, [PlayerId.Of("p2")] = 0 },
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 },
                DdpPool.Empty,
                new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
                },
                DrowZzzPhaseState.WaitingForPlay,
                outcome: null,
                bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 }, System.Array.Empty<PendingCounteredEffect>());

            var next = rule.Apply(session, new PlayCardAction(bedtimeCard));
            // Outcome が設定済 → WaitingForCounterResponse には遷移しない(IsTerminated ガード)
            Assert.That(next.PhaseState, Is.Not.EqualTo(DrowZzzPhaseState.WaitingForCounterResponse));
        }
    }
}
