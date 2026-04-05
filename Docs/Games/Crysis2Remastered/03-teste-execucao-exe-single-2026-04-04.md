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

## Validacao posterior no Steam Deck

Depois da validacao inicial acima, o trainer foi testado no fluxo real do Steam Deck com Cheat Deck e jogo aberto.

### Comportamento observado

Os sintomas relatados foram estes:

- a janela do trainer abre junto com o jogo;
- nos primeiros segundos a tela responde;
- quando o jogo termina de iniciar, a interface passa a ficar estatica;
- os controles deixam de responder de forma confiavel;
- o log visual da janela para de atualizar;
- a lista original de cheats exigia rolagem e nao ficava boa na tela do Deck.

### Evidencia coletada

Foi adicionado um log em arquivo ao lado do executavel:

- `Crysis2RemasteredTrainer.log`

Exemplo de evidencia observada durante o teste no Deck:

- o trainer chegou a anexar no processo do jogo;
- em uma sessao posterior houve `Detached from PID ...`;
- tentativas de ativar cheat depois disso falharam com `Game process is not attached`.

Isso mostrou duas classes de problema:

1. problema de interface e interacao no ambiente Wine/Proton do Deck;
2. problema de responsividade e estabilidade do polling do trainer quando o jogo fica ativo.

## Correcoes aplicadas depois do teste no Deck

Com base nos problemas acima, o trainer foi ajustado nas seguintes frentes:

### 1. Log mais util

- o trainer agora grava log na mesma pasta do `.exe`;
- o log da janela tambem passou a registrar pedidos de enable/disable;
- excecoes passaram a incluir mais contexto no arquivo de log.

### 2. Layout da tela

- os cheats foram reorganizados para aparecerem todos na mesma tela;
- a interface deixou de depender de rolagem para visualizar os cheats principais no Deck.

### 3. Controles mais robustos

- a tentativa baseada em checkbox se mostrou fragil no Steam Deck;
- a interface foi trocada para botoes explicitos de `Enable` e `Disable`;
- cada cheat passou a mostrar estado visual `Enabled` ou `Disabled`.

### 4. Responsividade do trainer

- o polling de attach e manutencao deixou de rodar na thread principal da UI;
- essas operacoes foram movidas para background;
- as atualizacoes visuais passaram a voltar para a UI de forma segura.

### 5. Auto-enable no attach

- ao detectar attach em um novo PID do jogo, o trainer agora tenta habilitar todos os cheats automaticamente;
- o log registra o inicio e o fim desse processo, alem de falhas individuais por cheat.

## Situacao atual

O trainer ficou significativamente mais preparado para o fluxo real do Steam Deck do que na validacao inicial.

Em termos praticos, a evolucao foi esta:

- antes: validacao apenas de abertura do `.exe`;
- depois: validacao com sintomas reais no Deck;
- agora: trainer com layout adaptado, log em arquivo, botoes explicitos, polling em background e auto-enable ao anexar no jogo.

## Proximo teste recomendado

O proximo teste real no Steam Deck deve verificar especificamente:

1. se a janela continua responsiva depois que o jogo termina de iniciar;
2. se os botoes `Enable` e `Disable` respondem no Cheat Deck;
3. se o log da janela continua atualizando durante o jogo;
4. se o `Crysis2RemasteredTrainer.log` registra attach, auto-enable e eventuais falhas de memoria;
5. se os cheats realmente permanecem ativos in-game apos o attach automatico.

## Conclusão

O artefato `cheat-deck` com nome final de release está abrindo corretamente como aplicação Windows.

O teste no Steam Deck deixou de ser apenas uma pendencia teorica e passou a alimentar correcoes reais no trainer.

O proximo nivel de validacao agora nao e mais "ver se abre", e sim confirmar se a versao atual corrigida permanece responsiva e consegue manter os cheats ativos durante a sessao real do jogo no Deck.


