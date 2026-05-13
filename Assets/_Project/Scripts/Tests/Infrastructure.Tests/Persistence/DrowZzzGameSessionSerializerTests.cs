using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Game;
using Drowsy.Infrastructure.Persistence;
// NUnit と UnityEngine の双方が PropertyAttribute を提供するため曖昧参照を回避する type alias
// (M4-PR1 で確立、両 using 必須、`csharp-nunit-unityengine-property-conflict` memory 永続化済)
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="DrowZzzGameSessionSerializer"/> の Save / Load round-trip 検証(M4-PR5、INF-048〜INF-071)。
    /// </summary>
    /// <remarks>
    /// テスト path は <see cref="Path.GetTempPath"/> 起点の <see cref="Guid.NewGuid"/> ベースで隔離する
    /// (Unity の <c>UnityEngine.Application.persistentDataPath</c> を直接書き込まない、テスト並列実行時の衝突回避)。
    /// 各 <see cref="TearDown"/> で生成した一時ディレクトリを削除する。
    /// </remarks>
    [TestFixture]
    public sealed class DrowZzzGameSessionSerializerTests
    {
        private string _tempDir;
        private DrowZzzGameSessionSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "drowzzz-tests", Guid.NewGuid().ToString("N"));
            _serializer = new DrowZzzGameSessionSerializer();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private string TempPath(string fileName = "session.json") => Path.Combine(_tempDir, fileName);

        // ===== INF-052 + INF-054: Save → Load 主経路 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-054")]
        public void Given_全機能入りSession_When_SaveしてLoad_Then_元Sessionと等価()
        {
            // Given
            var original = DrowZzzSessionTestFixtures.FullSessionWithAllFeatures();
            var path = TempPath();

            // When
            _serializer.Save(original, path);
            var loaded = _serializer.Load(path);

            // Then
            Assert.That(loaded, Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-052")]
        public void Given_親ディレクトリ未作成_When_Save_Then_自動作成される()
        {
            // Given(_tempDir 自体が未作成、ファイルパスが nested)
            var nested = Path.Combine(_tempDir, "nested", "subdir", "session.json");

            // When
            _serializer.Save(DrowZzzSessionTestFixtures.MinimalSession(), nested);

            // Then
            Assert.That(File.Exists(nested), Is.True);
        }

        // ===== INF-053: 上書き =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-053")]
        public void Given_既存ファイル_When_Save_Then_上書きされる()
        {
            // Given(旧 session を保存済)
            var path = TempPath();
            _serializer.Save(DrowZzzSessionTestFixtures.MinimalSession(), path);

            // When(新 session で上書き)
            var newSession = DrowZzzSessionTestFixtures.SessionWithWinnerOutcome();
            _serializer.Save(newSession, path);
            var loaded = _serializer.Load(path);

            // Then
            Assert.That(loaded, Is.EqualTo(newSession));
        }

        // ===== INF-049: UTF-8 BOM なし =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-049")]
        public void Given_Save後_When_ファイルバイト先頭を確認_Then_UTF8_BOMが付かない()
        {
            // Given/When
            var path = TempPath();
            _serializer.Save(DrowZzzSessionTestFixtures.MinimalSession(), path);
            var bytes = File.ReadAllBytes(path);

            // Then(UTF-8 BOM = 0xEF 0xBB 0xBF を持たない)
            Assert.That(bytes.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(
                (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF),
                Is.False,
                "セーブファイルが UTF-8 BOM 付きで書き込まれている(BOM なしが要件)");
        }

        // ===== INF-058: Outcome=null round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-058")]
        public void Given_Outcome_null_When_SaveしてLoad_Then_Outcomeはnull()
        {
            // Given
            var original = DrowZzzSessionTestFixtures.MinimalSession();
            Assume.That(original.Outcome, Is.Null);
            var path = TempPath();

            // When
            _serializer.Save(original, path);
            var loaded = _serializer.Load(path);

            // Then
            Assert.That(loaded.Outcome, Is.Null);
        }

        // ===== INF-056: WinnerOutcome の serialize 形 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-056")]
        public void Given_Outcome_WinnerOutcome_When_Save_Then_typeとwinnerが書き出される()
        {
            // Given
            var session = DrowZzzSessionTestFixtures.SessionWithWinnerOutcome();
            var path = TempPath();

            // When
            _serializer.Save(session, path);
            var json = JObject.Parse(File.ReadAllText(path));

            // Then
            var outcome = json["Outcome"];
            Assert.That(outcome, Is.Not.Null);
            Assert.That(outcome["type"]?.ToString(), Is.EqualTo("Winner"));
            Assert.That(outcome["winner"]?.ToString(), Is.EqualTo("PlayerA"));
        }

        // ===== INF-057: DrawOutcome の serialize 形 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-057")]
        public void Given_Outcome_DrawOutcome_When_Save_Then_typeのみ書き出される()
        {
            // Given
            var session = DrowZzzSessionTestFixtures.SessionWithDrawOutcome();
            var path = TempPath();

            // When
            _serializer.Save(session, path);
            var json = JObject.Parse(File.ReadAllText(path));

            // Then
            var outcome = json["Outcome"];
            Assert.That(outcome, Is.Not.Null);
            Assert.That(outcome["type"]?.ToString(), Is.EqualTo("Draw"));
            Assert.That(outcome["winner"], Is.Null);
        }

        // ===== INF-060: 存在しないファイル =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-060")]
        public void Given_存在しないpath_When_Load_Then_FileNotFoundException()
        {
            // Given(_tempDir すら未作成)
            var path = TempPath("nonexistent.json");

            // When/Then
            Assert.That(() => _serializer.Load(path), Throws.TypeOf<FileNotFoundException>());
        }

        // ===== INF-061: session=null =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-061")]
        public void Given_sessionがnull_When_Save_Then_ArgumentNullException()
        {
            // When/Then
            Assert.That(
                () => _serializer.Save(null, TempPath()),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ===== INF-062: Save の path 空白 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-062")]
        public void Given_Save_path_null_When_呼ぶ_Then_ArgumentException()
        {
            Assert.That(
                () => _serializer.Save(DrowZzzSessionTestFixtures.MinimalSession(), null),
                Throws.TypeOf<ArgumentException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-062")]
        public void Given_Save_path_空白_When_呼ぶ_Then_ArgumentException()
        {
            Assert.That(
                () => _serializer.Save(DrowZzzSessionTestFixtures.MinimalSession(), "  "),
                Throws.TypeOf<ArgumentException>());
        }

        // ===== INF-063: Load の path 空白 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-063")]
        public void Given_Load_path_null_When_呼ぶ_Then_ArgumentException()
        {
            Assert.That(() => _serializer.Load(null), Throws.TypeOf<ArgumentException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-063")]
        public void Given_Load_path_空白_When_呼ぶ_Then_ArgumentException()
        {
            Assert.That(() => _serializer.Load("  "), Throws.TypeOf<ArgumentException>());
        }

        // ===== INF-064: 破損 JSON =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-064")]
        public void Given_破損JSON_When_Load_Then_InvalidDataException()
        {
            // Given
            var path = TempPath();
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(path, "{ this-is-not-valid-json");

            // When/Then
            Assert.That(() => _serializer.Load(path), Throws.TypeOf<InvalidDataException>());
        }

        // ===== INF-065: schemaVersion 不一致 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-065")]
        public void Given_schemaVersion不一致_When_Load_Then_InvalidDataException()
        {
            // Given(まず正常 save → SchemaVersion を改ざん)
            var path = TempPath();
            _serializer.Save(DrowZzzSessionTestFixtures.MinimalSession(), path);
            var json = JObject.Parse(File.ReadAllText(path));
            json["SchemaVersion"] = 999;
            File.WriteAllText(path, json.ToString());

            // When/Then
            Assert.That(
                () => _serializer.Load(path),
                Throws.TypeOf<InvalidDataException>().With.Message.Contains("999"));
        }

        // ===== INF-070: 必須プロパティ欠落 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-070")]
        public void Given_GameStateプロパティ欠落_When_Load_Then_InvalidOperationException()
        {
            // Given(まず正常 save → GameState を削除)
            var path = TempPath();
            _serializer.Save(DrowZzzSessionTestFixtures.MinimalSession(), path);
            var json = JObject.Parse(File.ReadAllText(path));
            json.Remove("GameState");
            File.WriteAllText(path, json.ToString());

            // When/Then
            Assert.That(
                () => _serializer.Load(path),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("GameState"));
        }

        // ===== INF-059: DefaultSavePath が persistentDataPath/drowzzz/<fileName> を返す =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-059")]
        public void Given_fileName_When_DefaultSavePath_Then_persistentDataPath_drowzzz_fileNameを返す()
        {
            // Given/When
            var result = DrowZzzGameSessionSerializer.DefaultSavePath("custom.json");

            // Then(Path.Combine 経路で persistentDataPath / "drowzzz" / "custom.json" が結合される)
            var expected = Path.Combine(UnityEngine.Application.persistentDataPath, "drowzzz", "custom.json");
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-059")]
        public void Given_fileName省略_When_DefaultSavePath_Then_session_jsonが既定値()
        {
            // Given/When
            var result = DrowZzzGameSessionSerializer.DefaultSavePath();

            // Then
            var expected = Path.Combine(UnityEngine.Application.persistentDataPath, "drowzzz", "session.json");
            Assert.That(result, Is.EqualTo(expected));
        }

        // ===== INF-071: DefaultSavePath の fileName 空白 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-071")]
        public void Given_fileName_null_When_DefaultSavePath_Then_ArgumentException()
        {
            Assert.That(() => DrowZzzGameSessionSerializer.DefaultSavePath(null), Throws.TypeOf<ArgumentException>());
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-071")]
        public void Given_fileName_空白_When_DefaultSavePath_Then_ArgumentException()
        {
            Assert.That(() => DrowZzzGameSessionSerializer.DefaultSavePath("  "), Throws.TypeOf<ArgumentException>());
        }
    }
}
