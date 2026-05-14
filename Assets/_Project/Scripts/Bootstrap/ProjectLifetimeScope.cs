using VContainer;
using VContainer.Unity;

namespace Drowsy.Bootstrap
{
    /// <summary>
    /// アプリ全体寿命の VContainer LifetimeScope(M5 骨格)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §1「VContainer LifetimeScope 階層構造」+ §7.2「Bootstrap への注入経路」で確定。
    /// 本 PR (M5-PR1) では <b>骨格</b> のみを配置し、<see cref="Configure"/> 内では Register を行わない
    /// (実 Register は M5-PR3 以降で順次追加)。
    /// <para>
    /// <b>後続 PR で追加される登録</b>:M5-PR3 で <c>IGameConfig</c> / <c>ICardCatalog&lt;IEffect&gt;</c> /
    /// <c>IRandomSource</c> / <c>IDrowZzzGameSessionSerializer</c> / <c>IUserSettings</c> / <c>DrowZzzRule</c>
    /// / <c>DefaultSavePath</c> を Register、同時に <c>[SerializeField]</c> による SO Asset 注入と null チェックを
    /// 導入する。詳細は ADR-0016 §7.2 のスニペットを参照。
    /// </para>
    /// <para>
    /// <b>sealed 設計</b>:VContainer の <see cref="LifetimeScope"/> は継承前提だが、本プロジェクトでは
    /// Phase 3 の Scene 切替まで拡張不要(ADR-0016 §6 で確定)のため <c>sealed</c> で意図を明示する。
    /// </para>
    /// </remarks>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        /// <inheritdoc />
        protected override void Configure(IContainerBuilder builder)
        {
            // M5-PR1 段階では空。M5-PR3 で SerializeField + null チェック + Register 群を追加する。
        }
    }
}
