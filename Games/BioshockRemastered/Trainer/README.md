# Trainer do BioShock Remastered

Trainer standalone no mesmo molde dos pacotes de Crysis Remastered deste repositorio.

## Estrutura

- `src/` codigo-fonte WinForms/C#
- `profiles/` perfil editavel do trainer
- `release/cheat-deck/` pacote final mais simples para Steam Deck
- `release/portable/` pacote editavel
- `dist/` saida local de build

## Caminho mais rapido

Se voce quer apenas o pacote final para Cheat Deck, use:

- `release/cheat-deck/BioshockRemastered-CheatDeck.exe`

## Build alvo

- Jogo: BioShock Remastered
- Processo: `BioshockHD.exe`
- Build base da tabela: Steam `v1.0.122872`
- Fonte tecnica: `Bioshock_REM_v1.2_Released.CT` + `Bioshock_REM_Readme.txt`

## Cheats implementados

- `F1` God Mode
- `F2` Invisible
- `F3` Lock Consumables
- `F4` 1-Hit Kill Enemy
- `F5` No Alerts
- `F6` Protect Little Sister
- `F7` Unlock Gene Slots
- `F12` Disable all active hooks

Tambem existem atalhos numericos `1` a `7`, seguindo a mesma ordem.

## Uso recomendado

1. Abra o jogo.
2. Carregue completamente o save.
3. Abra o trainer.
4. Ative apenas os cheats desejados.
5. Antes de voltar ao menu principal, carregar outro save ou trocar DLC/main game, pressione `F12`.

## Build

No Windows:

```powershell
.\build-trainer.ps1
```

Em ambientes com Mono:

```bash
./build-trainer.sh
```

O build gera:

- `dist/BioshockRemasteredTrainer.exe`
- `release/cheat-deck/BioshockRemastered-CheatDeck.exe`
- `release/cheat-deck/LEIA-ME-PTBR.md`
- `release/portable/BioshockRemasteredTrainer.exe`
- `release/portable/profiles/bioshock-remastered.steam-v1.0.122872.json`

## Limitacao importante

O executavel compila e empacota os hooks portados da tabela CE, mas o comportamento real ainda precisa ser validado dentro do jogo alvo. Como a propria tabela avisa, fast travel, cutscenes, retorno ao menu e troca entre DLC/main game podem recriar ponteiros; use `F12` antes dessas transicoes.
