# language: ja
@DZ-CARD-08
機能: カード No.08「廻るための知恵」
  Instinct(本能)キーワード + 選択式 ChoiceEffect で自他いずれかに「ベッド破損 SDP 符号反転」永続影響を付与。
  3 枚デッキ前提で、同一プレイヤーに同種影響が 1〜3 件付く可能性があり、保有数の奇偶で反転判定。
  No.06「2 倍化」と重複保有時は「逆転 → 2 倍化」順で計算(SDP -8 → +8 → +16)。
  2026-05-17 で導入、`InvertBedDamageSdpInfluenceMarkerEffect` 新設。

  背景:
    前提 InMemoryCardCatalog に Card "08"「廻るための知恵」が最上位 ChoiceEffect(2 分岐)1 件で登録されている
    かつ 選択 1 = [AdjustSdpEffect(Opponent,+5), KeywordedEffect([Instinct], ApplyInfluenceEffect(Self, PlayerInfluence(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffect, Perpetual)))]
    かつ 選択 2 = [AdjustSdpEffect(Self,+5), KeywordedEffect([Instinct], ApplyInfluenceEffect(Opponent, PlayerInfluence(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffect, Perpetual)))]
    かつ N=2(p1, p2)

  @DZ-294 @DZ-295
  シナリオ: Card 08 を Choice 0(自分強化)でプレイ
    前提 p1 が current で WaitingForPlay、p1 が Card "08" を手札に持つ
    もし p1 が PlayCardAction(Card="08", Choice=0) を適用する
    ならば p2 の SDP が +5 になる
    かつ p1 の Influences に InvertBedDamage 影響(永続)が 1 件追加される

  @DZ-296 @DZ-297
  シナリオ: Card 08 を Choice 1(相手押し付け)でプレイ
    前提 p1 が current で WaitingForPlay、p1 が Card "08" を手札に持つ
    もし p1 が PlayCardAction(Card="08", Choice=1) を適用する
    ならば p1 の SDP が +5 になる
    かつ p2 の Influences に InvertBedDamage 影響(永続)が 1 件追加される

  @DZ-298
  シナリオ: Instinct で Card 08 は AbandonAction の捨て対象として選択不可
    前提 p1 が current で WaitingForPlay、p1 が Card "08" を手札に持つ
    もし p1 が AbandonAction(Choice=GainSdp, CardIndex=Card 08 の index) で IsLegalMove を確認する
    ならば 結果は false である(Instinct = 放棄対象不可、ADR-0011 §4.2)

  @DZ-299
  シナリオ: 1 件保有でベッド破損反転(回復方向)
    前提 p2 が PlayerInfluence(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffect, Perpetual) を 1 件保有
    かつ BedDamages[p2] = 40%
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の SDP が +8 になる(40/5=8 の符号反転、回復方向)

  @DZ-300
  シナリオ: 2 件保有で元に戻る(奇偶判定 = 偶数)
    前提 p2 が PlayerInfluence(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffect, Perpetual) を 2 件保有
    かつ BedDamages[p2] = 40%
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の SDP が -8 になる(2 件 = 反転 × 反転 = 元、通常の減算)

  @DZ-301
  シナリオ: Invert 1 件 + Double 1 件で「逆転 → 2 倍化」順
    前提 p2 が PlayerInfluence(OwnPhaseStart, InvertBedDamageSdpInfluenceMarkerEffect, Perpetual) を 1 件 + PlayerInfluence(OwnPhaseStart, DoubleBedDamageSdpInfluenceMarkerEffect, _) を 1 件保有
    かつ BedDamages[p2] = 40%
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 の SDP が +16 になる(SDP -8 → 反転 +8 → 2 倍化 +16、オーナー JIT 確定順序)

  @DZ-302
  シナリオ: Choice 範囲外で illegal-move
    前提 p1 が current で WaitingForPlay、p1 が Card "08" を手札に持つ
    もし p1 が PlayCardAction(Card="08", Choice=2) で IsLegalMove を確認する
    ならば 結果は false である(Branches.Count==2 のため範囲外)
