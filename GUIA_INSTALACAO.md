# Guia de Instalação e Uso (Steam Deck / SteamOS)

Este guia cobre a instalação e uso do trainer offline para Crysis Remastered.

## Pré-requisitos

- Steam Deck em Desktop Mode
- Crysis Remastered instalado via Steam
- SteamTinkerLaunch (STL) instalado
- Cheat Engine disponível no ambiente (comando `cheatengine-x86_64`)

## Estrutura do projeto

- `crysis_remastered_basic.ct`: tabela do Cheat Engine
- `steamdeck/stl_launch.sh`: launcher para abrir CE + iniciar o jogo
- `steamdeck/check_build.sh`: valida se a build instalada continua travada
- `steamdeck/build.lock.example`: modelo de lock de build
- `steamdeck/stl_profile.env`: exemplo de variáveis para STL

## Instalação

1. Copie este projeto para o Deck, por exemplo:
   - `/home/deck/tools/crysis-remastered-trainer`
2. Garanta permissão de execução nos scripts:
   - `chmod +x /home/deck/tools/crysis-remastered-trainer/steamdeck/*.sh`
3. Crie o lock de build:
   - `cp steamdeck/build.lock.example steamdeck/build.lock`
4. Preencha em `steamdeck/build.lock`:
   - `EXPECTED_BUILD_ID`
   - `EXPECTED_MANIFEST_ID`

## Configuração do SteamTinkerLaunch

1. Abra as opções do jogo no STL.
2. Configure comando customizado para:
   - `/home/deck/tools/crysis-remastered-trainer/steamdeck/stl_launch.sh %command%`
3. Se necessário, configure variáveis de ambiente com base em `steamdeck/stl_profile.env`:
   - `CE_BIN`
   - `TABLE_PATH`
   - `LOG_FILE`

## Primeiro uso

1. Inicie o jogo pela Steam (com STL ativo).
2. O launcher abrirá o Cheat Engine com `crysis_remastered_basic.ct`.
3. A tabela faz auto-attach em `CrysisRemastered.exe`.
4. Hotkeys:
   - `F1`: vida infinita
   - `F2`: munição infinita
   - `F3`: sem recuo
   - `F12`: desativar tudo (panic key)

## Ajuste obrigatório das assinaturas AOB

A tabela foi entregue como template seguro (falha se AOB não bater). Você precisa substituir os padrões placeholders para a sua build travada:

- `CR_HEALTH_WRITE`
- `CR_AMMO_SUB`
- `CR_RECOIL_WRITE`

Sem isso, os scripts não ativam.

## Validar build travada

Rode:

```bash
/home/deck/tools/crysis-remastered-trainer/steamdeck/check_build.sh
```

Se o script falhar, a build mudou e as assinaturas devem ser revalidadas.

## Solução de problemas

- CE não abre:
  - Verifique `CE_BIN` e se o binário está no `PATH`.
- Jogo abre, mas script não ativa:
  - Assinaturas AOB incorretas ou desatualizadas.
- Build check falha:
  - Atualize `build.lock` somente após revalidar os 3 scripts na build nova.

## Uso seguro

- Apenas campanha offline (single-player).
- Não usar em multiplayer.
- Não inclui bypass de anti-cheat.
