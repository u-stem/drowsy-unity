using VContainer;
using VContainer.Unity;

namespace Drowsy.Bootstrap
{
    /// <summary>
    /// 1 対戦寿命の VContainer LifetimeScope(M5 骨格)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §1「VContainer LifetimeScope 階層構造」+ §2「登録対象と寿命」で確定。
    /// <see cref="ProjectLifetimeScope"/> の子スコープとして 1 対戦ごとに Create / Dispose する想定。
    /// 本 PR (M5-PR1) では <b>骨格</b> のみを配置し、<see cref="Configure"/> 内では Register を行わない
    /// (実 Register は M5-PR3 以降で順次追加)。
    /// <para>
    /// <b>後続 PR で追加される登録</b>:M5-PR3 で <c>StartGameUseCase</c> / <c>ApplyActionUseCase</c> /
    /// <c>DrowZzzGamePresenter</c>(<c>AsImplementedInterfaces().AsSelf()</c>)/ <c>IDrowZzzGameView</c>
    /// 実装(<c>RegisterComponentInHierarchy</c>)を Register。詳細は ADR-0016 §2 の登録対象表を参照。
    /// </para>
    /// <para>
    /// <b>M5 範囲のスコープ生成タイミング</b>:アプリ起動直後 1 回限定(リスタート / リトライ UI なし)。
    /// Phase 3 で「新規対戦ボタン」を追加する際に <c>GameLifetimeScope.Create()</c> / <c>Dispose()</c>
    /// の繰り返し利用に拡張(ADR-0016 §1)。
    /// </para>
    /// <para>
    /// <b>sealed 設計</b>:<see cref="ProjectLifetimeScope"/> 同様、Phase 3 まで継承不要のため <c>sealed</c>
    /// で意図を明示(ADR-0016 §6)。
    /// </para>
    /// </remarks>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        /// <inheritdoc />
        protected override void Configure(IContainerBuilder builder)
        {
            // M5-PR1 段階では空。M5-PR3 で UseCase / Presenter / View の Register 群を追加する。
        }
    }
}
