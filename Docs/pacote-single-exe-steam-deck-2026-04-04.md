# Pacote single-exe para uso no Steam Deck

Data: 2026-04-04

## Objetivo

Esta etapa foi feita para reduzir ao mínimo a fricção de uso no Steam Deck com Cheat Deck.

A exigência prática foi:

- copiar um `.exe`;
- apontar o Cheat Deck para esse `.exe`;
- não depender obrigatoriamente de uma pasta `profiles` externa para o fluxo básico.

## O que mudou

### 1. Perfil embutido no executável

O trainer agora possui um fallback interno com o perfil FR v1.4 embutido.

Com isso, se o arquivo externo abaixo não existir:

- `profiles/crysis2-remastered.fr-v1.4.json`

O executável passa a usar o perfil embutido e continua inicializando normalmente.

Isso elimina a dependência operacional do `profiles/` para o caso simples.

### 2. Geração de pacote single-exe

O build agora gera esta saída pronta para cópia:

- `Crysis2RemasteredTrainer/release/single-exe/Crysis2RemasteredTrainer.exe`

Esse é o artefato indicado para o fluxo mais simples no Cheat Deck.

### 3. Geração de pacote portable

O build também continua gerando um pacote editável:

- `Crysis2RemasteredTrainer/release/portable/Crysis2RemasteredTrainer.exe`
- `Crysis2RemasteredTrainer/release/portable/profiles/crysis2-remastered.fr-v1.4.json`

Esse pacote é útil quando for necessário ajustar manualmente o perfil.

## Fluxo recomendado para você

Use este arquivo:

- `Crysis2RemasteredTrainer/release/single-exe/Crysis2RemasteredTrainer.exe`

Fluxo:

1. copie esse `.exe` para o local que você usa no Steam Deck;
2. configure o Cheat Deck para abrir esse executável junto do jogo;
3. inicie o jogo normalmente;
4. use as hotkeys do trainer.

## Cheats disponíveis

- `F1` Lock Energy
- `F2` Lock Holster
- `F3` Lock Clip
- `F4` Invisible
- `F5` God Mode
- `F6` 1-Hit Kill
- `F12` Disable all

## Testes executados nesta etapa

### Teste 1. Rebuild completo

Comando executado:

```powershell
.\Crysis2RemasteredTrainer\build-trainer.ps1
```

Resultado:

- build concluído com sucesso.

### Teste 2. Verificação do pacote single-exe

Foi confirmado que o arquivo abaixo foi gerado:

- `Crysis2RemasteredTrainer/release/single-exe/Crysis2RemasteredTrainer.exe`

### Teste 3. Verificação do pacote portable

Foi confirmado que os arquivos abaixo foram gerados:

- `Crysis2RemasteredTrainer/release/portable/Crysis2RemasteredTrainer.exe`
- `Crysis2RemasteredTrainer/release/portable/profiles/crysis2-remastered.fr-v1.4.json`

## Limite técnico que continua existindo

Esse pacote agora está mais simples de distribuir e usar, mas existe um limite que não dá para esconder:

- eu ainda não validei esse trainer contra uma sessão real do jogo neste ambiente;
- então eu posso afirmar que o executável compila, gera os pacotes corretos e não depende mais do `profiles` para o caso simples;
- eu não posso afirmar com honestidade técnica que ele está 100% validado in-game sem teste na sua build alvo.

## Conclusão prática

Se você quer o caminho mais simples hoje, o arquivo certo é:

- `Crysis2RemasteredTrainer/release/single-exe/Crysis2RemasteredTrainer.exe`

Esse é o pacote mais próximo do que você pediu: copiar o executável e usar no Cheat Deck sem precisar levar a pasta de perfil junto.
