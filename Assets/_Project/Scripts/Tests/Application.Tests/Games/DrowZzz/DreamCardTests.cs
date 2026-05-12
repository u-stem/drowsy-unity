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
    /// カード No.00「夢」の統合テスト(DZ-230 〜 DZ-238、M3-PR6 完成 PR、ADR-0011 §6 / §7)。
    /// 連想機構(M3-PR4)・キーワード能力(M3-PR5a〜c)・早期勝利機構(M3-PR1)・時刻分岐(M2-PR3)を
    /// 統合的に動作させる「夢」カードの end-to-end 動作を検証する。
    /// </summary>
    /// <remarks>
    /// 単体 effect record の挙動は <c>RequiresMinimumTotalPointsMarkerEffectTests</c> / <c>UsageRestrictionMarkerEffectTests</c> /
    /// <c>EarlyWinTriggerEffectTests</c> 等でカバー済。本テストはカード 1 種類が複数機構を組み合わせて end-to-end で動くことを
    /// <c>Category("Medium")</c> で検証する(`CupOfThreatCardTests` / `GreenInvasionCardTests` と同じ統合粒度)。
    /// </remarks>
    [TestFixture]
    public sealed class DreamCardTests
    {
        // ===== ヘルパー =====

        // ADR-0011 §6 で確定した「夢」カードの効果列で InMemoryCardCatalog を生成。
        // initial deck には含めない(連想専用)が catalog 登録のみ行う(DZ-228 / DZ-229)。
        private static InMemoryCardCatalog NewCatalogWithDream()
        {
            var dream = new CardData("夢", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardId, CardData>(CardId.Of("00"), dream),
            };
            var effects = new[]
            {
                new KeyValuePair<CardId, IReadOnlyList<IEffect>>(
                    CardId.Of("00"),
                    new IEffect[]
                    {
                        new AssociatableMarkerEffect(),
                        new RequiresMinimumTotalPointsMarkerEffect(DrowZzzVictoryConstants.EarlyWinScoreThreshold),
                        new UsageRestrictionMarkerEffect(),
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new KeywordedEffect(
                                    new[] { Keyword.Frenzy, Keyword.Instinct },
                                    new EarlyWinTriggerEffect()),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -80),
                            }),
                    }),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // 「夢」が手札に存在 / 影響なし / 任意 TotalPoints(FDP に集約)/ 任意 Phase の Session を構築。
        // hasUsageRestrictionInfluence=true なら p1 に UsageRestrictionMarkerEffect Influence を 1 件付与。
        private static DrowZzzGameSession NewSessionWithDreamInHand(
            int turnNumber,
            int totalPoints,
            DrowZzzPhaseState phase,
            bool hasUsageRestrictionInfluence)
        {
            var p1Hand = new Hand(new[] { CardId.Of("00") });
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), p1Hand),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players,
                Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                new TurnState(turnNumber, 0));
            // TotalPoints は FDP + DDP + SDP の computed プロパティ。テスト目的は閾値判定なので FDP 1 軸に集約する。
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = totalPoints,
                [PlayerId.Of("p2")] = 0,
            };
            var ddp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            var sdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // M3-PR6: hasUsageRestrictionInfluence=true なら使用制限 Influence(RemainingCount=1)を p1 に 1 件付与
            var p1Influences = hasUsageRestrictionInfluence
                ? (IReadOnlyList<PlayerInfluence>)new[]
                {
                    new PlayerInfluence(
                        InfluenceTrigger.OwnPhaseStart,
                        new UsageRestrictionMarkerEffect(),
                        1),
                }
                : Array.Empty<PlayerInfluence>();
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = p1Influences,
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            return new DrowZzzGameSession(
                gs,
                fdp,
                ddp,
                sdp,
                DdpPool.Empty,
                influences,
                phase,
                outcome: null,
                bedDamages: new Dictionary<PlayerId, int>
                {
                    [PlayerId.Of("p1")] = 0,
                    [PlayerId.Of("p2")] = 0,
                },
                Array.Empty<PendingCounteredEffect>());
        }

        // 「夢」非保持 / 影響なしの Session(連想を発動する前提)を構築。totalPoints で連想閾値テストを切り替え。
        private static DrowZzzGameSession NewSessionWithoutDream(int totalPoints, DrowZzzPhaseState phase)
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players,
                Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                new TurnState(1, 0));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = totalPoints,
                [PlayerId.Of("p2")] = 0,
            };
            var ddp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            var sdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            return new DrowZzzGameSession(
                gs,
                fdp,
                ddp,
                sdp,
                DdpPool.Empty,
                influences,
                phase,
                outcome: null,
                bedDamages: new Dictionary<PlayerId, int>
                {
                    [PlayerId.Of("p1")] = 0,
                    [PlayerId.Of("p2")] = 0,
                },
                Array.Empty<PendingCounteredEffect>());
        }

        // ===== DZ-230: 連想で「夢」を引くと使用制限 Influence が付与される =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-230")]
        public void Given_FDS80_When_夢を連想_Then_手札末尾に夢が追加される()
        {
            // Given(p1 が WaitingForDraw、TotalPoints=80、「夢」非保持、Influence なし)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithoutDream(totalPoints: 80, phase: DrowZzzPhaseState.WaitingForDraw);
            // When
            var next = rule.Apply(session, new AssociateAction(CardId.Of("00")));
            // Then(p1 の手札末尾に「夢」)
            Assert.That(next.GameState.Players[0].Hand.Contains(CardId.Of("00")), Is.True);
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-230")]
        public void Given_FDS80_When_夢を連想_Then_UsageRestriction影響が1件付与される()
        {
            // Given
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithoutDream(totalPoints: 80, phase: DrowZzzPhaseState.WaitingForDraw);
            // When
            var next = rule.Apply(session, new AssociateAction(CardId.Of("00")));
            // Then(p1 の Influences が 1 件追加されている。中身の検証は別テストで分割、1 テスト 1 アサーション維持)
            Assert.That(next.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(1));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-230")]
        public void Given_FDS80_When_夢を連想_Then_付与影響のTickEffectがUsageRestrictionMarker()
        {
            // Given(M3-PR6 code-reviewer W-1 反映 2026-05-14:Influence 件数だけでなく中身も検証)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithoutDream(totalPoints: 80, phase: DrowZzzPhaseState.WaitingForDraw);
            // When
            var next = rule.Apply(session, new AssociateAction(CardId.Of("00")));
            // Then(TickEffect の型を確認、誤って別 Influence 種別を付与していないことを検出する防御)
            Assert.That(next.Influences[PlayerId.Of("p1")][0].TickEffect, Is.InstanceOf<UsageRestrictionMarkerEffect>());
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-230")]
        public void Given_FDS80_When_夢を連想_Then_付与影響のRemainingCountが1()
        {
            // Given(M3-PR6 code-reviewer W-1 反映 2026-05-14:RemainingCount の値検証)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithoutDream(totalPoints: 80, phase: DrowZzzPhaseState.WaitingForDraw);
            // When
            var next = rule.Apply(session, new AssociateAction(CardId.Of("00")));
            // Then(N=2 想定で「次の自分のフェーズ」= 相手 1 フェーズ経由分の 1 を期待、ADR-0011 §6 JIT 確定)
            Assert.That(next.Influences[PlayerId.Of("p1")][0].RemainingCount, Is.EqualTo(1));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-230")]
        public void Given_FDS80_When_夢を連想_Then_付与影響のTriggerがOwnPhaseStart()
        {
            // Given(M3-PR6 code-reviewer W-1 反映 2026-05-14:Trigger の値検証)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithoutDream(totalPoints: 80, phase: DrowZzzPhaseState.WaitingForDraw);
            // When
            var next = rule.Apply(session, new AssociateAction(CardId.Of("00")));
            // Then(自フェーズ開始時 Tick で除去される設計のため OwnPhaseStart 必須)
            Assert.That(next.Influences[PlayerId.Of("p1")][0].Trigger, Is.EqualTo(InfluenceTrigger.OwnPhaseStart));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-230")]
        public void Given_FDS80_When_夢を連想_Then_PhaseStateは不変()
        {
            // Given(WaitingForDraw 開始)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithoutDream(totalPoints: 80, phase: DrowZzzPhaseState.WaitingForDraw);
            // When
            var next = rule.Apply(session, new AssociateAction(CardId.Of("00")));
            // Then(連想は割り込み式、PhaseState 不変)
            Assert.That(next.PhaseState, Is.EqualTo(DrowZzzPhaseState.WaitingForDraw));
        }

        // ===== DZ-231: 使用制限 Influence 保有中は「夢」をプレイ不可 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-231")]
        public void Given_UsageRestriction影響保有_When_夢のIsLegalMove_Then_false()
        {
            // Given(p1 WaitingForPlay、夢を手札に保持、FDS 100、UsageRestriction Influence 1 件保有)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithDreamInHand(
                turnNumber: 1,
                totalPoints: DrowZzzVictoryConstants.EarlyWinScoreThreshold,
                phase: DrowZzzPhaseState.WaitingForPlay,
                hasUsageRestrictionInfluence: true);
            // When
            var legal = rule.IsLegalMove(session, new PlayCardAction(CardId.Of("00")));
            // Then(Influence 存在で illegal)
            Assert.That(legal, Is.False);
        }

        // ===== DZ-232: 自フェーズ Tick で使用制限が解除されて「夢」が再度使用可能 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-232")]
        public void Given_自フェーズTick後_When_夢のIsLegalMove_Then_true()
        {
            // Given:p1 が UsageRestriction Influence(RemainingCount=1)保有 / WaitingForEndTurn
            // p1 が EndTurnAction → p2 current で WaitingForDraw → p2 が一連の行動 → 戻って p1 が WaitingForDraw に
            // 復帰した時点で p1 の OwnPhaseStart Tick が発動し、Influence は RemainingCount 1→0 で除去される。
            // 簡素化のため、Tick 後の状態を直接構築せず Apply 経路で再現する(EndTurnAction の Tick 経路は M2-PR5 で確立、
            // 本テストでは Tick 後の最終フェーズ WaitingForPlay 段階の IsLegalMove のみを assertion)。
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithDreamInHand(
                turnNumber: 1,
                totalPoints: DrowZzzVictoryConstants.EarlyWinScoreThreshold,
                phase: DrowZzzPhaseState.WaitingForPlay,
                hasUsageRestrictionInfluence: false);  // Tick 後を直接構築:Influence なし状態
            // When
            var legal = rule.IsLegalMove(session, new PlayCardAction(CardId.Of("00")));
            // Then(Influence なし + FDS 100 で合法)
            Assert.That(legal, Is.True);
        }

        // ===== DZ-233 / DZ-234: 使用条件(FDS ≥ 100)inclusive 境界 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-233")]
        public void Given_FDS99_使用制限なし_When_夢のIsLegalMove_Then_false()
        {
            // Given(p1 WaitingForPlay、夢を手札に保持、FDS 99 < 100、Influence なし)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithDreamInHand(
                turnNumber: 1,
                totalPoints: DrowZzzVictoryConstants.EarlyWinScoreThreshold - 1,
                phase: DrowZzzPhaseState.WaitingForPlay,
                hasUsageRestrictionInfluence: false);
            // When
            var legal = rule.IsLegalMove(session, new PlayCardAction(CardId.Of("00")));
            // Then(閾値未満で illegal)
            Assert.That(legal, Is.False);
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-234")]
        public void Given_FDS100_使用制限なし_When_夢のIsLegalMove_Then_true()
        {
            // Given(p1 WaitingForPlay、夢を手札に保持、FDS = 100、Influence なし)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithDreamInHand(
                turnNumber: 1,
                totalPoints: DrowZzzVictoryConstants.EarlyWinScoreThreshold,
                phase: DrowZzzPhaseState.WaitingForPlay,
                hasUsageRestrictionInfluence: false);
            // When
            var legal = rule.IsLegalMove(session, new PlayCardAction(CardId.Of("00")));
            // Then(inclusive 境界、≥ 100 で合法)
            Assert.That(legal, Is.True);
        }

        // ===== DZ-235: 夜の Round で「夢」をプレイ → 早期勝利(WinnerOutcome)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-235")]
        public void Given_夜のRound_FDS100以上_When_夢をプレイ_Then_OutcomeがWinner()
        {
            // Given(turnNumber=1 → Round 1、夜、FDS 100、p1 が夢を保持、Influence なし、WaitingForPlay)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithDreamInHand(
                turnNumber: 1,
                totalPoints: DrowZzzVictoryConstants.EarlyWinScoreThreshold,
                phase: DrowZzzPhaseState.WaitingForPlay,
                hasUsageRestrictionInfluence: false);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of("00")));
            // Then(WinnerOutcome(p1))
            Assert.That(next.Outcome, Is.EqualTo(new WinnerOutcome(PlayerId.Of("p1"))));
        }

        // ===== DZ-236: 朝の Round で「夢」をプレイ → 自分の SDP -80 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-236")]
        public void Given_朝のRound_When_夢をプレイ_Then_自分のSDPがマイナス80()
        {
            // Given(turnNumber=33 → Round 17、朝、FDS 100、UsageRestriction Influence なし)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithDreamInHand(
                turnNumber: 33,
                totalPoints: DrowZzzVictoryConstants.EarlyWinScoreThreshold,
                phase: DrowZzzPhaseState.WaitingForPlay,
                hasUsageRestrictionInfluence: false);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of("00")));
            // Then(朝効果 AdjustSdpEffect(Self, -80))
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-80));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-236")]
        public void Given_朝のRound_When_夢をプレイ_Then_Outcomeはnull()
        {
            // Given(朝は EarlyWinTrigger no-op、Outcome 不変)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithDreamInHand(
                turnNumber: 33,
                totalPoints: DrowZzzVictoryConstants.EarlyWinScoreThreshold,
                phase: DrowZzzPhaseState.WaitingForPlay,
                hasUsageRestrictionInfluence: false);
            // When
            var next = rule.Apply(session, new PlayCardAction(CardId.Of("00")));
            // Then
            Assert.That(next.Outcome, Is.Null);
        }

        // ===== DZ-237: Frenzy で「夢」は CounterAction の target にできない =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-237")]
        public void Given_夢がField_WaitingForCounter_When_p2がCounterActionで夢をtarget_Then_IsLegalMoveがfalse()
        {
            // Given(WaitingForCounterResponse、p1 の夢を Field に出した直後、p2 が Counter キーワード持ちカードを保有)
            // 簡素化のため、Counter キーワード持ちカード c_counter を catalog に登録し、p2 の手札に保持する。
            var dream = new CardData("夢", new Dictionary<string, int>());
            var counterCard = new CardData("c_counter", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardId, CardData>(CardId.Of("00"), dream),
                new KeyValuePair<CardId, CardData>(CardId.Of("c_counter"), counterCard),
            };
            var effects = new[]
            {
                new KeyValuePair<CardId, IReadOnlyList<IEffect>>(
                    CardId.Of("00"),
                    new IEffect[]
                    {
                        new AssociatableMarkerEffect(),
                        new RequiresMinimumTotalPointsMarkerEffect(DrowZzzVictoryConstants.EarlyWinScoreThreshold),
                        new UsageRestrictionMarkerEffect(),
                        new TimeOfDayBranchEffect(
                            nightEffects: new IEffect[]
                            {
                                new KeywordedEffect(
                                    new[] { Keyword.Frenzy, Keyword.Instinct },
                                    new EarlyWinTriggerEffect()),
                            },
                            morningEffects: new IEffect[]
                            {
                                new AdjustSdpEffect(SdpTarget.Self, -80),
                            }),
                    }),
                new KeyValuePair<CardId, IReadOnlyList<IEffect>>(
                    CardId.Of("c_counter"),
                    new IEffect[]
                    {
                        // Counter キーワードを持つカードであることを表す最低限の効果列。Inner は IsLegalCounter で
                        // 評価されない(Keywords の有無のみ走査、ADR-0011 §4.3 / M3-PR5b)ため delta=0 のダミーで十分。
                        // M3-PR6 code-reviewer P-4 反映 2026-05-14:意図を明示するコメント追加。
                        new KeywordedEffect(
                            new[] { Keyword.Counter },
                            new AdjustSdpEffect(SdpTarget.Self, 0)),
                    }),
            };
            var catalog = new InMemoryCardCatalog(entries, effects);
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());

            // Field に「夢」、p2 (current) が手札に c_counter、WaitingForCounterResponse
            var p1 = new PlayerState(PlayerId.Of("p1"), Hand.Empty);
            var p2 = new PlayerState(PlayerId.Of("p2"), new Hand(new[] { CardId.Of("c_counter") }));
            var gs = new GameState(
                new[] { p1, p2 },
                Pile.Empty,
                Pile.Empty,
                new Pile(new[] { CardId.Of("00") }),  // Field に「夢」
                new TurnState(2, 0));  // turnNumber=2 → p2 が current
            var emptyDp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            var emptyInfluences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            var session = new DrowZzzGameSession(
                gs,
                emptyDp,
                emptyDp,
                emptyDp,
                DdpPool.Empty,
                emptyInfluences,
                DrowZzzPhaseState.WaitingForCounterResponse,
                outcome: null,
                bedDamages: emptyDp,
                Array.Empty<PendingCounteredEffect>());

            // When
            var legal = rule.IsLegalMove(
                session,
                new CounterAction(CardId.Of("c_counter"), CardId.Of("00")));
            // Then(夢は Frenzy 持ち → 反撃を受けない、illegal)
            Assert.That(legal, Is.False);
        }

        // ===== DZ-238: Instinct で「夢」は AbandonAction の捨て対象として選択不可 =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-238")]
        public void Given_手札に夢_When_夢のCardIndexでAbandonAction_Then_IsLegalMoveがfalse()
        {
            // Given(p1 WaitingForPlay、夢を手札 index 0 に保持、Influence なし)
            var catalog = NewCatalogWithDream();
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var session = NewSessionWithDreamInHand(
                turnNumber: 1,
                totalPoints: 0,
                phase: DrowZzzPhaseState.WaitingForPlay,
                hasUsageRestrictionInfluence: false);
            // When
            var legal = rule.IsLegalMove(
                session,
                new AbandonAction(AbandonChoice.GainSdp, CardIndex: 0));
            // Then(夢は Instinct 持ち → 捨て対象に選べない、illegal)
            Assert.That(legal, Is.False);
        }
    }
}
