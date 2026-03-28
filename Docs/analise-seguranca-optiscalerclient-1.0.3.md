# Análise de segurança: OptiscalerClient 1.0.3 Portable

Data da análise: 28/03/2026

Arquivo analisado:

- `D:\Downloads\Chrome\OptiscalerClient_1.0.3_Portable\OptiscalerClient.exe`

Objetivo:

- Verificar se há sinais fortes de malware ou de origem duvidosa antes do uso.

## Resumo executivo

Conclusão curta:

- Não encontrei indícios fortes de malware nesse pacote.
- O cenário atual é de "provavelmente seguro para teste/uso comum", mas não de "segurança absoluta".
- Os principais pontos a favor são: correspondência com release público no GitHub, commit do release batendo com o `ProductVersion` embutido no executável, scan local do Microsoft Defender sem detecção e resultado público no VirusTotal de `1/70`.
- Os principais pontos de cautela são: o executável principal não é assinado digitalmente, o projeto é comunitário e não oficial do OptiScaler, e eu não consegui validar um checksum oficial publicado pelo autor para o asset baixado.

Veredito prático:

- Eu usaria somente se o download tiver vindo do release oficial do projeto no GitHub.
- Eu evitaria usar esse binário com privilégios de administrador sem necessidade.
- Eu não misturaria isso com jogos competitivos ou com anti-cheat, porque isso é um risco funcional/TOS separado de malware.

## Evidências locais

### 1. Conteúdo da pasta

Arquivos encontrados:

- `OptiscalerClient.exe` - 57.461.685 bytes
- `av_libglesv2.dll` - 5.426.176 bytes
- `libHarfBuzzSharp.dll` - 1.804.872 bytes
- `libSkiaSharp.dll` - 9.414.216 bytes
- `config.json` - 1.305 bytes

### 2. Hash SHA-256 do executável principal

- `464A043462040991281275864B309F4E45FEA8CD9CCA0E72B4DBDC9EB56B02AD`

Esse hash é a identidade mais importante do binário analisado.

### 3. Metadados do executável

Metadados lidos do `VersionInfo`:

- `FileDescription`: `OptiscalerClient`
- `ProductName`: `OptiscalerClient`
- `CompanyName`: `OptiscalerClient`
- `FileVersion`: `1.0.3.0`
- `ProductVersion`: `1.0.3+a7994c0ea50cd1c983786786c68d3cdedac142ac`
- `OriginalFilename`: `OptiscalerClient.dll`

Ponto importante:

- O `ProductVersion` embute o commit `a7994c0ea50cd1c983786786c68d3cdedac142ac`.
- Esse mesmo commit aparece no release público `OptiscalerClient-1.0.3` do repositório do autor.

### 4. Assinatura digital

Resultado da checagem de assinatura:

- `OptiscalerClient.exe`: `NotSigned`
- `av_libglesv2.dll`: `NotSigned`
- `libHarfBuzzSharp.dll`: assinatura válida da Microsoft
- `libSkiaSharp.dll`: assinatura válida da Microsoft

Interpretação:

- Ausência de assinatura digital não prova malware.
- Mas reduz confiança operacional, porque você não tem uma cadeia criptográfica forte do editor do binário.

### 5. Marca de download da internet

O executável contém `Zone.Identifier`:

- `ZoneId=3`

Interpretação:

- O Windows reconhece que o arquivo veio da internet.
- Isso é esperado para um download normal e não é um indicador de malware por si só.

### 6. Configuração embutida no pacote

O `config.json` aponta para repositórios plausíveis e coerentes com a proposta do app:

- `Agustinm28/Optiscaler-Client`
- `optiscaler/OptiScaler`
- `Agustinm28/OptiScaler-Betas`
- `Agustinm28/OptiScaler-Extras`
- `optiscaler/fakenvapi`
- `Nukem9/dlssg-to-fsr3`

Isso é compatível com um frontend/gerenciador de downloads para o ecossistema do OptiScaler.

### 7. Scan local do Microsoft Defender

Foi executado um scan customizado na pasta:

- Início do scan: 28/03/2026 16:17:16
- Término do scan: 28/03/2026 16:17:17
- Tipo: verificação personalizada

Resultado observado:

- O scan concluiu normalmente.
- Não apareceu evento de detecção associado a essa verificação.

Interpretação:

- Isso conta a favor do arquivo.
- Não é prova absoluta, mas é um sinal local importante.

## Evidências públicas

### 1. O projeto público existe e é open source

O repositório público existe:

- `Agustinm28/Optiscaler-Client`

O README do projeto informa claramente:

- não é um projeto oficial do OptiScaler;
- o autor não é afiliado à equipe do OptiScaler;
- é um projeto pessoal sem finalidade comercial;
- o uso é por conta e risco do usuário.

Isso é um ponto positivo de transparência, mas também significa que você está confiando em um projeto comunitário, não no time oficial do mod principal.

### 2. O release 1.0.3 existe e bate com o executável

O release público:

- `OptiscalerClient-1.0.3`
- publicado em `26 de março de 2026`
- aponta para o commit `a7994c0`

Esse commit é exatamente o mesmo embutido no `ProductVersion` do executável local.

Interpretação:

- Isso reduz bastante a chance de o binário ser uma montagem aleatória ou um arquivo com identidade forjada de forma grosseira.

### 3. Resultado público no VirusTotal

O hash do executável já existe no VirusTotal.

Resumo observado:

- `1/70 security vendor flagged this file as malicious`
- O único motor que marcou foi `Zillya`, com `Downloader.MLoki.Win64.10`
- Microsoft, Kaspersky, ESET, Malwarebytes, Symantec, BitDefender, Google e outros apareceram como `Undetected`

Resultado de comportamento sandboxado disponível no VirusTotal:

- `Network comms`: `NOT FOUND`
- sem regras MITRE, IDS ou Sigma relevantes
- comportamento visto majoritariamente compatível com app `.NET` / `Avalonia`

Interpretação:

- `1/70` costuma parecer mais com falso positivo do que com evidência forte de malware, especialmente quando os motores principais não detectam nada.
- Ainda assim, não é correto tratar isso como "zero risco".

### 4. Aviso importante do projeto oficial OptiScaler

O repositório oficial do OptiScaler alerta que existem sites falsos se passando pelo projeto e afirma que os lugares legítimos são o GitHub, o Discord e a página NexusMods indicada por eles.

Interpretação:

- Se esse pacote foi obtido por um site qualquer, eu não confiaria.
- Se veio do release oficial do GitHub do `Agustinm28/Optiscaler-Client`, a confiança sobe bastante.

## Julgamento técnico

Minha avaliação final:

- Não há sinal forte de trojan, spyware ou dropper ativo.
- Há boa consistência entre:
  - o executável local,
  - o repositório público,
  - o release público,
  - o commit embutido no binário,
  - e o comportamento externo visto no VirusTotal.
- O maior problema de confiança não é "parece malware", e sim:
  - ser um projeto não oficial;
  - não ter assinatura digital;
  - e depender de você ter baixado o pacote do lugar certo.

## Recomendação prática

Eu considero aceitável usar se todas estas condições forem verdadeiras:

- você baixou do release oficial no GitHub do `Agustinm28/Optiscaler-Client`;
- você não vai executar como administrador sem necessidade;
- você entende que o risco principal restante é de confiança operacional, não de detecção clara de malware;
- você não pretende usar em jogos/ambientes onde modificação de arquivos possa gerar problema com anti-cheat ou política do jogo.

Eu não recomendaria usar se:

- o arquivo veio de reupload, encurtador, site espelho, canal de Telegram, Discord aleatório ou página "oficial" desconhecida;
- você precisa de confiança alta nível corporativo;
- você quer usar em máquina sensível sem isolar primeiro.

## Próximos passos recomendados

Para subir a confiança de "provavelmente seguro" para "o mais verificado possível":

1. Baixar novamente somente do release oficial do GitHub do autor.
2. Comparar o SHA-256 do novo download com este valor:
   - `464A043462040991281275864B309F4E45FEA8CD9CCA0E72B4DBDC9EB56B02AD`
3. Executar a primeira abertura com rede monitorada e sem privilégios elevados.
4. Se quiser risco mínimo, testar antes em Windows Sandbox ou máquina virtual.

## Fontes públicas consultadas

- Repositório do cliente: <https://github.com/Agustinm28/Optiscaler-Client>
- Release `OptiscalerClient-1.0.3`: <https://github.com/Agustinm28/Optiscaler-Client/releases/tag/OptiscalerClient-1.0.3>
- Release oficial do OptiScaler com aviso sobre sites falsos: <https://github.com/optiscaler/OptiScaler/releases>
- Hash no VirusTotal: <https://www.virustotal.com/gui/file/464A043462040991281275864B309F4E45FEA8CD9CCA0E72B4DBDC9EB56B02AD>
- Post público do autor anunciando a versão 1.0.3: <https://www.reddit.com/r/radeon/comments/1s4opsd/optiscaler_client_103_betas_fsr_4_int8_support/>

## Observação final

Este parecer responde à pergunta "há sinais concretos de que isso seja malicioso?".

Resposta:

- No estado atual da análise, não encontrei sinais concretos fortes de malware.
- O risco residual que sobra é principalmente de confiança na cadeia de distribuição e no fato de ser um projeto comunitário não assinado.
