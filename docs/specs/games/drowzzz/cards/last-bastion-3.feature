# language: ja
機能: カード No.15「最後の砦Ⅲ」(Phase 2 完結後)

  「最後の砦Ⅰ→Ⅱ→Ⅲ」エスカレーション 3 連鎖の終端カード。Choice2 でも連想なし。

  @DZ-355
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog に Card "15" を登録する
    ならば カード名は "最後の砦Ⅲ" であり
    かつ 効果列は ChoiceEffect(2 分岐、Choice2 にも連想 effect なし)

  @DZ-356
  シナリオ: Choice0 プレイで SDP +10 / -10
    前提 p1 が WaitingForPlay、p1 が Card "15" を手札に持つ
    もし p1 が PlayCardAction(Card="15", Choice=0) を適用する
    ならば p1 の SDP が +10 になる
    かつ p2 の SDP が -10 になる

  @DZ-357
  シナリオ: Choice1 プレイで SDP -4 / -30(連鎖最強攻撃)
    前提 p1 が WaitingForPlay、p1 が Card "15" を手札に持つ
    もし p1 が PlayCardAction(Card="15", Choice=1) を適用する
    ならば p1 の SDP が -4 になる
    かつ p2 の SDP が -30 になる

  @DZ-358
  シナリオ: Choice1 プレイ後、Hand に No.13/14/15 は追加されない(終端、連想なし)
    前提 p1 が WaitingForPlay、p1 が Card "15" を手札に持つ
    もし p1 が PlayCardAction(Card="15", Choice=1) を適用する
    ならば p1 の Hand には No.13/14/15 はいずれも追加されない(終端、連想 effect なし)
