using Drowsy.Application.Games.DrowZzz.Effects;

namespace Drowsy.Application.Tests.Stubs
{
    /// <summary>
    /// <see cref="EffectInterpreter.Apply"/> の <c>_</c> ケース(未知の <see cref="IEffect"/> 派生型)を
    /// テストするための dummy 型。本番コードからは参照されない(Tests assembly のみで利用)。
    /// </summary>
    /// <remarks>
    /// ADR-0006 §M1 進行中の学び §「`_` ケースカバレッジ確保」で確立した
    /// 「テスト用ダミー派生型による <c>_</c> ケースカバレッジ確保」パターンの再利用(ADR-0007 §1.3)。
    /// 将来 <see cref="IEffect"/> 派生型が増えても、本型は switch 対象外であり続けるため
    /// <c>_</c> ケースの到達経路を持続させる。
    /// </remarks>
    internal sealed record UnknownEffect : IEffect;
}
