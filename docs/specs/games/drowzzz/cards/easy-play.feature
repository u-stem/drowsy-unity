# language: ja
機能: カード No.10「安直過ぎる一手」(Phase 2 完結後、ADR-0021 と同 PR)

  御業(SDP -10 自己コスト)+ 乙へのベッド 30% 破損 + カウント 1 の「ドロー禁止」Marker を付与する戦術カード。
  No.09 と対をなす(攻撃手段封鎖 vs リソース供給封鎖)。
  カウント 1 「ドロー禁止」Marker 導入で顕在化した進行不能化(stuck)問題への対応として
  ADR-0021「EndTurnAction の全フェーズ合法化条件」を確定(本 PR 同梱)。

  @DZ-314
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog(本物デッキでは ScriptableObjectCardCatalog)に Card "10" を登録する
    ならば カード名は "安直過ぎる一手" であり
    かつ 効果列は 3 件:AdjustSdpEffect(Self, -10) + DamageBedEffect(Opponent, 30) + ApplyInfluenceEffect(Opponent, EasyPlayInfluence)

  @DZ-315
  シナリオ: プレイ時に自分の SDP が 10 減る
    前提 p1 が WaitingForPlay、p1 が Card "10" を手札に持つ
    もし p1 が PlayCardAction(Card="10") を適用する
    ならば p1 の SDP が -10 になる

  @DZ-316
  シナリオ: プレイ時に相手の BedDamage が 30 増える
    前提 p1 が WaitingForPlay、p1 が Card "10" を手札に持つ
    もし p1 が PlayCardAction(Card="10") を適用する
    ならば p2 の BedDamage が +30 になる

  @DZ-317
  シナリオ: プレイ時に相手の Influences に EasyPlayMarker が付与される
    前提 p1 が WaitingForPlay、p1 が Card "10" を手札に持つ
    もし p1 が PlayCardAction(Card="10") を適用する
    ならば p2 の Influences に PlayerInfluence(OwnPhaseStart, RestrictDrawCardInfluenceMarkerEffect, 1) が追加される

  @DZ-318
  シナリオ: 本 Marker 保有時、DrawCardAction が illegal
    前提 p2 が PlayerInfluence(OwnPhaseStart, RestrictDrawCardInfluenceMarkerEffect, 1) を 1 件保有
    かつ p2 が WaitingForDraw
    もし p2 が DrawCardAction で IsLegalMove を確認する
    ならば 結果は false である(山札からの手段ドロー禁止)

  @DZ-319
  シナリオ: 本 Marker 保有時、WaitingForDraw でも EndTurnAction が legal(ADR-0021 stuck 脱出弁)
    前提 p2 が 本 Marker(カウント 1)を 1 件保有
    かつ p2 が WaitingForDraw
    もし p2 が EndTurnAction で IsLegalMove を確認する
    ならば 結果は true である(stuck 化 Marker 保有時の全フェーズ合法化)

  @DZ-319
  シナリオ: 本 Marker 保有時、WaitingForPlay でも EndTurnAction が legal(ADR-0021 全フェーズ合法化)
    前提 p2 が 本 Marker(カウント 1)を 1 件保有
    かつ p2 が WaitingForPlay(No.10 単独では stuck ではないが ADR-0021 ホワイトリスト方式で合法化)
    もし p2 が EndTurnAction で IsLegalMove を確認する
    ならば 結果は true である(Marker 保有時は PhaseState 問わず合法、ADR-0021)

  @DZ-320
  シナリオ: 本 Marker 保有時でも他 4 アクション(PlayCard / Counter / Abandon / Associate)は通常判定
    前提 p2 が 本 Marker(カウント 1)を 1 件保有
    かつ p2 が WaitingForPlay、p2 の手札に任意の Card "X" を持つ
    もし p2 が PlayCardAction(Card="X") で IsLegalMove を確認する
    ならば 結果は通常通り(本 Marker は影響しない、No.09 とは別概念)

  @DZ-321
  シナリオ: カウント 1 Marker は p2 フェーズ全体で機能(ADR-0020:Tick は count 不変)
    前提 p2 が 本 Marker(カウント 1)を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の Influences 件数が 1 のまま残る(RemainingCount=1、p2 フェーズで IsLegalDraw walk が機能)

  @DZ-322
  シナリオ: p2 自身の EndTurn 冒頭で Marker が除去される(ADR-0020 の Decrement、stuck 脱出弁経由)
    前提 p2 が 本 Marker(カウント 1)を 1 件保有
    かつ p2 が WaitingForDraw(ADR-0021 で stuck 脱出弁として EndTurn 合法化)
    もし p2 が EndTurnAction を適用する
    ならば p2 の Influences 件数が 0 になる(EndTurn 冒頭 Decrement で 1→0 除去)

  @DZ-322
  シナリオ: p2 自身の EndTurn 冒頭で Marker が除去される(ADR-0020 の Decrement、通常経路)
    前提 p2 が 本 Marker(カウント 1)を 1 件保有
    かつ p2 が WaitingForEndTurn(通常の EndTurn 合法経路)
    もし p2 が EndTurnAction を適用する
    ならば p2 の Influences 件数が 0 になる(Decrement は PhaseState 非依存)
