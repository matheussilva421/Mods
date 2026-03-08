# Guia de Instalacao — Crysis 3 Remastered (Steam Deck / PC)

---

## Como funciona

O CryEngine 3 carrega automaticamente o arquivo `autoexec.cfg` da pasta raiz
do jogo ao iniciar. Basta colocar o arquivo la e todos os cheats ativam
sozinhos, sem instalar nada extra.

---

## Instalacao rapida (Steam Deck)

### Opcao A — Script automatico

1. Entre no **Desktop Mode** (Steam > Ligar/Desligar > Ir para Desktop)
2. Abra o **Konsole** (menu de aplicativos > Sistema > Konsole)
3. Execute:

   ```bash
   bash ~/tools/crysis-3-remastered-trainer/steamdeck/install_steamdeck.sh
   ```

   > Ajuste o caminho se o repositorio estiver em outro lugar.

4. O script localiza o jogo e copia o `autoexec.cfg` automaticamente.

### Opcao B — Copia manual

```bash
cp autoexec.cfg ~/.steam/steam/steamapps/common/"Crysis 3 Remastered"/
```

Confirme que o arquivo esta na **raiz** do jogo, nao dentro de `Bin64/`:

```
Crysis 3 Remastered/
├── Bin64/
├── autoexec.cfg  <- CORRETO
├── Engine/
└── ...
```

---

## Ativar -devmode (recomendado para g_godMode)

1. Steam > botao direito em **Crysis 3 Remastered** > Propriedades
2. Opcoes de Inicializacao > digite: `-devmode`
3. Salve e inicie o jogo

> Sem `-devmode`, a vida infinita pode nao funcionar em versoes retail.

---

## Cheats incluidos

| CVar | Efeito | Requer -devmode? |
|------|--------|:---:|
| `g_godMode 1` | Invencibilidade | Sim |
| `i_unlimitedammo 1` | Municao infinita (todas as armas) | Nao |
| `g_huntingBowInfiniteAmmo 1` | Municao infinita do Predator Bow | Nao |
| `g_suitArmorHealthValue 500` | Armadura Nanosuit 2.0 maxima | Nao |
| `g_suitCloakEnergyDrainAdjuster 0` | Cloak sem gastar energia | Nao |
| `g_suitRecoilEnergyCost 0` | Modo poder sem gastar energia | Nao |
| `g_suitEnergyRechargeTime 0` | Recarga de energia instantanea | Nao |
| `g_suitEnergyRechargeDelay 0` | Sem delay de recarga | Nao |
| `pl_fallDamage_SpeedFatal 9999` | Sem dano de queda | Nao |

Cheats opcionais (descomentar no arquivo):

| CVar | Efeito |
|------|--------|
| `pl_moveSpeed 2.0` | Velocidade aumentada |
| `pl_jumpHeight 2.0` | Pulo mais alto |
| `cl_fov 90` | Campo de visao |
| `pl_maxSpeed 999` | Remove limite de velocidade |
| `i_recoil_suppression 1` | Sem recuo |

---

## Verificar se esta funcionando

1. Inicie o jogo pelo Steam
2. Pressione **~** (til) para abrir o console
3. Digite `g_godMode` e pressione Enter
4. Deve aparecer: `g_godMode = 1`

---

## Solucao de problemas

**Nenhum cheat funciona**
- Verifique se `autoexec.cfg` esta na raiz do jogo (nao dentro de `Bin64/`)
- O nome deve ser exatamente `autoexec.cfg` (sem maiusculas, sem `.txt`)

**g_godMode nao ativa**
- Adicione `-devmode` nas Opcoes de Inicializacao (veja acima)

**Armadura do Nanosuit nao funciona**
- Se `g_suitArmorHealthValue` falhar, use `g_godMode 1` como substituto

**Predator Bow sem municao infinita**
- Se `g_huntingBowInfiniteAmmo` nao funcionar, `i_unlimitedammo 1` cobre todas
  as armas incluindo o arco

**Jogo no cartao SD**
- Copie manualmente:
  ```bash
  cp autoexec.cfg /run/media/mmcblk0p1/steamapps/common/"Crysis 3 Remastered"/
  ```

**Desativar um cheat especifico**
- Edite o `autoexec.cfg` na pasta do jogo
- Adicione `#` na frente da linha e reinicie o jogo

---

## Estrutura do projeto

```
games/crysis-3-remastered/
├── autoexec.cfg                     <- arquivo drop-in (este e o principal)
├── crysis3_remastered_basic.ct      <- tabela Cheat Engine (metodo avancado)
├── GUIA_INSTALACAO.md               <- este guia
└── steamdeck/
    ├── install_steamdeck.sh         <- script de instalacao automatica
    ├── stl_launch.sh                <- launcher para Cheat Engine via STL
    ├── check_build.sh               <- valida se a build instalada esta travada
    ├── build.lock.example           <- modelo de lock de build
    └── stl_profile.env              <- exemplo de variaveis para STL
```

---

## Aviso

- Apenas campanha single-player offline
- Nao usar em multiplayer
- Nao inclui bypass de anti-cheat

---

## Metodo avancado — Cheat Engine (opcional)

> Use apenas se o `autoexec.cfg` nao funcionar ou precisar de cheats mais
> granulares via memoria.

**Pre-requisitos:**
- Steam Deck em Desktop Mode
- SteamTinkerLaunch (STL) instalado via ProtonUp-Qt
- Cheat Engine (binario Linux `cheatengine-x86_64`)

**Configuracao:**

1. Instale ProtonUp-Qt pela Discover Store
2. Instale SteamTinkerLaunch pelo ProtonUp-Qt
3. Reinicie o Steam
4. No Crysis 3, habilite compatibilidade e selecione SteamTinkerLaunch
5. Baixe o Cheat Engine (build Linux) e coloque em `~/tools/cheatengine/`
6. `chmod +x ~/tools/cheatengine/cheatengine-x86_64`
7. No STL, configure o comando customizado:
   ```
   ~/tools/crysis-3-remastered-trainer/steamdeck/stl_launch.sh %command%
   ```
8. Configure as variaveis:
   ```
   CE_BIN=~/tools/cheatengine/cheatengine-x86_64
   TABLE_PATH=~/tools/crysis-3-remastered-trainer/crysis3_remastered_basic.ct
   ```

**ATENCAO:** A tabela `.ct` e um template. Os padraos AOB sao placeholders
e precisam ser substituidos pelos bytes reais da sua versao do jogo.

**Hotkeys (apos configurar os AOBs):**
- `F1`: vida infinita
- `F2`: municao infinita
- `F3`: energia Nanosuit infinita
- `F4`: sem recuo
- `F12`: desativar tudo (panic key)
