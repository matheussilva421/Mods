#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
src_dir="$root/src"
out_dir="$root/dist"
out_exe="$out_dir/BioshockRemasteredTrainer.exe"
profiles_out="$out_dir/profiles"
release_dir="$root/release"
cheat_deck_dir="$release_dir/cheat-deck"
portable_dir="$release_dir/portable"
portable_profiles_dir="$portable_dir/profiles"
profile_name="bioshock-remastered.steam-v1.0.122872.json"
cheat_deck_exe_path="$cheat_deck_dir/BioshockRemastered-CheatDeck.exe"
cheat_deck_readme_path="$cheat_deck_dir/LEIA-ME-PTBR.md"

pick_compiler() {
  if [[ -n "${MONO_ROOT:-}" ]]; then
    local mono_bin="$MONO_ROOT/bin/mono"
    local mcs_exe="$MONO_ROOT/lib/mono/4.5/mcs.exe"
    if [[ -x "$mono_bin" && -f "$mcs_exe" ]]; then
      local default_prefix="${MONO_ROOT%%/Library/Frameworks/Mono.framework/Versions/*}"
      export MONO_GAC_PREFIX="${MONO_GAC_PREFIX:-$default_prefix}"
      printf '%s\n%s\n' "$mono_bin" "$mcs_exe"
      return 0
    fi
  fi

  local candidate
  for candidate in mcs mono-csc csc; do
    if command -v "$candidate" >/dev/null 2>&1; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  return 1
}

compiler_spec="$(pick_compiler || true)"
if [[ -z "$compiler_spec" ]]; then
  echo "No supported C# compiler found. Install Mono SDK or another compiler that provides mcs/mono-csc/csc." >&2
  exit 1
fi

compiler=()
if [[ "$compiler_spec" == *$'\n'* ]]; then
  while IFS= read -r line; do
    [[ -n "$line" ]] && compiler+=("$line")
  done <<EOF
$compiler_spec
EOF
else
  compiler=("$compiler_spec")
fi

mkdir -p "$out_dir" "$profiles_out" "$release_dir" "$cheat_deck_dir" "$portable_dir" "$portable_profiles_dir"
rm -f "$profiles_out"/*.json
rm -rf "$cheat_deck_dir"/*
rm -rf "$portable_dir"/*
mkdir -p "$cheat_deck_dir" "$portable_dir" "$portable_profiles_dir"

sources=(
  "$src_dir/Program.cs"
  "$src_dir/NativeMethods.cs"
  "$src_dir/ByteHelper.cs"
  "$src_dir/PatternScanner.cs"
  "$src_dir/EmbeddedProfile.cs"
  "$src_dir/TrainerProfile.cs"
  "$src_dir/ProcessMemory.cs"
  "$src_dir/MainForm.cs"
)

"${compiler[@]}" \
  -nologo \
  -target:winexe \
  -platform:x86 \
  -out:"$out_exe" \
  -r:System.dll \
  -r:System.Drawing.dll \
  -r:System.Windows.Forms.dll \
  -r:System.Web.Extensions.dll \
  "${sources[@]}"

cp "$root/profiles/$profile_name" "$profiles_out/"
cp "$out_exe" "$cheat_deck_exe_path"
cp "$out_exe" "$portable_dir/BioshockRemasteredTrainer.exe"
cp "$root/profiles/$profile_name" "$portable_profiles_dir/"

cat > "$cheat_deck_readme_path" <<'EOF'
# Como usar

Arquivo principal:

- `BioshockRemastered-CheatDeck.exe`

Uso rapido:

1. copie esse `.exe` para o Steam Deck;
2. aponte o Cheat Deck para esse arquivo;
3. abra o jogo e carregue seu save;
4. abra o trainer depois que o save terminar de carregar;
5. use as teclas abaixo.

Hotkeys:

- `F1` God Mode
- `F2` Invisible
- `F3` Lock Consumables
- `F4` 1-Hit Kill Enemy
- `F5` No Alerts
- `F6` Protect Little Sister
- `F7` Unlock Gene Slots
- `F12` Disable all

Observacao:

- esse `.exe` ja tem o perfil embutido;
- para o caso simples, nao precisa copiar pasta `profiles`;
- ao voltar para o menu principal, use `F12` para restaurar os hooks antes de carregar outro save.
EOF

echo "Built trainer to $out_dir"
echo "Prepared Cheat Deck package in $cheat_deck_dir"
echo "Prepared portable package in $portable_dir"
