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

  # ファイル全体に [Test] / [UnityTest] が含まれていれば、Category も同じ数以上必要。
  # [TestCase(...)] は 1 メソッドに複数付与されるため grep ベースでは正確なメソッド数を数えられず、
  # ここでは除外する(誤検出を防ぐ。Phase 1+ で Roslyn Analyzer に格上げして対応)。
  # grep -c は行カウントで `[Category("Small"), Category("Normal")]` のような 1 行複数 Category を
  # 過小カウントしてしまうため、`grep -o` で出現単位にカウントする。
  # `//` `///` `*`(block コメント継続行)で始まる行は xmldoc / コメント中の `[Test]` / `[UnityTest]`
  # 言及を誤カウントしないよう除外する。
  non_comment=$(grep -vE '^[[:space:]]*(///?|\*)' "$file" 2>/dev/null || true)
  # grep が 0 件で exit 1 を返すと `set -o pipefail` でスクリプト全体が失敗するため、`|| true` で握り潰す。
  test_count=$(printf '%s\n' "$non_comment" | { grep -oE '\[(Test|UnityTest)[],]' || true; } | wc -l | tr -d ' ')

  if [ "$test_count" -eq 0 ]; then
    continue
  fi

  size_count=$(printf '%s\n' "$non_comment" | { grep -oE 'Category\("(Small|Medium|Large)"\)' || true; } | wc -l | tr -d ' ')
  type_count=$(printf '%s\n' "$non_comment" | { grep -oE 'Category\("(Normal|SemiNormal|Abnormal|SuperNormal)"\)' || true; } | wc -l | tr -d ' ')

  if [ "$size_count" -lt "$test_count" ] || [ "$type_count" -lt "$test_count" ]; then
    echo "❌ $file: [Test] が $test_count 件あるが、Size カテゴリ $size_count 件 / Type カテゴリ $type_count 件しかない"
    echo "   各 [Test] には [Category] を最低 2 つ付けてください:"
    echo "     Size: Small / Medium / Large"
    echo "     Type: Normal / SemiNormal / Abnormal / SuperNormal"
    failed=1
  fi
done

exit $failed
