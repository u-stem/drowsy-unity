# language: ja
機能: InMemoryCardCatalog (ICardCatalog の in-memory 実装)

  @APP-012
  シナリオ: entries の防御コピー (正常系・Small)
    前提 entries に CardId.Of("X") と CardData を 1 件登録
    かつ InMemoryCardCatalog を生成
    もし 元の entries を変更する (例: entries に追加 / 削除)
    ならば catalog 側の挙動は変化しない (Get / TryGet が当初の登録内容のまま)

  @APP-013
  シナリオ: 登録済 CardId に対して Get が CardData を返す (正常系・Small)
    前提 InMemoryCardCatalog に CardId.Of("X") と CardData を登録
    もし Get(CardId.Of("X")) を呼ぶ
    ならば 登録した CardData が返る

  @APP-014
  シナリオ: 未登録 CardId に対して Get が例外を投げる (異常系・Small)
    前提 空の InMemoryCardCatalog
    もし Get(CardId.Of("Y")) を呼ぶ
    ならば KeyNotFoundException が発生する

  @APP-015
  シナリオ: 登録済 CardId に対して TryGet が true を返す (正常系・Small)
    前提 InMemoryCardCatalog に CardId.Of("X") と CardData を登録
    もし TryGet(CardId.Of("X"), out _) を呼ぶ
    ならば 戻り値は true である

  @APP-016
  シナリオ: 登録済 CardId に対して TryGet が data に対応 CardData を設定する (正常系・Small)
    前提 InMemoryCardCatalog に CardId.Of("X") と CardData を登録
    もし TryGet(CardId.Of("X"), out var data) を呼ぶ
    ならば data は登録した CardData である

  @APP-017
  シナリオ: 未登録 CardId に対して TryGet が false を返す (正常系・Small)
    前提 空の InMemoryCardCatalog
    もし TryGet(CardId.Of("Y"), out _) を呼ぶ
    ならば 戻り値は false である

  @APP-018
  シナリオ: 未登録 CardId に対して TryGet が data に null を設定する (正常系・Small)
    前提 空の InMemoryCardCatalog
    もし TryGet(CardId.Of("Y"), out var data) を呼ぶ
    ならば data は null である

  @APP-019
  シナリオ: entries に null を渡して生成 (異常系・Small)
    前提 entries に null
    もし InMemoryCardCatalog を生成する
    ならば ArgumentNullException が発生する

  @APP-020
  シナリオ: entries に null CardData を含む場合 (異常系・Small)
    前提 entries に CardId.Of("X") と null CardData の組を含む
    もし InMemoryCardCatalog を生成する
    ならば ArgumentException が発生する
