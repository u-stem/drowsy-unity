# language: ja
機能: ICardCatalog(カードデータ解決 interface)

  # 注(ADR-0018): lookup key は CardTypeId(種別 ID)。CardId(instance unique 識別子)から
  # catalog を引く場合は cardId.TypeId を渡す。

  @APP-007
  シナリオ: 登録済 CardTypeId に対して Get が CardData を返す (正常系・Small)
    前提 ICardCatalog のダミー実装 DummyCatalog に CardTypeId.Of("X") と対応 CardData を登録
    もし DummyCatalog.Get(CardTypeId.Of("X")) を呼ぶ
    ならば 登録した CardData が返る

  @APP-008
  シナリオ: 登録済 CardTypeId に対して TryGet が true を返す (正常系・Small)
    前提 ICardCatalog のダミー実装 DummyCatalog に CardTypeId.Of("X") と対応 CardData を登録
    もし DummyCatalog.TryGet(CardTypeId.Of("X"), out _) を呼ぶ
    ならば 戻り値は true である

  @APP-008
  シナリオ: 登録済 CardTypeId に対して TryGet が data に対応 CardData を設定する (正常系・Small)
    前提 ICardCatalog のダミー実装 DummyCatalog に CardTypeId.Of("X") と対応 CardData を登録
    もし DummyCatalog.TryGet(CardTypeId.Of("X"), out var data) を呼ぶ
    ならば data は登録した CardData である

  @APP-009
  シナリオ: 未登録 CardTypeId に対して TryGet が false を返す (正常系・Small)
    前提 空の DummyCatalog
    もし DummyCatalog.TryGet(CardTypeId.Of("Y"), out _) を呼ぶ
    ならば 戻り値は false である

  @APP-009
  シナリオ: 未登録 CardTypeId に対して TryGet が data に null を設定する (正常系・Small)
    前提 空の DummyCatalog
    もし DummyCatalog.TryGet(CardTypeId.Of("Y"), out var data) を呼ぶ
    ならば data は null である

  @APP-010
  シナリオ: 未登録 CardTypeId に対して Get が例外を投げる (異常系・Small)
    前提 空の DummyCatalog
    もし DummyCatalog.Get(CardTypeId.Of("Y")) を呼ぶ
    ならば KeyNotFoundException が発生する
