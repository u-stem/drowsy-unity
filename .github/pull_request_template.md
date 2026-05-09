## 変更概要

<このPRで何を変えるか、1〜3 行で>

## 変更の動機

<なぜ必要か、関連 Issue / 設計判断 / ADR>

## レビュー観点

<レビュアー(または再読する未来の自分)に見てほしい点>

## Self-Review チェックリスト

### 必須項目
- [ ] Conventional Commits 規約に従っている (`feat / fix / docs / refactor / test / chore / build / ci`)
- [ ] 機密情報(handle / メール / Cloud ID / API key)を含まない
- [ ] CLAUDE.md / docs/ の対応する記述を更新した(該当する場合)
- [ ] Phase 0 完了後: lefthook の pre-commit が pass

### 該当する場合のみ
- [ ] テストを追加 / 更新した(コード変更時)
- [ ] EARS / .feature を追加 / 更新した(Domain 変更時)
- [ ] カバレッジ目標を維持(Domain C1 100%、Application 80%)
- [ ] Roslyn Analyzer の警告ゼロ(Phase 4C-2 以降)
- [ ] asmdef の references / dependency-rules.md と整合(レイヤ追加時)

## レビュー実施

- [ ] Claude Code `code-reviewer` subagent によるレビュー(結果を PR コメントで添付)
- [ ] `/ultrareview`(重要な PR の場合)
- [ ] 自己再読(時間を置いてからの再確認)

## 関連リンク

<Issue / Discussion / 関連 PR / 設計ドキュメント>
