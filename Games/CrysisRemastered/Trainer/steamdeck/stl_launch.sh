#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
TABLE_PATH="${TABLE_PATH:-$SCRIPT_DIR/../crysis_remastered_basic.ct}"
CE_BIN="${CE_BIN:-cheatengine-x86_64}"
LOG_FILE="${LOG_FILE:-/tmp/crysis_remastered_ce.log}"

if [[ $# -eq 0 ]]; then
  echo "[trainer] no game command provided."
  echo "[trainer] expected usage: stl_launch.sh %command%"
  exit 1
fi

if [[ ! -f "$TABLE_PATH" ]]; then
  echo "[trainer] table not found: $TABLE_PATH"
  exit 1
fi

if ! command -v "$CE_BIN" >/dev/null 2>&1; then
  echo "[trainer] cheat engine binary not found: $CE_BIN"
  echo "[trainer] set CE_BIN to the full binary path if needed."
  exit 1
fi

echo "[trainer] launching CE table: $TABLE_PATH"
"$CE_BIN" "$TABLE_PATH" >"$LOG_FILE" 2>&1 &
CE_PID=$!
echo "[trainer] CE started (pid $CE_PID). log: $LOG_FILE"

exec "$@"
