# WebGL / IL2CPP 実機検証手順(M4-PR7 で確立)

M4-PR5 で `link.xml` を追加し M4-PR7 の WebGL Build 実機検証に持ち越していた項目(ADR-0012 §「M4-PR5 完成記録」)を、本 M4-PR7 で消化する。本ドキュメントは Unity Editor から WebGL ビルドを行い、`Newtonsoft.Json` / `R3` / Domain / Application / Infrastructure の IL2CPP AOT 動作を確認する手順を残す。

## 検証対象

| 項目 | 確認内容 |
| ---- | ---- |
| **`link.xml` の型保持** | `Newtonsoft.Json` / `Drowsy.Domain` / `Drowsy.Application` / `Drowsy.Infrastructure` の型が WebGL Build 後も削除されない |
| **`DrowZzzGameSessionSerializer.Save / Load`** | WebGL の IndexedDB 経由で `Application.persistentDataPath` 配下に Session JSON が書き込まれ、再起動後に Load で復元できる |
| **`PlayerPrefsUserSettings`** | BGM / SE / Language の getter / setter / Save が WebGL ブラウザの IndexedDB 永続化に乗る |
| **`R3` ReactiveProperty** | `IUserSettings` の Observable が WebGL Build で正常に subscribe / OnNext 動作する |
| **`UniTask`** | `PlayerPrefsUserSettings` 等の async API が WebGL の Single-Thread モデルで動く(ADR-0016 §5 で言及された Thread Pool 制約) |

## 前提

> 注:本手順書は **M4-PR7 PR 全体(複数 commit)が main にマージされた後** に実施する。M4-PR7 第 1 弾 commit(`DrowZzzGameConfigAsset` SO 実装 + 本手順書追加)時点では `.asset` 配置 + WebGL Build 検証は未実施で、ユーザー手作業を待った後段 commit で消化される(M4-PR7 の commit 構造は ADR-0012 §「M4-PR7 完成記録」参照)。

- M4-PR1〜PR6 + M4-PR7 の `DrowZzzGameConfigAsset` 実装が完了し、`.asset` が `Assets/_Project/Data/Configuration/` + `Assets/_Project/Data/Catalogs/` に配置されている
- Unity 6000.4.6f1 / URP 17.4.0 / WebGL ターゲット
- `link.xml`(M4-PR5 で導入)が `Assets/` 直下または `Assets/_Project/` 配下に配置されている

## 検証手順

### 手順 1: WebGL Build 設定の確認

1. Unity Editor 上で `File > Build Profiles` を開く(Unity 6 で `Build Settings` から名称変更、`Cmd/Ctrl+Shift+B` でも可)
2. 左サイドバーの **`Web > Web`**(Unity 6 で `WebGL` から名称変更、内部実装は WebGL 維持)を選択 →(初回は Unity Hub の Add Modules で `WebGL Build Support` インストール → Editor 再起動)
3. Build Profile Configurations から **`Desktop - Development`** を選択して `Add Build Profile`(M4-PR7 第 6 弾で確立)
4. 作成された Profile を Active にし、画面右上の `Player Settings...` で `Project Settings > Player` を開く:
   - **Api Compatibility Level**:.NET Standard 2.1
   - **Managed Stripping Level**:Low(推奨)or Minimal(`link.xml` の動作確認重視)
   - **Strip Engine Code**:OFF(本検証では型ストリッピングを抑えて link.xml の効果を独立に確認)
   - **Publishing Settings > Compression Format**:Gzip(M4-PR7 第 6 弾で初期確定、後で Brotli に切替可)

### 手順 2: 空 Scene からの最小 Build(Bootstrap 未実装でも build 通過を確認)

M5-PR1 着手前は Bootstrap シーンが未実装のため、**任意の空シーンを 1 つ Scene List に追加**して Build を通す。本検証の目的は「型 stripping が link.xml で防がれているか」 + 「`DrowZzzGameSessionSerializer` / `PlayerPrefsUserSettings` の依存型が含まれるか」を確認するもので、Play 操作は M5-PR3 以降。

1. 空 Scene(`Scenes/Sandbox.unity` 等)を Hierarchy で作成し、Build Profiles ウィンドウ左サイドバーの `Scene List` から `Add Open Scenes` で追加
2. Build & Run を実行(出力フォルダは `Builds/WebGL/`)
3. Unity Console / Build ログにエラーが出ないことを確認
4. `Builds/WebGL/Build/*.data` などの WebGL artifact が生成されることを確認

### 手順 3: 型保持の検証(ILSpy / dotPeek / WebAssembly inspector)

WebGL Build の WebAssembly バイナリ内に Domain / Application / Infrastructure / Newtonsoft.Json の型が残っているかを確認する(Editor の Build Report 経由 or `Library/Bee/artifacts/WebGL/` の中間成果物)。

簡易確認:
- `Window > Analysis > Build Report` で `link.xml` 経由保持された型を一覧
- `Drowsy.Domain` / `Drowsy.Application` / `Drowsy.Infrastructure` / `Newtonsoft.Json` の型が **stripped されていない** こと

### 手順 4: ブラウザ実行(任意、M5-PR3 以降の本格 UI 後に再実施推奨)

M4-PR7 範囲では実 Play できる UI がないため、本ステップは **M5-PR8 で WebGL 本格検証時に併走** する。M4-PR7 内では Build 通過 + 型保持確認までを完了基準とする(JIT 確定 2026-05-13、ADR-0012 §「M4-PR7 着手時の項目」)。

## 既知の制約 / 注意点

| 制約 | 対応 |
| ---- | ---- |
| `OnApplicationQuit` 非発火 | WebGL ではタブ閉じ時に発火しない、Auto-save を `EndTurn` 後のみで担保(ADR-0016 §8)|
| Thread Pool 制限 | `UniTask.RunOnThreadPool` が実質 Main Thread fallback、I/O ブロックが目立つ場合は `UniTask.Yield(PlayerLoopTiming.LastUpdate)` 挿入(ADR-0016 §5.2)|
| IndexedDB の同期 I/O 不可 | `PlayerPrefs.Save()` / `File.WriteAllText` は同期 API だが WebGL では async に強制される(Unity 仕様) |

## チェックリスト(M4-PR7 完成時)

- [ ] Player Settings(Stripping Level / Api Compat / Compression)が確定値で記録されている
- [ ] WebGL Build が **Error 0 件** で通る(Warning は許容、本 PR description に列挙)
- [ ] Build Report で `Drowsy.Domain` / `Drowsy.Application` / `Drowsy.Infrastructure` / `Newtonsoft.Json` の型が含まれることを確認
- [ ] Build 出力サイズが許容範囲(`Builds/WebGL/Build/*.wasm.gz` が ~20-30MB 以内、過剰膨張がない)
- [ ] 本ドキュメントの「既知の制約」を M5-PR5 / M5-PR8 への申し送り項目として PR description に明記
- [ ] スクリーンショット 1 枚(Build Report or Build 成功 Console)を PR description に添付

## 検証結果(M5-PR8 = Phase 2 完結 PR、2026-05-16)

M5-PR1〜PR7(VContainer / UI Toolkit / R3 / UniTask 統合)+ PR #94(scene-setup 手順書)+ PR #95(ADR-0017 PlayerRoster wrapper)+ PR #96(ADR-0018 CardTypeId 分離)完了後の WebGL Build 検証結果。**M5 完成定義「Play モード操作」充足の前提として、WebGL Build が引き続き成功することを確認**。

### Build サマリ(オーナー実機、2026-05-16 17:22)

| 項目 | 値 |
| ---- | ---- |
| Result | **Success**(Editor.log: `Build Finished, Result: Success.`) |
| 完全 Build 時間 | **52.5 秒**(PlayerBuildInfo `duration: 52513`、内訳:Postprocess built player 38.4s + ProducePlayerScriptAssemblies 10.7s + Writing asset files 1.6s + Creating compressed player package 0.8s + Building scenes 0.4s + その他) |
| Incremental Build 時間 | **9.6 秒**(2 回目以降の差分 Build) |
| 出力先 | `Builds/Web/Build/WebGL/`(Build Profile による出力、M4-PR7 時点の `Builds/Web/Build/` 直下出力から場所が変わったのは Build Profile 設定差) |
| 出力サイズ合計 | **88 MB**(`Builds/Web/Build/WebGL/` 配下、`WebGL.data` + `WebGL.framework.js` + `WebGL.wasm` + `WebGL.loader.js` + `TemplateData/` + `index.html`) |
| Error 数 | 0 |
| Warning 数 | 0(本 PR スコープ内、`Stripping Runtime Debug Shader Variants` 注記は info レベル) |

### M4-PR7 検証(2026-05-14)との差分

| 項目 | M4-PR7(空 Scene + 最小実装) | M5-PR8(本格 Scene + UI + Persistence + VContainer + R3 + UniTask) | 差分評価 |
| ---- | ---- | ---- | ---- |
| Build 時間 | 59 秒 | 52.5 秒 | ほぼ誤差範囲(scene が小さくて差が出にくい / M5 で増えた asset は SO 2 種 + UXML 2 種 + UI Toolkit 1 theme 程度) |
| 出力サイズ | 確認なし(空 Scene 想定で type 保持確認が主目的) | 88 MB | M5 範囲で実用的サイズ |
| Result | Success | Success | 一貫 |

M5-PR1〜PR7 + PR #94 / #95 / #96 の追加実装(VContainer 1.17.0 / UniTask 2.5.10 / R3 1.3.0 / UI Toolkit / Newtonsoft.Json 永続化 / 全 Domain refactor)が **WebGL IL2CPP Build に影響なし** であることを確認。

### Phase 2 完了基準(ADR-0005 §7)との関係

本検証により ADR-0005 §7 Phase 2 完了の最小定義のうち「WebGL Build が継続して通る」要件を達成。残る軸(Play モード操作)はオーナー側で M5-PR7 / PR #96 マージ後の実機検証で達成済(Hand 重複検出エラーの根本対処後)。Phase 2 完結条件充足。

## 関連

- ADR: [ADR-0012 §「M4-PR5 完成記録」](../adr/0012-m4-scriptableobject-and-persistence.md) — `link.xml` 導入 + WebGL 検証を M4-PR7 へ持ち越し
- ADR: [ADR-0016 §5.2 / §8](../adr/0016-m5-bootstrap-presentation.md) — M5 で WebGL 制約に踏み込む申し送り
- Spec: [`docs/specs/infrastructure/persistence/`](../specs/infrastructure/persistence/) — Serializer 仕様
