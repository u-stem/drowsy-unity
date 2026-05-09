# CLAUDE.md (drowsy-unity)

このファイルは drowsy-unity プロジェクト固有の Claude / 開発規約を定義する。
ユーザーグローバル CLAUDE.md (`~/.claude/CLAUDE.md`) と矛盾する箇所は本ファイルが優先される。

---

## 1. コメント・ドキュメントの言語

ユーザーグローバル規約は「コメント・コミットメッセージは英語」を採用しているが、本プロジェクトでは以下に上書きする。

| 対象 | 言語 |
| ---- | ---- |
| コード内コメント (`//`, `/* */`, `///`) | **日本語** |
| docstring / XML doc comment | **日本語** |
| コミットメッセージ本文 | **日本語** |
| PR / Issue 本文 | **日本語** |
| README / 設計ドキュメント | **日本語** |
| コード識別子(クラス・メソッド・変数名) | **英語**(従来通り) |
| コミットメッセージの type (feat / fix / docs / refactor / test / chore / build / ci) | **英語**(従来通り) |
| 設定ファイル内のキー・値 | **英語**(YAML / JSON の仕様上必須) |

例:
```csharp
// 山札からカードを 1 枚引いて手札に加える
public Card DrawTopCard(IRandomSource rng) { ... }
```

## 2. テンプレ由来ファイルの扱い

`.gitignore` (`github/gitignore` 由来) / `.gitattributes` (`gitattributes/gitattributes` 由来) のように外部公式テンプレートを採用しているファイルは、ライセンス由来のヘッダーコメント・出典情報・テンプレ本体のコメントを改変しない(将来テンプレを sync する際の競合を避ける)。本プロジェクト固有で追加するセクションのコメントのみ日本語で記述する。

## 3. ユーザーグローバル規約の継承

ユーザーグローバル CLAUDE.md (`~/.claude/CLAUDE.md`) の以下の規約は本プロジェクトでも全て適用する。

- 完了時の必須報告フォーマット
- 出力衛生(個人情報・Public/Private 境界)
- 禁止事項(機密コミット禁止、`*.backup-*` への書き込み禁止、`--no-verify` 禁止 等)
- メモリ運用(auto memory / episodic-memory の 2 層)
- TDD ループと作業フロー
- パッケージ管理優先順位 (JS/TS は bun、Python は uv)

## 4. プロジェクト固有事項

- ターゲット: Unity 6000.4.6f1 / URP 17.4.0 / WebGL 主体
- アーキテクチャ: Domain / Application / Infrastructure / Presentation の asmdef 分割(Phase 1 で構築予定)
- DI / 非同期 / Pub-Sub / Reactive: VContainer + UniTask + MessagePipe + R3 を採用予定
- Unity Cloud Services: 利用しない方針(`organizationId` / `cloudProjectId` は空欄を維持)。Unity Editor を起動した際に自動再リンクされた場合は速やかに Unlink すること

## 5. アーキテクチャ依存ルール

`Assets/_Project/Scripts/` は Clean Architecture 寄りの 4 層 + Bootstrap 構成を採用する。依存方向: `Bootstrap → {Infrastructure, Presentation} → Application → Domain`(逆方向は不可)。

- **Domain** は純粋 C# (`noEngineReferences: true`) で UnityEngine への依存を持たない
- 内側のレイヤは外側を知らない(Domain は Application を知らない、Application は Infrastructure を知らない)
- **Infrastructure → Application** の参照は「Application が定義したインターフェースを Infrastructure が実装する」(Ports & Adapters パターン)目的のみ。Application の具象 UseCase クラスを Infrastructure から直接呼ばない
- **Presentation → Application** も同様にインターフェース経由。Domain への直接参照も許可するが View に Domain エンティティを直接バインドせず Presenter / DTO 経由を推奨
- 依存方向の違反は将来 Roslyn Analyzer または lefthook で機械検出することを検討
