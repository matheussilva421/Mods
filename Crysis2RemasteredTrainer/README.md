# Crysis 2 Remastered Trainer

This folder contains a standalone Windows trainer executable intended to be launched alongside the game by Cheat Deck on Steam Deck.

## What is implemented

- process auto-attach to `Crysis2Remastered.exe`
- WinForms UI with hotkeys and toggles
- global hotkeys `F1`, `F2`, `F3`, `F4`, `F12`
- AOB pattern scan support
- direct patch support with original-byte restore
- relative-address write support for byte flags
- FR v1.4 based profile in `profiles/crysis2-remastered.fr-v1.4.json`

## Implemented cheats

- `F1` Lock Energy
- `F2` Lock Holster
- `F3` Lock Clip (No Reload)
- `F4` Invisible
- `F12` Disable all active cheats

## Important limitation

This executable now uses real patterns from the Fearless Revolution `Crysis2 REM_v1.4_Released.CT` table, but I have not validated it against a live game process in this environment. The implementation is based on direct patch ports from that table, not on Cheat Engine runtime scripts.

## Build

Run:

```powershell
.\build-trainer.ps1
```

Output:

- `dist/Crysis2RemasteredTrainer.exe`
- `dist/profiles/crysis2-remastered.fr-v1.4.json`
- `dist/profiles/crysis2-remastered.template.json`

## Cheat Deck usage

Point Cheat Deck to launch:

- `Crysis2RemasteredTrainer.exe`

Keep the `profiles` folder beside the executable.
