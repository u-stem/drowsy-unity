# language: ja
@GS-OUTCOME
機能: GameOutcome(ゲーム終了状態)の値オブジェクト
  ゲーム終了の事実と終わり方(勝者あり / 引き分け)を表す Domain 層の sealed record 階層。
  ADR-0010 §2、M3-PR1 で導入。

  @GS-101
  シナリオ: WinnerOutcome の構築と二重ガード
    もし PlayerId="p1" で WinnerOutcome を生成する
    ならば Winner が "p1" と一致する
    かつ Winner=null での生成は ArgumentNullException を投げる
    かつ 既存 WinnerOutcome に対して with { Winner = null } は ArgumentNullException を投げる

  @GS-102
  シナリオ: WinnerOutcome の値同値性
    前提 a = WinnerOutcome("p1")、b = WinnerOutcome("p1")
    ならば a と b は Equals で true を返す
    かつ WinnerOutcome("p1") と WinnerOutcome("p2") は false を返す

  @GS-103
  シナリオ: DrawOutcome は常に等価
    前提 a = DrawOutcome()、b = DrawOutcome()
    ならば a と b は Equals で true を返す

  @GS-104
  シナリオ: WinnerOutcome と DrawOutcome は非等価
    前提 a = WinnerOutcome("p1")、b = DrawOutcome()
    ならば a と b は Equals で false を返す
