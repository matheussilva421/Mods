# Guia de Instalacao e Uso (Steam Deck / SteamOS)

Este guia cobre a instalacao e uso do trainer offline para Crysis 3 Remastered.

## Como instalar o Cheat Engine no SteamOS

No Steam Deck, o sistema base e imutavel (read-only). Entao o ideal e instalar ferramentas em `/home/deck` e executar tudo via Desktop Mode.

1. Entre no Desktop Mode.
2. Instale o ProtonUp-Qt pela Discover Store.
3. Abra o ProtonUp-Qt e instale o SteamTinkerLaunch.
4. Reinicie a Steam.
5. No Crysis 3 Remastered, habilite compatibilidade e selecione SteamTinkerLaunch.
6. Baixe uma build Linux do Cheat Engine com o binario `cheatengine-x86_64`.
7. Coloque em um caminho de usuario, por exemplo:
   - `/home/deck/tools/cheatengine`
8. De permissao de execucao:
   - `chmod +x /home/deck/tools/cheatengine/cheatengine-x86_64`
9. Configure o comando custom do STL para:
   - `/home/deck/tools/crysis3-remastered-trainer/steamdeck/stl_launch.sh %command%`
10. Se necessario, configure variaveis de ambiente:
   - `CE_BIN=/home/deck/tools/cheatengine/cheatengine-x86_64`
   - `TABLE_PATH=/home/deck/tools/crysis3-remastered-trainer/crysis3_remastered_basic.ct`

Observacao: versoes atuais do STL nao incluem mais instalacao automatica de Cheat Engine, entao a configuracao do binario e manual.

## Pre-requisitos

- Steam Deck em Desktop Mode
- Crysis 3 Remastered instalado via Steam
- SteamTinkerLaunch (STL) instalado
- Cheat Engine disponivel no ambiente (comando `cheatengine-x86_64`)

## Estrutura do projeto

- `crysis3_remastered_basic.ct`: tabela do Cheat Engine
- `steamdeck/stl_launch.sh`: launcher para abrir CE + iniciar o jogo
- `steamdeck/check_build.sh`: valida se a build instalada continua travada
- `steamdeck/build.lock.example`: modelo de lock de build
- `steamdeck/stl_profile.env`: exemplo de variaveis para STL

## Instalacao

1. Copie este projeto para o Deck, por exemplo:
   - `/home/deck/tools/crysis3-remastered-trainer`
2. Garanta permissao de execucao nos scripts:
   - `chmod +x /home/deck/tools/crysis3-remastered-trainer/steamdeck/*.sh`
3. Crie o lock de build:
   - `cp steamdeck/build.lock.example steamdeck/build.lock`
4. Preencha em `steamdeck/build.lock`:
   - `EXPECTED_BUILD_ID`
   - `EXPECTED_MANIFEST_ID`

## Configuracao do SteamTinkerLaunch

1. Abra as opcoes do jogo no STL.
2. Configure comando customizado para:
   - `/home/deck/tools/crysis3-remastered-trainer/steamdeck/stl_launch.sh %command%`
3. Se necessario, configure variaveis de ambiente com base em `steamdeck/stl_profile.env`:
   - `CE_BIN`
   - `TABLE_PATH`
   - `LOG_FILE`

## Primeiro uso

1. Inicie o jogo pela Steam (com STL ativo).
2. O launcher abrira o Cheat Engine com `crysis3_remastered_basic.ct`.
3. A tabela faz auto-attach em `Crysis3Remastered.exe`.
4. Hotkeys:
   - `F1`: vida infinita
   - `F2`: energia do nanosuit infinita
   - `F3`: municao infinita
   - `F4`: sem recuo
   - `F5`: municao infinita do Arco Predador
   - `F12`: desativar tudo (panic key)

## Ajuste obrigatorio das assinaturas AOB

A tabela foi entregue como template seguro (falha se AOB nao bater). Voce precisa substituir os padroes placeholders para a sua build travada:

- `C3R_HEALTH_WRITE`
- `C3R_ENERGY_WRITE`
- `C3R_AMMO_SUB`
- `C3R_RECOIL_WRITE`
- `C3R_BOW_AMMO_SUB`

Sem isso, os scripts nao ativam.

## Validar build travada

Rode:

```bash
/home/deck/tools/crysis3-remastered-trainer/steamdeck/check_build.sh
```

Se o script falhar, a build mudou e as assinaturas devem ser revalidadas.

## Solucao de problemas

- CE nao abre:
  - Verifique `CE_BIN` e se o binario esta no `PATH`.
- Jogo abre, mas script nao ativa:
  - Assinaturas AOB incorretas ou desatualizadas.
- Build check falha:
  - Atualize `build.lock` somente apos revalidar os 5 scripts na build nova.

## Uso seguro

- Apenas campanha offline (single-player).
- Nao usar em multiplayer.
- Nao inclui bypass de anti-cheat.

