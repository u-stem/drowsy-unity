# language: ja
@DZ-CARD-00
機能: カード No.00「夢」 (M3-PR6)
  連想専用カード + 使用条件(FDS ≥ 100)+ 使用制限(連想後 1 フェーズ待機)+ 時刻分岐
  (夜 = 狂乱+本能付き早期勝利 / 朝 = 自 SDP -80)を統合的に表現するカード。
  ADR-0011 §6 / §7、M3-PR6 で導入。

  背景:
    前提 InMemoryCardCatalog に Card "00"「夢」が以下の effect 列で登録されている
      - AssociatableMarkerEffect
      - RequiresMinimumTotalPointsMarkerEffect(100)
      - UsageRestrictionMarkerEffect
      - TimeOfDayBranchEffect(夜: KeywordedEffect([Frenzy, Instinct], EarlyWinTriggerEffect) / 朝: AdjustSdpEffect(Self, -80))
    かつ initial deck には Card "00" が含まれない(連想専用)
    かつ N=2(p1, p2)

  @DZ-230
  シナリオ: 連想で「夢」を引くと使用制限 Influence が付与される
    前提 p1 が current で WaitingForDraw、p1 の TotalPoints が 80
    もし p1 が AssociateAction(Card="00") を適用する
    ならば p1 の手札末尾に Card "00" が追加される
    かつ p1 の Influences 末尾に PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, RemainingCount=1) が追加される
    かつ session.PhaseState は WaitingForDraw のまま不変(連想は割り込み式)

  @DZ-231
  シナリオ: 使用制限 Influence 保有中は「夢」をプレイ不可
    前提 p1 が current で WaitingForPlay、p1 が Card "00" を手札に持つ
    かつ p1 が PlayerInfluence(OwnPhaseStart, UsageRestrictionMarkerEffect, 1) を 1 件保有
    かつ p1 の TotalPoints が 100 以上(他条件はすべて満たす)
    もし IsLegalMove(PlayCardAction(Card="00")) を評価する
    ならば 結果は false である(UsageRestrictionMarkerEffect 持ち Influence の存在で illegal)

  @DZ-232
  シナリオ: 自フェーズ開始時の Tick で使用制限が解除され「夢」が再使用可能になる
    前提 p1 が UsageRestrictionMarkerEffect Influence(RemainingCount=1)を保有
    かつ p1 が WaitingForEndTurn で TotalPoints 100 以上
    もし p1 が EndTurnAction を適用し p2 が WaitingForDraw に遷移
    かつ p2 が DrawCardAction → PlayCardAction(p2 の任意手段)→ EndTurnAction を順次適用
    かつ ふたたび p1 が current(WaitingForDraw)になる前のターン進行で p1 の OwnPhaseStart Tick が走る
    ならば p1 の Influences 件数は 0(RemainingCount 1 → 0 で除去)
    かつ p1 が WaitingForPlay に到達した時点で IsLegalMove(PlayCardAction(Card="00")) は true

  @DZ-233
  シナリオ: FDS 99 では「夢」プレイ不可(閾値未満)
    前提 p1 が current で WaitingForPlay、p1 が Card "00" を手札に持つ
    かつ p1 の UsageRestrictionMarkerEffect Influence は存在しない(制限解除済)
    かつ p1 の TotalPoints が 99
    もし IsLegalMove(PlayCardAction(Card="00")) を評価する
    ならば 結果は false である(RequiresMinimumTotalPointsMarkerEffect(100) で閾値未満)

  @DZ-234
  シナリオ: FDS 100 では「夢」プレイ可能(inclusive 境界)
    前提 p1 が current で WaitingForPlay、p1 が Card "00" を手札に持つ
    かつ p1 の UsageRestrictionMarkerEffect Influence は存在しない(制限解除済)
    かつ p1 の TotalPoints がちょうど 100
    もし IsLegalMove(PlayCardAction(Card="00")) を評価する
    ならば 結果は true である(inclusive 境界、≥ 100 で合法)

  @DZ-235
  シナリオ: 夜の Round で「夢」をプレイ → 早期勝利
    前提 Clock.RoundNumber=10(夜)の DrowZzzGameSession(現プレイヤー p1)
    かつ p1 の TotalPoints が 100 以上、UsageRestrictionMarkerEffect 影響なし
    かつ p1 が Card "00" を手札に持つ、WaitingForPlay
    もし p1 が PlayCardAction(Card="00") を適用する
    ならば session.Outcome は WinnerOutcome(p1)
    かつ session.IsTerminated は true

  @DZ-236
  シナリオ: 朝の Round で「夢」をプレイ → 自分の SDP が -80
    前提 Clock.RoundNumber=17(朝)の DrowZzzGameSession(現プレイヤー p1)
    かつ p1 の UsageRestrictionMarkerEffect 影響なし、TotalPoints は閾値以上で WaitingForPlay
    かつ p1 が Card "00" を手札に持つ
    もし p1 が PlayCardAction(Card="00") を適用する
    ならば SDP[p1] が プレイ前比で -80
    かつ session.Outcome は null(朝は EarlyWinTrigger no-op)

  @DZ-237
  シナリオ: 狂乱(Frenzy)で「夢」は反撃を受けない
    前提 p1 が Card "00" を Field に出した直後で、session.PhaseState = WaitingForCounterResponse
    かつ p2 が Counter キーワード持ちの手札カード c_counter を保有
    もし IsLegalMove(CounterAction(Counter=c_counter, Target="00")) を評価する
    ならば 結果は false である(夢の夜効果列に Frenzy 含む KeywordedEffect が存在、ADR-0011 §4.3)

  @DZ-238
  シナリオ: 本能(Instinct)で「夢」は放棄の捨て対象として選択不可
    前提 p1 が current で WaitingForPlay、p1 の手札 index 0 に Card "00" がある
    もし IsLegalMove(AbandonAction(Choice=GainSdp, CardIndex=0)) を評価する
    ならば 結果は false である(Instinct 含むカードは捨て対象選択不可、ADR-0011 §4.2)
