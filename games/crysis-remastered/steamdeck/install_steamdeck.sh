#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CFG_SRC="$SCRIPT_DIR/../autoexec.cfg"

if [[ ! -f "$CFG_SRC" ]]; then
  echo "[installer] autoexec.cfg nao encontrado em: $CFG_SRC"
  echo "[installer] certifique-se de que o repositorio esta completo."
  exit 1
fi

CANDIDATE_PATHS=(
  "$HOME/.steam/steam/steamapps/common/Crysis Remastered"
  "$HOME/.local/share/Steam/steamapps/common/Crysis Remastered"
)

GAME_DIR=""
for cand in "${CANDIDATE_PATHS[@]}"; do
  if [[ -d "$cand" ]]; then
    GAME_DIR="$cand"
    break
  fi
done

if [[ -z "$GAME_DIR" ]]; then
  echo "[installer] Crysis Remastered nao encontrado. Caminhos verificados:"
  for cand in "${CANDIDATE_PATHS[@]}"; do
    echo "  $cand"
  done
  echo ""
  echo "[installer] Se o jogo esta em outro local (ex.: cartao SD), copie manualmente:"
  echo "  cp \"$CFG_SRC\" \"/caminho/para/Crysis Remastered/autoexec.cfg\""
  exit 1
fi

CFG_DST="$GAME_DIR/autoexec.cfg"

if [[ -f "$CFG_DST" ]]; then
  echo "[installer] AVISO: autoexec.cfg ja existe em: $CFG_DST"
  echo "[installer] O arquivo sera substituido."
fi

cp "$CFG_SRC" "$CFG_DST"

echo ""
echo "[installer] Instalacao concluida!"
echo "[installer] Arquivo copiado para: $CFG_DST"
echo ""
echo "Proximos passos:"
echo "  1. Abra o Steam > Crysis Remastered > Propriedades > Opcoes de Inicializacao"
echo "     Adicione: -devmode"
echo "     (necessario para g_godMode / vida infinita funcionar)"
echo "  2. Inicie o jogo normalmente pela Steam."
echo "  3. Os cheats ativam automaticamente ao iniciar."
echo ""
echo "Para verificar: abra o console no jogo com a tecla ~ (til) e digite:"
echo "  g_godMode"
echo "  (deve retornar: g_godMode = 1)"
echo ""
echo "Para desativar um cheat especifico, edite o arquivo e coloque # na frente da linha:"
echo "  $CFG_DST"
