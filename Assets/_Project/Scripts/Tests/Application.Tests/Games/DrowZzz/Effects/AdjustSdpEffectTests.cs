using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Catalog;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Application.Tests.Stubs;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AdjustSdpEffect"/> を <see cref="EffectInterpreter.Apply"/> で評価した時の
    /// SDP 更新挙動を検証する(DZ-111 / DZ-112 / DZ-113)。
    /// </summary>
    /// <remarks>
    /// 普遍要件 DZ-110 (record + IEffect 実装) は <c>[Ubiquitous]</c> マーカーで構造的性質として
    /// 扱いテスト免除(`sealed record AdjustSdpEffect(...) : IEffect` の宣言で保証)。
    /// </remarks>
    [TestFixture]
    public sealed class AdjustSdpEffectTests
    {
        // ===== ヘルパー =====

        // SDP 操作テスト用の最小セッションを構築する。Players=[p1, p2]、currentPlayerIndex 引数で
        // 現プレイヤーを切り替え可能。FDP は 0 固定、SDP は引数で指定可能。
        private static DrowZzzGameSession NewSession(
            int currentPlayerIndex = 0,
            int sdpP1 = 0,
            int sdpP2 = 0)
        {
            var players = new[]
            {
                new PlayerState(PlayerId.Of("p1"), Hand.Empty),
                new PlayerState(PlayerId.Of("p2"), Hand.Empty),
            };
            var gs = new Drowsy.Domain.Game.GameState(
                players,
                Pile.Empty,
                Pile.Empty,
                Pile.Empty,
                new Drowsy.Domain.Game.TurnState(1, currentPlayerIndex));
            var fdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            var sdp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = sdpP1,
                [PlayerId.Of("p2")] = sdpP2,
            };
            // DDP / DdpPool は M2-PR4 で追加。本 fixture は SDP 操作テスト目的のため DDP=0 / 空 DdpPool で固定。
            var ddp = new Dictionary<PlayerId, int>
            {
                [PlayerId.Of("p1")] = 0,
                [PlayerId.Of("p2")] = 0,
            };
            // M2-PR5: Influences は本 fixture では空 list 固定(影響操作テストは別 fixture)
            var influences = new Dictionary<PlayerId, IReadOnlyList<PlayerInfluence>>
            {
                [PlayerId.Of("p1")] = Array.Empty<PlayerInfluence>(),
                [PlayerId.Of("p2")] = Array.Empty<PlayerInfluence>(),
            };
            return new DrowZzzGameSession(gs, fdp, ddp, sdp, DdpPool.Empty, influences, DrowZzzPhaseState.WaitingForPlay, outcome: null, bedDamages: new Dictionary<PlayerId, int> { [PlayerId.Of("p1")] = 0, [PlayerId.Of("p2")] = 0 });
        }

        // ===== DZ-111: Target=Self で現プレイヤーの SDP を変動 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-111")]
        public void Given_TargetSelf_Delta10_When_Apply_Then_現プレイヤーSDPが10増加()
        {
            // Given(現プレイヤー p1、SDP[p1]=0)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, sdpP1: 0, sdpP2: 0);
            // When
            var next = interpreter.Apply(session, new AdjustSdpEffect(SdpTarget.Self, 10));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(10));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-111")]
        public void Given_TargetSelf_Delta10_When_Apply_Then_相手のSDPは変化しない()
        {
            // Given(現プレイヤー p1、両者 SDP=0)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, sdpP1: 0, sdpP2: 0);
            // When
            var next = interpreter.Apply(session, new AdjustSdpEffect(SdpTarget.Self, 10));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(0));
        }

        // ===== DZ-112: Target=Opponent で相手の SDP を変動(N=2)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-112")]
        public void Given_TargetOpponent_Delta10_When_Apply_Then_相手のSDPが10増加()
        {
            // Given(現プレイヤー p1、Opponent = p2、両者 SDP=0)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, sdpP1: 0, sdpP2: 0);
            // When
            var next = interpreter.Apply(session, new AdjustSdpEffect(SdpTarget.Opponent, 10));
            // Then
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p2")], Is.EqualTo(10));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-112")]
        public void Given_TargetOpponent_currentPlayerIndex1_When_Apply_Then_p1のSDPが変動する()
        {
            // Given(現プレイヤー p2、Opponent = p1、両者 SDP=0)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 1, sdpP1: 0, sdpP2: 0);
            // When
            var next = interpreter.Apply(session, new AdjustSdpEffect(SdpTarget.Opponent, 10));
            // Then(現プレイヤーが変わると Opponent が解決し直す)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(10));
        }

        // ===== DZ-113: 負 Delta で SDP が負値になる(0 floor なし)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-113")]
        public void Given_SDP0_DeltaMinus5_When_Apply_Then_SDPがマイナス5になる()
        {
            // Given(現プレイヤー p1、SDP[p1]=0)
            var interpreter = new EffectInterpreter();
            var session = NewSession(currentPlayerIndex: 0, sdpP1: 0, sdpP2: 0);
            // When
            var next = interpreter.Apply(session, new AdjustSdpEffect(SdpTarget.Self, -5));
            // Then(0 floor なし、DZ-109 と整合)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-5));
        }
    }
}
