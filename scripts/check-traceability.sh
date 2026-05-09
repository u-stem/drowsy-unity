#!/usr/bin/env bash
# 要件トレーサビリティ検証: EARS 仕様 ID ↔ NUnit Test Property の双方向整合性
#
# 検出するもの:
#   - ERROR: テストの Property に存在しない要件 ID (typo / 削除された要件への参照)
#   - ERROR: テスト未対応の要件 ID ([Ubiquitous] / [Optional] マーカーなしの場合)
#   - WARNING: テスト未対応だが [Ubiquitous] / [Optional] マーカーありの要件
#
# CLAUDE.md「6. テスト方針」/「7. 機械検知方針」/ docs/testing-strategy.md を参照

set -euo pipefail

SPEC_DIR="docs/specs"
TEST_DIR="Assets/_Project/Scripts/Tests"

failed=0

# Step 1: EARS 仕様から ID 抽出
#   パターン: 行頭 "- [<MODULE>-<NUMBER>]" にマッチ
spec_lines=$(grep -rhE '^- \[[A-Z]+-[0-9]+\]' "$SPEC_DIR" 2>/dev/null | sort -u || true)

if [ -z "$spec_lines" ]; then
  echo "ℹ EARS 仕様 ID が一つも見つかりませんでした(${SPEC_DIR}/ 配下を確認)"
  exit 0
fi

spec_ids=$(echo "$spec_lines" | grep -oE '\[[A-Z]+-[0-9]+\]' | tr -d '[]' | sort -u)
spec_ids_exempt=$(echo "$spec_lines" | grep -E '\[(Ubiquitous|Optional)\]' | grep -oE '\[[A-Z]+-[0-9]+\]' | tr -d '[]' | sort -u)
spec_ids_required=$(comm -23 <(echo "$spec_ids") <(echo "$spec_ids_exempt"))

# Step 2: テストから Property 属性で参照されている ID 抽出
test_ids=$(grep -rhE 'Property\("Requirement",\s*"[A-Z]+-[0-9]+"\)' "$TEST_DIR" 2>/dev/null | grep -oE '"[A-Z]+-[0-9]+"' | tr -d '"' | sort -u || true)

# Step 3: 双方向検証

# 3a. テストに参照があるが EARS にない ID
orphan_in_tests=$(comm -23 <(echo "$test_ids") <(echo "$spec_ids") | grep -v '^$' || true)
if [ -n "$orphan_in_tests" ]; then
  echo "❌ ERROR: テストに参照されているが EARS 仕様に存在しない要件 ID(typo か削除済要件):"
  echo "$orphan_in_tests" | sed 's/^/   - /'
  failed=1
fi

# 3b. EARS の必須要件でテスト未対応
missing_required=$(comm -23 <(echo "$spec_ids_required") <(echo "$test_ids") | grep -v '^$' || true)
if [ -n "$missing_required" ]; then
  echo "❌ ERROR: テスト未対応の要件 ID(必須、[Ubiquitous]/[Optional] マーカーなし):"
  echo "$missing_required" | sed 's/^/   - /'
  echo "   対処: 対応するテストを追加し [Property(\"Requirement\", \"<ID>\")] を付与してください"
  failed=1
fi

# 3c. Ubiquitous/Optional 要件でテスト未対応(警告レベル)
missing_exempt=$(comm -23 <(echo "$spec_ids_exempt") <(echo "$test_ids") | grep -v '^$' || true)
if [ -n "$missing_exempt" ]; then
  echo "⚠ INFO: テスト未対応の要件 ID([Ubiquitous]/[Optional] マーカーあり、構造的性質として許容):"
  echo "$missing_exempt" | sed 's/^/   - /'
fi

# サマリ
spec_count=$(echo "$spec_ids" | wc -l | tr -d ' ')
test_count=$(echo "$test_ids" | wc -l | tr -d ' ')
echo ""
echo "📊 トレーサビリティ サマリ: 仕様 ID ${spec_count} 件 / テスト Property ID ${test_count} 件"

if [ $failed -eq 0 ]; then
  echo "✓ 整合性 OK"
fi

exit $failed
