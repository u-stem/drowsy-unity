# language: ja
機能: DrowZzz 固有型 skeleton (M1-PR2)

  @DZ-006
  シナリオ: 有効な引数で DrowZzzGameSession を生成 (正常系・Small)
    前提 N=2 の有効な GameState と PlayerId をキーに持つ FirstDrowsyPoints と DrowZzzTurnPhase
    もし DrowZzzGameSession を生成する
    ならば 各プロパティが入力と一致する

  @DZ-007
  シナリオ: GameState に null を渡して生成 (異常系・Small)
    前提 GameState に null
    もし DrowZzzGameSession を生成する
    ならば ArgumentNullException が発生する

  @DZ-008
  シナリオ: FirstDrowsyPoints に null を渡して生成 (異常系・Small)
    前提 FirstDrowsyPoints に null
    もし DrowZzzGameSession を生成する
    ならば ArgumentNullException が発生する

  @DZ-009
  シナリオ: FirstDrowsyPoints のキーが Players の PlayerId と不一致 (異常系・Small)
    前提 N=2 の GameState と FirstDrowsyPoints のキー集合が GameState.Players の PlayerId 集合と一致しない
    もし DrowZzzGameSession を生成する
    ならば ArgumentException が発生する

  @DZ-010
  シナリオ: CurrentRound 計算 (N=2、TurnNumber=1) (正常系・Small)
    前提 TurnNumber=1 の GameState を持つ DrowZzzGameSession
    もし CurrentRound プロパティを取得する
    ならば 1 が返る

  @DZ-012
  シナリオ: skeleton 段階の DrowZzzRule.IsLegalMove (異常系・Small)
    前提 DrowZzzRule のインスタンスと有効な session / action
    もし IsLegalMove(session, action) を呼ぶ
    ならば NotImplementedException が発生する

  @DZ-013
  シナリオ: skeleton 段階の DrowZzzRule.Apply (異常系・Small)
    前提 DrowZzzRule のインスタンスと有効な session / action
    もし Apply(session, action) を呼ぶ
    ならば NotImplementedException が発生する

  @DZ-014
  シナリオ: 既存 Session に with { GameState = null } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession
    もし with { GameState = null } を適用する
    ならば ArgumentNullException が発生する

  @DZ-015
  シナリオ: 既存 Session に with { FirstDrowsyPoints = null } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession
    もし with { FirstDrowsyPoints = null } を適用する
    ならば ArgumentNullException が発生する

  @DZ-016
  シナリオ: 既存 Session に with { FirstDrowsyPoints = キー不一致 } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession (Players = [p1, p2])
    もし with { FirstDrowsyPoints = [p1: 0, p3: 10] } を適用する
    ならば ArgumentException が発生する

  @DZ-017
  シナリオ: 既存 Session に with { GameState = Players 不一致 } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession (FDP keys = [p1, p2])
    もし with { GameState = Players が [p1, p3] の新 GameState } を適用する
    ならば ArgumentException が発生する
