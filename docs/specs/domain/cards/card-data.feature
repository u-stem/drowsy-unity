# language: ja
機能: CardData(カードの中身を表す不変値オブジェクト)

  @CDATA-003
  シナリオ: 有効な name と属性で生成すると Name が保持される (正常系・Small)
    前提 name "Joker" と属性 {"power": 10, "rarity": 3}
    もし new CardData(name, attributes) で生成する
    ならば 生成された CardData の Name は "Joker" である

  @CDATA-003
  シナリオ: 有効な name と属性で生成すると Attributes が保持される (正常系・Small)
    前提 name "Joker" と属性 {"power": 10}
    もし new CardData(name, attributes) で生成する
    ならば 生成された CardData の Attributes["power"] は 10 である

  @CDATA-004
  シナリオ: 存在するキーで HasAttribute (正常系・Small)
    前提 属性 {"power": 10} を持つ CardData
    もし HasAttribute("power") を呼ぶ
    ならば true が返る

  @CDATA-005
  シナリオ: 存在しないキーで HasAttribute (正常系・Small)
    前提 属性 {"power": 10} を持つ CardData
    もし HasAttribute("cost") を呼ぶ
    ならば false が返る

  @CDATA-006
  シナリオ: 存在するキーで GetAttribute (正常系・Small)
    前提 属性 {"power": 10} を持つ CardData
    もし GetAttribute("power", -1) を呼ぶ
    ならば 10 が返る

  @CDATA-007
  シナリオ: 存在しないキーで GetAttribute (準正常系・Small)
    前提 属性 {"power": 10} を持つ CardData
    もし GetAttribute("cost", 999) を呼ぶ
    ならば 999 が返る

  @CDATA-008
  シナリオ: 順序の異なる同じキー値ペアは等価 (正常系・Small)
    前提 CardData("X", {"a": 1, "b": 2}) と CardData("X", {"b": 2, "a": 1})
    もし 等価比較する
    ならば 2 つは等価である

  @CDATA-008
  シナリオ: 異なる Name は非等価 (正常系・Small)
    前提 CardData("X", {"a": 1}) と CardData("Y", {"a": 1})
    もし 等価比較する
    ならば 2 つは非等価である

  @CDATA-008
  シナリオ: 同じキーで異なる値は非等価 (正常系・Small)
    前提 CardData("X", {"a": 1}) と CardData("X", {"a": 2})
    もし 等価比較する
    ならば 2 つは非等価である

  @CDATA-008
  シナリオ: 両方空辞書の同名 CardData は等価 (正常系・Small)
    前提 CardData("X", {}) を 2 つ
    もし 等価比較する
    ならば 2 つは等価である

  @CDATA-008
  シナリオ: 単一属性で同じキー値は等価 (正常系・Small)
    前提 CardData("X", {"a": 1}) を 2 つ
    もし 等価比較する
    ならば 2 つは等価である

  @CDATA-008
  シナリオ: 属性キー数が異なる場合は非等価 (正常系・Small)
    前提 CardData("X", {"a": 1}) と CardData("X", {"a": 1, "b": 2})
    もし 等価比較する
    ならば 2 つは非等価である

  @CDATA-008
  シナリオ: 同数だがキー名が異なる場合は非等価 (正常系・Small)
    前提 CardData("X", {"a": 1, "b": 2}) と CardData("X", {"a": 1, "c": 2})
    もし 等価比較する
    ならば 2 つは非等価である

  @CDATA-008
  シナリオ: 同一インスタンス自身との等価比較は true (正常系・Small)
    前提 任意の CardData
    もし 自分自身と等価比較する
    ならば true が返る

  @CDATA-008
  シナリオ: Equals(CardData) に null を渡すと false (正常系・Small)
    前提 任意の CardData
    もし null の CardData を引数に Equals を呼ぶ
    ならば false が返る

  @CDATA-009
  シナリオ: 等価な CardData の GetHashCode は一致 (正常系・Small)
    前提 CardData("X", {"a": 1, "b": 2}) と CardData("X", {"b": 2, "a": 1})
    もし GetHashCode を呼ぶ
    ならば 2 つのハッシュ値は等しい

  @CDATA-010
  シナリオ: 生成後にソース辞書の既存キーを変更しても CardData は不変 (正常系・Small)
    前提 ソース辞書 {"a": 1} と そこから生成した CardData
    もし ソース辞書に "a"=999 を書き込む
    ならば CardData.Attributes["a"] は 1 のまま

  @CDATA-010
  シナリオ: 生成後にソース辞書に新キーを追加しても CardData は不変 (正常系・Small)
    前提 ソース辞書 {"a": 1} と そこから生成した CardData
    もし ソース辞書に "b"=2 を追加する
    ならば CardData.Attributes は "b" を含まない

  @CDATA-011
  シナリオ: null name で生成を試みる (異常系・Small)
    前提 null name
    もし new CardData(null, {}) で生成する
    ならば ArgumentException が発生する

  @CDATA-011
  シナリオ: 空文字列 name で生成を試みる (異常系・Small)
    前提 空文字列 ""
    もし new CardData("", {}) で生成する
    ならば ArgumentException が発生する

  @CDATA-011
  シナリオ: 空白のみ name で生成を試みる (異常系・Small)
    前提 空白のみ "   "
    もし new CardData("   ", {}) で生成する
    ならば ArgumentException が発生する

  @CDATA-012
  シナリオ: null attributes で生成を試みる (異常系・Small)
    前提 null attributes
    もし new CardData("X", null) で生成する
    ならば ArgumentNullException が発生する

  @CDATA-013
  シナリオ: null キーを含む attributes で生成を試みる (異常系・Small)
    前提 attributes [("valid", 1), (null, 2)]
    もし new CardData("X", attributes) で生成する
    ならば ArgumentException が発生する

  @CDATA-013
  シナリオ: 空文字列キーを含む attributes で生成を試みる (異常系・Small)
    前提 attributes {"valid": 1, "": 2}
    もし new CardData("X", attributes) で生成する
    ならば ArgumentException が発生する

  @CDATA-013
  シナリオ: 空白のみキーを含む attributes で生成を試みる (異常系・Small)
    前提 attributes {"valid": 1, "   ": 2}
    もし new CardData("X", attributes) で生成する
    ならば ArgumentException が発生する

  @CDATA-014
  シナリオ: null キーで HasAttribute を呼ぶ (異常系・Small)
    前提 任意の CardData
    もし HasAttribute(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @CDATA-014
  シナリオ: null キーで GetAttribute を呼ぶ (異常系・Small)
    前提 任意の CardData
    もし GetAttribute(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @CDATA-015
  シナリオ: 等価な 2 つの CardData は operator== で true (正常系・Small)
    前提 CardData("X", {"a": 1}) を 2 つ
    もし operator== で比較する
    ならば true が返る

  @CDATA-015
  シナリオ: 非等価な 2 つの CardData は operator!= で true (正常系・Small)
    前提 CardData("X", {"a": 1}) と CardData("Y", {"a": 1})
    もし operator!= で比較する
    ならば true が返る

  @CDATA-015
  シナリオ: 非等価な 2 つの CardData は operator== で false (正常系・Small)
    前提 CardData("X", {"a": 1}) と CardData("Y", {"a": 1})
    もし operator== で比較する
    ならば false が返る

  @CDATA-015
  シナリオ: 両方 null の operator== は true (正常系・Small)
    前提 null の CardData 参照を 2 つ
    もし operator== で比較する
    ならば true が返る

  @CDATA-015
  シナリオ: 片方 null で他方非 null の operator== は false (正常系・Small)
    前提 null の CardData 参照と 非 null の CardData 参照
    もし operator== で比較する
    ならば false が返る

  @CDATA-017
  シナリオ: Equals(object) に null を渡すと false (正常系・Small)
    前提 任意の CardData
    もし Equals((object)null) を呼ぶ
    ならば false が返る

  @CDATA-017
  シナリオ: Equals(object) に異なる型を渡すと false (正常系・Small)
    前提 任意の CardData
    もし Equals((object)"not a CardData") を呼ぶ
    ならば false が返る
