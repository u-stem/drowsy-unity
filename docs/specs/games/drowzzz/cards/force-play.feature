# language: ja
機能: カード No.09「強引過ぎる一手」(Phase 2 完結後、ADR-0020 と同 PR)

  御業(高火力 SDP 変動)+ カウント 1 の使用・放棄禁止 Marker 影響を相手に付与する戦術カード。
  カウント 1 Marker 機能化を動機として ADR-0020「Influence の RemainingCount 減算タイミングを EndTurn 冒頭へ移行」を確定した。

  @DZ-303
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog(本物デッキでは ScriptableObjectCardCatalog)に Card "09" を登録する
    ならば カード名は "強引過ぎる一手" であり
    かつ 効果列は 3 件:AdjustSdpEffect(Self, -10) + AdjustSdpEffect(Opponent, +10) + ApplyInfluenceEffect(Opponent, ForcePlayInfluence)

  @DZ-304
  シナリオ: プレイ時に自分の SDP が 10 減る
    前提 p1 が WaitingForPlay、p1 が Card "09" を手札に持つ
    もし p1 が PlayCardAction(Card="09") を適用する
    ならば p1 の SDP が -10 になる

  @DZ-305
  シナリオ: プレイ時に相手の SDP が 10 増える
    前提 p1 が WaitingForPlay、p1 が Card "09" を手札に持つ
    もし p1 が PlayCardAction(Card="09") を適用する
    ならば p2 の SDP が +10 になる

  @DZ-306
  シナリオ: プレイ時に相手の Influences に ForcePlayMarker が付与される
    前提 p1 が WaitingForPlay、p1 が Card "09" を手札に持つ
    もし p1 が PlayCardAction(Card="09") を適用する
    ならば p2 の Influences に PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffect, 1) が追加される

  @DZ-307
  シナリオ: 本 Marker 保有時、PlayCardAction が illegal
    前提 p2 が PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarkerEffect, 1) を 1 件保有
    かつ p2 が WaitingForPlay
    もし p2 が任意の PlayCardAction で IsLegalMove を確認する
    ならば 結果は false である(CardTypeId 非依存、すべての PlayCardAction が illegal)

  @DZ-308
  シナリオ: 本 Marker 保有時、AbandonAction が illegal
    前提 p2 が 本 Marker を 1 件保有
    かつ p2 が WaitingForPlay
    もし p2 が AbandonAction(choice=任意) で IsLegalMove を確認する
    ならば 結果は false である(放棄不可)

  @DZ-309
  シナリオ: 本 Marker 保有時、CounterAction(経路 1:相手フェーズの反撃)が illegal
    前提 p2 が 本 Marker を 1 件保有
    かつ p1 が WaitingForCounterResponse(p1 がプレイしたカードに対して p2 が反撃するフェーズ)
    もし p2 が CounterAction で IsLegalMove を確認する
    ならば 結果は false である(オーナー JIT 確定 2026-05-17:「使用」に CounterAction も含む)

  @DZ-309
  シナリオ: 本 Marker 保有時、CounterAction(経路 2:自フェーズの反撃の反撃)が illegal
    前提 p2 が 本 Marker を 1 件保有
    かつ p2 が WaitingForEndTurn(p2 自フェーズ中、Pending あり)
    もし p2 が CounterAction(経路 2)で IsLegalMove を確認する
    ならば 結果は false である(両経路で禁止)

  @DZ-310
  シナリオ: 本 Marker 保有時でも AssociateAction は許可
    前提 p2 が 本 Marker を 1 件保有
    かつ p2 が自フェーズ中(WaitingForDraw / WaitingForPlay / WaitingForEndTurn のいずれか)
    かつ p2 の TotalPoints >= AssociationThreshold で、対象カードに AssociatableMarkerEffect がある
    もし p2 が AssociateAction(card=連想可能カード) で IsLegalMove を確認する
    ならば 結果は true である(連想は明示禁止対象外、テキスト「使用や放棄」のみ)

  @DZ-311
  シナリオ: 本 Marker 保有時でも EndTurnAction は許可(進行不能化回避)
    前提 p2 が 本 Marker を 1 件保有
    かつ p2 が WaitingForEndTurn
    もし p2 が EndTurnAction で IsLegalMove を確認する
    ならば 結果は true である(進行不能化回避)

  @DZ-312
  シナリオ: カウント 1 Marker は p2 フェーズ全体で機能(ADR-0020:Tick は count 不変)
    前提 p2 が 本 Marker(カウント 1)を 1 件保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の Influences 件数が 1 のまま残る(RemainingCount=1、p2 フェーズで IsLegalPlayCard / IsLegalCounter / IsLegalAbandon の walk が機能)

  @DZ-313
  シナリオ: p2 自身の EndTurn 冒頭で Marker が除去される(ADR-0020 の Decrement)
    前提 p2 が 本 Marker(カウント 1)を 1 件保有
    かつ p2 が WaitingForEndTurn
    もし p2 が EndTurnAction を適用する
    ならば p2 の Influences 件数が 0 になる(EndTurn 冒頭 Decrement で 1→0 除去)
