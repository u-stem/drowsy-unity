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
