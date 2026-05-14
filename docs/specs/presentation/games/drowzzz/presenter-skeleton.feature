# language: ja
機能: DrowZzzGamePresenter (Presentation 層 Presenter 骨格) (M5-PR2)

  @PRES-002
  シナリオ: ctor で startGameUseCase = null は ArgumentNullException (異常系・Small)
    前提 IDrowZzzGameView / IDrowZzzGameSessionSerializer / IUserSettings / ApplyActionUseCase / savePath 有効値
    もし new DrowZzzGamePresenter(null, applyActionUseCase, view, serializer, userSettings, "path/a") を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-003
  シナリオ: ctor で applyActionUseCase = null は ArgumentNullException (異常系・Small)
    前提 他の引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, null, view, serializer, userSettings, "path/a") を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-004
  シナリオ: ctor で view = null は ArgumentNullException (異常系・Small)
    前提 他の引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, null, serializer, userSettings, "path/a") を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-005
  シナリオ: ctor で serializer = null は ArgumentNullException (異常系・Small)
    前提 他の引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, null, userSettings, "path/a") を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-006
  シナリオ: ctor で userSettings = null は ArgumentNullException (異常系・Small)
    前提 他の引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, null, "path/a") を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-007
  シナリオ: ctor で savePath = null は ArgumentNullException (異常系・Small)
    前提 他の引数すべて有効
    もし new DrowZzzGamePresenter(startGameUseCase, applyActionUseCase, view, serializer, userSettings, null) を呼ぶ
    ならば ArgumentNullException が発生する

  @PRES-008
  シナリオ: ctor で savePath が空・空白のみは ArgumentException (異常系・Small)
    前提 他の引数すべて有効
    もし new DrowZzzGamePresenter(...,  "") または new DrowZzzGamePresenter(..., "   ") を呼ぶ
    ならば ArgumentException が発生する

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
  シナリオ: BootAsync 完了で View.Render(session) が 1 回呼ばれる (正常系・Small)
    前提 MockSerializer.LoadAsyncBehavior = ReturnSession + LoadAsyncReturnSession = SessionFactory.NewSession()
    もし Start() を呼んだ後、UniTask の完了を待つ
    ならば MockView.RenderedSessions.Count == 1 && RenderedSessions[0] == 注入した session

  @PRES-013
  シナリオ: Dispose() の二重呼び出しは silent no-op (正常系・Small)
    前提 Presenter 構築済 + Dispose() 1 回呼び済
    もし 2 回目の Dispose() を呼ぶ
    ならば 例外を投げず、副作用なし(冪等性)
