#!/usr/bin/env bash
# 新規 Domain 配下の *.cs に対応する EARS 仕様が存在するか簡易チェック
#
# 規律レベル: Domain 層は仕様駆動開発 (SBE) を必須とするため、
# 対応する docs/specs/domain/<module>/ ディレクトリに EARS 仕様 (*.md) が
# 1 つ以上存在することを必須とする。
# (同じ機能を複数クラスでカバーする場合は機能単位の spec で 1 ファイルにまとめて良い。
#  例: IRandomSource.cs / XorShiftRandom.cs に対する random-source.md)
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

  # 例外: Compat/ は BCL 不足を補う互換 polyfill 置き場のため仕様駆動の対象外
  # (採用根拠: docs/adr/0004-init-setter-polyfill.md)
  # 例外: Properties/ は assembly attribute(InternalsVisibleTo 等)置き場のため仕様駆動の対象外
  # (post-Phase2 #5 残対応で Properties/AssemblyInfo.cs を追加した際に整理、2026-05-17)
  case "$file" in
    Assets/_Project/Scripts/Domain/Compat/*) continue ;;
    Assets/_Project/Scripts/Domain/Properties/*) continue ;;
  esac

  # 例: Assets/_Project/Scripts/Domain/Cards/Pile.cs → docs/specs/domain/cards/
  rel="${file#Assets/_Project/Scripts/Domain/}"
  module=$(dirname "$rel" | tr '[:upper:]' '[:lower:]')

  spec_dir="docs/specs/domain/${module}"

  # ディレクトリ存在チェック
  if [ ! -d "$spec_dir" ]; then
    echo "❌ $file: 対応する spec ディレクトリが存在しません ($spec_dir/)"
    echo "   作成方法: mkdir -p $spec_dir && cp docs/specs/.template.md $spec_dir/<feature>.md"
    failed=1
    continue
  fi

  # ディレクトリ内に .md ファイル(.template.* 以外)が 1 つ以上必要
  md_count=$(find "$spec_dir" -maxdepth 1 -name "*.md" -not -name ".template.*" 2>/dev/null | wc -l | tr -d ' ')
  if [ "$md_count" -eq 0 ]; then
    echo "❌ $file: $spec_dir/ に EARS 仕様 (*.md) が存在しません"
    echo "   作成方法: cp docs/specs/.template.md $spec_dir/<feature>.md"
    failed=1
  fi

  # .feature の存在は推奨(警告のみ、failed にはしない)
  feature_count=$(find "$spec_dir" -maxdepth 1 -name "*.feature" -not -name ".template.*" 2>/dev/null | wc -l | tr -d ' ')
  if [ "$feature_count" -eq 0 ]; then
    echo "⚠ $file: $spec_dir/ に Gherkin シナリオ (*.feature) が見つかりません(推奨)"
  fi
done

exit $failed
