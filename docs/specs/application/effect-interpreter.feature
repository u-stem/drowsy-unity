# language: ja
# EffectInterpreter の受け入れシナリオ
# 対応 EARS: docs/specs/application/effect-interpreter.md (APP-031〜APP-035)
# 配置先: docs/specs/application/effect-interpreter.feature

機能: EffectInterpreter

  @APP-033
  シナリオ: session が null のとき ArgumentNullException("session") を投げる (異常系・Small)
    前提 EffectInterpreter のインスタンスを生成する
    もし Apply(null, effect) を呼ぶ (effect は任意の IEffect 派生型)
    ならば ArgumentNullException が投げられる
    かつ 例外の ParamName は "session" である

  @APP-034
  シナリオ: effect が null のとき ArgumentNullException("effect") を投げる (異常系・Small)
    前提 EffectInterpreter のインスタンスを生成する
    もし Apply(session, null) を呼ぶ (session は任意の DrowZzzGameSession)
    ならば ArgumentNullException が投げられる
    かつ 例外の ParamName は "effect" である

  @APP-035
  シナリオ: 未知の IEffect 派生型のとき NotImplementedException を投げる (異常系・Small)
    前提 EffectInterpreter のインスタンスを生成する
    かつ UnknownEffect (テスト用の任意の IEffect 派生型) のインスタンスを用意する
    もし Apply(session, unknownEffect) を呼ぶ
    ならば NotImplementedException が投げられる
    かつ 例外メッセージに "UnknownEffect" (派生型の Type 名) が含まれる
