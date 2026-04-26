# Analise do trainer FLiNG +14 - 2026-04-26

## Objetivo

Analisar estaticamente o arquivo externo `BioShock Remastered v1.0-Update 2 Plus 14 Trainer.exe` para entender o que pode inspirar melhorias nos trainers deste repositorio, sem executar o binario e sem copiar implementacao proprietaria.

## Arquivo analisado

- Caminho local: `C:\Users\slvma\Downloads\BioShock Remastered v1.0-Update 2 Plus 14 Trainer.exe`
- Tamanho: `1.087.488` bytes
- SHA256: `4100022E2F4AA38542CCA1753C9B2BFE6D39039763FA131EE14CD202C1BBA107`
- MD5: `2F780BB993817BA4FF4866B174527F19`
- Assinatura digital: ausente
- Windows Defender: varredura customizada executada; nenhum alerta foi retornado no output

## Metadados PE

- Tipo: PE nativo Windows, 32-bit, GUI subsystem
- Machine: `0x014C`
- Timestamp de link: `2016-12-21 11:10:20 UTC`
- CLR/.NET: ausente
- Secoes principais: `.text`, `.rdata`, `.data`, `.rsrc`, `.reloc`
- `FileDescription`: `FLiNG@3DMGAME Presents - BioShock Remastered v1.0-Update 2 Plus 14 Trainer`
- `CompanyName`: `3DMGAME`
- `ProductVersion`: `1.0.433.1`
- `LegalCopyright`: `FLiNG@3DMGAME Copyright (C) 2016`

## Comportamento tecnico inferido

O binario segue o padrao classico de trainer nativo:

- localiza o processo alvo (`Bioshock.exe` / `BioshockHD.exe`);
- enumera processos e modulos com Toolhelp APIs;
- abre o processo com permissao suficiente para leitura/escrita;
- le memoria para localizar versao, padroes ou enderecos;
- escreve memoria e/ou injeta codigo em regioes alocadas;
- usa hotkeys por polling (`GetAsyncKeyState`);
- usa arquivo de configuracao `TrainerSettings.ini`;
- possui suporte a `TrSpeedHack.dll` / `TrSpeedHack_x86.dll` para alteracao de velocidade global.

Imports relevantes observados:

- `OpenProcess`
- `ReadProcessMemory`
- `WriteProcessMemory`
- `VirtualAllocEx`
- `VirtualFreeEx`
- `VirtualQueryEx`
- `CreateRemoteThread`
- `CreateToolhelp32Snapshot`
- `Process32FirstW` / `Process32NextW`
- `Module32FirstW` / `Module32NextW`
- `GetAsyncKeyState`
- `GetPrivateProfileStringW` / `WritePrivateProfileStringW`
- `AdjustTokenPrivileges`

## Sinais de seguranca

Pontos normais para trainer, mas que ainda exigem cuidado:

- o executavel nao e assinado;
- o manifesto contem referencias a `asInvoker` e `requireAdministrator`;
- o proprio binario exibe mensagens pedindo permissao de administrador quando nao consegue anexar ou escrever memoria;
- usa APIs de escrita e alocacao em processo remoto, que tambem sao comuns em malware;
- inclui recursos de musica/UI (`TrainerBGM.mid`) e configuracao local (`TrainerSettings.ini`).

Nao foi observado, nos strings extraidos, um indicador obvio de download HTTP/auto-update malicioso. Ainda assim, o criterio pratico deve ser: nao executar binarios de terceiros fora de ambiente controlado quando o objetivo e apenas aprender padroes de design.

## Funcoes do FLiNG +14

Fontes publicas listam as seguintes opcoes para esta versao:

- `Numpad 1` Infinite Health
- `Numpad 2` Infinite EVE
- `Numpad 3` Infinite Items
- `Numpad 4` No Reload
- `Numpad 5` Infinite Money
- `Numpad 6` Infinite ADAM
- `Numpad 7` Max Wallet Capacity
- `Numpad 8` Super Speed (Movement Speed)
- `Numpad 9` Super Jump
- `Numpad 0` One Hit Kill
- `Alt + Numpad 1/2/3/4` 2x/4x/8x/16x Money
- `Alt + Numpad 5/6/7/8` 2x/4x/8x/16x ADAM
- `Page Up` Super Speed (Game Speed)
- `Page Down` Slow Motion
- `Home` Disable All

Comparacao com o trainer atual do repositorio:

| Area | FLiNG +14 | Trainer atual |
| --- | --- | --- |
| Vida | Infinite Health | God Mode |
| EVE | Infinite EVE | Lock Consumables cobre consumo |
| Itens/ammo | Infinite Items, No Reload | Lock Consumables |
| Dinheiro/ADAM | Infinite + multiplicadores | Lock Consumables / sem multiplicadores dedicados |
| Carteira | Max Wallet Capacity | nao implementado |
| Mobilidade | Player Speed, Super Jump | nao implementado |
| Tempo global | Game Speed / Slow Motion | nao implementado |
| Combate | One Hit Kill | 1-Hit Kill Enemy |
| Stealth/alerta | nao destacado nas fontes publicas | Invisible, No Alerts |
| Little Sister | nao destacado nas fontes publicas | Protect Little Sister |
| Gene Bank | nao destacado nas fontes publicas | Unlock Gene Slots |

## O que vale aproveitar

1. **Paridade de experiencia**

O FLiNG e forte em atalhos previsiveis e opcoes numericas. Seus trainers ja tem WinForms, hotkeys globais e log, mas podem ganhar uma camada de UX com agrupamento por categoria: sobrevivencia, recursos, mobilidade, tempo e sistema.

2. **Diagnostico de ambiente**

O binario externo tem mensagens explicitas para versao nao suportada, jogo nao encontrado, falta de permissao e falha de escrita. O trainer atual ja loga erros, mas pode melhorar a tela principal com diagnostico acionavel:

- processo encontrado ou ausente;
- bitness esperada do jogo;
- permissao insuficiente;
- modulo alvo encontrado;
- padroes encontrados/faltando;
- sugestao de reiniciar jogo/trainer quando o endereco nao for encontrado.

3. **Validacao de versao**

O FLiNG tenta detectar versao do jogo e possui opcao interna `IgnoreGameVersion`. Para o repositorio, o caminho mais seguro e registrar uma matriz de builds suportadas no perfil JSON e mostrar aviso claro quando hash/tamanho/versao do modulo nao baterem.

4. **Configuracao externa**

O FLiNG usa `TrainerSettings.ini`. O repositorio ja usa perfil JSON; vale evoluir isso para:

- hotkeys editaveis por perfil;
- toggles padrao por jogo;
- preferencia de som/notificacao;
- modo Cheat Deck enxuto;
- modo tecnico com logs detalhados.

5. **Recursos faltantes no BioShock**

As maiores lacunas funcionais, em ordem de impacto pratico:

- Max Wallet Capacity;
- multiplicadores de Money/ADAM;
- Super Jump;
- Player Movement Speed;
- Game Speed / Slow Motion;
- No Reload separado de Lock Consumables;
- Infinite EVE/Items/Money/ADAM como toggles separados, mesmo que internamente compartilhem hooks.

## O que evitar

- Nao copiar codigo, recursos visuais, sons ou strings proprietarias.
- Nao depender de execucao do trainer externo como referencia principal.
- Nao transformar cheats em autoativacao ao anexar; manter ativacao manual e `Disable All` seguro.
- Nao esconder falhas: padrao faltando, permissao insuficiente e versao incompatvel devem aparecer claramente.
- Nao usar isso para jogos online ou com anticheat; manter o escopo single-player/local.

## Backlog recomendado

### Fase 1 - Robustez e diagnostico

- Adicionar painel de diagnostico no WinForms:
  - processo alvo;
  - PID;
  - caminho do executavel;
  - modulo alvo;
  - permissao de leitura/escrita;
  - build detectada;
  - ultimo padrao que falhou.
- Reduzir permissao inicial de `ProcessAllAccess` quando possivel.
- Mostrar mensagens de erro curtas na UI e detalhes completos no log.
- Adicionar validacao opcional de build no JSON.

### Fase 2 - UX Cheat Deck

- Criar agrupamento visual por categoria.
- Permitir hotkeys alternativas configuraveis.
- Adicionar comando `Home` como alias de disable-all, mantendo `F12`.
- Padronizar nomes de status entre BioShock, Crysis 2 e Crysis 3.

### Fase 3 - Paridade funcional BioShock

- Separar `Lock Consumables` em toggles menores quando tecnicamente estavel:
  - Infinite EVE;
  - Infinite Items/Ammo;
  - Infinite Money;
  - Infinite ADAM;
  - No Reload.
- Pesquisar e portar Max Wallet Capacity.
- Pesquisar multiplicadores Money/ADAM.
- Pesquisar Player Speed e Super Jump.

### Fase 4 - Tempo global

- Avaliar biblioteca propria simples para speedhack somente se o ganho compensar o risco.
- Preferir solucao explicita, documentada e reversivel.
- Manter Game Speed / Slow Motion desativados por padrao.

## Referencias externas usadas para lista de funcoes

- `https://cheats4game.net/4263-bioshock-remastered-trainer-14-v10-update-2-fling.html`
- `https://vgtimes.com/games/bioshock/files/6727-trainer-14-1.0-update-2-fling.html`
- `https://www.playground.ru/bioshock/cheat/bioshock_remastered_trejner_trainer_14_1_0_update_2_fling-820809`
