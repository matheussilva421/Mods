# Crysis Remastered Offline Trainer (Steam Deck / SteamOS)

This workspace contains a starter implementation for an offline trainer flow using:

- SteamTinkerLaunch (STL)
- Cheat Engine (running in Proton/Wine environment)
- A Cheat Engine table with three toggles:
  - `F1` Infinite Health
  - `F2` Infinite Ammo
  - `F3` No Recoil
  - `F12` Panic key (disable all active entries)

## Important

- Single-player offline only.
- No multiplayer support.
- No anti-cheat bypass.
- Build-locked workflow: keep one validated game build and do not auto-update.

## Files

- `crysis_remastered_basic.ct`: CE table with Lua auto-attach + AA scripts.
- `steamdeck/stl_launch.sh`: wrapper to launch CE table then start the game command.
- `steamdeck/stl_profile.env`: STL integration profile example.
- `steamdeck/check_build.sh`: validates installed Steam build against a lock file.
- `steamdeck/build.lock.example`: example lock file format.
- `GUIA_INSTALACAO.md`: Portuguese install and usage guide for Steam Deck.

## Installing Cheat Engine on SteamOS

1. Switch to Desktop Mode.
2. Install ProtonUp-Qt from Discover.
3. Open ProtonUp-Qt and install SteamTinkerLaunch.
4. Restart Steam.
5. In Crysis Remastered, enable compatibility and select SteamTinkerLaunch.
6. Download a Linux Cheat Engine build that includes `cheatengine-x86_64`.
7. Put it in a user path, for example:
   - `/home/deck/tools/cheatengine`
8. Set executable permission:
   - `chmod +x /home/deck/tools/cheatengine/cheatengine-x86_64`
9. Configure STL custom command:
   - `/home/deck/tools/crysis-remastered-trainer/steamdeck/stl_launch.sh %command%`
10. Set environment variables if needed:
   - `CE_BIN=/home/deck/tools/cheatengine/cheatengine-x86_64`
   - `TABLE_PATH=/home/deck/tools/crysis-remastered-trainer/crysis_remastered_basic.ct`

Note: modern STL versions no longer provide built-in Cheat Engine auto-download, so the CE binary setup is manual.

## Quick Start (Steam Deck Desktop Mode)

1. Install SteamTinkerLaunch and ensure it is active for Crysis Remastered.
2. Install Cheat Engine in your chosen environment and make sure `cheatengine-x86_64` is callable.
3. Copy this project to the Deck (or clone directly there).
4. Create a lock file from the current validated build:
   - Copy `steamdeck/build.lock.example` to `steamdeck/build.lock`.
   - Fill `APP_ID`, `EXPECTED_BUILD_ID`, and `EXPECTED_MANIFEST_ID`.
5. Use `steamdeck/stl_profile.env` as reference for STL env values.
6. Point STL custom command to:
   - `/home/deck/tools/crysis-remastered-trainer/steamdeck/stl_launch.sh %command%`
7. Start the game via Steam.
8. CE should open with `crysis_remastered_basic.ct`, auto-attach to `CrysisRemastered.exe`, then hotkeys will be available.

## AOB Signatures (required once per locked build)

The table ships with placeholder signatures for safe failure. You must replace each AOB with build-specific bytes:

- `CR_HEALTH_WRITE` in script `[F1] Infinite Health`
- `CR_AMMO_SUB` in script `[F2] Infinite Ammo`
- `CR_RECOIL_WRITE` in script `[F3] No Recoil`

If AOB is not matched, CE should refuse activation (expected behavior).

### Suggested update workflow

1. Open CE and attach to `CrysisRemastered.exe`.
2. Find the relevant instruction with "Find out what writes/accesses this address".
3. Generate a unique AOB around that instruction.
4. Replace both:
   - `aobscanmodule(..., <pattern>)`
   - `assert(..., <exact bytes>)`
5. Validate toggle on the locked build.
6. Keep auto-update disabled for the game.

## Troubleshooting

- CE opens but scripts fail to enable:
  - AOB mismatch. Recreate signatures for your locked build.
- Game starts but CE does not attach:
  - Confirm process name is `CrysisRemastered.exe`.
  - Confirm STL runs `steamdeck/stl_launch.sh` and table path is correct.
- Build lock check fails:
  - Update `steamdeck/build.lock` only after re-validating all three scripts.
