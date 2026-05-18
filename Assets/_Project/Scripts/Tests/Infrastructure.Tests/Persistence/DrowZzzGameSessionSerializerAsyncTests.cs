using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using Drowsy.Infrastructure.Persistence;
using NUnit.Framework;
using UnityEngine.TestTools;

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
    /// <b>NUnit + UniTask の EditMode 実行(M5-PR5 で <c>[UnityTest]</c> へ修正)</b>:
    /// 実 <see cref="DrowZzzGameSessionSerializer"/> の <c>SaveAsync</c> / <c>LoadAsync</c> は
    /// <c>UniTask.RunOnThreadPool</c> で ThreadPool 上に処理を投げ、完了後にメインスレッドへ戻る。これを
    /// <c>async Task</c> + <c>.AsTask()</c> で待つと、NUnit テストランナーがメインスレッドをブロックしたまま
    /// ThreadPool タスクの継続がメインスレッドへ戻れず <b>デッドロック</b> する(Test Runner が終わらない)。
    /// そのため round-trip / ファイル不在検証は <c>[UnityTest]</c> + <c>IEnumerator</c> + <c>UniTask.ToCoroutine()</c>
    /// で書く(Unity Test Runner が coroutine として実行 → PlayerLoop が回り継続がメインスレッドへ戻れる)。
    /// 引数 null / 空白の検査は <c>RunOnThreadPool</c> 投入前に同期 throw されるため、ThreadPool を経由せず
    /// 通常の <c>[Test]</c> + sync lambda の <see cref="Throws"/> で捕捉できる。
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

        [UnityTest]
        [Category("Small"), Category("Normal"), NUnit.Framework.Property("Requirement", "INF-083")]
        public IEnumerator Given_MinimalSession_When_SaveAsyncしてLoadAsync_Then_元Sessionと等価()
            => UniTask.ToCoroutine(async () =>
            {
                // Given
                var original = DrowZzzSessionTestFixtures.MinimalSession();
                var path = TempPath();

                // When
                await _serializer.SaveAsync(original, path);
                var loaded = await _serializer.LoadAsync(path);

                // Then
                Assert.That(loaded, Is.EqualTo(original));
            });

        [UnityTest]
        [Category("Small"), Category("Normal"), NUnit.Framework.Property("Requirement", "INF-083")]
        public IEnumerator Given_全機能入りSession_When_SaveAsyncしてLoadAsync_Then_元Sessionと等価()
            => UniTask.ToCoroutine(async () =>
            {
                // Given
                var original = DrowZzzSessionTestFixtures.FullSessionWithAllFeatures();
                var path = TempPath();

                // When
                await _serializer.SaveAsync(original, path);
                var loaded = await _serializer.LoadAsync(path);

                // Then
                Assert.That(loaded, Is.EqualTo(original));
            });

        // ===== INF-084: SaveAsync session = null =====

        [Test, Category("Small"), Category("Abnormal"), NUnit.Framework.Property("Requirement", "INF-084")]
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
        [Category("Small"), Category("Abnormal"), NUnit.Framework.Property("Requirement", "INF-085")]
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
        [Category("Small"), Category("Abnormal"), NUnit.Framework.Property("Requirement", "INF-086")]
        public void Given_LoadAsync_path無効_When_呼ぶ_Then_ArgumentException(string path)
        {
            Assert.That(
                () => _serializer.LoadAsync(path),
                Throws.TypeOf<ArgumentException>());
        }

        // ===== INF-087: LoadAsync ファイル不在 =====

        // ファイル不在の FileNotFoundException は Load 本体(ThreadPool 上)で投げられるため、
        // [UnityTest] + UniTask.ToCoroutine で PlayerLoop を回し、try/catch で捕捉する。
        [UnityTest]
        [Category("Small"), Category("Abnormal"), NUnit.Framework.Property("Requirement", "INF-087")]
        public IEnumerator Given_存在しないpath_When_LoadAsync_Then_FileNotFoundException()
            => UniTask.ToCoroutine(async () =>
            {
                try
                {
                    await _serializer.LoadAsync(TempPath("not-exist.json"));
                    Assert.Fail("FileNotFoundException が投げられるべきだった");
                }
                catch (FileNotFoundException)
                {
                    // 期待通り、テスト成功(FileNotFoundException 以外は ToCoroutine が再 throw → テスト失敗)
                }
            });
    }
}
