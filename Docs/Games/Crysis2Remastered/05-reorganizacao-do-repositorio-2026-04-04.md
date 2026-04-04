# Reorganizacao do repositorio

Data: 2026-04-04

## Objetivo

Deixar o GitHub mais limpo para quem entra no repositorio e quer entender rapidamente:

- onde esta o codigo;
- onde esta o binario final;
- onde esta a documentacao;
- o que faz parte do build local e o que faz parte da release.

## Mudancas aplicadas

### 1. Codigo movido para `src/`

Os arquivos `.cs` do trainer foram movidos para:

- `Games/Crysis2Remastered/Trainer/src/`

Isso separa claramente codigo-fonte de artefatos de build e de release.

### 2. Release renomeada para uso real

A pasta final mais simples agora e:

- `Games/Crysis2Remastered/Trainer/release/cheat-deck/`

Antes, a nomenclatura `single-exe` era tecnica demais. `cheat-deck` descreve melhor o destino do pacote.

### 3. `dist/` saiu do versionamento

A pasta abaixo passou a ser tratada como saida local de build:

- `Games/Crysis2Remastered/Trainer/dist/`

Ela continua sendo usada para compilar e testar, mas nao precisa poluir o GitHub.

### 4. Documentacao consolidada por projeto

Os arquivos em `Docs/` foram agrupados em:

- `Docs/Games/Crysis2Remastered/Trainer/`

Tambem foi criado um indice:

- `Docs/Games/Crysis2Remastered/Trainer/README.md`

## Estrutura final

```text
README.md
Docs/
  Games/Crysis2Remastered/Trainer/
    README.md
    01-...
    02-...
    03-...
    04-...
    05-...
Games/Crysis2Remastered/Trainer/
  src/
  profiles/
  release/
    cheat-deck/
    portable/
  build-trainer.ps1
  README.md
```

## Testes executados

1. rebuild completo com o novo caminho de `src/`;
2. validacao da nova pasta `release/cheat-deck/`;
3. abertura do executavel final apos a reorganizacao.

## Conclusao

A organizacao agora separa claramente:

- codigo
- release final
- build local
- documentacao

Isso reduz ruido na raiz do repositorio e deixa o GitHub mais facil de navegar.

