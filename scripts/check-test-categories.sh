#!/usr/bin/env bash
# [Test] 属性に Size/Type の [Category] が 2 つ以上付いているか簡易チェック
#
# Phase 0 段階では grep ベース簡易検知のため誤検出/見逃しの可能性あり。
# Phase 1 以降でカスタム Roslyn Analyzer に格上げ予定。
#
# CLAUDE.md「6. テスト方針」/ docs/testing-strategy.md「2. テスト分類」を参照

set -euo pipefail

failed=0

for file in "$@"; do
  if [ ! -f "$file" ]; then continue; fi
  case "$file" in
    *.cs) ;;
    *) continue ;;
  esac

  # ファイル全体に [Test] が含まれていれば、Category も同じ数以上必要
  test_count=$(grep -cE '\[Test[],]' "$file" 2>/dev/null || true)

  if [ "$test_count" -eq 0 ]; then
    continue
  fi

  size_count=$(grep -cE 'Category\("(Small|Medium|Large)"\)' "$file" 2>/dev/null || true)
  type_count=$(grep -cE 'Category\("(Normal|SemiNormal|Abnormal|SuperNormal)"\)' "$file" 2>/dev/null || true)

  if [ "$size_count" -lt "$test_count" ] || [ "$type_count" -lt "$test_count" ]; then
    echo "❌ $file: [Test] が $test_count 件あるが、Size カテゴリ $size_count 件 / Type カテゴリ $type_count 件しかない"
    echo "   各 [Test] には [Category] を最低 2 つ付けてください:"
    echo "     Size: Small / Medium / Large"
    echo "     Type: Normal / SemiNormal / Abnormal / SuperNormal"
    failed=1
  fi
done

exit $failed
