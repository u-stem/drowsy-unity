# language: ja
機能: IDrowZzzGameSessionSerializer (Application 層 Persistence interface) (M5-PR1)

  @APP-045
  シナリオ: Save に session = null で ArgumentNullException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)
    もし Save(null, "any") を呼ぶ
    ならば ArgumentNullException が発生する

  @APP-046
  シナリオ: Save に path = null・空・空白のみ で ArgumentException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)+ 有効な session
    もし Save(session, null) または Save(session, "") または Save(session, "   ") を呼ぶ
    ならば ArgumentException が発生する

  @APP-047
  シナリオ: Save → Load の round-trip で同一 session が返る (正常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)+ 有効な session
    もし Save(session, "path/a") を呼んだ後 Load("path/a") を呼ぶ
    ならば 返却値は同一の session 参照(fake は変換なし)

  @APP-048
  シナリオ: Load に path = null・空・空白のみ で ArgumentException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)
    もし Load(null) または Load("") または Load("   ") を呼ぶ
    ならば ArgumentException が発生する

  @APP-049
  シナリオ: Load に未保存 path で FileNotFoundException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake、空ストア)
    もし Load("path/not-saved") を呼ぶ
    ならば FileNotFoundException が発生する

  @APP-050
  シナリオ: SaveAsync に session = null で ArgumentNullException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)
    もし SaveAsync(null, "any") を呼ぶ
    ならば ArgumentNullException が発生する

  @APP-051
  シナリオ: SaveAsync に path = null・空・空白のみ で ArgumentException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)+ 有効な session
    もし SaveAsync(session, null) または SaveAsync(session, "") または SaveAsync(session, "   ") を呼ぶ
    ならば ArgumentException が発生する

  @APP-052
  シナリオ: SaveAsync → LoadAsync の round-trip で同一 session が返る (正常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)+ 有効な session
    もし SaveAsync(session, "path/a") を await した後 LoadAsync("path/a") を await する
    ならば 返却値は同一の session 参照(fake は変換なし)

  @APP-053
  シナリオ: SaveAsync に cancelled CancellationToken で OperationCanceledException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)+ 有効な session + cancelled な CancellationToken
    もし SaveAsync(session, "any", cts.Token) を呼ぶ
    ならば OperationCanceledException が発生する

  @APP-054
  シナリオ: LoadAsync に path = null・空・空白のみ で ArgumentException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)
    もし LoadAsync(null) または LoadAsync("") または LoadAsync("   ") を呼ぶ
    ならば ArgumentException が発生する

  @APP-055
  シナリオ: LoadAsync に未保存 path で FileNotFoundException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake、空ストア)
    もし LoadAsync("path/not-saved") を await する
    ならば FileNotFoundException が発生する

  @APP-056
  シナリオ: LoadAsync に cancelled CancellationToken で OperationCanceledException (異常系・Small)
    前提 IDrowZzzGameSessionSerializer 実装(fake)+ cancelled な CancellationToken
    もし LoadAsync("any", cts.Token) を呼ぶ
    ならば OperationCanceledException が発生する
