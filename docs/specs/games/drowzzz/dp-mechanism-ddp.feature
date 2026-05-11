# language: ja
機能: DrowZzz DP 機構 (M2-PR4 / DDP 範囲)

  @DZ-130
  シナリオ: 有効な DDP で DrowZzzGameSession を生成 (正常系・Small)
    前提 N=2 の有効な GameState と FDP / DDP / SDP / DdpPool / PhaseState
    もし DrowZzzGameSession を生成する
    ならば DrawDrowsyPoints プロパティが入力と一致する(防御コピーが保持される)

  @DZ-131
  シナリオ: DDP に null を渡して生成 (異常系・Small)
    前提 DDP に null
    もし DrowZzzGameSession を生成する
    ならば ArgumentNullException が発生する

  @DZ-132
  シナリオ: DDP のキー集合が Players の PlayerId と不一致 (異常系・Small)
    前提 N=2 の GameState と DDP のキー集合が GameState.Players の PlayerId 集合と一致しない
    もし DrowZzzGameSession を生成する
    ならば ArgumentException が発生する

  @DZ-133
  シナリオ: DdpPool に null を渡して生成 (異常系・Small)
    前提 DdpPool に null
    もし DrowZzzGameSession を生成する
    ならば ArgumentNullException が発生する

  @DZ-134
  シナリオ: 既存 Session に with { DrawDrowsyPoints = null } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession
    もし with { DrawDrowsyPoints = null } を適用する
    ならば ArgumentNullException が発生する

  @DZ-135
  シナリオ: 既存 Session に with { DrawDrowsyPoints = キー不一致 } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession (Players = [p1, p2])
    もし with { DrawDrowsyPoints = [p1: 0, p3: 5] } を適用する
    ならば ArgumentException が発生する

  @DZ-136
  シナリオ: 既存 Session に with { DdpPool = null } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession
    もし with { DdpPool = null } を適用する
    ならば ArgumentNullException が発生する

  @DZ-137
  シナリオ: 負値の DDP も保持される (正常系・Small、0 floor なし)
    前提 DDP に [p1: -20, p2: -5] を持つ DrowZzzGameSession
    もし DrawDrowsyPoints を取得する
    ならば 負値も保持された Dictionary が返る (0 floor 適用なし)

  @DZ-138
  シナリオアウトライン: TotalPoints は FDP + DDP + SDP の合計を返す (正常系・Small)
    前提 FDP=<fdp>、DDP=<ddp>、SDP=<sdp> の DrowZzzGameSession
    もし TotalPoints(playerId) を取得する
    ならば <total> が返る

    例:
      | fdp | ddp | sdp | total | 備考                                   |
      | 100 |   5 |  10 | 115   | 全項正値                                |
      | 100 |  -5 |  10 | 105   | DDP 負値                                |
      | 100 | -30 | -20 |  50   | 全項減少                                |
      |   0 |   0 |   0 |   0   | 全項 0(StartGame 直後の最低値)         |

  @DZ-139
  シナリオ: StartGameUseCase が全プレイヤーの DDP を 0 で初期化 (正常系・Small)
    前提 StartGameUseCase に N=2 の player ids と初期山札
    もし Execute() を呼ぶ
    ならば 全プレイヤーの DDP が 0 で初期化された DrowZzzGameSession が返る

  @DZ-140
  シナリオ: StartGameUseCase が DdpPool を Shuffle 済みの 39 要素で初期化 (正常系・Small)
    前提 StubGameConfig (DdpPool = 39 要素) と決定的な IRandomSource
    もし Execute() を呼ぶ
    ならば session.DdpPool.Values の Count が 39
    かつ session.DdpPool.Values のマルチセットが IGameConfig.DdpPool と一致する(Shuffle で順序のみ変更)

  @DZ-141
  シナリオアウトライン: DDP 抽選対象ターン {5, 9, 13, 17, 21} 開始時に N 枚抽選 + DrawDrowsyPoints 累積 (正常系・Medium)
    前提 ターン <prevTurn> 完了直前で 後手の EndTurnAction.Apply 待ち の DrowZzzGameSession
    もし 後手の EndTurnAction.Apply で次ターン <drawTurn> に進める
    ならば DdpPool から先頭 2 枚 (N=2) が取り出される
    かつ 先手の DrawDrowsyPoints[先手] += 取り出した 1 枚目
    かつ 後手の DrawDrowsyPoints[後手] += 取り出した 2 枚目

    例:
      | prevTurn | drawTurn | 備考       |
      |        4 |        5 | 23:00 1回目 |
      |        8 |        9 | 01:00 2回目 |
      |       12 |       13 | 03:00 3回目 |
      |       16 |       17 | 05:00 4回目 |
      |       20 |       21 | 07:00 5回目 |

  @DZ-142
  シナリオアウトライン: 抽選対象外ターン {2, 3, 4, 6, ...} 開始時は DDP / DdpPool 不変 (正常系・Medium)
    前提 ターン <prevTurn> 完了直前で 後手の EndTurnAction.Apply 待ち の DrowZzzGameSession
    もし 後手の EndTurnAction.Apply で次ターン <nextTurn> に進める
    ならば DdpPool は不変
    かつ DrawDrowsyPoints は全プレイヤー 0 のまま

    例:
      | prevTurn | nextTurn | 備考      |
      |        1 |        2 |           |
      |        2 |        3 |           |
      |        3 |        4 |           |
      |        5 |        6 |           |
      |        9 |       10 |           |

  @DZ-143
  シナリオ: ターン境界以外(先手 EndTurn だけ)では DDP 抽選を行わない (準正常系・Medium)
    前提 ターン 4 の先手 EndTurnAction.Apply 待ち の DrowZzzGameSession (CurrentPlayerIndex == 0)
    もし 先手の EndTurnAction.Apply (CurrentPlayerIndex 0 → 1、TurnState は同ターン進行)
    ならば DdpPool は不変
    かつ DrawDrowsyPoints は全プレイヤー 0 のまま (Turn 5 の抽選は後手 EndTurn まで保留)

  @DZ-144
  シナリオ: 複数回 DDP 抽選で DrawDrowsyPoints が累積される (正常系・Large)
    前提 ターン 1 開始の StartGame 済み DrowZzzGameSession
    かつ 決定的な IRandomSource で DdpPool[0..1] = +5 / -10
    かつ 同 IRandomSource の状態を継続して Turn 9 開始時の DdpPool 先頭が +20 / +25 となる Shuffle 結果
    もし Turn 1 から Turn 9 まで N=2 各プレイヤーが Draw / Play / EndTurn を 8 ターン繰り返す
    ならば 先手 DrawDrowsyPoints = +5 (Turn 5) + +20 (Turn 9) = 25
    かつ 後手 DrawDrowsyPoints = -10 (Turn 5) + +25 (Turn 9) = 15

  @DZ-148
  シナリオ: DdpPool ctor に null を渡す (異常系・Small)
    前提 source に null
    もし DdpPool を生成する
    ならば ArgumentNullException が発生する

  @DZ-149
  シナリオ: 空の DdpPool から Draw する (異常系・Small)
    前提 空の DdpPool
    もし Draw() を呼ぶ
    ならば InvalidOperationException が発生する

  @DZ-150
  シナリオ: 3 要素の DdpPool から Draw する (正常系・Small)
    前提 DdpPool [10, 20, 30]
    もし Draw() を呼ぶ
    ならば (Drawn = 10, Remaining = DdpPool[20, 30]) が返る

  @DZ-151
  シナリオ: DdpPool.Shuffle に null rng を渡す (異常系・Small)
    前提 DdpPool [10, 20, 30] と null の IRandomSource
    もし Shuffle(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @DZ-152
  シナリオ: 決定的 rng で DdpPool.Shuffle (正常系・Small)
    前提 DdpPool [10, 20, 30, 40] と シード固定の IRandomSource
    もし Shuffle(rng) を呼ぶ
    ならば 同じシードで再 Shuffle した結果と Values が一致する (決定性)
    かつ Values のマルチセットが {10, 20, 30, 40} と一致する (Fisher-Yates 順列性)

  @DZ-154
  シナリオ: StubGameConfig.DdpPool は 39 要素の規定パターン (正常系・Small)
    前提 デフォルト StubGameConfig
    もし DdpPool を取得する
    ならば Count == 39 (13 種 × 3 枚)
    かつ Distinct() == {-30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30}
    かつ 各値の出現回数 == 3

