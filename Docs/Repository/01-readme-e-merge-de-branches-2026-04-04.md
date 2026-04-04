# Reescrita do README e unificacao das branches

Data: 2026-04-04

## Objetivo

Registrar duas mudancas de manutencao do repositorio:

1. reescrita do README principal para refletir a estrutura por jogos;
2. unificacao das branches `master` e `main` sem reintroduzir arquivos antigos e obsoletos.

## Situacao encontrada

Depois do `fetch`, o repositorio passou a mostrar:

- `master`
- `origin/master`
- `origin/main`

A branch `origin/main` era a branch padrao remota, mas estava com uma estrutura antiga e incompleta em relacao ao estado atual desenvolvido em `master`.

## Decisao tomada

A branch `master` foi tratada como estado mais atual do projeto.

O repositorio foi reorganizado e o README reescrito em cima dessa estrutura mais nova. Depois disso, a branch `main` foi integrada de forma controlada para que o historico fosse unificado sem trazer de volta os arquivos antigos da estrutura antiga.

## README reescrito

O novo README principal passou a focar em:

- organizacao por jogo;
- caminho rapido para o download atual;
- padrao para expansao futura;
- localizacao do projeto ativo e da documentacao.

Tambem foram alinhados:

- `Games/README.md`
- `Games/Crysis2Remastered/README.md`
- `Docs/Games/README.md`

## Como as branches foram juntadas

A branch `main` remota existia, mas o estado mais atual estava em `master`.

A unificacao foi feita preservando o estado mais novo do repositorio e mantendo o historico de merge, evitando restaurar os arquivos antigos que estavam apenas na `main` antiga.

Resultado pratico:

- o conteudo atual ficou consolidado;
- `master` e `main` deixaram de representar estados concorrentes e inconsistentes;
- o GitHub passa a apontar para um historico mais coerente.

## Validacoes executadas

1. inspeção das branches locais e remotas;
2. comparação entre `origin/main` e `master`;
3. rebuild do projeto atual;
4. abertura do executavel final apos a reorganizacao.

## Conclusao

O repositorio agora esta mais limpo para o usuario e mais previsivel para manutencao futura:

- layout por jogos
- README principal reescrito
- documentacao separada por escopo
- branches consolidadas em torno do estado atual
