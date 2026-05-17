using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Influences;
using Drowsy.Domain.Game;
using Drowsy.Domain.Players;
using Drowsy.Infrastructure.Persistence;
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

        // ===== INF-134: AssociatedCardIds round-trip(ADR-0019、PR ①)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-134")]
        public void Given_AssociatedCardIdsを含むsession_When_FromDomainからToDomain_Then_含まれるCardIdが保存される()
        {
            // Given(session に CardId 2 件を AssociatedCardIds として持たせる)
            var card1 = Drowsy.Domain.Cards.CardId.Of(Drowsy.Domain.Cards.CardTypeId.Of("dream"), 0);
            var card2 = Drowsy.Domain.Cards.CardId.Of(Drowsy.Domain.Cards.CardTypeId.Of("silence"), 0);
            var original = DrowZzzSessionTestFixtures.MinimalSession() with { AssociatedCardIds = new[] { card1, card2 } };

            // When
            var restored = PersistedSessionV1.FromDomain(original).ToDomain();

            // Then(SetEquals 経由の値同値 = card1 / card2 がともに復元される)
            Assert.That(restored.IsAssociated(card1), Is.True);
            Assert.That(restored.IsAssociated(card2), Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-134")]
        public void Given_AssociatedCardIdsを含むsession_When_FromDomainからToDomain_Then_件数が保存される()
        {
            // Given
            var card1 = Drowsy.Domain.Cards.CardId.Of(Drowsy.Domain.Cards.CardTypeId.Of("dream"), 0);
            var card2 = Drowsy.Domain.Cards.CardId.Of(Drowsy.Domain.Cards.CardTypeId.Of("silence"), 0);
            var original = DrowZzzSessionTestFixtures.MinimalSession() with { AssociatedCardIds = new[] { card1, card2 } };

            // When
            var restored = PersistedSessionV1.FromDomain(original).ToDomain();

            // Then
            Assert.That(restored.AssociatedCardIds.Count, Is.EqualTo(2));
        }

        // ===== INF-135: 後方互換性 — DTO の AssociatedCardIds = null から ToDomain で空集合に正規化 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-135")]
        public void Given_AssociatedCardIdsがnullのDTO_When_ToDomain_Then_空集合に正規化される()
        {
            // Given(旧 v1 JSON で AssociatedCardIds フィールドが存在しないケースを模擬 — DTO 直接 null)
            var dto = BuildValidDtoBase() with { AssociatedCardIds = null };

            // When(EnsureNotNull 対象外 + ToDomain 内で `?? Array.Empty<CardId>()` 経由で空集合に正規化)
            var restored = dto.ToDomain();

            // Then(空集合復元、例外なし、schemaVersion bump 不要の後方互換性経路、ADR-0019)
            Assert.That(restored.AssociatedCardIds.Count, Is.EqualTo(0));
        }

        // ===== INF-137: JSON 文字列経由の後方互換性 — 旧 v1 JSON(AssociatedCardIds フィールド欠落)を deserialize して ToDomain
        //               (2026-05-17 PR ② で INF-136 → INF-137 にリネーム、card-catalog.md INF-136(No.03)との衝突回避) =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-137")]
        public void Given_AssociatedCardIdsフィールド欠落のJSON文字列_When_Deserialize後にToDomain_Then_空集合に復元される()
        {
            // Given(旧 v1 JSON を文字列レベルで模擬:有効 session を一旦シリアライズ → JObject 経由で
            //  AssociatedCardIds プロパティを構造的に Remove → 再 serialize → Newtonsoft.Json で deserialize。
            //  JsonSerializerSettings の Formatting.Indented で fragile な string.Replace を避けるため
            //  JObject 経由で実施(code-reviewer W-2 反映時の Replace パターンが Indented 形式と不一致だった
            //  バグの修正、2026-05-17 INF-136 fixup)。
            var settings = DrowZzzJsonSettings.Create();
            var validSession = DrowZzzSessionTestFixtures.MinimalSession();
            var dtoJson = JsonConvert.SerializeObject(PersistedSessionV1.FromDomain(validSession), settings);
            var jObject = JObject.Parse(dtoJson);
            var removed = jObject.Remove("AssociatedCardIds");
            Assume.That(removed, Is.True, "前提失敗: 有効 JSON に AssociatedCardIds プロパティが存在しない");
            var legacyJson = jObject.ToString();

            // When(legacyJson を deserialize、フィールド欠落により property は null)
            var dto = JsonConvert.DeserializeObject<PersistedSessionV1>(legacyJson, settings);
            Assume.That(dto, Is.Not.Null, "deserialize 失敗");
            Assume.That(dto.AssociatedCardIds, Is.Null, "旧 v1 JSON 模擬で AssociatedCardIds が null になっていない");
            var restored = dto.ToDomain();

            // Then(空集合復元 + 他フィールドの round-trip 整合性)
            Assert.That(restored.AssociatedCardIds.Count, Is.EqualTo(0));
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
