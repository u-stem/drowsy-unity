# language: ja
機能: ApplyActionUseCase (統一 Action 適用 UseCase) (M1-PR6)

  @APP-023
  シナリオ: DrawCardAction の正常委譲 (正常系・Small)
    前提 WaitingForDraw / 山札あり / DrowZzzRule + ApplyActionUseCase
    もし Execute(session, DrawCardAction) を呼ぶ
    ならば 結果は rule.Apply(session, DrawCardAction) と等価

  @APP-024
  シナリオ: PlayCardAction の正常委譲 (正常系・Small)
    前提 WaitingForPlay / 手札に c1 / DrowZzzRule + ApplyActionUseCase
    もし Execute(session, PlayCardAction(c1)) を呼ぶ
    ならば 結果は rule.Apply(session, PlayCardAction(c1)) と等価

  @APP-025
  シナリオ: EndTurnAction の正常委譲 (正常系・Small)
    前提 WaitingForEndTurn / DrowZzzRule + ApplyActionUseCase
    もし Execute(session, EndTurnAction) を呼ぶ
    ならば 結果は rule.Apply(session, EndTurnAction) と等価

  @APP-026
  シナリオ: IsLegalMove false で InvalidOperationException (異常系・Small)
    前提 WaitingForPlay (DrawCardAction は非合法) の DrowZzzGameSession
    もし Execute(session, DrawCardAction) を呼ぶ
    ならば InvalidOperationException が発生する

  @APP-027
  シナリオ: StartGameAction は常に InvalidOperationException (異常系・Small)
    前提 任意の有効な DrowZzzGameSession
    もし Execute(session, StartGameAction) を呼ぶ
    ならば InvalidOperationException が発生する (StartGameUseCase 経由で扱うため)

  @APP-028
  シナリオ: session = null で ArgumentNullException (異常系・Small)
    前提 session = null
    もし Execute(null, DrawCardAction) を呼ぶ
    ならば ArgumentNullException が発生する

  @APP-029
  シナリオ: action = null で ArgumentNullException (異常系・Small)
    前提 session に有効な DrowZzzGameSession
    もし Execute(session, null) を呼ぶ
    ならば ArgumentNullException が発生する

  @APP-030
  シナリオ: rule = null で ApplyActionUseCase 生成は ArgumentNullException (異常系・Small)
    前提 rule = null
    もし new ApplyActionUseCase(null) を呼ぶ
    ならば ArgumentNullException が発生する
