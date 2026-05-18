# language: ja
機能: カード No.18「対抗手段」(Phase 2 完結後)

  Echo キーワード機構の初導入カード。受けている影響から 1 つを選び、その発生源カードを再使用 + 選択影響は除去。
  「反撃」はカードテキスト上の特殊効果分類(フレーバー)で、機構上の Counter キーワード(M3-PR5)とは独立。
  ADR-0023 で導入された `PlayerInfluence.OriginEffects` 動的注入 + `ApplyEchoReuse` ヘルパーで実装。

  @DZ-377
  シナリオ: catalog 登録(Ubiquitous)
    前提 InMemoryCardCatalog に Card "18" を登録する
    ならば カード名は "対抗手段" であり
    かつ 効果列は 1 件:KeywordedEffect([Echo], ReuseInfluenceSourceEffect)
    かつ HasKeywordInEffects(effects, Echo) が true である

  @DZ-378
  シナリオ: Influences 空時は illegal
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持ち、p1 の Influences が空
    もし p1 が PlayCardAction(Card="18", Choice=0) で IsLegalMove を確認する
    ならば 結果は false である(無対象なら使えない、ADR-0023 §6)

  @DZ-379
  シナリオ: Influences 1 件 + Choice 0 で合法
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持ち、p1 が Influences 1 件保有
    もし p1 が PlayCardAction(Card="18", Choice=0) で IsLegalMove を確認する
    ならば 結果は true である

  @DZ-380
  シナリオ: Influences 1 件 + Choice 1 は範囲外で illegal
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持ち、p1 が Influences 1 件保有
    もし p1 が PlayCardAction(Card="18", Choice=1) で IsLegalMove を確認する
    ならば 結果は false である(Choice 範囲外、ADR-0023 §6)

  @DZ-381
  シナリオ: Self 起点で SDP +5(自分の影響を Reuse)
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持つ
    かつ p1 が Influence(Trigger=OwnPhaseStart, TickEffect=任意, OriginEffects=[AdjustSdpEffect(Self, +5)]) を保有
    もし p1 が PlayCardAction(Card="18", Choice=0) を適用する
    ならば p1 の SDP が +5 される(Self 起点で本カード使用者に再発動)
    かつ p2 の SDP が 変動なし

  @DZ-382
  シナリオ: Self 起点で Opponent SDP -3(相手の影響を逆利用)
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持つ
    かつ p1 が Influence(OriginEffects=[AdjustSdpEffect(Opponent, -3)]) を保有
    もし p1 が PlayCardAction(Card="18", Choice=0) を適用する
    ならば p1 の SDP が 変動なし
    かつ p2 の SDP が -3 される(Self 起点で Opponent = p2)

  @DZ-383
  シナリオ: 選択 Influence は除去(consume)
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持ち、p1 が Influences 3 件保有
    もし p1 が PlayCardAction(Card="18", Choice=1) を適用する
    ならば p1 の Influences.Count が 2 になる(選択 index の影響を除去、ADR-0023 §1)

  @DZ-384
  シナリオ: OriginEffects 空 list は no-op + 除去
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持つ
    かつ p1 が Influence(OriginEffects=[]) を保有
    もし p1 が PlayCardAction(Card="18", Choice=0) を適用する
    ならば p1 / p2 の SDP / Bed / Outcome に副作用なし
    かつ p1 の Influences.Count が 0 になる(空 list でも Influence は除去)

  @DZ-385
  シナリオ: Hand から Field へ移動
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持ち、p1 が Influences 1 件保有
    もし p1 が PlayCardAction(Card="18", Choice=0) を適用する
    ならば p1 の Hand から Card "18" が Remove される
    かつ Field 先頭に Card "18" が AddTop される

  @DZ-386
  シナリオ: Reuse 中の新規 Influence の OriginEffects は空 list(連鎖防止)
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持つ
    かつ p1 が Influence(OriginEffects=[ApplyInfluenceEffect(Self, newInf)]) を保有
    もし p1 が PlayCardAction(Card="18", Choice=0) を適用する
    ならば p1 の Influences に追加された新規 Influence の OriginEffects は空 list である(連鎖 Reuse 防止、ADR-0023 §5)

  @DZ-387
  シナリオ: Reuse 中の ReuseInfluenceSourceEffect 自身は no-op(再帰防止)
    前提 p1 が WaitingForPlay、p1 が Card "18" を手札に持つ
    かつ p1 が Influence(OriginEffects=[ReuseInfluenceSourceEffect()]) を保有
    もし p1 が PlayCardAction(Card="18", Choice=0) を適用する
    ならば 追加の副作用なし(再帰防止、ADR-0023 §5)
    かつ p1 の Influences.Count が 0 になる(Influence は除去)

  @DZ-388
  シナリオ: OriginEffects 動的注入(他カードへの波及)
    前提 p1 が ApplyInfluenceEffect(Self, ...) を含むカードを手札に持つ
    もし p1 が当該カードを PlayCardAction で適用する
    ならば p1 の Influences に追加された新規 Influence の OriginEffects は、当該カードの effects 列スナップショットと等しい
    (EffectInterpreter が context.CurrentCardEffects から動的注入、ADR-0023 §7)
