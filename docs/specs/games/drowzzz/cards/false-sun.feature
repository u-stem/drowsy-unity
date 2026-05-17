# language: ja
機能: カード No.12「偽りの太陽」(Phase 2 完結後、ADR-0022 と同 PR)

  Reactive Influence(アクション後発動型)の初導入。夜に使うと永続的に「使用したら SDP-10 / 放棄したら SDP+5」の影響を自分に背負う戦術カード。
  朝に使うと即時 SDP -4 / +18 のみ(影響付与なし)。

  @DZ-334
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog(本物デッキでは ScriptableObjectCardCatalog)に Card "12" を登録する
    ならば カード名は "偽りの太陽" であり
    かつ 効果列は 1 件:TimeOfDayBranchEffect(夜 4 件 / 朝 2 件)

  @DZ-335
  シナリオ: 夜 — 自分の SDP が 4 減る
    前提 p1 が WaitingForPlay、夜(turnNumber=1)、p1 が Card "12" を手札に持つ
    もし p1 が PlayCardAction(Card="12") を適用する
    ならば p1 の SDP が -4 になる

  @DZ-336
  シナリオ: 夜 — 相手の SDP が 6 増える
    前提 p1 が WaitingForPlay、夜、p1 が Card "12" を手札に持つ
    もし p1 が PlayCardAction(Card="12") を適用する
    ならば p2 の SDP が +6 になる

  @DZ-337
  シナリオ: 夜 — 自分の Influences に 2 件追加される
    前提 p1 が WaitingForPlay、夜、p1 が Card "12" を手札に持つ
    もし p1 が PlayCardAction(Card="12") を適用する
    ならば p1 の Influences に 2 件追加される
    かつ 1 件目は PlayerInfluence(OnOwnPlayCardAfter, AdjustSdpAfterPlayCardEffect(-10), Perpetual)
    かつ 2 件目は PlayerInfluence(OnOwnAbandonAfter, AdjustSdpAfterAbandonEffect(+5), Perpetual)

  @DZ-338
  シナリオ: 朝 — 自分の SDP が 4 減る
    前提 p1 が WaitingForPlay、朝(turnNumber=33)、p1 が Card "12" を手札に持つ
    もし p1 が PlayCardAction(Card="12") を適用する
    ならば p1 の SDP が -4 になる

  @DZ-339
  シナリオ: 朝 — 相手の SDP が 18 増える
    前提 p1 が WaitingForPlay、朝、p1 が Card "12" を手札に持つ
    もし p1 が PlayCardAction(Card="12") を適用する
    ならば p2 の SDP が +18 になる

  @DZ-340
  シナリオ: 朝 — 影響付与なし
    前提 p1 が WaitingForPlay、朝、p1 が Card "12" を手札に持つ
    もし p1 が PlayCardAction(Card="12") を適用する
    ならば p1 の Influences は不変(空のまま)

  @DZ-341
  シナリオ: Reactive — 本 PlayCard Influence 保有時、他カード使用で SDP-10
    前提 p1 が PlayerInfluence(OnOwnPlayCardAfter, AdjustSdpAfterPlayCardEffect(-10), Perpetual) を 1 件保有
    かつ p1 が WaitingForPlay、p1 が任意の Card "X" を手札に持つ(効果なしダミー)
    もし p1 が PlayCardAction(Card="X") を適用する
    ならば p1 の SDP が -10 になる

  @DZ-342
  シナリオ: Reactive — 本 Abandon Influence 保有時、AbandonAction(GainSdp)で SDP+10(合算)
    前提 p1 が PlayerInfluence(OnOwnAbandonAfter, AdjustSdpAfterAbandonEffect(+5), Perpetual) を 1 件保有
    かつ p1 が WaitingForPlay、p1 が任意のカードを手札に持つ
    もし p1 が AbandonAction(CardIndex=0, Choice=GainSdp) を適用する
    ならば p1 の SDP が +10(GainSdp +5 + Reactive +5 の合算)になる

  @DZ-342
  シナリオ: Reactive — 本 Abandon Influence 保有時、AbandonAction(RepairBed)で SDP+5(Reactive のみ)
    前提 p1 が PlayerInfluence(OnOwnAbandonAfter, AdjustSdpAfterAbandonEffect(+5), Perpetual) を 1 件保有
    かつ p1 の BedDamages が 30%(RepairBed の合法条件を満たす)
    かつ p1 が WaitingForPlay、p1 が任意のカードを手札に持つ
    もし p1 が AbandonAction(CardIndex=0, Choice=RepairBed) を適用する
    ならば p1 の SDP が +5(Reactive のみ、RepairBed は SDP に作用しない)になる
    かつ BedDamages が修繕分減る(別シナリオで詳細検証)

  @DZ-343
  シナリオ: snapshot ベース walk — 本カードプレイ自体には Reactive が適用されない
    前提 p1 が WaitingForPlay、夜、p1 が Card "12" を手札に持つ(Reactive 影響は未保有)
    もし p1 が PlayCardAction(Card="12") を適用する
    ならば p1 の SDP は -4(本カード即時効果のみ、Reactive SDP-10 は適用されない)

  @DZ-344
  シナリオ: 他プレイヤー保護 — Reactive は保有者のアクションでのみ発動
    前提 p2 が PlayerInfluence(OnOwnPlayCardAfter, AdjustSdpAfterPlayCardEffect(-10), Perpetual) を 1 件保有
    かつ p1 が current、WaitingForPlay、p1 が任意のカードを手札に持つ
    もし p1 が PlayCardAction を適用する
    ならば p2 の SDP は不変(p1 のアクションでは p2 の Reactive は発動しない)
