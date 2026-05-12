# language: ja
@DZ-INFLUENCE
機能: 継続影響(Influence)モデル
  「自分のフェーズ開始時に SDP を減らす」のような継続効果を、有限カウントで保有するためのモデル。
  ADR-0007 §1.5「継続影響」、M2-PR5 で導入。

  背景:
    前提 用語規約は ADR-0009 に従う(ターン = 30 分単位、フェーズ = 1 プレイヤー 1 行動)

  @DZ-155
  シナリオ: PlayerInfluence の構築
    もし Trigger=OwnPhaseStart、TickEffect=AdjustSdpEffect(Self, -5)、RemainingCount=3 で PlayerInfluence を生成する
    ならば Trigger は OwnPhaseStart である
    かつ TickEffect は AdjustSdpEffect(Self, -5) と同一
    かつ RemainingCount は 3 である

  @DZ-156
  シナリオ: RemainingCount < 1 の防御
    もし RemainingCount=0 で PlayerInfluence を生成する
    ならば ArgumentOutOfRangeException が発生する

  @DZ-181 @DZ-176
  シナリオ: フェーズ開始時の Tick(発動回数 -1、カード No.02 統合テスト DZ-176 と同じ動作確認)
    前提 p1 が WaitingForEndTurn、p2 が PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,-5), 3) を 1 件保有
    もし p1 が EndTurnAction を適用する
    ならば p2 が新しい current player になる
    かつ p2 の SDP が -5 になる
    かつ p2 の影響の RemainingCount が 2 になる

  @DZ-181 @DZ-177
  シナリオ: RemainingCount 1 → 0 で除去(統合テスト DZ-177 と同じ動作確認)
    前提 p2 が PlayerInfluence(OwnPhaseStart, AdjustSdpEffect(Self,-5), 1) を 1 件保有
    もし p1 が EndTurnAction を適用する
    ならば p2 の影響リストは空になる

  @DZ-182 @DZ-178
  シナリオ: 他プレイヤーの影響は Tick されない(統合テスト DZ-178 と同じ動作確認)
    前提 p1 と p2 が各 PlayerInfluence(OwnPhaseStart,...,3) を 1 件ずつ保有
    かつ p1 が WaitingForEndTurn
    もし p1 が EndTurnAction を適用する
    ならば p2 のみ Tick される
    かつ p1 の影響の RemainingCount は 3 のまま
