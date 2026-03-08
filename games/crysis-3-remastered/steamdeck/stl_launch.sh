#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
TABLE_PATH="${TABLE_PATH:-$SCRIPT_DIR/../crysis3_remastered_basic.ct}"
CE_BIN="${CE_BIN:-cheatengine-x86_64}"
LOG_FILE="${LOG_FILE:-/tmp/crysis3_remastered_ce.log}"

if [[ $# -eq 0 ]]; then
  echo "[trainer] Nenhum comando de jogo fornecido."
  echo "[trainer] Uso esperado: stl_launch.sh %command%"
  exit 1
fi

if [[ ! -f "$TABLE_PATH" ]]; then
  echo "[trainer] Tabela nao encontrada: $TABLE_PATH"
  exit 1
fi

if ! command -v "$CE_BIN" >/dev/null 2>&1; then
  echo "[trainer] Cheat Engine nao encontrado: $CE_BIN"
  echo "[trainer] Defina CE_BIN com o caminho completo do binario."
  exit 1
fi

echo "[trainer] Iniciando CE com tabela: $TABLE_PATH"
"$CE_BIN" "$TABLE_PATH" >"$LOG_FILE" 2>&1 &
CE_PID=$!
echo "[trainer] CE iniciado (pid $CE_PID). Log: $LOG_FILE"

exec "$@"
