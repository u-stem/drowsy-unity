# language: ja
機能: カード No.13「最後の砦Ⅰ」(Phase 2 完結後)

  「最後の砦Ⅰ→Ⅱ→Ⅲ」エスカレーション 3 連鎖の起点。自動連想 effect の初導入。
  Choice2 を選ぶと「最後の砦Ⅱ」(No.14)を自動連想で Hand に追加 + AssociatedCardIds に記録。

  @DZ-345
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog に Card "13" を登録する
    ならば カード名は "最後の砦Ⅰ" であり
    かつ 効果列は 1 件:ChoiceEffect(2 分岐)

  @DZ-346
  シナリオ: Choice0 プレイで SDP 変動(連想なし)
    前提 p1 が WaitingForPlay、p1 が Card "13" を手札に持つ
    もし p1 が PlayCardAction(Card="13", Choice=0) を適用する
    ならば p1 の SDP が +6 になる
    かつ p2 の SDP が -10 になる
    かつ p1 の Hand に No.14 は追加されない

  @DZ-347
  シナリオ: Choice1 プレイで SDP 変動
    前提 p1 が WaitingForPlay、p1 が Card "13" を手札に持つ
    もし p1 が PlayCardAction(Card="13", Choice=1) を適用する
    ならば p1 の SDP が -4 になる
    かつ p2 の SDP が -10 になる

  @DZ-348
  シナリオ: Choice1 プレイで Hand に No.14 が自動連想される
    前提 p1 が WaitingForPlay、p1 が Card "13" を手札に持つ
    もし p1 が PlayCardAction(Card="13", Choice=1) を適用する
    ならば p1 の Hand に CardId.Of("14", 0) が追加される

  @DZ-349
  シナリオ: Choice1 プレイで AssociatedCardIds に No.14 が記録される
    前提 p1 が WaitingForPlay、p1 が Card "13" を手札に持つ
    もし p1 が PlayCardAction(Card="13", Choice=1) を適用する
    ならば session の AssociatedCardIds に CardId.Of("14", 0) が含まれる

  @DZ-350
  シナリオ: Choice1 プレイ 2 回で No.14 が Instance 自動採番で 2 枚追加される
    前提 p1 が WaitingForPlay、p1 が Card "13" を 2 枚(Instance 0, 1)を手札に持つ
    もし p1 が Card "13" Instance 0 を Choice=1 でプレイ → 続けて Card "13" Instance 1 を Choice=1 でプレイ
    ならば p1 の Hand に CardId.Of("14", 0) と CardId.Of("14", 1) の 2 件が追加される
