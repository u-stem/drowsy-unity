using System;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Games.DrowZzz.Effects;
using Drowsy.Application.Tests.Stubs;
using NUnit.Framework;
using static Drowsy.Application.Tests.Stubs.SessionFactory;

namespace Drowsy.Application.Tests.Games.DrowZzz
{
    /// <summary>
    /// <see cref="EffectInterpreter"/> の防御要件(APP-033 / APP-034 / APP-035)を検証する。
    /// 普遍要件 APP-031 / APP-032 は <c>[Ubiquitous]</c> マーカーで構造的性質として扱い、テスト免除
    /// (sealed class / namespace 配置 / 純関数性は型シグネチャと switch のみで担保される)。
    /// </summary>
    [TestFixture]
    public sealed class EffectInterpreterTests
    {
        // ===== ヘルパー =====
        // EffectInterpreter の _ ケース(APP-033/034/035)では session の中身を参照しないため、
        // SessionFactory.NewSession() の空デッキ / 空ハンドで十分(docs/todo.md TODO #2 第 1 弾)。

        // ===== APP-033: session が null → ArgumentNullException("session") =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-033")]
        public void Given_sessionがnull_When_Apply_Then_ArgumentNullException_ParamName_session_を投げる()
        {
            // Given
            var interpreter = new EffectInterpreter();
            IEffect effect = new UnknownEffect();
            // When / Then
            var ex = Assert.Throws<ArgumentNullException>(() => interpreter.Apply(null!, effect));
            Assert.That(ex!.ParamName, Is.EqualTo("session"));
        }

        // ===== APP-034: effect が null → ArgumentNullException("effect") =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-034")]
        public void Given_effectがnull_When_Apply_Then_ArgumentNullException_ParamName_effect_を投げる()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var session = NewSession();
            // When / Then
            var ex = Assert.Throws<ArgumentNullException>(() => interpreter.Apply(session, null!));
            Assert.That(ex!.ParamName, Is.EqualTo("effect"));
        }

        // ===== APP-035: 未知の IEffect 派生型 → NotImplementedException + 型名がメッセージに含まれる =====

        [Test, Category("Small"), Category("Abnormal"), Property("Requirement", "APP-035")]
        public void Given_未知のIEffect派生型_When_Apply_Then_NotImplementedException_に派生型名が含まれる()
        {
            // Given
            var interpreter = new EffectInterpreter();
            var session = NewSession();
            IEffect effect = new UnknownEffect();
            // When / Then
            var ex = Assert.Throws<NotImplementedException>(() => interpreter.Apply(session, effect));
            Assert.That(ex!.Message, Does.Contain(nameof(UnknownEffect)));
        }
    }
}
