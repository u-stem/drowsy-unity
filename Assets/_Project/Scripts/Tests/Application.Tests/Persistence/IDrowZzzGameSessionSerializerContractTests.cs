using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Persistence;
using Drowsy.Application.Tests.Stubs;
using NUnit.Framework;
using static Drowsy.Application.Tests.Stubs.SessionFactory;

namespace Drowsy.Application.Tests.Persistence
{
    /// <summary>
    /// <see cref="IDrowZzzGameSessionSerializer"/> の契約テスト(M5-PR1)。
    /// </summary>
    /// <remarks>
    /// <see cref="IDrowZzzGameSessionSerializer"/> interface 契約(引数検査・例外契約・Async/Sync 等価性)を
    /// <see cref="InMemoryDrowZzzGameSessionSerializer"/> fake 経由で機械検証する。
    /// JSON シリアライズの詳細は Infrastructure.Tests 側の 43 テスト(M4-PR5)で担保。
    /// <para>
    /// <b>例外型判別</b>:<see cref="ArgumentNullException"/> は <see cref="ArgumentException"/> の派生
    /// なので、Save(null, "any") の検査と Save(session, null) の検査を区別するために
    /// <see cref="Throws.TypeOf{T}"/>(派生型を含まない厳密一致)を使う。
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class IDrowZzzGameSessionSerializerContractTests
    {
        /// <summary>テスト対象を interface 型で生成して、interface 契約のみに依存することを明示する。</summary>
        private static IDrowZzzGameSessionSerializer NewSerializer()
            => new InMemoryDrowZzzGameSessionSerializer();

        // ---- APP-045: Save 引数 session = null ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-045")]
        public void Given_sessionNull_When_Save_Then_ArgumentNullException()
        {
            // Given
            var serializer = NewSerializer();

            // When / Then
            Assert.That(
                () => serializer.Save(null, "any"),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---- APP-046: Save 引数 path 無効(null / empty / whitespace) ----

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [Category("Small"), Category("Abnormal"), Property("Requirement", "APP-046")]
        public void Given_pathInvalid_When_Save_Then_ArgumentException(string path)
        {
            // Given
            var serializer = NewSerializer();
            var session = NewSession();

            // When / Then(ArgumentException 厳密一致、ArgumentNullException は session null 経路のみ)
            Assert.That(
                () => serializer.Save(session, path),
                Throws.TypeOf<ArgumentException>());
        }

        // ---- APP-047: Save → Load round-trip ----

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-047")]
        public void Given_savedSession_When_Load_Then_ReturnsSavedSession()
        {
            // Given
            var serializer = NewSerializer();
            var session = NewSession();
            serializer.Save(session, "path/a");

            // When
            var loaded = serializer.Load("path/a");

            // Then(fake は変換なしのため same reference を確認、Infrastructure 側は別途 round-trip 検証済)
            Assert.That(loaded, Is.SameAs(session));
        }

        // ---- APP-048: Load 引数 path 無効 ----

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [Category("Small"), Category("Abnormal"), Property("Requirement", "APP-048")]
        public void Given_pathInvalid_When_Load_Then_ArgumentException(string path)
        {
            // Given
            var serializer = NewSerializer();

            // When / Then
            Assert.That(
                () => serializer.Load(path),
                Throws.TypeOf<ArgumentException>());
        }

        // ---- APP-049: Load 未保存 path ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-049")]
        public void Given_unsavedPath_When_Load_Then_FileNotFoundException()
        {
            // Given
            var serializer = NewSerializer();

            // When / Then
            Assert.That(
                () => serializer.Load("path/not-saved"),
                Throws.TypeOf<FileNotFoundException>());
        }

        // ---- APP-050: SaveAsync 引数 session = null ----

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-050")]
        public void Given_sessionNull_When_SaveAsync_Then_ArgumentNullException()
        {
            // Given
            var serializer = NewSerializer();

            // When / Then(同期的に validate して throw する設計のため Assert.Throws で捕捉できる)
            Assert.That(
                () => serializer.SaveAsync(null, "any"),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---- APP-051: SaveAsync 引数 path 無効 ----

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [Category("Small"), Category("Abnormal"), Property("Requirement", "APP-051")]
        public void Given_pathInvalid_When_SaveAsync_Then_ArgumentException(string path)
        {
            // Given
            var serializer = NewSerializer();
            var session = NewSession();

            // When / Then
            Assert.That(
                () => serializer.SaveAsync(session, path),
                Throws.TypeOf<ArgumentException>());
        }

        // ---- APP-052: SaveAsync → LoadAsync round-trip ----

        // NUnit 3.x の async サポートは System.Threading.Tasks.Task のみで、UniTask 戻り値を
        // 直接認識できず AsyncTaskInvocationRegion が NullReferenceException を投げる。
        // そのため UniTask は .AsTask() で System.Threading.Tasks.Task に変換した上で await する。
        [Test, Category("Small"), Category("Normal"), Property("Requirement", "APP-052")]
        public async System.Threading.Tasks.Task Given_savedSessionAsync_When_LoadAsync_Then_ReturnsSavedSession()
        {
            // Given
            var serializer = NewSerializer();
            var session = NewSession();
            await serializer.SaveAsync(session, "path/a").AsTask();

            // When
            var loaded = await serializer.LoadAsync("path/a").AsTask();

            // Then
            Assert.That(loaded, Is.SameAs(session));
        }

        // ---- APP-053: SaveAsync cancelled token ----

        // InMemoryDrowZzzGameSessionSerializer.SaveAsync は ct.ThrowIfCancellationRequested() を
        // 同期実行するため、UniTask を返す前に同期で OperationCanceledException が投げられる。
        // よって async lambda ではなく sync lambda で Throws を検証する(NUnit の UniTask 非対応回避)。
        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-053")]
        public void Given_cancelledToken_When_SaveAsync_Then_OperationCanceledException()
        {
            // Given
            var serializer = NewSerializer();
            var session = NewSession();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // When / Then
            Assert.That(
                () => serializer.SaveAsync(session, "path/a", cts.Token),
                Throws.InstanceOf<OperationCanceledException>());
        }

        // ---- APP-054: LoadAsync 引数 path 無効 ----

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [Category("Small"), Category("Abnormal"), Property("Requirement", "APP-054")]
        public void Given_pathInvalid_When_LoadAsync_Then_ArgumentException(string path)
        {
            // Given
            var serializer = NewSerializer();

            // When / Then
            Assert.That(
                () => serializer.LoadAsync(path),
                Throws.TypeOf<ArgumentException>());
        }

        // ---- APP-055: LoadAsync 未保存 path ----

        // InMemoryDrowZzzGameSessionSerializer.LoadAsync は本体を同期実行し、未保存 path の
        // FileNotFoundException も同期で投げられる(UniTask が返る前)。sync lambda で検証する。
        // 本動作は fake 固有(Infrastructure 具象は UniTask.RunOnThreadPool 経由で ThreadPool 上で
        // 投げられるため、Infrastructure 側 round-trip テストは Infrastructure.Tests で M4-PR5 が担保)。
        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-055")]
        public void Given_unsavedPath_When_LoadAsync_Then_FileNotFoundException()
        {
            // Given
            var serializer = NewSerializer();

            // When / Then
            Assert.That(
                () => serializer.LoadAsync("path/not-saved"),
                Throws.InstanceOf<FileNotFoundException>());
        }

        // ---- APP-056: LoadAsync cancelled token ----

        // SaveAsync cancelled と同じく ct.ThrowIfCancellationRequested() の同期 throw を sync lambda で捕捉。
        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-056")]
        public void Given_cancelledToken_When_LoadAsync_Then_OperationCanceledException()
        {
            // Given
            var serializer = NewSerializer();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // When / Then
            Assert.That(
                () => serializer.LoadAsync("path/a", cts.Token),
                Throws.InstanceOf<OperationCanceledException>());
        }
    }
}
