# language: ja
機能: PlayerState(プレイヤー状態 = 識別子 + 手札)

  @PLAYER-011
  シナリオ: 有効な Id で生成すると Id プロパティが保持される (正常系・Small)
    前提 PlayerId.Of("p1") と Hand.Empty
    もし new PlayerState(id, hand) で生成する
    ならば PlayerState.Id は PlayerId.Of("p1") である

  @PLAYER-011
  シナリオ: 有効な Hand で生成すると Hand プロパティが保持される (正常系・Small)
    前提 PlayerId.Of("p1") と [a] を持つ Hand
    もし new PlayerState(id, hand) で生成する
    ならば PlayerState.Hand は [a] を含む

  @PLAYER-012
  シナリオ: 同じ Id と同じ Hand の 2 つの PlayerState は等価 (正常系・Small)
    前提 同じ Id と同じ Hand を持つ PlayerState を 2 つ
    もし Equals 比較
    ならば 等価

  @PLAYER-012
  シナリオ: 異なる Id は非等価 (正常系・Small)
    前提 同じ Hand だが異なる Id の 2 つの PlayerState
    もし Equals 比較
    ならば 非等価

  @PLAYER-012
  シナリオ: 異なる Hand は非等価 (正常系・Small)
    前提 同じ Id だが異なる Hand の 2 つの PlayerState
    もし Equals 比較
    ならば 非等価

  @PLAYER-012
  シナリオ: N=2 (独立した 2 人のプレイヤー) は非等価 (正常系・Small)
    前提 PlayerId.Of("p1") + Hand.Empty と PlayerId.Of("p2") + Hand.Empty
    もし Equals 比較
    ならば 2 つは非等価

  @PLAYER-013
  シナリオ: 等価な 2 つの PlayerState の GetHashCode は一致 (正常系・Small)
    前提 同じ Id と同じ Hand の PlayerState を 2 つ
    もし GetHashCode を呼ぶ
    ならば 2 つのハッシュ値は等しい

  @PLAYER-014
  シナリオ: with 式で Hand を差し替えると新 PlayerState の Id は不変 (正常系・Small)
    前提 PlayerId.Of("p1") + Hand.Empty の PlayerState
    もし with 式で Hand を新しい Hand に差し替える
    ならば 新 PlayerState の Id は元と同じ "p1" のまま

  @PLAYER-014
  シナリオ: with 式で Hand を差し替えると新 PlayerState の Hand は新値 (正常系・Small)
    前提 PlayerId.Of("p1") + Hand.Empty の PlayerState
    かつ 新しい Hand([CardId.Of("a")])
    もし with 式で Hand を差し替える
    ならば 新 PlayerState の Hand は [CardId.Of("a")] を含む

  @PLAYER-014
  シナリオ: with 式で Hand を差し替えても元 PlayerState の Hand は不変 (正常系・Small)
    前提 PlayerId.Of("p1") + Hand.Empty の PlayerState
    もし with 式で Hand を新しい Hand に差し替える
    ならば 元 PlayerState の Hand は Hand.Empty のまま

  @PLAYER-015
  シナリオ: コンストラクタで Id が null (異常系・Small)
    前提 null Id, Hand.Empty
    もし new PlayerState(null, hand) で生成
    ならば ArgumentNullException が発生

  @PLAYER-016
  シナリオ: コンストラクタで Hand が null (異常系・Small)
    前提 PlayerId.Of("p1"), null Hand
    もし new PlayerState(id, null) で生成
    ならば ArgumentNullException が発生

  @PLAYER-017
  シナリオ: with 式で Id を null に差し替え (異常系・Small)
    前提 任意の PlayerState
    もし with { Id = null } を評価
    ならば init setter から ArgumentNullException が発生

  @PLAYER-018
  シナリオ: with 式で Hand を null に差し替え (異常系・Small)
    前提 任意の PlayerState
    もし with { Hand = null } を評価
    ならば init setter から ArgumentNullException が発生
