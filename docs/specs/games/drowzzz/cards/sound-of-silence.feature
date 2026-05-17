# language: ja
@DZ-CARD-04
機能: カード No.04「静寂を纏う」
  対人介入カード:相手の手札から 1 枚を選んで 2 フェーズ使用禁止にする戦術カード。
  ADR-0019 PR ② で導入(連想由来カードは選択不可、`AssociatedCardIds` 初の consumer)。
  Phase 2 完結後の第 2 新規カード追加(2026-05-17)。

  背景:
    前提 InMemoryCardCatalog に Card "04"「静寂を纏う」が 2 件の最上位 effect で登録されている
    かつ 「静寂を纏う」の効果列は [TimeOfDayBranchEffect(夜: SDP変動のみ / 朝: SDP変動のみ), ApplyTargetedRestrictionEffect(Opponent, 2)]
    かつ 夜分岐は [AdjustSdpEffect(Self,-12), AdjustSdpEffect(Opponent,+5)]
    かつ 朝分岐は [AdjustSdpEffect(Self,+5), AdjustSdpEffect(Opponent,-8)]
    かつ N=2(p1, p2)

  @DZ-260 @DZ-261 @DZ-262
  シナリオ: 夜フェーズで Card 04 をプレイ
    前提 Clock.IsNight が true(ターン 1〜16)
    かつ p1 が current で WaitingForPlay、p1 が Card "04" を手札に持つ
    かつ p2 が Card "X" を手札に持つ
    もし p1 が PlayCardAction(Card="04", TargetCardId="X") を適用する
    ならば p1 の SDP が -12 になる
    かつ p2 の SDP が +5 になる
    かつ p2 の Influences に PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect("X"), 2) が 1 件追加される

  @DZ-263
  シナリオ: 朝フェーズで Card 04 をプレイ
    前提 Clock.IsMorning が true(ターン 17〜21)
    かつ p1 が current で WaitingForPlay、p1 が Card "04" を手札に持つ
    かつ p2 が Card "X" を手札に持つ
    もし p1 が PlayCardAction(Card="04", TargetCardId="X") を適用する
    ならば p1 の SDP が +5 になる
    かつ p2 の SDP が -8 になる
    かつ p2 の Influences に PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect("X"), 2) が 1 件追加される

  @DZ-264
  シナリオ: TargetCardId なしで illegal-move
    前提 p1 が current で WaitingForPlay、p1 が Card "04" を手札に持つ
    もし p1 が PlayCardAction(Card="04", TargetCardId=null) で IsLegalMove を確認する
    ならば 結果は false である

  @DZ-265
  シナリオ: 相手手札にない TargetCardId で illegal-move
    前提 p1 が current で WaitingForPlay、p1 が Card "04" を手札に持つ
    かつ p2 が Card "X" を手札に持たない
    もし p1 が PlayCardAction(Card="04", TargetCardId="X") で IsLegalMove を確認する
    ならば 結果は false である

  @DZ-266
  シナリオ: 連想由来 TargetCardId で illegal-move(ADR-0019)
    前提 p1 が current で WaitingForPlay、p1 が Card "04" を手札に持つ
    かつ p2 が Card "X" を手札に持つ
    かつ session.AssociatedCardIds に Card "X" が含まれる
    もし p1 が PlayCardAction(Card="04", TargetCardId="X") で IsLegalMove を確認する
    ならば 結果は false である

  @DZ-267
  シナリオ: 使用禁止 Influence 保有時に対象カードプレイ illegal-move
    前提 p2 が PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect("Y"), 2) を 1 件保有
    かつ p2 が current で WaitingForPlay、p2 が Card "Y" を手札に持つ
    もし p2 が PlayCardAction(Card="Y") で IsLegalMove を確認する
    ならば 結果は false である

  @DZ-268
  シナリオ: カウント 1 から Tick で除去
    前提 p2 が PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect("Y"), 1) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の Influences 件数が 0 になる
