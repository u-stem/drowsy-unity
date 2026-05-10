# language: ja
機能: IGameAction(ゲームアクションのマーカー interface)

  @APP-002
  シナリオ: record が IGameAction を実装できる (正常系・Small)
    前提 IGameAction を実装した sealed record DummyAction を定義
    もし DummyAction のインスタンスを IGameAction として代入する
    ならば 代入は成功する
