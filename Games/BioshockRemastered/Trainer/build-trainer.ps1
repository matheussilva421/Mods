$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $root "src"
$outDir = Join-Path $root "dist"
$outExe = Join-Path $outDir "BioshockRemasteredTrainer.exe"
$profilesOut = Join-Path $outDir "profiles"
$releaseDir = Join-Path $root "release"
$cheatDeckDir = Join-Path $releaseDir "cheat-deck"
$portableDir = Join-Path $releaseDir "portable"
$portableProfilesDir = Join-Path $portableDir "profiles"
$profileName = "bioshock-remastered.steam-gog-v1.0.122872.json"
$cheatDeckExeName = "BioshockRemastered-CheatDeck.exe"
$cheatDeckExePath = Join-Path $cheatDeckDir $cheatDeckExeName
$cheatDeckReadmePath = Join-Path $cheatDeckDir "LEIA-ME-PTBR.md"
$compiler = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if (-not (Test-Path $compiler)) {
    throw "Compiler not found: $compiler"
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $profilesOut | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
New-Item -ItemType Directory -Force -Path $cheatDeckDir | Out-Null
New-Item -ItemType Directory -Force -Path $portableDir | Out-Null
New-Item -ItemType Directory -Force -Path $portableProfilesDir | Out-Null

Get-ChildItem $profilesOut -Filter *.json -ErrorAction SilentlyContinue | Remove-Item -Force
if (Test-Path $cheatDeckDir) {
    Get-ChildItem $cheatDeckDir -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse
}
New-Item -ItemType Directory -Force -Path $cheatDeckDir | Out-Null
if (Test-Path $portableDir) {
    Get-ChildItem $portableDir -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse
}
New-Item -ItemType Directory -Force -Path $portableDir | Out-Null
New-Item -ItemType Directory -Force -Path $portableProfilesDir | Out-Null

$sources = @(
    (Join-Path $srcDir "Program.cs"),
    (Join-Path $srcDir "NativeMethods.cs"),
    (Join-Path $srcDir "ByteHelper.cs"),
    (Join-Path $srcDir "PatternScanner.cs"),
    (Join-Path $srcDir "EmbeddedProfile.cs"),
    (Join-Path $srcDir "TrainerProfile.cs"),
    (Join-Path $srcDir "ProcessMemory.cs"),
    (Join-Path $srcDir "MainForm.cs")
)

$arguments = @(
    "/nologo",
    "/target:winexe",
    "/platform:x86",
    "/out:$outExe",
    "/r:System.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Web.Extensions.dll"
) + $sources

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Copy-Item (Join-Path $root "profiles\$profileName") $profilesOut -Force
Copy-Item $outExe $cheatDeckExePath -Force
Copy-Item $outExe (Join-Path $portableDir "BioshockRemasteredTrainer.exe") -Force
Copy-Item (Join-Path $root "profiles\$profileName") $portableProfilesDir -Force

$cheatDeckReadme = @'
# Como usar

Arquivo principal:

- `BioshockRemastered-CheatDeck.exe`

Uso rapido:

1. copie esse `.exe` para o Steam Deck;
2. aponte o Cheat Deck para esse arquivo;
3. abra o jogo e carregue seu save;
4. abra o trainer depois que o save terminar de carregar;
5. use as teclas abaixo.

Hotkeys:

- `F1` God Mode
- `F2` Invisible
- `F3` Lock Consumables
- `F4` 1-Hit Kill Enemy
- `F5` No Alerts
- `F6` Protect Little Sister
- `F7` Unlock Gene Slots
- `F12` Disable all

Observacao:

- esse `.exe` ja tem o perfil embutido;
- para o caso simples, nao precisa copiar pasta `profiles`;
- Steam/GOG `v1.0.122872` sao o alvo validado por perfil;
- a build Epic conhecida usa executavel diferente (`FinalEpic` / `ChangeNumber=127355`) e sera bloqueada ate existir perfil proprio;
- ao voltar para o menu principal, use `F12` para restaurar os hooks antes de carregar outro save.
'@
Set-Content -Path $cheatDeckReadmePath -Value $cheatDeckReadme -Encoding UTF8

Write-Host "Built trainer to $outDir"
Write-Host "Prepared Cheat Deck package in $cheatDeckDir"
Write-Host "Prepared portable package in $portableDir"
