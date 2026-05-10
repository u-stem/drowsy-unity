# language: ja
機能: IGameRule(ゲームルール interface)

  @APP-005
  シナリオ: 合法判定が true のときに Apply を呼ぶと新しい Session が返る (正常系・Small)
    前提 IGameRule の最小ダミー実装 DummyRule を用意
    かつ DummyRule.IsLegalMove(session, action) が true を返す状態
    もし DummyRule.Apply(session, action) を呼ぶ
    ならば 新しい Session インスタンスが返る
