# language: ja
機能: TurnState(ゲームのターン進行状態)

  @TURN-003
  シナリオ: 有効な値で生成すると TurnNumber が保持される (正常系・Small)
    前提 turnNumber=3 と currentPlayerIndex=1
    もし new TurnState(3, 1) で生成
    ならば TurnNumber は 3 である

  @TURN-003
  シナリオ: 有効な値で生成すると CurrentPlayerIndex が保持される (正常系・Small)
    前提 turnNumber=3 と currentPlayerIndex=1
    もし new TurnState(3, 1) で生成
    ならば CurrentPlayerIndex は 1 である

  @TURN-004
  シナリオ: Initial(0) で TurnNumber=1 / CurrentPlayerIndex=0 (正常系・Small)
    前提 playerIndex=0
    もし TurnState.Initial(0) を呼ぶ
    ならば TurnNumber は 1 / CurrentPlayerIndex は 0 である

  @TURN-004
  シナリオ: Initial(2) で TurnNumber=1 / CurrentPlayerIndex=2 (正常系・Small)
    前提 playerIndex=2
    もし TurnState.Initial(2) を呼ぶ
    ならば TurnNumber は 1 / CurrentPlayerIndex は 2 である

  @TURN-005
  シナリオ: Next で TurnNumber が 1 増える (正常系・Small)
    前提 TurnNumber=3 の TurnState
    もし Next(playerCount=2) を呼ぶ
    ならば 新 TurnState の TurnNumber は 4

  @TURN-006
  シナリオ: Next で中間プレイヤーから次へ (正常系・Small)
    前提 CurrentPlayerIndex=0、playerCount=3
    もし Next(3) を呼ぶ
    ならば 新 CurrentPlayerIndex は 1

  @TURN-006
  シナリオ: Next で最終プレイヤーから 0 に巻き戻り (正常系・Small)
    前提 CurrentPlayerIndex=2、playerCount=3
    もし Next(3) を呼ぶ
    ならば 新 CurrentPlayerIndex は 0

  @TURN-007
  シナリオ: 同じ TurnNumber と CurrentPlayerIndex なら等価 (正常系・Small)
    前提 TurnState(3, 1) を 2 つ
    もし Equals 比較
    ならば 等価

  @TURN-007
  シナリオ: TurnNumber が異なれば非等価 (正常系・Small)
    前提 TurnState(3, 1) と TurnState(4, 1)
    もし Equals 比較
    ならば 非等価

  @TURN-007
  シナリオ: CurrentPlayerIndex が異なれば非等価 (正常系・Small)
    前提 TurnState(3, 1) と TurnState(3, 2)
    もし Equals 比較
    ならば 非等価

  @TURN-008
  シナリオ: 等価な TurnState の GetHashCode は一致 (正常系・Small)
    前提 TurnState(3, 1) を 2 つ
    もし GetHashCode
    ならば 2 つのハッシュ値は等しい

  @TURN-009
  シナリオ: turnNumber=0 で生成 (異常系・Small)
    前提 turnNumber=0
    もし new TurnState(0, 0) で生成
    ならば ArgumentOutOfRangeException が発生

  @TURN-010
  シナリオ: currentPlayerIndex 負で生成 (異常系・Small)
    前提 currentPlayerIndex=-1
    もし new TurnState(1, -1) で生成
    ならば ArgumentOutOfRangeException が発生

  @TURN-011
  シナリオ: Initial に負の playerIndex (異常系・Small)
    前提 playerIndex=-1
    もし TurnState.Initial(-1) を呼ぶ
    ならば ArgumentOutOfRangeException が発生

  @TURN-012
  シナリオ: Next に playerCount=0 (異常系・Small)
    前提 任意の TurnState、playerCount=0
    もし Next(0) を呼ぶ
    ならば ArgumentOutOfRangeException が発生

  @TURN-012
  シナリオ: Next に playerCount 負 (異常系・Small)
    前提 任意の TurnState、playerCount=-1
    もし Next(-1) を呼ぶ
    ならば ArgumentOutOfRangeException が発生
