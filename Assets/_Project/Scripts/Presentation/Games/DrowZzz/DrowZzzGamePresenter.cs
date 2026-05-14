using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Application.Persistence;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Configuration;

namespace Drowsy.Presentation.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz ゲームの Presenter(MVP の P、Pure C#)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §3.2「Presenter」で確定。本 PR (M5-PR2) では:
    /// <list type="bullet">
    /// <item><see cref="Start"/> / <see cref="Dispose"/> / <see cref="BootAsync"/> は本実装</item>
    /// <item>Handler 3 種(<see cref="HandleDrawClicked"/> / <see cref="HandlePlayClicked"/> /
    /// <see cref="HandleEndTurnClicked"/>)は <see cref="NotImplementedException"/> stub(M5-PR4 で本実装)</item>
    /// <item><see cref="BootAsync"/> 内の新規対戦経路(LoadAsync が <see cref="FileNotFoundException"/>)も
    /// <see cref="NotImplementedException"/> stub(M5-PR4 で <c>StartGameUseCase.Execute(players, initialDeck)</c>
    /// の引数生成を確定して本実装、ADR-0016 §3.2 line 238 の TBD と整合)</item>
    /// </list>
    /// <para>
    /// <b>VContainer 統合</b>:<see cref="IStartable"/>.<see cref="Start"/> は Container 構築後に
    /// VContainer が 1 回のみ呼ぶ(VContainer 1.17.0 仕様)。<see cref="IDisposable"/>.<see cref="Dispose"/> は
    /// Game スコープ Dispose 時に VContainer が呼ぶ。MonoBehaviour 側で明示的に呼ぶ必要なし。
    /// </para>
    /// <para>
    /// <b>状態公開</b>:<see cref="SessionStream"/> は <c>Subject&lt;DrowZzzGameSession&gt;</c> ベースで
    /// Boot 完了後のみ <c>OnNext</c> が発火する。<see cref="ReactiveProperty{T}"/> のような nullable 注釈を避け、
    /// ADR-0015 NRT 不採用方針整合を保つ。Subscribe 後の最初の <c>OnNext</c> のみ発火、 Subscribe より前の発火は
    /// 伝搬しない(M5 では Boot 完了 → View Subscribe 順が <see cref="Start"/> 内で確定するため初期描画は確実)。
    /// </para>
    /// <para>
    /// <b>イベント配線</b>:<see cref="Start"/> で View の 3 event を購読、 <see cref="Dispose"/> で対称的に
    /// 解除する(`event += / -=` 対称運用、Unity 文化整合、ADR-0016 §3.1)。
    /// </para>
    /// </remarks>
    public sealed class DrowZzzGamePresenter : IStartable, IDisposable
    {
        private readonly StartGameUseCase _startGameUseCase;
        private readonly ApplyActionUseCase _applyActionUseCase;
        private readonly IDrowZzzGameView _view;
        private readonly IDrowZzzGameSessionSerializer _serializer;
        private readonly IUserSettings _userSettings;
        private readonly string _savePath;

        private readonly Subject<DrowZzzGameSession> _sessionSubject = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _cts = new();

        private DrowZzzGameSession _current;
        private bool _disposed;

        /// <summary>
        /// Boot 完了後にのみ <c>OnNext</c> が発火する Observable。View は本ストリームを Subscribe して
        /// <see cref="IDrowZzzGameView.Render"/> を呼び出す配線が <see cref="Start"/> 内で組まれる。
        /// </summary>
        public Observable<DrowZzzGameSession> SessionStream => _sessionSubject;

        /// <summary>
        /// 現セッション。Boot 完了前 / リプレイ復元失敗時 / <see cref="BootAsync"/> stub 経路(M5-PR2 の
        /// 新規対戦経路)では null。
        /// </summary>
        /// <remarks>
        /// <b>消費側の注意</b>:本プロパティを参照する前に <see cref="IsReady"/> が true であることを必ず確認すること。
        /// ADR-0015 NRT 不採用方針下では戻り値型に nullable 注釈を付けないため、ガードなしで使うと
        /// <see cref="NullReferenceException"/> を起こす。Phase 3 で消費側の null チェック責務が広がった場合は
        /// internal 化または別 readonly 型 (<c>SessionRef</c> 等) の導入を別 ADR で検討する。
        /// </remarks>
        public DrowZzzGameSession Current => _current;

        /// <summary><see cref="Current"/> が non-null かどうか。Boot 完了後に true。</summary>
        public bool IsReady => _current is not null;

        /// <summary>
        /// Presenter を生成する。ctor では依存の null 検査のみを行い、Boot 処理は <see cref="Start"/> で実行。
        /// </summary>
        /// <param name="startGameUseCase">新規対戦開始(M5-PR4 で BootAsync 内から呼び出し)</param>
        /// <param name="applyActionUseCase">Action 適用(M5-PR4 で Handler 内から呼び出し)</param>
        /// <param name="view">View 抽象(MockDrowZzzGameView または DrowZzzGameView MonoBehaviour)</param>
        /// <param name="serializer">永続化 serializer(ADR-0016 §5.2)</param>
        /// <param name="userSettings">ユーザー設定(M5-PR6 で View バインディング)</param>
        /// <param name="savePath">セーブパス(<c>DrowZzzGameSessionSerializer.DefaultSavePath()</c> 経由で Bootstrap が解決、
        /// ADR-0016 §7.2 で Project Singleton として <c>RegisterInstance</c>)</param>
        /// <exception cref="ArgumentNullException">いずれかの参照型引数が null</exception>
        /// <exception cref="ArgumentException"><paramref name="savePath"/> が空白のみ</exception>
        public DrowZzzGamePresenter(
            StartGameUseCase startGameUseCase,
            ApplyActionUseCase applyActionUseCase,
            IDrowZzzGameView view,
            IDrowZzzGameSessionSerializer serializer,
            IUserSettings userSettings,
            string savePath)
        {
            _startGameUseCase = startGameUseCase ?? throw new ArgumentNullException(nameof(startGameUseCase));
            _applyActionUseCase = applyActionUseCase ?? throw new ArgumentNullException(nameof(applyActionUseCase));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
            if (savePath is null)
            {
                throw new ArgumentNullException(nameof(savePath));
            }
            if (string.IsNullOrWhiteSpace(savePath))
            {
                throw new ArgumentException("savePath は空・空白のみにできません", nameof(savePath));
            }
            _savePath = savePath;
        }

        /// <inheritdoc />
        public void Start()
        {
            // View event 配線(Handler 3 種は M5-PR2 では NotImplementedException stub)
            _view.OnDrawClicked += HandleDrawClicked;
            _view.OnPlayClicked += HandlePlayClicked;
            _view.OnEndTurnClicked += HandleEndTurnClicked;

            // 状態 → View(Boot 完了後の OnNext のみ View に伝搬)
            _sessionSubject.Subscribe(s => _view.Render(s)).AddTo(_disposables);

            // 起動シーケンス(UniTask、Forget の内側で try/catch + Cancellation Token 連動)
            BootAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// 起動シーケンス。セーブファイル存在判定 → 復元 / 新規対戦の分岐を行う。
        /// </summary>
        /// <remarks>
        /// M5-PR2 では新規対戦経路は <see cref="NotImplementedException"/> stub(JIT 確定 2026-05-14、M5-PR4 で
        /// <c>StartGameUseCase.Execute(players, initialDeck)</c> の引数生成を確定して本実装)。
        /// LoadAsync 成功経路と cancellation 経路は本 PR で動作する。
        /// </remarks>
        private async UniTaskVoid BootAsync(CancellationToken ct)
        {
            try
            {
                DrowZzzGameSession session;
                try
                {
                    session = await _serializer.LoadAsync(_savePath, ct);
                }
                catch (FileNotFoundException)
                {
                    // 初回起動 or 永続化ファイル削除済 → 新規対戦(M5-PR4 で本実装)
                    throw new NotImplementedException(
                        "BootAsync の新規対戦経路は M5-PR4 で実装する(StartGameUseCase.Execute の" +
                        "players / initialDeck 引数生成戦略を JIT 確定後、ADR-0016 §3.2 line 238 の TBD を解消)。");
                }
                _current = session;
                _sessionSubject.OnNext(session);
            }
            // catch 順序は OperationCanceledException → NotImplementedException → Exception の順を変えない
            // (前者はいずれも Exception の派生、後で来る catch は前者を吸わない順序が必要、W-1 反映)。
            catch (OperationCanceledException)
            {
                // Presenter Dispose 中、何もしない(意図された取り消し)
            }
            catch (NotImplementedException ex)
            {
                // M5-PR2 では新規対戦経路を呼ばないテスト構成のため、stub 例外は明示的に記録のみして握りつぶす
                // (View に未初期化状態を見せない方が UX 上自然、M5-PR4 で削除される暫定処理)
                Debug.LogWarning($"[DrowZzzGamePresenter] BootAsync stub: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DrowZzzGamePresenter] BootAsync failed: {ex}");
            }
        }

        // 以下、Handler 3 種は M5-PR4 で本実装。M5-PR2 では View event 配線の確認のみが目的。

        private void HandleDrawClicked()
        {
            throw new NotImplementedException("HandleDrawClicked は M5-PR4 で実装する(ApplyActionUseCase.Execute 経由)");
        }

        private void HandlePlayClicked(CardId cardId)
        {
            throw new NotImplementedException("HandlePlayClicked は M5-PR4 で実装する(ApplyActionUseCase.Execute 経由)");
        }

        private void HandleEndTurnClicked()
        {
            throw new NotImplementedException("HandleEndTurnClicked は M5-PR4 で実装する(ApplyActionUseCase.Execute + Auto-save 経由)");
        }

        /// <inheritdoc />
        /// <remarks>
        /// 2 回目以降の <see cref="Dispose"/> は silent no-op(冪等性、PRES-013)。VContainer の通常運用では
        /// <c>Start → Dispose</c> の順が保証されるが、 <see cref="Start"/> 未呼び出し状態で <see cref="Dispose"/>
        /// を呼んでも <c>event -=</c> は未購読ハンドラに対して no-op、 <c>_cts.Cancel/Dispose</c> /
        /// <c>_disposables.Dispose</c> / <c>_sessionSubject.Dispose</c> はいずれも初期構築済 instance に対する
        /// 通常の Dispose 経路で安全に動作する(code-reviewer S-3 反映)。
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            // event 解除(Start で配線したものと対称運用、Start 未呼び出し時は no-op)
            _view.OnDrawClicked -= HandleDrawClicked;
            _view.OnPlayClicked -= HandlePlayClicked;
            _view.OnEndTurnClicked -= HandleEndTurnClicked;

            // 進行中の BootAsync を中断
            _cts.Cancel();
            _cts.Dispose();

            // R3 / Subject の解放
            _disposables.Dispose();
            _sessionSubject.Dispose();
        }
    }
}
