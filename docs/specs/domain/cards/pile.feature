# language: ja
機能: 山札の操作 (Pile)

  @PILE-005
  シナリオ: AddTop で先頭に挿入 (正常系・Small)
    前提 [B, C] が積まれた山札
    もし AddTop(A) を呼ぶ
    ならば 結果は [A, B, C] である

  @PILE-001
  シナリオ: AddTop 後も元の山札は不変 (正常系・Small)
    前提 [B, C] が積まれた山札
    もし AddTop(A) を呼ぶ
    ならば 元の山札は [B, C] のまま変更されない

  @PILE-006
  シナリオ: AddBottom で末尾に追加 (正常系・Small)
    前提 [A, B] が積まれた山札
    もし AddBottom(C) を呼ぶ
    ならば 結果は [A, B, C] である

  @PILE-007
  シナリオ: 空でない山札から Draw (正常系・Small)
    前提 [A, B, C] が積まれた山札
    もし Draw() を呼ぶ
    ならば 引いたカードは A である
    かつ 残り山札は [B, C] である

  @PILE-007
  シナリオ: 1 枚しかない山札から Draw (準正常系・Small)
    前提 [A] が積まれた山札
    もし Draw() を呼ぶ
    ならば 引いたカードは A である
    かつ 残り山札は空である

  @PILE-009
  シナリオ: 空の山札から Draw (異常系・Small)
    前提 空の山札 (Pile.Empty)
    もし Draw() を呼ぶ
    ならば InvalidOperationException が発生する

  @PILE-010
  シナリオ: AddTop に null (異常系・Small)
    前提 任意の山札
    もし AddTop(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @PILE-011
  シナリオ: AddBottom に null (異常系・Small)
    前提 任意の山札
    もし AddBottom(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @PILE-013
  シナリオ: コンストラクタに null (異常系・Small)
    前提 IEnumerable<CardId> として null
    もし new Pile(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @PILE-008
  シナリオ: シャッフルが seed 固定で決定的 (準正常系・Small)
    前提 [A, B, C, D, E] が積まれた山札
    かつ シード 42 の XorShiftRandom を 2 つ生成
    もし それぞれシャッフルする
    ならば 2 つの結果は同じ並びになる

  @PILE-008
  シナリオ: シャッフル後も要素集合は同一 (正常系・Small)
    前提 [A, B, C, D, E] が積まれた山札
    かつ 任意の XorShiftRandom
    もし シャッフルする
    ならば 結果の要素集合(順不同)は元と同じ

  @PILE-012
  シナリオ: Shuffle に null IRandomSource (異常系・Small)
    前提 任意の山札
    もし Shuffle(null) を呼ぶ
    ならば ArgumentNullException が発生する

  @PILE-003 @PILE-004
  シナリオ: Pile.Empty (正常系・Small)
    前提 Pile.Empty
    もし IsEmpty / Count を確認
    ならば IsEmpty は true / Count は 0

  @PILE-014
  シナリオ: 同順序同要素の Pile は等価 (正常系・Small)
    前提 [A, B] と [A, B] の 2 つの Pile
    もし Equals 比較
    ならば 等価

  @PILE-014
  シナリオ: 同枚数で異なる順序の Pile は非等価 (正常系・Small)
    前提 [A, B] と [B, A] の 2 つの Pile
    もし Equals 比較
    ならば 非等価

  @PILE-014
  シナリオ: 同枚数で異なるカードの Pile は非等価 (正常系・Small)
    前提 [A, B] と [A, C] の 2 つの Pile
    もし Equals 比較
    ならば 非等価

  @PILE-014
  シナリオ: 異なる枚数の Pile は非等価 (正常系・Small)
    前提 [A] と [A, B] の 2 つの Pile
    もし Equals 比較
    ならば 非等価

  @PILE-014
  シナリオ: 同一インスタンスの Pile は等価 (正常系・Small)
    前提 任意の Pile
    もし 自分自身と Equals 比較
    ならば 等価

  @PILE-014
  シナリオ: 両方空 Pile は等価 (正常系・Small)
    前提 Pile.Empty と new Pile([])
    もし Equals 比較
    ならば 等価

  @PILE-014
  シナリオ: Equals(Pile) に null を渡すと false (正常系・Small)
    前提 任意の Pile
    もし null の Pile を引数に Equals
    ならば false

  @PILE-015
  シナリオ: 等価な Pile の GetHashCode は一致 (正常系・Small)
    前提 [A, B] と [A, B] の 2 つの Pile
    もし GetHashCode
    ならば 2 つのハッシュ値は等しい

  @PILE-015
  シナリオ: 両方空 Pile の GetHashCode は一致 (正常系・Small)
    前提 Pile.Empty と new Pile([])
    もし GetHashCode
    ならば 2 つのハッシュ値は等しい

  @PILE-016
  シナリオ: 等価な Pile は operator== で true (正常系・Small)
    前提 [A] を持つ 2 つの Pile
    もし operator== で比較
    ならば true

  @PILE-016
  シナリオ: 非等価な Pile は operator== で false (正常系・Small)
    前提 [A] と [B] の 2 つの Pile
    もし operator== で比較
    ならば false

  @PILE-016
  シナリオ: 非等価な Pile は operator!= で true (正常系・Small)
    前提 [A] と [B] の 2 つの Pile
    もし operator!= で比較
    ならば true

  @PILE-016
  シナリオ: 両方 null は operator== で true (正常系・Small)
    前提 null の Pile 参照を 2 つ
    もし operator== で比較
    ならば true

  @PILE-016
  シナリオ: 片方 null で他方非 null は operator== で false (左側 null) (正常系・Small)
    前提 null の Pile 参照と Pile.Empty
    もし operator== で比較
    ならば false

  @PILE-016
  シナリオ: 左側非 null で右側 null は operator== で false (正常系・Small)
    前提 Pile.Empty と null の Pile 参照
    もし operator== で比較
    ならば false

  @PILE-017
  シナリオ: Equals(object) に null を渡すと false (正常系・Small)
    前提 Pile.Empty
    もし Equals((object)null) を呼ぶ
    ならば false

  @PILE-017
  シナリオ: Equals(object) に異なる型を渡すと false (正常系・Small)
    前提 Pile.Empty
    もし Equals((object)"not a Pile") を呼ぶ
    ならば false
