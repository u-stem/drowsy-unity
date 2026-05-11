# language: ja
機能: DrowZzz ターン終了 (EndTurnAction) (M1-PR6)

  @DZ-067
  シナリオ: WaitingForEndTurn で IsLegalMove(EndTurnAction) は true (正常系・Small)
    前提 TurnPhase = WaitingForEndTurn の DrowZzzGameSession
    もし IsLegalMove(session, EndTurnAction) を呼ぶ
    ならば true が返る

  @DZ-068
  シナリオ: WaitingForDraw で IsLegalMove(EndTurnAction) は false (準正常系・Small)
    前提 TurnPhase = WaitingForDraw の DrowZzzGameSession
    もし IsLegalMove(session, EndTurnAction) を呼ぶ
    ならば false が返る

  @DZ-068
  シナリオ: WaitingForPlay で IsLegalMove(EndTurnAction) は false (準正常系・Small)
    前提 TurnPhase = WaitingForPlay の DrowZzzGameSession
    もし IsLegalMove(session, EndTurnAction) を呼ぶ
    ならば false が返る

  @DZ-069
  シナリオ: Apply で TurnNumber +1 (正常系・Small)
    前提 WaitingForEndTurn / Turn = TurnState(3, 0)
    もし Apply(session, EndTurnAction) を呼ぶ
    ならば 結果の Turn.TurnNumber = 4

  @DZ-070
  シナリオ: Apply で CurrentPlayerIndex が次に進む (正常系・Small)
    前提 N=2 / WaitingForEndTurn / CurrentPlayerIndex=0
    もし Apply(session, EndTurnAction) を呼ぶ
    ならば 結果の CurrentPlayerIndex = 1

  @DZ-070
  シナリオ: Apply で CurrentPlayerIndex がラップする (正常系・Small)
    前提 N=2 / WaitingForEndTurn / CurrentPlayerIndex=1
    もし Apply(session, EndTurnAction) を呼ぶ
    ならば 結果の CurrentPlayerIndex = 0 (= (1 + 1) % 2)

  @DZ-071
  シナリオ: Apply で TurnPhase が WaitingForDraw に戻る (正常系・Small)
    前提 WaitingForEndTurn
    もし Apply(session, EndTurnAction) を呼ぶ
    ならば 結果の TurnPhase = WaitingForDraw

  @DZ-072
  シナリオ: Apply で Players (Hand 含む) は不変 (正常系・Small)
    前提 N=2 / WaitingForEndTurn / Players[0].Hand=[a], Players[1].Hand=[b]
    もし Apply を呼ぶ
    ならば 結果の Players[0].Hand=[a], Players[1].Hand=[b] (不変)

  @DZ-073
  シナリオ: Apply で Deck は不変 (正常系・Small)
    前提 WaitingForEndTurn / Deck = [d1, d2]
    もし Apply を呼ぶ
    ならば 結果の Deck = [d1, d2]

  @DZ-074
  シナリオ: Apply で Field は不変 (正常系・Small)
    前提 WaitingForEndTurn / Field = [f1]
    もし Apply を呼ぶ
    ならば 結果の Field = [f1]

  @DZ-075
  シナリオ: Apply で FirstDrowsyPoints は不変 (正常系・Small)
    前提 WaitingForEndTurn / FDP = {p1: 0, p2: 10}
    もし Apply を呼ぶ
    ならば 結果の FDP = {p1: 0, p2: 10}

  @DZ-076
  シナリオ: WaitingForDraw で Apply は InvalidOperationException (異常系・Small)
    前提 TurnPhase = WaitingForDraw
    もし Apply(session, EndTurnAction) を呼ぶ
    ならば InvalidOperationException が発生する

  @DZ-076
  シナリオ: WaitingForPlay で Apply は InvalidOperationException (異常系・Small)
    前提 TurnPhase = WaitingForPlay
    もし Apply(session, EndTurnAction) を呼ぶ
    ならば InvalidOperationException が発生する
