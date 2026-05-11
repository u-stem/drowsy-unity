# language: ja
機能: AdjustSdpEffect (M2-PR3)

  @DZ-111
  シナリオ: Target=Self で現プレイヤーの SDP を +10 (正常系・Small)
    前提 現プレイヤーが p1 で SDP[p1]=0 の DrowZzzGameSession
    もし EffectInterpreter.Apply(session, AdjustSdpEffect(Self, 10)) を呼ぶ
    ならば 返却された session の SDP[p1] = 10、SDP[p2] は変化なし

  @DZ-112
  シナリオ: Target=Opponent で相手の SDP を +10 (正常系・Small、N=2)
    前提 現プレイヤーが p1 で SDP[p1]=0, SDP[p2]=0 の DrowZzzGameSession
    もし EffectInterpreter.Apply(session, AdjustSdpEffect(Opponent, 10)) を呼ぶ
    ならば 返却された session の SDP[p2] = 10、SDP[p1] は変化なし

  @DZ-113
  シナリオ: 負 Delta で SDP が負値になる (準正常系・Small、0 floor なし)
    前提 SDP[p1]=0 の DrowZzzGameSession (現プレイヤー p1)
    もし EffectInterpreter.Apply(session, AdjustSdpEffect(Self, -5)) を呼ぶ
    ならば 返却された session の SDP[p1] = -5 (DZ-109 と整合、負値許容)
