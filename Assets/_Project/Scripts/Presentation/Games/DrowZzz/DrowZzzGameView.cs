using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Configuration;
using Drowsy.Domain.Game;

namespace Drowsy.Presentation.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz ゲームの View 実装(UI Toolkit ベース MonoBehaviour)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §3.1「View interface」+ §6「シーン構成」+ §11 で確定。
    /// M5-PR3 で骨格、M5-PR4 で <see cref="Render"/> 本実装、M5-PR6 で <see cref="IUserSettings"/> の
    /// 設定 UI バインディングを追加した。
    /// <list type="bullet">
    /// <item><see cref="Render"/>:Round / 現プレイヤー / TurnPhase / 山札・場札・捨て札枚数 / FDP・DDP・SDP・Total /
    /// 現プレイヤー手札を各 Label に反映(M5-PR4)</item>
    /// <item>Play ボタン押下時に直近 <see cref="Render"/> の現プレイヤー手札先頭カードを <see cref="OnPlayClicked"/> で発火(M5-PR4)</item>
    /// <item><see cref="IUserSettings"/> を VContainer <c>[Inject]</c> で直接注入し、<see cref="UserSettingsBinder"/> で
    /// 設定 UI(BGM/SE Slider + Language Dropdown)と双方向バインド(M5-PR6、ADR-0016 §11「View に直接 Inject」)</item>
    /// </list>
    /// <para>
    /// <see cref="RenderOutcome"/> は引き続き <c>Debug.Log</c> 出力のみ(本実装は M5-PR7)。
    /// </para>
    /// <para>
    /// <b>イベント / バインダのライフサイクル</b>:button.clicked の配線・解除は <see cref="OnEnable"/> /
    /// <see cref="OnDisable"/> の対称運用(UIDocument の <c>rootVisualElement</c> はこの時点で利用可能)。一方
    /// <see cref="UserSettingsBinder"/> は <c>[Inject]</c> 注入された <see cref="IUserSettings"/> を必要とするため、
    /// 全 <c>Awake</c> / <c>OnEnable</c> / <c>[Inject]</c>(VContainer の <c>LifetimeScope.Awake</c> 内)完了後に
    /// 呼ばれる <see cref="Start"/> で生成し、<see cref="OnDestroy"/> で Dispose する(button.clicked の
    /// <c>OnEnable</c> 配線とは非対称だが、依存注入タイミングの都合、M5-PR6)。
    /// </para>
    /// <para>
    /// <b>VContainer 注入</b>:本 MonoBehaviour は <c>RegisterComponentInHierarchy&lt;DrowZzzGameView&gt;()</c>
    /// で Game スコープに登録される(ADR-0016 §2)。Presenter は <c>SessionStream</c> を Subscribe して
    /// 本 View の <see cref="Render"/> を呼ぶ。<see cref="IUserSettings"/> は <c>[Inject]</c> メソッド
    /// <see cref="Construct"/> で受け取る(ADR-0016 §11 M5-PR6「View に直接 Inject」)。
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DrowZzzGameView : MonoBehaviour, IDrowZzzGameView
    {
        /// <summary>UXML / USS を保持する <see cref="UIDocument"/>(同 GameObject 上、Inspector で割り当て)。</summary>
        [SerializeField] private UIDocument _uiDocument;

        private Button _drawButton;
        private Button _playButton;
        private Button _endTurnButton;

        // 以下 4 Label は Render 本実装で使用する。OnEnable で query 代入する。
        private Label _turnLabel;
        private Label _phaseLabel;
        private Label _pointsLabel;
        private Label _handLabel;

        // 直近 Render で受け取った session。Play ボタン押下時の手札先頭カード選択に使う。
        private DrowZzzGameSession _lastRendered;

        // VContainer [Inject] で注入されるユーザー設定(M5-PR6)。
        private IUserSettings _userSettings;

        // 設定 UI(Slider / Dropdown)と _userSettings の双方向バインダ。Start() で生成、OnDestroy() で Dispose。
        private UserSettingsBinder _settingsBinder;

        /// <inheritdoc />
        public event Action OnDrawClicked;

        /// <inheritdoc />
        public event Action<CardId> OnPlayClicked;

        /// <inheritdoc />
        public event Action OnEndTurnClicked;

        /// <summary>
        /// VContainer の <c>[Inject]</c> メソッド注入で <see cref="IUserSettings"/> を受け取る(M5-PR6)。
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="userSettings"/> が null
        /// (Bootstrap の <c>IUserSettings</c> 登録漏れを Boot 時点で検出)</exception>
        [Inject]
        public void Construct(IUserSettings userSettings)
        {
            _userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
        }

        private void OnEnable()
        {
            if (_uiDocument is null)
            {
                Debug.LogError(
                    "[DrowZzzGameView] UIDocument が Inspector で未設定です。" +
                    "DrowZzzGameView の _uiDocument フィールドに同 GameObject の UIDocument を割り当ててください。");
                return;
            }

            // VisualElement query(UXML の name 属性と一致させる)。status-label は固定タイトルのため query しない。
            var root = _uiDocument.rootVisualElement;
            _turnLabel = root.Q<Label>("turn-label");
            _phaseLabel = root.Q<Label>("phase-label");
            _pointsLabel = root.Q<Label>("points-label");
            _handLabel = root.Q<Label>("hand-label");
            _drawButton = root.Q<Button>("draw-button");
            _playButton = root.Q<Button>("play-button");
            _endTurnButton = root.Q<Button>("end-turn-button");

            // UIDocument が存在しても Source Asset(UXML)未割り当て / name 不一致だと Q<T>() は null を返す。
            // null のまま clicked 配線に進むと NullReferenceException になるため早期検出する。
            if (_drawButton is null || _playButton is null || _endTurnButton is null)
            {
                Debug.LogError(
                    "[DrowZzzGameView] ボタン要素が UXML から見つかりません。" +
                    "UIDocument の Source Asset に DrowZzzGame.uxml が割り当てられているか、" +
                    "name 属性(draw-button / play-button / end-turn-button)が一致しているか確認してください。");
                return;
            }

            // event 配線(OnDisable と対称運用)
            _drawButton.clicked += RaiseDrawClicked;
            _playButton.clicked += RaisePlayClicked;
            _endTurnButton.clicked += RaiseEndTurnClicked;
        }

        private void OnDisable()
        {
            // event 解除(OnEnable で配線したものと対称、null 条件は OnEnable 早期 return 時の保険)
            if (_drawButton is not null)
            {
                _drawButton.clicked -= RaiseDrawClicked;
            }
            if (_playButton is not null)
            {
                _playButton.clicked -= RaisePlayClicked;
            }
            if (_endTurnButton is not null)
            {
                _endTurnButton.clicked -= RaiseEndTurnClicked;
            }
        }

        private void Start()
        {
            // UserSettingsBinder は [Inject] で注入された _userSettings を必要とするため、全 Awake / OnEnable /
            // [Inject](LifetimeScope.Awake 内)完了後に呼ばれる Start() で生成する(クラス xmldoc 参照)。
            // _userSettings is null は VContainer 経由なら Construct の ?? throw で保証されるため通常到達しないが、
            // VContainer 非経由でテスト等から直接生成された場合の安全網として残す(code-reviewer M5-PR6 W-2)。
            if (_uiDocument is null || _userSettings is null)
            {
                Debug.LogError(
                    "[DrowZzzGameView] Start: UIDocument または IUserSettings が未準備です。" +
                    "UIDocument の Inspector 割り当て、または ProjectLifetimeScope の IUserSettings 登録を確認してください。");
                return;
            }

            var root = _uiDocument.rootVisualElement;
            var bgmSlider = root.Q<Slider>("bgm-slider");
            var seSlider = root.Q<Slider>("se-slider");
            var languageDropdown = root.Q<DropdownField>("language-dropdown");
            if (bgmSlider is null || seSlider is null || languageDropdown is null)
            {
                Debug.LogError(
                    "[DrowZzzGameView] Start: 設定 UI 要素が UXML から見つかりません。" +
                    "name 属性(bgm-slider / se-slider / language-dropdown)が一致しているか確認してください。");
                return;
            }

            _settingsBinder = new UserSettingsBinder(bgmSlider, seSlider, languageDropdown, _userSettings);
        }

        private void OnDestroy()
        {
            // Start() で生成した UserSettingsBinder を解放(Subscribe + RegisterValueChangedCallback の対称解除)。
            _settingsBinder?.Dispose();
        }

        private void RaiseDrawClicked() => OnDrawClicked?.Invoke();

        private void RaisePlayClicked()
        {
            // M5-PR4: 手札選択 UX は「現プレイヤーの手札先頭カードを自動選択」(M5-PR4 着手時 JIT 確定 2026-05-14)。
            // 手札一覧からのクリック選択 UI は Phase 3。
            if (_lastRendered is null)
            {
                Debug.LogWarning("[DrowZzzGameView] Play: まだ Render されていないため手札を選択できません");
                return;
            }
            // GameState の不変条件(コンストラクタで Players >= 1 / CurrentPlayerIndex が範囲内を保証)により
            // Players[CurrentPlayerIndex] のインデックスアクセスは安全。
            var gameState = _lastRendered.GameState;
            var hand = gameState.Players[gameState.Turn.CurrentPlayerIndex].Hand;
            if (hand.IsEmpty)
            {
                Debug.LogWarning("[DrowZzzGameView] Play: 現プレイヤーの手札が空のため発火しません");
                return;
            }
            OnPlayClicked?.Invoke(hand.Cards[0]);
        }

        private void RaiseEndTurnClicked() => OnEndTurnClicked?.Invoke();

        /// <inheritdoc />
        public void Render(DrowZzzGameSession session)
        {
            if (session is null)
            {
                // IDrowZzzGameView.Render は non-null 契約(ADR-0015)。null が来たら描画せず明示的に記録する。
                Debug.LogError("[DrowZzzGameView] Render: session が null です(non-null 契約違反)");
                return;
            }
            _lastRendered = session;

            // OnEnable が早期 return(UIDocument / ボタン未解決)した場合は Label も null なので描画をスキップ。
            if (_turnLabel is null)
            {
                Debug.LogWarning("[DrowZzzGameView] Render: VisualElement 未解決のため描画をスキップします");
                return;
            }

            var gameState = session.GameState;
            var currentPlayer = gameState.Players[gameState.Turn.CurrentPlayerIndex];
            var currentId = currentPlayer.Id;
            var hand = currentPlayer.Hand;

            _turnLabel.text =
                $"Round {session.CurrentRound} / Player {currentId.Value} / {session.PhaseState}";
            _phaseLabel.text =
                $"Deck {gameState.Deck.Count} / Field {gameState.Field.Count} / Discard {gameState.Discard.Count}";
            _pointsLabel.text =
                $"FDP {session.FirstDrowsyPoints[currentId]} / DDP {session.DrawDrowsyPoints[currentId]} / " +
                $"SDP {session.SecondDrowsyPoints[currentId]} / Total {session.TotalPoints(currentId)}";
            _handLabel.text = hand.IsEmpty
                ? "Hand: (empty)"
                : $"Hand: {string.Join(", ", hand.Cards.Select(c => c.Value))}";
        }

        /// <inheritdoc />
        public void RenderOutcome(GameOutcome outcome)
        {
            // M5-PR6 時点では Debug.Log 出力のみ。M5-PR7 で Winner / Draw 表示の本実装に置き換える。
            Debug.Log("[DrowZzzGameView] RenderOutcome: outcome を受信(M5-PR7 で UI 反映を実装)");
        }
    }
}
