# Reorganizacao do GitHub por jogos

Data: 2026-04-04

## Objetivo

Preparar o repositório para crescer com outros jogos sem misturar código, releases e documentação na raiz.

## Estrutura adotada

```text
README.md
Games/
  README.md
  Crysis2Remastered/
    README.md
    Trainer/
      src/
      profiles/
      release/
      build-trainer.ps1
      README.md
Docs/
  Games/
    README.md
    Crysis2Remastered/
      README.md
      01-...
      02-...
      03-...
      04-...
      05-...
      06-...
```

## Principios usados

1. a raiz mostra apenas visao geral do repositorio;
2. cada jogo tem sua propria pasta;
3. cada jogo pode ter mais de um projeto no futuro, como `Trainer`, `Mods`, `Patches` ou `Research`;
4. a documentacao acompanha o mesmo agrupamento por jogo.

## Resultado pratico

Agora, para adicionar outro jogo no futuro, o caminho fica previsivel:

- `Games/NomeDoJogo/Trainer/`
- `Docs/Games/NomeDoJogo/`

Isso reduz bagunca no GitHub e evita que a raiz fique cheia de pastas sem contexto.

