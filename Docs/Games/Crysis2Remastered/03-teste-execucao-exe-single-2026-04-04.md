# Teste de execução do EXE do trainer

Data: 2026-04-04

## Objetivo

Validar o comportamento mínimo do executável final destinado ao uso com Cheat Deck.

Arquivo testado:

- `Games/Crysis2Remastered/Trainer/release/cheat-deck/Crysis2Remastered-CheatDeck.exe`

## Teste executado

O executável foi iniciado diretamente no Windows para confirmar:

1. o processo sobe sem erro imediato;
2. a janela principal do trainer abre;
3. o programa não encerra sozinho logo após iniciar.

## Resultado

Resultado observado:

- processo iniciado com sucesso;
- janela criada com sucesso;
- título da janela detectado: `Crysis 2 Remastered Trainer`;
- o processo permaneceu ativo até ser encerrado manualmente ao final do teste.

## Evidência resumida

Saída capturada durante o teste:

```text
PROCESS_RUNNING PID=23064 TITLE=Crysis 2 Remastered Trainer
```

## Limite do teste

Esse teste confirma apenas a inicialização correta do executável final.

Ele não valida neste ambiente:

- attach real ao processo `Crysis2Remastered.exe`;
- funcionamento in-game dos cheats;
- comportamento via Cheat Deck no Steam Deck.

## Conclusão

O artefato `cheat-deck` com nome final de release está abrindo corretamente como aplicação Windows.

Para o próximo nível de validação, o teste precisa ser feito junto do jogo real no fluxo final do Steam Deck.



