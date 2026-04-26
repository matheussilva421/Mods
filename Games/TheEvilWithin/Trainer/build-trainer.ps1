$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $root "src"
$outDir = Join-Path $root "dist"
$outExe = Join-Path $outDir "TheEvilWithinTrainer.exe"
$profilesOut = Join-Path $outDir "profiles"
$releaseDir = Join-Path $root "release"
$cheatDeckDir = Join-Path $releaseDir "cheat-deck"
$portableDir = Join-Path $releaseDir "portable"
$portableProfilesDir = Join-Path $portableDir "profiles"
$profileName = "the-evil-within-epic.json"
$cheatDeckExeName = "TheEvilWithin-CheatDeck.exe"
$cheatDeckExePath = Join-Path $cheatDeckDir $cheatDeckExeName
$cheatDeckReadmePath = Join-Path $cheatDeckDir "LEIA-ME-PTBR.md"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

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
Get-ChildItem $cheatDeckDir -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse
Get-ChildItem $portableDir -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse
New-Item -ItemType Directory -Force -Path $cheatDeckDir | Out-Null
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
    "/platform:x64",
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
Copy-Item $outExe (Join-Path $portableDir "TheEvilWithinTrainer.exe") -Force
Copy-Item (Join-Path $root "profiles\$profileName") $portableProfilesDir -Force

$cheatDeckReadme = @"
# Como usar

Arquivo principal:

- `TheEvilWithin-CheatDeck.exe`

Uso rapido:

1. abra The Evil Within pela Epic Games;
2. execute este trainer;
3. use as teclas abaixo;
4. use `F12` ou `8` para desligar todos os cheats ativos.

Hotkeys:

- `F1` Infinite Health
- `F2` Infinite Stamina
- `F3` Infinite Items
- `F4` Infinite Green Gel
- `F5` Infinite Parts
- `F6` Infinite Keys
- `F7` No Spread
- `F8` Freeze Enemies
- `F12` Disable all

Observacao:

- o perfil Epic esta embutido no `.exe`;
- a pasta `profiles` permite editar o perfil sem recompilar.
"@
Set-Content -Path $cheatDeckReadmePath -Value $cheatDeckReadme -Encoding UTF8

Write-Host "Built trainer to $outDir"
Write-Host "Prepared Cheat Deck package in $cheatDeckDir"
Write-Host "Prepared portable package in $portableDir"
