# language: ja
機能: IRandomSource / XorShiftRandom

  @RND-001
  シナリオ: NextInt の戻り値が範囲内 (正常系・Small)
    前提 任意のシードの XorShiftRandom
    もし NextInt(0, 10) を 100 回呼ぶ
    ならば 各値は 0 以上 10 未満である

  @RND-002 @RND-003
  シナリオ: 同じシードで再現性がある (正常系・Small)
    前提 シード 42 の XorShiftRandom を 2 つ生成
    もし 各々 NextInt を 20 回呼び出す
    ならば 2 つの系列は完全に同じ

  @RND-005
  シナリオ: シード 0 は内部的に 1 に変換 (準正常系・Small)
    前提 シード 0 の XorShiftRandom
    もし NextInt を 10 回呼ぶ
    ならば 例外なく整数を返す
    かつ シード 1 の XorShiftRandom と完全に同じ系列になる

  @RND-004
  シナリオ: maxExclusive < min は異常 (異常系・Small)
    前提 任意のシードの XorShiftRandom
    もし NextInt(10, 5) を呼ぶ
    ならば ArgumentException が発生する

  @RND-004
  シナリオ: maxExclusive == min は異常 (異常系・Small)
    前提 任意のシードの XorShiftRandom
    もし NextInt(5, 5) を呼ぶ
    ならば ArgumentException が発生する
