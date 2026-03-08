#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CFG_SRC="$SCRIPT_DIR/../autoexec.cfg"

if [[ ! -f "$CFG_SRC" ]]; then
  echo "[installer] ERRO: autoexec.cfg nao encontrado em: $CFG_SRC"
  echo "[installer] Certifique-se de que o repositorio esta completo."
  exit 1
fi

CANDIDATE_PATHS=(
  "$HOME/.steam/steam/steamapps/common/Crysis 3 Remastered"
  "$HOME/.local/share/Steam/steamapps/common/Crysis 3 Remastered"
  "/run/media/mmcblk0p1/steamapps/common/Crysis 3 Remastered"
)

GAME_DIR=""
for cand in "${CANDIDATE_PATHS[@]}"; do
  if [[ -d "$cand" ]]; then
    GAME_DIR="$cand"
    break
  fi
done

if [[ -z "$GAME_DIR" ]]; then
  echo "[installer] ERRO: Crysis 3 Remastered nao encontrado."
  echo "[installer] Caminhos verificados:"
  for cand in "${CANDIDATE_PATHS[@]}"; do
    echo "  $cand"
  done
  echo ""
  echo "[installer] Se o jogo esta em outro local, copie manualmente:"
  echo "  cp \"$CFG_SRC\" \"/caminho/para/Crysis 3 Remastered/autoexec.cfg\""
  exit 1
fi

CFG_DST="$GAME_DIR/autoexec.cfg"

if [[ -f "$CFG_DST" ]]; then
  BACKUP="$CFG_DST.bak"
  echo "[installer] AVISO: autoexec.cfg ja existe. Criando backup em: $BACKUP"
  cp "$CFG_DST" "$BACKUP"
fi

cp "$CFG_SRC" "$CFG_DST"

echo ""
echo "[installer] Instalacao concluida!"
echo "[installer] Arquivo instalado em: $CFG_DST"
echo ""
echo "Proximos passos:"
echo "  1. Steam > Crysis 3 Remastered > Propriedades > Opcoes de Inicializacao"
echo "     Adicione: -devmode"
echo "     (necessario para g_godMode / vida infinita funcionar)"
echo "  2. Inicie o jogo normalmente pelo Steam."
echo "  3. Os cheats ativam automaticamente."
echo ""
echo "Para verificar: abra o console no jogo com ~ e digite:"
echo "  g_godMode"
echo "  (deve retornar: g_godMode = 1)"
echo ""
echo "Para desativar um cheat, edite e coloque # na frente da linha:"
echo "  $CFG_DST"
