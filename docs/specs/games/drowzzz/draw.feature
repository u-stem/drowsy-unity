# language: ja
機能: DrowZzz ドロー (DrawCardAction) (M1-PR4)

  @DZ-038
  シナリオ: WaitingForDraw で IsLegalMove(DrawCardAction) は true (正常系・Small)
    前提 PhaseState = WaitingForDraw の DrowZzzGameSession
    もし IsLegalMove(session, DrawCardAction) を呼ぶ
    ならば true が返る

  @DZ-039
  シナリオ: WaitingForPlay で IsLegalMove(DrawCardAction) は false (準正常系・Small)
    前提 PhaseState = WaitingForPlay の DrowZzzGameSession
    もし IsLegalMove(session, DrawCardAction) を呼ぶ
    ならば false が返る

  @DZ-039
  シナリオ: WaitingForEndTurn で IsLegalMove(DrawCardAction) は false (準正常系・Small)
    前提 PhaseState = WaitingForEndTurn の DrowZzzGameSession
    もし IsLegalMove(session, DrawCardAction) を呼ぶ
    ならば false が返る

  @DZ-040
  シナリオ: 現プレイヤーの手札枚数が +1 (正常系・Small)
    前提 WaitingForDraw / 山札 [c1, c2, c3] / 現プレイヤー手札 [] の DrowZzzGameSession
    もし Apply(session, DrawCardAction) を呼ぶ
    ならば 結果の現プレイヤー Hand.Count = 1

  @DZ-041
  シナリオ: 山札枚数が -1 (正常系・Small)
    前提 WaitingForDraw / 山札 [c1, c2, c3] の DrowZzzGameSession
    もし Apply(session, DrawCardAction) を呼ぶ
    ならば 結果の Deck.Count = 2

  @DZ-042
  シナリオ: 山札 Top のカードが手札に移動 (正常系・Small)
    前提 WaitingForDraw / 山札 Top = c1 の DrowZzzGameSession
    もし Apply(session, DrawCardAction) を呼ぶ
    ならば 結果の現プレイヤー Hand.Cards に c1 が含まれる

  @DZ-043
  シナリオ: PhaseState が WaitingForPlay に遷移 (正常系・Small)
    前提 WaitingForDraw の DrowZzzGameSession
    もし Apply(session, DrawCardAction) を呼ぶ
    ならば 結果の PhaseState = WaitingForPlay

  @DZ-044
  シナリオ: GameState.Turn は不変 (正常系・Small)
    前提 WaitingForDraw / Turn = TurnState(3, 1) の DrowZzzGameSession
    もし Apply(session, DrawCardAction) を呼ぶ
    ならば 結果の Turn = 元の Turn

  @DZ-045
  シナリオ: 他プレイヤーの手札は不変 (正常系・Small)
    前提 N=2 / WaitingForDraw / Players[0].Hand = [a], Players[1].Hand = [b], CurrentPlayerIndex=0
    もし Apply(session, DrawCardAction) を呼ぶ
    ならば 結果の Players[1].Hand = [b] (不変)

  @DZ-046
  シナリオ: WaitingForPlay で Apply は InvalidOperationException (異常系・Small)
    前提 PhaseState = WaitingForPlay の DrowZzzGameSession
    もし Apply(session, DrawCardAction) を呼ぶ
    ならば InvalidOperationException が発生する

  @DZ-047
  シナリオ: 山札枯渇で Apply は InvalidOperationException (異常系・Small)
    前提 WaitingForDraw / Deck = Pile.Empty の DrowZzzGameSession
    もし Apply(session, DrawCardAction) を呼ぶ
    ならば InvalidOperationException が発生する (Pile.Draw 由来)

  @DZ-048
  シナリオ: session = null で IsLegalMove は ArgumentNullException (異常系・Small)
    前提 session = null
    もし IsLegalMove(null, DrawCardAction) を呼ぶ
    ならば ArgumentNullException が発生する

  @DZ-049
  シナリオ: action = null で IsLegalMove は ArgumentNullException (異常系・Small)
    前提 session に有効な DrowZzzGameSession
    もし IsLegalMove(session, null) を呼ぶ
    ならば ArgumentNullException が発生する

  @DZ-050
  シナリオ: session = null で Apply は ArgumentNullException (異常系・Small)
    前提 session = null
    もし Apply(null, DrawCardAction) を呼ぶ
    ならば ArgumentNullException が発生する

  @DZ-051
  シナリオ: action = null で Apply は ArgumentNullException (異常系・Small)
    前提 session に有効な DrowZzzGameSession
    もし Apply(session, null) を呼ぶ
    ならば ArgumentNullException が発生する
