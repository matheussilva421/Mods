# Mods / Trainers

Repositorio para organizar trainers, mods e projetos tecnicos por jogo, com uma estrutura que pode crescer sem poluir a raiz.

## Estrutura principal

- `Games/` projetos agrupados por jogo
- `Docs/` documentacao agrupada por jogo e por manutencao do repositorio

## Jogos disponiveis hoje

- `Games/CrysisRemastered/`
- `Games/Crysis2Remastered/`
- `Games/Crysis3Remastered/`
- `Games/BioshockRemastered/`
- `Games/TheEvilWithin/`

## Downloads rapidos

- Crysis Remastered: `Games/CrysisRemastered/Trainer/crysis_remastered_basic.ct`
- Crysis 2 Remastered: `Games/Crysis2Remastered/Trainer/release/cheat-deck/Crysis2Remastered-CheatDeck.exe`
- Crysis 3 Remastered: `Games/Crysis3Remastered/Trainer/release/cheat-deck/Crysis3Remastered-CheatDeck.exe`
- BioShock Remastered: `Games/BioshockRemastered/Trainer/release/cheat-deck/BioshockRemastered-CheatDeck.exe`
- The Evil Within: `Games/TheEvilWithin/Trainer/release/cheat-deck/TheEvilWithin-CheatDeck.exe`

## Como o repositorio esta organizado

Cada jogo pode ter mais de um tipo de projeto no futuro. Exemplos:

- `Trainer/`
- `Mods/`
- `Patches/`
- `Research/`

Padrao atual:

```text
Games/
  NomeDoJogo/
    Trainer/
    Mods/
    Patches/
    Research/
Docs/
  Games/
    NomeDoJogo/
  Repository/
```

## Projetos atuais

- `Games/CrysisRemastered/Trainer/` trainer legado baseado em Cheat Engine
- `Games/Crysis2Remastered/Trainer/` trainer em `.exe` para Cheat Deck
- `Games/Crysis3Remastered/Trainer/` trainer em `.exe` para Cheat Deck
- `Games/BioshockRemastered/Trainer/` trainer em `.exe` para Cheat Deck
- `Games/TheEvilWithin/Trainer/` trainer em `.exe` para Cheat Deck

## Escopo atual

- single-player only
- foco em Steam Deck + Cheat Deck
- projetos baseados em engenharia reversa, tabelas CE e automacao de uso local
