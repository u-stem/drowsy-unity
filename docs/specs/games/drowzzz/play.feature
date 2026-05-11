# language: ja
機能: DrowZzz カードプレイ (PlayCardAction) (M1-PR5)

  @DZ-053
  シナリオ: PlayCardAction を null Card で生成 (異常系・Small)
    前提 card = null
    もし new PlayCardAction(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @DZ-053
  シナリオ: 既存 PlayCardAction に with { Card = null } (異常系・Small)
    前提 有効な PlayCardAction(CardId.Of("c1"))
    もし with { Card = null } を適用する
    ならば ArgumentNullException が発生する

  @DZ-054
  シナリオ: WaitingForPlay かつ Card が手札にある (正常系・Small)
    前提 TurnPhase = WaitingForPlay / 現プレイヤー Hand = [c1, c2] の DrowZzzGameSession
    もし IsLegalMove(session, PlayCardAction(CardId.Of("c1"))) を呼ぶ
    ならば true が返る

  @DZ-055
  シナリオ: WaitingForDraw で IsLegalMove(PlayCardAction) は false (準正常系・Small)
    前提 TurnPhase = WaitingForDraw の DrowZzzGameSession
    もし IsLegalMove(session, PlayCardAction(任意 card)) を呼ぶ
    ならば false が返る

  @DZ-055
  シナリオ: WaitingForEndTurn で IsLegalMove(PlayCardAction) は false (準正常系・Small)
    前提 TurnPhase = WaitingForEndTurn の DrowZzzGameSession
    もし IsLegalMove(session, PlayCardAction(任意 card)) を呼ぶ
    ならば false が返る

  @DZ-056
  シナリオ: WaitingForPlay だが Card が手札にない (準正常系・Small)
    前提 TurnPhase = WaitingForPlay / 現プレイヤー Hand = [c1] の DrowZzzGameSession
    もし IsLegalMove(session, PlayCardAction(CardId.Of("cX"))) を呼ぶ
    ならば false が返る

  @DZ-057
  シナリオ: Apply で現プレイヤー Hand から指定 Card が除かれる (正常系・Small)
    前提 WaitingForPlay / 現プレイヤー Hand = [c1, c2] / Field = []
    もし Apply(session, PlayCardAction(CardId.Of("c1"))) を呼ぶ
    ならば 結果の現プレイヤー Hand に c1 が含まれない

  @DZ-058
  シナリオ: Apply で現プレイヤー Hand.Count -1 (正常系・Small)
    前提 WaitingForPlay / 現プレイヤー Hand.Count = 2
    もし Apply(session, PlayCardAction(任意手札 card)) を呼ぶ
    ならば 結果の現プレイヤー Hand.Count = 1

  @DZ-059
  シナリオ: Apply で Field の Top = 指定 Card (AddTop) (正常系・Small)
    前提 WaitingForPlay / Field = [] / 手札に c1
    もし Apply(session, PlayCardAction(CardId.Of("c1"))) を呼ぶ
    ならば 結果の Field.Cards[0] = c1

  @DZ-060
  シナリオ: Apply で Field.Count +1 (正常系・Small)
    前提 WaitingForPlay / Field.Count = 0
    もし Apply(session, PlayCardAction(任意手札 card)) を呼ぶ
    ならば 結果の Field.Count = 1

  @DZ-061
  シナリオ: Apply で TurnPhase が WaitingForEndTurn に遷移 (正常系・Small)
    前提 WaitingForPlay
    もし Apply を呼ぶ
    ならば 結果の TurnPhase = WaitingForEndTurn

  @DZ-062
  シナリオ: Apply で Turn は不変 (正常系・Small)
    前提 WaitingForPlay / Turn = TurnState(3, 1)
    もし Apply を呼ぶ
    ならば 結果の Turn = 元の Turn

  @DZ-063
  シナリオ: Apply で Deck は不変 (正常系・Small)
    前提 WaitingForPlay / Deck = [d1, d2, d3]
    もし Apply を呼ぶ
    ならば 結果の Deck = [d1, d2, d3]

  @DZ-064
  シナリオ: Apply で他プレイヤー Hand は不変 (正常系・Small)
    前提 N=2 / Players[0] = current, Players[1].Hand = [b]
    もし Apply を呼ぶ
    ならば 結果の Players[1].Hand = [b]

  @DZ-065
  シナリオ: WaitingForDraw で Apply は InvalidOperationException (異常系・Small)
    前提 TurnPhase = WaitingForDraw
    もし Apply を呼ぶ
    ならば InvalidOperationException が発生する

  @DZ-066
  シナリオ: Card が手札にない状態で Apply は InvalidOperationException (異常系・Small)
    前提 WaitingForPlay / 現プレイヤー Hand = [c1] / action = PlayCardAction(CardId.Of("cX"))
    もし Apply を呼ぶ
    ならば InvalidOperationException が発生する
