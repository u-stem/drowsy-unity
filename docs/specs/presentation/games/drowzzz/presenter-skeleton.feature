# language: ja
機能: DrowZzzGamePresenter (Presentation 層 Presenter) (M5-PR2 骨格 / M5-PR4 Handler 拡張)

  @PRES-002
  シナリオ: ctor で startGameUseCase = null は ArgumentNullException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし new DrowZzzGamePresenter(null, applyActionUseCase, view, serializer, userSettings, "path/a", players, initialDeck) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-003
  シナリオ: ctor で applyActionUseCase = null は ArgumentNullException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, null, view, serializer, userSettings, "path/a", players, initialDeck) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-004
  シナリオ: ctor で view = null は ArgumentNullException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, null, serializer, userSettings, "path/a", players, initialDeck) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-005
  シナリオ: ctor で serializer = null は ArgumentNullException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, null, userSettings, "path/a", players, initialDeck) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-006
  シナリオ: ctor で userSettings = null は ArgumentNullException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, null, "path/a", players, initialDeck) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-007
  シナリオ: ctor で savePath = null は ArgumentNullException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, userSettings, null, players, initialDeck) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-008
  シナリオ: ctor で savePath が空・空白のみは ArgumentException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし savePath = "" または savePath = "   " で new DrowZzzGamePresenter(...) を呼ぶ
    ならば ArgumentException が発生する

  @PRES-014
  シナリオ: ctor で players = null は ArgumentNullException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, userSettings, "path/a", null, initialDeck) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-015
  シナリオ: ctor で initialDeck = null は ArgumentNullException (異常系・Small)
    前提 他の 7 引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, userSettings, "path/a", players, null) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-009
  シナリオ: Start() 直後に View の 3 event が購読される (正常系・Small)
    前提 Presenter 構築済 + Start() 未呼び
    もし Start() を呼ぶ
    ならば MockView.OnDrawClicked / OnPlayClicked / OnEndTurnClicked いずれも購読者数 = 1

  @PRES-010
  シナリオ: Dispose() 後に View の 3 event 購読が解除される (正常系・Small)
    前提 Presenter 構築済 + Start() 呼び済(3 event いずれも購読者 1)
    もし Dispose() を呼ぶ
    ならば MockView.OnDrawClicked / OnPlayClicked / OnEndTurnClicked いずれも購読者数 = 0

  @PRES-011
  シナリオ: BootAsync 復元経路で View.Render(session) が 1 回呼ばれる (正常系・Small)
    前提 MockSerializer.LoadAsyncBehavior = ReturnSession + LoadAsyncReturnSession = SessionFactory.NewSession()
    もし Start() を呼んだ後、UniTask の完了を待つ
    ならば MockView.RenderedSessions.Count == 1 && RenderedSessions[0] == 注入した session

  @PRES-012
  シナリオ: BootAsync 新規対戦経路で StartGameUseCase.Execute の session が Render される (正常系・Small)
    前提 MockSerializer.LoadAsyncBehavior = ThrowFileNotFound
    もし Start() を呼んだ後、UniTask の完了を待つ
    ならば StartGameUseCase.Execute(players, initialDeck) で生成した session が MockView.Render に 1 回渡る

  @PRES-013
  シナリオ: Dispose() の二重呼び出しは silent no-op (正常系・Small)
    前提 Presenter 構築済 + Dispose() 1 回呼び済
    もし 2 回目の Dispose() を呼ぶ
    ならば 例外を投げず、副作用なし(冪等性)

  @PRES-016
  シナリオ: Boot 完了後の合法な Draw クリックで session が更新され Render される (正常系・Small)
    前提 StartGameUseCase で生成した WaitingForDraw の session を Boot で復元済
    もし MockView.FireDrawClicked() を呼ぶ
    ならば DrawCardAction が適用され、MockView.RenderedSessions が 1 回増える

  @PRES-017
  シナリオ: 不合法手(WaitingForDraw で EndTurn)は無反応 (異常系・Small)
    前提 StartGameUseCase で生成した WaitingForDraw の session を Boot で復元済
    もし MockView.FireEndTurnClicked() を呼ぶ(EndTurn は WaitingForEndTurn でのみ合法)
    ならば InvalidOperationException は Presenter 内で握りつぶされ、MockView.RenderedSessions は増えない

  @PRES-018
  シナリオ: Boot 未完了で Handler を発火しても例外を投げず Render もしない (異常系・Small)
    前提 MockSerializer.LoadAsyncBehavior = ThrowOperationCanceled(BootAsync が _current をセットできない)
    もし MockView.FireDrawClicked() を呼ぶ
    ならば 例外を投げず、MockView.RenderedSessions は空のまま

  # ===== M5-PR5: Auto-save(EndTurn 後のみ)=====

  @PRES-019
  シナリオ: 合法な EndTurn 成功時に Auto-save が走る (正常系・Small)
    前提 Boot 完了後、Draw → Play を経て WaitingForEndTurn の session
    もし MockView.FireEndTurnClicked() を呼ぶ
    ならば EndTurn が適用され、MockSerializer.SaveAsyncCallCount が 1 増える

  @PRES-020
  シナリオ: 不合法な EndTurn では Auto-save が走らない (異常系・Small)
    前提 Boot 完了後、WaitingForDraw の session(EndTurn は不合法)
    もし MockView.FireEndTurnClicked() を呼ぶ
    ならば EndTurnAction は適用されず、MockSerializer.SaveAsyncCallCount は 0 のまま

  @PRES-021
  シナリオ: Draw 成功時は Auto-save が走らない (正常系・Small)
    前提 Boot 完了後、WaitingForDraw の session
    もし MockView.FireDrawClicked() を呼ぶ
    ならば DrawCardAction は適用されるが、Auto-save は EndTurn 後のみのため SaveAsyncCallCount は 0 のまま

  # ===== M5-PR7: Outcome の UI 反映と終了後の入力 disable =====

  @PRES-031
  シナリオ: Boot で IsTerminated な session を復元すると RenderOutcome が呼ばれる (正常系・Small)
    前提 MockSerializer.LoadAsyncReturnSession = NewSession() with Outcome = WinnerOutcome(p1)
    もし Start() を呼ぶ
    ならば SessionStream 購読が IsTerminated を検出し、MockView.RenderedOutcomes.Count == 1

  @PRES-032
  シナリオ: Outcome 確定後の Handler 発火は無反応 (異常系・Small)
    前提 IsTerminated な session を Boot で復元済
    もし MockView.FireDrawClicked() を呼ぶ
    ならば DrawCardAction は適用されず、MockView.RenderedSessions / RenderedOutcomes は Boot 時以降変化しない

  # @PRES-033(Action 適用で IsTerminated になる早期勝利 / Round 21 完了経路 + Auto-save Final)は
  # 本物 DrowZzzRule の終了経路を EditMode 単体テストで再現するのが統合的すぎるため Optional 手動 QA。
  # Auto-save 集約(action is EndTurnAction || next.IsTerminated)と SessionStream の IsTerminated 分岐は
  # PRES-019 / PRES-031 で機械検証済み(ADR-0016 §10)。
