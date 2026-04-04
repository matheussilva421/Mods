#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
LOCK_FILE="${1:-$SCRIPT_DIR/build.lock}"

if [[ ! -f "$LOCK_FILE" ]]; then
  echo "[build-check] missing lock file: $LOCK_FILE"
  echo "[build-check] copy build.lock.example to build.lock and fill values."
  exit 1
fi

# shellcheck disable=SC1090
source "$LOCK_FILE"

: "${APP_ID:?APP_ID is required in lock file}"
: "${EXPECTED_BUILD_ID:?EXPECTED_BUILD_ID is required in lock file}"
: "${DEPOT_ID:?DEPOT_ID is required in lock file}"

if [[ -n "${APPMANIFEST_PATH:-}" ]]; then
  APPMANIFEST="$APPMANIFEST_PATH"
else
  CANDIDATES=(
    "$HOME/.steam/steam/steamapps/appmanifest_${APP_ID}.acf"
    "$HOME/.local/share/Steam/steamapps/appmanifest_${APP_ID}.acf"
  )

  APPMANIFEST=""
  for cand in "${CANDIDATES[@]}"; do
    if [[ -f "$cand" ]]; then
      APPMANIFEST="$cand"
      break
    fi
  done
fi

if [[ -z "$APPMANIFEST" || ! -f "$APPMANIFEST" ]]; then
  echo "[build-check] appmanifest for app $APP_ID not found."
  exit 1
fi

actual_build_id="$(awk -F'"' '/"buildid"/{print $4; exit}' "$APPMANIFEST")"
if [[ -z "$actual_build_id" ]]; then
  echo "[build-check] could not parse buildid from $APPMANIFEST"
  exit 1
fi

manifest_pattern="\"${DEPOT_ID}\"[[:space:]]*\{"
if [[ -n "${EXPECTED_MANIFEST_ID:-}" ]]; then
  actual_manifest_id="$(awk -v pat="$manifest_pattern" -F'"' '
    $0 ~ pat { in_depot=1; next }
    in_depot && /"manifest"/ { print $4; exit }
    in_depot && /^\s*\}/ { in_depot=0 }
  ' "$APPMANIFEST")"
else
  actual_manifest_id=""
fi

echo "[build-check] appmanifest: $APPMANIFEST"
echo "[build-check] expected buildid:  $EXPECTED_BUILD_ID"
echo "[build-check] installed buildid: $actual_build_id"

if [[ "$actual_build_id" != "$EXPECTED_BUILD_ID" ]]; then
  echo "[build-check] FAIL: buildid mismatch"
  exit 2
fi

if [[ -n "${EXPECTED_MANIFEST_ID:-}" ]]; then
  echo "[build-check] expected manifest(${DEPOT_ID}):  $EXPECTED_MANIFEST_ID"
  echo "[build-check] installed manifest(${DEPOT_ID}): $actual_manifest_id"

  if [[ -z "$actual_manifest_id" ]]; then
    echo "[build-check] FAIL: depot manifest not found for depot $DEPOT_ID"
    exit 3
  fi

  if [[ "$actual_manifest_id" != "$EXPECTED_MANIFEST_ID" ]]; then
    echo "[build-check] FAIL: manifest mismatch"
    exit 4
  fi
fi

echo "[build-check] OK: build lock matches"
