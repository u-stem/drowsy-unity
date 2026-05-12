using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Drowsy.Domain.Cards;
using Drowsy.Infrastructure.Games.DrowZzz;

namespace Drowsy.Infrastructure.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="ScriptableObjectCardCatalog"/> の EditMode テスト(INF-004 〜 INF-012、M4-PR1 骨格)。
    /// <see cref="ScriptableObject.CreateInstance{T}"/> + <c>SetEntriesForTest</c>(internal、`InternalsVisibleTo`)で
    /// テスト経路を構築する。INF-001 / INF-002 / INF-003(Ubiquitous structural)/ INF-008(Optional、M4-PR2 で実装拡張)は
    /// テスト免除。
    /// </summary>
    [TestFixture]
    public sealed class ScriptableObjectCardCatalogTests
    {
        // ===== ヘルパー =====

        // テスト用 catalog 構築:CardEntryAsset 配列を直接設定して OnValidate 相当を実行する。
        private static ScriptableObjectCardCatalog CreateCatalog(params CardEntryAsset[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            catalog.SetEntriesForTest(entries);
            return catalog;
        }

        // 単一カード(CardId / Name のみ、属性なし)を構築するヘルパー
        private static CardEntryAsset NewEntry(string cardIdValue, string name) =>
            new CardEntryAsset(cardIdValue, name, System.Array.Empty<AttributeEntry>());

        // ===== INF-004: Get(登録済 id) → CardData =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-004")]
        public void Given_登録済id_When_Get_Then_CardDataを返す()
        {
            // Given(CardId "00" を「夢」名で登録)
            var catalog = CreateCatalog(NewEntry("00", "夢"));
            // When
            var data = catalog.Get(CardId.Of("00"));
            // Then(CardData.Name が一致)
            Assert.That(data.Name, Is.EqualTo("夢"));
        }

        // ===== INF-005: Get(未登録 id) → KeyNotFoundException =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-005")]
        public void Given_未登録id_When_Get_Then_KeyNotFoundException()
        {
            // Given(空 catalog)
            var catalog = CreateCatalog();
            // When/Then
            Assert.That(
                () => catalog.Get(CardId.Of("99")),
                Throws.TypeOf<KeyNotFoundException>());
        }

        // ===== INF-006: TryGet(登録済) → true + CardData(M4-PR1 code-reviewer W-2 反映 2026-05-14、2 件に分割)=====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-006")]
        public void Given_登録済id_When_TryGet_Then_trueを返す()
        {
            // Given
            var catalog = CreateCatalog(NewEntry("00", "夢"));
            // When
            var found = catalog.TryGet(CardId.Of("00"), out _);
            // Then(true)
            Assert.That(found, Is.True);
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-006")]
        public void Given_登録済id_When_TryGet_Then_CardDataがout引数にセットされる()
        {
            // Given
            var catalog = CreateCatalog(NewEntry("00", "夢"));
            // When
            catalog.TryGet(CardId.Of("00"), out var data);
            // Then(CardData.Name 一致)
            Assert.That(data.Name, Is.EqualTo("夢"));
        }

        // ===== INF-007: TryGet(未登録) → false + null(M4-PR1 code-reviewer W-2 反映 2026-05-14、2 件に分割)=====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-007")]
        public void Given_未登録id_When_TryGet_Then_falseを返す()
        {
            // Given
            var catalog = CreateCatalog();
            // When
            var found = catalog.TryGet(CardId.Of("99"), out _);
            // Then(false)
            Assert.That(found, Is.False);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-007")]
        public void Given_未登録id_When_TryGet_Then_out引数がnull()
        {
            // Given
            var catalog = CreateCatalog();
            // When
            catalog.TryGet(CardId.Of("99"), out var data);
            // Then(data null)
            Assert.That(data, Is.Null);
        }

        // ===== INF-009: OnValidate 重複 ID → Debug.LogError =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-009")]
        public void Given_重複CardIdValue_When_OnValidate_Then_DebugLogError()
        {
            // Given/When: 重複 CardIdValue "00" を 2 件持つ catalog を構築
            // LogAssert.Expect は SetEntriesForTest 呼び出し時の Debug.LogError をマッチ
            LogAssert.Expect(LogType.Error, new Regex("CardIdValue '00' が他 entry と重複"));
            CreateCatalog(
                NewEntry("00", "夢"),
                NewEntry("00", "夢2"));
            // Then: LogAssert で expectation を満たすか自動検証(末尾で実行)
        }

        // ===== INF-010: _entries null での graceful 動作 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-010")]
        public void Given_entries_null_When_Get_Then_KeyNotFoundException()
        {
            // Given(SetEntriesForTest で null を渡す)
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            catalog.SetEntriesForTest(null);
            // When/Then(空 catalog として Get は KeyNotFoundException)
            Assert.That(
                () => catalog.Get(CardId.Of("00")),
                Throws.TypeOf<KeyNotFoundException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-010")]
        public void Given_entries_null_When_TryGet_Then_falseとnull()
        {
            // Given
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            catalog.SetEntriesForTest(null);
            // When
            var found = catalog.TryGet(CardId.Of("00"), out var data);
            // Then
            Assert.That(!found && data is null, Is.True);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-010")]
        public void Given_entries_null_When_GetEffects_Then_空配列()
        {
            // Given
            var catalog = ScriptableObject.CreateInstance<ScriptableObjectCardCatalog>();
            catalog.SetEntriesForTest(null);
            // When
            var effects = catalog.GetEffects(CardId.Of("00"));
            // Then(空配列、本 PR 範囲では常に空)
            Assert.That(effects.Count, Is.EqualTo(0));
        }

        // ===== INF-011: 空白 / null CardIdValue の skip =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-011")]
        public void Given_空白CardIdValue_When_Get正常id_Then_他entryは影響なし()
        {
            // Given(空白 entry + 正常 entry の 2 件)
            var catalog = CreateCatalog(
                NewEntry("  ", "blank-name"),  // 空白 CardIdValue:skip 対象
                NewEntry("00", "夢"));
            // When
            var data = catalog.Get(CardId.Of("00"));
            // Then(正常 entry は影響なく Get できる)
            Assert.That(data.Name, Is.EqualTo("夢"));
        }

        // ===== INF-012: 不正 attributes(構築失敗 entry)の skip =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-012")]
        public void Given_不正attributes_When_Get正常id_Then_他entryは影響なし()
        {
            // Given(空 Name の entry → CardData ctor が ArgumentException、skip 対象 + Debug.LogError)
            // 構築失敗 entry の LogAssert を expect する
            LogAssert.Expect(LogType.Error, new Regex("entry\\[0\\].*構築に失敗"));
            var catalog = CreateCatalog(
                new CardEntryAsset("01", "", System.Array.Empty<AttributeEntry>()),  // 空 Name → 構築失敗
                NewEntry("00", "夢"));
            // When
            var data = catalog.Get(CardId.Of("00"));
            // Then(正常 entry は影響なく Get できる)
            Assert.That(data.Name, Is.EqualTo("夢"));
        }
    }
}
