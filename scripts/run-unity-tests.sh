#!/usr/bin/env bash
# Unity CLI で Test Runner を batch 実行する script(ADR-0012 / M4-PR2 で確立、2026-05-13)。
#
# Unity Editor を起動せずに CI / ローカルで NUnit テストを実行できる。
# `dotnet build` は型解決エラー(CS0246 等)を pre-commit で検出するが、テスト実行は
# Unity Test Framework が必須のため本 script を独立に整備する(CLAUDE.md §7「機械検知方針」)。
#
# 使い方:
#   bash scripts/run-unity-tests.sh                  # 引数なし → EditMode 全テスト
#   bash scripts/run-unity-tests.sh EditMode         # 明示的に EditMode
#   bash scripts/run-unity-tests.sh PlayMode         # PlayMode(Unity Editor の Test Runner 経由と同等)
#
# 出力:
#   Temp/test-results/<Platform>.xml   NUnit 3 XML 形式のテスト結果
#   Temp/test-results/<Platform>.log   Unity Editor のログ全文
#
# Exit code:
#   0  全テスト PASS
#   2  1 件以上の FAIL or Build エラー
#   その他  Unity CLI 起動失敗 / unknown error
#
# 注:
# - Unity の `-batchmode -quit -runTests` は同期実行(数十秒〜数分)。pre-commit には重すぎるため
#   手動 / CI レイヤーで利用する(`lefthook.yml` 統合は採用しない、M4-PR2 JIT 確定 2026-05-13)。
# - macOS 想定。Windows / Linux ユーザーは UNITY_PATH の自動検出ロジックを拡張する必要あり。

set -euo pipefail

UNITY_VERSION="6000.4.6f1"
UNITY_PATH="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
PROJECT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
RESULTS_DIR="${PROJECT_PATH}/Temp/test-results"

# テスト platform(default: EditMode、Pure C# 寄りで高速)
PLATFORM="${1:-EditMode}"

if [[ "$PLATFORM" != "EditMode" && "$PLATFORM" != "PlayMode" ]]; then
  echo "❌ 不正な platform: '$PLATFORM'(EditMode / PlayMode のいずれかを指定)" >&2
  exit 1
fi

if [[ ! -x "$UNITY_PATH" ]]; then
  echo "❌ Unity ${UNITY_VERSION} が見つかりません: $UNITY_PATH" >&2
  echo "   Unity Hub で ${UNITY_VERSION} をインストールしてください。" >&2
  exit 1
fi

mkdir -p "$RESULTS_DIR"
RESULTS_XML="${RESULTS_DIR}/${PLATFORM}.xml"
LOG_FILE="${RESULTS_DIR}/${PLATFORM}.log"

echo "🧪 Unity ${UNITY_VERSION} ${PLATFORM} test runner を実行中..."
echo "   プロジェクト: $PROJECT_PATH"
echo "   結果 XML:    $RESULTS_XML"
echo "   ログ:        $LOG_FILE"
echo ""

# Unity CLI の exit code はテスト失敗時に 2、Build エラー時に 1、起動失敗で他値。
# set -e の早期 abort を一時無効化して exit code を捕捉する。
# 注: `-runTests` は完了後 Unity Editor が自動終了するため `-quit` は理論上冗長だが、
# 一部の Unity 6 マイナーリリースで `-runTests` 単独だと終了しない事例が報告されており、
# 互換性のため `-quit` を保持する(GameCI も同じ二重指定を採用)。
set +e
"$UNITY_PATH" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -runTests \
  -testPlatform "$PLATFORM" \
  -testResults "$RESULTS_XML" \
  -logFile "$LOG_FILE"
EXIT_CODE=$?
set -e

echo ""
if [[ $EXIT_CODE -eq 0 ]]; then
  echo "✓ 全テスト PASS(exit code: 0)"
elif [[ $EXIT_CODE -eq 2 ]]; then
  echo "❌ 1 件以上の FAIL(exit code: 2)— $RESULTS_XML を確認してください"
else
  echo "⚠ 不明な exit code: $EXIT_CODE(ログ $LOG_FILE 確認)"
fi

exit $EXIT_CODE
