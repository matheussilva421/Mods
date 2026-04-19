$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $root "src"
$outDir = Join-Path $root "dist"
$outExe = Join-Path $outDir "Crysis3RemasteredTrainer.exe"
$profilesOut = Join-Path $outDir "profiles"
$releaseDir = Join-Path $root "release"
$cheatDeckDir = Join-Path $releaseDir "cheat-deck"
$portableDir = Join-Path $releaseDir "portable"
$portableProfilesDir = Join-Path $portableDir "profiles"
$cheatDeckExeName = "Crysis3Remastered-CheatDeck.exe"
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

Copy-Item (Join-Path $root "profiles\crysis3-remastered.fr-v1.4.json") $profilesOut -Force
Copy-Item $outExe $cheatDeckExePath -Force
Copy-Item $outExe (Join-Path $portableDir "Crysis3RemasteredTrainer.exe") -Force
Copy-Item (Join-Path $root "profiles\crysis3-remastered.fr-v1.4.json") $portableProfilesDir -Force

$cheatDeckReadme = @"
# Como usar

Arquivo principal:

- `Crysis3Remastered-CheatDeck.exe`

Uso rapido:

1. copie esse `.exe` para o Steam Deck;
2. aponte o Cheat Deck para esse arquivo;
3. abra o jogo junto com o trainer;
4. use as teclas abaixo.

Hotkeys:

- `F1` Lock Energy
- `F2` Lock Holster
- `F3` Lock Clip
- `F4` Lock Health
- `F5` 1-Hit Kill
- `F12` Disable all

Observacao:

- esse `.exe` ja tem o perfil embutido;
- para o caso simples, nao precisa copiar pasta `profiles`.
"@
Set-Content -Path $cheatDeckReadmePath -Value $cheatDeckReadme -Encoding UTF8

Write-Host "Built trainer to $outDir"
Write-Host "Prepared Cheat Deck package in $cheatDeckDir"
Write-Host "Prepared portable package in $portableDir"
