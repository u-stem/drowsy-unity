# language: ja
機能: DrowZzz DP 機構 (M2-PR3 / SDP 範囲)

  @DZ-100
  シナリオ: 有効な SDP で DrowZzzGameSession を生成 (正常系・Small)
    前提 N=2 の有効な GameState と FDP / SDP / PhaseState
    もし DrowZzzGameSession を生成する
    ならば SecondDrowsyPoints プロパティが入力と一致する(防御コピーが保持される)

  @DZ-101
  シナリオ: SDP に null を渡して生成 (異常系・Small)
    前提 SDP に null
    もし DrowZzzGameSession を生成する
    ならば ArgumentNullException が発生する

  @DZ-102
  シナリオ: SDP のキー集合が Players の PlayerId と不一致 (異常系・Small)
    前提 N=2 の GameState と SDP のキー集合が GameState.Players の PlayerId 集合と一致しない
    もし DrowZzzGameSession を生成する
    ならば ArgumentException が発生する

  @DZ-103
  シナリオアウトライン: TotalPoints は FDP + SDP の合計を返す (正常系・Small)
    前提 FirstDrowsyPoints が <fdp>、SecondDrowsyPoints が <sdp> の DrowZzzGameSession (DDP は M2-PR4 で 0 から加算予定)
    もし TotalPoints(playerId) を取得する
    ならば <total> が返る

    例:
      | fdp | sdp | total | 備考                |
      | 100 | 10  | 110   | SDP 正値             |
      | 100 | -10 | 90    | SDP 負値(0 floor なし、DZ-109)|

  @DZ-104
  シナリオ: TotalPoints に存在しない PlayerId を渡す (異常系・Small)
    前提 Players=[p1, p2] の DrowZzzGameSession
    もし TotalPoints(PlayerId.Of("p3")) を取得する
    ならば ArgumentException が発生する

  @DZ-105
  シナリオ: StartGameUseCase が全プレイヤーの SDP を 0 で初期化 (正常系・Small)
    前提 StartGameUseCase に N=2 の player ids と初期山札
    もし Execute() を呼ぶ
    ならば 全プレイヤーの SDP が 0 で初期化された DrowZzzGameSession が返る

  @DZ-107
  シナリオ: 既存 Session に with { SecondDrowsyPoints = null } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession
    もし with { SecondDrowsyPoints = null } を適用する
    ならば ArgumentNullException が発生する

  @DZ-108
  シナリオ: 既存 Session に with { SecondDrowsyPoints = キー不一致 } を適用 (異常系・Small)
    前提 N=2 の有効な DrowZzzGameSession (Players = [p1, p2])
    もし with { SecondDrowsyPoints = [p1: 0, p3: 5] } を適用する
    ならば ArgumentException が発生する

  @DZ-109
  シナリオ: 負値の SDP も保持される (正常系・Small、0 floor なし)
    前提 SDP に [p1: -20, p2: -5] を持つ DrowZzzGameSession (ADR-0009 戦略「持ち点低い方が勝ち」と整合)
    もし SecondDrowsyPoints を取得する
    ならば 負値も保持された Dictionary が返る (0 floor 適用なし)
