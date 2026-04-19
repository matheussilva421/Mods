# Trainer do Crysis 3 Remastered

Este projeto esta separado em areas claras de trabalho:

- `src/` codigo-fonte
- `profiles/` perfis editaveis do trainer
- `release/cheat-deck/` pacote final mais simples para Steam Deck
- `release/portable/` pacote editavel
- `dist/` saida local de build

## Caminho mais rapido

Se voce quer apenas o pacote final para Cheat Deck, use:

- `release/cheat-deck/Crysis3Remastered-CheatDeck.exe`

## Cheats implementados

- `F1` Lock Energy
- `F2` Lock Holster
- `F3` Lock Clip (No Reload)
- `F4` Lock Health
- `F5` 1-Hit Kill
- `F12` Disable all active cheats

## Build

Execute:

```powershell
.\build-trainer.ps1
```

O build gera:

- `dist/Crysis3RemasteredTrainer.exe`
- `release/cheat-deck/Crysis3Remastered-CheatDeck.exe`
- `release/cheat-deck/LEIA-ME-PTBR.md`
- `release/portable/Crysis3RemasteredTrainer.exe`
- `release/portable/profiles/crysis3-remastered.fr-v1.4.json`

## Limitacao importante

Este trainer foi baseado na tabela Fearless Revolution `Crysis3 REM_v1.4_Released.CT`. A geracao do executavel e a abertura do pacote final foram validadas aqui, mas o comportamento in-game ainda depende da build alvo do jogo.
