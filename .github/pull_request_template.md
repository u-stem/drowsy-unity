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
- [ ] **M2 効果追加時**:ドロー総数 ≤ 山札サイズ - 初期配布(現状 N=2 × MaxRound 21 × 1 Draw = 42、初期配布 10 を加えた 52 ≤ 山札 56、余裕 4 枚)。1 ターン複数 Draw 効果を追加した場合は再計算し、超過時は山札枯渇シナリオの仕様 ADR を別途起票([ADR-0007 §「山札枯渇」](/docs/adr/0007-m2-detail-card-effects.md) / [TODO #6](/docs/todo.md))
- [ ] **M2 DDP 抽選効果追加時**:DDP 総抽選回数 ≤ プール 39 枚(現状 5 タイミング × N=2 = 10 枚 ≤ 39、余裕 29 枚)。DDP 追加抽選効果を導入した場合は再計算し、超過時はプール枯渇シナリオの仕様 ADR を別途起票([ADR-0009 §3](/docs/adr/0009-m2-m3-dp-and-victory-conditions.md) / [TODO #9](/docs/todo.md))

## レビュー実施

- [ ] Claude Code `code-reviewer` subagent によるレビュー(結果を PR コメントで添付)
- [ ] `/ultrareview`(重要な PR の場合)
- [ ] 自己再読(時間を置いてからの再確認)

## 関連リンク

<Issue / Discussion / 関連 PR / 設計ドキュメント>
