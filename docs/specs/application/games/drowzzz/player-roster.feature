# language: ja
機能: PlayerRoster (Application 層 wrapper)

  @ROSTER-002
  シナリオ: ctor で players = null は ArgumentNullException (異常系・Small)
    前提 players = null
    もし new PlayerRoster(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @ROSTER-003
  シナリオ: ctor で players が空配列は ArgumentException (異常系・Small)
    前提 players = Array.Empty<PlayerId>()
    もし new PlayerRoster(players) を呼ぶ
    ならば ArgumentException が発生する(ArgumentNullException ではない厳密一致)

  @ROSTER-004
  シナリオ: 非空の players は Players プロパティで順序保持公開 (正常系・Small)
    前提 players = [PlayerId.Of("p1"), PlayerId.Of("p2")]
    もし new PlayerRoster(players) を呼ぶ
    ならば roster.Players は [p1, p2] と等しい(順序保持)
