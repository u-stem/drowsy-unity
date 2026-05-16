# language: ja
機能: CardId(カードの instance unique 識別子、ADR-0018 で refactor 済)

  @CARD-004
  シナリオ: 有効な (typeId, instance) で CardId を生成 (正常系・Small)
    前提 typeId = CardTypeId.Of("dream"), instance = 3
    もし CardId.Of(typeId, 3) を呼ぶ
    ならば 生成された CardId の TypeId は typeId、Instance は 3、Value は "dream#3"

  @CARD-005
  シナリオ: 異なる typeId の 2 つの CardId は非等価 (正常系・Small)
    前提 a = CardId.Of(CardTypeId.Of("dream"), 0), b = CardId.Of(CardTypeId.Of("sheep"), 0)
    もし a と b を Equals 比較
    ならば 非等価

  @CARD-005
  シナリオ: 同じ typeId で異なる instance の 2 つの CardId は非等価 (正常系・Small)
    前提 a = CardId.Of(typeId, 0), b = CardId.Of(typeId, 1)
    もし a と b を Equals 比較
    ならば 非等価

  @CARD-006
  シナリオ: typeId = null で生成を試みる (異常系・Small)
    前提 typeId = null
    もし CardId.Of(null, 0) を呼ぶ
    ならば ArgumentNullException が発生する

  @CARD-007
  シナリオ: instance = 負数 で生成を試みる (異常系・Small)
    前提 typeId = CardTypeId.Of("dream"), instance = -1
    もし CardId.Of(typeId, -1) を呼ぶ
    ならば ArgumentOutOfRangeException が発生する

  @CARD-003
  シナリオ: 同じ (typeId, instance) の 2 つの CardId は等価 (正常系・Small)
    前提 a = CardId.Of(typeId, 0), b = CardId.Of(typeId, 0)
    もし 等価比較
    ならば 等価(record 自動生成、GetHashCode も等しい)

  @CARD-009
  シナリオ: ToString は Value(<typeId>#<instance>)を返す (正常系・Small)
    前提 CardId.Of(CardTypeId.Of("dream"), 5)
    もし ToString() を呼ぶ
    ならば 文字列 "dream#5" が返る(Value と同じ)

  # ADR-0018 による要件 ID の意味置換 / 廃止:
  # - 旧 CARD-006(CardId.Of(null) で ArgumentException)は CARD-006 redefined: typeId null で ArgumentNullException
  # - 旧 CARD-007(CardId.Of("") で ArgumentException)は廃止。catalog key 検証は CTYPE-002(card-type-id.feature)に移行
  # - 旧 CARD-008(CardId.Of("   ") で ArgumentException)も廃止、CTYPE-002 に統合
  # - 新 CARD-007(instance 負数で ArgumentOutOfRangeException)新設
  # - 新 CARD-009(ToString 仕様、旧 CARD-005 を rename して番号衝突回避)
