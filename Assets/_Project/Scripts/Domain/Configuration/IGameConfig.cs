namespace Drowsy.Domain.Configuration
{
    /// <summary>
    /// ゲームバランス調整可能値の抽象。Phase 0 では空のひな形で、Phase 1 以降で
    /// プロパティ(InitialHandSize / MaxLifePoints / TurnLimit 等)を順次追加する。
    /// </summary>
    /// <remarks>
    /// 実装ガイドラインは <c>docs/architecture/constants-management.md</c> を参照。
    /// <list type="bullet">
    /// <item>本 interface は Drowsy.Domain で純粋 C# として宣言される(UnityEngine 非依存)</item>
    /// <item>具象実装は Drowsy.Infrastructure で <c>ScriptableObject</c> として提供される(Phase 1 以降)</item>
    /// <item>DI 登録は Drowsy.Bootstrap の VContainer LifetimeScope で行う</item>
    /// <item>テスト時は Mock / Stub 実装を注入することで異なる値で検証可能</item>
    /// </list>
    /// </remarks>
    public interface IGameConfig
    {
        // Phase 1 以降で具体プロパティを追加する。
        // 例:
        //   int InitialHandSize { get; }
        //   int MaxLifePoints { get; }
        //   System.TimeSpan TurnLimit { get; }
        //
        // 各プロパティは:
        //   - Domain ロジックから直接値として参照される(関数引数経由)
        //   - Infrastructure (GameConfigSO) の SerializeField 値が公開される
        //   - 意味と推奨範囲を docs/specs/domain/configuration/game-config.md に記述
    }
}
