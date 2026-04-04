# Como o EXE foi criado

Data: 2026-04-04

## Objetivo

Explicar de forma prática como o executável do trainer foi criado, quais arquivos entram no processo, o que foi necessário no ambiente e como reproduzir o build.

## Visão geral

O executável final do projeto é um aplicativo Windows em C# WinForms compilado manualmente com o compilador clássico do .NET Framework.

O fluxo atual do projeto é este:

1. o código-fonte fica em `Games/Crysis2Remastered/Trainer/src/`;
2. o script `build-trainer.ps1` compila o código com `csc.exe`;
3. o build gera um executável base em `dist/`;
4. o script copia esse executável para as pastas de release;
5. a release `cheat-deck` recebe o `.exe` final e um `LEIA-ME-PTBR.md`;
6. a release `portable` recebe o `.exe` e o perfil externo `.json`.

## O que foi necessário

### 1. Linguagem e stack

Foi usado:

- C#
- WinForms
- .NET Framework compiler (`csc.exe`)
- PowerShell para automação do build

### 2. Compilador usado

O build depende deste caminho no Windows:

- `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`

Esse compilador gera o executável Windows diretamente a partir dos arquivos `.cs`.

### 3. Assemblies referenciados

Durante o build, o projeto referencia:

- `System.dll`
- `System.Drawing.dll`
- `System.Windows.Forms.dll`
- `System.Web.Extensions.dll`

Esses assemblies são usados porque o trainer tem:

- interface gráfica WinForms;
- leitura de JSON via `JavaScriptSerializer`;
- manipulação de UI, timer, hotkeys e logs.

## Arquivos que entram no executável

O script compila estes arquivos de `src/`:

- `Program.cs`
- `NativeMethods.cs`
- `ByteHelper.cs`
- `PatternScanner.cs`
- `EmbeddedProfile.cs`
- `TrainerProfile.cs`
- `ProcessMemory.cs`
- `MainForm.cs`

### Função de cada arquivo

- `Program.cs`: ponto de entrada do app
- `NativeMethods.cs`: chamadas WinAPI como `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, `VirtualAllocEx`
- `ByteHelper.cs`: parsing e apoio para bytes/hex
- `PatternScanner.cs`: busca AOB em memória
- `EmbeddedProfile.cs`: perfil FR embutido em Base64 dentro do executável
- `TrainerProfile.cs`: modelo e carregamento do perfil JSON
- `ProcessMemory.cs`: attach no processo e leitura/escrita de memória remota
- `MainForm.cs`: interface, hotkeys, lógica dos cheats, hooks e manutenção dinâmica

## Como o perfil entra no EXE

O projeto usa dois caminhos possíveis para o perfil:

1. perfil externo em `profiles/crysis2-remastered.fr-v1.4.json`
2. perfil embutido no próprio executável via `EmbeddedProfile.cs`

Na prática:

- se o arquivo externo existir, ele pode ser carregado normalmente;
- se não existir, o trainer usa o perfil embutido.

Foi isso que permitiu criar a release mais simples para o Cheat Deck, em que o usuário pode copiar apenas o `.exe`.

## Como o build funciona

Arquivo principal do build:

- `Games/Crysis2Remastered/Trainer/build-trainer.ps1`

### Etapas executadas pelo script

1. define os caminhos de `src`, `dist`, `release/cheat-deck` e `release/portable`;
2. garante que as pastas existem;
3. limpa artefatos antigos das pastas de saída;
4. monta a lista de arquivos `.cs` a compilar;
5. chama o `csc.exe` com target `winexe` e plataforma `x64`;
6. gera o executável base em `dist/Crysis2RemasteredTrainer.exe`;
7. copia o binário final para `release/cheat-deck/Crysis2Remastered-CheatDeck.exe`;
8. escreve um `LEIA-ME-PTBR.md` dentro da pasta `release/cheat-deck`;
9. gera a release `portable` com o `.exe` e o perfil externo.

## Comando de build

Para gerar o executável, o comando é:

```powershell
.\Games\Crysis2Remastered\Trainer\build-trainer.ps1
```

## Saídas geradas

### Build local

- `Games/Crysis2Remastered/Trainer/dist/Crysis2RemasteredTrainer.exe`

### Release final para Cheat Deck

- `Games/Crysis2Remastered/Trainer/release/cheat-deck/Crysis2Remastered-CheatDeck.exe`
- `Games/Crysis2Remastered/Trainer/release/cheat-deck/LEIA-ME-PTBR.md`

### Release editável

- `Games/Crysis2Remastered/Trainer/release/portable/Crysis2RemasteredTrainer.exe`
- `Games/Crysis2Remastered/Trainer/release/portable/profiles/crysis2-remastered.fr-v1.4.json`

## O que foi necessário no código para esse EXE existir

Além da compilação em si, algumas decisões técnicas foram necessárias:

### 1. Perfil embutido

Sem o `EmbeddedProfile.cs`, a release simples dependeria de arquivos externos.

### 2. Estrutura de memória remota

O trainer precisava:

- localizar o processo do jogo;
- encontrar padrões AOB no módulo do jogo;
- aplicar patches de bytes;
- criar hooks com code cave quando o cheat não podia ser resolvido com um patch simples.

### 3. Hotkeys globais

O app registra hotkeys com `RegisterHotKey` para permitir o uso de `F1` a `F6` e `F12`.

### 4. Interface mínima

A janela WinForms foi mantida simples para funcionar como companion app do Cheat Deck e também servir para debug local.

## Limites do processo

Este build resolve a geração do executável e da release final, mas não elimina limites técnicos do trainer:

1. o comportamento in-game ainda depende da build do jogo casar com as assinaturas e lógica portadas da tabela FR;
2. o ambiente aqui validou build e inicialização do `.exe`, não combate real dentro do jogo;
3. mudanças futuras no jogo podem exigir atualização de AOBs, offsets ou hooks.

## Como repetir para outro jogo

Se você quiser usar o mesmo modelo para outro jogo, o caminho natural é replicar esta estrutura:

- `Games/NomeDoJogo/Trainer/src/`
- `Games/NomeDoJogo/Trainer/profiles/`
- `Games/NomeDoJogo/Trainer/release/`
- `Docs/Games/NomeDoJogo/`

Depois, adaptar:

- nome do processo alvo;
- perfil JSON;
- padrões AOB;
- patches e hooks;
- nome final da release para Cheat Deck.

## Conclusão

O `.exe` foi criado com um fluxo direto e reproduzível:

- código C# em `src/`
- compilação manual com `csc.exe`
- empacotamento via PowerShell
- perfil embutido para simplificar a release final
- validação de build e de abertura do executável

Esse desenho é simples o suficiente para repetir em outros jogos, mas ainda flexível para cheats que precisem de patch simples ou hook em memória.
