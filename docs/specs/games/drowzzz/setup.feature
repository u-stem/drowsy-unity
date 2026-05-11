# language: ja
機能: DrowZzz セットアップ (StartGameUseCase) (M1-PR3)

  @DZ-019
  シナリオ: 有効な引数で Execute を呼ぶと Players 数が入力と一致 (正常系・Small)
    前提 N=2 の players と 12 枚以上の initialDeck
    もし StartGameUseCase.Execute(players, initialDeck) を呼ぶ
    ならば 結果の GameState.Players.Count は players.Count と等しい

  @DZ-020
  シナリオ: FirstDrowsyPoints のキー集合が players と一致 (正常系・Small)
    前提 N=2 の players と 12 枚以上の initialDeck
    もし Execute を呼ぶ
    ならば 結果の FirstDrowsyPoints キー集合 = players の PlayerId 集合

  @DZ-021
  シナリオ: FirstDrowsyPoints の値が FdpPool から被りなく抽選される (正常系・Small)
    前提 N=2 の players、FdpPool = [0, 10, 20, 30, 35, 40, 45, 50, 55, 60]
    もし Execute を呼ぶ
    ならば 結果の FDP 値はすべて FdpPool の要素であり、互いに異なる

  @DZ-022
  シナリオ: 各プレイヤーの手札が 5 枚 (正常系・Small)
    前提 N=2 の players と 12 枚以上の initialDeck
    もし Execute を呼ぶ
    ならば 各プレイヤーの Hand.Count は 5

  @DZ-023
  シナリオ: 手札が交互順で配布される (正常系・Small)
    前提 N=2 の players と 山札 [c1, c2, ..., c20] (Top が c1)
    もし Execute を呼ぶ
    ならば Players[0].Hand = [c1, c3, c5, c7, c9]、Players[1].Hand = [c2, c4, c6, c8, c10]

  @DZ-024
  シナリオ: 山札残り枚数 (正常系・Small)
    前提 N=2 の players と 20 枚の initialDeck
    もし Execute を呼ぶ
    ならば 結果の Deck.Count は 10 (= 20 - 5 * 2)

  @DZ-025
  シナリオ: PhaseState が WaitingForDraw (正常系・Small)
    前提 N=2 の players と有効な initialDeck
    もし Execute を呼ぶ
    ならば 結果の PhaseState は WaitingForDraw

  @DZ-026
  シナリオ: Turn が Initial(0) (正常系・Small)
    前提 N=2 の players と有効な initialDeck
    もし Execute を呼ぶ
    ならば 結果の GameState.Turn は TurnState.Initial(0) と等価

  @DZ-027
  シナリオ: Deterministic Replay (準正常系・Small)
    前提 同一 players / initialDeck / IGameConfig と、同一 seed の IRandomSource を 2 つ用意
    もし それぞれで Execute を呼ぶ
    ならば 2 つの結果は完全に等価 (DrowZzzGameSession の Equals が true)

  @DZ-028
  シナリオ: players に null (異常系・Small)
    前提 players = null
    もし Execute(null, initialDeck) を呼ぶ
    ならば ArgumentNullException が発生する

  @DZ-029
  シナリオ: 空の players (異常系・Small)
    前提 players = 空配列
    もし Execute([], initialDeck) を呼ぶ
    ならば ArgumentException が発生する

  @DZ-030
  シナリオ: 重複 PlayerId の players (異常系・Small)
    前提 players = [PlayerId.Of("p1"), PlayerId.Of("p1")]
    もし Execute(players, initialDeck) を呼ぶ
    ならば ArgumentException が発生する

  @DZ-031
  シナリオ: initialDeck に null (異常系・Small)
    前提 initialDeck = null
    もし Execute(players, null) を呼ぶ
    ならば ArgumentNullException が発生する

  @DZ-032
  シナリオ: 山札枚数が配布に不足 (異常系・Small)
    前提 N=2 の players と 5 枚の initialDeck (10 枚未満)
    もし Execute(players, initialDeck) を呼ぶ
    ならば ArgumentException が発生する

  @DZ-033
  シナリオ: PlayerCount が FdpPool より多い (異常系・Small)
    前提 N=11 の players (FdpPool は 10 個しかない)
    もし Execute(players, initialDeck) を呼ぶ
    ならば InvalidOperationException が発生する

  @DZ-034
  シナリオ: DrowZzzRule.IsLegalMove(session, StartGameAction) は false (準正常系・Small)
    前提 既存 DrowZzzGameSession と StartGameAction
    もし DrowZzzRule.IsLegalMove(session, action) を呼ぶ
    ならば false が返る (StartGameUseCase 経由で扱うため、ADR-0006 §Implementation Notes)

  @DZ-037
  シナリオ: players に null 要素を含む (異常系・Small)
    前提 players = [PlayerId.Of("p1"), null]
    もし Execute(players, initialDeck) を呼ぶ
    ならば ArgumentException が発生する
