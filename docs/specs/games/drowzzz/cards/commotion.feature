# language: ja
@DZ-CARD-05
機能: カード No.05「喧騒を纏う」
  自己介入カード:自分の手札から 1 枚を共通山札 top に押し込み、次のターンの相手のドローを操作する戦術カード。
  No.04「静寂を纏う」と対をなす設計(No.04 = 相手手札の使用禁止、No.05 = 自分手札を相手に押し付け)。
  2026-05-17 で導入、連想由来除外は ADR-0019 PR ① の `AssociatedCardIds` を 2 つ目の consumer として利用。

  背景:
    前提 InMemoryCardCatalog に Card "05"「喧騒を纏う」が 2 件の最上位 effect で登録されている
    かつ 「喧騒を纏う」の効果列は [TimeOfDayBranchEffect(夜: SDP変動のみ / 朝: SDP変動のみ), StackHandCardOnDeckTopEffect(Self)]
    かつ 夜分岐は [AdjustSdpEffect(Self,-8), AdjustSdpEffect(Opponent,-18)]
    かつ 朝分岐は [AdjustSdpEffect(Self,-4), AdjustSdpEffect(Opponent,+12)]
    かつ N=2(p1, p2)

  @DZ-270 @DZ-271 @DZ-272
  シナリオ: 夜フェーズで Card 05 をプレイ
    前提 Clock.IsNight が true(ターン 1〜16)
    かつ p1 が current で WaitingForPlay、p1 が Card "05" と Card "X" を手札に持つ
    もし p1 が PlayCardAction(Card="05", TargetCardId="X") を適用する
    ならば p1 の SDP が -8 になる
    かつ p2 の SDP が -18 になる
    かつ p1 の手札に Card "X" が含まれない
    かつ 共通山札の top が Card "X" になる

  @DZ-273
  シナリオ: 朝フェーズで Card 05 をプレイ
    前提 Clock.IsMorning が true(ターン 17〜21)
    かつ p1 が current で WaitingForPlay、p1 が Card "05" と Card "X" を手札に持つ
    もし p1 が PlayCardAction(Card="05", TargetCardId="X") を適用する
    ならば p1 の SDP が -4 になる
    かつ p2 の SDP が +12 になる
    かつ p1 の手札に Card "X" が含まれない
    かつ 共通山札の top が Card "X" になる

  @DZ-274
  シナリオ: TargetCardId なしで illegal-move
    前提 p1 が current で WaitingForPlay、p1 が Card "05" を手札に持つ
    もし p1 が PlayCardAction(Card="05", TargetCardId=null) で IsLegalMove を確認する
    ならば 結果は false である

  @DZ-275
  シナリオ: 自分手札にない TargetCardId で illegal-move
    前提 p1 が current で WaitingForPlay、p1 が Card "05" を手札に持つ
    かつ p1 が Card "X" を手札に持たない
    もし p1 が PlayCardAction(Card="05", TargetCardId="X") で IsLegalMove を確認する
    ならば 結果は false である

  @DZ-276
  シナリオ: 連想由来 TargetCardId で illegal-move(ADR-0019、PR ① consumer 2 件目)
    前提 p1 が current で WaitingForPlay、p1 が Card "05" と Card "X" を手札に持つ
    かつ session.AssociatedCardIds に Card "X" が含まれる
    もし p1 が PlayCardAction(Card="05", TargetCardId="X") で IsLegalMove を確認する
    ならば 結果は false である
