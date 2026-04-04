# Crysis Remastered Offline Trainer Research (Steam Deck / SteamOS)

This repository currently contains a research template for an offline trainer flow using:

- SteamTinkerLaunch (STL)
- Cheat Engine (running in Proton/Wine environment)
- A Cheat Engine table with three toggles:
  - `F1` Infinite Health
  - `F2` Infinite Ammo
  - `F3` No Recoil
  - `F12` Panic key (disable all active entries)

## Current Status

- This repository does not contain a validated working cheat for Crysis 2 Remastered.
- The file `crysis_remastered_basic.ct` is only a template with placeholder AOB signatures.
- If you try to enable the entries as-is, they must fail by design.
- `autoexec.cfg` / `devmode` is not treated here as a working path for Crysis 2 Remastered.
- SteamTinkerLaunch no longer has native Cheat Engine integration; only manual custom-program workflows remain.

## Important

- Single-player offline only.
- No multiplayer support.
- No anti-cheat bypass.
- Build-locked workflow: keep one validated game build and do not auto-update.
- Treat this repository as notes, helper scripts, and a CE table skeleton only.

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

Note: modern STL versions no longer provide built-in Cheat Engine auto-download or native CE support, so the CE binary setup is manual and may still not be enough to produce a working setup for this game.

## What This Repo Actually Provides

1. A CE table skeleton with hotkeys and safe-failing placeholder hooks.
2. A build-lock checker script for Steam installs.
3. A manual STL launcher example for opening CE as a custom external program.
4. Documentation of the limitations discovered so far.

## AOB Signatures (required once per locked build)

The table ships with placeholder signatures for safe failure. You would need to replace each AOB with build-specific bytes from a validated reverse-engineering pass:

- `CR_HEALTH_WRITE` in script `[F1] Infinite Health`
- `CR_AMMO_SUB` in script `[F2] Infinite Ammo`
- `CR_RECOIL_WRITE` in script `[F3] No Recoil`

If AOB is not matched, CE should refuse activation. That is the current expected behavior of this repository.

### Suggested update workflow

1. Open CE and attach to `CrysisRemastered.exe`.
2. Find the relevant instruction with "Find out what writes/accesses this address".
3. Generate a unique AOB around that instruction.
4. Replace both:
   - `aobscanmodule(..., <pattern>)`
   - `assert(..., <exact bytes>)`
5. Validate toggle on the locked build.
6. Keep auto-update disabled for the game.

## Known Limitations

- `autoexec.cfg` and `devmode` are not documented here as working for current Crysis 2 Remastered builds.
- SteamTinkerLaunch built-in CE support is gone, so the old "just use STL + CE" guidance is outdated.
- The current table has not been validated against a current Crysis 2 Remastered Steam build.
- A build lock does not make the table work by itself; it only helps preserve a build after a working table already exists.
