<#
    Revit Model Slimmer - installer

    Copies the add-in into the per-user Revit add-ins folder for every Revit version
    found on this computer (2023-2026). No administrator rights are needed.

    Double-click Install.bat, or run:
        powershell -NoProfile -ExecutionPolicy Bypass -File Install.ps1
    Force specific versions:
        powershell -NoProfile -ExecutionPolicy Bypass -File Install.ps1 -Versions 2024,2025
#>
[CmdletBinding()]
param(
    [string[]]$Versions
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$addinsRoot = Join-Path $env:APPDATA 'Autodesk\Revit\Addins'
$manifest = Join-Path $here 'RevitModelSlimmer.addin'

Write-Host ''
Write-Host '=== Revit Model Slimmer installer ===' -ForegroundColor Cyan
Write-Host ''

if (-not (Test-Path $manifest)) {
    Write-Host "RevitModelSlimmer.addin was not found next to this script ($here)." -ForegroundColor Red
    Write-Host 'Extract the whole zip first, then run the installer from the extracted folder.'
    exit 1
}

# Builds shipped in this package: one sub-folder per Revit version.
$available = Get-ChildItem -Path $here -Directory |
    Where-Object { $_.Name -match '^\d{4}$' -and (Test-Path (Join-Path $_.FullName 'RevitModelSlimmer.dll')) } |
    ForEach-Object { $_.Name } | Sort-Object
Write-Host "Builds in this package : $($available -join ', ')"

# Detect installed Revit versions (program folder, registry, or an existing add-ins folder).
$detected = @()
foreach ($v in $available) {
    $signals = @(
        "C:\Program Files\Autodesk\Revit $v\Revit.exe",
        "HKLM:\SOFTWARE\Autodesk\Revit\$v",
        (Join-Path $addinsRoot $v)
    )
    foreach ($s in $signals) {
        if (Test-Path $s) { $detected += $v; break }
    }
}
Write-Host "Revit versions detected: $(if ($detected) { $detected -join ', ' } else { 'none' })"

if ($Versions) {
    $targets = $Versions | ForEach-Object { $_.Trim() } | Where-Object { $_ }
} elseif ($detected) {
    $targets = $detected
} else {
    Write-Host ''
    Write-Host 'No Revit installation was detected automatically.' -ForegroundColor Yellow
    $answer = Read-Host "Type the Revit version(s) to install for, separated by commas (e.g. 2024), or press Enter for all ($($available -join ', '))"
    if ([string]::IsNullOrWhiteSpace($answer)) { $targets = $available } else { $targets = $answer -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ } }
}

$unsupported = $targets | Where-Object { $available -notcontains $_ }
if ($unsupported) {
    Write-Host "No build is available for Revit $($unsupported -join ', '). Supported: $($available -join ', ')." -ForegroundColor Yellow
    $targets = $targets | Where-Object { $available -contains $_ }
}
if (-not $targets) {
    Write-Host 'Nothing to install.' -ForegroundColor Red
    exit 1
}

if (Get-Process -Name 'Revit' -ErrorAction SilentlyContinue) {
    Write-Host ''
    Write-Host 'Revit is currently running. The add-in only appears after Revit is restarted,' -ForegroundColor Yellow
    Write-Host 'and an already-loaded copy cannot be overwritten. Close Revit if the copy fails.' -ForegroundColor Yellow
}

Write-Host ''
$installed = @()
foreach ($v in $targets) {
    $dest = Join-Path $addinsRoot $v
    $destDll = Join-Path $dest 'RevitModelSlimmer'
    try {
        New-Item -ItemType Directory -Force -Path $destDll | Out-Null
        Copy-Item -Path (Join-Path $here "$v\RevitModelSlimmer.dll") -Destination $destDll -Force
        Copy-Item -Path $manifest -Destination $dest -Force
        # Remove the "downloaded from the internet" flag, otherwise Revit may refuse to load the DLL.
        Unblock-File -Path (Join-Path $destDll 'RevitModelSlimmer.dll') -ErrorAction SilentlyContinue
        Unblock-File -Path (Join-Path $dest 'RevitModelSlimmer.addin') -ErrorAction SilentlyContinue
        Write-Host "  Revit $v : installed to $dest" -ForegroundColor Green
        $installed += $v
    } catch {
        Write-Host "  Revit $v : FAILED - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ''
if ($installed) {
    Write-Host "Done. Start (or restart) Revit $($installed -join ', ')." -ForegroundColor Cyan
    Write-Host 'When Revit asks about an unsigned add-in, click "Always Load".'
    Write-Host 'You will find the tools on the new "Model Slimmer" ribbon tab.'
} else {
    Write-Host 'Nothing was installed. Close Revit and run the installer again.' -ForegroundColor Red
    exit 1
}
