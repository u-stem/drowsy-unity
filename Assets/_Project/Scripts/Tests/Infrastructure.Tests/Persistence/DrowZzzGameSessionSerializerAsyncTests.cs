using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Drowsy.Infrastructure.Persistence;
// 本ファイルは UnityEngine を import しないため、`Property` の type alias は不要
// (NUnit / UnityEngine の PropertyAttribute 衝突は UnityEngine import 時のみ発生、
// `csharp-nunit-unityengine-property-conflict` memory 参照)。`using NUnit.Framework` 経由で
// [Property(...)] が NUnit.Framework.PropertyAttribute に解決される。

namespace Drowsy.Infrastructure.Tests.Persistence
{
    /// <summary>
    /// <see cref="DrowZzzGameSessionSerializer"/> の非同期 API(<c>SaveAsync</c> / <c>LoadAsync</c>)の
    /// round-trip / 引数防御検証(M5-PR5、INF-083〜INF-087)。
    /// </summary>
    /// <remarks>
    /// 非同期 API は M5-PR1 で「同期 <c>Save</c> / <c>Load</c> を <c>UniTask.RunOnThreadPool</c> ラップ」として
    /// 実装済(ADR-0016 §5.2 初期推奨)。本テストはその round-trip と例外契約が同期版と等価であることを確認する。
    /// <para>
    /// <b>NUnit + UniTask</b>:NUnit 3.x は <c>UniTask</c> 戻り値を直接認識できないため、<c>UniTask</c> は
    /// <c>.AsTask()</c> で <c>System.Threading.Tasks.Task</c> に変換した上で <c>await</c> する(M5-PR1 で確立)。
    /// 引数 null / 空白の検査は <c>SaveAsync</c> / <c>LoadAsync</c> が <c>RunOnThreadPool</c> 投入前に同期実行する
    /// ため、引数防御テストは sync lambda の <see cref="Throws"/> で捕捉する(M5-PR1 契約テストと同パターン)。
    /// ファイル不在の <see cref="FileNotFoundException"/> は <c>Load</c> 本体(ThreadPool 上)で投げられるため
    /// <see cref="Assert.ThrowsAsync"/> で捕捉する。
    /// </para>
    /// <para>
    /// テスト path は <see cref="Path.GetTempPath"/> 起点の <see cref="Guid.NewGuid"/> ベースで隔離する
    /// (同期版テスト <c>DrowZzzGameSessionSerializerTests</c> と同パターン)。
    /// </para>
    /// <para>
    /// <b>INF-083</b> は 2 テスト(`MinimalSession` / `FullSessionWithAllFeatures` の 2 フィクスチャ)で
    /// カバーする。同一 <c>Property("Requirement", "INF-083")</c> を 2 メソッドが持つのは typo ではなく、
    /// EARS 上 INF-083 が「両フィクスチャの round-trip を検証する単一要件」として定義されているため
    /// (`check-traceability.sh` は <c>sort -u</c> で重複除去するため機械検証上も整合)。
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class DrowZzzGameSessionSerializerAsyncTests
    {
        private string _tempDir;
        private DrowZzzGameSessionSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "drowzzz-tests-async", Guid.NewGuid().ToString("N"));
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

        // ===== INF-083: SaveAsync → LoadAsync round-trip =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-083")]
        public async Task Given_MinimalSession_When_SaveAsyncしてLoadAsync_Then_元Sessionと等価()
        {
            // Given
            var original = DrowZzzSessionTestFixtures.MinimalSession();
            var path = TempPath();

            // When
            await _serializer.SaveAsync(original, path).AsTask();
            var loaded = await _serializer.LoadAsync(path).AsTask();

            // Then
            Assert.That(loaded, Is.EqualTo(original));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-083")]
        public async Task Given_全機能入りSession_When_SaveAsyncしてLoadAsync_Then_元Sessionと等価()
        {
            // Given
            var original = DrowZzzSessionTestFixtures.FullSessionWithAllFeatures();
            var path = TempPath();

            // When
            await _serializer.SaveAsync(original, path).AsTask();
            var loaded = await _serializer.LoadAsync(path).AsTask();

            // Then
            Assert.That(loaded, Is.EqualTo(original));
        }

        // ===== INF-084: SaveAsync session = null =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-084")]
        public void Given_sessionがnull_When_SaveAsync_Then_ArgumentNullException()
        {
            // SaveAsync は RunOnThreadPool 投入前に同期 throw するため sync lambda で捕捉できる。
            Assert.That(
                () => _serializer.SaveAsync(null, TempPath()),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ===== INF-085: SaveAsync path 無効 =====

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [Category("Small"), Category("Abnormal"), Property("Requirement", "INF-085")]
        public void Given_SaveAsync_path無効_When_呼ぶ_Then_ArgumentException(string path)
        {
            var session = DrowZzzSessionTestFixtures.MinimalSession();

            Assert.That(
                () => _serializer.SaveAsync(session, path),
                Throws.TypeOf<ArgumentException>());
        }

        // ===== INF-086: LoadAsync path 無効 =====

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [Category("Small"), Category("Abnormal"), Property("Requirement", "INF-086")]
        public void Given_LoadAsync_path無効_When_呼ぶ_Then_ArgumentException(string path)
        {
            Assert.That(
                () => _serializer.LoadAsync(path),
                Throws.TypeOf<ArgumentException>());
        }

        // ===== INF-087: LoadAsync ファイル不在 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-087")]
        public void Given_存在しないpath_When_LoadAsync_Then_FileNotFoundException()
        {
            // ファイル不在の FileNotFoundException は Load 本体(ThreadPool 上)で投げられるため
            // Assert.ThrowsAsync で捕捉する(.AsTask() で UniTask → Task 変換)。
            Assert.ThrowsAsync<FileNotFoundException>(
                async () => await _serializer.LoadAsync(TempPath("not-exist.json")).AsTask());
        }
    }
}
