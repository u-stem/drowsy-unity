using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Domain.Players;
using NUnit.Framework;
using static Drowsy.Application.Tests.Stubs.SessionFactory;

namespace Drowsy.Application.Tests.Games.DrowZzz.Effects
{
    /// <summary>
    /// <see cref="AdjustSdpEffect"/> を <see cref="EffectInterpreter.Apply"/> で評価した時の
    /// SDP 更新挙動を検証する(DZ-111 / DZ-112 / DZ-113)。
    /// </summary>
    /// <remarks>
    /// 普遍要件 DZ-110 (record + IEffect 実装) は <c>[Ubiquitous]</c> マーカーで構造的性質として
    /// 扱いテスト免除(`sealed record AdjustSdpEffect(...) : IEffect` の宣言で保証)。
    /// 2026-05-16 第 2 弾で <c>Drowsy.Application.Tests.Stubs.SessionFactory.NewSession</c> 経由に統合
    /// (`using static SessionFactory` パターン、`sdp: Dp(p1: ..., p2: ...)` で初期 SDP を制御)。
    /// </remarks>
    [TestFixture]
    public sealed class AdjustSdpEffectTests
    {
        // SDP 操作テスト用の最小セッションは SessionFactory.NewSession にデフォルト 0/0 で構築される。
        // 本 fixture では現プレイヤー切替 (currentPlayerIndex) と SDP 初期値 (sdp: Dp(...)) のみ可変。

        // ===== DZ-111: Target=Self で現プレイヤーの SDP を変動 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "DZ-111")]
        public void Given_TargetSelf_Delta10_When_Apply_Then_現プレイヤーSDPが10増加()
        {
            // Given(現プレイヤー p1、SDP[p1]=0)
            var interpreter = new EffectInterpreter();
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0);
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
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0);
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
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0);
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
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 1);
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
            var session = NewSession(
                phase: DrowZzzPhaseState.WaitingForPlay,
                currentPlayerIndex: 0);
            // When
            var next = interpreter.Apply(session, new AdjustSdpEffect(SdpTarget.Self, -5));
            // Then(0 floor なし、DZ-109 と整合)
            Assert.That(next.SecondDrowsyPoints[PlayerId.Of("p1")], Is.EqualTo(-5));
        }
    }
}
