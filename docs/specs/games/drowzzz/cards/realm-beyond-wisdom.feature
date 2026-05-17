# language: ja
@DZ-CARD-07
機能: カード No.07「知恵の及ばぬ領域」
  Frenzy(狂乱)キーワード + No.08「廻るための知恵」由来 Influence を 1 件消滅 +
  No.08 使用禁止 Influence(カウント 4)を相手に付与する対をなすカード。
  2026-05-17 で導入、`RemoveInvertBedDamageInfluenceEffect` 新設、`RestrictSpecificCardInfluenceEffect` (No.04 由来) 流用。

  背景:
    前提 InMemoryCardCatalog に Card "07"「知恵の及ばぬ領域」が 4 件の最上位 effect で登録されている
    かつ 効果列 = [AdjustSdpEffect(Self,-6), AdjustSdpEffect(Opponent,+5), RemoveInvertBedDamageInfluenceEffect(Opponent), KeywordedEffect([Frenzy], ApplyInfluenceEffect(Opponent, PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(CardTypeId.Of("08")), 4)))]
    かつ N=2(p1, p2)

  @DZ-287 @DZ-288 @DZ-291
  シナリオ: Card 07 をプレイ(SDP 変動 + Restrict 影響付与)
    前提 p1 が current で WaitingForPlay、p1 が Card "07" を手札に持つ
    もし p1 が PlayCardAction(Card="07") を適用する
    ならば p1 の SDP が -6 になる
    かつ p2 の SDP が +5 になる
    かつ p2 の Influences に PlayerInfluence(OwnPhaseStart, RestrictSpecificCardInfluenceEffect(CardTypeId.Of("08")), 4) が 1 件追加される

  @DZ-289
  シナリオ: 相手が InvertBedDamage 影響保有時 → 1 件削除
    前提 p2 が PlayerInfluence(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffect, Perpetual) を 2 件保有
    かつ p1 が current で WaitingForPlay、p1 が Card "07" を手札に持つ
    もし p1 が PlayCardAction(Card="07") を適用する
    ならば p2 の InvertBedDamage 影響件数が 2 → 1 に減る(1 件削除)
    かつ p2 の Influences には別途 RestrictCard08 影響が 1 件追加される

  @DZ-290
  シナリオ: 相手が InvertBedDamage 影響非保有時 → graceful no-op
    前提 p2 が InvertBedDamage 影響を保有しない
    かつ p1 が current で WaitingForPlay、p1 が Card "07" を手札に持つ
    もし p1 が PlayCardAction(Card="07") を適用する
    ならば p2 の Influences には RestrictCard08 影響のみが 1 件追加される(InvertBedDamage 削除は no-op)

  @DZ-292
  シナリオ: Frenzy 持ち Card 07 は CounterAction で illegal
    前提 p1 が Card "07" を手札に持ち、PlayCardAction(Card="07") をプレイした
    かつ p2 が CounterAction で Card "07" を target に指定
    もし p2 が CounterAction(target=Card "07") で IsLegalMove を確認する
    ならば 結果は false である(Frenzy は反撃を受けない、ADR-0011 §4.5、DZ-282 同パターン)
