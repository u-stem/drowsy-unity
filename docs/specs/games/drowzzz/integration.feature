# language: ja
機能: DrowZzz M1 統合シナリオ (StartGame → 数ラウンド進行) (M1-PR7)

  @DZ-077
  シナリオ: StartGameUseCase 直後の状態 (正常系・Medium)
    前提 IdentityRandom + StubGameConfig + N=2 players + 30 枚のダミー山札
    もし StartGameUseCase.Execute(players, deck) を呼ぶ
    ならば 結果の TurnPhase = WaitingForDraw、Turn.TurnNumber = 1、Turn.CurrentPlayerIndex = 0、各プレイヤー Hand.Count = 5

  @DZ-078
  シナリオ: 1 サブターン完走 (Draw → Play → EndTurn) (正常系・Medium)
    前提 StartGameUseCase 直後のセッション
    もし ApplyActionUseCase で Draw → Play(任意手札カード) → EndTurn を順に Execute
    ならば 各ステップで TurnPhase が WaitingForDraw → WaitingForPlay → WaitingForEndTurn → WaitingForDraw に遷移し、Turn.TurnNumber が 1 → 2 に増える

  @DZ-079
  シナリオ: 1 ラウンド完走 (N=2 サブターン) (正常系・Medium)
    前提 StartGameUseCase 直後のセッション
    もし 2 サブターン分 (Draw → Play → EndTurn × 2) を Execute
    ならば Turn.TurnNumber = 3 (= 1 + 2)、Turn.CurrentPlayerIndex = 0 (先行プレイヤーに戻る)

  @DZ-080
  シナリオ: 3 ラウンド完走 (6 サブターン) (正常系・Medium)
    前提 StartGameUseCase 直後のセッション (Field = 空、各 Hand = 5)
    もし 6 サブターン分 (3 ラウンド = Draw → Play → EndTurn × 6) を Execute
    ならば Turn.TurnNumber = 7、Field.Count = 6 (累積)、各プレイヤー Hand.Count = 5 (Draw 1 + Play 1 = ±0)

  @DZ-081
  シナリオ: Deterministic Replay (準正常系・Medium)
    前提 同一 players / deck / config と、同一 seed の XorShiftRandom を 2 つ用意
    もし それぞれで StartGame → 1 ラウンド完走を実行
    ならば 2 つの最終セッションは完全に等価 (DrowZzzGameSession.Equals が true)
