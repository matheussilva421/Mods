# Release final simplificada para o Cheat Deck

Data: 2026-04-04

## Objetivo

Deixar a release final mais direta para uso no Steam Deck, com foco em duas coisas:

1. nome de arquivo mais claro para o Cheat Deck;
2. instrucoes curtas em PT-BR dentro da propria pasta da release.

## O que foi alterado

### Nome final da release single-exe

O executavel principal da release simples passou a ser:

- `Games/Crysis2Remastered/Trainer/release/cheat-deck/Crysis2Remastered-CheatDeck.exe`

Esse nome reduz ambiguidade no momento de selecionar o app no Cheat Deck.

### Instrucoes curtas dentro da pasta final

Foi criado o arquivo:

- `Games/Crysis2Remastered/Trainer/release/cheat-deck/LEIA-ME-PTBR.md`

Esse arquivo resume:

- qual `.exe` usar;
- como apontar no Cheat Deck;
- hotkeys disponiveis;
- observacao de que o perfil ja esta embutido no `.exe`.

### Build atualizado

O build agora monta automaticamente:

- `release/cheat-deck/Crysis2Remastered-CheatDeck.exe`
- `release/cheat-deck/LEIA-ME-PTBR.md`
- `release/portable/Crysis2RemasteredTrainer.exe`
- `release/portable/profiles/crysis2-remastered.fr-v1.4.json`

## Teste executado

Depois da mudanca de nome e empacotamento, o executavel final da release simples foi aberto para validar a inicializacao.

Resultado:

- processo iniciou corretamente;
- janela abriu corretamente;
- o programa ficou ativo ate ser encerrado manualmente.

## Conclusao pratica

O arquivo mais direto para voce usar agora e:

- `Games/Crysis2Remastered/Trainer/release/cheat-deck/Crysis2Remastered-CheatDeck.exe`

