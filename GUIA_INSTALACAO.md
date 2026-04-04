# Guia de Instalacao e Uso (Steam Deck / SteamOS)

Este guia foi reduzido para refletir o estado real do projeto.

No estado atual, este repositorio nao entrega um cheat funcional validado para Crysis 2 Remastered. Ele entrega apenas:

- uma tabela `.ct` de template com assinaturas placeholder
- scripts auxiliares de launch e build lock
- anotacoes sobre as limitacoes atuais

## Estado atual

- `autoexec.cfg` / `devmode` nao esta tratado aqui como caminho funcional para Crysis 2 Remastered.
- O SteamTinkerLaunch nao tem mais suporte nativo ao Cheat Engine.
- O arquivo `crysis_remastered_basic.ct` falha por design ate que alguem substitua os AOBs placeholder por assinaturas reais e validadas.

## Como instalar o Cheat Engine no SteamOS

No Steam Deck, o sistema base e imutavel (read-only). Entao o ideal e instalar ferramentas em `/home/deck` e executar tudo via Desktop Mode.

1. Entre no Desktop Mode.
2. Instale o ProtonUp-Qt pela Discover Store.
3. Abra o ProtonUp-Qt e instale o SteamTinkerLaunch.
4. Reinicie a Steam.
5. No Crysis Remastered, habilite compatibilidade e selecione SteamTinkerLaunch.
6. Baixe uma build Linux do Cheat Engine com o binario `cheatengine-x86_64`.
7. Coloque em um caminho de usuario, por exemplo:
   - `/home/deck/tools/cheatengine`
8. De permissao de execucao:
   - `chmod +x /home/deck/tools/cheatengine/cheatengine-x86_64`
9. Configure o comando custom do STL para:
   - `/home/deck/tools/crysis-remastered-trainer/steamdeck/stl_launch.sh %command%`
10. Se necessario, configure variaveis de ambiente:
   - `CE_BIN=/home/deck/tools/cheatengine/cheatengine-x86_64`
   - `TABLE_PATH=/home/deck/tools/crysis-remastered-trainer/crysis_remastered_basic.ct`

Observacao: versoes atuais do STL nao incluem mais integracao nativa com Cheat Engine. No melhor caso, ele pode ser aberto como programa externo/custom.

## Pre-requisitos

- Steam Deck em Desktop Mode
- Crysis Remastered instalado via Steam
- SteamTinkerLaunch (STL) instalado
- Cheat Engine disponivel no ambiente (comando `cheatengine-x86_64`)

## Estrutura do projeto

- `crysis_remastered_basic.ct`: tabela do Cheat Engine
- `steamdeck/stl_launch.sh`: launcher para abrir CE + iniciar o jogo
- `steamdeck/check_build.sh`: valida se a build instalada continua travada
- `steamdeck/build.lock.example`: modelo de lock de build
- `steamdeck/stl_profile.env`: exemplo de variaveis para STL

## O que este repositorio realmente permite

1. Instalar o binario do Cheat Engine no SteamOS em modo manual.
2. Abrir o CE via launcher customizado do STL.
3. Preservar uma build especifica via `build.lock`.

Isso nao significa que os cheats atuais vao funcionar no jogo.

## Instalacao

1. Copie este projeto para o Deck, por exemplo:
   - `/home/deck/tools/crysis-remastered-trainer`
2. Garanta permissao de execucao nos scripts:
   - `chmod +x /home/deck/tools/crysis-remastered-trainer/steamdeck/*.sh`
3. Crie o lock de build:
   - `cp steamdeck/build.lock.example steamdeck/build.lock`
4. Preencha em `steamdeck/build.lock`:
   - `EXPECTED_BUILD_ID`
   - `EXPECTED_MANIFEST_ID`

## Configuracao do SteamTinkerLaunch

1. Abra as opcoes do jogo no STL.
2. Configure comando customizado para:
   - `/home/deck/tools/crysis-remastered-trainer/steamdeck/stl_launch.sh %command%`
3. Se necessario, configure variaveis de ambiente com base em `steamdeck/stl_profile.env`:
   - `CE_BIN`
   - `TABLE_PATH`
   - `LOG_FILE`

## Primeiro uso

1. Inicie o jogo pela Steam (com STL ativo).
2. O launcher pode abrir o Cheat Engine com `crysis_remastered_basic.ct`.
3. A tabela tenta fazer auto-attach em `CrysisRemastered.exe`.
4. Hotkeys:
   - `F1`: vida infinita
   - `F2`: municao infinita
   - `F3`: sem recuo
   - `F12`: desativar tudo (panic key)

## Ajuste obrigatorio das assinaturas AOB

A tabela foi entregue como template seguro. Voce precisa substituir os padroes placeholders para a sua build travada:

- `CR_HEALTH_WRITE`
- `CR_AMMO_SUB`
- `CR_RECOIL_WRITE`

Sem isso, os scripts nao ativam. Hoje, este repositorio nao inclui essas assinaturas reais.

## Validar build travada

Rode:

```bash
/home/deck/tools/crysis-remastered-trainer/steamdeck/check_build.sh
```

Se o script falhar, a build mudou e as assinaturas devem ser revalidadas.

## Solucao de problemas

- CE nao abre:
  - Verifique `CE_BIN` e se o binario esta no `PATH`.
- Jogo abre, mas script nao ativa:
  - Isso e esperado com a tabela atual, porque os AOBs sao placeholder.
- Build check falha:
  - Atualize `build.lock` somente apos revalidar os 3 scripts na build nova.

## Uso seguro

- Apenas campanha offline (single-player).
- Nao usar em multiplayer.
- Nao inclui bypass de anti-cheat.

