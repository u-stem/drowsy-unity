# language: ja
機能: CardId(カード識別子)

  @CARD-004
  シナリオ: 有効な値で CardId を生成 (正常系・Small)
    前提 文字列 "hearts-7"
    もし CardId.Of で生成する
    ならば 生成された CardId の Value は "hearts-7" である

  @CARD-007
  シナリオ: 空文字列で生成を試みる (異常系・Small)
    前提 空文字列 ""
    もし CardId.Of で生成する
    ならば ArgumentException が発生する

  @CARD-006
  シナリオ: null で生成を試みる (異常系・Small)
    前提 null
    もし CardId.Of で生成する
    ならば ArgumentException が発生する

  @CARD-008
  シナリオ: 空白のみの文字列で生成を試みる (異常系・Small)
    前提 文字列 "   "
    もし CardId.Of で生成する
    ならば ArgumentException が発生する

  @CARD-003
  シナリオ: 同じ値の CardId は等価 (正常系・Small)
    前提 CardId.Of("X") を 2 つ生成
    もし 等価比較する
    ならば 2 つは等価である

  @CARD-003
  シナリオ: 異なる値の CardId は非等価 (正常系・Small)
    前提 CardId.Of("X") と CardId.Of("Y")
    もし 等価比較する
    ならば 2 つは非等価である

  @CARD-005
  シナリオ: ToString は Value を返す (正常系・Small)
    前提 CardId.Of("clubs-Q")
    もし ToString() を呼ぶ
    ならば 文字列 "clubs-Q" が返る
