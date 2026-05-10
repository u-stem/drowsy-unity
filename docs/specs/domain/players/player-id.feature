# language: ja
機能: PlayerId(プレイヤー識別子)

  @PLAYER-003
  シナリオ: 同じ値の PlayerId は等価 (正常系・Small)
    前提 PlayerId.Of("p1") を 2 つ生成
    もし 等価比較する
    ならば 2 つは等価である

  @PLAYER-003
  シナリオ: 異なる値の PlayerId は非等価 (正常系・Small)
    前提 PlayerId.Of("p1") と PlayerId.Of("p2")
    もし 等価比較する
    ならば 2 つは非等価である

  @PLAYER-004
  シナリオ: 有効な値で PlayerId を生成 (正常系・Small)
    前提 文字列 "p1"
    もし PlayerId.Of で生成する
    ならば 生成された PlayerId の Value は "p1" である

  @PLAYER-005
  シナリオ: ToString は Value を返す (正常系・Small)
    前提 PlayerId.Of("alice")
    もし ToString() を呼ぶ
    ならば 文字列 "alice" が返る

  @PLAYER-006
  シナリオ: null で生成を試みる (異常系・Small)
    前提 null
    もし PlayerId.Of で生成する
    ならば ArgumentException が発生する

  @PLAYER-007
  シナリオ: 空文字列で生成を試みる (異常系・Small)
    前提 空文字列 ""
    もし PlayerId.Of で生成する
    ならば ArgumentException が発生する

  @PLAYER-008
  シナリオ: 空白のみの文字列で生成を試みる (異常系・Small)
    前提 文字列 "   "
    もし PlayerId.Of で生成する
    ならば ArgumentException が発生する
