using System;
using UnityEngine;
using UnityEngine.UIElements;
using Drowsy.Application.Games.DrowZzz;
using Drowsy.Domain.Cards;
using Drowsy.Domain.Game;

namespace Drowsy.Presentation.Games.DrowZzz
{
    /// <summary>
    /// DrowZzz ゲームの View 実装(UI Toolkit ベース MonoBehaviour、M5-PR3 骨格)。
    /// </summary>
    /// <remarks>
    /// ADR-0016 §3.1「View interface」+ §6「シーン構成」+ §11 M5-PR3 で確定。
    /// 本 PR (M5-PR3) では:
    /// <list type="bullet">
    /// <item>UXML(<c>DrowZzzGame.uxml</c>)の VisualElement を <see cref="OnEnable"/> で query し、
    /// 3 ボタンの <c>clicked</c> を <see cref="IDrowZzzGameView"/> の event に橋渡しする</item>
    /// <item><see cref="Render"/> / <see cref="RenderOutcome"/> は <c>Debug.Log</c> 出力のみ
    /// (ADR-0016 §11 M5-PR3 備考「Render は Debug.Log 出力でひとまず動作確認」、本実装は Render が M5-PR4 /
    /// RenderOutcome が M5-PR7)</item>
    /// <item>Play ボタンは手札選択 UX 未実装のためプレースホルダ <see cref="CardId"/> を発火
    /// (M5-PR4 で手札 UI に置き換え)</item>
    /// </list>
    /// <para>
    /// <b>イベント対称運用</b>:<see cref="OnEnable"/> で <c>button.clicked +=</c>、 <see cref="OnDisable"/> で
    /// <c>button.clicked -=</c> を行う(Unity 文化の <c>OnEnable/OnDisable</c> 対称運用、ADR-0016 §3.1)。
    /// </para>
    /// <para>
    /// <b>VContainer 注入</b>:本 MonoBehaviour は <c>RegisterComponentInHierarchy&lt;DrowZzzGameView&gt;()</c>
    /// で Game スコープに登録される(M5-PR3 着手時 JIT 確定 2026-05-14、ADR-0016 §2 登録対象表 line 119)。
    /// Presenter は <c>SessionStream</c> を Subscribe して本 View の <see cref="Render"/> を呼ぶ配線を持つ。
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

        // 以下 3 Label は M5-PR4 の Render 本実装で使用する。M5-PR3 段階では OnEnable で query 代入のみ。
        private Label _statusLabel;
        private Label _turnLabel;
        private Label _phaseLabel;

        /// <inheritdoc />
        public event Action OnDrawClicked;

        /// <inheritdoc />
        public event Action<CardId> OnPlayClicked;

        /// <inheritdoc />
        public event Action OnEndTurnClicked;

        private void OnEnable()
        {
            if (_uiDocument is null)
            {
                Debug.LogError(
                    "[DrowZzzGameView] UIDocument が Inspector で未設定です。" +
                    "DrowZzzGameView の _uiDocument フィールドに同 GameObject の UIDocument を割り当ててください。");
                return;
            }

            // VisualElement query(UXML の name 属性と一致させる)
            var root = _uiDocument.rootVisualElement;
            _statusLabel = root.Q<Label>("status-label");
            _turnLabel = root.Q<Label>("turn-label");
            _phaseLabel = root.Q<Label>("phase-label");
            _drawButton = root.Q<Button>("draw-button");
            _playButton = root.Q<Button>("play-button");
            _endTurnButton = root.Q<Button>("end-turn-button");

            // UIDocument が存在しても Source Asset(UXML)未割り当て / name 不一致だと Q<T>() は null を返す。
            // null のまま clicked 配線に進むと NullReferenceException になるため早期検出する(code-reviewer W-4 反映)。
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

        private void RaiseDrawClicked() => OnDrawClicked?.Invoke();

        private void RaisePlayClicked()
        {
            // M5-PR3: 手札選択 UX 未実装のためプレースホルダ CardId を発火する。
            // M5-PR4 で手札一覧 UI からの選択値に置き換える(ADR-0016 §11 M5-PR4 行)。
            OnPlayClicked?.Invoke(CardId.Of("__m5pr3_placeholder__"));
        }

        private void RaiseEndTurnClicked() => OnEndTurnClicked?.Invoke();

        /// <inheritdoc />
        public void Render(DrowZzzGameSession session)
        {
            // M5-PR3: Debug.Log 出力でひとまず動作確認(ADR-0016 §11 M5-PR3 備考)。
            // M5-PR4 で山札残数 / 手札 / 場札 / 現プレイヤー / TurnPhase / SDP / FDP / DDP / Round を
            // 各 Label / VisualElement に反映する本実装に置き換える。
            Debug.Log("[DrowZzzGameView] Render: session を受信(M5-PR4 で UI 反映を実装)");
        }

        /// <inheritdoc />
        public void RenderOutcome(GameOutcome outcome)
        {
            // M5-PR3: Debug.Log 出力のみ。M5-PR7 で Winner / Draw 表示の本実装に置き換える。
            Debug.Log("[DrowZzzGameView] RenderOutcome: outcome を受信(M5-PR7 で UI 反映を実装)");
        }
    }
}
