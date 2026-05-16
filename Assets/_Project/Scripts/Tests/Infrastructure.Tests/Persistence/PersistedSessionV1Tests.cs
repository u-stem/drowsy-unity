using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using Drowsy.Infrastructure.Persistence.Models;

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="PersistedSessionV1"/> の <c>FromDomain</c> / <c>ToDomain</c> 変換 + <c>EnsureNotNull</c> 防御経路検証
    /// (B-5 第 1 弾、Infrastructure カバレッジ補完、INF-123〜127)。
    /// </summary>
    /// <remarks>
    /// <see cref="PersistedSessionV1"/> は <c>internal sealed record</c> で <c>InternalsVisibleTo("Drowsy.Infrastructure.Tests")</c>
    /// 経由で参照可能(`Assets/_Project/Scripts/Infrastructure/AssemblyInfo.cs`)。
    /// session 構築は <c>DrowZzzSessionTestFixtures.MinimalSession()</c> を再利用する。
    /// </remarks>
    [TestFixture]
    public sealed class PersistedSessionV1Tests
    {
        // ===== INF-123: FromDomain で DTO が session と一致 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-123")]
        public void Given_有効session_When_FromDomain_Then_DTOがsessionと一致()
        {
            // Given
            var session = DrowZzzSessionTestFixtures.MinimalSession();

            // When
            var dto = PersistedSessionV1.FromDomain(session);

            // Then(SchemaVersion + 5 dictionary の string key 変換 + 要素数)
            Assert.That(dto.SchemaVersion, Is.EqualTo(1));
            Assert.That(dto.GameState, Is.SameAs(session.GameState));
            Assert.That(dto.DdpPool, Is.SameAs(session.DdpPool));
            Assert.That(dto.PhaseState, Is.EqualTo(session.PhaseState));
            Assert.That(dto.Outcome, Is.EqualTo(session.Outcome));
            Assert.That(dto.PendingCounteredEffects, Is.SameAs(session.PendingCounteredEffects));
            Assert.That(dto.FirstDrowsyPoints.Count, Is.EqualTo(session.FirstDrowsyPoints.Count));
            Assert.That(dto.DrawDrowsyPoints.Count, Is.EqualTo(session.DrawDrowsyPoints.Count));
            Assert.That(dto.SecondDrowsyPoints.Count, Is.EqualTo(session.SecondDrowsyPoints.Count));
            Assert.That(dto.Influences.Count, Is.EqualTo(session.Influences.Count));
            Assert.That(dto.BedDamages.Count, Is.EqualTo(session.BedDamages.Count));
        }

        // ===== INF-124: FromDomain → ToDomain 往復で元 session と等価 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-124")]
        public void Given_FromDomainで生成したDTO_When_ToDomain_Then_元sessionと等価()
        {
            // Given
            var original = DrowZzzSessionTestFixtures.MinimalSession();
            var dto = PersistedSessionV1.FromDomain(original);

            // When
            var restored = dto.ToDomain();

            // Then(値同値)
            Assert.That(restored, Is.EqualTo(original));
        }

        // ===== INF-125: FromDomain(null) は ArgumentNullException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-125")]
        public void Given_sessionがnull_When_FromDomain_Then_ArgumentNullException()
        {
            // Given / When / Then
            var ex = Assert.Throws<ArgumentNullException>(() => PersistedSessionV1.FromDomain(null));
            Assert.That(ex.ParamName, Is.EqualTo("session"));
        }

        // ===== INF-126: GameState=null の DTO で ToDomain → InvalidOperationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-126")]
        public void Given_GameStateがnull_When_ToDomain_Then_InvalidOperationException()
        {
            // Given(他は埋めて GameState だけ null)
            var dto = BuildValidDtoBase() with { GameState = null };

            // When / Then
            var ex = Assert.Throws<InvalidOperationException>(() => dto.ToDomain());
            Assert.That(ex.Message, Does.Contain("GameState"));
        }

        // ===== INF-127: 必須 property が null の DTO で ToDomain → InvalidOperationException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-127")]
        public void Given_FirstDrowsyPointsがnull_When_ToDomain_Then_InvalidOperationException()
        {
            var dto = BuildValidDtoBase() with { FirstDrowsyPoints = null };
            var ex = Assert.Throws<InvalidOperationException>(() => dto.ToDomain());
            Assert.That(ex.Message, Does.Contain("FirstDrowsyPoints"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-127")]
        public void Given_DrawDrowsyPointsがnull_When_ToDomain_Then_InvalidOperationException()
        {
            var dto = BuildValidDtoBase() with { DrawDrowsyPoints = null };
            var ex = Assert.Throws<InvalidOperationException>(() => dto.ToDomain());
            Assert.That(ex.Message, Does.Contain("DrawDrowsyPoints"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-127")]
        public void Given_SecondDrowsyPointsがnull_When_ToDomain_Then_InvalidOperationException()
        {
            var dto = BuildValidDtoBase() with { SecondDrowsyPoints = null };
            var ex = Assert.Throws<InvalidOperationException>(() => dto.ToDomain());
            Assert.That(ex.Message, Does.Contain("SecondDrowsyPoints"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-127")]
        public void Given_DdpPoolがnull_When_ToDomain_Then_InvalidOperationException()
        {
            var dto = BuildValidDtoBase() with { DdpPool = null };
            var ex = Assert.Throws<InvalidOperationException>(() => dto.ToDomain());
            Assert.That(ex.Message, Does.Contain("DdpPool"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-127")]
        public void Given_Influencesがnull_When_ToDomain_Then_InvalidOperationException()
        {
            var dto = BuildValidDtoBase() with { Influences = null };
            var ex = Assert.Throws<InvalidOperationException>(() => dto.ToDomain());
            Assert.That(ex.Message, Does.Contain("Influences"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-127")]
        public void Given_BedDamagesがnull_When_ToDomain_Then_InvalidOperationException()
        {
            var dto = BuildValidDtoBase() with { BedDamages = null };
            var ex = Assert.Throws<InvalidOperationException>(() => dto.ToDomain());
            Assert.That(ex.Message, Does.Contain("BedDamages"));
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-127")]
        public void Given_PendingCounteredEffectsがnull_When_ToDomain_Then_InvalidOperationException()
        {
            var dto = BuildValidDtoBase() with { PendingCounteredEffects = null };
            var ex = Assert.Throws<InvalidOperationException>(() => dto.ToDomain());
            Assert.That(ex.Message, Does.Contain("PendingCounteredEffects"));
        }

        // ===== ヘルパー =====

        /// <summary>
        /// 全 property を非 null で埋めた DTO を返す(INF-126 / INF-127 の各テストで `with` で 1 property だけ
        /// null に差し替えて検証する)。
        /// </summary>
        private static PersistedSessionV1 BuildValidDtoBase() =>
            PersistedSessionV1.FromDomain(DrowZzzSessionTestFixtures.MinimalSession());
    }
}
