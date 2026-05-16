using System;
using System.Text;
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
    /// 設定 UI バインディング、M5-PR7 で <see cref="RenderOutcome"/> 本実装 + Outcome 確定後の入力 disable を追加。
    /// <list type="bullet">
    /// <item><see cref="Render"/>:Round / 現プレイヤー / TurnPhase / 山札・場札・捨て札枚数 / FDP・DDP・SDP・Total /
    /// 現プレイヤー手札を各 Label に反映(M5-PR4)</item>
    /// <item>Play ボタン押下時に直近 <see cref="Render"/> の現プレイヤー手札先頭カードを <see cref="OnPlayClicked"/> で発火(M5-PR4)</item>
    /// <item><see cref="IUserSettings"/> を VContainer <c>[Inject]</c> で直接注入し、<see cref="UserSettingsBinder"/> で
    /// 設定 UI(BGM/SE Slider + Language Dropdown)と双方向バインド(M5-PR6)</item>
    /// <item><see cref="RenderOutcome"/>:ゲーム終了結果(Winner / Draw)を <c>outcome-label</c> に表示し、
    /// Draw / Play / EndTurn の 3 ボタンを <c>SetEnabled(false)</c> で disable(M5-PR7、ADR-0016 §11 M5-PR7、
    /// JIT 確定「View ボタン disable + Presenter event 無視」の多層防御の View 側)</item>
    /// </list>
    /// <para>
    /// <b>イベント / バインダのライフサイクル</b>:button.clicked の配線・解除は <see cref="OnEnable"/> /
    /// <see cref="OnDisable"/> の対称運用。<see cref="UserSettingsBinder"/> は <c>[Inject]</c> 注入された
    /// <see cref="IUserSettings"/> を必要とするため <see cref="Start"/> で生成し <see cref="OnDestroy"/> で Dispose する
    /// (button.clicked の <c>OnEnable</c> 配線とは非対称だが、依存注入タイミングの都合、M5-PR6)。
    /// </para>
    /// <para>
    /// <b>VContainer 注入</b>:本 MonoBehaviour は <c>RegisterComponentInHierarchy&lt;DrowZzzGameView&gt;()</c>
    /// で Game スコープに登録される(ADR-0016 §2)。Presenter は <c>SessionStream</c> を Subscribe して
    /// 本 View の <see cref="Render"/> / <see cref="RenderOutcome"/> を呼ぶ。<see cref="IUserSettings"/> は
    /// <c>[Inject]</c> メソッド <see cref="Construct"/> で受け取る。
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

        // 以下 5 Label は Render / RenderOutcome 本実装で使用する。OnEnable で query 代入する。
        private Label _turnLabel;
        private Label _phaseLabel;
        private Label _pointsLabel;
        private Label _handLabel;
        private Label _outcomeLabel;

        // 直近 Render で受け取った session。Play ボタン押下時の手札先頭カード選択に使う。
        private DrowZzzGameSession _lastRendered;

        // VContainer [Inject] で注入されるユーザー設定(M5-PR6)。
        private IUserSettings _userSettings;

        // 設定 UI(Slider / Dropdown)と _userSettings の双方向バインダ。Start() で生成、OnDestroy() で Dispose。
        private UserSettingsBinder _settingsBinder;

        // post-Phase2 アルゴリズム最適化レビュー Top-4 反映:
        // Render は R3 OnNext で毎セッション更新ごとに呼ばれるホットパス。旧実装は
        // `string.Join(", ", hand.Cards.Select(c => c.Value))` で Select デリゲート + Enumerator +
        // 中間配列 + 最終文字列を毎 Render で alloc していた。StringBuilder を field 再利用 + for
        // ループに置換し、per-Render の LINQ alloc を排除する(StringBuilder の内部配列は容量超過時
        // のみ再 alloc、手札 ~10 枚規模では初回確保以降は alloc ゼロ)。
        private readonly StringBuilder _handTextBuilder = new(64);

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
            _outcomeLabel = root.Q<Label>("outcome-label");
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
            if (hand.IsEmpty)
            {
                _handLabel.text = "Hand: (empty)";
            }
            else
            {
                // Top-4 最適化:StringBuilder 再利用 + for ループで LINQ + string.Join alloc を排除
                _handTextBuilder.Clear();
                _handTextBuilder.Append("Hand: ");
                var cards = hand.Cards;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (i > 0)
                    {
                        _handTextBuilder.Append(", ");
                    }
                    _handTextBuilder.Append(cards[i].Value);
                }
                _handLabel.text = _handTextBuilder.ToString();
            }
        }

        /// <inheritdoc />
        public void RenderOutcome(GameOutcome outcome)
        {
            if (outcome is null)
            {
                // IDrowZzzGameView.RenderOutcome は non-null 契約(ADR-0015)。null は描画せず明示的に記録する。
                Debug.LogError("[DrowZzzGameView] RenderOutcome: outcome が null です(non-null 契約違反)");
                return;
            }

            // OnEnable が早期 return した場合(UIDocument 未設定 / ボタン未解決)、または UXML から
            // outcome-label が削除された場合は _outcomeLabel が null になる。Render の _turnLabel ガードと
            // 同様に、未解決時は描画をスキップする(両ガードとも「OnEnable で query した代表 Label の null 検査」)。
            if (_outcomeLabel is null)
            {
                Debug.LogWarning("[DrowZzzGameView] RenderOutcome: VisualElement 未解決のため描画をスキップします");
                return;
            }

            // GameOutcome は abstract record(WinnerOutcome / DrawOutcome の 2 派生、ADR-0010 §3 / §4)。
            // 将来の派生追加に備えて switch の _ ケースを残す。
            _outcomeLabel.text = outcome switch
            {
                WinnerOutcome winner => $"Winner: {winner.Winner.Value}",
                DrawOutcome => "Draw",
                _ => $"Outcome: {outcome.GetType().Name}",
            };

            // Outcome 確定後は入力を受け付けない(M5-PR7、JIT 確定:View ボタン disable + Presenter event 無視の
            // 多層防御の View 側)。リトライボタンは Phase 3。
            _drawButton?.SetEnabled(false);
            _playButton?.SetEnabled(false);
            _endTurnButton?.SetEnabled(false);
        }
    }
}
