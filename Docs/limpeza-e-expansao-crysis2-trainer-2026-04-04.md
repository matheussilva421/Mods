# Limpeza do repositório e expansão do trainer do Crysis 2 Remastered

Data: 2026-04-04

## Objetivo

Esta rodada teve dois objetivos:

1. remover arquivos legados e redundantes que não faziam mais parte do fluxo ativo do projeto;
2. ampliar o trainer do `Crysis 2 Remastered` com cheats extras da tabela Fearless Revolution (`FR v1.4`), com foco em `God Mode` e `1-Hit Kill`.

## Organização aplicada no repositório

O repositório foi consolidado para manter um único projeto ativo como fonte principal:

- `Crysis2RemasteredTrainer/`

Arquivos e estruturas antigas que deixaram de fazer sentido para o estado atual do projeto foram removidos:

- `crysis_remastered_basic.ct`
- `steamdeck/` inteiro
- `GUIA_INSTALACAO.md`
- perfis template não usados do trainer
- documentação antiga que não tinha relação com o projeto ativo

Resultado prático:

- o `README.md` da raiz agora aponta diretamente para o trainer ativo;
- o build do trainer copia apenas o perfil real em uso;
- o diretório `dist/` fica mais previsível e sem perfis sobrando;
- o repositório fica mais legível para manutenção e publicação no GitHub.

## Expansão funcional do trainer

### Cheats mantidos

Os cheats já presentes continuaram suportados:

- `F1` Lock Energy
- `F2` Lock Holster
- `F3` Lock Clip (No Reload)
- `F4` Invisible
- `F12` Disable all

### Cheats adicionados

Foram adicionados:

- `F5` God Mode
- `F6` 1-Hit Kill

## Implementação técnica

### 1. Suporte a alocação remota

Foram adicionadas chamadas nativas para:

- `VirtualAllocEx`
- `VirtualFreeEx`

Isso permitiu criar code caves dentro do processo remoto do jogo.

### 2. Alocação próxima ao ponto de hook

O arquivo `ProcessMemory.cs` passou a suportar `AllocateNear(...)`.

Motivo:

- o trainer usa `JMP rel32` para desviar execução;
- esse salto exige que a cave fique dentro do alcance do deslocamento relativo;
- a alocação próxima reduz falhas de jump fora de alcance.

### 3. Novo tipo de cheat por hook/code cave

O `MainForm.cs` foi expandido para trabalhar com vários tipos de ação:

- `patch`
- `setbytes`
- `godmode`
- `onehitkill`

Isso separa cheats simples de cheats que precisam de lógica em runtime.

### 4. God Mode

O `God Mode` foi implementado em duas partes:

1. um hook coleta e grava o ponteiro da estrutura de vida do jogador;
2. um timer lê a vida máxima e reescreve o valor atual da vida.

Fluxo resumido:

- localizar o padrão da rotina usado pela tabela FR;
- instalar um hook curto;
- armazenar o ponteiro de vida do player em memória remota;
- no loop de manutenção do trainer, copiar `max health` para `current health`.

Vantagem desse desenho:

- a lógica fica mais próxima da tabela original;
- o cheat não depende de um endereço fixo absoluto;
- quando o ponteiro do player muda, o hook tende a recolhê-lo novamente.

### 5. 1-Hit Kill

O `1-Hit Kill` foi portado com code cave.

Fluxo resumido:

- localizar a rotina usada pela tabela FR para leitura de vida;
- instalar um hook;
- filtrar o player por assinatura usada pela própria tabela (`0x7777`);
- reduzir a vida de inimigos para um valor mínimo antes da rotina original continuar.

Observação importante:

- o cheat foi modelado para preservar o player e afetar alvos lidos pela rotina de vida observada na tabela FR;
- a validação real ainda depende do jogo rodando na build alvo.

## Arquivos principais alterados

### Código

- `Crysis2RemasteredTrainer/MainForm.cs`
- `Crysis2RemasteredTrainer/NativeMethods.cs`
- `Crysis2RemasteredTrainer/ProcessMemory.cs`

### Build e perfil

- `Crysis2RemasteredTrainer/build-trainer.ps1`
- `Crysis2RemasteredTrainer/profiles/crysis2-remastered.fr-v1.4.json`
- `Crysis2RemasteredTrainer/dist/profiles/crysis2-remastered.fr-v1.4.json`

### Documentação

- `README.md`
- `Crysis2RemasteredTrainer/README.md`
- `Docs/limpeza-e-expansao-crysis2-trainer-2026-04-04.md`

## Testes executados

### Teste 1. Rebuild completo do trainer

Comando executado:

```powershell
.\Crysis2RemasteredTrainer\build-trainer.ps1
```

Resultado esperado:

- recompilar o executável WinForms;
- limpar perfis antigos do `dist/profiles`;
- copiar apenas o perfil real `crysis2-remastered.fr-v1.4.json`.

Resultado obtido:

- build concluído com sucesso.

### Teste 2. Validação de artefatos gerados

Foi verificado que o build entrega:

- `Crysis2RemasteredTrainer/dist/Crysis2RemasteredTrainer.exe`
- `Crysis2RemasteredTrainer/dist/profiles/crysis2-remastered.fr-v1.4.json`

### Teste 3. Validação estrutural do repositório

Foi conferido via `git status` que:

- os arquivos legados removidos saíram do versionamento;
- os arquivos centrais do trainer ficaram como conjunto principal de mudança.

## Limites atuais

Existem limites que continuam válidos e precisam ser tratados com honestidade técnica:

1. eu não consegui validar o trainer contra um processo real do jogo neste ambiente;
2. a parte mais sensível do trabalho é a validação do comportamento in-game das rotinas `God Mode` e `1-Hit Kill`;
3. se a build do jogo divergir da build da tabela FR v1.4, os padrões podem deixar de casar;
4. como esse fluxo depende de manipulação de memória remota, a compatibilidade prática precisa ser confirmada no dispositivo final.

## Próximos passos recomendados

1. executar o trainer junto do `Crysis2Remastered.exe` na mesma build usada pela tabela FR;
2. testar separadamente `F5` e `F6` em combate real;
3. se um padrão falhar, atualizar a assinatura AOB correspondente antes de alterar a lógica do hook;
4. manter o repositório enxuto, evitando reintroduzir templates e fluxos antigos que não fazem parte do projeto atual.
