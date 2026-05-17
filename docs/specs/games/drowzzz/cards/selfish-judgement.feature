# language: ja
機能: カード No.16「自分勝手な審判」(Phase 2 完結後)

  条件分岐 effect の初導入(ConditionalApplyOrClearInfluencesEffect)。
  対象プレイヤーの保有 Influences 件数で「2 以下なら Apply、3 以上なら Clear」と分岐する戦術カード。

  @DZ-359
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog に Card "16" を登録する
    ならば カード名は "自分勝手な審判" であり
    かつ 効果列は ChoiceEffect(2 分岐)

  @DZ-360
  シナリオ: 選択1 — SDP 変動
    前提 p1 が WaitingForPlay、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=0) を適用する
    ならば p1 の SDP が -8 になる
    かつ p2 の SDP が +5 になる

  @DZ-361
  シナリオ: 選択1 + 甲影響 0 件 → Apply 経路(境界 0)
    前提 p1 が WaitingForPlay、p1 の Influences が 0 件、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=0) を適用する
    ならば p1 の Influences に PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, -4), Perpetual) が 1 件追加される

  @DZ-362
  シナリオ: 選択1 + 甲影響 2 件 → Apply 経路(境界 2)
    前提 p1 の Influences が 2 件、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=0) を適用する
    ならば p1 の Influences が 3 件に増える(既存 2 件 + 本カード影響 1 件)

  @DZ-363
  シナリオ: 選択1 + 甲影響 3 件 → Clear 経路(境界 3、本カード影響も付与されない)
    前提 p1 の Influences が 3 件、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=0) を適用する
    ならば p1 の Influences が空になる(Clear、本カード影響は Apply されない、排他)

  @DZ-364
  シナリオ: 選択2 — SDP 変動
    前提 p1 が WaitingForPlay、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=1) を適用する
    ならば p1 の SDP が +5 になる
    かつ p2 の SDP が -8 になる

  @DZ-365
  シナリオ: 選択2 + 乙影響 0 件 → 乙に Apply
    前提 p2 の Influences が 0 件、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=1) を適用する
    ならば p2 の Influences に PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self, -4), Perpetual) が 1 件追加される

  @DZ-366
  シナリオ: 選択2 + 乙影響 3 件 → 乙の影響を Clear
    前提 p2 の Influences が 3 件、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=1) を適用する
    ならば p2 の Influences が空になる(Clear、本カード影響なし)

  @DZ-367
  シナリオ: 他プレイヤー保護(選択1 では乙の影響は触らない)
    前提 p2 の Influences が 5 件、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=0) を適用する
    ならば p2 の Influences は 5 件のまま不変(Target=Self のため乙には触らない)

  @DZ-368
  シナリオ: 他プレイヤー保護(選択2 では甲の影響は触らない)
    前提 p1 の Influences が 5 件、p1 が Card "16" を手札に持つ
    もし p1 が PlayCardAction(Card="16", Choice=1) を適用する
    ならば p1 の Influences は 5 件のまま不変(Target=Opponent のため甲には触らない)
