# Trainer Cheat Deck do Crysis 3 Remastered

Data: 2026-04-19

## Objetivo

Criar um trainer em `.exe` para o Crysis 3 Remastered seguindo o mesmo formato ja usado neste repositorio: WinForms, perfil JSON editavel, perfil embutido no executavel, build PowerShell e pacote final simples para Cheat Deck.

## Fonte tecnica

O trainer foi portado a partir dos arquivos fornecidos:

- `Crysis3 REM_v1.4_Released.CT`
- `Crysis3 REM_Readme.txt`

## Cheats incluidos

- `F1` Lock Energy
- `F2` Lock Holster
- `F3` Lock Clip
- `F4` Lock Health
- `F5` 1-Hit Kill
- `F12` Disable all

## Implementacao

A tabela v1.4 usa um script principal para instalar hooks coletores e flags globais. O trainer preserva essa ideia:

- hooks centrais para energia, vida e stamina;
- hooks especificos de municao para holster e clip;
- flags remotos para ligar/desligar cada cheat sem reinstalar todos os hooks a cada tecla;
- restauracao dos hooks quando todos os cheats relacionados sao desativados.

## Validacao local

Foi executado:

```powershell
.\Games\Crysis3Remastered\Trainer\build-trainer.ps1
```

Resultado:

- `dist/Crysis3RemasteredTrainer.exe`
- `release/cheat-deck/Crysis3Remastered-CheatDeck.exe`
- `release/portable/Crysis3RemasteredTrainer.exe`
- `release/portable/profiles/crysis3-remastered.fr-v1.4.json`

## Limite

O build e o empacotamento foram validados localmente. O comportamento in-game ainda depende da build do jogo casar com os AOBs da tabela `Crysis3 REM_v1.4_Released.CT`.
