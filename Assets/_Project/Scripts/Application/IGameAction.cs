namespace Drowsy.Application
{
    /// <summary>
    /// ゲーム上のアクション(プレイヤーが実行できる操作)を表すマーカー interface。
    /// メンバを持たず、各ゲームの具体アクション(例: <c>DrowZzzAction</c>)が
    /// <c>record</c> でこの interface を実装し、<see cref="IGameRule{TAction, TSession}"/> の
    /// <c>TAction</c> 型パラメータに渡せる形にする。
    /// </summary>
    /// <remarks>
    /// C# の Discriminated Union 風表現を「マーカー interface + sealed record 階層」で代替する設計。
    /// </remarks>
    public interface IGameAction
    {
    }
}
