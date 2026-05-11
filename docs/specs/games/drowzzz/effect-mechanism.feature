# language: ja
# DrowZzz カード効果メカニズム (M2-PR1) の受け入れシナリオ
# 対応 EARS: docs/specs/games/drowzzz/effect-mechanism.md (DZ-082〜DZ-088)
# 配置先: docs/specs/games/drowzzz/effect-mechanism.feature

機能: DrowZzz カード効果メカニズム

  @DZ-083
  シナリオ: PlayCardAction.Apply で catalog.GetEffects が 1 回呼ばれる (正常系・Small)
    前提 spy catalog (GetEffects 呼び出し回数と引数を記録) を持つ DrowZzzRule を生成する
    かつ WaitingForPlay フェーズで現プレイヤーが手札に c1 を持つ session を準備する
    もし rule.Apply(session, PlayCardAction(c1)) を呼ぶ
    ならば spy catalog.GetEffects(c1) が ちょうど 1 回呼ばれている

  @DZ-084
  シナリオ: 効果空の catalog では完走し例外を投げない (正常系・Small)
    前提 全カード空効果列を返す InMemoryCardCatalog を持つ DrowZzzRule を生成する
    かつ WaitingForPlay フェーズで現プレイヤーが手札に c1 を持つ session を準備する
    もし rule.Apply(session, PlayCardAction(c1)) を呼ぶ
    ならば NotImplementedException や他の例外が投げられない (Interpreter.Apply は呼ばれない)

  @DZ-087
  シナリオ: catalog が null のとき ArgumentNullException("catalog") を投げる (異常系・Small)
    前提 EffectInterpreter のインスタンスを用意する
    もし new DrowZzzRule(null, interpreter) を呼ぶ
    ならば ArgumentNullException が投げられる
    かつ 例外の ParamName が "catalog" である

  @DZ-088
  シナリオ: interpreter が null のとき ArgumentNullException("interpreter") を投げる (異常系・Small)
    前提 空 InMemoryCardCatalog を用意する
    もし new DrowZzzRule(catalog, null) を呼ぶ
    ならば ArgumentNullException が投げられる
    かつ 例外の ParamName が "interpreter" である
