# Trainer Cheat Deck - 2026-04-25

## Objetivo

Criar um trainer standalone para BioShock Remastered no mesmo molde dos trainers `.exe` de Crysis 2/3 Remastered, com pacote rapido para Cheat Deck e perfil editavel.

## Fonte tecnica

- `Bioshock_REM_v1.2_Released.CT`
- `Bioshock_REM_Readme.txt`
- Processo alvo: `BioshockHD.exe`
- Build documentada: Steam/GOG `v1.0.122872`
- Build Epic confirmada por `Version.ini`: `Configuration=bioshockpcpatchfinal` / `ChangeNumber=127355`, suporte parcial com `Lock Consumables` bloqueado

## Implementacao

O trainer novo vive em `Games/BioshockRemastered/Trainer/` e segue a mesma organizacao dos demais:

- `src/` para codigo C#/WinForms
- `profiles/` para perfil JSON
- `release/cheat-deck/` para o executavel simples
- `release/portable/` para pacote editavel

O codigo porta os scripts principais da tabela CE para hooks x86 restauraveis:

- God Mode
- Invisible
- Lock Consumables
- 1-Hit Kill Enemy
- No Alerts
- Protect Little Sister
- Unlock Gene Slots

## Decisoes

- O executavel e compilado como `x86`, porque a tabela usa instrucoes e ponteiros x86 do `BioshockHD.exe`.
- O trainer nao autoativa todos os cheats ao anexar no processo; o usuario ativa manualmente o que quiser.
- O trainer detecta `Version.ini`; na build Epic `ChangeNumber=127355`, libera os hooks portados e bloqueia `Lock Consumables`.
- `F12` restaura os hooks ativos e deve ser usado antes de voltar ao menu, carregar outro save ou trocar DLC/main game.
- `1-Hit Kill Enemy` usa valor padrao `1.0` para tornar o toggle util sem UI de edicao de valor.

## Evidencia Epic recebida

O arquivo `Version.ini` enviado pelo usuario em 2026-04-25 contem:

```ini
[Version]
Configuration=bioshockpcpatchfinal
ChangeNumber=127355
```

Esse arquivo confirma a build, mas nao permite portar os hooks sozinho. O `BioshockHD.exe` Epic recebido depois permitiu verificar que os AOBs de Invisible, 1-Hit Kill, No Alerts e Protect Little Sister continuam presentes, enquanto o coletor de player/God Mode e Unlock Gene Slots precisaram de padroes Epic equivalentes. `Lock Consumables` permanece bloqueado na Epic porque varios caminhos de consumo foram recompilados e ainda precisam de port dedicado.

## Validacao

A validacao local obrigatoria e:

```powershell
.\build-trainer.ps1
```

Essa validacao confirma compilacao e geracao dos pacotes. A validacao in-game ainda depende do jogo instalado na build alvo.
