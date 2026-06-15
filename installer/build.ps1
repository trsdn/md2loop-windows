<#
.SYNOPSIS
    Builds md2loop installer (publish + Inno Setup)
.PARAMETER Architecture
    Target architecture: x64 or arm64 (default: current machine)
.PARAMETER Version
    Version string for the installer (default: 1.0.0)
#>
param(
    [ValidateSet("x64", "arm64")]
    [string]$Architecture,
    [string]$Version = "1.0.0"
)

if (-not $Architecture) {
    $Architecture = if ([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -eq "Arm64") { "arm64" } else { "x64" }
}

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$publishDir = "$root\publish"
$distDir = "$root\dist"

Write-Host "🔨 Building md2loop v$Version for win-$Architecture..." -ForegroundColor Cyan

# Clean
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

# Publish self-contained single-file
Write-Host "📦 Publishing..." -ForegroundColor Yellow
dotnet publish "$root\md2loop\md2loop.csproj" `
    -c Release `
    -r "win-$Architecture" `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}

# Build installer with Inno Setup
$iscc = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    $iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $iscc)) {
    Write-Host "⚠️  Inno Setup not found. Skipping installer, creating ZIP only." -ForegroundColor Yellow
} else {
    Write-Host "🏗️  Building installer..." -ForegroundColor Yellow
    & $iscc "/DMyAppVersion=$Version" "$root\installer\md2loop.iss"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Installer build failed!" -ForegroundColor Red
        exit 1
    }
}

# Also create portable ZIP
$zipPath = "$distDir\md2loop-win-$Architecture-v$Version.zip"
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath
Write-Host ""
Write-Host "✅ Build complete!" -ForegroundColor Green
Write-Host "   Installer: $distDir\md2loop-setup-$Version.exe" -ForegroundColor White
Write-Host "   Portable:  $zipPath" -ForegroundColor White
