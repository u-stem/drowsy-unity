# language: ja
機能: カード No.01「コップ一杯の脅威」 (M2-PR3)

  @DZ-126
  シナリオ: 夜の Round で Card "01" をプレイ (正常系・Medium、統合)
    前提 InMemoryCardCatalog に Card "01"「コップ一杯の脅威」が TimeOfDayBranchEffect で登録済
    かつ Clock.RoundNumber=1 (夜) の DrowZzzGameSession (現プレイヤー p1)
    かつ p1 の手札に Card "01" がある
    かつ p1 / p2 の SDP=0、山札 top に c1 がある
    もし PlayCardAction(Card "01") を ApplyActionUseCase 経由で実行する
    ならば p1 (自分) の SDP = -4、p1 の手札に c1 が追加され、p2 (相手) の SDP = -10

  @DZ-127
  シナリオ: 朝の Round で Card "01" をプレイ (正常系・Medium、統合)
    前提 InMemoryCardCatalog に Card "01" が登録済
    かつ Clock.RoundNumber=17 (朝) の DrowZzzGameSession (現プレイヤー p1)
    かつ p1 の手札に Card "01" がある、SDP[p1]=SDP[p2]=0
    もし PlayCardAction(Card "01") を ApplyActionUseCase 経由で実行する
    ならば SDP[p1] = -4、SDP[p2] = +10、山札 / 手札 (c1 等) に変化なし
