# drowsy-unity

Unity 6 で開発する 2D カードゲーム。汎用カードゲームエンジン基盤を先に構築し、具体的なゲームルールは ScriptableObject と Rule クラスで差し込む設計を採用する。

## ステータス

Phase 0: 環境セットアップ中。

## ターゲット

| 項目 | 設定 |
| ---- | ---- |
| Unity Editor | 6000.4.6f1 |
| Render Pipeline | URP 17.4.0 |
| 主対象プラットフォーム | WebGL |
| 副対象プラットフォーム | StandaloneOSX (検討中) |
| マルチプレイ | 初期対象外 (将来 Mirror / Netcode for GameObjects を選定) |

## 必要環境

- Unity Hub + Unity Editor 6000.4.6f1
- .NET SDK 8 以降 (`dotnet format` 実行用)
- [`uv`](https://github.com/astral-sh/uv) (unity-mcp の Python 実行環境)
- [`lefthook`](https://github.com/evilmartians/lefthook) (pre-commit フック)
- [`gitleaks`](https://github.com/gitleaks/gitleaks) (機密検出)

## ディレクトリ構成

`Assets/_Project/` 配下に Domain / Application / Infrastructure / Presentation を asmdef で分割する設計 (後続フェーズで構築)。

## 開発フロー

- コミット規約: Conventional Commits (`feat`, `fix`, `docs`, `refactor`, `test`, `chore`)
- pre-commit: `lefthook` 経由で `gitleaks` + `dotnet format --verify-no-changes`
- CI: GitHub Actions + GameCI で WebGL ビルドとテストを実行

## ライセンス

未定。
