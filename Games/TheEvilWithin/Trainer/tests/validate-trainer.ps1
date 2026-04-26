$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$profilePath = Join-Path $root "profiles\the-evil-within-epic.json"
$sourceDir = Join-Path $root "src"
$requiredSources = @(
    "Program.cs",
    "NativeMethods.cs",
    "ByteHelper.cs",
    "PatternScanner.cs",
    "EmbeddedProfile.cs",
    "TrainerProfile.cs",
    "ProcessMemory.cs",
    "MainForm.cs"
)

if (-not (Test-Path $profilePath)) {
    throw "Profile not found: $profilePath"
}

foreach ($source in $requiredSources) {
    $path = Join-Path $sourceDir $source
    if (-not (Test-Path $path)) {
        throw "Source not found: $path"
    }
}

$profile = Get-Content -Raw $profilePath | ConvertFrom-Json
if ($profile.ProcessName -ne "EvilWithin.exe") {
    throw "Unexpected ProcessName: $($profile.ProcessName)"
}

if ($profile.ModuleName -ne "EvilWithin.exe") {
    throw "Unexpected ModuleName: $($profile.ModuleName)"
}

if (-not $profile.Cheats -or $profile.Cheats.Count -lt 1) {
    throw "Profile has no cheats."
}

$hotkeys = @{}
foreach ($cheat in $profile.Cheats) {
    if ([string]::IsNullOrWhiteSpace($cheat.Id)) {
        throw "A cheat has no Id."
    }

    if ([string]::IsNullOrWhiteSpace($cheat.Name)) {
        throw "Cheat $($cheat.Id) has no Name."
    }

    if ([string]::IsNullOrWhiteSpace($cheat.Hotkey)) {
        throw "Cheat $($cheat.Id) has no Hotkey."
    }

    if ($hotkeys.ContainsKey($cheat.Hotkey)) {
        throw "Duplicate hotkey $($cheat.Hotkey) for $($cheat.Id) and $($hotkeys[$cheat.Hotkey])."
    }

    $hotkeys[$cheat.Hotkey] = $cheat.Id

    if ($cheat.ActionType -eq "hook") {
        if ($cheat.OverwriteSize -lt 5) {
            throw "Hook cheat $($cheat.Id) has invalid OverwriteSize."
        }

        if ([string]::IsNullOrWhiteSpace($cheat.CaveBytes)) {
            throw "Hook cheat $($cheat.Id) has no CaveBytes."
        }
    }
}

function Convert-HexToBytes([string] $hex) {
    $hex.Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object {
        if ($_ -eq "?" -or $_ -eq "??") {
            -1
        }
        else {
            [Convert]::ToInt32($_, 16)
        }
    }
}

function Find-Pattern($data, $pattern) {
    $tokens = @(Convert-HexToBytes $pattern)
    if ($tokens.Count -eq 0 -or $data.Length -lt $tokens.Count) {
        return @()
    }

    $hits = New-Object System.Collections.Generic.List[int]
    $i = 0
    while ($i -le $data.Length - $tokens.Count) {
        if ($tokens[0] -ge 0) {
            $i = [Array]::IndexOf($data, [byte]$tokens[0], $i)
            if ($i -lt 0 -or $i -gt $data.Length - $tokens.Count) {
                break
            }
        }

        $matched = $true
        for ($j = 0; $j -lt $tokens.Count; $j++) {
            if ($tokens[$j] -ge 0 -and $data[$i + $j] -ne $tokens[$j]) {
                $matched = $false
                break
            }
        }

        if ($matched) {
            $hits.Add($i)
        }

        $i++
    }

    return @($hits)
}

$exePath = $env:EVIL_WITHIN_EXE
if ([string]::IsNullOrWhiteSpace($exePath)) {
    $exePath = "C:\Users\slvma\Downloads\EvilWithin.exe"
}

if (Test-Path $exePath) {
    $data = [System.IO.File]::ReadAllBytes($exePath)
    foreach ($cheat in $profile.Cheats) {
        if ([string]::IsNullOrWhiteSpace($cheat.Pattern)) {
            continue
        }

        $hits = @(Find-Pattern $data $cheat.Pattern)
        if ($hits.Count -ne 1) {
            throw "Pattern for $($cheat.Id) expected 1 hit, found $($hits.Count): $($cheat.Pattern)"
        }
    }

    Write-Host "Validated profile and binary signatures against $exePath"
}
else {
    Write-Host "Validated profile structure. Binary signature scan skipped because exe was not found: $exePath"
}
