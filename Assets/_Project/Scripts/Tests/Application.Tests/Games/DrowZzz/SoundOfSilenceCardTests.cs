using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// カード No.04「静寂を纏う」の統合テスト(DZ-260 〜 DZ-268、ADR-0019 PR ②)。
    /// 時間帯分岐 + <see cref="ApplyTargetedRestrictionEffect"/>(動的影響付与)+
    /// <see cref="RestrictSpecificCardInfluenceEffect"/>(特定カード使用禁止 marker)の組み合わせを
    /// `IsLegalPlayCard` 拡張(TargetCardId 必須 / 相手手札所持 / AssociatedCardIds 除外)と統合的に検証する。
    /// </summary>
    [TestFixture]
    public sealed class SoundOfSilenceCardTests
    {
        // ===== ヘルパー =====

        // No.04 = SoundOfSilence のカード CardTypeId(4 引数 PlayCardAction で利用)
        private static readonly CardTypeId SilenceTypeId = CardTypeId.Of("04");
        // 相手が手札に持つ「使用禁止対象」のサンプルカード CardTypeId(本テスト fixture では catalog 登録なしの dummy)
        private static readonly CardTypeId TargetTypeId = CardTypeId.Of("X");

        // 「静寂を纏う」の効果列(2 件で構成)
        //  (1) TimeOfDayBranchEffect:時間帯依存の SDP 変動のみ(夜: 自分-12/相手+5 / 朝: 自分+5/相手-8)
        //  (2) ApplyTargetedRestrictionEffect(Opponent, 2):時間帯非依存の動的使用禁止影響付与
        // 設計判断:オーナー仕様「甲乙の選択 + 影響付与」は夜/朝共通のため最上位配置。
        //   `IsLegalPlayCard` の最上位 scan(M3-PR6「nested は walk しない」方針)でも検出されるため、
        //   `TimeOfDayBranchEffect` 内 nested 配置では illegal 判定が機能しないバグ(2026-05-17 PR ② 開発中に発覚)を回避。
        private static IEffect[] SoundOfSilenceEffects() => new IEffect[]
        {
            new TimeOfDayBranchEffect(
                nightEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, -12),
                    new AdjustSdpEffect(SdpTarget.Opponent, 5),
                },
                morningEffects: new IEffect[]
                {
                    new AdjustSdpEffect(SdpTarget.Self, 5),
                    new AdjustSdpEffect(SdpTarget.Opponent, -8),
                }),
            new ApplyTargetedRestrictionEffect(SdpTarget.Opponent, 2),
        };

        // InMemoryCardCatalog に「静寂を纏う」(CardTypeId "04") を登録して返す
        private static InMemoryCardCatalog NewCatalogWithCardFour()
        {
            var card04 = new CardData("静寂を纏う", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(SilenceTypeId, card04),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    SilenceTypeId,
                    (IReadOnlyList<IEffect>)SoundOfSilenceEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // p1 の手札に Card "04"、p2 の手札に Card "X" を持たせる session を構築(N=2)。
        // turnNumber で夜(=1)/朝(=33)を切り替える(CupOfThreatCardTests と同方式)。
        private static DrowZzzGameSession NewSession(
            int turnNumber,
            DrowZzzPhaseState phase = DrowZzzPhaseState.WaitingForPlay,
            IReadOnlyList<PlayerInfluence> p2Influences = null,
            IReadOnlyCollection<CardId> associatedCardIds = null,
            bool includeTargetInOpponentHand = true)
        {
            var p1Hand = new Hand(new[] { CardId.Of(SilenceTypeId, 0) });
            var p2Hand = includeTargetInOpponentHand
                ? new Hand(new[] { CardId.Of(TargetTypeId, 0) })
                : Hand.Empty;
            var influences = p2Influences == null
                ? null
                : new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
                {
                    [PlayerId.Of("p1")] = System.Array.Empty<PlayerInfluence>(),
                    [PlayerId.Of("p2")] = p2Influences,
                };
            return SessionFactory.NewSession(
                phase: phase,
                p0Hand: p1Hand,
                p1Hand: p2Hand,
                turnNumber: turnNumber,
                fdp: SessionFactory.Dp(p1: 0, p2: 0),
                influences: influences,
                associatedCardIds: associatedCardIds);
        }

        private static DrowZzzRule NewRule(InMemoryCardCatalog catalog) =>
            new DrowZzzRule(catalog, new EffectInterpreter());

        // ===== DZ-260 / 261 / 262: 夜のプレイ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-260")]
        public void Given_夜のフェーズ_When_Card04をプレイ_Then_自分のSDPがマイナス12()
        {
            // Given(turnNumber=1 → 夜、p1 が Card "04"、p2 が Card "X")
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(SilenceTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-12));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-261")]
        public void Given_夜のフェーズ_When_Card04をプレイ_Then_相手のSDPがプラス5()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(SilenceTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(5));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-262")]
        public void Given_夜のフェーズ_When_Card04をプレイ_Then_相手のInfluencesに使用禁止が付与される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(turnNumber: 1);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(SilenceTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then(p2 の Influences に動的構築された PlayerInfluence(OwnPhaseStart, Restrict("X"), 2) が末尾追加)
            var expected = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictSpecificCardInfluenceEffect(TargetTypeId),
                2);
            Assert.That(next.Influences[PlayerId.Of("p2")], Contains.Item(expected));
        }

        // ===== DZ-263: 朝のプレイ =====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-263")]
        public void Given_朝のフェーズ_When_Card04をプレイ_Then_自分のSDPがプラス5()
        {
            // Given(turnNumber=33 → 朝)
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(SilenceTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(5));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-263")]
        public void Given_朝のフェーズ_When_Card04をプレイ_Then_相手のSDPがマイナス8()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(SilenceTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-8));
        }

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-263")]
        public void Given_朝のフェーズ_When_Card04をプレイ_Then_相手のInfluencesに使用禁止が付与される()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(turnNumber: 33);
            // When
            var next = rule.Apply(session, new PlayCardAction(
                CardId.Of(SilenceTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0)));
            // Then
            var expected = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictSpecificCardInfluenceEffect(TargetTypeId),
                2);
            Assert.That(next.Influences[PlayerId.Of("p2")], Contains.Item(expected));
        }

        // ===== DZ-264: TargetCardId なし → illegal =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-264")]
        public void Given_TargetCardIdなし_When_Card04をIsLegalMove_Then_false()
        {
            // Given
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(turnNumber: 1);
            // When / Then(TargetCardId 未指定 = null は ApplyTargetedRestrictionEffect 持ちカードでは illegal)
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(CardId.Of(SilenceTypeId, 0))), Is.False);
        }

        // ===== DZ-265: 相手手札にない TargetCardId → illegal =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-265")]
        public void Given_相手手札にないTargetCardId_When_Card04をIsLegalMove_Then_false()
        {
            // Given(p2 が Card "X" を持たない = includeTargetInOpponentHand: false)
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(turnNumber: 1, includeTargetInOpponentHand: false);
            // When / Then
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(
                CardId.Of(SilenceTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0))), Is.False);
        }

        // ===== DZ-266: AssociatedCardIds 含有の TargetCardId → illegal =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-266")]
        public void Given_AssociatedCardIds含有のTargetCardId_When_Card04をIsLegalMove_Then_false()
        {
            // Given(p2 が Card "X" を手札に持つが、Card "X" が連想由来 = AssociatedCardIds に含まれる)
            var rule = NewRule(NewCatalogWithCardFour());
            var session = NewSession(
                turnNumber: 1,
                associatedCardIds: new[] { CardId.Of(TargetTypeId, 0) });
            // When / Then(連想由来は選択不可、ADR-0019 PR ②)
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(
                CardId.Of(SilenceTypeId, 0),
                TargetCardId: CardId.Of(TargetTypeId, 0))), Is.False);
        }

        // ===== DZ-267: 使用禁止 Influence 保有時に対象カードプレイ illegal =====

        [Test, Category("Medium"), Category("Abnormal"), Property("Requirement", "DZ-267")]
        public void Given_使用禁止InfluencePlayer_When_対象カードをIsLegalMove_Then_false()
        {
            // Given(p1 が「Card "X" 使用禁止」Influence を保有、p1 の手札に Card "X" がある状態)
            // catalog 上 Card "X" を登録(プレイ対象として扱うため)
            var card = new CardData("X", new Dictionary<string, int>());
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(TargetTypeId, card),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(
                    TargetTypeId,
                    new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) }),
            };
            var catalog = new InMemoryCardCatalog(entries, effects);
            var rule = NewRule(catalog);
            var restrictionInfluence = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictSpecificCardInfluenceEffect(TargetTypeId),
                2);
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = new[] { restrictionInfluence },
                [PlayerId.Of("p2")] = System.Array.Empty<PlayerInfluence>(),
            };
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                p0Hand: new Hand(new[] { CardId.Of(TargetTypeId, 0) }),
                turnNumber: 1,
                influences: influences);
            // When / Then(Influence に「X 使用禁止」が含まれるため Card "X" は illegal)
            Assert.That(rule.IsLegalMove(session, new PlayCardAction(CardId.Of(TargetTypeId, 0))), Is.False);
        }

        // ===== DZ-268: カウント 1 Marker は p2 フェーズ全体で機能、p2 EndTurn で除去(ADR-0020)=====

        [Test, Category("Medium"), Category("Normal"), Property("Requirement", "DZ-268")]
        public void Given_カウント1の使用禁止Influence_When_自フェーズTick_Then_カウント1で残存()
        {
            // Given(p2 が「Card "X" 使用禁止」Influence カウント 1 保有、p1 が WaitingForEndTurn → EndTurn で p2 自フェーズへ)
            // ADR-0020:p2 Tick で TickEffect (marker no-op) 適用、count 1 のまま。除去は p2 自身の EndTurn まで遅延。
            // これにより p2 フェーズ中の IsLegalPlayCard が本 Influence を Walk して Card "X" を illegal 化できる。
            var rule = NewRule(NewCatalogWithCardFour());
            var influence = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new RestrictSpecificCardInfluenceEffect(TargetTypeId),
                1);
            var sessionInPlay = NewSession(turnNumber: 1, p2Influences: new[] { influence });
            var session = sessionInPlay with { PhaseState = DrowZzzPhaseState.WaitingForEndTurn };
            // When
            var next = rule.Apply(session, new EndTurnAction());
            // Then(ADR-0020:count=1 marker は p2 フェーズで機能、count=1 のまま残存)
            Assert.That(next.Influences[PlayerId.Of("p2")].Count, Is.EqualTo(1));
            Assert.That(next.Influences[PlayerId.Of("p2")][0].RemainingCount, Is.EqualTo(1));
        }
    }
}
