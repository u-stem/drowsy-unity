using System;
using System.Collections.Generic;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using NUnit.Framework;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// カード No.18「対抗手段」の統合テスト(DZ-377 〜 DZ-388、ADR-0023、2026-05-18 で導入)。
    /// Echo キーワード機構の初導入カード。受けている影響から 1 つを選び発生源カードを再使用 + 選択影響は除去。
    /// </summary>
    [TestFixture]
    public sealed class CountermeasureCardTests
    {
        private static readonly CardTypeId CountermeasureTypeId = CardTypeId.Of("18");
        private static readonly CardTypeId DummyApplyInfluenceTypeId = CardTypeId.Of("Y");

        private static IEffect[] CountermeasureEffects() => new IEffect[]
        {
            new KeywordedEffect(new[] { Keyword.Echo }, new ReuseInfluenceSourceEffect()),
        };

        private static InMemoryCardCatalog NewCatalogWithCard18Only()
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CountermeasureTypeId, new CardData("対抗手段", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CountermeasureTypeId, (IReadOnlyList<IEffect>)CountermeasureEffects()),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // No.18 + ApplyInfluence(Self) 持ちダミーカード Y の catalog(DZ-388 動的注入検証用)
        private static InMemoryCardCatalog NewCatalogWithCard18AndApplyInfluence(IReadOnlyList<IEffect> applyInfluenceCardEffects)
        {
            var entries = new[]
            {
                new KeyValuePair<CardTypeId, CardData>(CountermeasureTypeId, new CardData("対抗手段", new Dictionary<string, int>())),
                new KeyValuePair<CardTypeId, CardData>(DummyApplyInfluenceTypeId, new CardData("Y", new Dictionary<string, int>())),
            };
            var effects = new[]
            {
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(CountermeasureTypeId, (IReadOnlyList<IEffect>)CountermeasureEffects()),
                new KeyValuePair<CardTypeId, IReadOnlyList<IEffect>>(DummyApplyInfluenceTypeId, applyInfluenceCardEffects),
            };
            return new InMemoryCardCatalog(entries, effects);
        }

        // p1 が Card "18" を 1 枚保有 + p1 が指定 Influences を保有する WaitingForPlay session を作る
        private static DrowZzzGameSession NewSessionWithCard18AndInfluences(IReadOnlyList<PlayerInfluence> p1Influences)
        {
            var card18 = CardId.Of(CountermeasureTypeId, 0);
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = p1Influences,
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            return SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                p0Hand: Hand.Empty.Add(card18),
                influences: influences);
        }

        // ---- DZ-378:Influences 空時は illegal ----

        [Test, Category("Medium"), Category("Abnormal")]
        [Property("Requirement", "DZ-378")]
        public void Given_Influences空_When_Card18をPlayCard_Then_IsLegalMoveがfalse()
        {
            // Given
            var session = NewSessionWithCard18AndInfluences(Array.Empty<PlayerInfluence>());
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 0);

            // When
            bool legal = rule.IsLegalMove(session, action);

            // Then
            Assert.That(legal, Is.False);
        }

        // ---- DZ-379:合法経路(Influences 1 件 + Choice 0)----

        [Test, Category("Medium"), Category("Normal")]
        [Property("Requirement", "DZ-379")]
        public void Given_Influences1件_Choice0_When_Card18をPlayCard_Then_IsLegalMoveがtrue()
        {
            // Given
            var inf = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, 0),
                1,
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, +5) });
            var session = NewSessionWithCard18AndInfluences(new[] { inf });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 0);

            // When
            bool legal = rule.IsLegalMove(session, action);

            // Then
            Assert.That(legal, Is.True);
        }

        // ---- DZ-380:Choice 範囲外は illegal ----

        [Test, Category("Medium"), Category("Abnormal")]
        [Property("Requirement", "DZ-380")]
        public void Given_Influences1件_Choice1_When_Card18をPlayCard_Then_IsLegalMoveがfalse()
        {
            // Given
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 0), 1);
            var session = NewSessionWithCard18AndInfluences(new[] { inf });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 1);

            // When
            bool legal = rule.IsLegalMove(session, action);

            // Then
            Assert.That(legal, Is.False);
        }

        // ---- DZ-381:Self 起点で SDP +5 ----

        [Test, Category("Medium"), Category("Normal")]
        [Property("Requirement", "DZ-381")]
        public void Given_InfluenceOriginEffectsSelfSDPプラス5_When_Card18をPlayCard_Then_自分SDPプラス5_相手不変()
        {
            // Given
            var originEffects = new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, +5) };
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 0), 1, originEffects);
            var session = NewSessionWithCard18AndInfluences(new[] { inf });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 0);

            // When
            var after = rule.Apply(session, action);

            // Then
            Assert.That(after.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(+5));
            Assert.That(after.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0));
        }

        // ---- DZ-382:Self 起点で Opponent SDP -3 ----

        [Test, Category("Medium"), Category("Normal")]
        [Property("Requirement", "DZ-382")]
        public void Given_InfluenceOriginEffectsOpponentSDPマイナス3_When_Card18をPlayCard_Then_相手SDPマイナス3_自分不変()
        {
            // Given
            var originEffects = new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, -3) };
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 0), 1, originEffects);
            var session = NewSessionWithCard18AndInfluences(new[] { inf });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 0);

            // When
            var after = rule.Apply(session, action);

            // Then
            Assert.That(after.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0));
            Assert.That(after.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(-3));
        }

        // ---- DZ-383:選択 Influence は除去(consume)----

        [Test, Category("Medium"), Category("Normal")]
        [Property("Requirement", "DZ-383")]
        public void Given_Influences3件_When_Card18をPlayCardChoice1_Then_選択Influence除去後2件()
        {
            // Given
            var inf0 = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 1), 1);
            var inf1 = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 2), 1);
            var inf2 = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 3), 1);
            var session = NewSessionWithCard18AndInfluences(new[] { inf0, inf1, inf2 });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 1);

            // When
            var after = rule.Apply(session, action);

            // Then
            var p1Influences = after.Influences[PlayerId.Of("p1")];
            Assert.That(p1Influences.Count, Is.EqualTo(2));
            // inf0 と inf2 が残る(選択 inf1 が除去された)
            Assert.That(p1Influences[0], Is.EqualTo(inf0));
            Assert.That(p1Influences[1], Is.EqualTo(inf2));
        }

        // ---- DZ-384:OriginEffects 空 list は no-op + 除去 ----

        [Test, Category("Medium"), Category("SemiNormal")]
        [Property("Requirement", "DZ-384")]
        public void Given_OriginEffects空list_When_Card18をPlayCard_Then_副作用なし_Influence除去()
        {
            // Given
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 0), 1, Array.Empty<IEffect>());
            var session = NewSessionWithCard18AndInfluences(new[] { inf });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 0);

            // When
            var after = rule.Apply(session, action);

            // Then
            Assert.That(after.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0));
            Assert.That(after.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0));
            Assert.That(after.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(0));
        }

        // ---- DZ-385:Hand から Field へ移動 ----

        [Test, Category("Medium"), Category("Normal")]
        [Property("Requirement", "DZ-385")]
        public void Given_Card18手札_When_PlayCardAction_Then_HandからRemove_FieldにAdd()
        {
            // Given
            var card18 = CardId.Of(CountermeasureTypeId, 0);
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 0), 1);
            var session = NewSessionWithCard18AndInfluences(new[] { inf });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(card18, Choice: 0);

            // When
            var after = rule.Apply(session, action);

            // Then
            Assert.That(after.GameState.Players[0].Hand.Contains(card18), Is.False);
            Assert.That(after.GameState.Field.Cards[0], Is.EqualTo(card18));
        }

        // ---- DZ-386:連鎖 Reuse 防止 ----

        [Test, Category("Medium"), Category("SemiNormal")]
        [Property("Requirement", "DZ-386")]
        public void Given_OriginEffectsApplyInfluence_When_Card18をPlayCard_Then_新Influence_OriginEffects空list()
        {
            // Given:p1 に Influence 1 件保有。その OriginEffects は ApplyInfluenceEffect(Self, newInf) で
            // Reuse 時に p1 へ新規 Influence を付与する構造。
            var newInf = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, +1),
                2);
            var originEffects = new IEffect[] { new ApplyInfluenceEffect(SdpTarget.Self, newInf) };
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 0), 1, originEffects);
            var session = NewSessionWithCard18AndInfluences(new[] { inf });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 0);

            // When
            var after = rule.Apply(session, action);

            // Then:p1 の Influences は (元 inf 除去後) + (Reuse 中に付与された newInf) = 1 件
            // その新規 Influence の OriginEffects は空 list(連鎖 Reuse 防止、ADR-0023 §5)
            var p1Influences = after.Influences[PlayerId.Of("p1")];
            Assert.That(p1Influences.Count, Is.EqualTo(1));
            Assert.That(p1Influences[0].OriginEffects.Count, Is.EqualTo(0));
        }

        // ---- DZ-387:再帰防止(ReuseInfluenceSourceEffect 自身は no-op)----

        [Test, Category("Medium"), Category("Abnormal")]
        [Property("Requirement", "DZ-387")]
        public void Given_OriginEffectsReuseInfluenceSource_When_Card18をPlayCard_Then_副作用なし_Influence除去()
        {
            // Given:OriginEffects = [ReuseInfluenceSourceEffect()] は再帰防止で no-op + Influence は除去
            var originEffects = new IEffect[] { new ReuseInfluenceSourceEffect() };
            var inf = new PlayerInfluence(InfluenceTrigger.OwnPhaseStart, new AdjustSdpEffect(SdpTarget.Self, 0), 1, originEffects);
            var session = NewSessionWithCard18AndInfluences(new[] { inf });
            var rule = new DrowZzzRule(NewCatalogWithCard18Only(), new EffectInterpreter());
            var action = new PlayCardAction(CardId.Of(CountermeasureTypeId, 0), Choice: 0);

            // When
            var after = rule.Apply(session, action);

            // Then
            Assert.That(after.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(0));
            Assert.That(after.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0));
            Assert.That(after.Influences[PlayerId.Of("p1")].Count, Is.EqualTo(0));
        }

        // ---- DZ-388:OriginEffects 動的注入(他カードプレイ時)----

        [Test, Category("Medium"), Category("Normal")]
        [Property("Requirement", "DZ-388")]
        public void Given_ApplyInfluence持ちカード_When_PlayCard_Then_新Influence_OriginEffectsはカード効果列()
        {
            // Given:Y カードは ApplyInfluence(Self, dummyInf) 1 件のみの効果列を持つ。
            var dummyInf = new PlayerInfluence(
                InfluenceTrigger.OwnPhaseStart,
                new AdjustSdpEffect(SdpTarget.Self, +2),
                3);
            var yEffects = new IEffect[] { new ApplyInfluenceEffect(SdpTarget.Self, dummyInf) };
            var catalog = NewCatalogWithCard18AndApplyInfluence(yEffects);

            var cardY = CardId.Of(DummyApplyInfluenceTypeId, 0);
            var session = SessionFactory.NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                p0Hand: Hand.Empty.Add(cardY));
            var rule = new DrowZzzRule(catalog, new EffectInterpreter());
            var action = new PlayCardAction(cardY, Choice: 0);

            // When
            var after = rule.Apply(session, action);

            // Then:p1 の Influences に dummyInf 同等(OriginEffects は Y カードの効果列スナップショット)が 1 件追加
            var p1Influences = after.Influences[PlayerId.Of("p1")];
            Assert.That(p1Influences.Count, Is.EqualTo(1));
            // OriginEffects = Y の効果列(=yEffects)が動的に詰められる
            Assert.That(p1Influences[0].OriginEffects.Count, Is.EqualTo(yEffects.Length));
            for (int i = 0; i < yEffects.Length; i++)
            {
                Assert.That(p1Influences[0].OriginEffects[i], Is.EqualTo(yEffects[i]));
            }
        }
    }
}
