# language: ja
機能: カード No.17「見掛け倒しの障壁」(Phase 2 完結後)

  既存 Counter キーワード機構を使う初の本物の反撃カード。SDP 変動なし、Counter キーワード付与のみの最小カード。
  既存 ADR-0011 §4.3 / §4.4 / §4.5 の動作をそのまま consumer する。

  @DZ-369
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog に Card "17" を登録する
    ならば カード名は "見掛け倒しの障壁" であり
    かつ 効果列は 1 件:KeywordedEffect([Counter], AdjustSdpEffect(Self, 0))
    かつ HasKeywordInEffects(effects, Counter) が true である

  @DZ-370
  シナリオ: 通常 PlayCardAction で SDP 変動なし
    前提 p1 が WaitingForPlay、p1 が Card "17" を手札に持つ
    もし p1 が PlayCardAction(Card="17") を適用する
    ならば p1 の SDP が 0 のまま
    かつ p2 の SDP が 0 のまま

  @DZ-371
  シナリオ: 通常 PlayCardAction で Hand から Field へ移動
    前提 p1 が WaitingForPlay、p1 が Card "17" を手札に持つ
    もし p1 が PlayCardAction(Card="17") を適用する
    ならば p1 の Hand から Card "17" が Remove される
    かつ Field 先頭に Card "17" が AddTop される(後に Counter で打ち消し可能、ただし本テストでは確認まで)

  @DZ-372
  シナリオ: Counter 経路 1 — 非 Frenzy Target に反撃で IsLegalMove が true
    前提 Field に非 Frenzy カード(例 Card "01")があり PhaseState が WaitingForCounterResponse
    かつ p2(counterPlayerIndex)が Card "17" を手札に持つ
    もし p2 が CounterAction(Counter=Card "17", Target=Field[0]) で IsLegalMove を確認する
    ならば 結果は true である

  @DZ-373
  シナリオ: Counter 経路 1 — Frenzy Target には反撃不可
    前提 Field に Frenzy 持ちカード(例 Card "06")があり PhaseState が WaitingForCounterResponse
    かつ p2 が Card "17" を手札に持つ
    もし p2 が CounterAction(Counter=Card "17", Target=Field[0]) で IsLegalMove を確認する
    ならば 結果は false である(Frenzy 持ちは反撃不可、ADR-0011 §4.5)

  @DZ-374
  シナリオ: Apply — Card "17" と Target が Discard へ移動
    前提 Field に非 Frenzy カード Card "X"、p2 が Card "17" を手札に持ち WaitingForCounterResponse
    もし p2 が CounterAction(Counter=Card "17", Target=Card "X") を適用する
    ならば p2 の Hand から Card "17" が Remove される
    かつ Field から Card "X" が Remove される
    かつ Discard に Card "17" と Card "X" の両方が含まれる

  @DZ-375
  シナリオ: Apply — PendingCounteredEffects に 1 件追加(反撃の反撃のための記録)
    前提 Field に非 Frenzy カード Card "X"、p2 が Card "17" を手札に持ち WaitingForCounterResponse
    もし p2 が CounterAction(Counter=Card "17", Target=Card "X") を適用する
    ならば PendingCounteredEffects に (CounterCard=Card "17", OriginalCard=Card "X", OriginalEffects=Card "X" の効果列) が 1 件追加される

  @DZ-376
  シナリオ: Apply — PhaseState が WaitingForEndTurn に戻る
    前提 Field に非 Frenzy カード Card "X"、p2 が Card "17" を手札に持ち WaitingForCounterResponse
    もし p2 が CounterAction(Counter=Card "17", Target=Card "X") を適用する
    ならば PhaseState が WaitingForEndTurn になる(元プレイヤーのターン進行に戻る)
