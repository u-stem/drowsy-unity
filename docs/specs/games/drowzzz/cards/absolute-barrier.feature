# language: ja
機能: カード No.19「絶対障壁」(Phase 2 完結後)

  「ゲーム開始時自動連想 marker」機構の初導入カード(ADR-0024)。No.17「見掛け倒しの障壁」の上位版で、
  Counter + Frenzy 両キーワード持ち(反撃カードでありながら反撃を受けない)。共通山札に含まれず、
  先行プレイヤーがゲーム開始時に自動連想で 1 枚だけ持つ特殊カード。
  既存 Counter / Frenzy 機構(ADR-0011 §4.3 / §4.5)+ 新規 ADR-0024 機構を consume する。

  @DZ-389
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog に Card "19" を登録する
    ならば カード名は "絶対障壁" であり
    かつ 効果列は 2 件:KeywordedEffect([Counter, Frenzy], AdjustSdpEffect(Self, 0)) + AssociateToFirstPlayerOnGameStartEffect()
    かつ HasKeywordInEffects(effects, Counter) が true である
    かつ HasKeywordInEffects(effects, Frenzy) が true である

  @DZ-390
  シナリオ: 開始時自動連想 — 先行プレイヤー Hand に追加
    前提 InMemoryCardCatalog に Card "19" を含む 2 枚以上の catalog
    かつ 配布に十分な initialDeck
    もし StartGameUseCase.Execute(players=[p1, p2], initialDeck) を実行する
    ならば 先行プレイヤー(shuffledPlayers[0])の Hand に CardId.Of("19", 0) が含まれる

  @DZ-391
  シナリオ: 開始時自動連想 — AssociatedCardIds に記録
    前提 InMemoryCardCatalog に Card "19" を含む catalog
    もし StartGameUseCase.Execute を実行する
    ならば session.AssociatedCardIds に CardId.Of("19", 0) が含まれる(ADR-0019 連想由来記録)

  @DZ-392
  シナリオ: marker なし catalog では自動連想されない
    前提 InMemoryCardCatalog が AssociateToFirstPlayerOnGameStartEffect を含むカードを 1 枚も登録していない
    もし StartGameUseCase.Execute を実行する
    ならば 先行プレイヤー Hand に CardId.Of("19", 0) は含まれない
    かつ session.AssociatedCardIds に CardId.Of("19", 0) は含まれない

  @DZ-393
  シナリオ: 後手プレイヤーは Card "19" を持たない
    前提 InMemoryCardCatalog に Card "19" を含む catalog
    もし StartGameUseCase.Execute を実行する
    ならば 後手プレイヤー(shuffledPlayers[1])の Hand に CardId.Of("19", 0) は含まれない(先行限定)

  @DZ-394
  シナリオ: Counter 経路 — 非 Frenzy Target に反撃で IsLegalMove が true
    前提 Field に非 Frenzy カード Card "X" があり PhaseState が WaitingForCounterResponse
    かつ p2 が Card "19" を手札に持つ
    もし p2 が CounterAction(Counter=Card "19", Target=Card "X") で IsLegalMove を確認する
    ならば 結果は true である(No.17 と共通の Counter 経路)

  @DZ-395
  シナリオ: Apply — Card "19" と Target が Discard へ移動
    前提 Field に非 Frenzy カード Card "X"、p2 が Card "19" を手札に持ち WaitingForCounterResponse
    もし p2 が CounterAction(Counter=Card "19", Target=Card "X") を適用する
    ならば p2 の Hand から Card "19" が Remove される
    かつ Field から Card "X" が Remove される
    かつ Discard に Card "19" と Card "X" の両方が含まれる

  @DZ-396
  シナリオ: Frenzy 経路 — Card "19" には反撃不可
    前提 Field に Card "19"(p1 がプレイした直後)、p2 が他の Counter カード(例 Card "17")を手札に持ち WaitingForCounterResponse
    もし p2 が CounterAction(Counter=Card "17", Target=Card "19") で IsLegalMove を確認する
    ならば 結果は false である(Frenzy 持ちは反撃不可、ADR-0011 §4.5)
