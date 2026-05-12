# language: ja
@DZ-CARD-02
機能: カード No.02「緑の侵攻」
  選択式カード + 継続影響の付与 / 除去を統合的に行うカード。
  ADR-0007 §1.5「継続影響」、M2-PR5 で導入。

  背景:
    前提 InMemoryCardCatalog に Card "02"「緑の侵攻」が ChoiceEffect 1 件で登録されている
    かつ 「緑の侵攻」が持つ影響は PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,-5), 3) である
    かつ N=2(p1, p2)

  @DZ-170 @DZ-171
  シナリオ: 選択1 で攻撃的にプレイ
    前提 p1 が current で WaitingForPlay、p1 が Card "02" を手札に持つ
    もし p1 が PlayCardAction(Card="02", Choice=0) を適用する
    ならば p1 の SDP が -6 になる
    かつ p2 の Influences に 1 件追加される

  @DZ-172
  シナリオ: 選択1 で既存影響を削除 → 新規付与
    前提 p2 が既存 PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,-3), 2) を 1 件保有
    かつ p1 が current で WaitingForPlay、p1 が Card "02" を手札に持つ
    もし p1 が PlayCardAction(Card="02", Choice=0, InfluenceRemovalIndex=0) を適用する
    ならば p2 の Influences 件数が 1 になる
    かつ 残った影響は新規付与の GreenInvasionInfluence である

  @DZ-173 @DZ-174
  シナリオ: 選択2 で防御的にプレイ
    前提 p1 が current で WaitingForPlay、p1 が Card "02" を手札に持つ
    もし p1 が PlayCardAction(Card="02", Choice=1) を適用する
    ならば p2 の SDP が +6 になる
    かつ p1 の Influences に 1 件追加される

  @DZ-175
  シナリオ: 選択範囲外で illegal-move
    前提 p1 が current で WaitingForPlay、p1 が Card "02" を手札に持つ
    もし p1 が PlayCardAction(Card="02", Choice=2) で IsLegalMove を確認する
    ならば 結果は false である

  @DZ-176
  シナリオ: フェーズ進行で影響が Tick される
    前提 p2 が GreenInvasionInfluence(カウント 3) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 が新しい current player になる
    かつ p2 の SDP が -5 になる
    かつ p2 の影響の RemainingCount が 2 になる

  @DZ-177
  シナリオ: カウント 1 から Tick で除去
    前提 p2 が GreenInvasionInfluence(カウント 1) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の Influences 件数が 0 になる

  @DZ-178
  シナリオ: 他プレイヤーの影響は Tick されない
    前提 p1 と p2 が各 GreenInvasionInfluence(カウント 3) を 1 件ずつ保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 のみ Tick される(SDP -5、カウント 2)
    かつ p1 の影響の RemainingCount は 3 のまま
