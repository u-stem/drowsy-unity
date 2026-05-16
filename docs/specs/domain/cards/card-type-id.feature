# language: ja
機能: CardTypeId(カード種別 ID、ADR-0018 で新設)

  @CTYPE-002
  シナリオ: null は ArgumentException (異常系・Small)
    前提 value = null
    もし CardTypeId.Of(null) を呼ぶ
    ならば ArgumentException が発生する

  @CTYPE-002
  シナリオ: 空文字列は ArgumentException (異常系・Small)
    前提 value = ""
    もし CardTypeId.Of("") を呼ぶ
    ならば ArgumentException が発生する

  @CTYPE-002
  シナリオ: 空白のみは ArgumentException (異常系・Small)
    前提 value = "   "
    もし CardTypeId.Of("   ") を呼ぶ
    ならば ArgumentException が発生する

  @CTYPE-003
  シナリオ: 非空 string は Value に保持される (正常系・Small)
    前提 value = "dream"
    もし CardTypeId.Of("dream") を呼ぶ
    ならば typeId.Value は "dream"

  @CTYPE-005
  シナリオ: '#' を含む文字列は ArgumentException (異常系・Small)
    前提 value = "dream#extra"(ADR-0018 §8 で CardId.Value の区切り文字として予約)
    もし CardTypeId.Of("dream#extra") を呼ぶ
    ならば ArgumentException が発生する

  @CTYPE-004
  シナリオ: 同じ Value の 2 つの CardTypeId は等価 (正常系・Small)
    前提 a = CardTypeId.Of("dream"), b = CardTypeId.Of("dream")
    もし a と b を Equals 比較
    ならば 等価(record auto-equals)

  @CTYPE-004
  シナリオ: 異なる Value の 2 つの CardTypeId は非等価 (正常系・Small)
    前提 a = CardTypeId.Of("dream"), b = CardTypeId.Of("sheep")
    もし a と b を Equals 比較
    ならば 非等価
