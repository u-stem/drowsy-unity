# language: ja
@INF-072 @INF-073 @INF-074 @INF-075 @INF-076 @INF-077 @INF-078 @INF-079
機能: DrowZzzGameConfigAsset の Designer-friendly な L3 ゲームバランス値編集

  M4-PR7 で追加される `IGameConfig` の ScriptableObject 実装。Designer は Unity Inspector で
  FdpPool / DdpPool を編集可能になる。新規 `.asset` 作成時は Reset で本物のデフォルト値が自動投入され、
  空 / null は OnValidate で Debug.LogError 通知される。

  @INF-072
  シナリオ: SerializeField 経由で設定された FdpPool が読み取れる
    前提 DrowZzzGameConfigAsset のインスタンスを ScriptableObject.CreateInstance で生成する
    かつ SetPoolsForTest で FdpPool に [1, 2, 3] を投入する
    もし FdpPool プロパティを取得する
    ならば [1, 2, 3] が IReadOnlyList<int> として返る

  @INF-073
  シナリオ: SerializeField 経由で設定された DdpPool が読み取れる
    前提 DrowZzzGameConfigAsset のインスタンスを生成する
    かつ SetPoolsForTest で DdpPool に [10, 20, 30] を投入する
    もし DdpPool プロパティを取得する
    ならば [10, 20, 30] が IReadOnlyList<int> として返る

  @INF-074
  シナリオ: _fdpPool が null のとき FdpPool は空配列を返す
    前提 DrowZzzGameConfigAsset のインスタンスを生成する
    かつ SetPoolsForTest で _fdpPool に null を投入する
    もし FdpPool プロパティを取得する
    ならば Array.Empty<int>() と等価な空 IReadOnlyList が返る

  @INF-075
  シナリオ: _ddpPool が null のとき DdpPool は空配列を返す
    前提 DrowZzzGameConfigAsset のインスタンスを生成する
    かつ SetPoolsForTest で _ddpPool に null を投入する
    もし DdpPool プロパティを取得する
    ならば Array.Empty<int>() と等価な空 IReadOnlyList が返る

  @INF-076
  シナリオ: Reset() で FdpPool に ADR-0006 §M1 のデフォルト 10 要素が投入される
    前提 DrowZzzGameConfigAsset の新規インスタンスを生成する
    もし Reset を呼ぶ
    ならば FdpPool が [0, 10, 20, 30, 35, 40, 45, 50, 55, 60] と一致する

  @INF-077
  シナリオ: Reset() で DdpPool に DdpPoolConstants.BuildDefaultPool() の 39 要素(13 種 × 3 枚)が投入される
    前提 DrowZzzGameConfigAsset の新規インスタンスを生成する
    もし Reset を呼ぶ
    ならば DdpPool が DdpPoolConstants.BuildDefaultPool() と要素順保持で一致する

  @INF-078
  シナリオ: _fdpPool が null のとき OnValidate が Debug.LogError を発火する
    前提 DrowZzzGameConfigAsset のインスタンスを生成し _fdpPool を null に設定する
    もし OnValidate を呼ぶ
    ならば 「DrowZzzGameConfigAsset: FdpPool が空」を含む Debug.LogError が Asset リンク付きで発火する

  @INF-079
  シナリオ: _ddpPool が null のとき OnValidate が Debug.LogError を発火する
    前提 DrowZzzGameConfigAsset のインスタンスを生成し _ddpPool を null に設定する
    もし OnValidate を呼ぶ
    ならば 「DrowZzzGameConfigAsset: DdpPool が空」を含む Debug.LogError が Asset リンク付きで発火する
