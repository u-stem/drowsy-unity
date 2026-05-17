# language: ja
@DZ-CARD-03
機能: カード No.03「身体にいいもの」
  時間帯分岐 + 継続影響(永続)付与のハイブリッドカード。
  No.01「コップ一杯の脅威」(時間帯分岐)+ No.02「緑の侵攻」(継続影響付与)の組み合わせに
  加え、永続影響(InfluenceConstants.Perpetual = int.MaxValue)を初導入する。
  ADR-0007 §1.5「継続影響」、Phase 2 完結後の初の新規カード追加(2026-05-17)。

  背景:
    前提 InMemoryCardCatalog に Card "03"「身体にいいもの」が TimeOfDayBranchEffect 1 件で登録されている
    かつ 「身体にいいもの」の夜分岐は [AdjustSdpEffect(Self,-20), AdjustSdpEffect(Opponent,+5), ApplyInfluenceEffect(Self, PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,+4), Perpetual))]
    かつ 「身体にいいもの」の朝分岐は [AdjustSdpEffect(Self,-10), AdjustSdpEffect(Opponent,+5), ApplyInfluenceEffect(Self, PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,-6), Perpetual))]
    かつ N=2(p1, p2)

  @DZ-247 @DZ-248 @DZ-249
  シナリオ: 夜フェーズで Card 03 をプレイ
    前提 Clock.IsNight が true(ターン 1〜16)
    かつ p1 が current で WaitingForPlay、p1 が Card "03" を手札に持つ
    もし p1 が PlayCardAction(Card="03") を適用する
    ならば p1 の SDP が -20 になる
    かつ p2 の SDP が +5 になる
    かつ p1 の Influences に PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,+4), Perpetual) が 1 件追加される

  @DZ-250 @DZ-251 @DZ-252
  シナリオ: 朝フェーズで Card 03 をプレイ
    前提 Clock.IsMorning が true(ターン 17〜21)
    かつ p1 が current で WaitingForPlay、p1 が Card "03" を手札に持つ
    もし p1 が PlayCardAction(Card="03") を適用する
    ならば p1 の SDP が -10 になる
    かつ p2 の SDP が +5 になる
    かつ p1 の Influences に PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,-6), Perpetual) が 1 件追加される

  @DZ-253
  シナリオ: 影響 x(SDP+4 永続)が Tick で発動
    前提 p2 が PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,+4), Perpetual) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 が新しい current player になる
    かつ p2 の SDP が +4 になる
    かつ p2 の影響の RemainingCount が Perpetual - 1(= int.MaxValue - 1) になる

  @DZ-254
  シナリオ: 影響 y(SDP-6 永続)が Tick で発動
    前提 p2 が PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,-6), Perpetual) を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 が新しい current player になる
    かつ p2 の SDP が -6 になる
    かつ p2 の影響の RemainingCount が Perpetual - 1 になる
