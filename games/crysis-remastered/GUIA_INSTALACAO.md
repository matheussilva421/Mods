# Guia de Instalacao e Uso — Crysis Remastered (Steam Deck / SteamOS)

---

## Metodo Simples (Recomendado) — autoexec.cfg

O Crysis Remastered usa CryEngine, que carrega automaticamente um arquivo chamado
`autoexec.cfg` da pasta raiz do jogo ao inicializar. Basta colocar o arquivo lá
e os cheats ativam sozinhos — sem instalar nada extra.

### Opcao A: Script de instalacao automatica

1. No Steam Deck, entre no **Desktop Mode**
   - Pressione o botao Steam > Ligar/Desligar > Ir para Desktop
2. Abra o **Konsole** (terminal)
   - Clique no menu de aplicativos > Sistema > Konsole
3. Execute o script de instalacao:

   ```bash
   bash ~/tools/crysis-remastered-trainer/steamdeck/install_steamdeck.sh
   ```

   > Se voce salvou o repositorio em outro caminho, ajuste o caminho acima.

4. O script vai encontrar o jogo automaticamente e copiar o `autoexec.cfg`
5. Siga as instrucoes que aparecerem no terminal

### Opcao B: Copia manual

1. Localize a pasta raiz do jogo no Steam Deck:
   - `~/.steam/steam/steamapps/common/Crysis Remastered/`
   - ou `~/.local/share/Steam/steamapps/common/Crysis Remastered/`
   - Se o jogo esta num cartao SD: `/run/media/mmcblk0p1/steamapps/common/Crysis Remastered/`

2. Copie o arquivo `autoexec.cfg` para dentro dessa pasta:

   ```bash
   cp autoexec.cfg ~/.steam/steam/steamapps/common/"Crysis Remastered"/
   ```

3. Confirme que o arquivo esta na **raiz** do jogo, nao dentro de `Bin64/`:

   ```
   Crysis Remastered/
   ├── Bin64/
   ├── autoexec.cfg  <- AQUI (correto)
   ├── Engine/
   └── ...
   ```

---

## Ativar -devmode (recomendado)

Alguns cheats, em especial a vida infinita (`g_godMode`), precisam do modo de
desenvolvimento ativo para funcionar em todas as versoes do jogo.

1. Abra o Steam (Game Mode ou Desktop Mode)
2. Clique com botao direito em **Crysis Remastered**
3. Selecione **Propriedades**
4. Va em **Opcoes de Inicializacao**
5. Digite: `-devmode`
6. Feche a janela

> Sem `-devmode`, o jogo pode ignorar silenciosamente o `g_godMode 1`.

---

## Cheats incluidos

| CVar | Efeito | Requer -devmode? |
|------|--------|:---:|
| `g_godMode 1` | Vida infinita / invencibilidade | Sim |
| `i_unlimitedammo 1` | Municao infinita (pentes nao diminuem) | Nao |
| `pl_nanosuit_energy 99999` | Energia do Nanosuit maxima | Nao |
| `pl_fallDamage_SpeedFatal 9999` | Sem dano de queda | Nao |
| `pl_fallDamage_SpeedSafe 9999` | Sem dano de queda (velocidade segura) | Nao |

Cheats opcionais (comentados no arquivo, basta remover o `#`):

| CVar | Efeito |
|------|--------|
| `pl_moveSpeed 2.0` | Velocidade de movimento aumentada |
| `pl_jumpHeight 2.0` | Altura de pulo aumentada |
| `cl_fov 90` | Campo de visao em graus |

---

## Verificar se esta funcionando

1. Inicie o jogo normalmente pelo Steam
2. Carregue uma partida salva ou inicie uma nova campanha
3. Abra o console com a tecla **~** (til)
4. Digite `g_godMode` e pressione Enter
5. Deve aparecer: `g_godMode = 1`
   - Se aparecer `0`: veja a secao de solucao de problemas abaixo

---

## Solucao de problemas

**autoexec.cfg nao carrega (nenhum cheat funciona)**
- Verifique se o arquivo esta na RAIZ do jogo, nao dentro de `Bin64/`
- O arquivo deve se chamar exatamente `autoexec.cfg` (sem maiusculas, sem extensao extra)

**g_godMode nao ativa (continua levando dano)**
- Adicione `-devmode` nas Opcoes de Inicializacao do Steam (veja secao acima)
- Sem `-devmode`, o CryEngine pode ignorar este CVar em versoes retail

**Energia do Nanosuit nao fica cheia**
- Tente substituir `pl_nanosuit_energy 99999` por `pl_nanosuit_energy_max 99999`
  no arquivo `autoexec.cfg` (abra o arquivo e edite a linha correspondente)

**Jogo instalado em cartao SD**
- O script automatico pode nao encontrar o jogo
- Copie manualmente para o caminho do SD:
  ```bash
  cp autoexec.cfg /run/media/mmcblk0p1/steamapps/common/"Crysis Remastered"/
  ```
  (ajuste o caminho conforme o ponto de montagem do seu cartao)

**Quero desativar um cheat especifico**
- Edite o arquivo `autoexec.cfg` na pasta do jogo
- Coloque `#` na frente da linha que quer desativar
- Salve e reinicie o jogo

---

## Estrutura do projeto

```
games/crysis-remastered/
├── autoexec.cfg                  <- arquivo drop-in (este e o principal)
├── crysis_remastered_basic.ct    <- tabela Cheat Engine (metodo avancado)
├── GUIA_INSTALACAO.md            <- este guia
└── steamdeck/
    ├── install_steamdeck.sh      <- script de instalacao automatica
    ├── stl_launch.sh             <- launcher para Cheat Engine via STL
    ├── check_build.sh            <- valida se a build instalada esta travada
    ├── build.lock.example        <- modelo de lock de build
    └── stl_profile.env           <- exemplo de variaveis para STL
```

---

## Uso seguro

- Apenas campanha offline (single-player)
- Nao usar em multiplayer
- Nao inclui bypass de anti-cheat

---

## Metodo Avancado (Alternativo) — Cheat Engine

> Este metodo requer mais configuracao e conhecimento tecnico.
> Use apenas se o metodo `autoexec.cfg` nao funcionar ou se quiser cheats
> mais avancados que exigem manipulacao direta de memoria.

**Pre-requisitos:**
- Steam Deck em Desktop Mode
- Crysis Remastered instalado via Steam
- SteamTinkerLaunch (STL) instalado
- Cheat Engine disponivel no sistema (binario `cheatengine-x86_64`)

**Como instalar o Cheat Engine no SteamOS:**

1. Entre no Desktop Mode
2. Instale o ProtonUp-Qt pela Discover Store
3. Abra o ProtonUp-Qt e instale o SteamTinkerLaunch
4. Reinicie a Steam
5. No Crysis Remastered, habilite compatibilidade e selecione SteamTinkerLaunch
6. Baixe uma build Linux do Cheat Engine com o binario `cheatengine-x86_64`
7. Coloque em um caminho de usuario, por exemplo:
   - `/home/deck/tools/cheatengine`
8. De permissao de execucao:
   - `chmod +x /home/deck/tools/cheatengine/cheatengine-x86_64`
9. Configure o comando custom do STL para:
   - `/home/deck/tools/crysis-remastered-trainer/steamdeck/stl_launch.sh %command%`
10. Se necessario, configure variaveis de ambiente:
    - `CE_BIN=/home/deck/tools/cheatengine/cheatengine-x86_64`
    - `TABLE_PATH=/home/deck/tools/crysis-remastered-trainer/crysis_remastered_basic.ct`

**ATENCAO:** A tabela `crysis_remastered_basic.ct` foi entregue como template.
Os padroes AOB sao placeholders e precisam ser substituidos pelos bytes reais
da sua versao do jogo antes de funcionar. Sem isso, os scripts nao ativam.

**Hotkeys da tabela (apos configuracao dos AOBs):**
- `F1`: vida infinita
- `F2`: municao infinita
- `F3`: sem recuo
- `F12`: desativar tudo (panic key)
