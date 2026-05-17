# language: ja
機能: カード No.11「機械仕掛けの冬将軍」(Phase 2 完結後)

  狂乱(Frenzy)持ち + 即時 SDP 変動(自分 -4 / 相手 -8)+ 乙に永続「自フェーズ開始時に SDP-n(n = 乙の Hand.Count)」を付与する戦術カード。
  動的計算 TickEffect の初導入(`AdjustSdpByHandCountEffect`)。

  @DZ-323
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog(本物デッキでは ScriptableObjectCardCatalog)に Card "11" を登録する
    ならば カード名は "機械仕掛けの冬将軍" であり
    かつ 効果列は 3 件:AdjustSdpEffect(Self, -4) + AdjustSdpEffect(Opponent, -8) + KeywordedEffect([Frenzy], ApplyInfluenceEffect(Opponent, WinterGeneralInfluence))

  @DZ-324
  シナリオ: プレイ時に自分の SDP が 4 減る
    前提 p1 が WaitingForPlay、p1 が Card "11" を手札に持つ
    もし p1 が PlayCardAction(Card="11") を適用する
    ならば p1 の SDP が -4 になる

  @DZ-325
  シナリオ: プレイ時に相手の SDP が 8 減る
    前提 p1 が WaitingForPlay、p1 が Card "11" を手札に持つ
    もし p1 が PlayCardAction(Card="11") を適用する
    ならば p2 の SDP が -8 になる

  @DZ-326
  シナリオ: プレイ時に相手の Influences に WinterGeneral が付与される
    前提 p1 が WaitingForPlay、p1 が Card "11" を手札に持つ
    もし p1 が PlayCardAction(Card="11") を適用する
    ならば p2 の Influences に PlayerInfluence(OwnPhaseStart, AdjustSdpByHandCountEffect, InfluenceConstants.Perpetual) が追加される

  @DZ-327
  シナリオ: Frenzy で反撃を受けない
    前提 Field に Card "11"、p2 が WaitingForCounterResponse
    かつ p2 が Counter キーワード持ちカードを手札に保持
    もし p2 が CounterAction(target=Card "11") で IsLegalMove を確認する
    ならば 結果は false である(Frenzy は反撃を受けない、ADR-0011 §4.5)

  @DZ-328
  シナリオ: 動的計算 — Hand.Count=3 で SDP-3
    前提 p2 が PlayerInfluence(OwnPhaseStart, AdjustSdpByHandCountEffect, Perpetual) を 1 件保有
    かつ p2 の Hand に 3 枚のカード
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 が新しい current player になる
    かつ p2 の SDP が -3 になる(Hand.Count=3 → 動的 SDP-3)

  @DZ-329
  シナリオ: graceful no-op — Hand.Count=0 で SDP 不変
    前提 p2 が本 Influence を 1 件保有
    かつ p2 の Hand が空(Hand.Count=0)
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の SDP は不変(Hand.Count=0 → SDP-0 = no-op)

  @DZ-330
  シナリオ: ADR-0020 — Tick は count 不変、Perpetual は実質除去されない
    前提 p2 が本 Influence(Perpetual)を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の Influence の RemainingCount は Perpetual のまま(Tick で count 不変、ADR-0020)

  @DZ-331
  シナリオ: 動的計算 — Hand.Count=1 最小非ゼロ境界
    前提 p2 が本 Influence を 1 件保有
    かつ p2 の Hand に 1 枚のカード
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の SDP が -1 になる

  @DZ-332
  シナリオ: 他プレイヤー(p1)の SDP は変更されない
    前提 p2 が本 Influence を 1 件保有
    かつ p2 の Hand に 3 枚のカード
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p1 の SDP は初期値 0 のまま(他プレイヤー保護、ガード回帰防御)

  @DZ-333
  シナリオ: 動的計算 — Hand.Count=5 大き目境界
    前提 p2 が本 Influence を 1 件保有
    かつ p2 の Hand に 5 枚のカード
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の SDP が -5 になる(foreach 累積バグなし)
