#!/usr/bin/env bash
# Unity Editor 起動なしに AssetDatabase を refresh して `.meta` + `.csproj` を機械的に再生成する script
# (CLAUDE.md §7 機械検知方針、2026-05-13 で確立)。
#
# Unity 標準では Editor を起動 → Auto-refresh で .meta / csproj が自動更新される。Editor を閉じている時 /
# CI / 完全自動化したい場合に本 script を使う。
#
# 使い方:
#   bash scripts/refresh-unity-assets.sh
#
# 前提:
# - Unity Editor が **閉じている** こと(Unity は 1 プロジェクト 1 プロセス制約、起動中は本 script 失敗)
# - Unity 6000.4.6f1 が Unity Hub 経由でインストール済
#
# Exit code:
#   0  refresh 成功(.meta / csproj 更新済)
#   それ以外  起動失敗 / ライセンス未認証 / Unity が起動中で競合 等
#
# 注:
# - 数十秒〜数分かかる(Unity Editor startup + AssetDatabase scan)。**pre-commit には統合しない**
#   (毎 commit に重すぎる、JIT 確定 2026-05-13)。手動 / CI レイヤーで利用する。
# - **Editor を常駐させる方が標準ワークフロー**(Auto-refresh が自動で走るため本 script 不要)。
#   本 script は CI / 完全機械化したい時のオプション扱い。
# - macOS 想定。Windows / Linux ユーザーは UNITY_PATH の自動検出ロジックを拡張する必要あり。

set -euo pipefail

UNITY_VERSION="6000.4.6f1"
UNITY_PATH="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
PROJECT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
LOG_FILE="${PROJECT_PATH}/Temp/refresh-assets.log"

if [[ ! -x "$UNITY_PATH" ]]; then
  echo "❌ Unity ${UNITY_VERSION} が見つかりません: $UNITY_PATH" >&2
  echo "   Unity Hub で ${UNITY_VERSION} をインストールしてください。" >&2
  exit 1
fi

mkdir -p "$(dirname "$LOG_FILE")"

echo "🔄 Unity ${UNITY_VERSION} batchmode で AssetDatabase を refresh 中..."
echo "   プロジェクト: $PROJECT_PATH"
echo "   ログ:        $LOG_FILE"
echo "   注:Unity Editor が起動中だと「複数の Unity を起動して同じプロジェクトを開くことはできません」"
echo "       エラーで失敗します。Editor を閉じてから再実行してください。"
echo ""

set +e
"$UNITY_PATH" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -logFile "$LOG_FILE"
EXIT_CODE=$?
set -e

echo ""
if [[ $EXIT_CODE -eq 0 ]]; then
  echo "✓ AssetDatabase refresh 成功(.meta / .csproj 更新済、log: $LOG_FILE)"
else
  echo "❌ refresh 失敗(exit code: $EXIT_CODE、log: $LOG_FILE を確認)"
fi

exit $EXIT_CODE
