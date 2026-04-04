$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outDir = Join-Path $root "dist"
$outExe = Join-Path $outDir "Crysis2RemasteredTrainer.exe"
$profilesOut = Join-Path $outDir "profiles"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $compiler)) {
    throw "Compiler not found: $compiler"
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $profilesOut | Out-Null

$sources = @(
    (Join-Path $root "Program.cs"),
    (Join-Path $root "NativeMethods.cs"),
    (Join-Path $root "ByteHelper.cs"),
    (Join-Path $root "PatternScanner.cs"),
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

Get-ChildItem (Join-Path $root "profiles") -Filter *.json | Copy-Item -Destination $profilesOut -Force

Write-Host "Built trainer to $outDir"
