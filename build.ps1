# Copyright (c) 2026 KibaOfficial
# All rights reserved.

param(
    [string]$Version = "0.2.0"
)

$ErrorActionPreference = "Stop"

# ── Config ────────────────────────────────────────────────────────────────────
$InnoCompiler    = "F:\Inno Setup 6\ISCC.exe"
$AppProject      = "src/KCraft.App"
$InstallerDir    = "installer"
$WinPublishDir   = "publish/win-x64"
$LinuxPublishDir = "publish/linux-x64"
$WinSetupName    = "KCraft-v$Version-Setup.exe"
$LinuxTarName    = "KCraft-v$Version-linux-x64.tar.gz"

# ── Helpers ───────────────────────────────────────────────────────────────────
function Write-Step  { param($msg) Write-Host "  >> $msg" -ForegroundColor Cyan   }
function Write-Ok    { param($msg) Write-Host "  OK $msg" -ForegroundColor Green  }
function Write-Fail  { param($msg) Write-Host "  !! $msg" -ForegroundColor Red    }
function Write-Title { param($msg) Write-Host "`n$msg" -ForegroundColor Yellow    }

function Invoke-Step {
    param([string]$Label, [scriptblock]$Action)
    Write-Step $Label
    try   { & $Action; Write-Ok $Label }
    catch { Write-Fail "$Label failed: $_"; exit 1 }
}

# ── Banner ────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host " _  ______            __ _" -ForegroundColor DarkGreen
Write-Host "| |/ / ___|_ __ __ _ / _| |_" -ForegroundColor DarkGreen
Write-Host "| ' / |   | '__/ _' | |_| __|" -ForegroundColor Green
Write-Host "| . \ |___| | | (_| |  _| |_" -ForegroundColor Green
Write-Host "|_|\_\____|_|  \__,_|_|  \__|" -ForegroundColor Cyan
Write-Host ""
Write-Host " Release Build  v$Version" -ForegroundColor White
Write-Host " $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor DarkGray
Write-Host ""

# ── Clean ─────────────────────────────────────────────────────────────────────
Write-Title "[ 1 / 5 ]  Clean"
Invoke-Step "Removing old publish output" {
    if (Test-Path "publish") { Remove-Item "publish" -Recurse -Force }
    if (!(Test-Path $InstallerDir)) { New-Item -ItemType Directory -Path $InstallerDir | Out-Null }
}

# ── Windows Publish ───────────────────────────────────────────────────────────
Write-Title "[ 2 / 5 ]  Publish  Windows x64"
Invoke-Step "dotnet publish win-x64" {
    dotnet publish $AppProject -c Release -r win-x64 --self-contained true -o $WinPublishDir /p:Version=$Version 2>&1 | Out-Null
}

# ── Windows Installer ─────────────────────────────────────────────────────────
Write-Title "[ 3 / 5 ]  Installer  Windows (Inno Setup)"
if (!(Test-Path $InnoCompiler)) {
    Write-Host "  .. Inno Setup not found at '$InnoCompiler' — skipping installer" -ForegroundColor DarkYellow
} else {
    Invoke-Step "ISCC KCraft.iss" {
        & $InnoCompiler ".\KCraft.iss" /DMyAppVersion=$Version /Q 2>&1 | Out-Null
    }
}

# ── Linux Publish ─────────────────────────────────────────────────────────────
Write-Title "[ 4 / 5 ]  Publish  Linux x64"
Invoke-Step "dotnet publish linux-x64" {
    dotnet publish $AppProject -c Release -r linux-x64 --self-contained true -o $LinuxPublishDir /p:Version=$Version 2>&1 | Out-Null
}

# ── Linux tar.gz ──────────────────────────────────────────────────────────────
Write-Title "[ 5 / 5 ]  Archive  Linux tar.gz"
$linuxTarPath = Join-Path $InstallerDir $LinuxTarName
Invoke-Step "tar -czf $LinuxTarName" {
    if (Test-Path $linuxTarPath) { Remove-Item $linuxTarPath -Force }
    tar -czf $linuxTarPath -C $LinuxPublishDir .
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host " Build complete!  v$Version" -ForegroundColor Green
Write-Host "─────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host ""

$winPath = Join-Path $InstallerDir $WinSetupName
if (Test-Path $winPath) {
    $winSize = [math]::Round((Get-Item $winPath).Length / 1MB, 1)
    Write-Host "  Windows   installer/$WinSetupName  ($winSize MB)" -ForegroundColor Cyan
} else {
    Write-Host "  Windows   installer/$WinSetupName  (skipped)" -ForegroundColor DarkYellow
}

if (Test-Path $linuxTarPath) {
    $linuxSize = [math]::Round((Get-Item $linuxTarPath).Length / 1MB, 1)
    Write-Host "  Linux     $linuxTarPath  ($linuxSize MB)" -ForegroundColor Cyan
}

Write-Host ""