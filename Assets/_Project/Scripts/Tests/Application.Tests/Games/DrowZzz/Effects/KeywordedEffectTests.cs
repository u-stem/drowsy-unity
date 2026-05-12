using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="KeywordedEffect"/> の構造的不変条件(null / empty 防御、HasKeyword 判定)と
    /// <see cref="EffectInterpreter.Apply"/> 経路での意味論(inner を逐次評価、Keywords は副作用なし)を検証する
    /// (DZ-210 / DZ-211 / DZ-212)。ADR-0011 §4 / M3-PR5a で導入。
    /// </summary>
    [TestFixture]
    public sealed class KeywordedEffectTests
    {
        // ===== ヘルパー =====

        // 注:本テストは EffectInterpreter.Apply を直接呼ぶ(Rule 経路ではない)。
        // ApplyAdjustSdp / KeywordedEffect.Apply 等の interpreter 内部は PhaseState を参照しないため、
        // session.PhaseState を WaitingForPlay で固定しても合法性ガードに影響しない(DZ-212 評価論はフェーズ独立)。
        private static DrowZzzGameSession NewSession(int sdpP1 = 0)
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, Pile.Empty,
                new TurnState(1, 0));
            var fdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var ddp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 };
            var sdp = new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = sdpP1, [PlayerId.Of("p2")] = 0 };
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
            return new DrowZzzGameSession(
                gs, fdp, ddp, sdp, DdpPool.Empty, influences, DrowZzzPhaseState.WaitingForPlay,
                outcome: null, bedDamages: bed);
        }

        // ===== DZ-210: null / empty 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-210")]
        public void Given_Keywordsがnull_When_KeywordedEffect生成_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new KeywordedEffect(keywords: null, inner: new AssociatableMarkerEffect()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-210")]
        public void Given_Innerがnull_When_KeywordedEffect生成_Then_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _ = new KeywordedEffect(keywords: new[] { Keyword.Instinct }, inner: null));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-210")]
        public void Given_空Keywords_When_KeywordedEffect生成_Then_ArgumentException()
        {
            // 1 件以上必須(空 list での wrap は無意味、ADR-0011 §4)
            Assert.Throws<ArgumentException>(() =>
                _ = new KeywordedEffect(keywords: Array.Empty<Keyword>(), inner: new AssociatableMarkerEffect()));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-210")]
        public void Given_KeywordedEffectにwith_Keywords_null_Then_ArgumentNullException()
        {
            var effect = new KeywordedEffect(new[] { Keyword.Instinct }, new AssociatableMarkerEffect());
            Assert.Throws<ArgumentNullException>(() => _ = effect with { Keywords = null });
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-210")]
        public void Given_KeywordedEffectにwith_Inner_null_Then_ArgumentNullException()
        {
            var effect = new KeywordedEffect(new[] { Keyword.Instinct }, new AssociatableMarkerEffect());
            Assert.Throws<ArgumentNullException>(() => _ = effect with { Inner = null });
        }

        // ===== DZ-211: HasKeyword 判定 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-211")]
        public void Given_Instinctを含むKeywordedEffect_When_HasKeyword_Instinct_Then_true()
        {
            var effect = new KeywordedEffect(new[] { Keyword.Instinct }, new AssociatableMarkerEffect());
            Assert.That(effect.HasKeyword(Keyword.Instinct), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-211")]
        public void Given_Instinctを含むKeywordedEffect_When_HasKeyword_Frenzy_Then_false()
        {
            var effect = new KeywordedEffect(new[] { Keyword.Instinct }, new AssociatableMarkerEffect());
            Assert.That(effect.HasKeyword(Keyword.Frenzy), Is.False);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-211")]
        public void Given_Frenzy_Instinct複数Keyword_When_HasKeyword_Counter_Then_false()
        {
            var effect = new KeywordedEffect(
                new[] { Keyword.Frenzy, Keyword.Instinct },
                new AssociatableMarkerEffect());
            Assert.That(effect.HasKeyword(Keyword.Counter), Is.False);
        }

        // ===== DZ-212: Apply 意味論(inner を逐次評価、Keywords 副作用なし)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-212")]
        public void Given_InnerがAdjustSdpEffect_When_KeywordedEffectをApply_Then_SDPがDeltaぶん変化()
        {
            // KeywordedEffect でラップしても inner の AdjustSdpEffect の効果が走る
            var interpreter = new EffectInterpreter();
            var session = NewSession(sdpP1: 10);
            var effect = new KeywordedEffect(
                new[] { Keyword.Instinct },
                new AdjustSdpEffect(SdpTarget.Self, Delta: 5));
            var next = interpreter.Apply(session, effect);
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(15));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-212")]
        public void Given_InnerがAssociatableMarker_When_KeywordedEffectをApply_Then_session不変()
        {
            // inner が no-op マーカーの場合、ラップしても no-op(Keywords は判別用で副作用なし)
            var interpreter = new EffectInterpreter();
            var session = NewSession();
            var effect = new KeywordedEffect(
                new[] { Keyword.Instinct, Keyword.Frenzy },
                new AssociatableMarkerEffect());
            var next = interpreter.Apply(session, effect);
            Assert.That(next, Is.EqualTo(session));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-212")]
        public void Given_nested_KeywordedEffect_When_Apply_Then_最内のinnerまで再帰評価()
        {
            // KeywordedEffect([Frenzy], KeywordedEffect([Instinct], AdjustSdpEffect(Self, 7)))
            //   → Frenzy / Instinct は副作用なし、最内 AdjustSdpEffect の効果が走る
            var interpreter = new EffectInterpreter();
            var session = NewSession(sdpP1: 0);
            var nested = new KeywordedEffect(
                new[] { Keyword.Frenzy },
                new KeywordedEffect(
                    new[] { Keyword.Instinct },
                    new AdjustSdpEffect(SdpTarget.Self, Delta: 7)));
            var next = interpreter.Apply(session, nested);
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(7));
        }

        // ===== 値同値性(record の Equals override 検証)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-210")]
        public void Given_同一Keywords_同一Inner_When_等値比較_Then_true()
        {
            var a = new KeywordedEffect(new[] { Keyword.Instinct }, new AssociatableMarkerEffect());
            var b = new KeywordedEffect(new[] { Keyword.Instinct }, new AssociatableMarkerEffect());
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-210")]
        public void Given_Keywords順序違い_When_等値比較_Then_false()
        {
            // [Frenzy, Instinct] と [Instinct, Frenzy] は順序保持シーケンス同値で非等値
            var a = new KeywordedEffect(new[] { Keyword.Frenzy, Keyword.Instinct }, new AssociatableMarkerEffect());
            var b = new KeywordedEffect(new[] { Keyword.Instinct, Keyword.Frenzy }, new AssociatableMarkerEffect());
            Assert.That(a, Is.Not.EqualTo(b));
        }
    }
}
