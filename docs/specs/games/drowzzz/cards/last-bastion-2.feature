# language: ja
機能: カード No.14「最後の砦Ⅱ」(Phase 2 完結後)

  「最後の砦Ⅰ→Ⅱ→Ⅲ」エスカレーション 3 連鎖の中間カード。Choice2 で No.15 を自動連想。

  @DZ-351
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog に Card "14" を登録する
    ならば カード名は "最後の砦Ⅱ" であり
    かつ 効果列は ChoiceEffect(2 分岐)

  @DZ-352
  シナリオ: Choice0 プレイで SDP +8 / -10(連想なし)
    前提 p1 が WaitingForPlay、p1 が Card "14" を手札に持つ
    もし p1 が PlayCardAction(Card="14", Choice=0) を適用する
    ならば p1 の SDP が +8 になる
    かつ p2 の SDP が -10 になる

  @DZ-353
  シナリオ: Choice1 プレイで SDP -4 / -20
    前提 p1 が WaitingForPlay、p1 が Card "14" を手札に持つ
    もし p1 が PlayCardAction(Card="14", Choice=1) を適用する
    ならば p1 の SDP が -4 になる
    かつ p2 の SDP が -20 になる

  @DZ-354
  シナリオ: Choice1 プレイで No.15 が自動連想 + AssociatedCardIds 記録
    前提 p1 が WaitingForPlay、p1 が Card "14" を手札に持つ
    もし p1 が PlayCardAction(Card="14", Choice=1) を適用する
    ならば p1 の Hand に CardId.Of("15", 0) が追加される
    かつ session の AssociatedCardIds に CardId.Of("15", 0) が含まれる
