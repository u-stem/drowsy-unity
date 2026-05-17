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
  シナリオ: フェーズ進行で影響が Tick される(ADR-0020:Tick は TickEffect のみ、count 不変)
    前提 p2 が GreenInvasionInfluence(カウント 3) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 が新しい current player になる
    かつ p2 の SDP が -5 になる
    かつ p2 の影響の RemainingCount は 3 のまま(p2 自身の EndTurn で -1 されるまで遅延)

  @DZ-177
  シナリオ: カウント 1 でも次の自フェーズで 1 回機能、p2 自身の EndTurn で除去(ADR-0020)
    前提 p2 が GreenInvasionInfluence(カウント 1) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の SDP が -5 になる
    かつ p2 の Influences 件数が 1 で残る(RemainingCount=1、除去は p2 自身の EndTurn まで遅延)

  @DZ-178
  シナリオ: p2 Tick は count を変えない(ADR-0020:Decrement は p2 自身の EndTurn 時)
    前提 p1 と p2 が各 GreenInvasionInfluence(カウント 3) を 1 件ずつ保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p1 の影響の RemainingCount は 2 になる(p1 EndTurn 冒頭の Decrement で 3→2)
    かつ p2 の影響の RemainingCount は 3 のまま(Tick で TickEffect 適用のみ、count 不変)

  @DZ-179
  シナリオ: 自プレイヤー自身の EndTurn 冒頭で Decrement(ADR-0020 新規)
    前提 p1 が GreenInvasionInfluence(カウント 3) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p1 の影響の RemainingCount が 2 になる(EndTurn 冒頭 Decrement で 3→2)
