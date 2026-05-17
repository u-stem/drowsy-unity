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
    /// <see cref="ChoiceEffect"/> の構築 / 検証 / 値同値性を検証する(DZ-166 / DZ-167 / DZ-168)。
    /// </summary>
    [TestFixture]
    public sealed class ChoiceEffectTests
    {
        // ===== DZ-166: 構築の正常系 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-166")]
        public void Given_2分岐_When_ChoiceEffectを生成_Then_Branches件数が2()
        {
            // Given
            var branches = new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, 1) },
            };
            // When
            var ce = new ChoiceEffect(branches);
            // Then
            Assert.That(ce.Branches.Count, Is.EqualTo(2));
        }

        // ===== DZ-167: 構築時の防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-167")]
        public void Given_Branchesにnull_When_ChoiceEffectを生成_Then_ArgumentNullExceptionを投げる()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ChoiceEffect(null));
            Assert.That(ex!.ParamName, Is.EqualTo("Branches"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-167")]
        public void Given_Branches1件_When_ChoiceEffectを生成_Then_ArgumentExceptionを投げる()
        {
            var branches = new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
            };
            Assert.Throws<ArgumentException>(() => new ChoiceEffect(branches));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-167")]
        public void Given_innerにnull要素_When_ChoiceEffectを生成_Then_ArgumentExceptionを投げる()
        {
            var branches = new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
                new IEffect[] { null },
            };
            Assert.Throws<ArgumentException>(() => new ChoiceEffect(branches));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-167")]
        public void Given_inner自体がnull_When_ChoiceEffectを生成_Then_ArgumentNullExceptionを投げる()
        {
            var branches = new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
                null,
            };
            var ex = Assert.Throws<ArgumentNullException>(() => new ChoiceEffect(branches));
            Assert.That(ex!.ParamName, Is.EqualTo("branches"));
        }

        // ===== DZ-168: 値同値性(順序保持シーケンス同値)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-168")]
        public void Given_同分岐構造の2インスタンス_When_Equals_Then_true()
        {
            // Given
            var a = new ChoiceEffect(new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, 1) },
            });
            var b = new ChoiceEffect(new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, 1) },
            });
            // When / Then
            Assert.That(a, Is.EqualTo(b));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-168")]
        public void Given_分岐順序が逆の2インスタンス_When_Equals_Then_false()
        {
            // Given(branches[0] と branches[1] の順を交換)
            var a = new ChoiceEffect(new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, 1) },
            });
            var b = new ChoiceEffect(new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, 1) },
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
            });
            // When / Then
            Assert.That(a, Is.Not.EqualTo(b));
        }

        // ===== DZ-169: EffectInterpreter に直接渡すと NotImplementedException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "DZ-169")]
        public void Given_ChoiceEffectをInterpreterに直接渡す_When_Apply_Then_NotImplementedExceptionを投げる()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var ce = new ChoiceEffect(new IEffect[][]
            {
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Self, -1) },
                new IEffect[] { new AdjustSdpEffect(SdpTarget.Opponent, 1) },
            });
            // When / Then(rule 経由でのみ unwrap される設計、直接渡された場合は防御例外)
            Assert.Throws<NotImplementedException>(() =>
                interpreter.Apply(BuildMinimalSession(), ce));
        }

        // ChoiceEffect の直接渡しテスト用、最小 session を構築する(他テストファイルと同パターン、共通ヘルパー化は
        // M2 全体の `docs/todo.md`「共通テストヘルパー抽出」TODO で別途追跡)。
        private static DrowZzzGameSession BuildMinimalSession()
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new GameState(
                players, Pile.Empty, Pile.Empty, Pile.Empty,
                new TurnState(1, 0));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
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
                gs, fdp, ddp, sdp,
                DdpPool.Empty,
                influences,
                DrowZzzPhaseState.WaitingForPlay, outcome: null, bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 }, System.Array.Empty<PendingCounteredEffect>());
        }
    }
}
