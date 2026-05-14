# Main.unity Scene セットアップ手順(M5)

このファイルは `Main.unity` Scene を Unity Editor で組み立て、DrowZzz の UI を Play モードで確認するための
ステップバイステップ手順書である。

M5-PR1〜PR7 で UI のコード一式(`DrowZzzGameView` / `DrowZzzGamePresenter` / `UserSettingsBinder` /
`DrowZzzGame.uxml` / `.uss` / `ProjectLifetimeScope` / `GameLifetimeScope`)は揃っているが、`Main.unity` Scene と
GameObject ツリーは **Unity Editor での手作業** が必要である。Scene の `.unity` ファイルは GameObject / Component の
`fileID` / GUID 参照の整合性を Unity Editor が管理するため、リポジトリ外(Claude 等)からのテキスト編集では
正確に再現できない。本手順書はその手作業を機械的に追える形に落としたもの。

設計の根拠は [ADR-0016 §6 シーン構成](../adr/0016-m5-bootstrap-presentation.md) / §7.2(Bootstrap への注入経路)。

---

## 前提

| 前提 | 確認 PR |
| ---- | ---- |
| `Assets/_Project/Data/Configuration/DrowZzzGameConfig.asset` 配置済 | M4-PR7 |
| `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset` 配置済 | M4-PR7 |
| `Assets/_Project/UI/Games/DrowZzz/DrowZzzGame.uxml` / `.uss` | M5-PR3 / PR6 / PR7 |
| `ProjectLifetimeScope` / `GameLifetimeScope` の Configure 実装済 | M5-PR3 / PR4 |
| `DrowZzzGameView` / `DrowZzzGamePresenter` / `UserSettingsBinder` | M5-PR2〜PR7 |

> Unity 6000.4.6f1 を前提とする。メニュー名・コンポーネント名は本書では英語表記で記す。Editor の表示言語が
> 日本語の場合は読み替える(例: `Assets > Create` = `アセット > 作成`、`Add Component` = `コンポーネントを追加`、
> `File > Build Profiles` = `ファイル > ビルドプロファイル`)。Unity のマイナーバージョンで変わる場合もあるため、
> 見つからないときは Unity の検索(`Add Component` 検索ボックス / `Assets > Create` の検索)を使う。

---

## 完成形のシーン構造(ADR-0016 §6)

```
Main.unity
  ├─ [ProjectLifetimeScope]  (GameObject)
  │     └ ProjectLifetimeScope (Component)
  │         - _gameConfig   ← DrowZzzGameConfig.asset
  │         - _cardCatalog  ← DrowZzzCardCatalog.asset
  │     └─ [GameLifetimeScope]  (GameObject, ProjectLifetimeScope の子)
  │           └ GameLifetimeScope (Component)
  │           └─ [DrowZzzGameView]  (GameObject, GameLifetimeScope の子)
  │                 └ UIDocument (Component)
  │                     - Source Asset  ← DrowZzzGame.uxml
  │                     - Panel Settings ← DrowZzzPanelSettings.asset
  │                 └ DrowZzzGameView (Component)
  │                     - _uiDocument ← 同 GameObject の UIDocument
  └─ EventSystem  (GameObject)
```

`GameLifetimeScope` を `ProjectLifetimeScope` GameObject の子に置くと、VContainer が GameObject 階層から
親 `LifetimeScope` を自動解決する(Project スコープの Singleton を Game スコープから Resolve できる)。

---

## 手順

### Step 1: PanelSettings アセットを作成する

UI Toolkit のランタイム描画には `PanelSettings` アセットが必要。

1. Project ウィンドウで `Assets/_Project/UI/Games/DrowZzz/` を選択
2. `Assets > Create > UI Toolkit > Panel Settings Asset`
3. 名前を `DrowZzzPanelSettings` にする
4. Inspector で `Theme Style Sheet` 欄を確認する。`UnityDefaultRuntimeTheme`(UI Toolkit パッケージ導入時に
   Unity が `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss` を自動生成する。通常は PanelSettings
   作成前から存在する)が割り当たっていること。**空の場合は手動で割り当てる**(未割り当てだと Play 時に
   `Can't Generate Mesh, No Font Asset has been assigned.` が出る)

### Step 2: Main.unity Scene を新規作成して「開く」

1. `Assets/Scenes/` を選択(なければ `Assets/` 直下でも可)
2. `Assets > Create > Scene`
3. 名前を `Main` にする(`Main.unity`)
4. **作成した `Main.unity` をダブルクリックして開く**。`Assets > Create > Scene` は `.unity` アセットを作るだけで
   自動では開かない。開かないまま GameObject を作ると、起動時の `Untitled`(`名称未設定`)シーンに入ってしまい、
   `Main.unity` は空のまま残る
5. **確認**: Unity のタイトルバー / Hierarchy 最上段が `Main` になっていること(`Untitled` / `名称未設定` のままなら
   もう一度 `Main.unity` をダブルクリックして開く)。以降の GameObject はすべてこの `Main` シーンに置く
6. デフォルトで存在する `Main Camera` / `Directional Light` はそのままで可(UI Toolkit は Camera 非依存だが、残しても害はない)

### Step 3: GameObject ツリーを配置する

#### 3-1. ProjectLifetimeScope

1. Hierarchy で右クリック → `Create Empty`、名前を `ProjectLifetimeScope` にする
2. `Add Component` → `ProjectLifetimeScope` を検索して追加(`Drowsy.Bootstrap.ProjectLifetimeScope`)
   - **注意**: 検索ボックスに `Scope` / `LifetimeScope` 等の部分文字列を入れると、名前が似た `GameLifetimeScope` も
     並んで表示される。ここで足すのは `ProjectLifetimeScope`(取り違えやすい。`Project` まで入力すれば絞り込める)
   - **確認**: 追加後の Inspector に `Game Config` / `Card Catalog` の 2 つのオブジェクト参照欄が出ていれば正しい。
     `Parent` / `Auto Run` / `Auto Inject Game Objects` しか出ず `Game Config` 欄が無い場合は `GameLifetimeScope` を
     誤って足しているので、コンポーネント右上の `⋮` → `Remove Component` で外し `ProjectLifetimeScope` を足し直す
   - `Parent` / `Auto Run` / `Auto Inject Game Objects` は VContainer の `LifetimeScope` 基底クラス由来で、両コンポーネント共通で表示される

#### 3-2. GameLifetimeScope(ProjectLifetimeScope の子)

1. `ProjectLifetimeScope` を右クリック → `Create Empty`、名前を `GameLifetimeScope` にする(自動で子になる)
2. `Add Component` → `GameLifetimeScope` を追加(`Drowsy.Bootstrap.GameLifetimeScope`)
   - こちらは SerializeField を持たないため、Inspector は `Parent` / `Auto Run` / `Auto Inject Game Objects` のみ
     (`Game Config` 欄が無いのが正しい。3-1 とは逆の判別ポイント)
   - GameObject 階層で `ProjectLifetimeScope` の子になっていれば VContainer が親スコープを自動解決するため、
     `Parent` 欄の手動設定は不要

#### 3-3. DrowZzzGameView(GameLifetimeScope の子)

1. `GameLifetimeScope` を右クリック → `Create Empty`、名前を `DrowZzzGameView` にする
2. `Add Component` → `DrowZzzGameView` を追加(`Drowsy.Presentation.Games.DrowZzz.DrowZzzGameView`)
   - `DrowZzzGameView` は `[RequireComponent(typeof(UIDocument))]` を持つため、**`UIDocument` コンポーネントが自動で同時に追加される**

#### 3-4. EventSystem

1. Hierarchy で右クリック → `UI > Event System`(または `Create Empty` + `Add Component > Event System`)
   - UI Toolkit のポインタ / キーボード入力サポート用

### Step 4: Inspector で参照を割り当てる

#### 4-1. ProjectLifetimeScope の SerializeField

`ProjectLifetimeScope` GameObject を選択し、`ProjectLifetimeScope` コンポーネントの Inspector で:

| フィールド | 割り当てるアセット |
| ---- | ---- |
| `_gameConfig` | `Assets/_Project/Data/Configuration/DrowZzzGameConfig.asset` |
| `_cardCatalog` | `Assets/_Project/Data/Catalogs/DrowZzzCardCatalog.asset` |

> 未割り当てのまま Play すると、`Configure` 内の null チェックが `InvalidOperationException` を投げる
> (Build 後の沈黙 fail を防ぐ意図的な fail-fast、ADR-0016 §7.2)。

#### 4-2. UIDocument の設定

`DrowZzzGameView` GameObject を選択し、`UIDocument` コンポーネントの Inspector で:

| フィールド | 割り当てるアセット |
| ---- | ---- |
| `Source Asset` (Visual Tree Asset) | `Assets/_Project/UI/Games/DrowZzz/DrowZzzGame.uxml` |
| `Panel Settings` | `Assets/_Project/UI/Games/DrowZzz/DrowZzzPanelSettings.asset`(Step 1 で作成) |

#### 4-3. DrowZzzGameView の SerializeField

同じ `DrowZzzGameView` GameObject の `DrowZzzGameView` コンポーネントの Inspector で:

| フィールド | 割り当て |
| ---- | ---- |
| `_uiDocument` | 同 GameObject の `UIDocument` コンポーネント(ドラッグ&ドロップ、または `○` から選択) |

> 未割り当てのまま Play すると、`OnEnable` / `Start` が `Debug.LogError` を出す
> (`UIDocument が Inspector で未設定です`)。

### Step 5: Build Settings に Main.unity を登録する

1. `File > Build Profiles`(または `File > Build Settings`)
2. `Main.unity` を Scene リストに追加(`Add Open Scenes` または ドラッグ)し、index 0 にする

> Play モードで確認するだけなら Build Settings 登録は必須ではないが、WebGL Build(M5-PR8)では必要。

### Step 6: Play モードで確認する

1. `Main.unity` を開いた状態で Play ボタンを押す
2. Game ビューに UI が表示されることを確認(下記「確認ポイント」)

---

## 確認ポイント

| 確認項目 | 期待される挙動 | 関連 PR |
| ---- | ---- | ---- |
| Boot で UI が表示される | タイトル「DrowZzz」+ Turn / Phase / Points / Hand ラベル + Draw / Play / End Turn ボタン | M5-PR3 / PR4 |
| 初回起動(セーブファイルなし) | `BootAsync` が新規対戦を開始し、`Render` で初期状態が表示される | M5-PR4 |
| Draw ボタン | 押すと山札から 1 枚引かれ、Hand ラベルと Deck 残数が更新される | M5-PR4 |
| Play ボタン | 押すと現プレイヤーの手札先頭カードが場に出る(`WaitingForPlay` フェーズで合法な場合) | M5-PR4 |
| End Turn ボタン | 押すとターンが進み、自動セーブされる(次回起動で復元される) | M5-PR4 / PR5 |
| 不合法な手 | ボタンを押しても何も起きず、Console に `Debug.LogWarning` が出る | M5-PR4 |
| 設定 UI(BGM / SE Slider + Language Dropdown) | スライダー / ドロップダウンを操作すると `IUserSettings` に反映される(`PlayerPrefs` 永続化) | M5-PR6 |
| ゲーム終了(Round 21 完了 / 早期勝利) | `outcome-label` に「Winner: p1」または「Draw」が表示され、3 ボタンが disable される | M5-PR7 |

> M5-PR7(`outcome-label` / `RenderOutcome` 本実装)がマージ前の場合、最後の行(ゲーム終了表示)は
> `RenderOutcome` が `Debug.Log` 出力のみになる。M5-PR7 マージ後に `outcome-label` 表示が有効になる。

---

## トラブルシューティング

| 症状 | 原因 | 対処 |
| ---- | ---- | ---- |
| 作業中のシーンが `Untitled` / `名称未設定` のまま | `Main.unity` を作成しただけで開いていない | `Assets/Scenes/Main` をダブルクリックして開き、GameObject を作り直す(Step 2) |
| `ProjectLifetimeScope` GameObject の Inspector に `Game Config` / `Card Catalog` 欄が無い | `GameLifetimeScope` コンポーネントを誤って追加している | コンポーネント `⋮` → `Remove Component` で外し、`ProjectLifetimeScope` を追加し直す(Step 3-1) |
| Console に `Can't Generate Mesh, No Font Asset has been assigned.` | `DrowZzzPanelSettings` の `Theme Style Sheet` 未割り当て | `DrowZzzPanelSettings.asset` を選択し、`Theme Style Sheet` に `UnityDefaultRuntimeTheme`(`Assets/UI Toolkit/UnityThemes/` に自動生成)を割り当てる(Step 1) |
| Play 時に `InvalidOperationException: DrowZzzGameConfigAsset が Inspector で未設定です` | `ProjectLifetimeScope._gameConfig` 未割り当て | Step 4-1 を実施 |
| Play 時に `InvalidOperationException: ScriptableObjectCardCatalog が Inspector で未設定です` | `ProjectLifetimeScope._cardCatalog` 未割り当て | Step 4-1 を実施 |
| Console に `[DrowZzzGameView] UIDocument が Inspector で未設定です` | `DrowZzzGameView._uiDocument` 未割り当て | Step 4-3 を実施 |
| Console に `[DrowZzzGameView] ボタン要素が UXML から見つかりません` | `UIDocument` の Source Asset 未割り当て / `DrowZzzGame.uxml` の name 属性不一致 | Step 4-2 を実施、または UXML の name 属性を確認 |
| UI が真っ白 / 表示されない | `UIDocument` の Panel Settings 未割り当て | Step 1 + Step 4-2 を実施 |
| `DrowZzzGameView` の `Add Component` で見つからない | スクリプトが未コンパイル / コンパイルエラー | Console のエラーを解消し、Unity Editor を再コンパイル(Focus / Cmd+R) |
| ボタンを押しても反応しない | `EventSystem` がシーンにない | Step 3-4 を実施 |
| `GameLifetimeScope` のスコープが `ProjectLifetimeScope` を親と認識しない | GameObject 階層で子になっていない | `GameLifetimeScope` を `ProjectLifetimeScope` の子に配置(Step 3-2) |

---

## 関連

- [ADR-0016 §6 シーン構成と Bootstrap 配置 / §7.2 Bootstrap への注入経路](../adr/0016-m5-bootstrap-presentation.md)
- [`docs/architecture/designer-workflow.md`](designer-workflow.md)(SO Asset の作成 / 編集、M4-PR7)
- [`docs/architecture/webgl-il2cpp-verification.md`](webgl-il2cpp-verification.md)(WebGL Build 検証、M4-PR7)
- 実装: `Assets/_Project/Scripts/Bootstrap/`(`ProjectLifetimeScope` / `GameLifetimeScope`)、`Assets/_Project/Scripts/Presentation/Games/DrowZzz/`(`DrowZzzGameView` / `DrowZzzGamePresenter` / `UserSettingsBinder`)
- UI: `Assets/_Project/UI/Games/DrowZzz/`(`DrowZzzGame.uxml` / `.uss`)
