# ワークフロー

drowsy-unity の開発ワークフロー詳細。CLAUDE.md §8 の補足。

---

## 1. ブランチ戦略

### 永続ブランチ

- `main`: 常にデプロイ可能な状態。**直接 push 禁止**(GitHub branch protection で物理ブロック)

### 一時ブランチ

| プレフィックス | 用途 | 例 |
| ---- | ---- | ---- |
| `feature/` | 新機能 | `feature/pile-domain` |
| `fix/` | 不具合修正 | `fix/draw-empty-pile` |
| `chore/` | 環境整備・依存更新 | `chore/upgrade-r3` |
| `docs/` | ドキュメント | `docs/architecture-rules` |
| `refactor/` | リファクタ | `refactor/rename-pile-to-deck` |
| `test/` | テスト追加・修正 | `test/add-shuffle-cases` |

ブランチ名は kebab-case、簡潔に。長い説明は PR description で行う。

## 2. 標準ワークフロー

```
1. main から feature/<name> ブランチを切る
   git switch -c feature/<name>

2. ローカルで作業 (commit は小さく頻繁に)
   git add ... && git commit -m "feat: ..."

3. push
   git push -u origin feature/<name>

4. PR 作成
   gh pr create --fill --base main

5. Self-Review (Claude code-reviewer subagent または /ultrareview)
   結果を PR コメントに貼り付ける

6. CI が pass (Phase 6 以降)
   - lint / test / coverage / security

7. Squash Merge to main
   - PR description が main の commit message になるよう、
     PR description は Conventional Commits 形式で書く

8. 一時ブランチ削除
   git branch -d feature/<name>
   git push origin --delete feature/<name>  # GitHub 側
```

## 3. Branch Protection 設定

### Phase 0 段階

GitHub の Settings > Branches > Add rule で `main` に対し:

- ☑ Require a pull request before merging
  - ☐ Require approvals: 0(個人開発で Self-Approval 不可のため)
  - ☑ Dismiss stale pull request approvals when new commits are pushed
  - ☑ Require conversation resolution before merging
- ☐ Require status checks to pass before merging(Phase 0 では未設定)
- ☑ Require linear history(履歴を直線的に保つ)
- ☑ Restrict pushes that create matching files(force push 禁止)
- ☑ Do not allow bypassing the above settings(Admin もバイパス禁止)

### Phase 6 完了後の追加

- ☑ Require status checks to pass before merging
  - ☑ Require branches to be up to date before merging
  - 必須チェック:
    - `lint`(dotnet format + Roslyn Analyzer)
    - `test-domain`(Domain.Tests EditMode)
    - `coverage`(C0 / C1 閾値)
    - `security`(gitleaks)
    - `build-webgl`(WebGL ビルド成功)

これにより「すべての CI を pass しない PR は merge 不可」が物理保証される。

## 4. Squash Merge を採用する理由

- 1 feature ブランチでの commit が多数ある場合(WIP / fixup commit 等)、main に取り込む時点で整理する
- main の history が「論理的変更単位」に揃う
- revert 操作が容易(1 commit を revert すれば 1 機能を取り消せる)
- PR description が main の commit message になるため、PR description で文脈を残す

設定: GitHub Settings > General > Pull Requests > **Allow squash merging のみ有効化**(Merge commits / Rebase merging は無効化推奨)

## 5. Conventional Commits

### type 一覧

| type | 用途 |
| ---- | ---- |
| `feat` | 新機能 |
| `fix` | バグ修正 |
| `docs` | ドキュメントのみ |
| `refactor` | 振る舞いを変えないコード変更 |
| `test` | テスト追加・修正 |
| `chore` | ビルド・補助ツール・環境整備 |
| `build` | ビルドシステム・依存変更 |
| `ci` | CI 設定変更 |
| `perf` | パフォーマンス改善 |
| `style` | フォーマット変更 |

### scope(任意)

`feat(domain): カード抽選ロジックを追加` のように、変更範囲を `()` で示す。

### 本文・フッタ

```
<type>(<scope>): <subject>

<body: 何をなぜ変えたかの詳細、複数段落可>

<footer: BREAKING CHANGE / 関連 Issue 等>

Co-Authored-By: ...
```

## 6. Self-Review の実施手順

個人開発でも一定のレビュー証跡を残すため、以下のいずれかを実施:

### A. Claude Code の code-reviewer subagent

```
/code-reviewer 該当 PR の差分を独立コンテキストで深掘りレビュー
```

または、Claude セッション内で `Agent` ツールを使い `subagent_type="code-reviewer"` を指定。

レビュー結果を PR コメントに `gh pr comment <PR#> --body "..."` で貼り付ける。

### B. /ultrareview(課金あり、重要 PR)

multi-agent cloud review。重要な変更(設計判断・破壊的変更・大規模リファクタ)で利用。

### C. 時間を置いた自己再読

最低 30 分以上経ってから自分で diff を読み直す。即座のミスではなく構造的な問題に気付ける。

### 証跡の残し方

すべて PR コメントとして残す:

```bash
gh pr comment <PR#> --body "$(cat <<EOF
## Self-Review (Claude code-reviewer)
$(reviewer の出力)
EOF
)"
```

## 7. Phase 6 で追加される強制

- GitHub Actions + GameCI による CI
- カバレッジ閾値割れで CI 失敗
- branch protection の Required status checks に上記を登録
- これにより「Self-Review チェックリストにチェックを付けただけ」では merge できなくなる

## 8. 参考

- [Conventional Commits](https://www.conventionalcommits.org/ja/v1.0.0/)
- [GitHub Branch Protection Rules](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [GitHub Squash Merging](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/incorporating-changes-from-a-pull-request/about-pull-request-merges#squash-and-merge-your-commits)
