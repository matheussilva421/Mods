# Mods

Repositório central para organizar **vários mods de vários jogos** de forma escalável.

## Estrutura proposta

```text
Mods/
├── games/                      # Mods separados por jogo
│   └── <nome-do-jogo>/
│       └── <nome-do-mod>/
│           ├── src/            # Código-fonte do mod
│           ├── assets/         # Texturas, modelos, áudio, etc.
│           ├── docs/           # Documentação específica do mod
│           ├── scripts/        # Build, empacotamento e utilitários
│           ├── tests/          # Testes automatizados e validações
│           ├── build/          # Saída temporária de build (gitignored)
│           ├── release/        # Artefatos finais para distribuição
│           └── README.md       # Como instalar/usar/desenvolver o mod
│
├── shared/                     # Recursos compartilhados entre mods
│   └── README.md
│
├── tooling/                    # Ferramentas e automações do repositório
│   └── README.md
│
└── templates/
    └── mod-template/           # Template base para criar novos mods
```

## Convenções recomendadas

- Um mod por pasta em `games/<jogo>/<mod>/`.
- Nomes de pastas em `kebab-case` (ex.: `skyrim`, `better-hud`).
- Todo mod deve ter seu próprio `README.md` com:
  - jogo alvo e versão;
  - requisitos/dependências;
  - instruções de instalação;
  - instruções de build e testes;
  - changelog resumido.
- Tudo que for reaproveitável entre jogos deve ir para `shared/`.
- Scripts utilitários globais (lint, release, validações) devem ficar em `tooling/`.

## Como criar um novo mod

1. Copie o conteúdo de `templates/mod-template`.
2. Cole em `games/<nome-do-jogo>/<nome-do-mod>/`.
3. Ajuste o `README.md` e o `manifest.example.json` para o jogo/mod alvo.
4. Implemente código em `src/` e assets em `assets/`.

## Próximo passo sugerido

Quando você definir o primeiro jogo, podemos criar um **scaffold automático** (script) para gerar mods com essa estrutura e metadados padronizados.
