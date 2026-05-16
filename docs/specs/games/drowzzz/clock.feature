# language: ja
機能: DrowZzz Clock (M2-PR2)

  @DZ-090 @DZ-094
  シナリオアウトライン: ターン境界での Hour 計算 (正常系・Small)
    前提 RoundNumber = <round> の DrowZzzClock
    もし Hour プロパティを取得する
    ならば <hour> が返る

    例:
      | round | hour | 備考          |
      | 1     | 21   | 21:00 開始    |
      | 2     | 21   | 21:30         |
      | 16    | 4    | 04:30 (夜の終端) |
      | 17    | 5    | 05:00 (朝の始端) |
      | 21    | 7    | 07:00 (最終ターン) |

  @DZ-091 @DZ-094
  シナリオアウトライン: ターン境界での Minute 計算 (正常系・Small)
    前提 RoundNumber = <round> の DrowZzzClock
    もし Minute プロパティを取得する
    ならば <minute> が返る

    例:
      | round | minute | 備考          |
      | 1     | 0      | 21:00         |
      | 2     | 30     | 21:30         |
      | 16    | 30     | 04:30 (夜の終端) |
      | 17    | 0      | 05:00 (朝の始端) |
      | 21    | 0      | 07:00 (最終)  |

  @DZ-092 @DZ-095
  シナリオ: RoundNumber=1 で IsNight (正常系・Small)
    前提 RoundNumber = 1 の DrowZzzClock
    もし IsNight プロパティを取得する
    ならば true が返る

  @DZ-092 @DZ-095
  シナリオ: RoundNumber=16 で IsNight (正常系・Small、夜の終端)
    前提 RoundNumber = 16 の DrowZzzClock
    もし IsNight プロパティを取得する
    ならば true が返る

  @DZ-095
  シナリオ: RoundNumber=17 で IsNight (準正常系・Small、朝側境界)
    前提 RoundNumber = 17 の DrowZzzClock
    もし IsNight プロパティを取得する
    ならば false が返る

  @DZ-093 @DZ-096
  シナリオ: RoundNumber=17 で IsMorning (正常系・Small、朝の始端)
    前提 RoundNumber = 17 の DrowZzzClock
    もし IsMorning プロパティを取得する
    ならば true が返る

  @DZ-093 @DZ-096
  シナリオ: RoundNumber=21 で IsMorning (正常系・Small、朝の最終)
    前提 RoundNumber = 21 の DrowZzzClock
    もし IsMorning プロパティを取得する
    ならば true が返る

  @DZ-096
  シナリオ: RoundNumber=16 で IsMorning (準正常系・Small、夜側境界)
    前提 RoundNumber = 16 の DrowZzzClock
    もし IsMorning プロパティを取得する
    ならば false が返る

  @DZ-097
  シナリオアウトライン: Session.Clock.RoundNumber と CurrentRound の同義性 (正常系・Small)
    前提 TurnNumber = <turnNumber> の DrowZzzGameSession (N=2)
    もし Clock.RoundNumber と CurrentRound を取得する
    ならば 両者の値が <round> で一致する

    例:
      | turnNumber | round | 備考                |
      | 1          | 1     | ターン 1 フェーズ 1   |
      | 31         | 16    | ターン 16 フェーズ 1 (夜の終端) |
      | 41         | 21    | ターン 21 フェーズ 1 (朝の最終) |

  @DZ-098
  シナリオ: RoundNumber=22 で IsNight (異常系・Small、過渡的防御値)
    前提 RoundNumber = 22 の DrowZzzClock (M3 で IsTerminated がガードする前の過渡的状態)
    もし IsNight プロパティを取得する
    ならば false が返る

  @DZ-098
  シナリオ: RoundNumber=22 で IsMorning (異常系・Small、過渡的防御値)
    前提 RoundNumber = 22 の DrowZzzClock (M3 で IsTerminated がガードする前の過渡的状態)
    もし IsMorning プロパティを取得する
    ならば false が返る
