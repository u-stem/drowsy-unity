# language: ja
機能: Hand(プレイヤーの手札を表す不変値オブジェクト、重複拒否・順序付き)

  @HAND-003
  シナリオ: 有効な cards で生成すると Cards が同順序で保持される (正常系・Small)
    前提 [CardId.Of("a"), CardId.Of("b")]
    もし new Hand(cards) で生成する
    ならば Cards は [CardId.Of("a"), CardId.Of("b")] の順で並ぶ

  @HAND-004
  シナリオ: Hand.Empty の Count は 0 (正常系・Small)
    前提 Hand.Empty
    もし Count を読む
    ならば 0 が返る

  @HAND-004
  シナリオ: Hand.Empty の IsEmpty は true (正常系・Small)
    前提 Hand.Empty
    もし IsEmpty を読む
    ならば true が返る

  @HAND-005
  シナリオ: 既存にない CardId を Add すると末尾に追加 (正常系・Small)
    前提 [CardId.Of("a")] を持つ Hand
    もし Add(CardId.Of("b")) する
    ならば 新 Hand の Cards は [CardId.Of("a"), CardId.Of("b")] である

  @HAND-006
  シナリオ: 中間の CardId を Remove (正常系・Small)
    前提 [CardId.Of("a"), CardId.Of("b"), CardId.Of("c")] を持つ Hand
    もし Remove(CardId.Of("b")) する
    ならば 新 Hand の Cards は [CardId.Of("a"), CardId.Of("c")] である

  @HAND-006
  シナリオ: 先頭の CardId を Remove (正常系・Small)
    前提 [CardId.Of("a"), CardId.Of("b"), CardId.Of("c")] を持つ Hand
    もし Remove(CardId.Of("a")) する
    ならば 新 Hand の Cards は [CardId.Of("b"), CardId.Of("c")] である

  @HAND-006
  シナリオ: 末尾の CardId を Remove (正常系・Small)
    前提 [CardId.Of("a"), CardId.Of("b"), CardId.Of("c")] を持つ Hand
    もし Remove(CardId.Of("c")) する
    ならば 新 Hand の Cards は [CardId.Of("a"), CardId.Of("b")] である

  @HAND-007
  シナリオ: 存在する CardId で Contains は true (正常系・Small)
    前提 [CardId.Of("a")] を持つ Hand
    もし Contains(CardId.Of("a")) を呼ぶ
    ならば true が返る

  @HAND-008
  シナリオ: 存在しない CardId で Contains は false (正常系・Small)
    前提 [CardId.Of("a")] を持つ Hand
    もし Contains(CardId.Of("z")) を呼ぶ
    ならば false が返る

  @HAND-009
  シナリオ: Count は枚数を返す (正常系・Small)
    前提 [CardId.Of("a"), CardId.Of("b")] を持つ Hand
    もし Count を読む
    ならば 2 が返る

  @HAND-010
  シナリオ: 空 Hand の IsEmpty は true (正常系・Small)
    前提 空 Hand
    もし IsEmpty を読む
    ならば true が返る

  @HAND-010
  シナリオ: 1 枚以上の Hand の IsEmpty は false (正常系・Small)
    前提 1 枚以上のカードを持つ Hand
    もし IsEmpty を読む
    ならば false が返る

  @HAND-011
  シナリオ: 同順序同要素の 2 Hand は等価 (正常系・Small)
    前提 [a, b] と [a, b] の 2 つの Hand
    もし Equals 比較
    ならば 等価

  @HAND-011
  シナリオ: 同枚数で異なる順序は非等価 (正常系・Small)
    前提 [a, b] と [b, a] の 2 つの Hand
    もし Equals 比較
    ならば 非等価

  @HAND-011
  シナリオ: 同枚数で異なるカードは非等価 (正常系・Small)
    前提 [a, b] と [a, c] の 2 つの Hand
    もし Equals 比較
    ならば 非等価

  @HAND-011
  シナリオ: 異なる枚数は非等価 (正常系・Small)
    前提 [a] と [a, b] の 2 つの Hand
    もし Equals 比較
    ならば 非等価

  @HAND-011
  シナリオ: 同一インスタンスは等価 (正常系・Small)
    前提 任意の Hand
    もし 自分自身と Equals 比較
    ならば 等価

  @HAND-011
  シナリオ: 両方空 Hand は等価 (正常系・Small)
    前提 Hand.Empty を 2 つ参照
    もし Equals 比較
    ならば 等価

  @HAND-011
  シナリオ: Equals(Hand) に null を渡すと false (正常系・Small)
    前提 任意の Hand
    もし null の Hand を引数に Equals
    ならば false

  @HAND-012
  シナリオ: 等価な Hand の GetHashCode は一致 (正常系・Small)
    前提 [a, b] と [a, b] の 2 つの Hand
    もし GetHashCode
    ならば 2 つのハッシュ値は等しい

  @HAND-012
  シナリオ: 両方空 Hand の GetHashCode は一致 (正常系・Small)
    前提 空 Hand を 2 つ
    もし GetHashCode
    ならば 2 つのハッシュ値は等しい

  @HAND-013
  シナリオ: 等価な Hand は operator== で true (正常系・Small)
    前提 [a] を持つ 2 つの Hand
    もし operator== で比較
    ならば true

  @HAND-013
  シナリオ: 非等価な Hand は operator== で false (正常系・Small)
    前提 [a] と [b] の 2 つの Hand
    もし operator== で比較
    ならば false

  @HAND-013
  シナリオ: 非等価な Hand は operator!= で true (正常系・Small)
    前提 [a] と [b] の 2 つの Hand
    もし operator!= で比較
    ならば true

  @HAND-013
  シナリオ: 両方 null は operator== で true (正常系・Small)
    前提 null の Hand 参照を 2 つ
    もし operator== で比較
    ならば true

  @HAND-013
  シナリオ: 片方 null で他方非 null は operator== で false (左側 null) (正常系・Small)
    前提 null の Hand 参照と 非 null Hand
    もし operator== で比較
    ならば false

  @HAND-013
  シナリオ: 左側非 null で右側 null は operator== で false (正常系・Small)
    前提 非 null Hand と null の Hand 参照
    もし operator== で比較
    ならば false

  @HAND-014
  シナリオ: Equals(object) に null を渡すと false (正常系・Small)
    前提 任意の Hand
    もし Equals((object)null) を呼ぶ
    ならば false

  @HAND-014
  シナリオ: Equals(object) に異なる型を渡すと false (正常系・Small)
    前提 任意の Hand
    もし Equals((object)"not a Hand") を呼ぶ
    ならば false

  @HAND-015
  シナリオ: コンストラクタ cards が null (異常系・Small)
    前提 null cards
    もし new Hand(null) で生成
    ならば ArgumentNullException が発生

  @HAND-016
  シナリオ: コンストラクタ cards に null CardId を含む (異常系・Small)
    前提 [CardId.Of("a"), null]
    もし new Hand(cards) で生成
    ならば ArgumentException が発生

  @HAND-017
  シナリオ: コンストラクタ cards に重複 CardId を含む (異常系・Small)
    前提 [CardId.Of("a"), CardId.Of("a")]
    もし new Hand(cards) で生成
    ならば ArgumentException が発生

  @HAND-018
  シナリオ: Add(null) (異常系・Small)
    前提 任意の Hand
    もし Add(null) を呼ぶ
    ならば ArgumentNullException が発生

  @HAND-019
  シナリオ: Add で既存 CardId を渡す (異常系・Small)
    前提 [CardId.Of("a")] を持つ Hand
    もし Add(CardId.Of("a")) を呼ぶ
    ならば ArgumentException が発生

  @HAND-020
  シナリオ: Remove(null) (異常系・Small)
    前提 任意の Hand
    もし Remove(null) を呼ぶ
    ならば ArgumentNullException が発生

  @HAND-021
  シナリオ: Remove で不在 CardId を渡す (異常系・Small)
    前提 [CardId.Of("a")] を持つ Hand
    もし Remove(CardId.Of("z")) を呼ぶ
    ならば ArgumentException が発生

  @HAND-022
  シナリオ: Contains(null) (異常系・Small)
    前提 任意の Hand
    もし Contains(null) を呼ぶ
    ならば ArgumentNullException が発生
