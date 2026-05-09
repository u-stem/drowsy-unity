#!/usr/bin/env bash
# 新規 Domain 配下の *.cs に対応する EARS 仕様 (.md) が存在するか簡易チェック
#
# 規律レベル: Domain 層は仕様駆動開発 (SBE) を必須とするため、
# 対応する docs/specs/domain/<module>/<feature>.md が無い場合エラー。
#
# CLAUDE.md「6. テスト方針」/ docs/testing-strategy.md「1. 仕様駆動開発」を参照

set -euo pipefail

failed=0

for file in "$@"; do
  if [ ! -f "$file" ]; then continue; fi
  case "$file" in
    *.cs) ;;
    *) continue ;;
  esac

  # Domain 配下の .cs のみ対象
  case "$file" in
    Assets/_Project/Scripts/Domain/*) ;;
    *) continue ;;
  esac

  # 例: Assets/_Project/Scripts/Domain/Cards/Pile.cs → docs/specs/domain/cards/pile.md
  rel="${file#Assets/_Project/Scripts/Domain/}"
  module=$(dirname "$rel" | tr '[:upper:]' '[:lower:]')
  feature=$(basename "$rel" .cs | tr '[:upper:]' '[:lower:]')

  spec="docs/specs/domain/${module}/${feature}.md"
  feature_file="docs/specs/domain/${module}/${feature}.feature"

  if [ ! -f "$spec" ]; then
    echo "❌ $file: 対応する EARS 仕様が見つかりません ($spec)"
    echo "   作成方法: cp docs/specs/.template.md $spec"
    failed=1
  fi

  # .feature は推奨のみ(警告だが失敗にしない)
  if [ ! -f "$feature_file" ]; then
    echo "⚠ $file: 対応する Gherkin シナリオがありません ($feature_file)"
    echo "   推奨: cp docs/specs/.template.feature $feature_file"
  fi
done

exit $failed
