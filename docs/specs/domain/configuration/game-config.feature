# language: ja
# IGameConfig は Phase 0 では空のひな形 interface のため、シナリオなし。
# Phase 1 以降で具体プロパティを追加する際に、本ファイルへ受け入れシナリオを追記する。

機能: IGameConfig (ゲームバランス設定)

  # 例: Phase 1 で InitialHandSize プロパティを追加した際のシナリオ雛形
  #
  # @CFG-101
  # シナリオアウトライン: ゲーム開始時に初期手札を InitialHandSize 枚配る (正常系・Small)
  #   前提 IGameConfig.InitialHandSize == <hand>
  #   かつ プレイヤーの空の手札
  #   もし StartGame を実行する
  #   ならば プレイヤーの手札は <hand> 枚になる
  #
  #   例:
  #     | hand |
  #     | 5    |
  #     | 7    |
  #     | 1    |
