using System;
using System.Collections.Generic;
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
using Drowsy.Domain.Players;

namespace Drowsy.Presentation.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz ゲームの Presenter(MVP の P、Pure C#)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §3.2「Presenter」+ §8「Session 自動セーブ / 自動復元」で確定。M5-PR2 で骨格、M5-PR4 で
    /// Handler / 新規対戦経路、M5-PR5 で Auto-save、M5-PR7 で Outcome 反映 + Auto-save Final を実装した。
    /// <list type="bullet">
    /// <item>Handler 3 種(<see cref="HandleDrawClicked"/> / <see cref="HandlePlayClicked"/> /
    /// <see cref="HandleEndTurnClicked"/>)は <see cref="TryApplyAction"/> 経由で <see cref="ApplyActionUseCase"/>
    /// に Action を適用する</item>
    /// <item><see cref="BootAsync"/> はセーブファイルがあれば <c>LoadAsync</c> で復元、なければ
    /// <see cref="StartGameUseCase"/>.<c>Execute(players, initialDeck)</c> で新規対戦を開始する</item>
    /// <item><see cref="TryApplyAction"/> は EndTurn 成功時(ADR-0016 §8)または Outcome 確定時(Game 終了)に
    /// <see cref="AutoSaveAsync"/> で自動セーブする。EndTurn で終了した場合も Auto-save は 1 回に集約される
    /// (M5-PR7 で Auto-save 判定を <see cref="TryApplyAction"/> 内に集約、M5-PR5 の bool 返却を void に戻した)</item>
    /// <item><see cref="Start"/> の <c>SessionStream</c> 購読は <c>Render</c> に加え、<c>IsTerminated</c> なら
    /// <c>RenderOutcome</c> も呼ぶ(M5-PR7)。<see cref="TryApplyAction"/> は <c>_current.IsTerminated</c> なら
    /// 入力を無視する(Outcome 確定後の入力 disable の Presenter 側、View 側ボタン disable との多層防御)</item>
    /// </list>
    /// <para>
    /// <b>VContainer 統合</b>:<see cref="IStartable"/>.<see cref="Start"/> は Container 構築後に
    /// VContainer が 1 回のみ呼ぶ(VContainer 1.17.0 仕様)。<see cref="IDisposable"/>.<see cref="Dispose"/> は
    /// Game スコープ Dispose 時に VContainer が呼ぶ。
    /// </para>
    /// <para>
    /// <b>状態公開</b>:<see cref="SessionStream"/> は <c>Subject&lt;DrowZzzGameSession&gt;</c> ベースで
    /// Boot 完了後のみ <c>OnNext</c> が発火する。ADR-0015 NRT 不採用方針整合のため nullable 注釈を避ける。
    /// </para>
    /// <para>
    /// <b>不合法手 / 終了後入力の扱い</b>:<see cref="ApplyActionUseCase.Execute"/> は <c>IsLegalMove</c> false で
    /// <see cref="InvalidOperationException"/> を投げる(<c>DrowZzzRule.Apply</c> 内部の山札枯渇等も同型)。M5 範囲では
    /// 握りつぶして <c>Debug.LogWarning</c> 記録のみ(無反応)。Boot 未完了 / Outcome 確定後の入力も同様に無反応 + 警告ログ。
    /// </para>
    /// <para>
    /// <b>Auto-save の失敗</b>:<see cref="AutoSaveAsync"/> は <c>UniTaskVoid</c> + <c>Forget</c> の fire-and-forget。
    /// <see cref="OperationCanceledException"/>(Dispose 中)は握りつぶし、それ以外の例外は <c>Debug.LogError</c> 記録のみで
    /// ゲームプレイは継続する(M5-PR5 着手時 JIT 確定)。
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
        private readonly IReadOnlyList<PlayerId> _players;
        private readonly Pile _initialDeck;

        private readonly Subject<DrowZzzGameSession> _sessionSubject = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _cts = new();

        private DrowZzzGameSession _current;
        private bool _disposed;

        /// <summary>
        /// Boot 完了後にのみ <c>OnNext</c> が発火する Observable。View は本ストリームを Subscribe して
        /// <see cref="IDrowZzzGameView.Render"/>(+ 終了時は <c>RenderOutcome</c>)を呼び出す配線が
        /// <see cref="Start"/> 内で組まれる。
        /// </summary>
        public Observable<DrowZzzGameSession> SessionStream => _sessionSubject;

        /// <summary>
        /// 現セッション。Boot 完了前 / リプレイ復元と新規対戦の両方が失敗したときのみ null。
        /// </summary>
        /// <remarks>
        /// <b>消費側の注意</b>:本プロパティを参照する前に <see cref="IsReady"/> が true であることを必ず確認すること。
        /// ADR-0015 NRT 不採用方針下では戻り値型に nullable 注釈を付けないため、ガードなしで使うと
        /// <see cref="NullReferenceException"/> を起こす。Phase 3 で消費側の null チェック責務が広がった場合は
        /// internal 化または別 readonly 型の導入を別 ADR で検討する。
        /// </remarks>
        public DrowZzzGameSession Current => _current;

        /// <summary><see cref="Current"/> が non-null かどうか。Boot 完了後に true。</summary>
        public bool IsReady => _current is not null;

        /// <summary>
        /// Presenter を生成する。ctor では依存の null 検査のみを行い、Boot 処理は <see cref="Start"/> で実行。
        /// </summary>
        /// <param name="startGameUseCase">新規対戦開始(<see cref="BootAsync"/> 内から呼び出し)</param>
        /// <param name="applyActionUseCase">Action 適用(Handler 内から呼び出し)</param>
        /// <param name="view">View 抽象(MockDrowZzzGameView または DrowZzzGameView MonoBehaviour)</param>
        /// <param name="serializer">永続化 serializer(ADR-0016 §5.2)</param>
        /// <param name="userSettings">ユーザー設定(M5-PR6 で View バインディング)</param>
        /// <param name="savePath">セーブパス(<c>DrowZzzGameSessionSerializer.DefaultSavePath()</c> 経由で Bootstrap が解決)</param>
        /// <param name="roster">新規対戦のプレイヤー roster(Bootstrap が <see cref="PlayerRoster"/> wrapper として構築、
        /// ADR-0017 で確定 — VContainer 1.x の <c>CollectionInstanceProvider</c> が <c>IReadOnlyList&lt;T&gt;</c> を
        /// 予約型として扱う問題への対処)。</param>
        /// <param name="initialDeck">新規対戦の初期山札(Bootstrap が catalog から構築、ADR-0016 §3.2 / M5-PR4 着手時 JIT 確定)</param>
        /// <exception cref="ArgumentNullException">いずれかの参照型引数が null</exception>
        /// <exception cref="ArgumentException"><paramref name="savePath"/> が空白のみ</exception>
        public DrowZzzGamePresenter(
            StartGameUseCase startGameUseCase,
            ApplyActionUseCase applyActionUseCase,
            IDrowZzzGameView view,
            IDrowZzzGameSessionSerializer serializer,
            IUserSettings userSettings,
            string savePath,
            PlayerRoster roster,
            Pile initialDeck)
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
            // PlayerRoster ctor 側で null + 空配列検証済(ROSTER-002 / ROSTER-003、ADR-0017)。
            // ここでは roster 自体の null のみ早期検出する。players の重複 / null 要素検査は引き続き
            // StartGameUseCase.Execute に委譲する。
            if (roster is null) throw new ArgumentNullException(nameof(roster));
            _players = roster.Players;
            _initialDeck = initialDeck ?? throw new ArgumentNullException(nameof(initialDeck));
        }

        /// <inheritdoc />
        public void Start()
        {
            // View event 配線
            _view.OnDrawClicked += HandleDrawClicked;
            _view.OnPlayClicked += HandlePlayClicked;
            _view.OnEndTurnClicked += HandleEndTurnClicked;

            // 状態 → View(Boot 完了後の OnNext のみ View に伝搬)。
            // Outcome 確定済み(IsTerminated)なら Render に続けて RenderOutcome も呼ぶ(M5-PR7)。
            _sessionSubject.Subscribe(s =>
            {
                _view.Render(s);
                if (s.IsTerminated)
                {
                    _view.RenderOutcome(s.Outcome);
                }
            }).AddTo(_disposables);

            // 起動シーケンス(UniTask、Forget の内側で try/catch + Cancellation Token 連動)
            BootAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// 起動シーケンス。セーブファイル存在判定 → 復元 / 新規対戦の分岐を行う。
        /// </summary>
        /// <remarks>
        /// M5-PR4 で新規対戦経路を本実装(ctor 注入された <see cref="_players"/> / <see cref="_initialDeck"/> を
        /// <see cref="StartGameUseCase.Execute"/> に渡す)。復元した session が既に終了済み(`IsTerminated`)の場合も
        /// <c>OnNext</c> 経由で <see cref="Start"/> の Subscribe が <c>RenderOutcome</c> を呼ぶ。
        /// </remarks>
        private async UniTaskVoid BootAsync(CancellationToken ct)
        {
            // catch 順序は OperationCanceledException → Exception の順を変えない
            // (OCE は Exception の派生、後の catch が前者を吸わない順序が必要)。
            try
            {
                DrowZzzGameSession session;
                try
                {
                    session = await _serializer.LoadAsync(_savePath, ct);
                }
                catch (FileNotFoundException)
                {
                    // 初回起動 or 永続化ファイル削除済 → 新規対戦を開始する。
                    session = _startGameUseCase.Execute(_players, _initialDeck);
                }
                catch (Exception loadEx) when (loadEx is not OperationCanceledException)
                {
                    // Pres W-4 post-Phase2 レビュー反映:JSON 破損 / IOException 等で復元失敗した場合、
                    // 旧実装は `_current` を null のまま外側 catch に flow し、View が無言で初期化されず
                    // ゲームが止まる経路があった。FileNotFoundException と同様に新規対戦へフォールバックし
                    // ユーザーがゲームを開始できる状態を保つ(保存ファイルは破損のまま残るので、次の
                    // Auto-save で上書きされる)。
                    Debug.LogError(
                        $"[DrowZzzGamePresenter] BootAsync: 既存セッション復元に失敗、新規対戦にフォールバックします: {loadEx}");
                    session = _startGameUseCase.Execute(_players, _initialDeck);
                }
                // C-4 post-Phase2 レビュー反映:LoadAsync の継続が ThreadPool スレッド上にいる場合、
                // 続く `_sessionSubject.OnNext` → `Render` で UI Toolkit を呼ぶとメインスレッド外操作に
                // なり Unity が例外を投げる。明示的にメインスレッドへ戻してから OnNext を発火する。
                await UniTask.SwitchToMainThread(ct);
                _current = session;
                _sessionSubject.OnNext(session);
            }
            catch (OperationCanceledException)
            {
                // Presenter Dispose 中、何もしない(意図された取り消し)
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DrowZzzGamePresenter] BootAsync failed: {ex}");
            }
        }

        private void HandleDrawClicked() => TryApplyAction(new DrawCardAction());

        private void HandlePlayClicked(CardId cardId) => TryApplyAction(new PlayCardAction(cardId));

        private void HandleEndTurnClicked() => TryApplyAction(new EndTurnAction());

        /// <summary>
        /// <paramref name="action"/> を現セッションに適用し、成功時は <see cref="_current"/> 更新 + <c>OnNext</c> 発火、
        /// 必要なら Auto-save をトリガーする。
        /// </summary>
        /// <remarks>
        /// 入力を無視する条件:Boot 未完了(<see cref="_current"/> が null)/ Outcome 確定済み
        /// (<c>_current.IsTerminated</c>、M5-PR7、View ボタン disable との多層防御の Presenter 側)。
        /// <see cref="ApplyActionUseCase.Execute"/> の <see cref="InvalidOperationException"/>(IsLegalMove false /
        /// Apply 内部の山札枯渇等)は握りつぶして <c>Debug.LogWarning</c> 記録のみ。
        /// <para>
        /// <b>Auto-save の集約</b>:適用成功後、<c>action is EndTurnAction</c>(ADR-0016 §8「Auto-save は EndTurn 後」)
        /// または <c>next.IsTerminated</c>(Game 終了時の Auto-save Final、ADR-0016 §8)なら <see cref="AutoSaveAsync"/> を
        /// 1 回トリガーする。EndTurn で Game が終了した場合も両条件を OR で評価するため Auto-save は 1 回に集約される
        /// (M5-PR7 で M5-PR5 の bool 返却 + Handler 側 Auto-save 判定を本メソッド内に集約、void に戻した)。
        /// </para>
        /// </remarks>
        private void TryApplyAction(DrowZzzAction action)
        {
            if (_current is null)
            {
                Debug.LogWarning(
                    $"[DrowZzzGamePresenter] {action.GetType().Name}: Boot 未完了のため無視");
                return;
            }
            if (_current.IsTerminated)
            {
                // Outcome 確定後は入力を無視する(M5-PR7、JIT 確定:View ボタン disable + Presenter event 無視の
                // 多層防御の Presenter 側)。
                Debug.LogWarning(
                    $"[DrowZzzGamePresenter] {action.GetType().Name}: ゲーム終了済みのため無視");
                return;
            }
            try
            {
                var next = _applyActionUseCase.Execute(_current, action);
                _current = next;
                _sessionSubject.OnNext(next);
                // Auto-save:EndTurn 後(ADR-0016 §8)+ Game 終了時(Outcome 確定)。
                // EndTurn で終了しても両条件を OR 評価するため Auto-save は 1 回に集約される。
                // DrawCardAction / PlayCardAction は EndTurnAction でなく、合法手の適用後に IsTerminated にも
                // ならない正常ケースでは Auto-save しない(PRES-021、Auto-save は EndTurn 後 / 終了時のみ)。
                if (action is EndTurnAction || next.IsTerminated)
                {
                    AutoSaveAsync(_cts.Token).Forget();
                }
            }
            catch (InvalidOperationException ex)
            {
                // IsLegalMove false(不合法手)/ Apply 内部例外(山札枯渇等)の両方を同一扱いで握りつぶす。
                // M5 では無反応 + 警告ログ。例外型は明示してデバッグ容易性を保つ。
                Debug.LogWarning(
                    $"[DrowZzzGamePresenter] {action.GetType().Name} を適用できません" +
                    $"({ex.GetType().Name}: {ex.Message})");
            }
        }

        /// <summary>
        /// 現セッションを <see cref="_savePath"/> へ非同期に自動セーブする(EndTurn 成功後 / Game 終了時に呼ばれる)。
        /// </summary>
        /// <remarks>
        /// ADR-0016 §8。<c>UniTaskVoid</c> + <c>Forget</c> の fire-and-forget で、<see cref="Dispose"/> 時に
        /// <see cref="_cts"/> 経由でキャンセルされる。Game 終了時(Outcome 確定)も保存先はメイン path 上書きのみ
        /// (別名 backup は Phase 3、M5-PR7 着手時 JIT 確定 2026-05-14)。<see cref="OperationCanceledException"/> は
        /// 握りつぶし、それ以外の例外は <c>Debug.LogError</c> 記録のみでゲームプレイは継続する。
        /// <para>
        /// Pres W-3 post-Phase2 レビュー — Auto-save の逆順書き込み race について:
        /// EndTurn が連打されると複数の AutoSaveAsync が並行 fire-and-forget され、
        /// 後発が先発より早く完了して古い session で上書きされる理論可能性がある。M5(N=2 ホットシート、
        /// 1 ターン 1 EndTurn 想定)では UI 経路上発生しないが、Phase 3 で複数同時 EndTurn が入る場合は
        /// 序列化(SemaphoreSlim or 「実行中なら次の save を予約 1 件だけ保持」のスロット制御)に切り替えること。
        /// </para>
        /// </remarks>
        private async UniTaskVoid AutoSaveAsync(CancellationToken ct)
        {
            // TryApplyAction 成功直後の _current をローカルにキャプチャする(EndTurn 後 / 終了時の最新 session を保存)。
            // fire-and-forget 中に _current が次の Action で更新されても、保存対象はこの時点の session で確定する。
            var sessionToSave = _current;
            try
            {
                await _serializer.SaveAsync(sessionToSave, _savePath, ct);
            }
            catch (OperationCanceledException)
            {
                // Presenter Dispose 中、何もしない(意図された取り消し)
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DrowZzzGamePresenter] Auto-save failed: {ex}");
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// 2 回目以降の <see cref="Dispose"/> は silent no-op(冪等性、PRES-013)。VContainer の通常運用では
        /// <c>Start → Dispose</c> の順が保証されるが、 <see cref="Start"/> 未呼び出し状態で <see cref="Dispose"/>
        /// を呼んでも <c>event -=</c> は未購読ハンドラに対して no-op、 <c>_cts.Cancel/Dispose</c> /
        /// <c>_disposables.Dispose</c> / <c>_sessionSubject.Dispose</c> はいずれも初期構築済 instance に対する
        /// 通常の Dispose 経路で安全に動作する。 <c>_cts.Cancel()</c> は進行中の <see cref="BootAsync"/> /
        /// <see cref="AutoSaveAsync"/> を中断する。
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

            // 進行中の BootAsync / AutoSaveAsync を中断
            _cts.Cancel();
            _cts.Dispose();

            // R3 / Subject の解放
            _disposables.Dispose();
            _sessionSubject.Dispose();
        }
    }
}
