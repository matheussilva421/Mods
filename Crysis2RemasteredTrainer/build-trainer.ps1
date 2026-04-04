$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outDir = Join-Path $root "dist"
$outExe = Join-Path $outDir "Crysis2RemasteredTrainer.exe"
$profilesOut = Join-Path $outDir "profiles"
$releaseDir = Join-Path $root "release"
$singleExeDir = Join-Path $releaseDir "single-exe"
$portableDir = Join-Path $releaseDir "portable"
$portableProfilesDir = Join-Path $portableDir "profiles"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $compiler)) {
    throw "Compiler not found: $compiler"
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $profilesOut | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
New-Item -ItemType Directory -Force -Path $singleExeDir | Out-Null
New-Item -ItemType Directory -Force -Path $portableDir | Out-Null
New-Item -ItemType Directory -Force -Path $portableProfilesDir | Out-Null

Get-ChildItem $profilesOut -Filter *.json -ErrorAction SilentlyContinue | Remove-Item -Force
if (Test-Path $singleExeDir) {
    Get-ChildItem $singleExeDir -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse
}
New-Item -ItemType Directory -Force -Path $singleExeDir | Out-Null
if (Test-Path $portableDir) {
    Get-ChildItem $portableDir -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse
}
New-Item -ItemType Directory -Force -Path $portableDir | Out-Null
New-Item -ItemType Directory -Force -Path $portableProfilesDir | Out-Null

$sources = @(
    (Join-Path $root "Program.cs"),
    (Join-Path $root "NativeMethods.cs"),
    (Join-Path $root "ByteHelper.cs"),
    (Join-Path $root "PatternScanner.cs"),
    (Join-Path $root "EmbeddedProfile.cs"),
    (Join-Path $root "TrainerProfile.cs"),
    (Join-Path $root "ProcessMemory.cs"),
    (Join-Path $root "MainForm.cs")
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

Copy-Item (Join-Path $root "profiles\crysis2-remastered.fr-v1.4.json") $profilesOut -Force
Copy-Item $outExe (Join-Path $singleExeDir "Crysis2RemasteredTrainer.exe") -Force
Copy-Item $outExe (Join-Path $portableDir "Crysis2RemasteredTrainer.exe") -Force
Copy-Item (Join-Path $root "profiles\crysis2-remastered.fr-v1.4.json") $portableProfilesDir -Force

Write-Host "Built trainer to $outDir"
Write-Host "Prepared single-exe package in $singleExeDir"
Write-Host "Prepared portable package in $portableDir"
