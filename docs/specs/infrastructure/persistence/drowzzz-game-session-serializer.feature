# language: ja
# DrowZzzGameSessionSerializer の受け入れシナリオ。
# シナリオ名末尾に Type 分類を併記する: (正常系・Small) / (異常系・Small) / (準正常系・Small)

機能: DrowZzzGameSession の JSON 永続化

  @INF-052 @INF-054
  シナリオ: 親ディレクトリが存在しないパスで Save し、Load で復元できる (正常系・Small)
    前提 一時ディレクトリ配下に存在しないサブディレクトリを含む path を用意する
    かつ 任意の DrowZzzGameSession を構築する
    もし Save(session, path) を呼ぶ
    かつ Load(path) を呼ぶ
    ならば 復元された session が元 session と Equals で等価になる
    かつ 親ディレクトリが自動作成される

  @INF-053
  シナリオ: 既存ファイルへ Save すると上書きされる (正常系・Small)
    前提 path に旧 session の JSON を Save 済み
    もし 新 session で Save(newSession, path) を呼ぶ
    かつ Load(path) を呼ぶ
    ならば 復元された session が新 session と Equals で等価になる

  @INF-058
  シナリオ: Outcome=null(未終了)を round-trip で保持する (正常系・Small)
    前提 Outcome=null の session を構築する
    もし Save → Load を行う
    ならば 復元された session の Outcome は null

  @INF-056
  シナリオ: Outcome=WinnerOutcome の serialize 形 (正常系・Small)
    前提 Outcome=WinnerOutcome(PlayerA) の session を構築する
    もし Save を行う
    ならば JSON の outcome フィールドは {"type": "Winner", "winner": "PlayerA"} 構造を持つ

  @INF-057
  シナリオ: Outcome=DrawOutcome の serialize 形 (正常系・Small)
    前提 Outcome=DrawOutcome の session を構築する
    もし Save を行う
    ならば JSON の outcome フィールドは {"type": "Draw"} 構造を持つ

  @INF-050
  シナリオアウトライン: IEffect 全 12 派生型の round-trip (正常系・Medium)
    前提 <派生型> を効果列に含む session を構築する
    もし Save → Load を行う
    ならば 復元された effect が元 effect と Equals で等価になる

    例:
      | 派生型                                    |
      | AdjustSdpEffect                           |
      | ApplyInfluenceEffect                      |
      | RemoveInfluenceEffect                     |
      | DrawCardEffect                            |
      | DamageBedEffect                           |
      | EarlyWinTriggerEffect                     |
      | ChoiceEffect                              |
      | TimeOfDayBranchEffect                     |
      | KeywordedEffect                           |
      | RequiresMinimumTotalPointsMarkerEffect    |
      | UsageRestrictionMarkerEffect              |
      | AssociatableMarkerEffect                  |

  @INF-055
  シナリオ: wrapper 効果の再帰的 round-trip (正常系・Medium)
    前提 KeywordedEffect(Counter, ChoiceEffect([[AdjustSdpEffect], [DrawCardEffect]])) を構築する
    もし Save → Load を行う
    ならば 復元された effect が元 effect と Equals で等価になる
    かつ 内側の AdjustSdpEffect / DrawCardEffect / ChoiceEffect / KeywordedEffect すべてが個別に復元される

  @INF-060
  シナリオ: 存在しないファイル Load (異常系・Small)
    前提 path にファイルが存在しない
    もし Load(path) を呼ぶ
    ならば FileNotFoundException が発生する

  @INF-061
  シナリオ: Save の session=null (異常系・Small)
    もし Save(null, path) を呼ぶ
    ならば ArgumentNullException が発生する

  @INF-062 @INF-063
  シナリオアウトライン: path が空白系 (異常系・Small)
    もし <Method>(args, <path>) を呼ぶ
    ならば ArgumentException が発生する

    例:
      | Method | path  |
      | Save   |       |
      | Save   | "  "  |
      | Load   |       |
      | Load   | "  "  |

  @INF-064
  シナリオ: 破損 JSON の Load (異常系・Small)
    前提 path のファイル内容が "{ broken" のような不正な JSON
    もし Load(path) を呼ぶ
    ならば InvalidDataException が発生し、内側に JsonException を持つ

  @INF-065
  シナリオ: 未対応 schemaVersion (異常系・Small)
    前提 path に "schemaVersion": 999 の JSON が保存されている
    もし Load(path) を呼ぶ
    ならば InvalidDataException が発生する

  @INF-066 @INF-067
  シナリオアウトライン: Effect の type discriminator 異常 (異常系・Small)
    前提 IEffect JSON の "type" 値が <type>
    もし Load(path) を呼ぶ
    ならば JsonSerializationException が発生する

    例:
      | type                |
      | (欠落)              |
      | "UnknownEffectType" |

  @INF-068 @INF-069
  シナリオアウトライン: GameOutcome の type discriminator 異常 (異常系・Small)
    前提 GameOutcome JSON の "type" 値が <type>
    もし Load(path) を呼ぶ
    ならば JsonSerializationException が発生する

    例:
      | type                 |
      | (欠落)               |
      | "UnknownOutcomeType" |

  @INF-070
  シナリオ: 必須プロパティ欠落の DTO (異常系・Small)
    前提 path の JSON で "gameState" プロパティが欠落している
    もし Load(path) を呼ぶ
    ならば InvalidOperationException が発生し、欠落した property 名 "GameState" を含むメッセージを持つ

  @INF-071
  シナリオ: DefaultSavePath の fileName 空白 (異常系・Small)
    もし DefaultSavePath("  ") を呼ぶ
    ならば ArgumentException が発生する
