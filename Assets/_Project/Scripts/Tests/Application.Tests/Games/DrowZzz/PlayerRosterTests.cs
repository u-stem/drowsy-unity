using System;
using System.Collections.Generic;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Players;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="PlayerRoster"/> 値オブジェクトの単体テスト(ROSTER-002 / ROSTER-003 / ROSTER-004)。
    /// 構造的性質(ROSTER-001 = sealed record)は <c>[Ubiquitous]</c> マーカーでテスト免除し、
    /// 本 fixture は防御要件と Players プロパティの順序保持を検証する。
    /// </summary>
    /// <remarks>
    /// ADR-0017 で確定した PlayerRoster wrapper の不変条件(ctor で null + 空の早期検出)を網羅する。
    /// </remarks>
    [TestFixture]
    public sealed class PlayerRosterTests
    {
        // ===== ROSTER-002: ctor null 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "ROSTER-002")]
        public void Given_playersNull_When_Ctor_Then_ArgumentNullException()
        {
            // Given / When / Then
            var ex = Assert.Throws<ArgumentNullException>(() => _ = new PlayerRoster(null));
            Assert.That(ex!.ParamName, Is.EqualTo("players"));
        }

        // ===== ROSTER-003: ctor empty 防御 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "ROSTER-003")]
        public void Given_playersEmpty_When_Ctor_Then_ArgumentException()
        {
            // Given
            IReadOnlyList<PlayerId> empty = Array.Empty<PlayerId>();

            // When / Then
            // Throws.TypeOf<T>() は厳密型一致(NUnit 仕様、`Throws.Exactly<T>()` 相当)。
            // ArgumentNullException は ArgumentException の派生だが TypeOf では弾かれるため、
            // 「空配列入力には ArgumentException(基底型ぴったり)を投げる」契約を厳密検証する
            // (null 入力経路の ArgumentNullException との混同を防ぐ)。code-reviewer P-2 反映。
            Assert.That(
                () => _ = new PlayerRoster(empty),
                Throws.TypeOf<ArgumentException>());
        }

        // ===== ROSTER-004: 非空の players を Players プロパティで順序保持公開 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "ROSTER-004")]
        public void Given_nonEmptyPlayers_When_Ctor_Then_PlayersPreservesOrder()
        {
            // Given
            var p1 = PlayerId.Of("p1");
            var p2 = PlayerId.Of("p2");
            IReadOnlyList<PlayerId> players = new[] { p1, p2 };

            // When
            var roster = new PlayerRoster(players);

            // Then(順序保持で公開)
            Assert.That(roster.Players, Is.EqualTo(new[] { p1, p2 }));
        }
    }
}
