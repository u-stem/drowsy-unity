# language: ja
@DZ-VICTORY
機能: 勝利条件と終了判定
  早期勝利 / 終了時勝利 / 引き分け / Round 22 ガードの 4 軸を統合的に検証する。
  ADR-0010、M3-PR1 で導入。

  背景:
    前提 N=2(p1, p2)、用語規約は ADR-0009(ターン = ラウンド、フェーズ = 1 プレイヤー 1 行動)に従う

  # ===== 早期勝利 =====

  @DZ-183
  シナリオ: 夜 + 持ち点 100 で WinnerOutcome 設定
    前提 turnNumber=1(Round=1 夜)、p1 が current、p1 の TotalPoints が 100
    もし EarlyWinTriggerEffect を EffectInterpreter で評価する
    ならば session.Outcome は WinnerOutcome(p1) になる

  @DZ-184
  シナリオ: 朝では早期勝利不可
    前提 turnNumber=33(Round=17 朝)、p1 が current、p1 の TotalPoints が 100
    もし EarlyWinTriggerEffect を評価する
    ならば session.Outcome は null のまま

  @DZ-185
  シナリオ: 持ち点不足では早期勝利不可
    前提 turnNumber=1(Round=1 夜)、p1 が current、p1 の TotalPoints が 99
    もし EarlyWinTriggerEffect を評価する
    ならば session.Outcome は null のまま

  # ===== 終了時勝利 =====

  @DZ-190
  シナリオ: Round 21 完了で低スコア側が勝者
    前提 turnNumber=42(Round 21 後手フェーズ完了直前)、p1 SDP=10、p2 SDP=50
    もし p2 が EndTurnAction を適用する
    ならば Round 22 に到達し session.Outcome = WinnerOutcome(p1) になる

  @DZ-190
  シナリオ: Round 21 完了で両者同点 → 引き分け
    前提 turnNumber=42、両者 SDP=10
    もし p2 が EndTurnAction を適用する
    ならば session.Outcome = DrawOutcome()(tiebreaker なし、ADR-0010 §7)

  @DZ-191
  シナリオ: Round 21 内のフェーズ進行では終了判定が走らない
    前提 turnNumber=41(Round 21 先手フェーズ)
    もし p1 が EndTurnAction を適用する
    ならば newRound=21 のまま、session.Outcome は null のまま

  # ===== Round 22 ガード(終了済 session への Action は illegal) =====

  @DZ-189
  シナリオ: 終了済 session への Action は全て illegal
    前提 session.Outcome = WinnerOutcome(p1)、PhaseState=WaitingForDraw
    もし DrowZzzRule.IsLegalMove(session, new DrawCardAction()) を呼ぶ
    ならば 結果は false である

  # ===== IGameRule.IsTerminated / GetWinner 契約 =====

  @DZ-188
  シナリオ: GetWinner の契約(WinnerOutcome / DrawOutcome / 未終了)
    前提 rule = DrowZzzRule
    ならば WinnerOutcome(p1) 設定済 session で GetWinner は p1 を返す
    かつ DrawOutcome 設定済 session で GetWinner は null を返す
    かつ 未終了 session で GetWinner は InvalidOperationException を投げる
