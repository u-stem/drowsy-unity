# language: ja
@DZ-CARD-06
機能: カード No.06「牙の届かぬ領域」
  Frenzy(狂乱)キーワード + ベッド破損 SDP 変動 2 倍化 Influence を相手に付与する戦術カード。
  時間帯非依存の即時効果 + 継続影響(カウント 4)で長期的にベッド破損ダメージを増幅する設計。
  2026-05-17 で導入、`DoubleBedDamageSdpInfluenceMarkerEffect` 新設。

  背景:
    前提 InMemoryCardCatalog に Card "06"「牙の届かぬ領域」が 3 件の最上位 effect で登録されている
    かつ 効果列 = [AdjustSdpEffect(Self,-12), AdjustSdpEffect(Opponent,-4), KeywordedEffect([Frenzy], ApplyInfluenceEffect(Opponent, PlayerInfluence(OwnPhaseStart, DoubleBedDamageSdpInfluenceMarkerEffect, 4)))]
    かつ N=2(p1, p2)

  @DZ-279 @DZ-280 @DZ-281
  シナリオ: Card 06 をプレイ(時間帯非依存)
    前提 p1 が current で WaitingForPlay、p1 が Card "06" を手札に持つ
    もし p1 が PlayCardAction(Card="06") を適用する
    ならば p1 の SDP が -12 になる
    かつ p2 の SDP が -4 になる
    かつ p2 の Influences に PlayerInfluence(OwnPhaseStart, DoubleBedDamageSdpInfluenceMarkerEffect, 4) が 1 件追加される

  @DZ-282
  シナリオ: Frenzy 持ち Card 06 は CounterAction で illegal
    前提 p1 が Card "06" を手札に持ち、PlayCardAction(Card="06") をプレイした
    かつ p2 が CounterAction で Card "06" を target に指定
    もし p2 が CounterAction(target=Card "06") で IsLegalMove を確認する
    ならば 結果は false である(Frenzy は反撃を受けない、ADR-0011 §4.5)

  @DZ-283
  シナリオ: 2 倍化 Influence 保有時のベッド破損 Tick(ADR-0020:count 不変)
    前提 p2 が PlayerInfluence(OwnPhaseStart, DoubleBedDamageSdpInfluenceMarkerEffect, 4) を 1 件保有
    かつ BedDamages[p2] = 40%
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 が新しい current player になる
    かつ p2 の SDP が -16 になる(通常 40/5=8 → 2 倍化で 16)
    かつ p2 の影響の RemainingCount は 4 のまま(Tick は count 不変、p2 自身の EndTurn で -1)

  @DZ-284
  シナリオ: 2 倍化 Influence 非保有時の通常ベッド破損 Tick(非リグレッション)
    前提 p2 が DoubleBedDamageSdpInfluenceMarkerEffect の Influence を保有しない
    かつ BedDamages[p2] = 40%
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 が新しい current player になる
    かつ p2 の SDP が -8 になる(通常計算経路、2 倍化なし)

  @DZ-285
  シナリオ: カウント 1 Marker は p2 フェーズ全体で機能、p2 EndTurn で除去(ADR-0020)
    前提 p2 が PlayerInfluence(OwnPhaseStart, DoubleBedDamageSdpInfluenceMarkerEffect, 1) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の Influences 件数が 1 のまま残る(RemainingCount=1、除去は p2 自身の EndTurn まで遅延)
