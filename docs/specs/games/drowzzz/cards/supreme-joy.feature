# language: ja
機能: カード No.20「至上の喜び」(Phase 2 完結後)

  「カード固有放棄効果」機構の初導入カード(ADR-0025)。プレイ時 +20/-20 + Self 自爆 Marker、放棄時 +4/+6。
  既存 AbandonChoice(SDP+5 or Bed-20%)に加えてカード固有効果が累積発動する。

  @DZ-397
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog に Card "20" を登録する
    ならば カード名は "至上の喜び" であり
    かつ 効果列は 1 件最上位 PlayOrAbandonBranchEffect:
      PlayEffects: [AdjustSdp(Self,+20), AdjustSdp(Opp,-20), ApplyInfluence(Self, PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarker, 1))]
      AbandonEffects: [AdjustSdp(Self,+4), AdjustSdp(Opp,+6)]

  @DZ-398
  シナリオ: プレイ時 SDP +20/-20
    前提 p1 が WaitingForPlay、p1 が Card "20" を手札に持つ
    もし p1 が PlayCardAction(Card="20") を適用する
    ならば p1 の SDP が +20 増加する
    かつ p2 の SDP が -20 減少する

  @DZ-399
  シナリオ: プレイ時 Self に Restrict Marker 影響付与
    前提 p1 が WaitingForPlay、p1 が Card "20" を手札に持つ
    もし p1 が PlayCardAction(Card="20") を適用する
    ならば p1 の Influences に PlayerInfluence(OwnPhaseStart, RestrictAllUsageAndAbandonInfluenceMarker, RemainingCount=1) が 1 件追加される

  @DZ-400
  シナリオ: プレイ時 Hand から Field へ移動
    前提 p1 が WaitingForPlay、p1 が Card "20" を手札に持つ
    もし p1 が PlayCardAction(Card="20") を適用する
    ならば p1 の Hand から Card "20" が Remove される
    かつ Field 先頭に Card "20" が AddTop される

  @DZ-401
  シナリオ: 放棄 GainSdp — Self+5+4 / Opp+6
    前提 p1 が WaitingForPlay、p1 が Card "20" を手札に持つ
    もし p1 が AbandonAction(CardIndex=0, Choice=GainSdp) を適用する
    ならば p1 の SDP が +9 になる(AbandonChoice +5 + カード固有 +4)
    かつ p2 の SDP が +6 になる(カード固有 +6)

  @DZ-402
  シナリオ: 放棄 RepairBed — Bed -20% / Self+4 / Opp+6
    前提 p1 が WaitingForPlay、p1 が Card "20" を手札に持つ、p1 BedDamages=50%
    もし p1 が AbandonAction(CardIndex=0, Choice=RepairBed) を適用する
    ならば p1 の BedDamages が 30% になる(AbandonChoice -20)
    かつ p1 の SDP が +4 になる(カード固有 Self+4)
    かつ p2 の SDP が +6 になる(カード固有 Opp+6)

  @DZ-403
  シナリオ: 放棄 — Hand から Discard へ移動
    前提 p1 が WaitingForPlay、p1 が Card "20" を手札に持つ
    もし p1 が AbandonAction(CardIndex=0, Choice=GainSdp) を適用する
    ならば p1 の Hand から Card "20" が Remove される
    かつ Discard に Card "20" が含まれる

  @DZ-404
  シナリオ: 放棄では甲影響(Restrict Marker)は付与されない
    前提 p1 が WaitingForPlay、p1 が Card "20" を手札に持つ、p1 Influences 空
    もし p1 が AbandonAction(CardIndex=0, Choice=GainSdp) を適用する
    ならば p1 の Influences は空のまま(AbandonEffects には ApplyInfluenceEffect なし、ADR-0025 累積モデルの設計通り)
