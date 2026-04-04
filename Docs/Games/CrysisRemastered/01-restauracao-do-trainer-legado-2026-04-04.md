# Restauracao do trainer legado do Crysis Remastered

Data: 2026-04-04

## Motivo

O projeto do `Crysis Remastered` nao deveria ter sido removido. Ele existia no historico como um trainer baseado em Cheat Engine e scripts para Steam Deck.

## Fonte da restauracao

Os arquivos foram restaurados diretamente do commit:

- `566a88c`

Origem antiga dos arquivos:

- `games/crysis-remastered/`

Destino atual na estrutura por jogos:

- `Games/CrysisRemastered/Trainer/`

## Arquivos restaurados

- `crysis_remastered_basic.ct`
- `GUIA_INSTALACAO.md`
- `steamdeck/build.lock.example`
- `steamdeck/check_build.sh`
- `steamdeck/stl_launch.sh`
- `steamdeck/stl_profile.env`

## Decisao de organizacao

O projeto foi recolocado na estrutura nova do repositorio para manter consistencia com os outros jogos:

- `Games/CrysisRemastered/Trainer/`
- `Docs/Games/CrysisRemastered/`

## Observacao tecnica

Esse projeto restaurado e um fluxo legado baseado em Cheat Engine e SteamTinkerLaunch. Ele nao usa o mesmo modelo em `.exe` do Crysis 2 Remastered.

## Validacao executada

1. leitura dos arquivos direto do historico Git;
2. restauracao no novo caminho por jogo;
3. verificacao da presenca de todos os arquivos restaurados.
