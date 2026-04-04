# Crysis 2 Remastered Trainer

This folder contains a standalone Windows trainer executable intended to be launched alongside the game by Cheat Deck on Steam Deck.

## What is implemented

- process auto-attach to `Crysis2Remastered.exe`
- WinForms UI with hotkeys and toggles
- global hotkeys `F1`, `F2`, `F3`, `F4`, `F5`, `F6`, `F12`
- AOB pattern scan support
- direct patch support with original-byte restore
- relative-address write support for byte flags
- code-cave hook support for cheats that need runtime logic
- FR v1.4 based profile in `profiles/crysis2-remastered.fr-v1.4.json`

## Implemented cheats

- `F1` Lock Energy
- `F2` Lock Holster
- `F3` Lock Clip (No Reload)
- `F4` Invisible
- `F5` God Mode
- `F6` 1-Hit Kill
- `F12` Disable all active cheats

## Important limitation

This executable is now based on real patterns and logic extracted from the Fearless Revolution `Crysis2 REM_v1.4_Released.CT` table. I still have not runtime-validated it against a live game process in this environment, so final validation still needs to happen on the target game build.

## Build

Run:

```powershell
.\build-trainer.ps1
```

Output:

- `dist/Crysis2RemasteredTrainer.exe`
- `dist/profiles/crysis2-remastered.fr-v1.4.json`

## Cheat Deck usage

Point Cheat Deck to launch:

- `Crysis2RemasteredTrainer.exe`

Keep the `profiles` folder beside the executable.
