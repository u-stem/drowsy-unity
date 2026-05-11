# language: ja
機能: DrawCardEffect (M2-PR3)

  @DZ-115
  シナリオ: Target=Self Count=1 で現プレイヤーが 1 枚引く (正常系・Small)
    前提 山札に [c1, c2, c3] (top: c1) の DrowZzzGameSession (現プレイヤー p1、手札空)
    もし EffectInterpreter.Apply(session, DrawCardEffect(Self, 1)) を呼ぶ
    ならば p1 の手札に c1 が含まれ、山札は [c2, c3] になる

  @DZ-116
  シナリオ: Target=Self Count=3 で現プレイヤーが 3 枚引く (正常系・Small)
    前提 山札に [c1, c2, c3, c4, c5] (top: c1) の DrowZzzGameSession (現プレイヤー p1、手札空)
    もし EffectInterpreter.Apply(session, DrawCardEffect(Self, 3)) を呼ぶ
    ならば p1 の手札に c1, c2, c3 が含まれ、山札は [c4, c5] になる

  @DZ-117
  シナリオ: 山札が Count より少ない時の graceful degradation (異常系・Small)
    前提 山札に [c1, c2] (2 枚) の DrowZzzGameSession (現プレイヤー p1、手札空)
    もし EffectInterpreter.Apply(session, DrawCardEffect(Self, 3)) を呼ぶ
    ならば p1 の手札に c1, c2 が含まれ、山札は空、例外は発生しない

  @DZ-118
  シナリオ: Target=Opponent を本 PR では NotImplementedException で防御 (異常系・Small)
    前提 DrowZzzGameSession (現プレイヤー p1、手札空)
    もし EffectInterpreter.Apply(session, DrawCardEffect(Opponent, 1)) を呼ぶ
    ならば NotImplementedException が発生する (M2-PR3 範囲では Opponent ドローカードは未登場)
