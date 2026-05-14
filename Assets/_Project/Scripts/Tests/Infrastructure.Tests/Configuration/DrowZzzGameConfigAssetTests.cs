using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Infrastructure.Configuration;
// NUnit と UnityEngine の双方が PropertyAttribute を提供するため曖昧参照を回避する type alias
// (M4-PR1 で確立、両 using 必須、`csharp-nunit-unityengine-property-conflict` memory 永続化済)
using Property = NUnit.Framework.PropertyAttribute;

namespace Drowsy.Infrastructure.Tests.Configuration
{
    /// <summary>
    /// <see cref="DrowZzzGameConfigAsset"/> SO の最小契約テスト(M4-PR7、ADR-0012 §3)。
    /// <see cref="ScriptableObject.CreateInstance{T}"/> 経由で構築し、
    /// <c>internal SetPoolsForTest</c> + reflection 経由の <c>Reset</c> / <c>OnValidate</c> 呼び出しで
    /// Inspector 経路を経由せず検証する(M4-PR1 で確立した <see cref="ScriptableObjectCardCatalog"/> パターン継承)。
    /// </summary>
    [TestFixture]
    public sealed class DrowZzzGameConfigAssetTests
    {
        // ===== INF-072 / INF-073: SerializeField 経由の値読み取り =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-072")]
        public void Given_FdpPoolを設定済_When_FdpPoolプロパティ_Then_設定値が読み取れる()
        {
            // Given
            var so = ScriptableObject.CreateInstance<DrowZzzGameConfigAsset>();
            so.SetPoolsForTest(new[] { 1, 2, 3 }, new[] { 10, 20 });
            // When
            var fdpPool = so.FdpPool;
            // Then
            Assert.That(fdpPool, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-073")]
        public void Given_DdpPoolを設定済_When_DdpPoolプロパティ_Then_設定値が読み取れる()
        {
            // Given
            var so = ScriptableObject.CreateInstance<DrowZzzGameConfigAsset>();
            so.SetPoolsForTest(new[] { 1 }, new[] { 10, 20, 30 });
            // When
            var ddpPool = so.DdpPool;
            // Then
            Assert.That(ddpPool, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        // ===== INF-074 / INF-075: null 時の graceful 動作 =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-074")]
        public void Given_FdpPoolがnull_When_FdpPoolプロパティ_Then_空配列を返す()
        {
            // Given(SerializeField のデフォルトは null)
            var so = ScriptableObject.CreateInstance<DrowZzzGameConfigAsset>();
            so.SetPoolsForTest(null, new[] { 0 });
            // When
            var fdpPool = so.FdpPool;
            // Then
            Assert.That(fdpPool, Is.Empty);
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-075")]
        public void Given_DdpPoolがnull_When_DdpPoolプロパティ_Then_空配列を返す()
        {
            // Given
            var so = ScriptableObject.CreateInstance<DrowZzzGameConfigAsset>();
            so.SetPoolsForTest(new[] { 0 }, null);
            // When
            var ddpPool = so.DdpPool;
            // Then
            Assert.That(ddpPool, Is.Empty);
        }

        // ===== INF-076 / INF-077: Reset() でデフォルト値復元 =====

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-076")]
        public void Given_新規インスタンス_When_Resetを呼ぶ_Then_FdpPoolがADR_0006のデフォルト10要素になる()
        {
            // Given(Reset を private で呼ぶため reflection 経由)
            var so = ScriptableObject.CreateInstance<DrowZzzGameConfigAsset>();
            // When
            InvokeReset(so);
            // Then(ADR-0006 §M1 で確定した本物 FdpPool)
            Assert.That(so.FdpPool, Is.EqualTo(new[] { 0, 10, 20, 30, 35, 40, 45, 50, 55, 60 }));
        }

        [Test, Category("Small"), Category("Normal"), Property("Requirement", "INF-077")]
        public void Given_新規インスタンス_When_Resetを呼ぶ_Then_DdpPoolがDdpPoolConstantsの39要素になる()
        {
            // Given
            var so = ScriptableObject.CreateInstance<DrowZzzGameConfigAsset>();
            // When
            InvokeReset(so);
            // Then(DdpPoolConstants.BuildDefaultPool() と同値)
            Assert.That(so.DdpPool, Is.EqualTo(DdpPoolConstants.BuildDefaultPool()));
        }

        // ===== INF-078 / INF-079: OnValidate で空 / null は Debug.LogError =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-078")]
        public void Given_FdpPoolがnull_When_OnValidateが呼ばれる_Then_DebugLogErrorが発火()
        {
            // Given
            var so = ScriptableObject.CreateInstance<DrowZzzGameConfigAsset>();
            so.SetPoolsForTest(null, new[] { 0 });
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex($"{nameof(DrowZzzGameConfigAsset)}: FdpPool が空"));
            // When
            InvokeOnValidate(so);
            // Then(LogAssert.Expect が満たされる、明示 assert は不要)
        }

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "INF-079")]
        public void Given_DdpPoolがnull_When_OnValidateが呼ばれる_Then_DebugLogErrorが発火()
        {
            // Given
            var so = ScriptableObject.CreateInstance<DrowZzzGameConfigAsset>();
            so.SetPoolsForTest(new[] { 0 }, null);
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex($"{nameof(DrowZzzGameConfigAsset)}: DdpPool が空"));
            // When
            InvokeOnValidate(so);
            // Then(LogAssert.Expect 経由で検証)
        }

        // ===== Helpers =====

        // SO の private void Reset() を reflection で呼ぶ(Editor のみで呼ばれる method を test 内で再現)
        // M4-PR7 code-reviewer W-2 反映:`Assume.That` で method not found 時に NRE を起こさず Inconclusive、
        // `TargetInvocationException` を unwrap して内側例外を直接 throw(LogAssert.Expect の発火を阻害しない)
        private static void InvokeReset(DrowZzzGameConfigAsset so)
        {
            var method = typeof(DrowZzzGameConfigAsset).GetMethod(
                "Reset",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assume.That(method, Is.Not.Null,
                "Reset method が見つかりません(rename / signature 変更時は本ヘルパーを更新)");
            try
            {
                method.Invoke(so, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        // SO の private void OnValidate() を reflection で呼ぶ
        // M4-PR7 code-reviewer W-2 反映:同上の防御パターン
        private static void InvokeOnValidate(DrowZzzGameConfigAsset so)
        {
            var method = typeof(DrowZzzGameConfigAsset).GetMethod(
                "OnValidate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assume.That(method, Is.Not.Null,
                "OnValidate method が見つかりません(rename / signature 変更時は本ヘルパーを更新)");
            try
            {
                method.Invoke(so, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }
    }
}
