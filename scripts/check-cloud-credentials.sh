#!/usr/bin/env bash
# ProjectSettings.asset に Unity Cloud 関連の値が含まれていないかチェック
# (cloudProjectId / organizationId が空欄でない場合エラー)
#
# CLAUDE.md「4. プロジェクト固有事項」: Unity Cloud Services は利用しない方針

set -euo pipefail

target="ProjectSettings/ProjectSettings.asset"

if [ ! -f "$target" ]; then
  exit 0
fi

failed=0

# cloudProjectId に値が含まれている場合エラー
if grep -qE '^[[:space:]]*cloudProjectId:[[:space:]]+\S' "$target"; then
  echo "❌ $target: cloudProjectId に値が含まれています"
  grep -n 'cloudProjectId:' "$target" | head -1
  failed=1
fi

# organizationId に値が含まれている場合エラー
if grep -qE '^[[:space:]]*organizationId:[[:space:]]+\S' "$target"; then
  echo "❌ $target: organizationId に値が含まれています"
  grep -n 'organizationId:' "$target" | head -1
  failed=1
fi

if [ $failed -ne 0 ]; then
  echo ""
  echo "対処方法:"
  echo "  1. Unity Editor > Edit > Project Settings > Services で Unlink"
  echo "  2. もしくは 'git checkout HEAD -- $target' で空欄に戻す"
  exit 1
fi

exit 0
