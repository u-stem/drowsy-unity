using Drowsy.Application.Games.DrowZzz;
using Drowsy.Presentation.Games.DrowZzz;
using VContainer;
using VContainer.Unity;

namespace Drowsy.Bootstrap
{
    /// <summary>
    /// 1 対戦寿命の VContainer LifetimeScope(M5-PR3 で Configure 実装)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §1「VContainer LifetimeScope 階層構造」+ §2「登録対象と寿命」で確定。
    /// <see cref="ProjectLifetimeScope"/> の子スコープとして 1 対戦ごとに Create / Dispose する。
    /// Project スコープの Singleton(<c>IRandomSource</c> / <c>IGameConfig</c> / <c>ICardCatalog&lt;IEffect&gt;</c> /
    /// <c>IDrowZzzGameSessionSerializer</c> / <c>IUserSettings</c> / <c>DrowZzzRule</c> / セーブパス string)は
    /// 子スコープから解決可能なため、本スコープでは Game 寿命の型のみを登録する。
    /// <para>
    /// <b>登録対象</b>(ADR-0016 §2 登録対象表):
    /// <list type="bullet">
    /// <item><see cref="StartGameUseCase"/> — Game Singleton、ADR-0014 で 2 引数 ctor 化 → ADR-0024 で 3 引数 ctor に再拡張
    /// (<c>IRandomSource</c> / <c>IGameConfig</c> / <c>ICardCatalog&lt;IEffect&gt;</c> は Project スコープから解決)</item>
    /// <item><see cref="ApplyActionUseCase"/> — Game Singleton、ctor で <see cref="DrowZzzRule"/> を Project から解決</item>
    /// <item><see cref="DrowZzzGamePresenter"/> — Game Singleton、<c>AsImplementedInterfaces()</c> で
    /// <c>IStartable</c>(Boot 時 <c>Start()</c> 自動起動)/ <c>IDisposable</c>(スコープ Dispose 時解放)として登録、
    /// <c>AsSelf()</c> で具象型 Resolve 経路も両立</item>
    /// <item><see cref="DrowZzzGameView"/>(MonoBehaviour) — <c>RegisterComponentInHierarchy</c> で
    /// シーン階層から検索し <c>AsImplementedInterfaces()</c> で <see cref="IDrowZzzGameView"/> として登録
    /// (M5-PR3 着手時 JIT 確定 2026-05-14、<c>RegisterComponentInNewPrefab</c> は Phase 3 のリトライ UI で再評価)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>M5 範囲のスコープ生成タイミング</b>:アプリ起動直後 1 回限定(リスタート / リトライ UI なし)。
    /// Phase 3 で「新規対戦ボタン」を追加する際に <c>GameLifetimeScope.Create()</c> / <c>Dispose()</c>
    /// の繰り返し利用に拡張(ADR-0016 §1)。
    /// </para>
    /// </remarks>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        /// <inheritdoc />
        protected override void Configure(IContainerBuilder builder)
        {
            // Application UseCase(Project スコープの IRandomSource / IGameConfig / DrowZzzRule を解決)
            builder.Register<StartGameUseCase>(Lifetime.Singleton);
            builder.Register<ApplyActionUseCase>(Lifetime.Singleton);

            // Presentation
            builder.Register<DrowZzzGamePresenter>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
            // Pres W-2 post-Phase2 レビュー反映:View は MonoBehaviour で `Construct([Inject])` 経由のみ
            // 依存注入されるため、`AsImplementedInterfaces` のみで `AsSelf()` を付けない設計。
            // 将来テストや別 Resolver から `DrowZzzGameView` concrete 型を直接 Resolve したい場合は
            // `AsSelf()` を追加すること(現状は不要なため意図的に省略)。
            builder.RegisterComponentInHierarchy<DrowZzzGameView>()
                .AsImplementedInterfaces();
        }
    }
}
