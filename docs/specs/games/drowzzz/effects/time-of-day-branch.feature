# language: ja
機能: TimeOfDayBranchEffect (M2-PR3)

  @DZ-120
  シナリオ: 夜の Clock で NightEffects が評価される (正常系・Small)
    前提 Clock.RoundNumber=1 (夜) の DrowZzzGameSession (現プレイヤー p1、SDP[p1]=0)
    かつ TimeOfDayBranchEffect(NightEffects: [AdjustSdpEffect(Self, +10)], MorningEffects: [])
    もし EffectInterpreter.Apply(session, effect) を呼ぶ
    ならば 返却された session の SDP[p1] = 10 (NightEffects が評価される)

  @DZ-121
  シナリオ: 朝の Clock で MorningEffects が評価される (正常系・Small)
    前提 Clock.RoundNumber=17 (朝) の DrowZzzGameSession (現プレイヤー p1、SDP[p1]=0)
    かつ TimeOfDayBranchEffect(NightEffects: [], MorningEffects: [AdjustSdpEffect(Self, +10)])
    もし EffectInterpreter.Apply(session, effect) を呼ぶ
    ならば 返却された session の SDP[p1] = 10 (MorningEffects が評価される)

  @DZ-122
  シナリオ: 夜・朝でない時刻 (Round 22) では no-op (準正常系・Small)
    前提 Clock.RoundNumber=22 (過渡的範囲、IsNight=IsMorning=false、ADR-0008 §5) の DrowZzzGameSession
    かつ TimeOfDayBranchEffect(NightEffects: [AdjustSdpEffect(Self, +10)], MorningEffects: [AdjustSdpEffect(Self, +10)])
    もし EffectInterpreter.Apply(session, effect) を呼ぶ
    ならば 返却された session は入力と変化なし (どちらの効果列も評価されない)

  @DZ-123
  シナリオ: NightEffects に null を渡して生成 (異常系・Small)
    前提 NightEffects に null
    もし TimeOfDayBranchEffect を生成する
    ならば ArgumentNullException が発生する

  @DZ-124
  シナリオ: MorningEffects に null を渡して生成 (異常系・Small)
    前提 MorningEffects に null
    もし TimeOfDayBranchEffect を生成する
    ならば ArgumentNullException が発生する
