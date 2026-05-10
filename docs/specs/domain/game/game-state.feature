# language: ja
機能: GameState(ゲーム全体の状態を表す不変ルート集約)

  @GS-003
  シナリオ: 有効な引数で生成 - Players が保持される (正常系・Small)
    前提 [PlayerState(p1, Hand.Empty)] と Pile.Empty 3 つ
    もし new GameState(players, deck, discard, field) で生成
    ならば GameState.Players は [PlayerState(p1, Hand.Empty)] である

  @GS-003
  シナリオ: 有効な引数で生成 - Deck が保持される (正常系・Small)
    前提 任意の players、特定の Deck、Pile.Empty 2 つ
    もし new GameState で生成
    ならば GameState.Deck は入力 Deck と等価

  @GS-003
  シナリオ: 有効な引数で生成 - Discard が保持される (正常系・Small)
    前提 任意の players / Deck、特定の Discard、Pile.Empty 1 つ
    もし new GameState で生成
    ならば GameState.Discard は入力 Discard と等価

  @GS-003
  シナリオ: 有効な引数で生成 - Field が保持される (正常系・Small)
    前提 任意の players / Deck / Discard、特定の Field
    もし new GameState で生成
    ならば GameState.Field は入力 Field と等価

  @GS-003
  シナリオ: 有効な引数で生成 - Turn が保持される (正常系・Small)
    前提 任意の players / Deck / Discard / Field、特定の Turn
    もし new GameState で生成
    ならば GameState.Turn は入力 Turn と等価

  @GS-004
  シナリオ: N=1 全フィールド一致は等価 (正常系・Small)
    前提 同じ Players(1 人) と同じ Deck/Discard/Field の 2 つの GameState
    もし Equals 比較
    ならば 等価

  @GS-004
  シナリオ: N=2 全フィールド一致は等価 (正常系・Small)
    前提 同じ Players(2 人) と同じ Deck/Discard/Field の 2 つの GameState
    もし Equals 比較
    ならば 等価

  @GS-004
  シナリオ: Players の順序が異なる場合は非等価 (正常系・Small)
    前提 [p1, p2] の GameState と [p2, p1] の GameState
    もし Equals 比較
    ならば 非等価

  @GS-004
  シナリオ: Players 数が異なる場合は非等価 (正常系・Small)
    前提 1 人の GameState と 2 人の GameState
    もし Equals 比較
    ならば 非等価

  @GS-004
  シナリオ: Deck が異なる場合は非等価 (正常系・Small)
    前提 同じ Players / Discard / Field、異なる Deck
    もし Equals 比較
    ならば 非等価

  @GS-004
  シナリオ: Discard が異なる場合は非等価 (正常系・Small)
    前提 同じ Players / Deck / Field、異なる Discard
    もし Equals 比較
    ならば 非等価

  @GS-004
  シナリオ: Field が異なる場合は非等価 (正常系・Small)
    前提 同じ Players / Deck / Discard、異なる Field
    もし Equals 比較
    ならば 非等価

  @GS-004
  シナリオ: Turn が異なる場合は非等価 (正常系・Small)
    前提 同じ Players / Deck / Discard / Field、異なる Turn (TurnNumber 異)
    もし Equals 比較
    ならば 非等価

  @GS-004
  シナリオ: 同一インスタンスは等価 (正常系・Small)
    前提 任意の GameState
    もし 自分自身と Equals 比較
    ならば 等価

  @GS-004
  シナリオ: Equals(GameState) に null を渡すと false (正常系・Small)
    前提 任意の GameState
    もし null の GameState を Equals
    ならば false

  @GS-005
  シナリオ: 等価な 2 つの GameState の GetHashCode は一致 (正常系・Small)
    前提 全フィールド一致の 2 つの GameState
    もし GetHashCode
    ならば 2 つのハッシュ値は等しい

  @GS-006
  シナリオ: 等価な GameState は operator== で true (正常系・Small)
    前提 全フィールド一致の 2 つの GameState
    もし operator== で比較
    ならば true

  @GS-006
  シナリオ: 非等価な GameState は operator== で false (正常系・Small)
    前提 異なる Deck の 2 つの GameState
    もし operator== で比較
    ならば false

  @GS-006
  シナリオ: 非等価な GameState は operator!= で true (正常系・Small)
    前提 異なる Deck の 2 つの GameState
    もし operator!= で比較
    ならば true

  @GS-006
  シナリオ: 両方 null は operator== で true (正常系・Small)
    前提 null の GameState 参照を 2 つ
    もし operator== で比較
    ならば true

  @GS-006
  シナリオ: 片方 null で他方非 null は operator== で false (左 null) (正常系・Small)
    前提 null の GameState 参照と 非 null GameState
    もし operator== で比較
    ならば false

  @GS-006
  シナリオ: 左側非 null で右側 null は operator== で false (正常系・Small)
    前提 非 null GameState と null の GameState 参照
    もし operator== で比較
    ならば false

  @GS-007
  シナリオ: Equals(object) に null を渡すと false (正常系・Small)
    前提 任意の GameState
    もし Equals((object)null) を呼ぶ
    ならば false

  @GS-007
  シナリオ: Equals(object) に異なる型を渡すと false (正常系・Small)
    前提 任意の GameState
    もし Equals((object)"not a GameState") を呼ぶ
    ならば false

  @GS-008
  シナリオ: with 式で Deck を差し替えると新 GameState の Deck は新値 (正常系・Small)
    前提 GameState
    かつ 新しい Deck
    もし with 式で Deck を差し替える
    ならば 新 GameState の Deck は新値

  @GS-008
  シナリオ: with 式で Deck を差し替えても Players は不変 (正常系・Small)
    前提 GameState
    もし with 式で Deck を差し替える
    ならば 新 GameState の Players は元と同じ

  @GS-008
  シナリオ: with 式で Deck を差し替えても Discard は不変 (正常系・Small)
    前提 GameState
    もし with 式で Deck を差し替える
    ならば 新 GameState の Discard は元と同じ

  @GS-008
  シナリオ: with 式で Deck を差し替えても Field は不変 (正常系・Small)
    前提 GameState
    もし with 式で Deck を差し替える
    ならば 新 GameState の Field は元と同じ

  @GS-008
  シナリオ: with 式で Deck を差し替えても元 GameState は不変 (正常系・Small)
    前提 GameState
    もし with 式で Deck を差し替える
    ならば 元 GameState の Deck は元値のまま

  @GS-009
  シナリオ: 生成後にソースリストを変更しても GameState.Players は不変 (正常系・Small)
    前提 List<PlayerState> source とそこから生成した GameState
    もし source に新規 PlayerState を追加
    ならば GameState.Players は影響を受けない

  @GS-010
  シナリオ: コンストラクタ players が null (異常系・Small)
    前提 null players
    もし new GameState(null, deck, discard, field) で生成
    ならば ArgumentNullException が発生

  @GS-011
  シナリオ: コンストラクタ deck が null (異常系・Small)
    前提 null deck
    もし new GameState(players, null, discard, field) で生成
    ならば ArgumentNullException が発生

  @GS-012
  シナリオ: コンストラクタ discard が null (異常系・Small)
    前提 null discard
    もし new GameState(players, deck, null, field) で生成
    ならば ArgumentNullException が発生

  @GS-013
  シナリオ: コンストラクタ field が null (異常系・Small)
    前提 null field
    もし new GameState(players, deck, discard, null) で生成
    ならば ArgumentNullException が発生

  @GS-014
  シナリオ: コンストラクタ players に null 要素 (異常系・Small)
    前提 [PlayerState(p1, Hand.Empty), null] の players
    もし new GameState で生成
    ならば ArgumentException が発生

  @GS-015
  シナリオ: コンストラクタ players に重複 PlayerId (異常系・Small)
    前提 [PlayerState(p1, ...), PlayerState(p1, ...)] の players
    もし new GameState で生成
    ならば ArgumentException が発生

  @GS-016
  シナリオ: with 式で Players を null に (異常系・Small)
    前提 任意の GameState
    もし with { Players = null } を評価
    ならば ArgumentNullException が発生

  @GS-017
  シナリオ: with 式で Deck を null に (異常系・Small)
    前提 任意の GameState
    もし with { Deck = null } を評価
    ならば ArgumentNullException が発生

  @GS-018
  シナリオ: with 式で Discard を null に (異常系・Small)
    前提 任意の GameState
    もし with { Discard = null } を評価
    ならば ArgumentNullException が発生

  @GS-019
  シナリオ: with 式で Field を null に (異常系・Small)
    前提 任意の GameState
    もし with { Field = null } を評価
    ならば ArgumentNullException が発生

  @GS-020
  シナリオ: コンストラクタ turn が null (異常系・Small)
    前提 null turn
    もし new GameState(players, deck, discard, field, null) で生成
    ならば ArgumentNullException が発生

  @GS-021
  シナリオ: with 式で Turn を null に (異常系・Small)
    前提 任意の GameState
    もし with { Turn = null } を評価
    ならば ArgumentNullException が発生

  @GS-022
  シナリオ: Turn の CurrentPlayerIndex が Players 範囲外 - コンストラクタ経由 (異常系・Small)
    前提 1 人の Players と CurrentPlayerIndex=1 の Turn(範囲外)
    もし new GameState で生成
    ならば ArgumentException が発生

  @GS-022
  シナリオ: with 式で Turn を Players 範囲外に差し替え (異常系・Small)
    前提 1 人の Players の GameState
    もし with { Turn = TurnState(1, 5) } を評価
    ならば ArgumentException が発生

  @GS-022
  シナリオ: with 式で Players を縮小して既存 Turn が範囲外に (異常系・Small)
    前提 2 人の Players + CurrentPlayerIndex=1 の Turn の GameState
    もし with { Players = 1 人 } を評価
    ならば ArgumentException が発生
