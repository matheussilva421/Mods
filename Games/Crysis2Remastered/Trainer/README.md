# Crysis 2 Remastered Trainer

This project is organized into clear working areas:

- `src/` source code
- `profiles/` editable trainer profiles
- `release/cheat-deck/` simplest final package for Steam Deck
- `release/portable/` editable release package
- `dist/` local build output only

## Fastest path

If you just want the final package for Cheat Deck, use:

- `release/cheat-deck/Crysis2Remastered-CheatDeck.exe`

## Implemented cheats

- `F1` Lock Energy
- `F2` Lock Holster
- `F3` Lock Clip (No Reload)
- `F4` Invisible
- `F5` God Mode
- `F6` 1-Hit Kill
- `F12` Disable all active cheats

## Build

Run:

```powershell
.\build-trainer.ps1
```

The build generates:

- `dist/Crysis2RemasteredTrainer.exe`
- `release/cheat-deck/Crysis2Remastered-CheatDeck.exe`
- `release/cheat-deck/LEIA-ME-PTBR.md`
- `release/portable/Crysis2RemasteredTrainer.exe`
- `release/portable/profiles/crysis2-remastered.fr-v1.4.json`

## Important limitation

This trainer is based on the Fearless Revolution `Crysis2 REM_v1.4_Released.CT` table. The executable and package startup were validated here, but the in-game cheat behavior still needs validation against the target game build.
