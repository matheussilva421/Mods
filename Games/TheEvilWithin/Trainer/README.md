# Trainer do The Evil Within

Trainer Windows x64 para a versao Epic Games de `EvilWithin.exe`, baseado nas tabelas:

- `EvilWithin.CT`
- `The Evil Within v1.1.8.CT`

## Cheats implementados

- `F1` Infinite Health
- `F2` Infinite Stamina
- `F3` Infinite Items
- `F4` Infinite Green Gel
- `F5` Infinite Parts
- `F6` Infinite Keys
- `F7` No Spread
- `F8` Freeze Enemies
- `F12` Disable all active cheats

## Build

Execute:

```powershell
.\build-trainer.ps1
```

O build gera:

- `dist/TheEvilWithinTrainer.exe`
- `dist/profiles/the-evil-within-epic.json`
- `release/cheat-deck/TheEvilWithin-CheatDeck.exe`
- `release/cheat-deck/LEIA-ME-PTBR.md`
- `release/portable/TheEvilWithinTrainer.exe`
- `release/portable/profiles/the-evil-within-epic.json`

## Validacao

O validador confirma o perfil e, quando encontra o executavel alvo, tambem confere se cada assinatura usada pelo trainer aparece uma unica vez:

```powershell
.\tests\validate-trainer.ps1
```

Por padrao ele procura `C:\Users\slvma\Downloads\EvilWithin.exe`. Para usar outro caminho:

```powershell
$env:EVIL_WITHIN_EXE = "D:\Games\TheEvilWithin\EvilWithin.exe"
.\tests\validate-trainer.ps1
```

## Limitacao importante

Os scripts `No Reload` e `No Recoil` das CTs recebidas nao tiveram assinatura literal unica no `EvilWithin.exe` da Epic analisado nesta pasta. Eles ficaram fora desta primeira build para evitar patch em endereco incorreto.
